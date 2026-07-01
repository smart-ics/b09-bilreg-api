using System.Reflection;
using Serilog;
using Serilog.AspNetCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Bilreg.Api.Configurations;

public static class SerilogConfiguration
{
    public static void SerilogRequestLoggingOption(RequestLoggingOptions options)
    {
        // Customize the message template
        options.MessageTemplate = "[{RequestMethod}] {RequestScheme}://{RequestHost}{PathBase}{RequestPath} from {ClientIp} responded {StatusCode} in {Elapsed:N0} ms";
    
        // Attach additional properties to the request completion event
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("LocalIp", httpContext.Connection.LocalIpAddress);
            diagnosticContext.Set("Referer", httpContext.Request.Headers["Referer"]);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["sec-ch-ua"]);
            diagnosticContext.Set("UserName", httpContext.User.Identity?.Name??string.Empty);
            diagnosticContext.Set("PathBase", httpContext.Request?.PathBase?? string.Empty);
        };          
    }

    public static void ContextConfiguration(HostBuilderContext context, LoggerConfiguration configuration)
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.WithProperty("AppName", Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown")
            .Enrich.WithProperty("AppPublishDate", GetBuildDate(Assembly.GetExecutingAssembly()));
    }

    private static string GetBuildDate(Assembly assembly)
    {
        var attribute = assembly.GetCustomAttribute<BuildDateAttribute>();
        return attribute?.DateTime.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
    }

}