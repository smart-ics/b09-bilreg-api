using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public record PasienIdDto(string fs_mr, string JenisID, string NoID)
{
    public static PasienIdDto FromModel(PasienModel model)
    {
        var ktp = model.Ktp;
        var result = new PasienIdDto(model.PasienId, "KTP", ktp.Nik);
        return result;
    }
}
