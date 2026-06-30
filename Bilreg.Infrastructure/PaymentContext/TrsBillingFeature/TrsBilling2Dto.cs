using System.Globalization;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

//  resharper disable InconsistentNaming

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
    public static TaTrsBilling2Dto FromModelTrans(TrsBill2TransEventType model, string billingId, int modul)
    {
        var komponenTarifId = modul == 0 ? model.Komponen.BillKompId : string.Empty;
        var komponenTarifName = modul == 0 ? model.Komponen.BillKompName : string.Empty;
        var groupRekId = modul == 1 ? model.Komponen.BillKompId : string.Empty;
        var groupRekName = modul == 1 ? model.Komponen.BillKompName : string.Empty;
        
        var result = new TaTrsBilling2Dto(
            billingId, model.NoUrut,
            model.JenisBayar.JenisBayarId, model.Nilai, 0,
            //  payment
            billingId, "3000-01-01", "00:00:00",
            //  kasir
            string.Empty, 
            //  jasa-only props
            model.PetugasMedis.PpaId, komponenTarifId, 
            //  obat-only props
            groupRekId, 
            //  rekening
            model.Coa.Ppdp.CoaId, model.Coa.Pdpt.CoaId, model.Coa.Pdpt.CoaId,
            "", "", "", "", 
            //  support      
            komponenTarifName, groupRekName, "", model.PetugasMedis.PpaName);
        return result;
    }
    
    public static TaTrsBilling2Dto FromModelFinalization(TrsBill2FinalizationEventType model, 
        string billingId, int modul, string regId)
    {
        var komponenTarifId = modul == 0 ? model.Komponen.BillKompId : string.Empty;
        var komponenTarifName = modul == 0 ? model.Komponen.BillKompName : string.Empty;
        var groupRekId = modul == 1 ? model.Komponen.BillKompId : string.Empty;
        var groupRekName = modul == 1 ? model.Komponen.BillKompName : string.Empty;
        var tglBayar = model.TglBayar.ToString("yyyy-mm-dd");
        var jamBayar = model.TglBayar.ToString("HH:mm:ss");
        var paymentId = $"RO{regId[^8..]}";
        
        var result = new TaTrsBilling2Dto(
            billingId, model.NoUrut,
            model.JenisBayar.JenisBayarId, 0, model.Nilai,
            //  payment
            paymentId, tglBayar, jamBayar,
            //  kasir
            model.PetugasKasir, 
            //  jasa-only props
            model.PetugasMedis.PpaId, komponenTarifId, 
            //  obat-only props
            groupRekId, 
            //  rekening
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, 
            //  support      
            komponenTarifName, groupRekName, model.PetugasKasir, model.PetugasMedis.PpaName);
        return result;
    }
    
    public static (TaTrsBilling2Dto,TaTrsBilling2Dto)  FromModelPayment(TrsBill2PaymentEventType model, 
        string billingId, int modul, string paymentId)
    {
        var komponenTarifId = modul == 0 ? model.Komponen.BillKompId : string.Empty;
        var komponenTarifName = modul == 0 ? model.Komponen.BillKompName : string.Empty;
        var groupRekId = modul == 1 ? model.Komponen.BillKompId : string.Empty;
        var groupRekName = modul == 1 ? model.Komponen.BillKompName : string.Empty;
        var tglBayar = model.TglBayar.ToString("yyyy-mm-dd");
        var jamBayar = model.TglBayar.ToString("HH:mm:ss");
        
        var resultP = new TaTrsBilling2Dto(
            billingId, model.NoUrut,
            model.JenisBayar.JenisBayarId, 0, model.Nilai, 
            //  payment
            paymentId, tglBayar, jamBayar,
            //  kasir
            string.Empty, 
            //  jasa-only props
            model.PetugasMedis.PpaId, komponenTarifId, 
            //  obat-only props
            groupRekId, 
            //  rekening
            string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, 
            //  support      
            komponenTarifName, groupRekName, string.Empty, model.PetugasMedis.PpaName);

        var resultN = resultP with
        {
            fn_trs_p = 0,
            fn_trs_n = model.Nilai,
            fs_kd_jenis_bayar = "KAS"
        };
        
        return (resultP, resultN);
    }

    public ITrsBill2Event ToModel(int modul)
    {
        if (fs_kd_trs == fs_kd_trs_bayar)
            return ToTransEventModel(modul);
        if (fs_kd_trs_bayar[..2] == "RO")
            return ToFinalizationEventModel(modul);
        return ToPaymentEventModel(modul);
    }

    private TrsBill2TransEventType ToTransEventModel(int modul)
    {
        var komponen = modul == 0 ? 
            new TrsBill2KomponenType(fs_kd_detil_tarif,"") : 
            new TrsBill2KomponenType(fs_kd_grup_rek, "");
        var jenisBayar = TrsBillJenisBayarType.GetData(fs_kd_jenis_bayar);
        var ppa = new PpaReff(fs_kd_petugas_medis, "");
        var coa = new TrsBill2CoaType(
            new CoaType(fs_kd_rek_ppdp, ""),
            new CoaType(fs_kd_rek_pdpt, ""),
            new CoaType(fs_kd_rek_persediaan, ""),
            new CoaType(fs_kd_rek_pdpt_lain, ""),
            new CoaType(fs_kd_rek_tax, ""),
            new CoaType(fs_kd_rek_disc, ""));
        var result = new TrsBill2TransEventType(
            (int)fn_no_urut, komponen, jenisBayar, fn_trs_p, ppa, coa);
        return result;
    }

    private TrsBill2FinalizationEventType ToFinalizationEventModel(int modul)
    {
        var komponen = modul == 0 ?
            new TrsBill2KomponenType(fs_kd_detil_tarif, "") :
            new TrsBill2KomponenType(fs_kd_grup_rek, "");
        var jenisBayar = TrsBillJenisBayarType.GetData(fs_kd_jenis_bayar);
        var ppa = new PpaReff(fs_kd_petugas_medis, "");
        var tglBayar = DateTime.ParseExact(fd_tgl_bayar, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var result = new TrsBill2FinalizationEventType(
            (int)fn_no_urut, komponen, jenisBayar, fn_trs_n, ppa, fs_kd_petugas_kasir,
            fs_kd_trs_bayar, tglBayar);
        return result;
    }

    private TrsBill2PaymentEventType ToPaymentEventModel(int modul)
    {
        if (fs_kd_jenis_bayar == "KAS")
            throw new ArgumentException("JenisBayar must be KAS", nameof(fs_kd_jenis_bayar));

        var komponen = modul == 0 ? 
            new TrsBill2KomponenType(fs_kd_detil_tarif,"") : 
            new TrsBill2KomponenType(fs_kd_grup_rek, "");
        var jenisBayar = TrsBillJenisBayarType.GetData(fs_kd_jenis_bayar);
        var ppa = new PpaReff(fs_kd_petugas_medis, "");
        var tglBayar = DateTime.ParseExact(fd_tgl_bayar, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var payment = new PaymentType(fs_kd_trs_bayar, "", true);
        var result = new TrsBill2PaymentEventType(
            (int)fn_no_urut, komponen, jenisBayar, payment, fn_trs_p, tglBayar, ppa);
        return result;
    }
}