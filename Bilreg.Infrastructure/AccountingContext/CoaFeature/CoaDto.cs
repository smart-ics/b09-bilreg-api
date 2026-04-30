using Bilreg.Domain.AccountingContext.CoaFeature;

namespace Bilreg.Infrastructure.AccountingContext.CoaFeature;

public record CoaDto(string fs_kd_rek, string fs_nm_rek, 
    string fs_kd_rek_tipe, string fs_nm_rek_tipe)
{
    public static CoaDto FromModel(CoaType model)
    {
        return new CoaDto(
            model.CoaId,
            model.CoaName,
            model.CoaTipeType.CoaTipeId,
            model.CoaTipeType.CoaTipeName
        );
    }
    public CoaType ToModel()
    {
        var coaTipe = new CoaTipeType(fs_kd_rek_tipe, fs_nm_rek_tipe);
        return new CoaType(fs_kd_rek, fs_nm_rek, coaTipe);
    }
}
