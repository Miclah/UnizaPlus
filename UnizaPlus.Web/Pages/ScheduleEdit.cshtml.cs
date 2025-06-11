using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UnizaPlusBackEnd.Models;
using UnizaPlus.Web.Services;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleEditModel : PageModel
    {
        private readonly ScheduleService _scheduleService;
        private readonly ILogger<ScheduleEditModel> _logger;

        // pomoc AI
        [BindProperty]
        public ScheduleItem ScheduleItem { get; set; } = default;

        public List<string> Days { get; } = new List<string> { "Pondelok", "Utorok", "Streda", "Štvrtok", "Piatok" };
        
        public bool IsNewItem { get; private set; }

       
    }
}