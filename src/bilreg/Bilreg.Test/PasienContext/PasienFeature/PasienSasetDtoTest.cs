using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienSasetDtoTest
{
    [Fact]
    public void GivenModel_WhenFromModel_ThenDtoMatchesModel()
    {
        var model = new PasienSasetModel(
            pasienId: "P001",
            sasetId: "SAS-001",
            isApprovedUpload: true,
            tglJamApprovedUpload: new DateTime(2026, 10, 7, 10, 0, 0),
            fileGeneralConcentUpload: "upload.pdf",
            isApprovedView: false,
            tglJamApprovedView: new DateTime(3000, 1, 1),
            fileGeneralConcentView: "-"
        );

        var dto = PasienSasetDto.FromModel(model);

        dto.KodeMr.Should().Be("P001");
        dto.KodeSaset.Should().Be("SAS-001");
        dto.IsApprovedUpload.Should().BeTrue();
        dto.TglJamApprovedUpload.Should().Be(new DateTime(2026, 10, 7, 10, 0, 0));
        dto.FileGeneralConcentUpload.Should().Be("upload.pdf");
        dto.IsApprovedView.Should().BeFalse();
        dto.TglJamApprovedView.Should().Be(new DateTime(3000, 1, 1));
        dto.FileGeneralConcentView.Should().Be("-");
    }

    [Fact]
    public void GivenDto_WhenToModel_ThenModelMatchesDto()
    {
        var dto = new PasienSasetDto(
            KodeMr: "P001",
            KodeSaset: "SAS-001",
            IsApprovedUpload: true,
            TglJamApprovedUpload: new DateTime(2026, 10, 7, 10, 0, 0),
            FileGeneralConcentUpload: "upload.pdf",
            IsApprovedView: false,
            TglJamApprovedView: new DateTime(3000, 1, 1),
            FileGeneralConcentView: "-"
        );

        var model = dto.ToModel();

        model.PasienId.Should().Be("P001");
        model.SasetId.Should().Be("SAS-001");
        model.IsApprovedUpload.Should().BeTrue();
        model.TglJamApprovedUpload.Should().Be(new DateTime(2026, 10, 7, 10, 0, 0));
        model.FileGeneralConcentUpload.Should().Be("upload.pdf");
        model.IsApprovedView.Should().BeFalse();
        model.TglJamApprovedView.Should().Be(new DateTime(3000, 1, 1));
        model.FileGeneralConcentView.Should().Be("-");
    }
}
