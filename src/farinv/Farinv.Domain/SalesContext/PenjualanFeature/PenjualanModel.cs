using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Domain.SalesContext.ResepFeature;
using Farinv.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Farinv.Domain.SalesContext.PenjualanFeature;

public class PenjualanModel : IPenjualanKey
{
    private readonly List<PenjualanItemType> _listItem;
    private const string ID_PREFIX = "DU";

    public PenjualanModel(string penjualanId, DateTime penjualanDate, string resepId, RegReff reg,
        DokterReff dokter, LayananReff layanan, decimal subtotal, decimal totalEmbalase,
        decimal totalTax, decimal totalDiskon, decimal diskonLain, decimal biayaLain, decimal grandTotal,
        AuditTrailType auditTrail, IEnumerable<PenjualanItemType> listItem)
    {
        PenjualanId = penjualanId;
        PenjualanDate = penjualanDate;
        ResepId = resepId;
        Register = reg;
        Dokter = dokter;
        Layanan = layanan;
        SubTotal = subtotal;
        TotalEmbalase = totalEmbalase;
        TotalTax = totalTax;
        TotalDiskon = totalDiskon;
        DiskonLain = diskonLain;
        BiayaLain = biayaLain;
        GrandTotal = grandTotal;
        AuditTrail = auditTrail;
        _listItem = [.. listItem];
    }

    public static PenjualanModel Default => new("-", new DateTime(3000, 1, 1), "-",
        RegModel.Default.ToReff(), DokterType.Default.ToReff(),
        LayananType.Default.ToReff(), 0, 0, 0, 0, 0, 0, 0,
        AuditTrailType.Default, new List<PenjualanItemType>());

    public static PenjualanModel Key(string id) => new(id, new DateTime(3000, 1, 1), "-",
        RegModel.Default.ToReff(), DokterType.Default.ToReff(),
        LayananType.Default.ToReff(), 0, 0, 0, 0, 0, 0, 0,
        AuditTrailType.Default, new List<PenjualanItemType>());

    public static PenjualanModel CreateFromPoliRajal(
        RegModel reg, LayananType layanan,
        string userId, DateTime occurredAt = default)
    {
        Guard.Against.Null(reg);
        Guard.Against.Null(layanan);

        var newId = NunaId.New(ID_PREFIX);
        var auditTrail = AuditTrailType.Create(userId, occurredAt);
        var result = new PenjualanModel(newId, occurredAt, "-", reg.ToReff(), DokterType.Default.ToReff(), layanan.ToReff(),
            0, 0, 0, 0, 0, 0, 0, auditTrail, new List<PenjualanItemType>());

        return result;
    }

    public static PenjualanModel CreateFromResep(
        RegModel reg, LayananType layanan, ResepModel resep,
        string userId, DateTime occurredAt = default)
    {
        Guard.Against.Null(reg);
        Guard.Against.Null(layanan);

        var newId = NunaId.New(ID_PREFIX);
        var auditTrail = AuditTrailType.Create(userId, occurredAt);
        var result = new PenjualanModel(newId, occurredAt, resep.ResepId, reg.ToReff(), resep.Dokter, layanan.ToReff(),
            0, 0, 0, 0, 0, 0, 0, auditTrail, new List<PenjualanItemType>());

        return result;
    }

    public string PenjualanId { get; private set; }
    public DateTime PenjualanDate { get; private set; }
    public string ResepId { get; private set; }
    public RegReff Register { get; private set; }
    public DokterReff Dokter { get; private set; }
    public LayananReff Layanan { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TotalEmbalase { get; private set; }
    public decimal TotalTax { get; private set; }
    public decimal TotalDiskon { get; private set; }
    public decimal DiskonLain { get; private set; }
    public decimal BiayaLain { get; private set; }
    public decimal GrandTotal { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }
    public IEnumerable<PenjualanItemType> ListItem => _listItem;

    public void Void(string userId, DateTime voidedAt = default)
        => AuditTrail.Batal(userId, voidedAt);

public PenjualanReff ToReff() => new(PenjualanId, PenjualanDate, Register);
}

public record PenjualanReff(string PenjualanId, DateTime PenjualanDate, RegReff Register);