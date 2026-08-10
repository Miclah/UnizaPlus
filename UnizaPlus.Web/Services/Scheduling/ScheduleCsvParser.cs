using Microsoft.Extensions.Localization;
using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    public class ScheduleCsvParseResult
    {
        public List<ScheduleItem> Items { get; } = [];
        public List<string> Warnings { get; } = [];
    }

    /// <summary>
    /// Parses the UnizaPlus demo/upload CSV format:
    /// Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group
    /// Only Subject, Type, Day, Start and End are required; the rest may be empty.
    /// </summary>
    public static class ScheduleCsvParser
    {
        private static readonly string[] RequiredColumns = ["Subject", "Type", "Day", "Start", "End"];
        private static readonly HashSet<string> ValidTypes = new(["P", "C", "L"], StringComparer.OrdinalIgnoreCase);

        // Matches the schedule grid's rendered hour columns (Index.cshtml: hour 7..20) and the
        // duration values the CSS/edit form actually support (schedule.css defines widths for 1-4h).
        private const int MinHour = 7;
        private const int MaxHour = 20;
        private const int MaxDuration = 4;

        // localizer is optional so existing callers (tests, and any code without a DI scope)
        // keep working unlocalized - the key strings below are themselves the English text.
        public static async Task<ScheduleCsvParseResult> ParseAsync(TextReader reader, IStringLocalizer<SharedResource>? localizer = null)
        {
            var result = new ScheduleCsvParseResult();

            var headerLine = await reader.ReadLineAsync();
            if (headerLine == null)
            {
                result.Warnings.Add(Format(localizer, "The file is empty."));
                return result;
            }

            var headers = CsvLineSplitter.Split(headerLine);
            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                var name = headers[i].Trim();
                if (name.Length > 0)
                {
                    columnIndex[name] = i;
                }
            }

            var missingColumns = RequiredColumns.Where(c => !columnIndex.ContainsKey(c)).ToList();
            if (missingColumns.Count > 0)
            {
                result.Warnings.Add(Format(
                    localizer,
                    "Missing required columns: {0}. Expected header: {1},Room,Teacher,Group,SubjectCode",
                    string.Join(", ", missingColumns),
                    string.Join(",", RequiredColumns)));
                return result;
            }

            int lineNumber = 1;
            int nextId = 1;
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var fields = CsvLineSplitter.Split(line);

                string? GetField(string column)
                {
                    if (!columnIndex.TryGetValue(column, out var idx) || idx >= fields.Count)
                    {
                        return null;
                    }
                    var value = fields[idx].Trim();
                    return value.Length == 0 ? null : value;
                }

                var subject = GetField("Subject");
                if (subject == null)
                {
                    result.Warnings.Add(Format(localizer, "Row {0}: missing subject (Subject), row skipped.", lineNumber));
                    continue;
                }

                // Accepts both the current English day names and the Slovak names the interface
                // used to show, so CSV files written under the old interface still load.
                var dayRaw = GetField("Day");
                if (dayRaw == null || !ScheduleDays.TryNormalize(dayRaw, out var day))
                {
                    result.Warnings.Add(Format(localizer, "Row {0}: invalid or missing day '{1}', row skipped.", lineNumber, dayRaw ?? string.Empty));
                    continue;
                }

                var typeRaw = GetField("Type");
                if (typeRaw == null || !ValidTypes.Contains(typeRaw))
                {
                    result.Warnings.Add(Format(localizer, "Row {0}: invalid or missing type '{1}' (expected P, C or L), row skipped.", lineNumber, typeRaw ?? string.Empty));
                    continue;
                }

                var startRaw = GetField("Start");
                var endRaw = GetField("End");
                if (!int.TryParse(startRaw, out var start) || !int.TryParse(endRaw, out var end) || end <= start)
                {
                    result.Warnings.Add(Format(localizer, "Row {0}: invalid time range Start='{1}' End='{2}', row skipped.", lineNumber, startRaw ?? string.Empty, endRaw ?? string.Empty));
                    continue;
                }

                var duration = end - start;
                if (start < MinHour || start > MaxHour || end > MaxHour + 1 || duration > MaxDuration)
                {
                    result.Warnings.Add(Format(
                        localizer,
                        "Row {0}: time range Start={1} End={2} is outside the supported range ({3}-{4}, max duration {5}h), row skipped.",
                        lineNumber, start, end, MinHour, MaxHour, MaxDuration));
                    continue;
                }

                var item = new ScheduleItem
                {
                    Id = nextId++,
                    Subject = subject,
                    SubjectCode = GetField("SubjectCode") ?? string.Empty,
                    Type = typeRaw.ToUpperInvariant(),
                    Day = day,
                    StartHour = start,
                    Duration = duration,
                    Classroom = GetField("Room") ?? string.Empty,
                    Professor = GetField("Teacher") ?? string.Empty,
                    StudentGroups = GetField("Group") ?? string.Empty,
                };
                item.InitializeColor();
                result.Items.Add(item);
            }

            return result;
        }

        private static string Format(IStringLocalizer<SharedResource>? localizer, string key, params object[] args)
        {
            if (localizer != null)
            {
                return localizer[key, args];
            }

            return args.Length == 0 ? key : string.Format(key, args);
        }
    }
}
