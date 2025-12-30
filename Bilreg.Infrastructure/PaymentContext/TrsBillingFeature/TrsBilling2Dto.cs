// resharper disable InconsistentNaming

using System.Globalization;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public record TaTrsBilling2Dto(
    string fs_kd_trs,
    int fn_no_urut,
    string fs_kd_detil_tarif,
    string fs_kd_grup_rek,
    string fs_kd_trs_bayar,
    string fs_kd_jenis_bayar,
    decimal fn_trs_p,
    decimal fn_trs_n,
    string fs_kd_petugas_medis,
    string fs_kd_petugas_kasir,
    string fd_tgl_bayar,
    string fs_jam_bayar,
    string fs_kd_rek_ppdp,
    string fs_kd_rek_pdpt,
    string fs_kd_rek_pdpt_lain,
    string fs_kd_rek_disc,
    string fs_kd_rek_persediaan,
    string fs_kd_rek_tax,
    string fs_kd_rek_retur,
    string fs_nm_detil_tarif,
    string fs_nm_grup_rek,
    string fs_nm_peg_medis,
    string fs_nm_peg_kasir)
{
    public static TaTrsBilling2Dto FromModel(TrsBilling2JasaType model, string billingId)
    {
        return new TaTrsBilling2Dto(
            billingId,
            model.NoUrut,
            model.Komponen.KomponenId,
            "", // fs_kd_grup_rek - not in model
            model.PaymentId,
            model.JenisBayar,
            model.NilaiP,
            model.NilaiN,
            model.Ppa.PpaId,
            model.Kasir.PegId,
            model.PaymentDate.ToString(DateFormatEnum.YMD),
            model.PaymentDate.ToString(DateFormatEnum.HMS),
            model.RekPpdp, model.RekPdpt, "", "", "", "", "",
            "", "", "", "");
    }

    public static TaTrsBilling2Dto FromModel(TrsBilling2ObatType model, string billingId)
    {
        return new TaTrsBilling2Dto(
            billingId,
            model.NoUrut,
            "",
            model.GroupRek.GroupRekId,
            model.PaymentId,
            model.JenisBayar,
            model.NilaiP,
            model.NilaiN,
            model.Ppa.PpaId,
            model.Kasir.PegId,
            model.PaymentDate.ToString(DateFormatEnum.YMD),
            model.PaymentDate.ToString(DateFormatEnum.HMS),
            model.RekPpdp, model.RekPdpt, model.RekPdptLain, 
            model.RekDiskon, model.RekPersediaan, model.RekTax, model.RekRetur,
            "", "", "", "");
    }
    
    public ITrsBilling2 ToModel()
    {
        if (fs_kd_detil_tarif == "")
            return ToJasaModel();
        else
            return ToObatModel();
    }

    
    private TrsBilling2JasaType ToJasaModel()
    {
        var komponen = new KomponenReff(fs_kd_detil_tarif, fs_nm_detil_tarif);
        var ppa = new PpaReff(fs_kd_petugas_medis, fs_nm_peg_medis);
        var peg = PegType.Create(fs_kd_petugas_kasir, fs_nm_peg_kasir);
        var paymentDate = DateTime.ParseExact($"{fd_tgl_bayar} {fs_jam_bayar}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        return new TrsBilling2JasaType(
            fn_no_urut,
            fs_kd_trs_bayar,
            paymentDate,
            fs_kd_jenis_bayar,
            fn_trs_p,
            fn_trs_n,
            ppa,
            peg,
            komponen,
            fs_kd_rek_ppdp,
            fs_kd_rek_pdpt, fs_kd_rek_disc);
    }
    
    private TrsBilling2ObatType ToObatModel()
    {
        var groupRek = new GroupRekReff(fs_kd_grup_rek, fs_nm_grup_rek);
        var peg = PegType.Create(fs_kd_petugas_kasir, fs_nm_peg_kasir);
        var paymentDate = DateTime.ParseExact($"{fd_tgl_bayar} {fs_jam_bayar}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        return new TrsBilling2ObatType(
            fn_no_urut,
            fs_kd_trs_bayar,
            paymentDate,
            fs_kd_jenis_bayar,
            fn_trs_p,
            fn_trs_n,
            PpaType.Default.ToReff(),
            peg,
            groupRek,
            fs_kd_rek_ppdp,
            fs_kd_rek_pdpt, fs_kd_rek_disc, fs_kd_rek_pdpt_lain,
            fs_kd_rek_persediaan, fs_kd_rek_tax, fs_kd_rek_retur);
    }

}