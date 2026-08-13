using System.Diagnostics;
using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>
    /// Live data source: launches the existing UnizaPlusBackEnd scraper as a
    /// separate process (exactly as UnizaPlus.Web/Services/ScrapeService.cs
    /// did before this refactor) and reads back the CSV file it writes.
    ///
    /// This provider does not reference Selenium and does not contain any
    /// scraping logic itself - none of that changed. It is only registered
    /// in DI when UnizaPlus:DataSource is "Live" (see Program.cs), so in the
    /// default "Csv" mode this type is never constructed and the scraper
    /// process is never started.
    /// </summary>
    public class SeleniumScheduleProvider(ILogger<SeleniumScheduleProvider> logger, IConfiguration configuration) : IScheduleProvider
    {
        private readonly ILogger<SeleniumScheduleProvider> _logger = logger;
        private readonly IConfiguration _configuration = configuration;

        public async Task<List<ScheduleItem>> GetScheduleAsync()
        {
            try
            {
                string? username = _configuration["UnizaPlus:Live:Username"];
                string? password = _configuration["UnizaPlus:Live:Password"];
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("Live mode requires UnizaPlus:Live:Username and UnizaPlus:Live:Password (or the UnizaPlus__Live__Username / UnizaPlus__Live__Password environment variables) to be configured.");
                    return [];
                }

                foreach (var proc in Process.GetProcessesByName("UnizaPlusBackEnd"))
                {
                    try
                    {
                        proc.Kill();
                        await Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to kill existing scraper process");
                    }
                }

                string projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
                string scraperPath = Path.Combine(projectDir, "UnizaPlusBackEnd", "bin", "Debug", "net10.0", "UnizaPlusBackEnd.exe");
                string outputPath = Path.Combine(projectDir, "schedule.csv");

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
                    _logger.LogError("Failed to start scraper process at {ScraperPath}", scraperPath);
                    return [];
                }

                await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Scraper process exited with code {ExitCode}", process.ExitCode);
                    return [];
                }

                return await ReadScraperOutputAsync(outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running live scraper");
                return [];
            }
        }

        /// <summary>
        /// Reads the scraper's own output format, written unchanged by
        /// UnizaPlusBackEnd/Services/FileService.cs:
        /// Id,Day,StartHour,Duration,Type,Professor,Classroom,Subject,SubjectCode,StudentGroups,Color
        /// This is a different, older format than the demo/upload CSV format
        /// handled by ScheduleCsvParser - the two are intentionally separate.
        /// </summary>
        private async Task<List<ScheduleItem>> ReadScraperOutputAsync(string filePath)
        {
            var items = new List<ScheduleItem>();

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Scraper output file not found: {FilePath}", filePath);
                return items;
            }

            var lines = await File.ReadAllLinesAsync(filePath);
            int nextId = 1;

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var fields = CsvLineSplitter.Split(line);
                    if (fields.Count < 10)
                    {
                        _logger.LogWarning("Skipping scraper output line {Line}: not enough fields", i + 1);
                        continue;
                    }

                    var item = new ScheduleItem
                    {
                        Id = nextId++,
                        Day = fields[1],
                        StartHour = int.Parse(fields[2]),
                        Duration = int.Parse(fields[3]),
                        Type = fields[4],
                        Professor = fields[5],
                        Classroom = fields[6],
                        Subject = fields[7],
                        SubjectCode = fields[8],
                        StudentGroups = fields[9]
                    };

                    if (fields.Count > 10 && !string.IsNullOrEmpty(fields[10]))
                    {
                        item.Color = fields[10];
                    }
                    else
                    {
                        item.InitializeColor();
                    }

                    items.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping malformed scraper output line {Line}", i + 1);
                }
            }

            return items;
        }
    }
}
