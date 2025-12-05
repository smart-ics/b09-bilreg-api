using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record OrderTindakanModel : IOrderTindakanKey
{
    #region CREATION
    public OrderTindakanModel(
        string orderId,
        DateTime orderDate,
        PasienReff pasien,
        RegReff reg,
        PpaReff dokterOrder,
        LayananReff layanan,
        TarifReff tindakan)
    {
        OrderId = orderId;
        OrderDate = orderDate;
        Pasien = pasien;
        Reg = reg;
        DokterOrder = dokterOrder;
        Layanan = layanan;
        Tindakan = tindakan;
    }

    public static OrderTindakanModel Create(
        string orderId,
        DateTime orderDate,
        PasienReff pasien,
        RegReff reg,
        PpaReff dokterOrder,
        LayananReff layanan,
        TarifReff tindakan)
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.Null(pasien, nameof(pasien));
        Guard.Against.Null(reg, nameof(reg));
        Guard.Against.Null(dokterOrder, nameof(dokterOrder));
        Guard.Against.Null(layanan, nameof(layanan));
        Guard.Against.Null(tindakan, nameof(tindakan));

        return new OrderTindakanModel(orderId, orderDate, pasien, reg, dokterOrder, layanan, tindakan);
    }

    public static OrderTindakanModel Default => new(
        "-", new DateTime(3000,1,1),
        PasienModel.Default.ToReff(),
        RegModel.Default.ToReff(),
        PpaType.Default.ToReff(), 
        LayananType.Default.ToReff(), 
        TarifType.Default.ToReff()
    );

    public static IOrderTindakanKey Key(string id) => Default with { OrderId = id };
    #endregion

    #region PROPERTIES
    public string OrderId { get; init; }
    public DateTime OrderDate { get; init; }
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    public PpaReff DokterOrder { get; init; }
    public LayananReff Layanan { get; init; }
    public TarifReff Tindakan { get; init; }
    #endregion

    #region BEHAVIOR
    public OrderTindakanReff ToReff() => new OrderTindakanReff(OrderId, OrderDate, Tindakan);
    #endregion
}

public interface IOrderTindakanKey
{
    string OrderId { get; }
}

public record OrderTindakanReff(string OrderId, DateTime OrderDate, TarifReff Tindakan);


