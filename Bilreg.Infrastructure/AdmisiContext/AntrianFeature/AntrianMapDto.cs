using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public record AntrianMapDto(
    string fs_kd_dokter, string fs_kd_layanan,
    string fd_tgl_jadwal, string fs_jam_jadwal,
    int fn_no_antrian, string fs_flag,
    string fs_mr, string fs_nm_pasien,
    string fs_kd_trs_gen,
    string fs_nm_dokter, string fs_nm_layanan
)
{

    public static IEnumerable<AntrianMapDto> FromModel(AntrianMapModel model)
    {
        return model.ListSlot.Select(slot => new AntrianMapDto(
            fs_kd_dokter: model.Dokter.PpaId,
            fs_kd_layanan: model.Layanan.LayananId,
            fd_tgl_jadwal: model.TglPraktek.ToString("yyyy-MM-dd"),
            fs_jam_jadwal: model.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture),
            fn_no_antrian: slot.NoUrut,
            fs_flag: slot.Flag,
            fs_mr: slot.Pasien.PasienId,
            fs_nm_pasien: slot.Pasien.PasienName,
            fs_kd_trs_gen: slot.ReffId,
            fs_nm_dokter: model.Dokter.PpaName,
            fs_nm_layanan: model.Layanan.LayananName
        ));
    }

    public AntrianMapModel ToModel(IEnumerable<AntrianMapDto> dtos)
    {
        var list = dtos.ToList();
        var first = list[0];

        var dokter = new PpaReff(first.fs_kd_dokter, fs_nm_dokter);
        var layanan = new LayananReff(first.fs_kd_layanan, fs_nm_layanan);

        var tgl = DateOnly.Parse(first.fd_tgl_jadwal);
        var jam = TimeOnly.Parse(first.fs_jam_jadwal);

        var slots = list
            .OrderBy(x => x.fn_no_antrian)
            .Select(x => new AntrianMapSlotModel(
                NoUrut: x.fn_no_antrian,
                Pasien: new PasienReff(
                    x.fs_mr,
                    x.fs_nm_pasien,
                    new DateOnly(3000, 1, 1),
                    "-"
                ),
                Reg: new RegReff("-", fs_mr, fs_nm_pasien),
                ReffId: x.fs_kd_trs_gen,
                Flag: x.fs_flag
            ));

        var result = new AntrianMapModel(dokter, layanan, tgl, jam, slots);
        return result;
    }


}
