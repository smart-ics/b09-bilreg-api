using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public record TarifServiceReff(string TarifServiceId, string TarifServiceName)
{
    public static TarifServiceReff Create(string tarifServiceId, string tarifServiceName)
    {
        Guard.Against.NullOrWhiteSpace(tarifServiceId);
        Guard.Against.NullOrWhiteSpace(tarifServiceName);
        return new TarifServiceReff(tarifServiceId, tarifServiceName);
    }

    public static TarifServiceReff Default => new("-", "-");
}
