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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using stibe.api.Web.Filters;

namespace stibe.api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure request size limits for file uploads
            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = 104857600; // 100MB
            });

            // Configure form options for multipart forms
            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = 104857600; // 100MB
                options.MultipartHeadersLengthLimit = 16384;
            });

            return services;
        }

        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Entity Framework with MySQL and Production Optimizations
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
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

            return services;
        }

        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.Configure<PaymentSettings>(configuration.GetSection("Payment"));

            // Configure Google OAuth Settings
            services.Configure<GoogleOAuthSettings>(configuration.GetSection("GoogleOAuth"));

            services.AddAuthentication(options =>
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
                var googleSettings = configuration.GetSection("GoogleOAuth").Get<GoogleOAuthSettings>();
                if (googleSettings != null && googleSettings.Enabled)
                {
                    googleOptions.ClientId = googleSettings.ClientId;
                    googleOptions.ClientSecret = googleSettings.ClientSecret;
                }
            });

            services.AddAuthorization();

            return services;
        }

        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure Feature Flags
            services.Configure<FeatureFlags>(configuration.GetSection("FeatureFlags"));

            // Register custom services
            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IRazorpayService, RazorpayService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IUserCouponService, UserCouponService>();

            // Register location service based on feature flag
            if (configuration.GetValue<bool>("FeatureFlags:UseRealLocationService"))
            {
                services.AddHttpClient<ILocationService, GoogleLocationService>();
            }
            else
            {
                services.AddScoped<ILocationService, MockLocationService>();
            }

            services.AddScoped<IStaffWorkService, StaffWorkService>();
            services.Configure<EmailConfiguration>(configuration.GetSection("SmtpSettings"));

            // Register file services
            services.AddScoped<LocalFileService>();
            services.AddScoped<AzureBlobFileService>();
            services.AddScoped<HybridFileService>();

            // Register the active file service based on configuration
            var fileStorageProvider = configuration["FileStorage:Provider"]?.ToLowerInvariant() ?? "local";
            if (fileStorageProvider == "azure")
            {
                services.AddScoped<IFileService, AzureBlobFileService>();
            }
            else if (fileStorageProvider == "hybrid")
            {
                services.AddScoped<IFileService, HybridFileService>();
            }
            else
            {
                services.AddScoped<IFileService, LocalFileService>();
            }

            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IGstService, GstService>();

            if (configuration.GetValue<bool>("FeatureFlags:UseRealEmailService"))
            {
                services.AddScoped<IEmailService, RealEmailService>();
            }
            else
            {
                services.AddScoped<IEmailService, MockEmailService>();
            }

            return services;
        }

        public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Stibe Booking API",
                    Version = "v1",
                    Description = "Shop Booking Management System API"
                });

                // Add operation filter for file uploads
                c.OperationFilter<FileUploadOperationFilter>();

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

            return services;
        }

        public static IServiceCollection AddCorsServices(this IServiceCollection services)
        {
            services.AddCors(options =>
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

            return services;
        }
    }
}