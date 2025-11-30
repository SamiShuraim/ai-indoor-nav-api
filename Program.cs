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
    // Connection pooling settings
    MaxPoolSize = 100,              // Maximum connections in pool (default 100)
    MinPoolSize = 10,               // Keep minimum connections alive (default 1)
    ConnectionIdleLifetime = 300,   // Close idle connections after 5 minutes
    ConnectionPruningInterval = 10, // Check for idle connections every 10 seconds
    
    // Timeout settings
    Timeout = 30,                   // Connection timeout in seconds (default 15)
    CommandTimeout = 60,            // Command execution timeout in seconds (default 30)
    
    // Performance settings
    NoResetOnClose = false,         // Reset connection state on close for safety
    Pooling = true                  // Ensure pooling is enabled
};

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(
        connectionStringBuilder.ToString(),
        npgsqlOptions => {
            npgsqlOptions.UseNetTopologySuite();
            
            // Configure retry logic for transient failures
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null
            );
            
            // Set command timeout (same as connection string for consistency)
            npgsqlOptions.CommandTimeout(60);
        }
    )
    // Disable sensitive data logging in production for security
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    // Log detailed errors in development
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
);

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    db.Database.Migrate();
}

// Add this line before app.UseHttpsRedirection();
app.UseCors();

// Seed users at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    async Task SeedUsersAsync()
    {
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
            var userExists = await userManager.FindByNameAsync(u.UserName);
            if (userExists == null)
            {
                var user = new IdentityUser { UserName = u.UserName, Email = u.Email, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, u.Password);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create user {u.UserName}: {string.Join(", ", result.Errors)}");
                }
            }
        }
    }

    await SeedUsersAsync();
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

app.Run();