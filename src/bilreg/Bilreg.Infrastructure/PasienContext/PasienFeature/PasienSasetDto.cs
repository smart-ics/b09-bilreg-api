using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public record PasienSasetDto(
    string KodeMr,
    string KodeSaset,
    bool IsApprovedUpload,
    DateTime TglJamApprovedUpload,
    string FileGeneralConcentUpload,
    bool IsApprovedView,
    DateTime TglJamApprovedView,
    string FileGeneralConcentView)
{
    public static PasienSasetDto FromModel(PasienSasetModel model) =>
        new(
            model.PasienId,
            model.SasetId,
            model.IsApprovedUpload,
            model.TglJamApprovedUpload,
            model.FileGeneralConcentUpload,
            model.IsApprovedView,
            model.TglJamApprovedView,
            model.FileGeneralConcentView
        );

    public PasienSasetModel ToModel() =>
        new(
            KodeMr,
            KodeSaset,
            IsApprovedUpload,
            TglJamApprovedUpload,
            FileGeneralConcentUpload,
            IsApprovedView,
            TglJamApprovedView,
            FileGeneralConcentView
        );

    public static PasienSasetDto Default =>
        new("-", "-", false, new DateTime(3000, 1, 1), "-", false, new DateTime(3000, 1, 1), "-");
}
