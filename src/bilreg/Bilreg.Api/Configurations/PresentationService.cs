using System.Text;
using Bilreg.Api.Authorization;
using Bilreg.Api.Filters;
using Bilreg.Application.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nuna.Lib.ActionResultHelper;

namespace Bilreg.Api.Configurations;

public static class PresentationService
{

    public static IServiceCollection AddPresentation(this IServiceCollection services,
        IConfiguration configuration)
    {
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
                        StatusCodes.Status422UnprocessableEntity,
                        "Validation Error",
                        string.IsNullOrWhiteSpace(errors) ? "Invalid request." : errors);
                    return new UnprocessableEntityObjectResult(payload);
                };
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SchemaFilter<DefaultExampleSchemaFilter>();
            c.SchemaFilter<TataRekeningExampleSchemaFilter>();
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
                ValidAudience = configuration["Jwt:Audience"],
                ValidIssuer = configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? string.Empty))
            };
        });

        services.AddAuthorization();
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddScoped<AdmisiRanapEnabledFilter>();

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
}
