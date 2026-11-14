using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class OrderOpModel : IOrderOpKey
{
    private readonly List<OrderOpResourceModel> _listResource = new();

    #region CREATION

    public OrderOpModel(
        string orderOpId,
        DateTime orderDate,
        AuditTrailType auditTrail,
        PasienReff pasien,
        RegReff reg,
        Icd10Type icd10,
        JenisOperasiType jenisOperasi,
        string namaOperasi,
        UrgencyLevelEnum urgencyLevel,
        TarifReff tarif,
        PpaReff dokterOperator,
        int estimasiDurasiInMinutes,
        DateTime preferedDate,
        string specialEquipment,
        PpaReff? dokterPengorder = null,
        LayananReff? layananPengorder = null,
        string prosedurTambahan = "",
        KlasifikasiOperasiEnum klasifikasiOperasi = KlasifikasiOperasiEnum.Kecil,
        AnestesiPlanEnum anestesiPlan = AnestesiPlanEnum.General,
        PostOpCareUnitEnum postOpCareUnit = PostOpCareUnitEnum.Ward,
        string alasanBatal = "",
        IEnumerable<OrderOpResourceModel>? listResource = null)
    {
        OrderOpId = orderOpId;
        OrderDate = orderDate;
        AuditTrail = auditTrail;

        Pasien = pasien;
        Reg = reg;

        Icd10 = icd10;
        JenisOperasi = jenisOperasi;
        NamaOperasi = namaOperasi;
        UrgencyLevel = urgencyLevel;
        TarifOperasi = tarif ?? TarifReff.Default;

        DokterOperator = dokterOperator ?? PpaReff.Default;
        Dokter = DokterOperator;
        EstimasiDurasiInMinutes = estimasiDurasiInMinutes;
        PreferedDate = preferedDate;
        SpecialEquipment = specialEquipment;

        DokterPengorder = dokterPengorder ?? PpaReff.Default;
        LayananPengorder = layananPengorder ?? new LayananReff("-", "-");
        ProsedurTambahan = prosedurTambahan ?? string.Empty;
        KlasifikasiOperasi = klasifikasiOperasi;
        AnestesiPlan = anestesiPlan;
        PostOpCareUnit = postOpCareUnit;
        AlasanBatal = alasanBatal ?? string.Empty;

        if (listResource != null)
        {
            _listResource.AddRange(listResource);
        }
    }

    public static OrderOpModel Default => new OrderOpModel(
        "-", new DateTime(3000, 1, 1), AuditTrailType.Default,
        PasienModel.Default.ToReff(), RegModel.Default.ToReff(),
        Icd10Type.Default, JenisOperasiType.Default, "-", UrgencyLevelEnum.Elective,
        TarifReff.Default, PpaReff.Default, 0,
        new DateTime(3000, 1, 1), "-",
        PpaReff.Default, new LayananReff("-", "-"),
        string.Empty, KlasifikasiOperasiEnum.Kecil, AnestesiPlanEnum.General,
        PostOpCareUnitEnum.Ward, string.Empty, []);

    public static IOrderOpKey Key(string id) => new OrderOpModel(
        id, new DateTime(3000, 1, 1), AuditTrailType.Default,
        PasienModel.Default.ToReff(), RegModel.Default.ToReff(),
        Icd10Type.Default, JenisOperasiType.Default, "-", UrgencyLevelEnum.Elective,
        TarifReff.Default, PpaReff.Default, 0,
        new DateTime(3000, 1, 1), "-",
        PpaReff.Default, new LayananReff("-", "-"),
        string.Empty, KlasifikasiOperasiEnum.Kecil, AnestesiPlanEnum.General,
        PostOpCareUnitEnum.Ward, string.Empty, []);

    public static OrderOpModel CreateByReg(
        RegModel reg,
        LayananType layananPengorder,
        PpaType dokterPengorder,
        string userId,
        DateTime createdAt = default)
    {
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.Null(layananPengorder, nameof(layananPengorder));
        Guard.Against.Null(dokterPengorder, nameof(dokterPengorder));

        return CreateByReg(reg, layananPengorder.ToReff(), dokterPengorder.ToReff(), userId, createdAt);
    }

    public static OrderOpModel CreateByReg(
        RegModel reg,
        LayananReff layananPengorder,
        PpaReff dokterPengorder,
        string userId,
        DateTime createdAt = default)
    {
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.Null(layananPengorder, nameof(layananPengorder));
        Guard.Against.Null(dokterPengorder, nameof(dokterPengorder));

        var auditTrail = AuditTrailType.Create(userId, createdAt);
        var pasien = reg.Pasien;
        var result = new OrderOpModel(
            Ulid.NewUlid().ToString(),
            createdAt,
            auditTrail,
            pasien,
            reg.ToReff(),
            Icd10Type.Default,
            JenisOperasiType.Default,
            "-",
            UrgencyLevelEnum.Elective,
            TarifReff.Default,
            PpaReff.Default,
            0,
            new DateTime(3000, 1, 1),
            "-",
            dokterPengorder,
            layananPengorder,
            string.Empty,
            KlasifikasiOperasiEnum.Kecil,
            AnestesiPlanEnum.General,
            PostOpCareUnitEnum.Ward,
            string.Empty,
            []);
        return result;
    }

    #endregion

    #region PROPERTIES
    public string OrderOpId { get; init; }
    public DateTime OrderDate { get; init; }
    public AuditTrailType AuditTrail { get; private set; }

    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    public Icd10Type Icd10 { get; private set; }
    public JenisOperasiType JenisOperasi { get; private set; }
    public string NamaOperasi { get; private set; }
    public UrgencyLevelEnum UrgencyLevel { get; private set; }
    public TarifReff TarifOperasi { get; set; }

    public PpaReff DokterPengorder { get; private set; }
    public LayananReff LayananPengorder { get; private set; }
    public PpaReff DokterOperator { get; private set; }
    public PpaReff Dokter { get; private set; }
    public string ProsedurTambahan { get; private set; }
    public KlasifikasiOperasiEnum KlasifikasiOperasi { get; private set; }
    public AnestesiPlanEnum AnestesiPlan { get; private set; }
    public PostOpCareUnitEnum PostOpCareUnit { get; private set; }
    public string AlasanBatal { get; private set; }

    public IEnumerable<OrderOpResourceModel> ListResource => _listResource;

    public int EstimasiDurasiInMinutes { get; private set; }
    public DateTime PreferedDate { get; private set; }
    public string SpecialEquipment { get; private set; }

    public OpCaseStateEnum OrderOpState { get; private set; }
    #endregion

    #region BEHAVIORS
    public void SetKlinis(Icd10Type icd10, JenisOperasiType jenisOperasi, string namaOperasi)
    {
        Guard.Against.Null(icd10, nameof(icd10));
        Guard.Against.Null(jenisOperasi, nameof(jenisOperasi));
        Guard.Against.NullOrWhiteSpace(namaOperasi, nameof(namaOperasi));

        Icd10 = icd10;
        JenisOperasi = jenisOperasi;
        NamaOperasi = namaOperasi;
    }

    public void SetProsedur(string namaOperasi, string prosedurTambahan, KlasifikasiOperasiEnum klasifikasiOperasi)
    {
        Guard.Against.NullOrWhiteSpace(namaOperasi, nameof(namaOperasi));
        NamaOperasi = namaOperasi;
        ProsedurTambahan = prosedurTambahan ?? string.Empty;
        KlasifikasiOperasi = klasifikasiOperasi;
    }

    public void SetRencanaAnestesiDanPerawatan(AnestesiPlanEnum anestesiPlan, PostOpCareUnitEnum postOpCareUnit)
    {
        AnestesiPlan = anestesiPlan;
        PostOpCareUnit = postOpCareUnit;
    }

    public void SetDokterOperator(PpaReff dokterOperator)
    {
        DokterOperator = dokterOperator ?? PpaReff.Default;
        Dokter = DokterOperator;
    }

    public void SetDokterOperator(PpaType dokterOperator)
    {
        DokterOperator = dokterOperator?.ToReff() ?? PpaReff.Default;
        Dokter = DokterOperator;
    }

    public void SetUrgencyLevel(UrgencyLevelEnum urgencyLevel)
    {
        UrgencyLevel = urgencyLevel;
    }

    public void OperationalRequest(
        int estimasiDurasi,
        DateTime preferedDate,
        string specialEquipment,
        DateTime requestedAt = default)
    {
        Guard.Against.NullOrWhiteSpace(specialEquipment, nameof(specialEquipment));
        Guard.Against.Negative(estimasiDurasi, nameof(estimasiDurasi));

        if (preferedDate < requestedAt)
            throw new ArgumentException("Prefered Date invalid");

        EstimasiDurasiInMinutes = estimasiDurasi;
        PreferedDate = preferedDate;
        SpecialEquipment = specialEquipment;
    }

    public void OperationalRequest(
        PpaType dokterDpjp,
        int estimasiDurasi,
        DateTime preferedDate,
        string specialEquipment,
        DateTime requestedAt = default)
    {
        Guard.Against.Null(dokterDpjp, nameof(dokterDpjp));
        OperationalRequest(estimasiDurasi, preferedDate, specialEquipment, requestedAt);
        DokterOperator = dokterDpjp.ToReff();
        Dokter = DokterOperator;
    }

    public void SetTarif(TarifType tarif)
    {
        TarifOperasi = tarif?.ToReff() ?? TarifReff.Default;
    }

    public void SetTarif(TarifReff tarif)
    {
        TarifOperasi = tarif ?? TarifReff.Default;
    }

    public void AddResource(OrderOpResourceModel resource)
    {
        Guard.Against.Null(resource, nameof(resource));
        _listResource.Add(resource);
    }

    public void AddResource(
        OrderOpResourceTypeEnum resourceType,
        string itemName,
        int jumlah,
        string satuan,
        string keterangan = "")
    {
        var resource = OrderOpResourceModel.Create(
            Ulid.NewUlid().ToString(),
            OrderOpId,
            resourceType,
            itemName,
            jumlah,
            satuan,
            keterangan);
        _listResource.Add(resource);
    }

    public void SetResources(IEnumerable<OrderOpResourceModel> resources)
    {
        _listResource.Clear();
        if (resources != null)
        {
            _listResource.AddRange(resources);
        }
    }

    public void RemoveResource(string orderOpResourceId)
    {
        Guard.Against.NullOrWhiteSpace(orderOpResourceId, nameof(orderOpResourceId));
        _listResource.RemoveAll(x => x.OrderOpResourceId == orderOpResourceId);
    }

    public void Cancel(string userId, string alasanBatal, DateTime cancelledAt)
    {
        Guard.Against.NullOrWhiteSpace(alasanBatal, nameof(alasanBatal));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        AuditTrail.Batal(userId, cancelledAt);
        AlasanBatal = alasanBatal;
        OrderOpState = OpCaseStateEnum.Cancelled;
    }

    public OrderOpReff ToReff() => new OrderOpReff(OrderOpId, OrderDate, NamaOperasi);
    #endregion
}

public interface IOrderOpKey
{
    string OrderOpId { get; }
}

public record OrderOpReff(string OrderOpId, DateTime OrderDate, string NamaOperasi);

