using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class PolisUpdateHandlerTest
{
    private readonly Mock<IPolisRepo> _polisRepoMock = new();
    private readonly Mock<IPasienRepo> _pasienRepoMock = new();
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();

    private PolisUpdateHandler CreateSut() =>
        new(
            _polisRepoMock.Object,
            _pasienRepoMock.Object,
            _kelasRepoMock.Object
        );

    private static PasienModel SamplePasien(string id) => (PasienModel)PasienModel.Key(id);
    private static KelasType SampleKelas(string id, string name) =>
        new(id, name, true, KelasDkType.Default);

    private static PolisModel CreateSamplePolis(string polisId, PasienModel primaryPatient, params (PasienModel Pasien, string StatusCode)[] additionalCovers)
    {
        var coverList = new List<PolisCoverModel>
        {
            new(polisId, primaryPatient.ToReff(), StatusPesertaType.Create("P"))
        };
        foreach (var (p, s) in additionalCovers)
        {
            coverList.Add(new(polisId, p.ToReff(), StatusPesertaType.Create(s)));
        }

        return new PolisModel(
            polisId,
            "POLIS-ORIGINAL",
            "Atas Nama Original",
            new TipeJaminanReff("TJ01", "BPJS Kesehatan"),
            new KelasReff("KL-01", "Kelas 1"),
            new DateOnly(2026, 12, 31),
            false,
            coverList
        );
    }

    [Fact]
    public async Task Handle_ValidRequestForPrimaryParticipant_ShouldUpdatePolisAndSaveChanges()
    {
        // Arrange
        var pasien = SamplePasien("PAS001");
        var polis = CreateSamplePolis("POL001", pasien);
        var kelas = SampleKelas("KL-02", "Kelas VIP");
        var sut = CreateSut();

        var command = new PolisUpdateCommand(
            PolisId: "POL001",
            PasienId: "PAS001",
            NoPolis: "POLIS-UPDATED-001",
            AtasName: "Budi Santoso Updated",
            ExpiredDate: "2027-06-30",
            KelasRanapId: "KL-02",
            StatusPesertaId: "P",
            IsCoverRajal: true
        );

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPolisKey>(k => k.PolisId == "POL001")))
            .Returns(MayBe.From(polis));

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPasienKey>(k => k.PasienId == "PAS001")))
            .Returns(MayBe.From(pasien));

        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "KL-02")))
            .Returns(MayBe.From(kelas));

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PolisId.Should().Be("POL001");

        polis.NoPolis.Should().Be("POLIS-UPDATED-001");
        polis.AtasNama.Should().Be("Budi Santoso Updated");
        polis.ExpiredDate.Should().Be(new DateOnly(2027, 6, 30));
        polis.Kelas.KelasId.Should().Be("KL-02");
        polis.Kelas.KelasName.Should().Be("Kelas VIP");
        polis.IsCoverRajal.Should().BeTrue();
        polis.TipeJaminan.TipeJaminanId.Should().Be("TJ01");

        _polisRepoMock.Verify(x => x.SaveChanges(polis), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequestForDependentParticipant_ShouldUpdateParticipantStatusAndPolis()
    {
        // Arrange
        var primary = SamplePasien("PAS-P");
        var dependent = SamplePasien("PAS-S");
        var polis = CreateSamplePolis("POL001", primary, (dependent, "S"));
        var kelas = SampleKelas("KL-01", "Kelas 1");
        var sut = CreateSut();

        var command = new PolisUpdateCommand(
            PolisId: "POL001",
            PasienId: "PAS-S",
            NoPolis: "POLIS-UPDATED-002",
            AtasName: "Atas Nama Baru",
            ExpiredDate: "2028-01-01",
            KelasRanapId: "KL-01",
            StatusPesertaId: "A",
            IsCoverRajal: false
        );

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPolisKey>()))
            .Returns(MayBe.From(polis));

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe.From(dependent));

        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe.From(kelas));

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.PolisId.Should().Be("POL001");
        var updatedCover = polis.ListCover.First(c => c.Pasien.PasienId == "PAS-S");
        updatedCover.Status.StatusCode.Should().Be("A");
        _polisRepoMock.Verify(x => x.SaveChanges(polis), Times.Once);
    }

    [Fact]
    public async Task Handle_PolisNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL999", "PAS001", "NP01", "AN", "2027-01-01", "KL01", "P", false);

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPolisKey>()))
            .Returns(MayBe<PolisModel>.None);

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Polis POL999 not found*");
    }

    [Fact]
    public async Task Handle_PasienNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var polis = CreateSamplePolis("POL001", SamplePasien("PAS001"));
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL001", "PAS999", "NP01", "AN", "2027-01-01", "KL01", "P", false);

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPolisKey>()))
            .Returns(MayBe.From(polis));

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe<PasienModel>.None);

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Pasien PAS999 not found*");
    }

    [Fact]
    public async Task Handle_KelasNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var pasien = SamplePasien("PAS001");
        var polis = CreateSamplePolis("POL001", pasien);
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL001", "PAS001", "NP01", "AN", "2027-01-01", "KL-UNKNOWN", "P", false);

        _polisRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPolisKey>()))
            .Returns(MayBe.From(polis));

        _pasienRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe.From(pasien));

        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IKelasKey>()))
            .Returns(MayBe<KelasType>.None);

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Kelas KL-UNKNOWN not found*");
    }

    [Theory]
    [InlineData("", "PAS001", "NP01", "AN", "2027-01-01", "KL01", "P")]
    [InlineData("POL001", "", "NP01", "AN", "2027-01-01", "KL01", "P")]
    [InlineData("POL001", "PAS001", "", "AN", "2027-01-01", "KL01", "P")]
    [InlineData("POL001", "PAS001", "NP01", "", "2027-01-01", "KL01", "P")]
    [InlineData("POL001", "PAS001", "NP01", "AN", "", "KL01", "P")]
    [InlineData("POL001", "PAS001", "NP01", "AN", "2027-01-01", "", "P")]
    [InlineData("POL001", "PAS001", "NP01", "AN", "2027-01-01", "KL01", "")]
    public async Task Handle_EmptyOrWhitespaceRequiredFields_ShouldThrowArgumentException(
        string polisId, string pasienId, string noPolis, string atasName,
        string expiredDate, string kelasRanapId, string statusPesertaId)
    {
        // Arrange
        var sut = CreateSut();
        var command = new PolisUpdateCommand(polisId, pasienId, noPolis, atasName, expiredDate, kelasRanapId, statusPesertaId, false);

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_InvalidDateFormat_ShouldThrowFormatException()
    {
        // Arrange
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL001", "PAS001", "NP01", "AN", "31-12-2027", "KL01", "P", false);

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FormatException>();
    }

    [Fact]
    public async Task Handle_InvalidStatusPesertaCode_ShouldThrowArgumentException()
    {
        // Arrange
        var pasien = SamplePasien("PAS001");
        var polis = CreateSamplePolis("POL001", pasien);
        var kelas = SampleKelas("KL01", "Kelas 1");
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL001", "PAS001", "NP01", "AN", "2027-01-01", "KL01", "INVALID_CODE", false);

        _polisRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPolisKey>())).Returns(MayBe.From(polis));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(pasien));
        _kelasRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>())).Returns(MayBe.From(kelas));

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid status code*");
    }

    [Fact]
    public async Task Handle_PatientNotInCover_ShouldThrowArgumentException()
    {
        // Arrange
        var primary = SamplePasien("PAS-P");
        var outsider = SamplePasien("PAS-OUTSIDER");
        var polis = CreateSamplePolis("POL001", primary);
        var kelas = SampleKelas("KL01", "Kelas 1");
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL001", "PAS-OUTSIDER", "NP01", "AN", "2027-01-01", "KL01", "A", false);

        _polisRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPolisKey>())).Returns(MayBe.From(polis));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(outsider));
        _kelasRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>())).Returns(MayBe.From(kelas));

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Cover not found*");
    }

    [Fact]
    public async Task Handle_AttemptToChangePrimaryParticipantStatus_ShouldThrowArgumentException()
    {
        // Arrange
        var primary = SamplePasien("PAS-P");
        var polis = CreateSamplePolis("POL001", primary);
        var kelas = SampleKelas("KL01", "Kelas 1");
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL001", "PAS-P", "NP01", "AN", "2027-01-01", "KL01", "S", false);

        _polisRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPolisKey>())).Returns(MayBe.From(polis));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(primary));
        _kelasRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>())).Returns(MayBe.From(kelas));

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Status peserta utama tidak dapat diubah*");
    }

    [Fact]
    public async Task Handle_DuplicateStatusForNonPrimary_ShouldThrowArgumentException()
    {
        // Arrange
        var primary = SamplePasien("PAS-P");
        var dep1 = SamplePasien("PAS-S");
        var dep2 = SamplePasien("PAS-I");
        var polis = CreateSamplePolis("POL001", primary, (dep1, "S"), (dep2, "I"));
        var kelas = SampleKelas("KL01", "Kelas 1");
        var sut = CreateSut();
        var command = new PolisUpdateCommand("POL001", "PAS-S", "NP01", "AN", "2027-01-01", "KL01", "I", false);

        _polisRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPolisKey>())).Returns(MayBe.From(polis));
        _pasienRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(dep1));
        _kelasRepoMock.Setup(x => x.LoadEntity(It.IsAny<IKelasKey>())).Returns(MayBe.From(kelas));

        // Act
        var act = async () => await sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Status Kepesertaan already exist*");
    }
}
