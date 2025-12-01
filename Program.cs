using System.Text;
using System.Text.Json.Serialization;
using ai_indoor_nav_api;
using ai_indoor_nav_api.Data;
using ai_indoor_nav_api.Services;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.IO.Converters;

// Load .env file
DotEnvOptions options = new DotEnvOptions(probeLevelsToSearch: 6);
DotEnv.Load(options);

var builder = WebApplication.CreateBuilder(args);

// Configure URLs to bind to 0.0.0.0 on the PORT environment variable (for Render deployment)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
var urls = $"http://0.0.0.0:{port}";
Console.WriteLine($"Configuring application to listen on: {urls}");
builder.WebHost.UseUrls(urls);

builder.Configuration.AddEnvironmentVariables();

// Debug: Check if connection string is loaded
var connString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
if (string.IsNullOrEmpty(connString))
{
    Console.WriteLine("WARNING: DEFAULT_CONNECTION environment variable is not set!");
    Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
    Console.WriteLine($".env file exists: {File.Exists(".env")}");
}
else
{
    Console.WriteLine($"Connection string loaded: {connString.Substring(0, Math.Min(50, connString.Length))}...");
}

// Add services to the container.
builder.Services.AddControllers(options =>
    {
    })
    .AddNewtonsoftJson(opts =>
    {
        opts.SerializerSettings.Converters.Add(new FeatureJsonConverter());
        opts.SerializerSettings.Converters.Add(new GeometryConverter());
        opts.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        opts.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    });


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
             .AllowAnyHeader()
             .AllowAnyMethod();
    });
});

// Configure connection string with proper pooling and timeout settings
var connectionString = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");
var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    // Connection pooling settings - OPTIMIZED FOR 50+ CONCURRENT USERS
    MaxPoolSize = 50,               // Increased to handle 50 concurrent users
    MinPoolSize = 10,               // Keep 10 connections warm for faster response
    ConnectionIdleLifetime = 300,   // 5 minutes idle lifetime
    ConnectionPruningInterval = 30, // Check for idle connections every 30 seconds
    
    // Timeout settings - INCREASED for better reliability under load
    Timeout = 60,                   // 60 second connection timeout
    CommandTimeout = 60,            // 60 second command timeout (handles slow queries better)
    
    // CRITICAL for Supabase Transaction Pooling Mode
    NoResetOnClose = true,          // Must be true for transaction pooling!
    Pooling = true,                 // Enable client-side pooling
    
    // Multiplexing for better performance with transaction pooling
    Multiplexing = true,            // Enable multiplexing for transaction pooling
    
    // Note: KeepAlive is not supported with Multiplexing in Npgsql
    // Supabase's transaction pooler handles connection health at its level
    
    // Disable features not supported by transaction pooling
    // MaxAutoPrepare = 0 would disable prepared statements, but we handle this differently
};

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(
        connectionStringBuilder.ToString(),
        npgsqlOptions => {
            npgsqlOptions.UseNetTopologySuite();
            
            // CRITICAL: Disable prepared statements for transaction pooling mode
            // Transaction pooling doesn't support prepared statements
            npgsqlOptions.MaxBatchSize(1);
            
            // Configure retry logic for transient failures (INCREASED for high concurrency)
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,                           // More retries for high concurrency
                maxRetryDelay: TimeSpan.FromSeconds(15),    // Longer max delay between retries
                errorCodesToAdd: null                       // Use default Npgsql transient error codes
            );
            
            // Set command timeout (INCREASED for high concurrency)
            npgsqlOptions.CommandTimeout(60);
            
            // Migration settings for transaction pooling
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
        }
    )
    // Disable sensitive data logging in production for security
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    // Log detailed errors in development
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
);

// Add memory cache for node caching
builder.Services.AddMemoryCache();

// Register node cache service as singleton (shares cache across requests)
builder.Services.AddSingleton<NodeCacheService>();

// Register connection point detection service
builder.Services.AddScoped<ConnectionPointDetectionService>();

// Register navigation service
builder.Services.AddScoped<NavigationService>();

// Register visitor service as singleton to track visitor IDs
builder.Services.AddSingleton<VisitorService>();

// Register load balancer service as singleton to maintain state across requests
builder.Services.AddSingleton<LoadBalancerService>();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")))
        };
    
        // This is crucial - don't redirect on API endpoints
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MyDbContext>();


var app = builder.Build();

