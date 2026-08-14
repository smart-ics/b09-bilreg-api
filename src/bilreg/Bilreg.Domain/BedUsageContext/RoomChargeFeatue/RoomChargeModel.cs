using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.AutoNumberHelper;
using System.Security.Cryptography.X509Certificates;

namespace Bilreg.Domain.BedUsageContext.RoomChargeFeatue;

public class RoomChargeModel : IRoomChargeKey
{
    private readonly List<RoomChargeKomponenModel> _listKomponen;
    private const string ID_PREFIX = "RMC";
    public RoomChargeModel(
        string roomChargeId, 
        string pakaiBedId, 
        DateTime timeCharge, 
        string userId, 
        RegReff reg, 
        LayananReff layanan, 
        BedReff bed, 
        decimal tarif, 
        decimal diskon,
        decimal total,
        decimal qty,
        IEnumerable<RoomChargeKomponenModel> listKomponen)
    {
        RoomChargeId = roomChargeId;
        PakaiBedId = pakaiBedId;
        TimeCharge = timeCharge;
        UserId = userId;
        Reg = reg;
        Layanan = layanan;
        Bed = bed;
        Tarif = tarif;
        Diskon = diskon;
        Total = total;
        Qty = qty;
        _listKomponen = listKomponen.ToList() ?? [];
    }

    public static RoomChargeModel Create(PakaiBedModel pakaiBed, RegModel reg, 
        LayananType layanan, BedType bed, DateTime occurredAt, string userId)
    {
        var newId = NunaId.New(ID_PREFIX);

        var result = new RoomChargeModel(
            newId, 
            pakaiBed.PakaiBedId, 
            occurredAt, 
            userId, 
            reg.ToReff(), 
            layanan.ToReff(), 
            bed.ToReff(), 
            0, 0, 0, 0, []);

        return result;
    }
    public static IRoomChargeKey Key(string id) => new RoomChargeModel(
        id, "-",
        new DateTime(3000, 1, 1),
        "-",
        RegModel.Default.ToReff(),
        LayananType.Default.ToReff(),
        BedType.Default.ToReff(),
        0, 0, 0, 0, []);
    #region CREATION

    #endregion

    #region PROPERTY
    public string RoomChargeId { get; init; }
    public string PakaiBedId { get; init; }
    public DateTime TimeCharge { get; init; }
    public string UserId { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; private set; }
    public BedReff Bed { get; private set; }
    public decimal Tarif { get; private set; }
    public decimal Diskon { get; private set;  }
    public decimal Total { get; private set; }
    public decimal Qty {  get; set; }
    public IEnumerable<RoomChargeKomponenModel> ListKomponen => _listKomponen;

    #endregion

    #region BEHAVIOR

    #endregion
}

public interface IRoomChargeKey
{
    string RoomChargeId { get; }
}

public record RoomChargeKomponenModel(
    string DetilTarifId, string DetilTarifName,
    decimal Tarif, decimal Diskon, decimal Total);

public record RoomChargeView(string RoomChargeId, string PakaiBedId, DateTime TimeCharge, 
    string UserId,RegReff Reg,LayananReff Layanan,
    BedReff Bed, decimal Tarif, decimal Diskon, decimal Total,decimal Qty);