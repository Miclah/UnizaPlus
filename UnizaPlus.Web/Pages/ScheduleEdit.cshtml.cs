using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using UnizaPlus.Models;
using UnizaPlus.Web.Services;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleEditModel(ScheduleService scheduleService, IStringLocalizer<SharedResource> localizer) : PageModel
    {
        private readonly ScheduleService _scheduleService = scheduleService;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;

        [BindProperty]
        public ScheduleItem ScheduleItem { get; set; } = new();

        public IReadOnlyList<string> Days { get; } = ScheduleDays.All;

        public bool IsNewItem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                IsNewItem = true;
                ScheduleItem = new ScheduleItem
                {
                    Day = ScheduleDays.All[0],
                    StartHour = 7,
                    Duration = 1,
                    Type = "P"
                };
                return Page();
            }

            var existing = await _scheduleService.GetScheduleItemAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            IsNewItem = false;
            ScheduleItem = existing;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsNewItem = ScheduleItem.Id <= 0;

            if (string.IsNullOrWhiteSpace(ScheduleItem.Subject))
            {
                ModelState.AddModelError(nameof(ScheduleItem.Subject), _localizer["Subject is required."]);
            }
            if (string.IsNullOrWhiteSpace(ScheduleItem.Professor))
            {
                ModelState.AddModelError(nameof(ScheduleItem.Professor), _localizer["Professor is required."]);
            }
            if (string.IsNullOrWhiteSpace(ScheduleItem.Classroom))
            {
                ModelState.AddModelError(nameof(ScheduleItem.Classroom), _localizer["Classroom is required."]);
            }
            if (!ScheduleDays.All.Contains(ScheduleItem.Day))
            {
                ModelState.AddModelError(nameof(ScheduleItem.Day), _localizer["Invalid day."]);
            }
            if (ScheduleItem.Duration < 1 || ScheduleItem.Duration > 4)
            {
                ModelState.AddModelError(nameof(ScheduleItem.Duration), _localizer["Duration must be between 1 and 4 hours."]);
            }
            if (ScheduleItem.StartHour < 7 || ScheduleItem.StartHour > 20)
            {
                ModelState.AddModelError(nameof(ScheduleItem.StartHour), _localizer["Start time must be between 7:00 and 20:00."]);
            }
            if (ScheduleItem.Type is not ("P" or "C" or "L"))
            {
                ModelState.AddModelError(nameof(ScheduleItem.Type), _localizer["Invalid type."]);
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            bool slotAvailable = await _scheduleService.IsTimeSlotAvailableAsync(
                ScheduleItem.Day, ScheduleItem.StartHour, ScheduleItem.Duration, ScheduleItem.Id);

            if (!slotAvailable)
            {
                ModelState.AddModelError(string.Empty, _localizer["This time slot is already occupied or overlaps with another item."]);
                return Page();
            }

            ScheduleItem.InitializeColor();
            await _scheduleService.AddOrUpdateScheduleItemAsync(ScheduleItem);

            return RedirectToPage("Index");
        }
    }
}
