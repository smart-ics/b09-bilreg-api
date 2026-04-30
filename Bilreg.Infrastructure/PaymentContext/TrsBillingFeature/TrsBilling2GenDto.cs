using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public record TrsBilling2GenDto(
    string fs_kd_trs, decimal fn_no_urut, string fs_kd_detil_tarif, string fs_kd_grup_rek, 
    string fs_kd_trs_bayar, string fs_kd_jenis_bayar, decimal fn_trs_p, decimal fn_trs_n,
    string fd_tgl_bayar, string fs_jam_bayar, string fs_kd_petugas_medis, string fs_kd_petugas_kasir)
{
    public static TrsBilling2GenDto FromModel(TrsBilling2Model model)
    {
        return new TrsBilling2GenDto(
            model.TrsBillingId, model.NoUrut, model.KomponenId, model.GrupRekId,
            model.TrsBayarId, model.JenisBayar, model.NilaiP, model.NilaiN,
            model.TglJamBayar.ToString(DateFormatEnum.YMD), model.TglJamBayar.ToString(DateFormatEnum.HMS),
            model.MedisId, model.KasirId);
    }

    public TrsBilling2Model ToModel()
    {
        return new TrsBilling2Model(
            fs_kd_trs, (int)fn_no_urut, fs_kd_detil_tarif, fs_kd_grup_rek,
            fs_kd_trs_bayar, fs_kd_jenis_bayar, fn_trs_p, fn_trs_n,
            DateTime.ParseExact($"{fd_tgl_bayar} {fs_jam_bayar}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), 
            fs_kd_petugas_medis, fs_kd_petugas_kasir);
    }
}
