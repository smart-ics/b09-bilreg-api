using Bilreg.Domain.BillContext.TindakanFeature;

namespace Bilreg.Infrastructure.BillContext.TindakanFeature;

// resharper disable inconsistentnaming
public record JenisTarifDto(string fs_kd_jenis_tarif, string fs_nm_jenis_tarif, string fs_urut)
{
    public static JenisTarifDto FromModel(JenisTarifType model)
    {
        var result = new JenisTarifDto(model.JenisTarifId, model.JenisTarifName, model.NoUrut.ToString());
        return result;
    }

    public JenisTarifType ToModel()
    {
        var result = new JenisTarifType(fs_kd_jenis_tarif, fs_nm_jenis_tarif, int.Parse(fs_urut));
        return result;
    }
}