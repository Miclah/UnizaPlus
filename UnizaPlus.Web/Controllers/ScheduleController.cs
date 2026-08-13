using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using UnizaPlus.Models;
using UnizaPlus.Web.Services;
using UnizaPlus.Web.Services.Scheduling;

namespace UnizaPlus.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("move")]
    public class ScheduleController : ControllerBase
    {
        private readonly ScheduleService _scheduleService;
        private readonly ILogger<ScheduleController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IAntiforgery _antiforgery;

        public ScheduleController(ScheduleService scheduleService, ILogger<ScheduleController> logger, IStringLocalizer<SharedResource> localizer, IAntiforgery antiforgery)
        {
            _scheduleService = scheduleService;
            _logger = logger;
            _localizer = localizer;
            _antiforgery = antiforgery;
        }

        public class MoveScheduleItemRequest
        {
            public int Id { get; set; }
            public string Day { get; set; } = string.Empty;
            public int StartHour { get; set; }
        }

        [HttpPost("move")]
        public async Task<IActionResult> MoveScheduleItem([FromBody] MoveScheduleItemRequest request)
        {
            // AddControllers() (unlike AddRazorPages()) doesn't wire up automatic anti-forgery
            // validation, and this is a JSON body with no HTML form around it - so it's
            // validated explicitly here instead, against the header Index.cshtml/schedule.js
            // send (see AddAntiforgery's HeaderName in Program.cs).
            try
            {
                await _antiforgery.ValidateRequestAsync(HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return BadRequest(_localizer["Invalid or missing anti-forgery token."].Value);
            }

            try
            {
                _logger.LogInformation($"Received move request: ID={request.Id}, Day={request.Day}, Hour={request.StartHour}");

                if (request.Id <= 0)
                {
                    _logger.LogWarning($"Invalid item ID received: {request.Id}");
                    return BadRequest(_localizer["Invalid item ID"].Value);
                }

                if (!ScheduleDays.All.Contains(request.Day))
                {
                    _logger.LogWarning($"Invalid day received: {request.Day}");
                    return BadRequest(_localizer["Invalid day"].Value);
                }

                var item = await _scheduleService.GetScheduleItemAsync(request.Id);
                if (item == null)
                {
                    _logger.LogWarning($"Schedule item {request.Id} not found");
                    return NotFound(_localizer["Schedule item not found"].Value);
                }

                // Unlike the add/edit form, dragging is allowed to create a conflict - the grid
                // just highlights it - so this only rejects a drop outside the rendered hour range.
                if (!ScheduleOverlapChecker.IsWithinBoundaries(request.StartHour, item.Duration))
                {
                    _logger.LogWarning($"Out-of-range start hour received for item {request.Id}: {request.StartHour}");
                    return BadRequest(_localizer["Start time must be between 7:00 and 20:00."].Value);
                }

                var originalDay = item.Day;
                var originalHour = item.StartHour;

                item.Day = request.Day;
                item.StartHour = request.StartHour;

                await _scheduleService.UpdateScheduleItemAsync(item);

                _logger.LogInformation($"Moved item {request.Id} from {originalDay} {originalHour}:00 to {request.Day} {request.StartHour}:00");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing move request for item {request.Id}");
                return StatusCode(500, _localizer["An error occurred while processing your request"].Value);
            }
        }
    }
}