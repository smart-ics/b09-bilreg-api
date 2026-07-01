using Serilog;
using Taksaka.Engine;
using Taksaka.Infrastructure.DependencyInjection;
using Taksaka.Infrastructure.Logging;
using Taksaka.Infrastructure.SignalR;
using Taksaka.Server.DependencyInjection;
using Taksaka.Server.SignalR;

var builder = WebApplication.CreateBuilder(args);

SerilogConfiguration.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();

builder.Services
    .AddTaksakaInfrastructure(builder.Configuration)
    .AddTaksakaEngine()
    .AddTaksakaServer(builder.Configuration);

var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Taksaka API v1");
    });
}

app.UseCors("TaksakaCors");
app.UseRouting();
app.MapControllers();
app.MapHub<OperationsHub>(SignalREndpoints.Operations);
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

public partial class Program;
