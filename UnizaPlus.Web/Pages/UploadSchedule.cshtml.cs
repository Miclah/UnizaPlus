using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using UnizaPlus.Web.Services;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Web.Pages
{
    [RequestSizeLimit(MaxUploadBytes + 1_000_000)] // hard backstop with headroom for multipart overhead over the friendly check below
    [EnableRateLimiting("upload")]
    public class UploadScheduleModel(ScheduleService scheduleService, IStringLocalizer<SharedResource> localizer) : PageModel
    {
        // A real timetable CSV is a few KB; this is a generous but deliberate ceiling instead
        // of relying on Kestrel's implicit default (~28 MB) to be the only limit in effect.
        private const long MaxUploadBytes = 2_000_000;

        private readonly ScheduleService _scheduleService = scheduleService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                ErrorMessage = _localizer["Please select a CSV file."];
                return RedirectToPage();
            }

            if (file.Length > MaxUploadBytes)
            {
                ErrorMessage = _localizer["The file is too large (max {0} MB).", MaxUploadBytes / 1_000_000];
                return RedirectToPage();
            }

            if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = _localizer["The file must have a .csv extension."];
                return RedirectToPage();
            }

            ScheduleCsvParseResult result;
            await using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                result = await ScheduleCsvParser.ParseAsync(reader, _localizer);
            }

            // Zero items with warnings means the file itself is unreadable (missing columns,
            // empty file, every row invalid). Zero items with no warnings is a validly-formatted
            // CSV that just has no data rows - a deliberate "start with a blank schedule" upload.
            if (result.Items.Count == 0 && result.Warnings.Count > 0)
            {
                ErrorMessage = _localizer["The file could not be processed: {0}", result.Warnings[0]];
                return RedirectToPage();
            }

            await _scheduleService.UpdateAllScheduleItemsAsync(result.Items);

            SuccessMessage = result.Warnings.Count > 0
                ? _localizer["Uploaded {0} items. Skipped rows: {1} (reasons are in the application log).", result.Items.Count, result.Warnings.Count]
                : _localizer["Uploaded {0} items.", result.Items.Count];

            return RedirectToPage();
        }
    }
}
