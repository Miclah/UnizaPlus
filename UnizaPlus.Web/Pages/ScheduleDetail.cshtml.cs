using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UnizaPlusBackEnd.Models;
using UnizaPlus.Web.Services;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleDetailModel : PageModel
    {
        private readonly ScheduleService _scheduleService;

        public ScheduleDetailModel(ScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        public ScheduleItem? ScheduleItem { get; set; }

        // pomoc s AI
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
                "L" => "Laboratory Exercise",
                "P" => "Lecture",
                "C" => "Exercise",
                _ => type
            };
        }
    }
}