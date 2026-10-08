using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PasienContext.PasienFeature;

public class PasienApproveUploadSasetHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();

    private PasienApproveUploadSasetHandler CreateHandler() =>
        new(_pasienRepoMock.Object);

    private static PasienModel CreateSamplePasien(string pasienId = "00100123456")
    {
        return new PasienModel(
            pasienId: pasienId,
            person: new PersonInfoType(
                "Budi Santoso",
                new DateOnly(1990, 5, 20),
                "L",
                AlamatType.Default,
                new ContactType(JenisContactEnum.Mobile, "081234567890"),
                IdentitasType.Default
            ),
            nickName: "Budi",
            tempatLahir: "Surabaya",
            golDarah: GolDarahType.Default,
            namaIbuKandung: "Siti Aminah",
            ktp: KtpType.Default,
            kelurahan: KelurahanType.Default,
            kartuKeluarga: IdentitasType.Default,
            listContact: new List<ContactType>
            {
                new(JenisContactEnum.Email, "budi@example.com"),
                new(JenisContactEnum.Mobile, "081234567890")
            },
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
    public async Task UT01_GivenValidRequest_WhenHandle_ThenCallsApproveUploadSasetAndSavesChanges()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var cmd = new PasienApproveUploadSasetCmd(pasienId);

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        pasien.PasienSaset.Should().NotBeNull();
        pasien.PasienSaset.IsApprovedUpload.Should().BeTrue();
        pasien.PasienSaset.TglJamApprovedUpload.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        _pasienRepoMock.Verify(x => x.SaveChanges(pasien), Times.Once);
    }

    [Fact]
    public async Task UT02_GivenPatientWithExistingSaset_WhenHandle_ThenUpdatesExistingSasetApproval()
    {
        // Arrange
        const string pasienId = "00100123456";
        var pasien = CreateSamplePasien(pasienId);
        var existingSaset = new PasienSasetModel(
            pasienId,
            "SAT-12345",
            false,
            new DateTime(3000, 1, 1),
            "-",
            false,
            new DateTime(3000, 1, 1),
            "-"
        );
        pasien.SetSaset(existingSaset);

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == pasienId)))
            .Returns(MayBe.From(pasien));

        var handler = CreateHandler();
        var cmd = new PasienApproveUploadSasetCmd(pasienId);

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        pasien.PasienSaset.SasetId.Should().Be("SAT-12345");
        pasien.PasienSaset.IsApprovedUpload.Should().BeTrue();
        pasien.PasienSaset.TglJamApprovedUpload.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
        _pasienRepoMock.Verify(x => x.SaveChanges(pasien), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task UT03_GivenEmptyPasienId_WhenHandle_ThenThrowsArgumentException(string? invalidId)
    {
        // Arrange
        var handler = CreateHandler();
        var cmd = new PasienApproveUploadSasetCmd(invalidId!);

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _pasienRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienModel>()), Times.Never);
    }

    [Fact]
    public async Task UT04_GivenPasienNotFound_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        const string pasienId = "00100123456";
        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe<PasienModel>.None);

        var handler = CreateHandler();
        var cmd = new PasienApproveUploadSasetCmd(pasienId);

        // Act
        var act = async () => await handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*Pasien {pasienId} not found*");
        _pasienRepoMock.Verify(x => x.SaveChanges(It.IsAny<PasienModel>()), Times.Never);
    }
}
