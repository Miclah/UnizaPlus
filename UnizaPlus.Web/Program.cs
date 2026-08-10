using UnizaPlus.Web.Services;
using UnizaPlus.Web.Services.Scheduling;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapRazorPages();
app.MapControllers();

app.Run();
