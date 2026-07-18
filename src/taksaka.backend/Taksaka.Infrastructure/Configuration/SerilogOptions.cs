namespace Taksaka.Infrastructure.Configuration;

public sealed class SerilogOptions
{
    public const string SectionName = "Serilog";

    public string MinimumLevel { get; set; } = "Information";
}
