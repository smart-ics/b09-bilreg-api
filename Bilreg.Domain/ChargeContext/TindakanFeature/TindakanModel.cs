using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using System.Security.Cryptography.X509Certificates;

namespace Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;

public record TindakanModel : ITindakanKey
{
    #region CREATION
    
    public TindakanModel(
        string tindakanId,
        DateTime tindakanDate,
        AuditTrailType auditTrail,
        OrderTindakanReff orderTindakan,
        PasienReff pasien,
        RegReff reg,
        LayananReff layanan,
        TindakanTarifModel tarif)
    {
        TindakanId = tindakanId;
        TindakanDate = tindakanDate;
        AuditTrail = auditTrail;
        OrderTindakan = orderTindakan;
        Pasien = pasien;
        Reg = reg;
        Layanan = layanan;
        Tarif = tarif;
        
    }

    public static TindakanModel Create(
        string tindakanId,
        DateTime tindakanDate,
        AuditTrailType auditTrail,
        OrderTindakanReff orderTindakan,
        PasienReff pasien,
        RegReff reg,
        LayananReff layanan,
        TindakanTarifModel tarif)
    {
        Guard.Against.NullOrWhiteSpace(tindakanId, nameof(tindakanId));
        Guard.Against.Null(auditTrail, nameof(auditTrail));
        Guard.Against.Null(orderTindakan, nameof(orderTindakan));
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.Null(layanan, nameof(layanan));
        
        return new TindakanModel(tindakanId, tindakanDate, auditTrail, orderTindakan, 
            pasien, reg, layanan, tarif);
    }

    public static TindakanModel Default => new(
        "-", 
        DateTime.Today, 
        AuditTrailType.Default, 
        OrderTdkModel.Default.ToReff(),
        PasienModel.Default.ToReff(), 
        RegModel.Default.ToReff(),
        LayananType.Default.ToReff(), 
        TindakanTarifModel.Default
    );

    public static ITindakanKey Key(string id) => Default with { TindakanId = id };
    #endregion

    #region PROPERTIES
    public string TindakanId { get; init; }
    public DateTime TindakanDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public OrderTindakanReff OrderTindakan { get; init; }
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public TindakanTarifModel Tarif { get; init; }
    
    #endregion

    #region BEHAVIOR
    public TindakanReff ToReff() => new(TindakanId, TindakanDate, Tarif.Tarif);
    #endregion
}

public interface ITindakanKey
{
    string TindakanId { get; }
}

public record TindakanReff(string TindakanId, DateTime TindakanDate, TarifReff Tarif);

public record TindakanView(string TindakanId, DateTime TindakanDate, RegReff reg, TarifReff Tarif) : ITindakanKey;
