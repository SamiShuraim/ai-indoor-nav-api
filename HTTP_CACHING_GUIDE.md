# HTTP Caching Implementation Guide

## Overview

This API implements HTTP caching using **ETags** (Entity Tags) and **Last-Modified** headers to reduce bandwidth usage and improve performance for mobile clients. When data hasn't changed, the server returns a **304 Not Modified** response instead of resending the full data.

## 🚀 How It Works

### For Developers (API Side)

1. **ETag Generation**: When a GET request is made, the system serializes the response data and generates an MD5 hash (ETag) of the content.

2. **Cache Headers**: The server sends these headers with the response:
   - `ETag`: A unique identifier for the resource version (e.g., `"abc123def456"`)
   - `Cache-Control`: Defines caching behavior (e.g., `public, max-age=300`)
   - `Last-Modified`: The timestamp when the resource was last updated

3. **Validation**: On subsequent requests, the client sends:
   - `If-None-Match`: The ETag value from the previous response
   - `If-Modified-Since`: The Last-Modified timestamp

4. **Response Decision**:
   - If ETags match OR resource is not modified → **304 Not Modified** (no body)
   - If ETags don't match → **200 OK** with full data + new ETag

### For Mobile Apps (AI Agent Implementation)

Here's how to implement HTTP caching in a mobile app:

#### 1. **Store Cache Metadata**

When you receive a response, save these headers:

```kotlin
// Example in Kotlin/Android
data class CacheMetadata(
    val etag: String?,
    val lastModified: String?,
    val cacheControl: String?,
    val timestamp: Long,
    val data: String // The actual response body
)

class CacheManager {
    private val cache = mutableMapOf<String, CacheMetadata>()
    
    fun saveResponse(url: String, headers: Headers, body: String) {
        cache[url] = CacheMetadata(
            etag = headers["ETag"],
            lastModified = headers["Last-Modified"],
            cacheControl = headers["Cache-Control"],
            timestamp = System.currentTimeMillis(),
            data = body
        )
    }
}
```

```swift
// Example in Swift/iOS
struct CacheMetadata {
    let etag: String?
    let lastModified: String?
    let cacheControl: String?
    let timestamp: Date
    let data: Data
}

class CacheManager {
    private var cache: [String: CacheMetadata] = [:]
    
    func saveResponse(url: String, headers: [String: String], body: Data) {
        cache[url] = CacheMetadata(
            etag: headers["ETag"],
            lastModified: headers["Last-Modified"],
            cacheControl: headers["Cache-Control"],
            timestamp: Date(),
            data: body
        )
    }
}
```

#### 2. **Add Conditional Headers to Requests**

Before making a request, check if you have cached data and add validation headers:

```kotlin
// Kotlin/Android with OkHttp
fun makeRequest(url: String): Response {
    val cachedData = cacheManager.get(url)
    
    val requestBuilder = Request.Builder().url(url)
    
    // Add conditional headers if we have cached data
    cachedData?.etag?.let { 
        requestBuilder.addHeader("If-None-Match", it) 
    }
    cachedData?.lastModified?.let { 
        requestBuilder.addHeader("If-Modified-Since", it) 
    }
    
    val response = httpClient.newCall(requestBuilder.build()).execute()
    
    return when (response.code) {
        304 -> {
            // Use cached data
            println("Cache HIT - Using cached data")
            cachedData!!.data
        }
        200 -> {
            // Save new data to cache
            println("Cache MISS - Updating cache")
            val body = response.body?.string() ?: ""
            cacheManager.saveResponse(url, response.headers, body)
            body
        }
        else -> {
            throw Exception("Unexpected response code: ${response.code}")
        }
    }
}
```

```swift
// Swift/iOS with URLSession
func makeRequest(url: URL, completion: @escaping (Result<Data, Error>) -> Void) {
    var request = URLRequest(url: url)
    
    // Add conditional headers if we have cached data
    if let cachedData = cacheManager.get(url: url.absoluteString) {
        if let etag = cachedData.etag {
            request.setValue(etag, forHTTPHeaderField: "If-None-Match")
        }
        if let lastModified = cachedData.lastModified {
            request.setValue(lastModified, forHTTPHeaderField: "If-Modified-Since")
        }
    }
    
    URLSession.shared.dataTask(with: request) { data, response, error in
        guard let httpResponse = response as? HTTPURLResponse else {
            completion(.failure(error ?? URLError(.badServerResponse)))
            return
        }
        
        switch httpResponse.statusCode {
        case 304:
            // Use cached data
            print("Cache HIT - Using cached data")
            if let cachedData = self.cacheManager.get(url: url.absoluteString) {
                completion(.success(cachedData.data))
            }
        case 200:
            // Save new data to cache
            print("Cache MISS - Updating cache")
            if let data = data {
                let headers = httpResponse.allHeaderFields as! [String: String]
                self.cacheManager.saveResponse(url: url.absoluteString, headers: headers, body: data)
                completion(.success(data))
            }
        default:
            completion(.failure(URLError(.badServerResponse)))
        }
    }.resume()
}
```

