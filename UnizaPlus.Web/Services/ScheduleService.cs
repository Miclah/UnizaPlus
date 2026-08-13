using UnizaPlus.Models;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Web.Services
{
    /// <summary>
    /// Per-request façade over the current visitor's session schedule.
    /// Data comes from the configured IScheduleProvider (Csv or Live) on
    /// first access per session, then lives only in that session's slot in
    /// SessionScheduleStore - edits are never written back to disk and never
    /// visible to other visitors.
    /// </summary>
    public class ScheduleService(
        IHttpContextAccessor httpContextAccessor,
        SessionScheduleStore sessionStore,
        IScheduleProvider scheduleProvider,
        CsvScheduleProvider sampleProvider,
        ILogger<ScheduleService> logger)
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly SessionScheduleStore _sessionStore = sessionStore;
        private readonly IScheduleProvider _scheduleProvider = scheduleProvider;
        private readonly CsvScheduleProvider _sampleProvider = sampleProvider;
        private readonly ILogger<ScheduleService> _logger = logger;

        private HttpContext HttpContext =>
            _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("ScheduleService requires an active HTTP request.");

        public Task<List<ScheduleItem>> GetScheduleAsync() =>
            _sessionStore.GetOrCreateAsync(HttpContext, () => _scheduleProvider.GetScheduleAsync());

        public async Task<ScheduleItem?> GetScheduleItemAsync(int id)
        {
            var items = await GetScheduleAsync();
            return items.FirstOrDefault(i => i.Id == id);
        }

        public async Task UpdateScheduleItemAsync(ScheduleItem item)
        {
            var items = await GetScheduleAsync();
            var index = items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                items[index] = item;
                await _sessionStore.SetAsync(HttpContext, items);
            }
        }

        /// <summary>Updates an existing item (matched by Id) or appends a new one with a freshly assigned Id.</summary>
        public async Task<int> AddOrUpdateScheduleItemAsync(ScheduleItem item)
        {
            var items = await GetScheduleAsync();
            var index = items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                items[index] = item;
            }
            else
            {
                item.Id = items.Count == 0 ? 1 : items.Max(i => i.Id) + 1;
                items.Add(item);
            }

            await _sessionStore.SetAsync(HttpContext, items);
            return item.Id;
        }

        public async Task UpdateAllScheduleItemsAsync(List<ScheduleItem> items)
        {
            await _sessionStore.SetAsync(HttpContext, items);
            _logger.LogInformation("Replaced session schedule with {Count} items", items.Count);
        }

        public async Task ResetToSampleAsync()
        {
            var sample = await _sampleProvider.GetScheduleAsync();
            await _sessionStore.SetAsync(HttpContext, sample);
            _logger.LogInformation("Session schedule reset to sample data ({Count} items)", sample.Count);
        }

        /// <summary>Reloads from whichever IScheduleProvider is configured (Csv re-reads the file, Live re-scrapes).</summary>
        public async Task<bool> RefreshFromSourceAsync()
        {
            try
            {
                var items = await _scheduleProvider.GetScheduleAsync();
                if (items.Count == 0)
                {
                    // Don't silently blank out a working session on a soft failure
                    // (e.g. a scrape that "succeeded" but produced no rows).
                    _logger.LogWarning("Refresh produced 0 schedule items; leaving the current session schedule untouched.");
                    return false;
                }

                await _sessionStore.SetAsync(HttpContext, items);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing schedule from configured source");
                return false;
            }
        }

        public async Task<bool> IsTimeSlotAvailableAsync(string day, int startHour, int duration, int excludeItemId)
        {
            var items = await GetScheduleAsync();
            return ScheduleOverlapChecker.IsAvailable(items, day, startHour, duration, excludeItemId);
        }
    }
}
