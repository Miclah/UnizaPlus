namespace UnizaPlus.Models
{
    public static class ScheduleDays
    {
        public static readonly IReadOnlyList<string> All = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

        // The interface used to show Slovak day names. CSV files exported (or hand-written)
        // under that interface still use them, so both spellings are accepted and normalised
        // to the current English canonical value - old files keep working after the UI moved
        // to English. See UnizaPlus.Web/Services/Scheduling/ScheduleCsvParser.cs.
        private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Monday"] = "Monday",
            ["Tuesday"] = "Tuesday",
            ["Wednesday"] = "Wednesday",
            ["Thursday"] = "Thursday",
            ["Friday"] = "Friday",
            ["Pondelok"] = "Monday",
            ["Utorok"] = "Tuesday",
            ["Streda"] = "Wednesday",
            ["Štvrtok"] = "Thursday",
            ["Piatok"] = "Friday",
        };

        /// <summary>Resolves a day name in either English or Slovak to its canonical English value.</summary>
        public static bool TryNormalize(string? value, out string day)
        {
            if (value != null && Aliases.TryGetValue(value, out var normalized))
            {
                day = normalized;
                return true;
            }

            day = string.Empty;
            return false;
        }
    }
}
