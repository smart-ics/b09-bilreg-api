using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienModelTest
{
    private static PasienModel CreateSamplePasien(string pasienId = "P00000001")
    {
        return new PasienModel(
            pasienId: pasienId,
            person: PersonInfoType.Default,
            nickName: "TestNick",
            tempatLahir: "Jakarta",
            golDarah: GolDarahType.Default,
            namaIbuKandung: "Ibu Kandung",
            ktp: KtpType.Default,
            kelurahan: KelurahanType.Default,
            kartuKeluarga: IdentitasType.Default,
            listContact: new List<ContactType>(),
            keluarga: PasienKeluargaType.Default,
            agama: AgamaType.Default,
            suku: SukuType.Default,
            statusKawinDk: StatusKawinDkType.Default,
            pendidikanDk: PendidikanDkType.Default,
            pekerjaanDk: PekerjaanDkType.Default,
            tglMedRec: new DateTime(2020, 1, 1),
            isAktif: true
        );
    }

    [Fact]
    public void GivenDefaultPasien_WhenCreated_ThenPasienSasetIsNotNullAndDefault()
    {
        // Act
        var sut = PasienModel.Default;

        // Assert
        sut.PasienSaset.Should().NotBeNull();
        sut.PasienSaset.PasienId.Should().Be("-");
        sut.PasienSaset.IsApprovedUpload.Should().BeFalse();
        sut.PasienSaset.TglJamApprovedUpload.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void GivenNewPasien_WhenConstructed_ThenPasienSasetInitializedWithPasienId()
    {
        // Act
        var sut = CreateSamplePasien("00000001");

        // Assert
        sut.PasienSaset.Should().NotBeNull();
        sut.PasienSaset.PasienId.Should().Be("00000001");
        sut.PasienSaset.IsApprovedUpload.Should().BeFalse();
        sut.PasienSaset.TglJamApprovedUpload.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void GivenPasien_WhenSetSasetCalledWithValidModel_ThenPasienSasetUpdated()
    {
        // Arrange
        var sut = CreateSamplePasien("00000001");
        var approvedTime = new DateTime(2026, 10, 7, 10, 30, 0);
        var sasetModel = new PasienSasetModel(
            "00000001", "SAT-001", true, approvedTime, "general_consent.pdf",
            true, approvedTime, "view_consent.pdf"
        );

        // Act
        sut.SetSaset(sasetModel);

        // Assert
        sut.PasienSaset.Should().Be(sasetModel);
        sut.PasienSaset.SasetId.Should().Be("SAT-001");
        sut.PasienSaset.IsApprovedUpload.Should().BeTrue();
        sut.PasienSaset.TglJamApprovedUpload.Should().Be(approvedTime);
    }

    [Fact]
    public void GivenPasien_WhenSetSasetCalledWithNull_ThenFallsBackToDefaultWithPasienId()
    {
        // Arrange
        var sut = CreateSamplePasien("00000001");

        // Act
        sut.SetSaset(null!);

        // Assert
        sut.PasienSaset.Should().NotBeNull();
        sut.PasienSaset.PasienId.Should().Be("00000001");
        sut.PasienSaset.IsApprovedUpload.Should().BeFalse();
        sut.PasienSaset.TglJamApprovedUpload.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void GivenPasien_WhenApproveUploadSasetCalled_ThenPasienSasetIsApprovedUploadTrue()
    {
        // Arrange
        var sut = CreateSamplePasien("00000001");
        var approvedAt = new DateTime(2026, 10, 7, 15, 0, 0);

        // Act
        sut.ApproveUploadSaset(approvedAt);

        // Assert
        sut.PasienSaset.Should().NotBeNull();
        sut.PasienSaset.IsApprovedUpload.Should().BeTrue();
        sut.PasienSaset.TglJamApprovedUpload.Should().Be(approvedAt);
        sut.PasienSaset.PasienId.Should().Be("00000001");
    }

    [Fact]
    public void GivenPasienId_WhenGetNomorMedrec_ThenFormattedWithHyphens()
    {
        // Arrange
        var sut = CreateSamplePasien("00123456");

        // Act
        var result = sut.GetNomorMedrec();

        // Assert
        result.Should().Be("00-12-34-56");
    }

    [Fact]
    public void GivenActivePasien_WhenNonActiveAndReActiveCalled_ThenIsAktifUpdated()
    {
        // Arrange
        var sut = CreateSamplePasien("00000001");
        sut.IsAktif.Should().BeTrue();

        // Act & Assert
        sut.NonActive();
        sut.IsAktif.Should().BeFalse();

        sut.ReActive();
        sut.IsAktif.Should().BeTrue();
    }
}