#### 3. **React Native / JavaScript Example**

```javascript
class ApiClient {
  constructor() {
    this.cache = new Map();
  }

  async fetchWithCache(url) {
    const cachedData = this.cache.get(url);
    const headers = {};

    // Add conditional headers
    if (cachedData?.etag) {
      headers['If-None-Match'] = cachedData.etag;
    }
    if (cachedData?.lastModified) {
      headers['If-Modified-Since'] = cachedData.lastModified;
    }

    const response = await fetch(url, { headers });

    if (response.status === 304) {
      // Use cached data
      console.log('Cache HIT - Using cached data');
      return JSON.parse(cachedData.data);
    }

    if (response.status === 200) {
      // Save new data
      console.log('Cache MISS - Updating cache');
      const data = await response.text();
      
      this.cache.set(url, {
        etag: response.headers.get('ETag'),
        lastModified: response.headers.get('Last-Modified'),
        cacheControl: response.headers.get('Cache-Control'),
        timestamp: Date.now(),
        data: data
      });

      return JSON.parse(data);
    }

    throw new Error(`Unexpected status: ${response.status}`);
  }
}

// Usage
const api = new ApiClient();
const buildings = await api.fetchWithCache('https://api.example.com/api/Building');
```

## 📊 Cache Durations by Endpoint

Different endpoints have different cache durations based on how frequently the data changes:

| Endpoint | Duration | Reason |
|----------|----------|--------|
| `/api/Building` | 600s (10 min) | Buildings rarely change |
| `/api/Floor` | 600s (10 min) | Floors rarely change |
| `/api/Poi` | 300s (5 min) | POIs may be updated occasionally |
| `/api/RouteNode` | 300s (5 min) | Route nodes are relatively stable |
| `/api/Beacon` | 120s (2 min) | Beacons update more frequently |
| `/api/Beacon/active` | 60s (1 min) | Active status changes often |
| `/api/Beacon/low-battery` | 60s (1 min) | Battery levels change frequently |

## 🎯 Benefits for Mobile Apps

### 1. **Bandwidth Savings**
- 304 responses are typically < 200 bytes (just headers)
- 200 responses can be several KB or even MB for large datasets
- **Savings**: 90-99% bandwidth reduction when data hasn't changed

### 2. **Performance Improvements**
- Faster response times (304 responses are instant)
- Reduced server load
- Better UX with faster data loading

### 3. **Cost Savings**
- Less mobile data usage for users
- Lower server costs (less bandwidth and CPU usage)
- Reduced API rate limiting issues

## 🔧 Configuration

### Adjust Cache Duration

You can customize cache duration per endpoint:

```csharp
[HttpGet]
[HttpCache(Duration = 300, VaryByQuery = true)]
public async Task<ActionResult<FeatureCollection>> GetPois([FromQuery] int? floor)
{
    // ...
}
```

**Parameters:**
- `Duration`: Cache duration in seconds (0 = no max-age, only validation)
- `VaryByQuery`: Generate different ETags for different query parameters
- `UseLastModified`: Enable/disable Last-Modified header (default: true)
- `CacheControl`: "public" (shareable) or "private" (client-only) (default: "public")

### Disable Caching

Simply remove the `[HttpCache]` attribute from endpoints where caching is not desired.

## 📱 AI Agent Recommendations

For an AI agent implementing this on mobile:

1. **Persistent Storage**: Store cache metadata in SharedPreferences (Android) or UserDefaults (iOS)
2. **Expiration Logic**: Respect the `max-age` directive in `Cache-Control` header
3. **Cache Invalidation**: Clear cache after mutations (POST, PUT, DELETE)
4. **Memory Management**: Implement LRU (Least Recently Used) cache eviction
5. **Offline Support**: Serve cached data when offline, even if expired

### Example Cache Invalidation Strategy

