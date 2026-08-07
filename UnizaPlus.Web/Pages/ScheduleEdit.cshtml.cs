using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UnizaPlusBackEnd.Models;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleEditModel : PageModel
    {
        // pomoc AI
        [BindProperty]
        public ScheduleItem ScheduleItem { get; set; } = new();

        public List<string> Days { get; } = new List<string> { "Pondelok", "Utorok", "Streda", "�tvrtok", "Piatok" };
        
        public bool IsNewItem { get; private set; }

       
    }
}