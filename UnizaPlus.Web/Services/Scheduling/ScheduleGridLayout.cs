using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>Where one item sits within its group of mutually-overlapping items on the same day.</summary>
    public readonly record struct ScheduleItemPosition(ScheduleItem Item, int ColumnIndex, int ColumnCount);

    /// <summary>
    /// Assigns each item a column index/count within its day so overlapping items render
    /// side by side instead of stacked on top of each other (Google Calendar-style packing).
    /// Pure and day-agnostic: call once per day's items.
    /// </summary>
    public static class ScheduleGridLayout
    {
        public static List<ScheduleItemPosition> ComputeDayLayout(IEnumerable<ScheduleItem> dayItems)
        {
            var items = dayItems.OrderBy(i => i.StartHour).ThenBy(i => i.Id).ToList();
            if (items.Count == 0)
            {
                return [];
            }

            var clusters = GroupIntoOverlapClusters(items);
            var result = new List<ScheduleItemPosition>(items.Count);

            foreach (var cluster in clusters)
            {
                // Greedy interval-graph coloring: walk items in start-time order, placing each
                // in the first column whose previous occupant has already ended.
                var columnEndHour = new List<int>();
                var columnByItemId = new Dictionary<int, int>();

                foreach (var item in cluster)
                {
                    int column = columnEndHour.FindIndex(endHour => endHour <= item.StartHour);
                    if (column < 0)
                    {
                        column = columnEndHour.Count;
                        columnEndHour.Add(0);
                    }

                    columnEndHour[column] = item.StartHour + item.Duration;
                    columnByItemId[item.Id] = column;
                }

                int columnCount = columnEndHour.Count;
                foreach (var item in cluster)
                {
                    result.Add(new ScheduleItemPosition(item, columnByItemId[item.Id], columnCount));
                }
            }

            return result;
        }

        private static List<List<ScheduleItem>> GroupIntoOverlapClusters(List<ScheduleItem> sortedItems)
        {
            var clusters = new List<List<ScheduleItem>>();

            foreach (var item in sortedItems)
            {
                var touching = clusters.Where(cluster => cluster.Any(existing => Overlaps(existing, item))).ToList();

                if (touching.Count == 0)
                {
                    clusters.Add([item]);
                    continue;
                }

                var target = touching[0];
                target.Add(item);
                foreach (var merged in touching.Skip(1))
                {
                    target.AddRange(merged);
                    clusters.Remove(merged);
                }
            }

            return clusters;
        }

        private static bool Overlaps(ScheduleItem a, ScheduleItem b) =>
            a.StartHour < b.StartHour + b.Duration && b.StartHour < a.StartHour + a.Duration;
    }
}