```kotlin
class SmartCacheManager {
    fun invalidateAfterMutation(mutatedUrl: String) {
        // Clear specific resource
        cache.remove(mutatedUrl)
        
        // Clear related list endpoints
        if (mutatedUrl.contains("/api/Poi/")) {
            cache.keys.filter { it.contains("/api/Poi?") }.forEach { cache.remove(it) }
        }
        
        // Clear all if major update
        if (isMajorUpdate(mutatedUrl)) {
            cache.clear()
        }
    }
}
```

## 🧪 Testing Cache Behavior

### Using cURL

```bash
# First request - should return 200 OK with ETag
curl -i https://your-api.com/api/Building

# Copy the ETag value from response headers, then:
# Second request - should return 304 Not Modified
curl -i -H "If-None-Match: \"abc123def456\"" https://your-api.com/api/Building

# Using Last-Modified
curl -i -H "If-Modified-Since: Mon, 23 Nov 2025 12:00:00 GMT" https://your-api.com/api/Building
```

### Monitor Cache Effectiveness

The server logs show cache hits and misses:

```
[CACHE] 304 Not Modified: /api/Building
[CACHE] - ETag Match: true, Not Modified Since: false
[CACHE] 200 OK (Cache Miss): /api/Poi?floor=1
[CACHE] - Generated ETag: "xyz789abc123"
```

## 🔐 Security Considerations

1. **Public vs Private**: Currently set to "public" - fine for non-sensitive data
2. **Sensitive Data**: For user-specific data, consider:
   - Using `CacheControl = "private"` 
   - Adding `Vary: Authorization` header
   - Shorter cache durations
3. **Cache Poisoning**: ETags prevent serving stale data even if cache is compromised

## 📚 HTTP Caching Standards

This implementation follows:
- [RFC 7232](https://tools.ietf.org/html/rfc7232) - Conditional Requests
- [RFC 7234](https://tools.ietf.org/html/rfc7234) - HTTP Caching
- [MDN HTTP Caching](https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching)

## 🎓 Key Concepts Explained

### What is an ETag?
An **Entity Tag (ETag)** is a unique identifier for a specific version of a resource. Think of it as a "fingerprint" of the data. If the data changes, the ETag changes.

### What is 304 Not Modified?
A **304 Not Modified** status code tells the client: "The data you have cached is still valid, no need to download it again." The response body is empty.

### What is Cache-Control?
**Cache-Control** header tells clients (and intermediary caches) how to cache the response:
- `public`: Can be cached by any cache (browser, CDN, proxy)
- `private`: Only the end-user's browser can cache it
- `max-age=300`: Cache is fresh for 300 seconds
- `no-cache`: Always validate with server before using cached data

### What is Last-Modified?
**Last-Modified** header shows when the resource was last changed. Clients send this back as `If-Modified-Since` to check if newer data exists.

## 🚨 Troubleshooting

### Cache Not Working?

1. **Check logs**: Look for `[CACHE]` entries in server logs
2. **Verify headers**: Use browser DevTools Network tab to inspect headers
3. **Service registered?**: Ensure `HttpCacheService` is in `Program.cs`
4. **Attribute applied?**: Check if `[HttpCache]` is on the endpoint
5. **HTTP method?**: Caching only works for GET requests

### Always Getting 200 Instead of 304?

- Ensure `If-None-Match` header is being sent correctly (wrapped in quotes)
- Check if data is actually changing between requests
- Verify ETag format is correct (must be wrapped in double quotes)

### Cache Too Aggressive?

Reduce the `Duration` parameter or set it to 0 to force validation on every request.

## 📈 Monitoring & Analytics

Track these metrics to measure cache effectiveness:

1. **Cache Hit Rate**: (304 responses / total GET requests) × 100%
2. **Bandwidth Saved**: Sum of response sizes for 304s vs potential 200s
3. **Response Time**: Average response time for 304 vs 200

**Target**: Aim for 60-80% cache hit rate for relatively stable data.

---

## 🎯 Summary

This caching implementation provides:
- ✅ Automatic ETag generation for all GET endpoints
- ✅ 304 Not Modified responses to save bandwidth
- ✅ Last-Modified header support
- ✅ Configurable cache durations per endpoint
- ✅ Query parameter variation support
- ✅ Easy integration with mobile apps

**For Mobile Developers**: Store ETags, send them back with `If-None-Match`, handle 304 responses by using cached data. That's it! 🚀
