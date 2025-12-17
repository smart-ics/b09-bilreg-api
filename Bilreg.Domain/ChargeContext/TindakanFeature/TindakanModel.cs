using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.ValidationHelper;
using System.Security.Cryptography.X509Certificates;

namespace Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;

public record TindakanModel : ITindakanKey
{
    #region CREATION
    
    public TindakanModel(
        string tindakanId,
        DateTime tindakanDate,
        JenisTindakanEnum jenisTindakan,
        AuditTrailType auditTrail,
        OrderTindakanReff orderTindakan,
        PasienReff pasien,
        RegReff reg,
        LayananReff layanan,
        TipeTarifReff tipeTarif,
        TindakanTarifModel tarif)
    {
        TindakanId = tindakanId;
        TindakanDate = tindakanDate;
        JenisTindakan = jenisTindakan;
        AuditTrail = auditTrail;
        OrderTindakan = orderTindakan;
        Pasien = pasien;
        Reg = reg;
        Layanan = layanan;
        TipeTarif = tipeTarif;
        Tarif = tarif;
        
    }

    public static TindakanModel Create(
        JenisTindakanEnum jenisTindakan,
        OrderTdkModel orderTindakan,
        PasienModel pasien,
        RegModel reg,
        LayananType layanan,
        TipeTarifType tipeTarif,
        TindakanTarifModel tarif,
        string userId)
    {
        Guard.Against.Null(orderTindakan, nameof(orderTindakan));
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.Null(layanan, nameof(layanan));

        var auditTrail = AuditTrailType.Create(userId, DateTime.Now);
        var newId = Ulid.NewUlid().ToString();
        return new TindakanModel(newId, DateTime.Now, jenisTindakan, auditTrail, orderTindakan.ToReff(), 
            pasien.ToReff(), reg.ToReff(), layanan.ToReff(), tipeTarif.ToReff(), tarif);
    }

    public static TindakanModel Default => new(
        "-", 
        DateTime.Today, 
        JenisTindakanEnum.Tindakan,
        AuditTrailType.Default, 
        OrderTdkModel.Default.ToReff(),
        PasienModel.Default.ToReff(), 
        RegModel.Default.ToReff(),
        LayananType.Default.ToReff(), 
        TipeTarifType.Default.ToReff(),
        TindakanTarifModel.Default
    );

    public static ITindakanKey Key(string id) => Default with { TindakanId = id };
    #endregion

    #region PROPERTIES
    public string TindakanId { get; init; }
    public DateTime TindakanDate { get; init; }
    public JenisTindakanEnum JenisTindakan { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public OrderTindakanReff OrderTindakan { get; init; }
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public TipeTarifReff TipeTarif { get; init; }
    public TindakanTarifModel Tarif { get; init; }
    
    #endregion

    #region BEHAVIOR
    public TindakanReff ToReff() => new(TindakanId, TindakanDate, JenisTindakan, Tarif.Tarif);

    public void Void(string userId)
    {
        AuditTrail.Batal(userId, DateTime.Now);
    }

    #endregion
}

public interface ITindakanKey
{
    string TindakanId { get; }
}

public record TindakanReff(string TindakanId, DateTime TindakanDate, 
    JenisTindakanEnum JenisTindakan, TarifReff Tarif);

public record TindakanView(string TindakanId, DateTime TindakanDate, OrderTindakanReff OrderTdk,
    JenisTindakanEnum JenisTindakan, RegReff reg, LayananReff Layanan, TipeTarifReff TipeTarif, TarifReff Tarif) : ITindakanKey;

public enum JenisTindakanEnum
{
    Tindakan,
    Lab,
    Pakai_Bhp,
    Radiology
}