using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienSasetModelTest
{
    [Fact]
    public void GivenValidArguments_WhenConstructed_ThenPropertiesSetCorrectly()
    {
        // Arrange
        var approvedUploadDate = new DateTime(2026, 10, 7, 10, 0, 0);
        var approvedViewDate = new DateTime(2026, 10, 7, 11, 0, 0);

        // Act
        var sut = new PasienSasetModel(
            pasienId: "P001",
            sasetId: "S-12345",
            isApprovedUpload: true,
            tglJamApprovedUpload: approvedUploadDate,
            fileGeneralConcentUpload: "consent_upload.pdf",
            isApprovedView: true,
            tglJamApprovedView: approvedViewDate,
            fileGeneralConcentView: "consent_view.pdf"
        );

        // Assert
        sut.PasienId.Should().Be("P001");
        sut.SasetId.Should().Be("S-12345");
        sut.IsApprovedUpload.Should().BeTrue();
        sut.TglJamApprovedUpload.Should().Be(approvedUploadDate);
        sut.FileGeneralConcentUpload.Should().Be("consent_upload.pdf");
        sut.IsApprovedView.Should().BeTrue();
        sut.TglJamApprovedView.Should().Be(approvedViewDate);
        sut.FileGeneralConcentView.Should().Be("consent_view.pdf");
    }

    [Fact]
    public void GivenNoArgument_WhenDefaultCalled_ThenReturnsSafeDefaultValues()
    {
        // Act
        var sut = PasienSasetModel.Default();

        // Assert
        sut.PasienId.Should().Be("-");
        sut.SasetId.Should().Be("-");
        sut.IsApprovedUpload.Should().BeFalse();
        sut.TglJamApprovedUpload.Should().Be(new DateTime(3000, 1, 1));
        sut.FileGeneralConcentUpload.Should().Be("-");
        sut.IsApprovedView.Should().BeFalse();
        sut.TglJamApprovedView.Should().Be(new DateTime(3000, 1, 1));
        sut.FileGeneralConcentView.Should().Be("-");
    }

    [Fact]
    public void GivenPasienId_WhenDefaultCalled_ThenReturnsModelWithSpecifiedPasienId()
    {
        // Act
        var sut = PasienSasetModel.Default("P002");

        // Assert
        sut.PasienId.Should().Be("P002");
        sut.SasetId.Should().Be("-");
        sut.IsApprovedUpload.Should().BeFalse();
        sut.TglJamApprovedUpload.Should().Be(new DateTime(3000, 1, 1));
        sut.FileGeneralConcentUpload.Should().Be("-");
        sut.IsApprovedView.Should().BeFalse();
        sut.TglJamApprovedView.Should().Be(new DateTime(3000, 1, 1));
        sut.FileGeneralConcentView.Should().Be("-");
    }

    [Fact]
    public void GivenDefaultModel_WhenApproveUploadCalled_ThenIsApprovedUploadTrueAndDateUpdated()
    {
        // Arrange
        var sut = PasienSasetModel.Default("P003");
        var approvalTimestamp = new DateTime(2026, 10, 7, 14, 30, 0);

        // Act
        sut.ApproveUpload(approvalTimestamp);

        // Assert
        sut.IsApprovedUpload.Should().BeTrue();
        sut.TglJamApprovedUpload.Should().Be(approvalTimestamp);
        sut.PasienId.Should().Be("P003");
        sut.SasetId.Should().Be("-");
        sut.FileGeneralConcentUpload.Should().Be("-");
        sut.IsApprovedView.Should().BeFalse();
        sut.TglJamApprovedView.Should().Be(new DateTime(3000, 1, 1));
        sut.FileGeneralConcentView.Should().Be("-");
    }
}
