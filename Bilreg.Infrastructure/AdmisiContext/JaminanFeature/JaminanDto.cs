using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

//  resharper disable inconsistentnaming
namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public record JaminanDto(
    string fs_kd_jaminan, 
    string fs_nm_jaminan ,
    bool fb_aktif ,
    string fs_alm1_jaminan ,
    string fs_alm2_jaminan ,
    string fs_kota_jaminan ,
    string fs_kd_pos ,

    string fs_kd_cara_bayar_dk ,
    string fs_kd_grup_jaminan,
    string fs_kd_tipe_tarif_rawat_jalan,
    string fs_kd_tipe_tarif_rawat_inap,
    string fs_kd_tipe_brg_rawat_jalan, 
    string fs_kd_tipe_brg_rawat_inap,

    string fs_piut_rawat, 
    string fs_piut_obat_rawat, 
    string fs_kd_rek_ppdp_jasa_ri,
    string fs_kd_rek_ppdp_obat_ri,
    
    string fs_nm_cara_bayar_dk ,
    string fs_nm_grup_jaminan,
    string fs_nm_tarif_tipe_rawat_jalan,
    string fs_nm_tarif_tipe_rawat_inap,
    string fs_nm_piut_rawat,
    string fs_nm_piut_obat_rawat,
    string fs_nm_rek_ppdp_jasa_ri,
    string fs_nm_rek_ppdp_obat_ri,    

    string fs_nm_tipe_brg_rawat_jalan,
    string fs_nm_tipe_brg_rawat_inap
    )
{
    public static JaminanDto FromModel(JaminanType model)
    {
        var result = new JaminanDto(
            model.JaminanId,
            model.JaminanName,
            model.IsAktif,

            model.Alamat.Alamat[0],
            model.Alamat.Alamat[1],
            model.Alamat.Kota,
            model.Alamat.KodePos,
            
            model.CaraBayarDk.CaraBayarDkId,
            model.GroupJaminan.GroupJaminanId,
            
            model.TipeTarif.Rajal.TipeTarifId,
            model.TipeTarif.Ranap.TipeTarifId,
            model.TipeBarang.Rajal.TipeBarangId,
            model.TipeBarang.Ranap.TipeBarangId,

            model.Rekening.PpdpJasaRajal.CoaId,
            model.Rekening.PpdpObatRajal.CoaId,
            model.Rekening.PpdpJasaRanap.CoaId,
            model.Rekening.PpdpObatRanap.CoaId,
            
            model.CaraBayarDk.CaraBayarDkName,
            model.GroupJaminan.GroupJaminanName,
            
            model.TipeTarif.Rajal.TipeTarifName,
            model.TipeTarif.Ranap.TipeTarifName,
            model.Rekening.PpdpJasaRajal.CoaName,
            model.Rekening.PpdpObatRajal.CoaName,
            model.Rekening.PpdpJasaRanap.CoaName,
            model.Rekening.PpdpObatRanap.CoaName,

            model.TipeBarang.Rajal.TipeBarangName,
            model.TipeBarang.Ranap.TipeBarangName
        );
        return result;
    }
    
    public JaminanType ToModel()
    {
        var caraBayarDk = new CaraBayarDkType(fs_kd_cara_bayar_dk, fs_nm_cara_bayar_dk);
        var grupJaminan = new GroupJaminanReff(fs_kd_grup_jaminan, fs_nm_grup_jaminan);
        var alamat = new AlamatType([fs_alm1_jaminan, fs_alm2_jaminan], fs_kota_jaminan, fs_kd_pos);
        
        var tipeTarif = new JaminanTipeTarifType(
            new TipeTarifReff(fs_kd_tipe_tarif_rawat_jalan, fs_nm_tarif_tipe_rawat_jalan),
            new TipeTarifReff(fs_kd_tipe_tarif_rawat_inap, fs_nm_tarif_tipe_rawat_inap));

        var tipeBarang = new JaminanTipeBarangTipe(
            new JmnTipeBrgType(fs_kd_tipe_brg_rawat_jalan, fs_nm_tipe_brg_rawat_jalan),
            new JmnTipeBrgType(fs_kd_tipe_brg_rawat_inap, fs_nm_tipe_brg_rawat_inap));
        
        var rekening = new JaminanRekeningType(
            new CoaType(fs_piut_rawat, fs_nm_piut_rawat),
            new CoaType(fs_piut_obat_rawat, fs_nm_piut_obat_rawat),
            new CoaType(fs_kd_rek_ppdp_jasa_ri, fs_nm_rek_ppdp_jasa_ri),
            new CoaType(fs_kd_rek_ppdp_obat_ri, fs_nm_rek_ppdp_obat_ri));
        
        var jaminan = new JaminanType(fs_kd_jaminan, fs_nm_jaminan, fb_aktif,
            alamat, caraBayarDk, grupJaminan, tipeTarif, rekening, tipeBarang);

        return jaminan;
    }
}