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

// Add services to the container.
builder.Services.AddControllers(options =>
    {
    })
    .AddNewtonsoftJson(opts =>
    {
        opts.SerializerSettings.Converters.Add(new FeatureJsonConverter());
        opts.SerializerSettings.Converters.Add(new GeometryConverter());
        opts.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
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

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(
        Environment.GetEnvironmentVariable("DEFAULT_CONNECTION"),
        npgsqlOptions => npgsqlOptions.UseNetTopologySuite()
    )
);

// Register navigation service
builder.Services.AddScoped<NavigationService>();
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