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
    public TindakanTarifModel Tarif { get; private set; }
    
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