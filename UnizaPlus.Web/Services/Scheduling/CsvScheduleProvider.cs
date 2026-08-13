using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>
    /// Loads the schedule from a CSV file on disk. Used as the default provider
    /// (UnizaPlus:DataSource = Csv) and, pointed at the sample file, by the
    /// session Reset action regardless of the configured data source.
    /// </summary>
    public class CsvScheduleProvider(IConfiguration configuration, ILogger<CsvScheduleProvider> logger) : IScheduleProvider
    {
        private readonly ILogger<CsvScheduleProvider> _logger = logger;
        private readonly string _filePath = ResolvePath(configuration);

        public static string SampleFilePath => Path.Combine(AppContext.BaseDirectory, "sample-data", "schedule.csv");

        private static string ResolvePath(IConfiguration configuration)
        {
            var configured = configuration["UnizaPlus:CsvPath"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                return SampleFilePath;
            }

            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured);
        }

        public Task<List<ScheduleItem>> GetScheduleAsync() => LoadFromFileAsync(_filePath, _logger);

        public static async Task<List<ScheduleItem>> LoadFromFileAsync(string filePath, ILogger logger)
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("CSV schedule file not found: {FilePath}", filePath);
                return [];
            }

            using var reader = new StreamReader(filePath);
            var result = await ScheduleCsvParser.ParseAsync(reader);

            foreach (var warning in result.Warnings)
            {
                logger.LogWarning("{FilePath}: {Warning}", filePath, warning);
            }

            logger.LogInformation("Loaded {Count} schedule items from {FilePath}", result.Items.Count, filePath);
            return result.Items;
        }
    }
}
