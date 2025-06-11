using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UnizaPlusBackEnd.Models;
using UnizaPlus.Web.Services;

namespace UnizaPlus.Web.Pages
{
    public class IndexModel(ScheduleService scheduleService, ScraperService scraperService) : PageModel
    {
        private readonly ScheduleService _scheduleService = scheduleService;
        private readonly ScraperService _scraperService = scraperService;

        public List<ScheduleItem> ScheduleItems { get; set; } = new();
        public List<string> Days { get; } = new List<string> { "Pondelok", "Utorok", "Streda", "Štvrtok", "Piatok" };
        public bool IsScrapingInProgress { get; set; }
        public string? ErrorMessage { get; set; }
        public bool NoScheduleData { get; set; }

        // pomoc s AI
        public async Task<IActionResult> OnGetAsync(bool refresh = false)
        {
            if (refresh)
            {
                IsScrapingInProgress = true;
                bool scrapeResult = await _scraperService.RunAutoScraperAsync();
                if (!scrapeResult)
                {
                    ErrorMessage = "Failed to refresh schedule data. Please try again later.";
                }
                return RedirectToPage();
            }

            ScheduleItems = _scheduleService.GetScheduleItems();

            if (ScheduleItems.Count == 0)
            {
                string solutionDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\"));
                string filePath = Path.Combine(solutionDir, "schedule.csv");

                if (!System.IO.File.Exists(filePath))  
                {
                    NoScheduleData = true;
                }
            }

            return Page();
        }
    }
}