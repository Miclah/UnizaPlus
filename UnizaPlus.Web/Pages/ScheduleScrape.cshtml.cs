using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UnizaPlus.Web.Services;

namespace UnizaPlus.Web.Pages
{
    public class ScheduleScrapeModel : PageModel
    {
        private readonly ScraperService _scraperService;

        [TempData]
        public bool IsSuccess { get; set; }

        [TempData]
        public bool IsError { get; set; }

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

   
    }
}