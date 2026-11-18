using Bilreg.Api.Configurations;
using Bilreg.Api.Hubs;
using Bilreg.Api.SignalR;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

// Disable client certificate requirement on HTTPS for development
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;
    });
});

builder.Services
    .AddDomain(builder.Configuration)
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration)
    .AddMassTransitConfiguration(builder.Configuration);

builder.Host
    .UseSerilog(SerilogConfiguration.ContextConfiguration);

var app = builder.Build();

BusinessDateStartup.LogBusinessDateStatus(app);

app
    .UseSerilogRequestLogging(SerilogConfiguration.SerilogRequestLoggingOption)
    .UseMiddleware<ErrorHandlerMiddleware>();

// Disable HTTPS redirection entirely for development to avoid mTLS issues
// The HTTPS port still works but doesn't require client certificates now
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app
    .UseRouting()
    .UseCors("corsapp")
    .UseAuthentication()
    .UseAuthorization();

app.MapControllers();
app.MapHub<AdmissionQueueRefreshHub>(AdmissionQueueRefreshContracts.HubPath).RequireCors("corsapp");
app.MapHub<WardCockpitHub>(WardCockpitContracts.HubPath).RequireCors("corsapp");
app.MapHub<PharmacyQueueHub>(PharmacyQueueContracts.HubPath).RequireCors("corsapp");

app.UseSwagger(c => c.RouteTemplate = "openapi/{documentName}.json");

app
    .MapScalarApiReference(opt =>
    {
        opt.Title = "BilReg + Pharmacy-Inventory API - Documentation By Scalar";
        opt.Theme = ScalarTheme.Kepler;
        opt.DarkMode = true;
    });


app.Run();

public partial class Program { }
