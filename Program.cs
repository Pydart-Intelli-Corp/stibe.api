using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using stibe.api.Configuration;
using stibe.api.Data;
using stibe.api.Services.Interfaces;
using stibe.api.Services.Implementations.General;
using stibe.api.Services;
using System.Text;
using stibe.api.Services.Interfaces.Partner;
using stibe.api.Services.Implementations.MockServices;
using stibe.api.Services.Implementations.LocationServices;
using stibe.api.Services.Implementations.SecurityServices;
using stibe.api.Services.Implementations.PartnerServices.StaffServices;
using stibe.api.Services.Implementations.FileService;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Serilog;

// ===== ENVIRONMENT CONFIGURATION =====
// Quick environment switching for development/testing:
// 
// For DEVELOPMENT (default):
//   - Uses appsettings.Development.json
//   - More verbose logging
//   - Database auto-creation
//   - Swagger enabled
//
// For PRODUCTION:
//   - Set environment variable: ASPNETCORE_ENVIRONMENT=Production
//   - Uses appsettings.Production.json  
//   - Minimal logging
//   - No database auto-creation
//   - Swagger disabled in production
//
// Quick override (uncomment one line below):
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
// NOTE: Environment is managed by the host (IIS / web.config) for production. Avoid setting it here so deployments honor the host configuration.

// Configure environment - uses ASPNETCORE_ENVIRONMENT or defaults to Development for easy local development
// For production deployment, set ASPNETCORE_ENVIRONMENT=Production in your hosting environment
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
var isDevelopment = environment == "Development";

Console.WriteLine($"🌍 Environment: {environment}");
Console.WriteLine($"🔧 Development Mode: {isDevelopment}");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/stibe-api-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: isDevelopment ? 3 : 30,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .MinimumLevel.Is(isDevelopment ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", isDevelopment ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "StibeAPI")
    .Enrich.WithProperty("Environment", environment)
    .CreateLogger();

try
{
    Log.Information("Starting Stibe API...");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Configure request size limits for file uploads
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 104857600; // 100MB
});

// Configure form options for multipart forms
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 104857600; // 100MB
    options.MultipartHeadersLengthLimit = 16384;
});

// Configure Entity Framework with MySQL and Production Optimizations
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)), mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
    
    // Production optimizations
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    }
    else
    {
        options.EnableSensitiveDataLogging(true);
        options.EnableDetailedErrors(true);
    }
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
builder.Services.AddScoped<IRazorpayService, RazorpayService>();

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

// Configure CORS with environment-specific policies
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Development: Allow all origins for testing
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("Content-Disposition");
            });
    }
    else
    {
        // Production: Restrict to specific origins
        options.AddPolicy("Production",
            policy =>
            {
                policy.WithOrigins(
                        "http://192.168.1.34:5048",
                        "https://192.168.1.34:5048",
                        "http://202.164.153.160",
                        "http://192.168.1.34:5048",
                        "https://202.164.153.160",
                        "https://192.168.1.34:5048",
                        "https://stibe.app",
                        "https://www.stibe.app"
                      )
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .WithExposedHeaders("Content-Disposition");
            });
        
        // Fallback policy for API testing
        options.AddPolicy("AllowAll",
            policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .WithExposedHeaders("Content-Disposition");
            });
    }
});

// Build the application once all services are configured
var app = builder.Build();

// Get logger for startup configuration
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

// Explicitly set WebRootPath for production environment
if (app.Environment.IsProduction() && string.IsNullOrEmpty(app.Environment.WebRootPath))
{
    var productionWwwRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    app.Environment.WebRootPath = productionWwwRoot;
    startupLogger.LogInformation("🔧 Production WebRootPath set to: {WebRootPath}", productionWwwRoot);
}

startupLogger.LogInformation("🔧 Current WebRootPath: {WebRootPath}", app.Environment.WebRootPath);
startupLogger.LogInformation("🔧 Current ContentRootPath: {ContentRootPath}", app.Environment.ContentRootPath);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stibe Booking API v1");
        c.RoutePrefix = "swagger";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        c.DefaultModelExpandDepth(2);
        c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });

    // Automatically create database in development
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            context.Database.EnsureCreated();
            Log.Information("Database ensured for development environment");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to ensure database in development");
        }
    }

    app.UseDeveloperExceptionPage();
}
else
{
    // Production error handling
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    
    // Health check endpoint for production monitoring
    app.MapGet("/health", () => Results.Ok(new { 
        status = "healthy", 
        timestamp = DateTime.UtcNow,
        version = "1.0.0",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    }));
}

// Security headers for production
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: https:; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";
        await next();
    });
}

app.UseHttpsRedirection();

// Configure static files and uploads directory with improved production support
var wwwrootPath = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(wwwrootPath, "uploads");

// Ensure directories exist
Directory.CreateDirectory(wwwrootPath);
Directory.CreateDirectory(uploadsPath);

// Create subdirectories for uploads
var profileImagesPath = Path.Combine(uploadsPath, "profile-images");
var serviceImagesPath = Path.Combine(uploadsPath, "service-images");
var shopImagesPath = Path.Combine(uploadsPath, "shop-images");
var productImagesPath = Path.Combine(uploadsPath, "product-images");

Directory.CreateDirectory(profileImagesPath);
Directory.CreateDirectory(serviceImagesPath);
Directory.CreateDirectory(shopImagesPath);
Directory.CreateDirectory(productImagesPath);

startupLogger.LogInformation("📁 Upload directories created/verified:");
startupLogger.LogInformation("   📂 WWW Root: {WwwRootPath}", wwwrootPath);
startupLogger.LogInformation("   📂 Uploads: {UploadsPath}", uploadsPath);
startupLogger.LogInformation("   📂 Profile Images: {ProfileImagesPath}", profileImagesPath);

// Default static files (wwwroot) - should be first
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Set cache headers for static files
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=86400");
        startupLogger.LogDebug("📄 Serving static file: {FileName}", ctx.File.Name);
    }
});

// Static files for uploads with enhanced configuration
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = false,
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
            case ".gif":
                ctx.Context.Response.ContentType = "image/gif";
                break;
            case ".webp":
                ctx.Context.Response.ContentType = "image/webp";
                break;
            case ".svg":
                ctx.Context.Response.ContentType = "image/svg+xml";
                break;
        }
        
        startupLogger.LogDebug("📸 Serving upload file: {FileName} ({ContentType})", ctx.File.Name, ctx.Context.Response.ContentType);
    }
});

// Add a diagnostic endpoint to check uploads directory
app.MapGet("/api/test/uploads-info", () =>
{
    var result = new
    {
        uploadsPath,
        uploadsExists = Directory.Exists(uploadsPath),
        profileImagesExists = Directory.Exists(profileImagesPath),
        wwwrootPath,
        wwwrootExists = Directory.Exists(wwwrootPath),
        files = Directory.Exists(uploadsPath) ? 
            Directory.GetFiles(uploadsPath, "*", SearchOption.AllDirectories)
                .Select(f => new { 
                    path = f.Replace(uploadsPath, "").Replace("\\", "/"),
                    exists = File.Exists(f),
                    size = new FileInfo(f).Length 
                }).Take(10).ToArray() : 
            Array.Empty<object>()
    };
    return Results.Ok(result);
});

app.MapGet("/", context => {
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

// Use environment-specific CORS policy
var corsPolicy = app.Environment.IsDevelopment() ? "AllowAll" : "Production";
app.UseCors(corsPolicy);

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
