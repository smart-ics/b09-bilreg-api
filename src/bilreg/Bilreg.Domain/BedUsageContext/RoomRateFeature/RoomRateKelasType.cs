using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Domain.BedUsageContext.RoomRateFeature;

public record RoomRateFloatingType : IRoomRate<RoomRateKelasType>
{
    private readonly List<RoomRateKelasType> _listTipe;
    public RoomRateFloatingType(string kamarId,  
        KamarReff kamar, IEnumerable<RoomRateKelasType> listTipe)
    {
        KamarId = kamarId;
        Kamar = kamar;
        _listTipe = listTipe?.ToList() ?? [];
    }
    public static RoomRateFloatingType Default => new(string.Empty, 
        KamarType.Default.ToReff(), []);
    
    public string KamarId { get; }
    public KamarReff Kamar { get; }
    public IEnumerable<RoomRateKelasType> ListTipe => _listTipe;
}

public record RoomRateKelasType(
    TipeKamarReff TipeKamar, 
    KelasReff Kelas,
    IEnumerable<RoomRateKomponenType> ListKomponen): IRoomRateDetail
{
    public decimal TotalNilai => ListKomponen.Sum(x => x.Nilai);
}

