using UnizaPlus.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddControllers();

//pomoc AI
builder.Services.AddScoped<ScheduleService>(provider => {
    var logger = provider.GetRequiredService<ILogger<ScheduleService>>();
    return new ScheduleService(logger); 
});

builder.Services.AddScoped<ScraperService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

//pomoc AI
using (var scope = app.Services.CreateScope())
{
    var scraperService = scope.ServiceProvider.GetRequiredService<ScraperService>();
    var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
    
    string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
    string filePath = Path.Combine(solutionDir, "schedule.csv");
    
    if (!System.IO.File.Exists(filePath))
    {
        await scraperService.RunAutoScraperAsync();
    }
}

app.Run();