using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using UnizaPlus.Models;
using UnizaPlus.Web.Services;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleDetailModel : PageModel
    {
        private readonly ScheduleService _scheduleService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ScheduleDetailModel(ScheduleService scheduleService, IStringLocalizer<SharedResource> localizer)
        {
            _scheduleService = scheduleService;
            _localizer = localizer;
        }

        public ScheduleItem? ScheduleItem { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ScheduleItem = await _scheduleService.GetScheduleItemAsync(id);

            if (ScheduleItem == null)
            {
                return NotFound();
            }

            return Page();
        }

        public string GetClassTypeDescription(string type)
        {
            return type switch
            {
                "L" => _localizer["Laboratory Exercise"],
                "P" => _localizer["Lecture"],
                "C" => _localizer["Exercise"],
                _ => type
            };
        }
    }
}