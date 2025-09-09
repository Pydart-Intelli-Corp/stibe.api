using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using stibe.api.Configuration;
using stibe.api.Data;
using stibe.api.Services.Interfaces;
using stibe.api.Services.Implementations.General;
using System.Text;
using stibe.api.Services.Interfaces.Partner;
using stibe.api.Services.Implementations.MockServices;
using stibe.api.Services.Implementations.LocationServices;
using stibe.api.Services.Implementations.SecurityServices;
using stibe.api.Services.Implementations.PartnerServices.StaffServices;
using stibe.api.Services.Implementations.FileService;
using Microsoft.Extensions.FileProviders;
using Serilog;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/stibe-api-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

try
{
    Log.Information("Starting Stibe API...");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework with MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)));
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Configure Google OAuth Settings
builder.Services.Configure<GoogleOAuthSettings>(builder.Configuration.GetSection("GoogleOAuth"));

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
        ValidIssuer = jwtSettings?.Issuer,
        ValidAudience = jwtSettings?.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? "")),
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(googleOptions =>
{
    var googleSettings = builder.Configuration.GetSection("GoogleOAuth").Get<GoogleOAuthSettings>();
    if (googleSettings != null && googleSettings.Enabled)
    {
        googleOptions.ClientId = googleSettings.ClientId;
        googleOptions.ClientSecret = googleSettings.ClientSecret;
    }
});

// Configure Authorization
builder.Services.AddAuthorization();

// Configure Feature Flags
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("FeatureFlags"));

// Register custom services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// Configure Feature Flags first
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("FeatureFlags"));

// Register location service based on feature flag
if (builder.Configuration.GetValue<bool>("FeatureFlags:UseRealLocationService"))
{
    builder.Services.AddHttpClient<ILocationService, GoogleLocationService>();
}
else
{
    builder.Services.AddScoped<ILocationService, MockLocationService>();
}
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IStaffWorkService, StaffWorkService>();
builder.Services.Configure<EmailConfiguration>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<FeatureFlags>(builder.Configuration.GetSection("FeatureFlags"));
builder.Services.AddScoped<IFileService, LocalFileService>();

if (builder.Configuration.GetValue<bool>("FeatureFlags:UseRealEmailService"))
{
    builder.Services.AddScoped<IEmailService, RealEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}

// Configure logging with Serilog (already configured above)
// Serilog will handle all logging automatically

// ⭐ Updated Swagger Configuration with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Stibe Booking API",
        Version = "v1",
        Description = "Shop Booking Management System API"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\n\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Content-Disposition"); // For file downloads
        });
});

// Build the application once all services are configured
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stibe Booking API v1");
        c.RoutePrefix = "swagger";
    });

    // Automatically create database in development
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }

    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Configure static files and uploads directory
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(wwwrootPath, "uploads");
Directory.CreateDirectory(wwwrootPath);
Directory.CreateDirectory(uploadsPath);

// Default static files (wwwroot)
app.UseStaticFiles();

// Static files for uploads with caching and proper MIME types
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
    {
        // Set cache headers for uploaded files
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=3600");
        
        // Ensure proper MIME types for images
        var extension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                ctx.Context.Response.ContentType = "image/jpeg";
                break;
            case ".png":
                ctx.Context.Response.ContentType = "image/png";
                break;
        }
    }
});

app.MapGet("/", context => {
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});
app.UseCors("AllowAll");

// Add clean endpoint logging middleware
app.Use(async (context, next) =>
{
    var requestTime = DateTime.UtcNow;
    var requestId = Guid.NewGuid().ToString("N")[..8];
    
    // Only log API endpoints (ignore static files, swagger, etc.)
    var path = context.Request.Path.Value ?? "";
    var isApiEndpoint = path.StartsWith("/api/");
    
    if (isApiEndpoint)
    {
        var method = context.Request.Method;
        var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : "";
        
        // Determine operation type
        var operationType = method.ToUpperInvariant() switch
        {
            "GET" => "📥 PULL",
            "POST" => "📤 PUSH", 
            "PUT" => "📤 PUSH",
            "PATCH" => "📤 PUSH",
            "DELETE" => "🗑️ DELETE",
            _ => "📡 REQUEST"
        };
        
        // Log clean request
        Log.Information("🚀 [{RequestId}] {OperationType} {Method} {Path}{QueryString}", 
            requestId, operationType, method, path, queryString);
    }
    
    // Execute the request
    var originalBodyStream = context.Response.Body;
    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;
    
    try
    {
        await next(context);
        
        if (isApiEndpoint)
        {
            // Log clean response with body content
            var responseTime = DateTime.UtcNow;
            var duration = (responseTime - requestTime).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            
            var statusIcon = statusCode switch
            {
                >= 200 and < 300 => "✅",
                >= 300 and < 400 => "🔄",
                >= 400 and < 500 => "⚠️",
                >= 500 => "❌",
                _ => "❓"
            };
            
            var operationType = context.Request.Method.ToUpperInvariant() switch
            {
                "GET" => "📥 PULL",
                "POST" => "📤 PUSH", 
                "PUT" => "📤 PUSH",
                "PATCH" => "📤 PUSH",
                "DELETE" => "🗑️ DELETE",
                _ => "📡 REQUEST"
            };
            
            // Capture response body
            string responseBodyContent = "";
            responseBody.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(responseBody, leaveOpen: true))
            {
                responseBodyContent = await reader.ReadToEndAsync();
            }
            responseBody.Seek(0, SeekOrigin.Begin);
            
            // Log response with body content
            if (!string.IsNullOrEmpty(responseBodyContent))
            {
                Log.Information("{StatusIcon} [{RequestId}] {OperationType} {StatusCode} | {Duration:F0}ms | Response: {ResponseBody}", 
                    statusIcon, requestId, operationType, statusCode, duration, responseBodyContent);
            }
            else
            {
                Log.Information("{StatusIcon} [{RequestId}] {OperationType} {StatusCode} | {Duration:F0}ms", 
                    statusIcon, requestId, operationType, statusCode, duration);
            }
        }
        
        // Copy response back to original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }
    catch (Exception ex)
    {
        if (isApiEndpoint)
        {
            var responseTime = DateTime.UtcNow;
            var duration = (responseTime - requestTime).TotalMilliseconds;
            
            Log.Error("💥 [{RequestId}] EXCEPTION: {ExceptionMessage} | {Duration:F0}ms", 
                requestId, ex.Message, duration);
        }
        
        throw;
    }
    finally
    {
        context.Response.Body = originalBodyStream;
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

    Log.Information("Stibe API started successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Stibe API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
