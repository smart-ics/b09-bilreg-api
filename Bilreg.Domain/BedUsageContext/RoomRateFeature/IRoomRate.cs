using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Domain.BedUsageContext.RoomRateFeature;

public interface IRoomRate<out T> : IKamarKey
    where T : IRoomRateDetail
{
    KamarReff Kamar { get; }
    IEnumerable<T> ListTipe { get; }
}

public interface IRoomRateDetail
{
    TipeKamarReff TipeKamar { get; }
    decimal TotalNilai { get; }
}

public record RoomRateKomponenType(KomponenReff Komponen, decimal Nilai)
{
    public static RoomRateKomponenType Default => new (KomponenType.Default.ToReff(), 0);
};

