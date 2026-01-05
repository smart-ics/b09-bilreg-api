using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianMapHdrDto(
    string fs_kd_jadwal,
    string fs_kd_dokter,
    string fs_kd_layanan,
    DateTime fd_tgl_jadwal,
    string fs_jam_jadwal,
    string fs_jam_praktek,
    string fs_nm_dokter, 
    string fs_nm_layanan 
)
{
    public static AntrianMapHdrDto FromModel(AntrianMapHdrModel model)
    {

        var result = new AntrianMapHdrDto(
            fs_kd_jadwal: model.JadwalId,
            fs_kd_dokter: model.Dokter.PpaId,
            fs_kd_layanan: model.Layanan.LayananId,
            fd_tgl_jadwal:  model.TglJadwal.ToDateTime(TimeOnly.MinValue),
            fs_jam_jadwal: model.JamJadwal.ToString("HH:mm", CultureInfo.InvariantCulture),
            fs_jam_praktek:model.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture),
            fs_nm_dokter: model.Dokter.PpaName,
            fs_nm_layanan: model.Layanan.LayananName
        );
        return result;
    }
    public AntrianMapHdrModel ToModel(IEnumerable<AntrianMapDto> dtos)
    {
        var list = dtos.ToList();
        var slots = list
        .OrderBy(x => x.fn_no_antrian)
        .Select(x => new AntrianMapModel(
            TglPraktek: DateOnly.FromDateTime(fd_tgl_jadwal),
            DokterId: fs_kd_dokter,
            LayananId: fs_kd_layanan,
            JamJadwal: TimeOnly.Parse(fs_jam_jadwal),
            NoUrut: (int)x.fn_no_antrian,
            Pasien: new PasienReff(
                x.fs_mr,
                x.fs_nm_pasien,
                new DateOnly(3000, 1, 1),
                "-"
            ),
            Reg: new RegReff("-", x.fs_mr, x.fs_nm_pasien),
            ReffId: x.fs_kd_trs_gen,
            Flag: x.fs_flag
        ));

        return AntrianMapHdrModel.Create(
            jadwalId: fs_kd_jadwal,
            dokter: new PpaReff(fs_kd_dokter, fs_nm_dokter),
            layanan: new LayananReff(fs_kd_layanan, fs_nm_layanan),
            tglJadwal: DateOnly.FromDateTime(fd_tgl_jadwal),
            jamJadwal: TimeOnly.ParseExact(fs_jam_jadwal, @"HH\:mm"),
            jamPraktek: TimeOnly.ParseExact(fs_jam_praktek, @"HH\:mm"),
            listMap: slots
        );
    }

    public AntrianMapHdrView ToView()
    {
        var dokter = new PpaReff(fs_kd_dokter, fs_nm_dokter);
        var lyn = new LayananReff(fs_kd_layanan, fs_nm_layanan);
        var result = new AntrianMapHdrView(fs_kd_jadwal, dokter, lyn,
            DateOnly.FromDateTime(fd_tgl_jadwal),
            TimeOnly.ParseExact(fs_jam_jadwal, @"HH\:mm"),
            TimeOnly.ParseExact(fs_jam_praktek, @"HH\:mm"));
        return result;
    }


}