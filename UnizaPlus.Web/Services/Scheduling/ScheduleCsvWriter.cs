using System.Text;
using UnizaPlus.Models;

namespace UnizaPlus.Web.Services.Scheduling
{
    /// <summary>
    /// Writes the same CSV format ScheduleCsvParser reads
    /// (Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group), so an
    /// exported schedule can be re-uploaded later without any conversion.
    /// </summary>
    public static class ScheduleCsvWriter
    {
        private const string Header = "Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group";

        public static string Write(IEnumerable<ScheduleItem> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Header);

            foreach (var item in items)
            {
                sb.AppendLine(string.Join(",",
                    Escape(item.Subject),
                    Escape(item.SubjectCode),
                    Escape(item.Type),
                    Escape(item.Day),
                    item.StartHour.ToString(),
                    (item.StartHour + item.Duration).ToString(),
                    Escape(item.Classroom),
                    Escape(item.Professor),
                    Escape(item.StudentGroups)));
            }

            return sb.ToString();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}
