using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class OrderOpModel : IOrderOpKey
{
    #region CREATION

    public OrderOpModel(
        string orderOpId, DateTime orderDate, AuditTrailType auditTrail,
        PasienReff pasien, RegReff reg,
        Icd10Type icd10, JenisOperasiType jenisOperasi, string namaOperasi, UrgencyLevelEnum urgencyLevel,
        PpaReff dokter, int estimasiDurasiInMinutes, DateTime preferedDate, string specialEquipment)
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

        Dokter = dokter;
        EstimasiDurasiInMinutes = estimasiDurasiInMinutes;
        PreferedDate = preferedDate;
        SpecialEquipment = specialEquipment;
    }

    public static OrderOpModel Default => new OrderOpModel(
        "-", new DateTime(3000, 1, 1), AuditTrailType.Default,
        PasienModel.Default.ToReff(), RegModel.Default.ToReff(),
        Icd10Type.Default, JenisOperasiType.Default, "-", UrgencyLevelEnum.Elective,
        PpaType.Default.ToReff(), 0, 
        new DateTime(3000, 1, 1), "-");
    
    public static IOrderOpKey Key(string id) => new OrderOpModel( 
        id, new DateTime(3000, 1, 1), AuditTrailType.Default,
        PasienModel.Default.ToReff(), RegModel.Default.ToReff(),
        Icd10Type.Default, JenisOperasiType.Default, "-", UrgencyLevelEnum.Elective,
        PpaType.Default.ToReff(), 0, 
        new DateTime(3000, 1, 1), "-");
    
    public static OrderOpModel CreateByPasien(PasienModel pasien, string userId)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        
        var auditTrail = AuditTrailType.Create(userId, DateTime.Now);
        var result = new OrderOpModel(
            Ulid.NewUlid().ToString(), DateTime.Now, auditTrail, 
            pasien.ToReff(), RegModel.Default.ToReff(),
            Icd10Type.Default, JenisOperasiType.Default, "-", UrgencyLevelEnum.Elective,
            PpaType.Default.ToReff(), 
            0, new DateTime(3000,1,1),"-");
        return result;
    }
    public static OrderOpModel CreateByReg(RegModel reg, string userId)
    {
        Guard.Against.Null(reg, nameof(reg));
        
        var auditTrail = AuditTrailType.Create(userId, DateTime.Now);
        var pasien = reg.Pasien;
        var result = new OrderOpModel(
            Ulid.NewUlid().ToString(), DateTime.Now, auditTrail, 
            pasien, reg.ToReff(),
            Icd10Type.Default, JenisOperasiType.Default, "-", UrgencyLevelEnum.Elective,
            PpaType.Default.ToReff(), 
            0, new DateTime(3000,1,1), "-");
        return result;
    }
    #endregion

    #region PROPERTIES
    public string OrderOpId { get; init; }
    public DateTime OrderDate { get; init; }
    public AuditTrailType AuditTrail { get; private set; }
    
    public PasienReff Pasien { get; init; }
    
    public RegReff Reg { get; init; }
    public Icd10Type Icd10 { get; private set;}
    public JenisOperasiType JenisOperasi { get; private set; }
    public string NamaOperasi { get; private set; }
    public UrgencyLevelEnum UrgencyLevel { get; private set; }
    
    public PpaReff Dokter { get; private set;}
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

    public void OperationalRequest(PpaType dokterDpjp,
        int estimasiDurasi, DateTime preferedDate, string specialEquipment)
    {
        Guard.Against.Null(dokterDpjp, nameof(dokterDpjp));
        Guard.Against.NullOrWhiteSpace(specialEquipment, nameof(specialEquipment));
        Guard.Against.Negative(estimasiDurasi, nameof(estimasiDurasi));
        
        if (preferedDate < DateTime.Now)
            throw new ArgumentException("Perefered Date invalid");

        Dokter = dokterDpjp.ToReff();
        EstimasiDurasiInMinutes = estimasiDurasi;
        PreferedDate = preferedDate;
        SpecialEquipment = specialEquipment;
    }
    public OrderOpReff ToReff() => new OrderOpReff(OrderOpId, OrderDate, NamaOperasi);
    #endregion

}

public interface IOrderOpKey
{
    string OrderOpId { get; }
}

public record OrderOpReff(string OrderOpId, DateTime OrderDate, string NamaOperasi);