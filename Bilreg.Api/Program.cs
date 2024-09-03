using Bilreg.Api.Configurations;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

builder.Services
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
    .UseSwagger() 
    .UseSwaggerUI(); 

app.Run();
