using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.BedUsageContext.RoomRateFeature;

public record RoomRateRegulerType : IRoomRate<RoomRateRegulerTipeType>
{
    private readonly List<RoomRateRegulerTipeType> _listTipe;
    public RoomRateRegulerType(string kamarId, KamarReff kamar, 
        IEnumerable<RoomRateRegulerTipeType> listTipe)
    {
        KamarId = kamarId;
        Kamar = kamar;
        _listTipe = listTipe?.ToList() ?? [];
    }

    public static RoomRateRegulerType Default => new(string.Empty,
        KamarType.Default.ToReff(), []);
    public string KamarId { get; }
    public KamarReff Kamar { get; init; }
    public IEnumerable<RoomRateRegulerTipeType> ListTipe => _listTipe;
}

public record RoomRateRegulerTipeType(
    TipeKamarReff TipeKamar,
    IEnumerable<RoomRateKomponenType> ListKomponen) : IRoomRateDetail
{
    public decimal TotalNilai => ListKomponen.Sum(x => x.Nilai);
}