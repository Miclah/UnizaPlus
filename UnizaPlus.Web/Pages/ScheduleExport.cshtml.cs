using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using UnizaPlus.Web.Services;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleExportModel(ScheduleService scheduleService, IStringLocalizer<SharedResource> localizer) : PageModel
    {
        private readonly ScheduleService _scheduleService = scheduleService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        [BindProperty]
        public string Format { get; set; } = "csv";

        [BindProperty]
        public DateOnly SemesterStart { get; set; }

        [BindProperty]
        public DateOnly SemesterEnd { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // A typical 13-week semester starting on the next Monday, as an editable default.
            var today = DateOnly.FromDateTime(DateTime.Today);
            var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            SemesterStart = today.AddDays(daysUntilMonday);
            SemesterEnd = SemesterStart.AddDays(13 * 7 - 1);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var items = await _scheduleService.GetScheduleAsync();

            if (string.Equals(Format, "ics", StringComparison.OrdinalIgnoreCase))
            {
                if (SemesterEnd < SemesterStart)
                {
                    ErrorMessage = _localizer["The semester end date must be on or after the start date."];
                    return Page();
                }

                var ics = ScheduleIcsWriter.Write(items, SemesterStart, SemesterEnd, _localizer);
                var icsBytes = Encoding.UTF8.GetBytes(ics);
                return File(icsBytes, "text/calendar", "schedule.ics");
            }

            var csv = ScheduleCsvWriter.Write(items);
            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "schedule.csv");
        }
    }
}
