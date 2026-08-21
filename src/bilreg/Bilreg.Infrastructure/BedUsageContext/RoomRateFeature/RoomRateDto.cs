
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
    decimal fn_no_urut,
    string fs_kd_kelas,
    decimal fn_harike,
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

        var factory = new RoomRateFactory();
        return roomRateKind switch
        {
            "FLOATING" => factory.Create(kamar, CreateFloating(listDto)),
            "DAILY"    => factory.Create(kamar, CreateDaily(listDto)),
            "REGULER"  => factory.Create(kamar, CreateReguler(listDto)),
            _ => throw new Exception("Invalid room rate kind"),
        };        
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
        var result = listDto
            .GroupBy(x => new
            {
                x.fs_kd_kamar, x.fs_nm_kamar, 
                x.fs_kd_tipe_kamar,  x.fs_nm_tipe_kamar,
                x.fn_harike
            })
            .Select(x => new RoomRateDayType(
                new TipeKamarReff(x.Key.fs_kd_tipe_kamar, x.Key.fs_nm_tipe_kamar, true),
                Convert.ToInt32(x.Key.fn_harike),
                x.Select(y => new RoomRateKomponenType(
                    new KomponenReff(y.fs_kd_detil_tarif, y.fs_nm_detil_tarif), y.fn_tarif))
            ));
        return result;
    }
    private static IEnumerable<RoomRateRegulerTipeType> CreateReguler(IEnumerable<RoomRateDto> listDto)
    {
        var result = listDto
            .GroupBy(x => new
            {
                x.fs_kd_kamar, x.fs_nm_kamar, 
                x.fs_kd_tipe_kamar,  x.fs_nm_tipe_kamar
            })
            .Select(x => new RoomRateRegulerTipeType(
                new TipeKamarReff(x.Key.fs_kd_tipe_kamar, x.Key.fs_nm_tipe_kamar, true),
                x.Select(y => new RoomRateKomponenType(
                    new KomponenReff(y.fs_kd_detil_tarif, y.fs_nm_detil_tarif), y.fn_tarif))
            ));
        return result;
    }
    
}