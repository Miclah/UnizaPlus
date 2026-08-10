using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>
    /// Writes an RFC 5545 iCalendar (.ics) file: one weekly-recurring VEVENT per schedule item,
    /// anchored at its first occurrence on/after <paramref name="semesterStart"/> and repeating
    /// via RRULE until <paramref name="semesterEnd"/>. Times are written as RFC 5545 "floating"
    /// local time (no TZID/UTC) - the importing calendar app displays them in its own local time
    /// zone, which avoids needing a VTIMEZONE block and is the right call for a schedule whose
    /// students are all in the same time zone as their calendar app.
    /// </summary>
    public static class ScheduleIcsWriter
    {
        private const string DateTimeFormat = "yyyyMMdd'T'HHmmss";
        private const string TimestampFormat = "yyyyMMdd'T'HHmmss'Z'";

        public static string Write(
            IEnumerable<ScheduleItem> items,
            DateOnly semesterStart,
            DateOnly semesterEnd,
            IStringLocalizer<SharedResource>? localizer = null)
        {
            var lines = new List<string>
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//UnizaPlus//Schedule Export//EN",
                "CALSCALE:GREGORIAN",
            };

            var stamp = DateTime.UtcNow.ToString(TimestampFormat, CultureInfo.InvariantCulture);

            foreach (var item in items)
            {
                var dayOfWeek = ToDayOfWeek(item.Day);
                var byDay = ToByDayCode(item.Day);
                if (dayOfWeek == null || byDay == null)
                {
                    continue; // unrecognised day value - nothing sane to anchor a weekday recurrence to
                }

                var firstOccurrence = FirstOccurrenceOnOrAfter(semesterStart, dayOfWeek.Value);
                if (firstOccurrence > semesterEnd)
                {
                    continue; // this weekday's class never actually falls within the given range
                }

                var dtStart = firstOccurrence.ToDateTime(new TimeOnly(item.StartHour, 0));
                var dtEnd = dtStart.AddHours(item.Duration);
                var until = semesterEnd.ToDateTime(new TimeOnly(23, 59, 59));

                lines.Add("BEGIN:VEVENT");
                lines.Add($"UID:schedule-item-{item.Id}@unizaplus");
                lines.Add($"DTSTAMP:{stamp}");
                lines.Add($"DTSTART:{dtStart.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}");
                lines.Add($"DTEND:{dtEnd.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}");
                lines.Add($"RRULE:FREQ=WEEKLY;BYDAY={byDay};UNTIL={until.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}");
                AddFoldedLine(lines, "SUMMARY", $"{item.Subject} ({TypeLabel(item.Type, localizer)})");
                if (!string.IsNullOrWhiteSpace(item.Classroom))
                {
                    AddFoldedLine(lines, "LOCATION", item.Classroom);
                }
                var description = BuildDescription(item);
                if (description.Length > 0)
                {
                    AddFoldedLine(lines, "DESCRIPTION", description);
                }
                lines.Add("END:VEVENT");
            }

            lines.Add("END:VCALENDAR");

            // RFC 5545 requires CRLF line endings.
            return string.Join("\r\n", lines) + "\r\n";
        }

        /// <summary>Raw (unescaped) multi-line description text - AddFoldedLine escapes it exactly once.</summary>
        private static string BuildDescription(ScheduleItem item)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.Professor))
            {
                parts.Add(item.Professor);
            }
            if (!string.IsNullOrWhiteSpace(item.StudentGroups))
            {
                parts.Add(item.StudentGroups);
            }
            return string.Join("\n", parts);
        }

        private static string TypeLabel(string type, IStringLocalizer<SharedResource>? localizer)
        {
            if (localizer == null)
            {
                return type;
            }

            return type switch
            {
                "P" => localizer["Lecture"],
                "C" => localizer["Exercise"],
                "L" => localizer["Lab"],
                _ => type,
            };
        }

        private static DayOfWeek? ToDayOfWeek(string day) => day switch
        {
            "Monday" => DayOfWeek.Monday,
            "Tuesday" => DayOfWeek.Tuesday,
            "Wednesday" => DayOfWeek.Wednesday,
            "Thursday" => DayOfWeek.Thursday,
            "Friday" => DayOfWeek.Friday,
            _ => null,
        };

        private static string? ToByDayCode(string day) => day switch
        {
            "Monday" => "MO",
            "Tuesday" => "TU",
            "Wednesday" => "WE",
            "Thursday" => "TH",
            "Friday" => "FR",
            _ => null,
        };

        private static DateOnly FirstOccurrenceOnOrAfter(DateOnly from, DayOfWeek target)
        {
            int diff = ((int)target - (int)from.DayOfWeek + 7) % 7;
            return from.AddDays(diff);
        }

        /// <summary>Escapes RFC 5545 TEXT special characters: backslash, semicolon, comma, and (already-embedded) newlines.</summary>
        private static string EscapeText(string value) => value
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");

        /// <summary>
        /// Appends "NAME:escaped value", folded to RFC 5545's 75-octet content-line limit -
        /// continuation lines start with a single space, which readers strip back out.
        /// </summary>
        private static void AddFoldedLine(List<string> lines, string name, string rawValue)
        {
            var content = $"{name}:{EscapeText(rawValue)}";
            var bytes = Encoding.UTF8.GetBytes(content);

            const int firstLineMaxOctets = 75;
            const int continuationMaxOctets = 74; // +1 for the mandatory leading space = 75

            if (bytes.Length <= firstLineMaxOctets)
            {
                lines.Add(content);
                return;
            }

            int start = 0;
            bool isFirst = true;
            while (start < bytes.Length)
            {
                int max = isFirst ? firstLineMaxOctets : continuationMaxOctets;
                int len = Math.Min(max, bytes.Length - start);

                // Don't split a multi-byte UTF-8 sequence across two physical lines.
                while (len > 1 && (bytes[start + len - 1] & 0xC0) == 0x80)
                {
                    len--;
                }

                var chunk = Encoding.UTF8.GetString(bytes, start, len);
                lines.Add(isFirst ? chunk : " " + chunk);
                start += len;
                isFirst = false;
            }
        }
    }
}
