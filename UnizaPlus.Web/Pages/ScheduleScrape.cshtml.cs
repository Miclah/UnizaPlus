using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleScrapeModel : PageModel
    {
        [TempData]
        public bool IsSuccess { get; set; }

        [TempData]
        public bool IsError { get; set; }

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

   
    }
}