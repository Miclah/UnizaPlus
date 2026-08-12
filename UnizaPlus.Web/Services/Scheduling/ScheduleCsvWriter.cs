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

            // CSV/formula injection (CWE-1236): a cell starting with =, +, -, @, tab or CR is
            // interpreted as a live formula by Excel/Sheets/LibreOffice when this file is
            // reopened. Prefix it with a quote so it's read back as plain text instead. Values
            // come from an anonymous CSV upload or the edit form, so this can't be trusted.
            if ("=+-@\t\r".IndexOf(value[0]) >= 0)
            {
                value = "'" + value;
            }

            return value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
                ? $"\"{value.Replace("\"", "\"\"")}\""
                : value;
        }
    }
}
