using System.Diagnostics;
using UnizaPlusBackEnd.Models;

namespace UnizaPlus.Web.Services
{
    public class ScraperService
    {
        // pomoc AI
        private readonly ILogger<ScraperService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ScraperService(ILogger<ScraperService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private ScheduleService GetScheduleService()
        {
            return _serviceProvider.GetRequiredService<ScheduleService>();
        }

        public async Task<bool> RunScraperAsync(string username, string password)
        {
            try
            {
                // pomoc s AI
                foreach (var proc in Process.GetProcessesByName("UnizaPlusBackEnd"))
                {
                    try
                    {
                        proc.Kill();
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Failed to kill existing process: {ex.Message}");
                    }
                }

                string projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
                string scraperPath = Path.Combine(projectDir, "UnizaPlusBackEnd", "bin", "Debug", "net8.0", "UnizaPlusBackEnd.exe");

                var processInfo = new ProcessStartInfo
                {
                    FileName = scraperPath,
                    Arguments = $"--username \"{username}\" --password \"{password}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return false;
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return false;
                }

                await RefreshScheduleFromFileAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running scraper");
                return false;
            }
        }

        private async Task RefreshScheduleFromFileAsync()
        {
            try
            {
                string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
                string outputPath = Path.Combine(solutionDir, "schedule.csv");

                if (File.Exists(outputPath))
                {
                    
                    await Task.CompletedTask; 
                    GetScheduleService().ReloadScheduleData();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing schedule from file");
            }
        }

        public async Task<bool> RunAutoScraperAsync()
        {
            try
            {
                string projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
                string scraperPath = Path.Combine(projectDir, "UnizaPlusBackEnd", "bin", "Debug", "net8.0", "UnizaPlusBackEnd.exe");
                string username = "***REMOVED-USERNAME***";
                string password = "***REMOVED-PASSWORD***";

                // pomoc s AI
                var processInfo = new ProcessStartInfo
                {
                    FileName = scraperPath,
                    Arguments = $"--username \"{username}\" --password \"{password}\" --auto",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return false;
                }

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return false;
                }

                await RefreshScheduleFromFileAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running auto scraper");
                return false;
            }
        }
    }
}