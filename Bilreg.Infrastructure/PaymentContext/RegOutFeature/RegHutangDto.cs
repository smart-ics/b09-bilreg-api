using Bilreg.Domain.PaymentContext.RegOutFeature;
using System.Globalization;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public record RegHutangDto(
    string fs_kd_reg,
    string fd_tgl_piutang,
    decimal fn_piutang,
    decimal fn_sisa,
    decimal fn_nilai_jasa,
    decimal fn_nilai_obat)
{
    public RegHutangType ToModel()
    {
        var piutangDate = DateOnly.ParseExact(fd_tgl_piutang, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var result = new RegHutangType(fs_kd_reg, piutangDate, fn_piutang, fn_sisa, fn_nilai_jasa, fn_nilai_obat);
        return result;
    }
}
