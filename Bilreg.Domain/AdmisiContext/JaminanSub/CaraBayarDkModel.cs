using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkFeature;

public record CaraBayarDkType : ICaraBayarDkKey
{
    public CaraBayarDkType(string caraBayarDkId, string caraBayarDkName)
    {
        Guard.Against.NullOrWhiteSpace(caraBayarDkId, nameof(caraBayarDkId));
        Guard.Against.NullOrWhiteSpace(caraBayarDkName, nameof(caraBayarDkName));

        CaraBayarDkId = caraBayarDkId;
        CaraBayarDkName = caraBayarDkName;
    }
    
    public string CaraBayarDkId { get; init; }
    public string CaraBayarDkName { get; init; }
    
    public static ICaraBayarDkKey Key(string id) => new CaraBayarDkType(id, "-");
    public static CaraBayarDkType Default => new("-", "-");
}

public interface ICaraBayarDkKey
{
    string CaraBayarDkId {get;}
}