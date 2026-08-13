using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>Plain-number summary of a timetable, shown above the grid - no charts, just counts.</summary>
    public class ScheduleStatistics
    {
        public int TotalHours { get; init; }
        public int LectureHours { get; init; }
        public int ExerciseHours { get; init; }
        public int LabHours { get; init; }
        public int TotalGapHours { get; init; }
        public int? EarliestStartHour { get; init; }
        public int? LatestEndHour { get; init; }
        public IReadOnlyList<string> FreeDays { get; init; } = [];
    }

    /// <summary>Pure computation over the current schedule - no ASP.NET dependency.</summary>
    public static class ScheduleStatisticsCalculator
    {
        public static ScheduleStatistics Compute(IReadOnlyList<ScheduleItem> items, IReadOnlyList<string> allDays)
        {
            int HoursOfType(string type) => items.Where(i => i.Type == type).Sum(i => i.Duration);

            int totalGapHours = items
                .GroupBy(i => i.Day)
                .Sum(dayItems =>
                {
                    var sorted = dayItems.OrderBy(i => i.StartHour).ToList();
                    int gap = 0;
                    for (int i = 1; i < sorted.Count; i++)
                    {
                        var idle = sorted[i].StartHour - (sorted[i - 1].StartHour + sorted[i - 1].Duration);
                        if (idle > 0)
                        {
                            gap += idle;
                        }
                    }
                    return gap;
                });

            var daysWithClasses = items.Select(i => i.Day).ToHashSet();
            var freeDays = allDays.Where(d => !daysWithClasses.Contains(d)).ToList();

            return new ScheduleStatistics
            {
                TotalHours = items.Sum(i => i.Duration),
                LectureHours = HoursOfType("P"),
                ExerciseHours = HoursOfType("C"),
                LabHours = HoursOfType("L"),
                TotalGapHours = totalGapHours,
                EarliestStartHour = items.Count == 0 ? null : items.Min(i => i.StartHour),
                LatestEndHour = items.Count == 0 ? null : items.Max(i => i.StartHour + i.Duration),
                FreeDays = freeDays,
            };
        }
    }
}
