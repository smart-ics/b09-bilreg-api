using System.Text;
using Bilreg.Api.Authorization;
using Bilreg.Api.Filters;
using Bilreg.Api.SignalR;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Nuna.Lib.ActionResultHelper;
using System.Text.Json;

namespace Bilreg.Api.Configurations;

public static class PresentationService
{

    public static IServiceCollection AddPresentation(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<AdmissionQueueApiOptions>,AdmissionQueueApiOptionsValidator>();
        services.AddOptions<AdmissionQueueApiOptions>()
            .Bind(configuration.GetSection(AdmissionQueueApiOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IAdmissionQueueRolloutConfig, AdmissionQueueRolloutConfig>();
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = string.Join("; ",
                        context.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage));
                    var payload = new JSend(
                        StatusCodes.Status400BadRequest,
                        "AQ_INVALID_REQUEST",
                        string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors);
                    return new BadRequestObjectResult(payload);
                };
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SchemaFilter<DefaultExampleSchemaFilter>();
            //c.SchemaFilter<TataRekeningExampleSchemaFilter>();
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidIssuer = configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        path.StartsWithSegments(AdmissionQueueRefreshContracts.HubPath))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    CreateAuthLogger(context.HttpContext).LogWarning(
                        context.Exception,
                        "JWT authentication failed: {ErrorMessage}",
                        context.Exception.Message);
                    return Task.CompletedTask;
                },

                OnTokenValidated = context =>
                {
                    CreateAuthLogger(context.HttpContext).LogDebug(
                        "JWT token validated for {UserName}",
                        context.Principal?.Identity?.Name ?? "(unknown)");
                    return Task.CompletedTask;
                },

                OnChallenge = context =>
                {
                    CreateAuthLogger(context.HttpContext).LogWarning(
                        "JWT challenge issued: {Error} {ErrorDescription}",
                        context.Error,
                        context.ErrorDescription);
                    context.HandleResponse();
                    context.Response.StatusCode=StatusCodes.Status401Unauthorized;
                    context.Response.ContentType="application/json";
                    return context.Response.WriteAsync(JsonSerializer.Serialize(new JSend(
                        StatusCodes.Status401Unauthorized,"AQ_UNAUTHENTICATED","Authentication is required.")));
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode=StatusCodes.Status403Forbidden;
                    context.Response.ContentType="application/json";
                    return context.Response.WriteAsync(JsonSerializer.Serialize(new JSend(
                        StatusCodes.Status403Forbidden,"AQ_FORBIDDEN","Access is forbidden.")));
                }
            };
        });

        services.AddAuthorization();
        services.AddSignalR();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddScoped<AdmisiRanapEnabledFilter>();
        services.AddScoped<JourneyEndpointsEnabledFilter>();

        services.AddCors(p => p.AddPolicy("corsapp", policyBuilder =>
        {
            policyBuilder.WithOrigins("*")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowAnyOrigin();
        }));
        
        services.AddHttpContextAccessor();
        
        return services;
    }

    private static ILogger CreateAuthLogger(HttpContext httpContext) =>
        httpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Bilreg.Api.Authentication");
}
