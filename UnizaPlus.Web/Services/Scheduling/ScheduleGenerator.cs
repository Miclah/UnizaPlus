using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>
    /// Optional soft ranking criteria for generated schedules. These never change which
    /// combinations are considered valid - only how equally-good ones (same conflict count)
    /// are ordered against each other.
    /// </summary>
    public class ScheduleGenerationPreferences
    {
        public bool NoEarlyMornings { get; set; }
        public bool MinimizeGaps { get; set; }
        public bool FreeFriday { get; set; }
    }

    /// <summary>
    /// One selectable block: every item here shares Subject and Type and has a non-empty
    /// StudentGroups, differing only by which group it belongs to. The generator must include
    /// exactly one alternative from each block in every candidate timetable.
    /// </summary>
    public class ScheduleAlternativeBlock(string subject, string type, IReadOnlyList<ScheduleItem> alternatives)
    {
        public string Subject { get; } = subject;
        public string Type { get; } = type;
        public IReadOnlyList<ScheduleItem> Alternatives { get; } = alternatives;
    }

    /// <summary>One full candidate timetable produced by the generator.</summary>
    public class GeneratedSchedule(IReadOnlyList<ScheduleItem> items, int conflictCount, double preferenceScore)
    {
        public IReadOnlyList<ScheduleItem> Items { get; } = items;

        /// <summary>Number of overlapping item pairs. 0 means this timetable has no time conflicts.</summary>
        public int ConflictCount { get; } = conflictCount;

        /// <summary>Lower is better. Only meaningful when comparing variants with the same ConflictCount.</summary>
        public double PreferenceScore { get; } = preferenceScore;
    }

    public class ScheduleGenerationResult(IReadOnlyList<GeneratedSchedule> variants, int blockCount)
    {
        /// <summary>Always at least one entry - the fixed items alone if there are no blocks at all.</summary>
        public IReadOnlyList<GeneratedSchedule> Variants { get; } = variants;

        /// <summary>How many alternative-choice blocks were found in the input.</summary>
        public int BlockCount { get; } = blockCount;

        /// <summary>True when every returned variant has zero time conflicts.</summary>
        public bool IsConflictFree => Variants.Count > 0 && Variants[0].ConflictCount == 0;

        /// <summary>The lowest conflict count achieved by any explored combination (0 if a clean schedule exists).</summary>
        public int BestConflictCount => Variants.Count > 0 ? Variants[0].ConflictCount : 0;
    }

    /// <summary>
    /// Picks one alternative per <see cref="ScheduleAlternativeBlock"/> so the resulting timetable
    /// has as few time conflicts as possible - ideally none - then ranks the equally-good results
    /// by whichever preferences are enabled. Plain backtracking with branch-and-bound pruning on
    /// the running conflict count; no external solver, no I/O, no ASP.NET dependency, so it can be
    /// unit tested directly. See ExtractBlocks/ExtractFixedItems for the data-shape rules (same
    /// Subject+Type with different, non-empty StudentGroups = alternatives of one block; empty
    /// StudentGroups, or a Subject+Type with no sibling group, is fixed).
    /// </summary>
    public class ScheduleGenerator
    {
        // Hard safety net so a pathological input can't hang a request. The exhaustive,
        // pruned search finishes in well under this for any realistically sized timetable -
        // see ScheduleGeneratorTests for the pruning behaviour this relies on.
        private const int MaxNodesVisited = 200_000;
        private const int MaxVariantsCollected = 200;

        /// <summary>Groups items that share Subject+Type, have a non-empty StudentGroups, and have at least one sibling (2+ such items) - i.e. an actual choice.</summary>
        public static IReadOnlyList<ScheduleAlternativeBlock> ExtractBlocks(IReadOnlyList<ScheduleItem> items)
        {
            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.StudentGroups))
                .GroupBy(i => (i.Subject, i.Type))
                .Where(g => g.Count() >= 2)
                .OrderBy(g => g.Key.Subject, StringComparer.Ordinal)
                .ThenBy(g => g.Key.Type, StringComparer.Ordinal)
                .Select(g => new ScheduleAlternativeBlock(g.Key.Subject, g.Key.Type, g.ToList()))
                .ToList();
        }

        /// <summary>Everything not part of a block: items with an empty StudentGroups, plus any item whose Subject+Type has no alternative sibling.</summary>
        public static IReadOnlyList<ScheduleItem> ExtractFixedItems(IReadOnlyList<ScheduleItem> items)
        {
            var blockItemIds = new HashSet<int>(ExtractBlocks(items).SelectMany(b => b.Alternatives).Select(i => i.Id));
            return items.Where(i => !blockItemIds.Contains(i.Id)).ToList();
        }

        public ScheduleGenerationResult Generate(IReadOnlyList<ScheduleItem> items, ScheduleGenerationPreferences preferences, int maxVariants = 5)
        {
            var blocks = ExtractBlocks(items);
            var fixedItems = ExtractFixedItems(items);

            // Most-constrained-first: blocks with fewer alternatives branch less and tend to hit
            // a disqualifying conflict sooner, which is exactly when the search below can prune.
            var orderedBlocks = blocks.OrderBy(b => b.Alternatives.Count).ToList();

            var search = new Search(fixedItems, orderedBlocks);
            search.Run();

            if (search.Solutions.Count == 0)
            {
                // Only reachable if the node budget was exhausted before completing a single
                // combination - fall back to the fixed items alone rather than returning nothing.
                var baseline = CountConflicts(fixedItems);
                return new ScheduleGenerationResult([new GeneratedSchedule(fixedItems, baseline, Score(fixedItems, preferences))], blocks.Count);
            }

            var best = search.BestConflictCount;
            var variants = search.Solutions
                .Where(s => s.ConflictCount == best)
                .Select(s => new GeneratedSchedule(s.Items, s.ConflictCount, Score(s.Items, preferences)))
                .OrderBy(v => v.PreferenceScore)
                .ThenBy(v => string.Join(",", v.Items.Select(i => i.Id))) // stable tie-break for deterministic output
                .Take(maxVariants)
                .ToList();

            return new ScheduleGenerationResult(variants, blocks.Count);
        }

        private static double Score(IReadOnlyList<ScheduleItem> items, ScheduleGenerationPreferences preferences)
        {
            double score = 0;

            if (preferences.NoEarlyMornings)
            {
                score += items.Count(i => i.StartHour < 9);
            }

            if (preferences.FreeFriday)
            {
                score += items.Count(i => i.Day == "Friday");
            }

            if (preferences.MinimizeGaps)
            {
                score += items
                    .GroupBy(i => i.Day)
                    .Sum(dayItems =>
                    {
                        var sorted = dayItems.OrderBy(i => i.StartHour).ToList();
                        double gap = 0;
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
            }

            return score;
        }

        private static bool Overlaps(ScheduleItem a, ScheduleItem b) =>
            a.Day == b.Day &&
            a.StartHour < b.StartHour + b.Duration &&
            b.StartHour < a.StartHour + a.Duration;

        private static int CountConflicts(IReadOnlyList<ScheduleItem> items)
        {
            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    if (Overlaps(items[i], items[j]))
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Backtracking search over one alternative per block. Conflict count only ever goes up
        /// as more items are committed, so the moment a partial assignment's conflict count would
        /// exceed the best complete assignment found so far, that whole branch is abandoned
        /// without recursing further - see the pruning check in the alternatives loop below.
        /// </summary>
        private sealed class Search(IReadOnlyList<ScheduleItem> fixedItems, IReadOnlyList<ScheduleAlternativeBlock> blocks)
        {
            private readonly IReadOnlyList<ScheduleAlternativeBlock> _blocks = blocks;
            private readonly List<ScheduleItem> _committed = new(fixedItems);
            private int _nodesVisited;

            public int BestConflictCount { get; private set; } = int.MaxValue;
            public List<(IReadOnlyList<ScheduleItem> Items, int ConflictCount)> Solutions { get; } = [];

            public void Run()
            {
                var baseline = CountConflicts(_committed);
                Backtrack(0, baseline);
            }

            private void Backtrack(int blockIndex, int conflictsSoFar)
            {
                if (_nodesVisited++ > MaxNodesVisited)
                {
                    return;
                }

                if (blockIndex == _blocks.Count)
                {
                    if (conflictsSoFar < BestConflictCount)
                    {
                        BestConflictCount = conflictsSoFar;
                        Solutions.RemoveAll(s => s.ConflictCount > BestConflictCount);
                    }

                    if (conflictsSoFar <= BestConflictCount && Solutions.Count < MaxVariantsCollected)
                    {
                        Solutions.Add((new List<ScheduleItem>(_committed), conflictsSoFar));
                    }
                    return;
                }

                foreach (var alternative in _blocks[blockIndex].Alternatives)
                {
                    int added = 0;
                    foreach (var other in _committed)
                    {
                        if (Overlaps(alternative, other))
                        {
                            added++;
                        }
                    }

                    int newConflicts = conflictsSoFar + added;
                    if (newConflicts > BestConflictCount)
                    {
                        continue; // pruned: this choice already can't beat the best complete assignment found so far
                    }

                    _committed.Add(alternative);
                    Backtrack(blockIndex + 1, newConflicts);
                    _committed.RemoveAt(_committed.Count - 1);
                }
            }
        }
    }
}
