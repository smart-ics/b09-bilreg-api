using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record OrderTdkModel : IOrderTdkKey
{
    #region CREATION
    public OrderTdkModel(
        string orderId, DateTime orderDate, 
        PasienReff pasien, RegReff reg, 
        PpaReff dokterOrder, LayananReff layanan, TarifReff tarif,
        string freeTextOrder, StatusOrderEnum statusOrder,
        AuditTrailType auditTrail)
    {
        OrderTdkId = orderId;
        OrderTdkDate = orderDate;

        Pasien = pasien;
        Reg = reg;
        DokterOrder = dokterOrder;
        Layanan = layanan;

        Tarif = tarif;
        FreeTextOrder = freeTextOrder;
        StatusOrder = statusOrder;

        
        AuditTrail = auditTrail;
    }

    public static OrderTdkModel Create(PasienModel pasien, PpaType dokter, 
        LayananType layanan, TarifType tarif, string userId)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(layanan, nameof(layanan));
        Guard.Against.Null(tarif, nameof(tarif));

        if (!dokter.IsDokter())
            throw new Exception("Dokter order harus dokter");
        
        var newId = Ulid.NewUlid().ToString();
        
        return new OrderTdkModel(newId, DateTime.Now, 
            pasien.ToReff(), RegModel.Default.ToReff(), 
            dokter.ToReff(), layanan.ToReff(), tarif.ToReff(), "-", 
            StatusOrderEnum.Ordered,
            AuditTrailType.Create(userId, DateTime.Now));
    }
    
    public static OrderTdkModel Create(PasienModel pasien, PpaType dokter, 
        LayananType layanan, string freeTextOrder, string userId)
    {
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(dokter, nameof(dokter));
        Guard.Against.Null(layanan, nameof(layanan));
        Guard.Against.NullOrWhiteSpace(freeTextOrder);

        if (!dokter.IsDokter())
            throw new Exception("Dokter order harus dokter");
        
        var newId = Ulid.NewUlid().ToString();
        
        return new OrderTdkModel(newId, DateTime.Now,
            pasien.ToReff(), RegModel.Default.ToReff(), 
            dokter.ToReff(), layanan.ToReff(), TarifType.Default.ToReff(), freeTextOrder, 
            StatusOrderEnum.Ordered, AuditTrailType.Create(userId, DateTime.Now));
    }
    public static OrderTdkModel Create(RegModel reg, PpaType dokter, 
        LayananType layanan, TarifType tarif, string userId)
    {
        Guard.Against.Null(reg);
        Guard.Against.Null(dokter);
        Guard.Against.Null(layanan);
        Guard.Against.Null(tarif);

        if (!dokter.IsDokter())
            throw new Exception("Dokter order harus dokter");
        if (!reg.IsAktif)
            throw new ArgumentException("Pasien sudah tidak aktif");            
        var newId = Ulid.NewUlid().ToString();
        
        return new OrderTdkModel(newId, DateTime.Now, reg.Pasien,
            reg.ToReff(), dokter.ToReff(), 
            layanan.ToReff(), tarif.ToReff(), "-", StatusOrderEnum.Ordered, 
            AuditTrailType.Create(userId, DateTime.Now));
    }

    public static OrderTdkModel Create(RegModel reg, PpaType dokter, 
        LayananType layanan, string freeTextOrder, string userId)
    {
        Guard.Against.Null(reg);
        Guard.Against.Null(dokter);
        Guard.Against.Null(layanan);
        Guard.Against.NullOrWhiteSpace(freeTextOrder);

        if (!dokter.IsDokter())
            throw new Exception("Dokter order harus dokter");
        if (!reg.IsAktif)
            throw new ArgumentException("Pasien sudah tidak aktif");            
        var newId = Ulid.NewUlid().ToString();
        
        return new OrderTdkModel(newId, DateTime.Now, reg.Pasien,
            reg.ToReff(), dokter.ToReff(), 
            layanan.ToReff(), TarifType.Default.ToReff(), freeTextOrder, StatusOrderEnum.Ordered, 
            AuditTrailType.Create(userId, DateTime.Now));
    }

    public static OrderTdkModel Default => new(
        "-", new DateTime(3000,1,1), PasienModel.Default.ToReff(),
        RegModel.Default.ToReff(),PpaType.Default.ToReff(),
        LayananType.Default.ToReff(), TarifType.Default.ToReff(), 
        "-", StatusOrderEnum.Ordered,
        AuditTrailType.Default);

    public static IOrderTdkKey Key(string id) => Default with { OrderTdkId = id };
    #endregion

    #region PROPERTIES
    public string OrderTdkId { get; init; }
    public DateTime OrderTdkDate { get; init; }
    
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    
    public PpaReff DokterOrder { get; init; }
    public LayananReff Layanan { get; init; }
    public TarifReff Tarif { get; init; }
    public string FreeTextOrder { get; init; }
    public StatusOrderEnum StatusOrder { get; private set; }

    public AuditTrailType AuditTrail { get; init; }
    #endregion

    #region BEHAVIOR
    public OrderTindakanReff ToReff() => new OrderTindakanReff(OrderTdkId, OrderTdkDate, Tarif);

    public void Execute(string userId)
    {
        AuditTrail.Modif(userId, DateTime.Now);
        StatusOrder = StatusOrderEnum.Executed;
    }

    public void Cancel(string userId)
    {
        AuditTrail.Batal(userId, DateTime.Now);
        StatusOrder = StatusOrderEnum.Cancelled;
    }
    #endregion
}

public interface IOrderTdkKey
{
    string OrderTdkId { get; }
}

public record OrderTindakanReff(string OrderId, DateTime OrderDate, TarifReff Tindakan);


public enum StatusOrderEnum
{ Ordered, Executed, Cancelled }
