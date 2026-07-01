using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

// resharper disable inconsistentnaming
public record TipeRekDto(string fs_kd_rek_tipe, string fs_nm_rek_tipe)
{
    public static TipeRekDto FromModel(TipeRekType model)
    {
        var result = new TipeRekDto(model.TipeRekId, model.TipeRekName);
        return result;
    }

    public TipeRekType ToModel()
    {
        var result = new TipeRekType(fs_kd_rek_tipe, fs_nm_rek_tipe);
        return result;
    }
}