// resharper disable InconsistentNaming

using System.Globalization;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;

public record TaTrsBilling2Dto(
    string fs_kd_trs, decimal fn_no_urut, 
    string fs_kd_jenis_bayar, decimal fn_trs_p, decimal fn_trs_n,
    
    string fs_kd_trs_bayar, string fd_tgl_bayar, string fs_jam_bayar,
    string fs_kd_petugas_kasir, string fs_kd_petugas_medis,
    string fs_kd_detil_tarif, string fs_kd_grup_rek,

    string fs_kd_rek_ppdp, string fs_kd_rek_pdpt, string fs_kd_rek_disc,
    string fs_kd_rek_pdpt_lain, string fs_kd_rek_persediaan, string fs_kd_rek_tax, string fs_kd_rek_retur,
    
    string fs_nm_detil_tarif, string fs_nm_grup_rek, string fs_nm_peg_kasir, string fs_nm_peg_medis)
{
    public static TaTrsBilling2Dto FromModel(TrsBilling2Base model, string billingId)
    {
        TaTrsBilling2Dto result = null;
        if (model is TrsBilling2JasaType jasa)
            result = FromModelJasa(jasa, billingId); 
        if (model is TrsBilling2ObatType obat)
            result = FromModelObat(obat, billingId);
        return result;
    }
    private static TaTrsBilling2Dto FromModelJasa(TrsBilling2JasaType model, string billingId)
    {
        return new TaTrsBilling2Dto(
            billingId, model.NoUrut,
            model.NilaiBilling.JenisBayar, model.NilaiBilling.NilaiP, model.NilaiBilling.NilaiN,
            //  payment
            model.PaymentId, model.PaymentDate.ToString(DateFormatEnum.YMD),
            model.PaymentDate.ToString(DateFormatEnum.HMS),
            //  kasir
            model.Kasir.PegId, 
            //  jasa-only props
            model.Ppa.PpaId, model.Komponen.KomponenId, 
            //  obat-only props
            "", 
            //  rekening
            model.Rekening.Ppdp, model.Rekening.Pdpt, model.Rekening.Diskon,
            "", "", "", "", 
            //  support      
            model.Komponen.KomponenName, "", model.Kasir.PegName, model.Ppa.PpaName);
    }

    private static TaTrsBilling2Dto FromModelObat(TrsBilling2ObatType model, string billingId)
    {
        return new TaTrsBilling2Dto(
            billingId, model.NoUrut,
            model.NilaiBilling.JenisBayar, model.NilaiBilling.NilaiP, model.NilaiBilling.NilaiN,
            //  payment
            model.PaymentId, model.PaymentDate.ToString(DateFormatEnum.YMD),
            model.PaymentDate.ToString(DateFormatEnum.HMS),
            //  kasir
            model.Kasir.PegId, 
            //  jasa-only props
            "", "", 
            //  obat-only props
            model.GroupRek.GroupRekId, 
            //  rekening
            model.Rekening.Ppdp, model.Rekening.Pdpt, model.Rekening.Diskon,
            model.Rekening.PdptLain, model.Rekening.Persediaan, 
            model.Rekening.Tax, model.Rekening.Retur,  
            //  support      
            "", model.GroupRek.GroupRekName, model.Kasir.PegName, "");
    }
    
    public TrsBilling2Base ToModel()
    {
        if (fs_kd_detil_tarif.Trim() == "")
            return ToJasaModel();
        return ToObatModel();
    }
    private TrsBilling2JasaType ToJasaModel()
    {
        var komponen = new KomponenReff(fs_kd_detil_tarif, fs_nm_detil_tarif);
        var ppa = new PpaReff(fs_kd_petugas_medis, fs_nm_peg_medis);
        var kasir = PegType.Create(fs_kd_petugas_kasir, fs_nm_peg_kasir);
        var paymentDate = DateTime.ParseExact($"{fd_tgl_bayar} {fs_jam_bayar}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var nilaiBilling = new NilaiBillingType(fs_kd_jenis_bayar, fn_trs_p, fn_trs_n);
        var rekening = new RekJasaType(fs_kd_rek_ppdp, fs_kd_rek_pdpt, fs_kd_rek_disc);
        return new TrsBilling2JasaType(
            (int)fn_no_urut, fs_kd_trs_bayar, paymentDate,
            nilaiBilling, ppa, kasir, komponen, rekening);
    }
    
    private TrsBilling2ObatType ToObatModel()
    {
        var groupRek = new GroupRekReff(fs_kd_detil_tarif, fs_nm_detil_tarif);
        var kasir = PegType.Create(fs_kd_petugas_kasir, fs_nm_peg_kasir);
        var paymentDate = DateTime.ParseExact($"{fd_tgl_bayar} {fs_jam_bayar}", "yyyy-MM-dd HH:mm:ss", 
            CultureInfo.InvariantCulture);
        var nilaiBilling = new NilaiBillingType(fs_kd_jenis_bayar, fn_trs_p, fn_trs_n);
        var rekening = new RekObatType(fs_kd_rek_ppdp, fs_kd_rek_pdpt, fs_kd_rek_disc,
            fs_kd_rek_pdpt_lain, fs_kd_rek_persediaan, fs_kd_rek_tax, fs_kd_rek_retur);
        return new TrsBilling2ObatType(
            (int)fn_no_urut, fs_kd_trs_bayar, paymentDate,
            nilaiBilling, kasir, groupRek, rekening);
    }
}