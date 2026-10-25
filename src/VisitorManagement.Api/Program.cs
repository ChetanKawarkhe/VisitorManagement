using VisitorManagement.Infrastructure.Messaging;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using VisitorManagement.Api.Correlation;
using VisitorManagement.Api.Middleware;
using VisitorManagement.Api.Models;
using VisitorManagement.Application.DTOs.Auth;
using VisitorManagement.Application.DTOs.Visitors;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Application.Services;
using VisitorManagement.Application.Validators;
using VisitorManagement.Infrastructure;
using VisitorManagement.Infrastructure.Authentication;
using VisitorManagement.Infrastructure.Persistence;

// Configure Serilog before building the application.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] " +
            "[CorrelationId: {CorrelationId}] " +
            "[Source: {Source}] " +
            "{Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/visitor-management-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true,
        outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] " +
            "[CorrelationId: {CorrelationId}] " +
            "[Source: {Source}] " +
            "{Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog.
    builder.Host.UseSerilog();

    // Controllers and automatic model validation.
    builder.Services
        .AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                string correlationId =
                    context.HttpContext.Items.TryGetValue(
                        CorrelationIdMiddleware.ItemKey,
                        out var value)
                    && value is string id
                        ? id
                        : Guid.NewGuid().ToString("D");

                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value!.Errors
                            .Select(error =>
                                string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? "Invalid value."
                                    : error.ErrorMessage)
                            .ToArray());

                var response = ApiErrorResponse.Create(
                    StatusCodes.Status400BadRequest,
                    "Validation Failed",
                    "Please correct the highlighted fields.",
                    correlationId,
                    errors);

                return new BadRequestObjectResult(response);
            };
        });

    builder.Services.AddEndpointsApiExplorer();

    // Infrastructure.
    builder.Services.AddInfrastructure(builder.Configuration);
    // Azure Storage Queue messaging.
    builder.Services.AddVisitorQueueMessaging(builder.Configuration);

    // Application services.
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IVisitorService, VisitorService>();
    builder.Services.AddScoped<ILookupService, LookupService>();

    // Correlation context.
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddScoped<
        ICorrelationContext,
        HttpCorrelationContext>();

    // FluentValidation.
    builder.Services.AddScoped<
        IValidator<CreateVisitorRequest>,
        CreateVisitorRequestValidator>();

    builder.Services.AddScoped<
        IValidator<UpdateVisitorRequest>,
        UpdateVisitorRequestValidator>();

    builder.Services.AddScoped<
        IValidator<LoginRequest>,
        LoginRequestValidator>();

    // JWT configuration.
    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
        ?? throw new InvalidOperationException(
            "JWT configuration is missing.");

    if (string.IsNullOrWhiteSpace(jwtOptions.Key) ||
        Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32)
    {
        throw new InvalidOperationException(
            "JWT signing key must contain at least 32 UTF-8 bytes.");
    }

    if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
        string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
        jwtOptions.ExpirationMinutes <= 0)
    {
        throw new InvalidOperationException(
            "JWT configuration is invalid.");
    }

    // JWT authentication.
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.Key)),

                    NameClaimType =
                        System.Security.Claims.ClaimTypes.Name,

                    RoleClaimType =
                        System.Security.Claims.ClaimTypes.Role
                };
        });

    builder.Services.AddAuthorization();

    // Allow the configured Blazor WebAssembly origin.
    const string blazorCorsPolicy = "BlazorWeb";

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(blazorCorsPolicy, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        });
    });

    // Swagger.
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Visitor Management API",
            Version = "v1",
            Description = "Employee Visitor Management Application"
        });

        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT access token."
            });

        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
    });

    var app = builder.Build();

    // Development database initialization.
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await context.Database.MigrateAsync();

        var seeder = scope.ServiceProvider
            .GetRequiredService<DatabaseSeeder>();

        await seeder.SeedAsync();
    }

    // Middleware order is important.

    // 1. Assign correlation ID first.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // 2. Handle exceptions centrally.
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 3. Standardize empty error responses.
    app.UseStatusCodePages(async statusCodeContext =>
    {
        var context = statusCodeContext.HttpContext;

        int status = context.Response.StatusCode;

        string title = status switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            _ => "Request Failed"
        };

        string message = status switch
        {
            400 => "The request is invalid.",
            401 => "Authentication is required.",
            403 => "You do not have permission to perform this action.",
            404 => "The requested resource was not found.",
            409 => "The request conflicts with the current resource state.",
            _ => "The request could not be completed."
        };

        string correlationId =
            context.Items.TryGetValue(
                CorrelationIdMiddleware.ItemKey,
                out var value)
            && value is string id
                ? id
                : Guid.NewGuid().ToString("D");

        var response = ApiErrorResponse.Create(
            status,
            title,
            message,
            correlationId);

        await context.Response.WriteAsJsonAsync(
            response,
            cancellationToken: context.RequestAborted);
    });

    // 4. Log HTTP request completion.
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        options.GetLevel = (
            httpContext,
            elapsed,
            exception) =>
        {
            if (exception is not null ||
                httpContext.Response.StatusCode >= 500)
            {
                return Serilog.Events.LogEventLevel.Error;
            }

            if (httpContext.Response.StatusCode >= 400)
            {
                return Serilog.Events.LogEventLevel.Warning;
            }

            return Serilog.Events.LogEventLevel.Information;
        };
    });

    // 5. HTTPS.
    app.UseHttpsRedirection();

    // 6. Allow requests from the configured Blazor origin.
    // CORS must execute before authentication and authorization.
    app.UseCors(blazorCorsPolicy);

    // 7. Authentication and authorization.
    app.UseAuthentication();
    app.UseAuthorization();
    // 7. Swagger in development.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // 8. Controllers.
    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(
        exception,
        "Visitor Management API terminated unexpectedly.");

    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program
{
}