using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record Icd10Dto(string fs_kd_icd, string fs_ket_icd)
{
    public static Icd10Dto FromModel(Icd10Type model)
        => new(model.Icd10Id, model.Icd10Name);

    public Icd10Type ToModel()
        => new Icd10Type(fs_kd_icd, fs_ket_icd);
}
