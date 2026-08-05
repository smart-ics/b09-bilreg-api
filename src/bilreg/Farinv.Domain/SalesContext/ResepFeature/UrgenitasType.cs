using Ardalis.GuardClauses;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;

namespace Farinv.Domain.SalesContext.ResepFeature;

public record UrgenitasType
{
    private UrgenitasType(string id, string name)
    {
        Guard.Against.NullOrWhiteSpace(id);
        Guard.Against.NullOrWhiteSpace(name);
        
        UrgenitasId = id;
        UrgenitasName = name;
    }
    public string UrgenitasId { get; init; }
    public string UrgenitasName { get; init; }
    
    public static UrgenitasType Default => new (AppConst.DASH,AppConst.DASH);
}