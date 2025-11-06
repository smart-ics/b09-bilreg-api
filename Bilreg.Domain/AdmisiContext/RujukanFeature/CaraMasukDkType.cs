using Ardalis.GuardClauses;

namespace Bilreg.Domain.AdmisiContext.RujukanFeature;

public record CaraMasukDkType : ICaraMasukDkKey
{
    #region CREATION
    public CaraMasukDkType(string caraMasukDkId, string caraMasukDkName)
    {
        Guard.Against.NullOrWhiteSpace(caraMasukDkId, nameof(caraMasukDkId));
        Guard.Against.NullOrWhiteSpace(caraMasukDkName, nameof(caraMasukDkName));

        CaraMasukDkId = caraMasukDkId;
        CaraMasukDkName = caraMasukDkName;
    }

    public static CaraMasukDkType Default => new CaraMasukDkType("-", "-");
    public static ICaraMasukDkKey Key(string id) => new CaraMasukDkType(id, "-");
    public static CaraMasukDkType DatangSendiri => new("8", "DATANG SENDIRI");
    public static CaraMasukDkType RujukanRs => new("1", "RUJUKAN RSU/RSK/RB");
    public static CaraMasukDkType RujukanPuskesmas => new("2", "RUJUKAN PUSKESMAS");
    public static CaraMasukDkType RujukanDokter => new("3", "RUJUKAN DR/DRG");

    
    #endregion
    
    public string CaraMasukDkId { get; init; }
    public string CaraMasukDkName { get; init; }
    
}

public interface ICaraMasukDkKey
{
    string CaraMasukDkId {get;}
}