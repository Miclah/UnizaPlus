using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using UnizaPlus.Models;
using UnizaPlus.Web.Services;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Web.Pages
{
    public class IndexModel(ScheduleService scheduleService, IStringLocalizer<SharedResource> localizer) : PageModel
    {
        private const int MinSupportedHour = 7;
        private const int MaxSupportedHour = 20;

        private readonly ScheduleService _scheduleService = scheduleService;

        public List<ScheduleItem> ScheduleItems { get; set; } = [];
        public HashSet<int> ConflictingItemIds { get; set; } = [];
        public Dictionary<int, ScheduleItemPosition> ItemPositions { get; set; } = [];
        public IReadOnlyList<string> Days { get; } = ScheduleDays.All;
        public int GridStartHour { get; set; } = MinSupportedHour;
        public int GridEndHour { get; set; } = MaxSupportedHour;
        public bool IsScrapingInProgress { get; set; }
        public string? ErrorMessage { get; set; }
        public bool NoScheduleData { get; set; }
        public ScheduleStatistics Stats { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(bool refresh = false)
        {
            if (refresh)
            {
                IsScrapingInProgress = true;
                bool refreshResult = await _scheduleService.RefreshFromSourceAsync();
                if (!refreshResult)
                {
                    ErrorMessage = localizer["Failed to refresh schedule data. Please try again later."];
                }
                return RedirectToPage();
            }

            ScheduleItems = await _scheduleService.GetScheduleAsync();
            NoScheduleData = ScheduleItems.Count == 0;
            ConflictingItemIds = ScheduleOverlapChecker.FindConflictingItemIds(ScheduleItems);
            Stats = ScheduleStatisticsCalculator.Compute(ScheduleItems, Days);

            if (ScheduleItems.Count > 0)
            {
                // Trim empty leading/trailing hour columns, with a one-hour margin, instead
                // of always rendering the full 7:00-20:00 range regardless of actual data.
                var earliestStart = ScheduleItems.Min(i => i.StartHour);
                var latestStart = ScheduleItems.Max(i => i.StartHour);
                GridStartHour = Math.Max(MinSupportedHour, earliestStart - 1);
                GridEndHour = Math.Min(MaxSupportedHour, latestStart + 1);
            }

            ItemPositions = Days
                .SelectMany(day => ScheduleGridLayout.ComputeDayLayout(ScheduleItems.Where(i => i.Day == day)))
                .ToDictionary(position => position.Item.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostResetAsync()
        {
            await _scheduleService.ResetToSampleAsync();
            return RedirectToPage();
        }

        /// <summary>True when this item shares its time slot with at least one other item (so it was split into columns).</summary>
        public bool IsNarrow(int itemId) => ItemPositions.TryGetValue(itemId, out var position) && position.ColumnCount > 1;

        /// <summary>Inline left/width for one item's column slice, expressed relative to a single hour-cell's width.</summary>
        public string GetItemStyle(int itemId, int duration)
        {
            if (!ItemPositions.TryGetValue(itemId, out var position))
            {
                return string.Empty;
            }

            double widthPercent = 100.0 * duration / position.ColumnCount;
            double leftPercent = position.ColumnIndex * widthPercent;

            return FormattableString.Invariant($"left: calc({leftPercent:0.###}% + 2px); width: calc({widthPercent:0.###}% - 4px);");
        }

        public static string GetTypeLabel(string type, IStringLocalizer<SharedResource> localizer) => type switch
        {
            "P" => localizer["Lecture"],
            "C" => localizer["Exercise"],
            "L" => localizer["Lab"],
            _ => type,
        };
    }
}
