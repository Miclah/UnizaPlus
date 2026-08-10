using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>Pure time-slot overlap logic, kept separate from session/HTTP concerns so it stays unit-testable.</summary>
    public static class ScheduleOverlapChecker
    {
        public static bool IsAvailable(IReadOnlyList<ScheduleItem> items, string day, int startHour, int duration, int excludeItemId)
        {
            bool hasOverlap = items.Any(item =>
                item.Id != excludeItemId &&
                item.Day == day &&
                (startHour < item.StartHour + item.Duration) &&
                (item.StartHour < startHour + duration));

            return !hasOverlap && IsWithinBoundaries(startHour, duration);
        }

        /// <summary>
        /// Grid-range check only, no overlap check. Used by the drag-to-move API: unlike
        /// creating/editing an item by hand, dragging is allowed to create a conflict (the
        /// grid highlights it) - it just can't leave the rendered 7:00-21:00 range.
        /// </summary>
        public static bool IsWithinBoundaries(int startHour, int duration) => startHour >= 7 && (startHour + duration) <= 21;

        /// <summary>
        /// Ids of items that overlap another item on the same day. Two items can overlap
        /// without sharing the same StartHour (e.g. 8-10 and 9-11), so this can't be
        /// detected from CSS/markup alone - the grid renders each item only at its own
        /// start cell and relies on absolute positioning to visually span the rest.
        /// </summary>
        public static HashSet<int> FindConflictingItemIds(IReadOnlyList<ScheduleItem> items)
        {
            var conflicting = new HashSet<int>();

            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    var a = items[i];
                    var b = items[j];

                    bool overlaps = a.Day == b.Day &&
                        a.StartHour < b.StartHour + b.Duration &&
                        b.StartHour < a.StartHour + a.Duration;

                    if (overlaps)
                    {
                        conflicting.Add(a.Id);
                        conflicting.Add(b.Id);
                    }
                }
            }

            return conflicting;
        }
    }
}
