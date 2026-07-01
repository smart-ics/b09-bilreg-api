namespace Farinv.Domain.SalesContext.ResepFeature;

public record BodyMetricType
{
    public BodyMetricType(decimal bodyWeight, decimal bodyHeight, decimal lingkarPinggang)
    {
        BodyWeight = bodyWeight;
        BodyHeight = bodyHeight;
        LingkarPinggang = lingkarPinggang;
    }

    public decimal BodyWeight { get; init; }
    public decimal BodyHeight { get; init; }
    public decimal LingkarPinggang { get; init; }

    public static BodyMetricType Default() => new(0, 0, 0);
}