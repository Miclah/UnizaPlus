using Microsoft.AspNetCore.Mvc;
using UnizaPlusBackEnd.Models;
using UnizaPlus.Web.Services;

//pomoc s AI
namespace UnizaPlus.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly ScheduleService _scheduleService;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(ScheduleService scheduleService, ILogger<ScheduleController> logger)
        {
            _scheduleService = scheduleService;
            _logger = logger;
        }

        public class MoveScheduleItemRequest
        {
            public int Id { get; set; }
            public string Day { get; set; } = string.Empty; 
            public int StartHour { get; set; }
        }

        // pomoc s AI
        [HttpPost("move")]
        public async Task<IActionResult> MoveScheduleItem([FromBody] MoveScheduleItemRequest request)
        {
            try
            {
                _logger.LogInformation($"Received move request: ID={request.Id}, Day={request.Day}, Hour={request.StartHour}");

                if (request.Id <= 0)
                {
                    _logger.LogWarning($"Invalid item ID received: {request.Id}");
                    return BadRequest("Invalid item ID");
                }

                var item = await _scheduleService.GetScheduleItemAsync(request.Id);
                if (item == null)
                {
                    _logger.LogWarning($"Schedule item {request.Id} not found");
                    return NotFound("Schedule item not found");
                }

                bool slotAvailable = await _scheduleService.IsTimeSlotAvailableAsync(
                    request.Day,
                    request.StartHour,
                    item.Duration,
                    item.Id);

                if (!slotAvailable)
                {
                    _logger.LogWarning($"Time slot conflict detected for item {request.Id}");
                    return BadRequest("This time slot is already occupied or overlaps with another item");
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
                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}