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

        #region Legacy Endpoints (Kept for Backward Compatibility)

        /// <summary>
        /// [LEGACY] Updates level state. No longer used - occupancy is tracked automatically.
        /// </summary>
        [HttpPost("levels/state")]
        public ActionResult<LevelStateUpdateResponse> UpdateLevelState([FromBody] LevelStateUpdateRequest request)
        {
            try
            {
                _loadBalancerService.UpdateLevelState(request);
                return Ok(new LevelStateUpdateResponse { Ok = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        /// <summary>
        /// [LEGACY] Controller tick. No longer applicable in simplified system.
        /// </summary>
        [HttpPost("control/tick")]
        public ActionResult<ControlTickResponse> ControlTick()
        {
            try
            {
                var response = _loadBalancerService.PerformControlTick();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        /// <summary>
        /// [LEGACY] Assigns a user to a level based on age and health condition.
        /// Use /arrivals/assign instead.
        /// </summary>
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
        /// [LEGACY] Gets current utilization (occupancy) for all levels.
        /// Use /metrics for more complete information.
        /// </summary>
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
        /// [LEGACY] Resets utilization counters (no-op in current system).
        /// </summary>
        [HttpPost("reset")]
        public ActionResult ResetUtilization()
        {
            try
            {
                _loadBalancerService.ResetUtilization();
                return Ok(new { message = "Reset endpoint (no effect in simplified system)" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        #endregion
    }
}
