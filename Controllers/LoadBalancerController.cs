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

        #region New Adaptive Load Balancer API

        /// <summary>
        /// Assigns a pilgrim to a level based on age and disability status.
        /// Uses adaptive algorithm with rolling statistics and dynamic age cutoffs.
        /// </summary>
        /// <param name="request">Contains age (int) and isDisabled (bool)</param>
        /// <returns>Assigned level with detailed decision information</returns>
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
        /// Updates the state of one or more levels (wait times, queue lengths, throughput).
        /// This data feeds into the adaptive controller.
        /// </summary>
        /// <param name="request">List of level states with wait estimates, queue lengths, and throughput</param>
        /// <returns>Confirmation of update</returns>
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
                return StatusCode(500, new { error = $"An error occurred while updating level state: {ex.Message}" });
            }
        }

        /// <summary>
        /// Manually triggers a controller tick to recompute alpha1, age_cutoff, and p_disabled.
        /// The controller normally runs automatically every minute.
        /// </summary>
        /// <returns>Updated controller state</returns>
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
                return StatusCode(500, new { error = $"An error occurred during controller tick: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets current metrics including controller state, counts, quantiles, and level wait times.
        /// </summary>
        /// <returns>Comprehensive metrics snapshot</returns>
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
        /// Updates runtime configuration. All parameters are optional.
        /// Changes take effect immediately.
        /// </summary>
        /// <param name="request">Configuration updates (any subset of parameters)</param>
        /// <returns>Full resolved configuration after update</returns>
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
        /// Health check endpoint for monitoring service liveness.
        /// </summary>
        /// <returns>Status OK</returns>
        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }

        #endregion

        #region Legacy API (Backward Compatibility)

        /// <summary>
        /// [LEGACY] Assigns a user to a level based on their age and health condition.
        /// This endpoint is maintained for backward compatibility.
        /// Use /arrivals/assign for the new adaptive algorithm with full features.
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
        /// [LEGACY] Gets the current utilization levels for all floors.
        /// Note: The new adaptive system doesn't track utilization in the same way.
        /// Use /metrics for comprehensive statistics.
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
        /// [LEGACY] Resets the utilization counters (not applicable in new system).
        /// </summary>
        [HttpPost("reset")]
        public ActionResult ResetUtilization()
        {
            try
            {
                _loadBalancerService.ResetUtilization();
                return Ok(new { message = "Utilization counters have been reset (legacy endpoint - no effect in new system)" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while resetting utilization: {ex.Message}" });
            }
        }

        #endregion
    }
}
