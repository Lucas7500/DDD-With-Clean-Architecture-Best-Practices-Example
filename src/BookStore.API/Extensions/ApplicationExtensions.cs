using Asp.Versioning;
using BookStore.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using System.Threading.RateLimiting;

namespace BookStore.API.Extensions
{
    internal static class ApplicationExtensions
    {
        private const string CorsPolicyName = "cors-policy";
        private const string FixedWindowPolicyName = "fixed-window";

        internal static void AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            string[] allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policyBuilder =>
                {
                    if (allowedOrigins.Length is 0)
                        throw new InvalidOperationException("CORS origins must be configured in production.");

                    policyBuilder
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        internal static void AddRateLimiterConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            int permitLimit = configuration.GetValue("RateLimiting:FixedWindow:PermitLimit", 100);
            int windowInSeconds = configuration.GetValue("RateLimiting:FixedWindow:WindowInSeconds", 60);
            int queueLimit = configuration.GetValue("RateLimiting:FixedWindow:QueueLimit", 0);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(FixedWindowPolicyName, httpContext =>
                {
                    string clientIp = GetClientIpAddress(httpContext);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientIp,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromSeconds(windowInSeconds),
                            QueueLimit = queueLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    ProblemDetails problemDetails = new()
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too Many Requests",
                        Type = "https://tools.ietf.org/html/rfc6585#section-4",
                        Detail = "Rate limit exceeded. Try again later.",
                        Instance = context.HttpContext.Request.Path,
                    };

                    problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
                };
            });
        }

        internal static void AddApiVersioningConfiguration(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
        }

        internal static void AddRoutingAdditionalConfiguration(this IServiceCollection services)
        {
            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
            });
        }

        internal static void AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "BookStore API",
                    Version = "v1"
                });
            });
        }

        internal static void UseSwaggerInDevelopment(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "BookStore API v1");
                    options.RoutePrefix = "swagger";
                });
            }
        }

        internal static void AddExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    Exception? exception = context.Features
                        .Get<IExceptionHandlerFeature>()?.Error;

                    if (exception is null)
                        return;

                    (int status, string title, string type) = exception switch
                    {
                        BusinessRuleValidationException =>
                            (StatusCodes.Status400BadRequest,
                             "Business rule violation",
                             "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

                        _ =>
                            (StatusCodes.Status500InternalServerError,
                             "Internal Server Error",
                             "https://tools.ietf.org/html/rfc7231#section-6.6.1")
                    };

                    context.Response.StatusCode = status;

                    ProblemDetails problemDetails = new()
                    {
                        Status = status,
                        Title = title,
                        Type = type,
                        Instance = context.Request.Path,
                    };

                    problemDetails.Extensions["traceId"] = context.TraceIdentifier;

                    if (app.ApplicationServices
                        .GetRequiredService<IHostEnvironment>()
                        .IsDevelopment())
                    {
                        problemDetails.Detail = exception.Message;
                        problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
                        problemDetails.Extensions["stackTrace"] = exception.StackTrace?.TrimStart();
                    }

                    await context.Response.WriteAsJsonAsync(problemDetails);
                });
            });
        }

        internal static IEndpointConventionBuilder RequireFixedWindowRateLimiting(this IEndpointConventionBuilder builder)
        {
            return builder.RequireRateLimiting(FixedWindowPolicyName);
        }

        internal static IEndpointConventionBuilder RequireCorsPolicy(this IEndpointConventionBuilder builder)
        {
            return builder.RequireCors(CorsPolicyName);
        }

        private static string GetClientIpAddress(HttpContext httpContext)
        {
            string? forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
