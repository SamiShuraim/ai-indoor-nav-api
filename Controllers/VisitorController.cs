using Microsoft.AspNetCore.Mvc;
using ai_indoor_nav_api.Services;

namespace ai_indoor_nav_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitorController : ControllerBase
    {
        private readonly VisitorService _visitorService;

        public VisitorController(VisitorService visitorService)
        {
            _visitorService = visitorService;
        }

        /// <summary>
        /// Retrieves visitor information by their unique ID.
        /// This endpoint can be used for QR code scanning to display visitor details.
        /// </summary>
        /// <param name="id">The unique visitor ID (format: XXXX-XXXX)</param>
        /// <returns>Visitor information including age, status, and assigned level</returns>
        [HttpGet("{id}")]
        public ActionResult GetVisitorInfo(string id)
        {
            try
            {
                var visitorInfo = _visitorService.GetVisitorInfo(id);
                
                if (visitorInfo == null)
                {
                    return NotFound(new { error = $"Visitor ID '{id}' not found" });
                }

                return Ok(visitorInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving visitor info: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets a simple HTML page for displaying visitor information.
        /// This is designed to be accessed via QR code scanning.
        /// </summary>
        /// <param name="id">The unique visitor ID</param>
        /// <returns>HTML page with visitor information</returns>
        [HttpGet("{id}/page")]
        [Produces("text/html")]
        public ActionResult GetVisitorPage(string id)
        {
            try
            {
                var visitorInfo = _visitorService.GetVisitorInfo(id);
                
                if (visitorInfo == null)
                {
                    return Content(GenerateNotFoundHtml(id), "text/html");
                }

                return Content(GenerateVisitorHtml(visitorInfo), "text/html");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets the count of active visitors
        /// </summary>
        [HttpGet("count")]
        public ActionResult GetActiveVisitorCount()
        {
            try
            {
                int count = _visitorService.GetActiveVisitorCount();
                return Ok(new { activeVisitors = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
            }
        }

        private string GenerateVisitorHtml(Models.VisitorInfoResponse visitor)
        {
            var statusColor = visitor.Status == "Disabled" ? "#e74c3c" : "#3498db";
            var levelColor = visitor.AssignedLevel switch
            {
                1 => "#27ae60",
                2 => "#f39c12",
                3 => "#9b59b6",
                _ => "#95a5a6"
            };
            
            var statusBadge = visitor.IsExpired 
                ? "<div class='badge expired'>EXPIRED</div>" 
                : "<div class='badge active'>ACTIVE</div>";

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Visitor {visitor.VisitorId}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 500px;
            width: 100%;
            padding: 40px 30px;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .visitor-id {{
            font-size: 32px;
            font-weight: bold;
            color: #2c3e50;
            letter-spacing: 2px;
            margin-bottom: 15px;
        }}
        .badge {{
            display: inline-block;
            padding: 8px 20px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: bold;
            text-transform: uppercase;
        }}
        .badge.active {{
            background: #2ecc71;
            color: white;
        }}
        .badge.expired {{
            background: #e74c3c;
            color: white;
        }}
        .info-grid {{
            display: grid;
            gap: 20px;
            margin-top: 30px;
        }}
        .info-item {{
            background: #f8f9fa;
            padding: 20px;
            border-radius: 12px;
            border-left: 4px solid #667eea;
        }}
        .info-label {{
            font-size: 12px;
            text-transform: uppercase;
            color: #7f8c8d;
            font-weight: 600;
            margin-bottom: 8px;
            letter-spacing: 1px;
        }}
        .info-value {{
            font-size: 24px;
            color: #2c3e50;
            font-weight: bold;
        }}
        .level-display {{
            text-align: center;
            margin-top: 30px;
            padding: 30px;
            background: {levelColor};
            border-radius: 15px;
            color: white;
        }}
        .level-label {{
            font-size: 14px;
            text-transform: uppercase;
            margin-bottom: 10px;
            opacity: 0.9;
        }}
        .level-number {{
            font-size: 64px;
            font-weight: bold;
        }}
        .timestamp {{
            text-align: center;
            margin-top: 20px;
            font-size: 12px;
            color: #95a5a6;
        }}
        .status-indicator {{
            border-left-color: {statusColor};
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='visitor-id'>{visitor.VisitorId}</div>
            {statusBadge}
        </div>
        
        <div class='info-grid'>
            <div class='info-item'>
                <div class='info-label'>Age</div>
                <div class='info-value'>{visitor.Age} years</div>
            </div>
            
            <div class='info-item status-indicator'>
                <div class='info-label'>Status</div>
                <div class='info-value'>{visitor.Status}</div>
            </div>
        </div>
        
        <div class='level-display'>
            <div class='level-label'>Assigned Level</div>
            <div class='level-number'>L{visitor.AssignedLevel}</div>
        </div>
        
        <div class='timestamp'>
            <div><strong>Assigned:</strong> {visitor.AssignedAt:yyyy-MM-dd HH:mm:ss} UTC</div>
            <div><strong>Expires:</strong> {visitor.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC</div>
        </div>
    </div>
</body>
</html>";
        }

        private string GenerateNotFoundHtml(string id)
        {
            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Visitor Not Found</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }}
        .container {{
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            max-width: 500px;
            width: 100%;
            padding: 60px 30px;
            text-align: center;
        }}
        .error-icon {{
            font-size: 72px;
            margin-bottom: 20px;
        }}
        h1 {{
            color: #2c3e50;
            margin-bottom: 15px;
        }}
        .visitor-id {{
            font-family: monospace;
            background: #f8f9fa;
            padding: 10px 20px;
            border-radius: 8px;
            display: inline-block;
            margin: 20px 0;
            font-size: 20px;
            color: #e74c3c;
        }}
        p {{
            color: #7f8c8d;
            line-height: 1.6;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='error-icon'>⚠️</div>
        <h1>Visitor Not Found</h1>
        <div class='visitor-id'>{id}</div>
        <p>The visitor ID you scanned could not be found in the system.<br>
        It may have expired or is invalid.</p>
    </div>
</body>
</html>";
        }
    }
}
