using Microsoft.AspNetCore.Mvc;
using ai_indoor_nav_api.Models;
using ai_indoor_nav_api.Services;

namespace ai_indoor_nav_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoadBalancerController : ControllerBase
    {
        private readonly LoadBalancerService _loadBalancerService;

        public LoadBalancerController(LoadBalancerService loadBalancerService)
        {
            _loadBalancerService = loadBalancerService;
        }

        /// <summary>
        /// Assigns a user to a level based on their age and health condition
        /// </summary>
        /// <param name="request">Contains age (int) and isHealthy (bool)</param>
        /// <returns>Assigned level and current utilization information</returns>
        [HttpPost("assign")]
        public ActionResult<LevelAssignmentResponse> AssignLevel([FromBody] LevelAssignmentRequest request)
        {
            if (request.Age < 0)
            {
                return BadRequest(new { error = "Age must be a non-negative integer" });
            }

            try
            {
                var response = _loadBalancerService.AssignLevel(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while assigning level: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets the current utilization levels for all floors
        /// </summary>
        /// <returns>Current utilization for levels 1, 2, and 3</returns>
        [HttpGet("utilization")]
        public ActionResult<LevelUtilizationResponse> GetUtilization()
        {
            try
            {
                var response = _loadBalancerService.GetUtilization();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving utilization: {ex.Message}" });
            }
        }

        /// <summary>
        /// Resets the utilization counters (useful for testing or daily resets)
        /// </summary>
        [HttpPost("reset")]
        public ActionResult ResetUtilization()
        {
            try
            {
                _loadBalancerService.ResetUtilization();
                return Ok(new { message = "Utilization counters have been reset" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while resetting utilization: {ex.Message}" });
            }
        }
    }
}
