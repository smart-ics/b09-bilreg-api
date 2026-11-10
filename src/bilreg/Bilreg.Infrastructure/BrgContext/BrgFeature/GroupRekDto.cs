using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Infrastructure.BrgContext.BrgFeature;

public record GroupRekDto(
    string fs_kd_grup_rek, 
    string fs_nm_grup_rek, 
    string fs_kd_grup_rek_dk, 
    string fs_nm_grup_rek_dk,
    string fs_kd_rekap_cetak = "",
    string fs_kd_rek_jual = "",
    string fs_kd_rek_jual_rj = "",
    string fs_kd_rek_jual_disc = "",
    string fs_kd_rek_jual_tax = "",
    string fs_kd_rek_persediaan = "")
{
    public static GroupRekDto FromModel(GroupRekType model)
    {
        var result = new GroupRekDto(
            model.GroupRekId, 
            model.GroupRekName, 
            model.GroupRekDk.GroupRekDkId, 
            model.GroupRekDk.GroupRekDkName,
            model.RekapCetakId,
            model.RekJual.CoaId == "-" ? string.Empty : model.RekJual.CoaId,
            model.RekJualRajal.CoaId == "-" ? string.Empty : model.RekJualRajal.CoaId,
            model.RekDisc.CoaId == "-" ? string.Empty : model.RekDisc.CoaId,
            model.RekTax.CoaId == "-" ? string.Empty : model.RekTax.CoaId,
            model.RekPersediaan.CoaId == "-" ? string.Empty : model.RekPersediaan.CoaId);
        return result;
    }

    public GroupRekType ToModel()
    {
        var groupRekDk = new GroupRekDkType(fs_kd_grup_rek_dk, fs_nm_grup_rek_dk);
        var result = new GroupRekType(
            fs_kd_grup_rek, 
            fs_nm_grup_rek, 
            groupRekDk,
            fs_kd_rekap_cetak ?? string.Empty,
            new CoaType(fs_kd_rek_jual ?? string.Empty, string.Empty),
            new CoaType(fs_kd_rek_jual_rj ?? string.Empty, string.Empty),
            new CoaType(fs_kd_rek_jual_disc ?? string.Empty, string.Empty),
            new CoaType(fs_kd_rek_jual_tax ?? string.Empty, string.Empty),
            new CoaType(fs_kd_rek_persediaan ?? string.Empty, string.Empty));
        return result;
    }
}