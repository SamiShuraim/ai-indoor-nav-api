using System.Text;
using ai_indoor_nav_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace ai_indoor_nav_api.Filters
{
    /// <summary>
    /// Action filter that implements HTTP caching with ETags and 304 Not Modified responses
    /// Apply this attribute to GET endpoints to enable caching
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HttpCacheAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Cache duration in seconds. Default is 300 seconds (5 minutes)
        /// Set to 0 to use only ETag validation without max-age
        /// </summary>
        public int Duration { get; set; } = 300;

        /// <summary>
        /// Whether to use Last-Modified header in addition to ETag
        /// </summary>
        public bool UseLastModified { get; set; } = true;

        /// <summary>
        /// Cache-Control directive: "public" (cacheable by any cache) or "private" (cacheable only by client)
        /// </summary>
        public string CacheControl { get; set; } = "public";

        /// <summary>
        /// Whether this resource varies by query parameters
        /// If true, different query params will generate different ETags
        /// </summary>
        public bool VaryByQuery { get; set; } = true;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            var response = httpContext.Response;

            // Only apply caching to GET requests
            if (request.Method != "GET")
            {
                await next();
                return;
            }

            // Get the cache service from DI
            var cacheService = httpContext.RequestServices.GetService<HttpCacheService>();
            if (cacheService == null)
            {
                // If service not registered, skip caching
                await next();
                return;
            }

            // Create a cache key based on the request path and query
            var cacheKey = GenerateCacheKey(request);

            // Check if client provided If-None-Match (ETag validation)
            var ifNoneMatch = request.Headers["If-None-Match"].FirstOrDefault();
            var ifModifiedSince = request.Headers["If-Modified-Since"].FirstOrDefault();

            // Execute the action
            var executedContext = await next();

            // Only process successful results
            if (executedContext.Result is not ObjectResult objectResult || 
                objectResult.StatusCode is null or < 200 or >= 300)
            {
                return;
            }

            var resultValue = objectResult.Value;
            if (resultValue == null)
            {
                return;
            }

            // Generate ETag for the response content
            var etag = cacheService.GenerateETag(resultValue);

            // Set Cache-Control header
            var cacheControlValue = Duration > 0 
                ? $"{CacheControl}, max-age={Duration}" 
                : $"{CacheControl}, no-cache"; // no-cache means "validate before use"

            response.Headers["Cache-Control"] = cacheControlValue;
            response.Headers["ETag"] = etag;

            // Handle Last-Modified if enabled
            DateTime? lastModified = null;
            if (UseLastModified)
            {
                lastModified = ExtractLastModifiedDate(resultValue);
                if (lastModified.HasValue)
                {
                    response.Headers["Last-Modified"] = lastModified.Value.ToString("R"); // RFC 1123 format
                }
            }

            // Check if client's cached version is still valid
            bool isETagMatch = !string.IsNullOrEmpty(ifNoneMatch) && 
                               cacheService.IsETagMatch(ifNoneMatch, etag);
            
            bool isNotModifiedSince = UseLastModified && 
                                      lastModified.HasValue &&
                                      !string.IsNullOrEmpty(ifModifiedSince) &&
                                      !cacheService.IsModifiedSince(ifModifiedSince, lastModified.Value);

            // Return 304 Not Modified if cache is valid
            if (isETagMatch || isNotModifiedSince)
            {
                Console.WriteLine($"[CACHE] 304 Not Modified: {request.Path}{request.QueryString}");
                Console.WriteLine($"[CACHE] - ETag Match: {isETagMatch}, Not Modified Since: {isNotModifiedSince}");
                
                // Replace the result with a 304 response
                executedContext.Result = new StatusCodeResult(304);
            }
            else
            {
                Console.WriteLine($"[CACHE] 200 OK (Cache Miss): {request.Path}{request.QueryString}");
                Console.WriteLine($"[CACHE] - Generated ETag: {etag}");
            }
        }

        /// <summary>
        /// Generates a cache key based on the request path and query parameters
        /// </summary>
        private string GenerateCacheKey(HttpRequest request)
        {
            var key = new StringBuilder(request.Path);
            
            if (VaryByQuery && request.Query.Any())
            {
                key.Append('?');
                foreach (var param in request.Query.OrderBy(q => q.Key))
                {
                    key.Append($"{param.Key}={param.Value}&");
                }
            }
            
            return key.ToString().TrimEnd('&');
        }

        /// <summary>
        /// Attempts to extract the most recent UpdatedAt or CreatedAt date from the response object
        /// This is used for the Last-Modified header
        /// </summary>
        private DateTime? ExtractLastModifiedDate(object value)
        {
            try
            {
                // Handle collections (arrays, lists)
                if (value is System.Collections.IEnumerable enumerable && value is not string)
                {
                    DateTime? maxDate = null;
                    
                    foreach (var item in enumerable)
                    {
                        var itemDate = ExtractDateFromObject(item);
                        if (itemDate.HasValue && (!maxDate.HasValue || itemDate.Value > maxDate.Value))
                        {
                            maxDate = itemDate;
                        }
                    }
                    
                    return maxDate;
                }
                
                // Handle single objects
                return ExtractDateFromObject(value);
            }
            catch
            {
                // If extraction fails, return null
                return null;
            }
        }

        /// <summary>
        /// Extracts UpdatedAt or CreatedAt property from an object using reflection
        /// </summary>
        private DateTime? ExtractDateFromObject(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();
            
            // Try to find UpdatedAt property first
            var updatedAtProp = type.GetProperty("UpdatedAt");
            if (updatedAtProp?.GetValue(obj) is DateTime updatedAt)
            {
                return updatedAt;
            }
            
            // Fall back to CreatedAt
            var createdAtProp = type.GetProperty("CreatedAt");
            if (createdAtProp?.GetValue(obj) is DateTime createdAt)
            {
                return createdAt;
            }
            
            return null;
        }
    }
}
