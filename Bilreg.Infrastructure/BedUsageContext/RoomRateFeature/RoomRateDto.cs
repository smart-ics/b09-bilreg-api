
// resharper disable inconsistentnaming

using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;

public record RoomRateDto(
    string fs_kd_kamar,
    string fs_kd_detil_tarif,
    string fs_kd_tipe_kamar,
    decimal fn_tarif,
    int fn_no_urut,
    string fs_kd_kelas,
    int fn_harike,
    string fs_nm_kamar,
    string fs_nm_detil_tarif,
    string fs_nm_tipe_kamar,
    string fs_nm_kelas)
{
    public static IEnumerable<RoomRateDto> FromModel(RoomRateRegulerType model, KelasReff kelas)
    {
        var result = model.ListTipe
            .SelectMany((hdr => hdr.ListKomponen), (hdr, dtl) => new { hdr, dtl })
            .Select((item, index) => new RoomRateDto(
                model.KamarId,
                item.dtl.Komponen.KomponenId,
                item.hdr.TipeKamar.TipeKamarId,
                item.dtl.Nilai,
                index + 1,  
                kelas.KelasId,
                1,
                model.Kamar.KamarId,
                item.dtl.Komponen.KomponenName,
                item.hdr.TipeKamar.TipeKamarId,
                kelas.KelasName));
        return result;
    }
    public static IEnumerable<RoomRateDto> FromModel(RoomRateDailyType model, KelasReff kelas)
    {
        var result = model.ListTipe
            .SelectMany((hdr => hdr.ListKomponen), (hdr, dtl) => new { hdr, dtl })
            .Select((item, index) => new RoomRateDto(
                model.KamarId,
                item.dtl.Komponen.KomponenId,
                item.hdr.TipeKamar.TipeKamarId,
                item.dtl.Nilai,
                index + 1,  
                kelas.KelasId,
                item.hdr.HariKe,
                model.Kamar.KamarId,
                item.dtl.Komponen.KomponenName,
                item.hdr.TipeKamar.TipeKamarId,
                kelas.KelasName));
        return result;
    }
    public static IEnumerable<RoomRateDto> FromModel(RoomRateFloatingType model)
    {
        var result = model.ListTipe
            .SelectMany((hdr => hdr.ListKomponen), (hdr, dtl) => new { hdr, dtl })
            .Select((item, index) => new RoomRateDto(
                model.KamarId,
                item.dtl.Komponen.KomponenId,
                item.hdr.TipeKamar.TipeKamarId,
                item.dtl.Nilai,
                index + 1,  // Sequential number
                item.hdr.Kelas.KelasId,
                1,
                model.Kamar.KamarId,
                item.dtl.Komponen.KomponenName,
                item.hdr.TipeKamar.TipeKamarId,
                item.hdr.Kelas.KelasName));
        return result;
    }
    
    public static IRoomRate<IRoomRateDetail> ToModel(IEnumerable<RoomRateDto> enumDto)
    {
        var listDto = enumDto.ToList();
        var roomRateKind = "";
        if (listDto.DistinctBy(x => x.fs_kd_kelas).Count() > 1)
            roomRateKind = "FLOATING";
        else if (listDto.DistinctBy(x => x.fn_harike).Count() > 1)
            roomRateKind = "DAILY";
        else
            roomRateKind = "REGULER";
        
        var kamarId = listDto.First().fs_kd_kamar;
        var kamarName = listDto.First().fs_nm_kamar;
        var kamar = new KamarType(kamarId, kamarName, BangsalType.Default.ToReff(), KelasType.Default.ToReff());
        
        IEnumerable<IRoomRateDetail> listDetail = roomRateKind switch
        {
            "FLOATING" => CreateFloating(listDto),
            "DAILY" => CreateDaily(listDto),
            //"REGULER" => CreateReguler(listDto),
            _ => throw new Exception("Invalid room rate kind")
        };
        var roomRateFactory = new RoomRateFactory();
        //IRoomRate result;
        if (listDetail is IEnumerable<RoomRateKelasType> listKelas)
        {
            
        }
        var result = roomRateFactory.Create(kamar, listDetail); //<--- this does not work.
        return result;
    }
    
    private static IEnumerable<RoomRateKelasType> CreateFloating(IEnumerable<RoomRateDto> listDto)
    {
        var result = listDto
            .GroupBy(x => new
            {
                x.fs_kd_kamar, x.fs_nm_kamar, 
                x.fs_kd_tipe_kamar,  x.fs_nm_tipe_kamar,
                x.fs_kd_kelas, x.fs_nm_kelas
            })
            .Select(x => new RoomRateKelasType(
                new TipeKamarReff(x.Key.fs_kd_tipe_kamar, x.Key.fs_nm_tipe_kamar, true),
                new KelasReff(x.Key.fs_kd_kelas, x.Key.fs_nm_kelas),
                x.Select(y => new RoomRateKomponenType(
                    new KomponenReff(y.fs_kd_detil_tarif, y.fs_nm_detil_tarif), y.fn_tarif))
                ));
        return result;
    }
    private static IEnumerable<RoomRateDayType> CreateDaily(IEnumerable<RoomRateDto> listDto)
    {
        throw new NotImplementedException();
    }
    private static IEnumerable<RoomRateRegulerType> CreateReguler(IEnumerable<RoomRateDto> listDto)
    {
        throw new NotImplementedException();
    }
    
}