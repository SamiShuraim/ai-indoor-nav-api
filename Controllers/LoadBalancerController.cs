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
        /// Assigns a pilgrim to a level based on age and disability status.
        /// Uses simple occupancy-based distribution to minimize crowding.
        /// </summary>
        /// <param name="request">Contains age (int) and isDisabled (bool)</param>
        /// <returns>Assigned level with occupancy information</returns>
        [HttpPost("arrivals/assign")]
        public ActionResult<ArrivalAssignResponse> AssignArrival([FromBody] ArrivalAssignRequest request)
        {
            try
            {
                var response = _loadBalancerService.AssignArrival(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while assigning level: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets current metrics including occupancy at each level.
        /// </summary>
        /// <returns>Occupancy snapshot for all levels</returns>
        [HttpGet("metrics")]
        public ActionResult<MetricsResponse> GetMetrics()
        {
            try
            {
                var response = _loadBalancerService.GetMetrics();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving metrics: {ex.Message}" });
            }
        }

        /// <summary>
        /// Updates runtime configuration (age threshold and target share for Level 1).
        /// </summary>
        /// <param name="request">Configuration updates</param>
        /// <returns>Updated configuration</returns>
        [HttpPost("config")]
        public ActionResult<ConfigResponse> UpdateConfig([FromBody] ConfigUpdateRequest request)
        {
            try
            {
                var response = _loadBalancerService.UpdateConfig(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while updating config: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets the current runtime configuration.
        /// </summary>
        /// <returns>Current configuration</returns>
        [HttpGet("config")]
        public ActionResult<ConfigResponse> GetConfig()
        {
            try
            {
                var response = _loadBalancerService.GetCurrentConfig();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving config: {ex.Message}" });
            }
        }

        /// <summary>
        /// Health check endpoint.
        /// </summary>
        /// <returns>Status OK</returns>
        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

    }
}
