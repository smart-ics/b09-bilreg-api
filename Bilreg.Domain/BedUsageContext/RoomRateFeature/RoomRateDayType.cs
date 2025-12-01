using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.BedUsageContext.RoomRateFeature;

public record RoomRateDailyType : IRoomRate<RoomRateDayType>
{
    private readonly List<RoomRateDayType> _listTipe;
    public RoomRateDailyType(string kamarId, KamarReff kamar, 
        IEnumerable<RoomRateDayType> listTipe)
    {
        KamarId = kamarId;
        Kamar = kamar;
        _listTipe = listTipe?.ToList() ?? [];
    }
    public static RoomRateDailyType Default => new(string.Empty, 
        KamarType.Default.ToReff(), []);
    public string KamarId { get; }
    public KamarReff Kamar { get; }
    public IEnumerable<RoomRateDayType> ListTipe => _listTipe;
}

public record RoomRateDayType(
    TipeKamarReff TipeKamar, int HariKe,
    IEnumerable<RoomRateKomponenType> ListKomponen): IRoomRateDetail
{
    public decimal TotalNilai => ListKomponen.Sum(x => x.Nilai);
}
