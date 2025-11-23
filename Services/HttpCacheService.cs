using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ai_indoor_nav_api.Services
{
    /// <summary>
    /// Service for generating ETags and managing HTTP cache headers
    /// </summary>
    public class HttpCacheService
    {
        /// <summary>
        /// Generates an ETag (Entity Tag) from an object by serializing it and creating a hash
        /// ETags are used to identify specific versions of a resource
        /// </summary>
        /// <param name="content">The object to generate an ETag for</param>
        /// <returns>A strong ETag wrapped in quotes (e.g., "abc123def456")</returns>
        public string GenerateETag(object content)
        {
            if (content == null)
            {
                return GenerateETagFromString(string.Empty);
            }

            // Serialize the content to JSON for consistent hashing
            var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
            {
                // Ensure consistent serialization regardless of property order
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return GenerateETagFromString(json);
        }

        /// <summary>
        /// Generates an ETag from a string content
        /// </summary>
        /// <param name="content">The string content to hash</param>
        /// <returns>A strong ETag wrapped in quotes</returns>
        public string GenerateETagFromString(string content)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
            var etag = Convert.ToBase64String(hash);
            
            // Return as a strong ETag (wrapped in quotes)
            return $"\"{etag}\"";
        }

        /// <summary>
        /// Generates an ETag from a byte array
        /// </summary>
        /// <param name="content">The byte array to hash</param>
        /// <returns>A strong ETag wrapped in quotes</returns>
        public string GenerateETagFromBytes(byte[] content)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(content);
            var etag = Convert.ToBase64String(hash);
            
            return $"\"{etag}\"";
        }

        /// <summary>
        /// Checks if the provided ETag matches any of the ETags in the If-None-Match header
        /// </summary>
        /// <param name="ifNoneMatch">The If-None-Match header value from the request</param>
        /// <param name="currentETag">The current ETag of the resource</param>
        /// <returns>True if the ETags match (resource not modified), false otherwise</returns>
        public bool IsETagMatch(string? ifNoneMatch, string currentETag)
        {
            if (string.IsNullOrEmpty(ifNoneMatch) || string.IsNullOrEmpty(currentETag))
            {
                return false;
            }

            // Handle wildcard (*)
            if (ifNoneMatch == "*")
            {
                return true;
            }

            // Split multiple ETags (comma-separated)
            var etags = ifNoneMatch.Split(',')
                .Select(e => e.Trim())
                .ToList();

            // Check if current ETag matches any of the provided ETags
            return etags.Contains(currentETag);
        }

        /// <summary>
        /// Checks if the resource has been modified since the provided date
        /// </summary>
        /// <param name="ifModifiedSince">The If-Modified-Since header value from the request</param>
        /// <param name="lastModified">The last modified date of the resource</param>
        /// <returns>True if the resource has been modified, false otherwise</returns>
        public bool IsModifiedSince(string? ifModifiedSince, DateTime lastModified)
        {
            if (string.IsNullOrEmpty(ifModifiedSince))
            {
                return true; // No cache validation header, consider it modified
            }

            if (!DateTime.TryParse(ifModifiedSince, out var clientCacheDate))
            {
                return true; // Invalid date format, consider it modified
            }

            // Truncate to seconds for comparison (HTTP dates don't include milliseconds)
            var serverDate = new DateTime(lastModified.Year, lastModified.Month, lastModified.Day,
                lastModified.Hour, lastModified.Minute, lastModified.Second, DateTimeKind.Utc);
            
            var clientDate = new DateTime(clientCacheDate.Year, clientCacheDate.Month, clientCacheDate.Day,
                clientCacheDate.Hour, clientCacheDate.Minute, clientCacheDate.Second, DateTimeKind.Utc);

            // Resource is modified if server date is newer than client cache date
            return serverDate > clientDate;
        }
    }
}
