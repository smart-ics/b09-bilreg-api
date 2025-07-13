using Ardalis.GuardClauses;

namespace Bilreg.Domain.PasienContext;

public record CaraMasukDkType : ICaraMasukDkKey
{
    public CaraMasukDkType(string caraMasukDkId, string caraMasukDkName)
    {
        Guard.Against.NullOrWhiteSpace(caraMasukDkId, nameof(caraMasukDkId));
        Guard.Against.NullOrWhiteSpace(caraMasukDkName, nameof(caraMasukDkName));

        CaraMasukDkId = caraMasukDkId;
        CaraMasukDkName = caraMasukDkName;
    }
    
    public string CaraMasukDkId { get; init; }
    public string CaraMasukDkName { get; init; }
    
    public static ICaraMasukDkKey Key(string id) => new CaraMasukDkType(id, "-");
    public static CaraMasukDkType Default => new("-", "-");
}

public interface ICaraMasukDkKey
{
    string CaraMasukDkId {get;}
}