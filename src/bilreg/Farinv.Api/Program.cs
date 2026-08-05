
using Farinv.Api.Configurations;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

builder.Services
    .AddDomain(builder.Configuration)
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

builder.Host
    .UseSerilog(SerilogConfiguration.ContextConfiguration);

var app = builder.Build();

app
    .UseSerilogRequestLogging(SerilogConfiguration.SerilogRequestLoggingOption)
    .UseMiddleware<ErrorHandlerMiddleware>()
    .UseHttpsRedirection()
    .UseRouting()
    .UseCors("corsapp")
    .UseAuthentication()
    .UseAuthorization()
    .UseEndpoints(ep => ep.MapControllers())
    .UseSwagger(c => c.RouteTemplate = "openapi/{documentName}.json");

app
    .MapScalarApiReference(opt =>
    {
        opt.Title = "Pharmacy-Inventory API - Documentation By Scalar";
        opt.Theme = ScalarTheme.Kepler;
        opt.DarkMode = true;
    });


app.Run();




