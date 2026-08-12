using System.Globalization;
using Microsoft.AspNetCore.Localization;
using UnizaPlus.Web.Services;
using UnizaPlus.Web.Services.Scheduling;

var builder = WebApplication.CreateBuilder(args);

// No ResourcesPath: the SDK's resx->manifest-resource naming already strips a folder
// literally named "Resources" (Resources/SharedResource.resx embeds as
// "UnizaPlus.Web.SharedResource.resources"), so ResourcesPath = "Resources" here would
// make IStringLocalizer<SharedResource> look for a name that doesn't exist.
builder.Services.AddLocalization();
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
    options.Cookie.HttpOnly = true;
});

builder.Services.AddSingleton<SessionScheduleStore>();
builder.Services.AddScoped<CsvScheduleProvider>();

// UnizaPlus:DataSource selects the schedule source: "Csv" (default, no external
// dependencies) or "Live" (scrapes vzdelavanie.uniza.sk via the UnizaPlusBackEnd
// Selenium process). In Csv mode, SeleniumScheduleProvider is never registered,
// so it can never be instantiated.
var dataSource = builder.Configuration["UnizaPlus:DataSource"] ?? "Csv";
if (string.Equals(dataSource, "Live", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<SeleniumScheduleProvider>();
    builder.Services.AddScoped<IScheduleProvider>(sp => sp.GetRequiredService<SeleniumScheduleProvider>());
}
else
{
    builder.Services.AddScoped<IScheduleProvider>(sp => sp.GetRequiredService<CsvScheduleProvider>());
}

builder.Services.AddScoped<ScheduleService>();

var supportedCultures = new[] { "en", "sk" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
localizationOptions.RequestCultureProviders = [new CookieRequestCultureProvider()];

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Catches responses that would otherwise be an empty-body 4xx/5xx (a 404 from routing, for
// example) and re-executes the pipeline against a page that renders something in the site's
// own style instead of a bare status code. Unhandled exceptions still go through
// UseExceptionHandler("/Error") above, not through here.
app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization(localizationOptions);
app.UseRouting();
app.UseSession();

// Liveness endpoint for an external cron pinger that keeps the Azure App Service F1 container
// warm (F1 has no Always On, so it sleeps after idling and the next visitor would otherwise eat
// the cold start). Deliberately trivial: no CSV read, no session, no auth, nothing about the
// environment/version in the body - just a bare 200. Map* calls don't control middleware order -
// ASP.NET Core dispatches endpoints after every app.Use* above regardless of where MapGet is
// written in this file - so this route still passes through UseHttpsRedirection, UseStaticFiles,
// UseRequestLocalization, UseRouting and UseSession. That's fine: none of them touch the response
// for a 200 (UseStatusCodePagesWithReExecute only reacts to 4xx/5xx), localization only reads a
// cookie, and UseSession is lazy and never triggered since the handler doesn't touch HttpContext.
// Session. Point the cron job at the https:// URL directly, though - UseHttpsRedirection still
// applies here, and an http:// ping would pay for a redirect round trip on every single check.
// There's no request-logging middleware in this app today (Microsoft.AspNetCore is already
// capped at Warning in appsettings.json, so ASP.NET Core's own per-request "Request
// starting/finished" logs are already off for every route); if HTTP logging or App Insights is
// added later, exclude this path there too.
app.MapGet("/health", () => Results.Ok());

// Sets the UI language cookie and redirects back. A GET endpoint (rather than a form POST)
// keeps the language switcher a plain link in the header; it only ever changes a display
// preference cookie, so it carries none of the risk a CSRF-protected state change would.
app.MapGet("/set-language", (string culture, string returnUrl, HttpContext context) =>
{
    if (Array.IndexOf(supportedCultures, culture) < 0)
    {
        culture = "en";
    }

    context.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

    return Results.LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

app.MapRazorPages();
app.MapControllers();

app.Run();
