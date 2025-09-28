using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using stibe.api.Configuration;
using stibe.api.Data;
using stibe.api.Services.Interfaces;
using stibe.api.Services.Implementations.General;
using stibe.api.Services.Implementations;
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

// ===== SIMPLIFIED CONFIGURATION =====
// Using single appsettings.json file for all environments
// Environment-specific settings can be overridden via environment variables

Console.WriteLine($"🌍 Starting Stibe API...");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/stibe-api-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .MinimumLevel.Is(Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "StibeAPI")
    .CreateLogger();

try
{
    Log.Information("Starting Stibe API...");

var builder = WebApplication.CreateBuilder(args);

// Debug configuration loading
Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
Log.Information("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);
Log.Information("Configuration Sources: {Sources}", 
    string.Join(", ", ((IConfigurationRoot)builder.Configuration).Providers.Select(p => p.GetType().Name)));

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
try 
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Log.Information("Database connection string loaded: {HasConnection}", !string.IsNullOrEmpty(connectionString));
    
    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Error("Database connection string is missing!");
        throw new InvalidOperationException("Database connection string 'DefaultConnection' is not configured");
    }
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 40)), mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        });
        
        // Standard optimizations
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    });
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to configure database");
    throw;
}

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
Log.Information("JWT Settings loaded: {HasJwtSettings}", jwtSettings != null);
Log.Information("JWT Secret Key present: {HasSecretKey}", !string.IsNullOrEmpty(jwtSettings?.SecretKey));

if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    Log.Error("JWT settings are missing or incomplete!");
    throw new InvalidOperationException("JWT settings are not properly configured");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<PaymentSettings>(builder.Configuration.GetSection("Payment"));

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
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IUserCouponService, UserCouponService>();

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
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IGstService, GstService>();

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

// Configure CORS - Allow all for flexibility
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Content-Disposition");
        });
});

// Build the application once all services are configured
var app = builder.Build();

// Get logger for startup configuration
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();

// Set WebRootPath if needed
if (string.IsNullOrEmpty(app.Environment.WebRootPath))
{
    var wwwRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    app.Environment.WebRootPath = wwwRoot;
    startupLogger.LogInformation("🔧 WebRootPath set to: {WebRootPath}", wwwRoot);
}

startupLogger.LogInformation("🔧 Current WebRootPath: {WebRootPath}", app.Environment.WebRootPath);
startupLogger.LogInformation("🔧 Current ContentRootPath: {ContentRootPath}", app.Environment.ContentRootPath);

// Configure the HTTP request pipeline.
// Always enable Swagger for API documentation
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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    environment = app.Environment.EnvironmentName
}));

// Simple ping endpoint
app.MapGet("/api/ping", () => Results.Ok(new { 
    message = "pong", 
    timestamp = DateTime.UtcNow 
}));

// Standard error handling
app.UseExceptionHandler("/Error");
app.UseHsts();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: https:; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'";
    await next();
});

app.UseHttpsRedirection();

// Configure static files and uploads directory with improved production support
var wwwrootPath = app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(wwwrootPath, "uploads");

// Create subdirectories for uploads
var profileImagesPath = Path.Combine(uploadsPath, "profile-images");
var serviceImagesPath = Path.Combine(uploadsPath, "service-images");
var shopImagesPath = Path.Combine(uploadsPath, "shop-images");
var productImagesPath = Path.Combine(uploadsPath, "product-images");

// Ensure directories exist with error handling
try
{
    Directory.CreateDirectory(wwwrootPath);
    Directory.CreateDirectory(uploadsPath);
    Directory.CreateDirectory(profileImagesPath);
    Directory.CreateDirectory(serviceImagesPath);
    Directory.CreateDirectory(shopImagesPath);
    Directory.CreateDirectory(productImagesPath);
}
catch (Exception ex)
{
    startupLogger.LogError(ex, "Failed to create upload directories");
    // Don't throw - this shouldn't prevent startup
}

startupLogger.LogInformation("📁 Upload directories created/verified:");
startupLogger.LogInformation("   📂 WWW Root: {WwwRootPath}", wwwrootPath);
startupLogger.LogInformation("   📂 Uploads: {UploadsPath}", uploadsPath);
startupLogger.LogInformation("   📂 Profile Images: {ProfileImagesPath}", profileImagesPath);

// Default static files (wwwroot) - should be first
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    OnPrepareResponse = ctx =>
    {
        // Set cache headers for static files
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=86400");
        
        // Handle APK files specifically
        var extension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        if (extension == ".apk")
        {
            ctx.Context.Response.ContentType = "application/vnd.android.package-archive";
            ctx.Context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{ctx.File.Name}\"");
            startupLogger.LogInformation("📱 Serving APK file: {FileName}", ctx.File.Name);
        }
        else
        {
            startupLogger.LogDebug("📄 Serving static file: {FileName}", ctx.File.Name);
        }
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

// Use CORS policy
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