// Apply migrations with timeout and error handling for transaction pooling
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    
    try
    {
        Console.WriteLine("Checking database connection and applying migrations...");
        
        // Use a timeout for migration operations
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        
        // For transaction pooling, check if migrations are needed first
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cts.Token);
        var pendingCount = pendingMigrations.Count();
        
        if (pendingCount > 0)
        {
            Console.WriteLine($"Applying {pendingCount} pending migration(s)...");
            await db.Database.MigrateAsync(cts.Token);
            Console.WriteLine("Migrations applied successfully");
        }
        else
        {
            Console.WriteLine("Database is up to date, no migrations needed");
            
            // Just verify connection works
            var canConnect = await db.Database.CanConnectAsync(cts.Token);
            if (canConnect)
            {
                Console.WriteLine("Database connection verified successfully");
            }
            else
            {
                throw new Exception("Cannot connect to database");
            }
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("ERROR: Migration operation timed out after 60 seconds");
        Console.WriteLine("This might indicate an issue with transaction pooling configuration");
        Console.WriteLine("Try running migrations manually or check your connection string");
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR during migration: {ex.Message}");
        Console.WriteLine($"Connection string (masked): {connectionStringBuilder.Host}:{connectionStringBuilder.Port}/{connectionStringBuilder.Database}");
        Console.WriteLine("Hint: For transaction pooling (port 6543), ensure:");
        Console.WriteLine("  1. NoResetOnClose=true is set");
        Console.WriteLine("  2. Connection timeout is reasonable (30-60s)");
        Console.WriteLine("  3. Database is accessible from this host");
        throw;
    }
}

// Add this line before app.UseHttpsRedirection();
app.UseCors();

// Seed users at startup (with timeout for transaction pooling)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    async Task SeedUsersAsync()
    {
        Console.WriteLine("Starting user seeding process...");
        
        // Use timeout for user seeding operations
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        
        var users = new[]
        {
            new { 
                UserName = Environment.GetEnvironmentVariable("USER1_USERNAME"),
                Email = Environment.GetEnvironmentVariable("USER1_EMAIL"),
                Password = Environment.GetEnvironmentVariable("USER1_PASSWORD")
            },
            new { 
                UserName = Environment.GetEnvironmentVariable("USER2_USERNAME"),
                Email = Environment.GetEnvironmentVariable("USER2_EMAIL"),
                Password = Environment.GetEnvironmentVariable("USER2_PASSWORD")
            },
            new { 
                UserName = Environment.GetEnvironmentVariable("USER3_USERNAME"),
                Email = Environment.GetEnvironmentVariable("USER3_EMAIL"),
                Password = Environment.GetEnvironmentVariable("USER3_PASSWORD")
            }
        };

        foreach (var u in users)
        {
            // Check for cancellation
            if (cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("User seeding cancelled due to timeout");
                break;
            }
            
            // Skip if any required field is missing
            if (string.IsNullOrEmpty(u.UserName) || string.IsNullOrEmpty(u.Email) || string.IsNullOrEmpty(u.Password))
            {
                Console.WriteLine($"WARNING: Skipping user creation - missing credentials for user configuration");
                continue;
            }

            try
            {
                Console.WriteLine($"Checking if user {u.UserName} exists...");
                var userExists = await userManager.FindByNameAsync(u.UserName);
                if (userExists == null)
                {
                    Console.WriteLine($"Creating user: {u.UserName}");
                    var user = new IdentityUser { UserName = u.UserName, Email = u.Email, EmailConfirmed = true };
                    var result = await userManager.CreateAsync(user, u.Password);
                    if (result.Succeeded)
                    {
                        Console.WriteLine($"Successfully created user: {u.UserName}");
                    }
                    else
                    {
                        Console.WriteLine($"WARNING: Failed to create user {u.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    Console.WriteLine($"User {u.UserName} already exists, skipping creation");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR creating user {u.UserName}: {ex.Message}");
                // Continue with next user instead of crashing
            }
        }
        
        Console.WriteLine("User seeding process completed");
    }

    try
    {
        await SeedUsersAsync();
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("WARNING: User seeding timed out - application will start without seeding users");
        // Don't crash - continue startup
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARNING: Error during user seeding: {ex.Message}");
        // Don't crash the application - allow it to start even if user seeding fails
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Pre-warm the cache for better performance on first requests
Console.WriteLine("===========================================");
Console.WriteLine("♨️  Pre-warming cache for optimal performance...");
Console.WriteLine("===========================================");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var cacheService = scope.ServiceProvider.GetRequiredService<NodeCacheService>();
        await cacheService.PrewarmCacheAsync();
        Console.WriteLine("✅ Cache pre-warming completed successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  WARNING: Cache pre-warming failed: {ex.Message}");
        Console.WriteLine("Application will continue but may have slower initial requests.");
    }
}

Console.WriteLine("===========================================");
Console.WriteLine("🚀 Application startup completed successfully!");
Console.WriteLine($"🌐 Listening on: http://0.0.0.0:{port}");
Console.WriteLine($"📊 Database: {connectionStringBuilder.Host}:{connectionStringBuilder.Port}");
Console.WriteLine($"🔌 Connection Pool: Min={connectionStringBuilder.MinPoolSize}, Max={connectionStringBuilder.MaxPoolSize}");
Console.WriteLine($"⏱️  Timeouts: Connection={connectionStringBuilder.Timeout}s, Command={connectionStringBuilder.CommandTimeout}s");
Console.WriteLine($"🔌 Connection Mode: Transaction Pooling (NoResetOnClose=true, Multiplexing=true)");
Console.WriteLine($"💾 Cache: Enabled with 30min duration (1hr for connection points)");
Console.WriteLine("===========================================");

app.Run();