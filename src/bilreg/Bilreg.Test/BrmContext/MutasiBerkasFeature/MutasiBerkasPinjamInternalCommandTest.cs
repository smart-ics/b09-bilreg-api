using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasPinjamInternalCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();
    private readonly Mock<IMutasiBerkasIdGenerator> _idGeneratorMock = new();

    private MutasiBerkasPinjamInternalHandler CreateSut() =>
        new(_mutasiRepoMock.Object, _idGeneratorMock.Object);

    private static JadwalPelayananType CreateSampleJadwal() =>
        new("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateExistingMutasi(
        string id,
        MutasiBerkasStatusEnum status,
        bool isVoid = false)
    {
        var model = MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: id,
            trsBuatId: "RG2610170001",
            pasienId: "00-12-34-56",
            tglMutasi: new DateOnly(2026, 10, 17),
            jamMutasi: new TimeOnly(8, 0, 0),
            petugasId: "USR01",
            jadwalPelayanan: CreateSampleJadwal(),
            volume: VolumeBerkasType.CreatePinjam(1, 0));

        if (status == MutasiBerkasStatusEnum.Received)
        {
            model.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 15, 0));
        }
        else if (status == MutasiBerkasStatusEnum.Returned)
        {
            model.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0), 1, 0);
        }
        else if (status == MutasiBerkasStatusEnum.Missing)
        {
            model.TandaiHilang("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(8, 30, 0));
        }

        if (isVoid)
        {
            model.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(9, 30, 0), "Koreksi void");
        }

        return model;
    }

    private static MutasiBerkasPinjamInternalCommand CreateValidCommand() =>
        new(
            MrId: "00-12-34-56",
            UnitPeminjamId: "CASEMIX",
            PetugasPeminjam: "PET_CASEMIX",
            PetugasKirim: "PET_FILING",
            Keperluan: "Audit Kelengkapan Klaim BPJS",
            TglPinjam: new DateOnly(2026, 10, 21),
            JamPinjam: new TimeOnly(8, 30, 0)
        );

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenValidCommand_ReturnsResponse_AndPersistsDispatchedMutasi()
    {
        // Arrange
        var cmd = CreateValidCommand();
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr(cmd.MrId))
            .Returns(MayBe<MutasiBerkasModel>.None);
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100001");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100001");
        result.TrsBuatId.Should().Be("PINJAM-20261021083000");
        result.MrId.Should().Be("00-12-34-56");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Dispatched);
        result.TotalPinjam.Should().Be(1m);
        result.UnitPeminjamId.Should().Be("CASEMIX");
        result.PetugasPeminjam.Should().Be("PET_CASEMIX");
        result.PetugasKirim.Should().Be("PET_FILING");
        result.Keperluan.Should().Be("Audit Kelengkapan Klaim BPJS");

        _idGeneratorMock.Verify(x => x.Generate(MutasiBerkasStatusEnum.Dispatched), Times.Once);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100001" &&
            m.TrsBuatId == "PINJAM-20261021083000" &&
            m.PasienId == "00-12-34-56" &&
            m.Status == MutasiBerkasStatusEnum.Dispatched &&
            m.PetugasId == "PET_FILING" &&
            m.Volume.QtyPinjamRi == 0m &&
            m.Volume.QtyPinjamRj == 1m &&
            m.Volume.TotalPinjam == 1m &&
            m.JadwalPelayanan.LayananId == "CASEMIX" &&
            m.JadwalPelayanan.DokterId == "PET_CASEMIX" &&
            m.JadwalPelayanan.TglPraktek == new DateOnly(2026, 10, 21) &&
            m.JadwalPelayanan.JamPraktek == "08:30"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPreviousMutasiWasReturned_SuccessfullyDispatchesInternalLoan()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var existingReturned = CreateExistingMutasi("MM26100001", MutasiBerkasStatusEnum.Returned);
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr(cmd.MrId))
            .Returns(MayBe.From(existingReturned));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100002");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100002");
        result.TotalPinjam.Should().Be(1m);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100002" &&
            m.Status == MutasiBerkasStatusEnum.Dispatched
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPreviousMutasiWasMissing_SuccessfullyDispatchesInternalLoan()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var existingMissing = CreateExistingMutasi("MH26100001", MutasiBerkasStatusEnum.Missing);
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr(cmd.MrId))
            .Returns(MayBe.From(existingMissing));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100003");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100003");
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100003" &&
            m.Status == MutasiBerkasStatusEnum.Dispatched
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPreviousMutasiWasVoided_SuccessfullyDispatchesInternalLoan()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var existingVoided = CreateExistingMutasi("MK26100001", MutasiBerkasStatusEnum.Dispatched, isVoid: true);
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr(cmd.MrId))
            .Returns(MayBe.From(existingVoided));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100004");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100004");
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoTglOrJamProvided_DefaultsToCurrentDateTime()
    {
        // Arrange
        var cmd = new MutasiBerkasPinjamInternalCommand(
            MrId: "00-12-34-56",
            UnitPeminjamId: "AUDIT",
            PetugasPeminjam: "PET_AUDIT",
            PetugasKirim: "PET_FILING",
            Keperluan: "Audit Medis Tahunan"
        );
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr(cmd.MrId))
            .Returns(MayBe<MutasiBerkasModel>.None);
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100005");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100005");
        result.TrsBuatId.Should().StartWith("PINJAM-");
        result.TotalPinjam.Should().Be(1m);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100005" &&
            m.Status == MutasiBerkasStatusEnum.Dispatched &&
            m.JadwalPelayanan.LayananId == "AUDIT" &&
            m.JadwalPelayanan.DokterId == "PET_AUDIT"
        )), Times.Once);
    }

    [Fact]
    public void Constructor_WithStringDates_ParsesCorrectly()
    {
        var cmd = new MutasiBerkasPinjamInternalCommand(
            "00-12-34-56", "CASEMIX", "PET_CASEMIX", "PET_FILING", "Keperluan klaim", "2026-10-21", "09:15:00");

        cmd.TglPinjam.Should().Be(new DateOnly(2026, 10, 21));
        cmd.JamPinjam.Should().Be(new TimeOnly(9, 15, 0));
    }
    #endregion

    #region DOUBLE-DISPATCH GUARD REJECTION
    [Fact]
    public async Task Handle_WhenLatestActiveIsDispatched_ThrowsInvalidOperationException()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var existingDispatched = CreateExistingMutasi("MK26100001", MutasiBerkasStatusEnum.Dispatched);
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr(cmd.MrId))
            .Returns(MayBe.From(existingDispatched));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Berkas rekam medis sedang beredar aktif di unit lain dan belum kembali.");

        _idGeneratorMock.Verify(x => x.Generate(It.IsAny<MutasiBerkasStatusEnum>()), Times.Never);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLatestActiveIsReceived_ThrowsInvalidOperationException()
    {
        // Arrange
        var cmd = CreateValidCommand();
        var existingReceived = CreateExistingMutasi("MP26100001", MutasiBerkasStatusEnum.Received);
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr(cmd.MrId))
            .Returns(MayBe.From(existingReceived));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Berkas rekam medis sedang beredar aktif di unit lain dan belum kembali.");

        _idGeneratorMock.Verify(x => x.Generate(It.IsAny<MutasiBerkasStatusEnum>()), Times.Never);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Never);
    }
    #endregion

    #region INPUT VALIDATION GUARDS
    [Fact]
    public async Task Handle_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = async () => await sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenMrIdIsWhitespace_ThrowsArgumentException(string? invalidMrId)
    {
        var cmd = CreateValidCommand() with { MrId = invalidMrId! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenUnitPeminjamIdIsWhitespace_ThrowsArgumentException(string? invalidUnit)
    {
        var cmd = CreateValidCommand() with { UnitPeminjamId = invalidUnit! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenPetugasPeminjamIsWhitespace_ThrowsArgumentException(string? invalidPetugas)
    {
        var cmd = CreateValidCommand() with { PetugasPeminjam = invalidPetugas! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenPetugasKirimIsWhitespace_ThrowsArgumentException(string? invalidPetugas)
    {
        var cmd = CreateValidCommand() with { PetugasKirim = invalidPetugas! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenKeperluanIsWhitespace_ThrowsArgumentException(string? invalidKeperluan)
    {
        var cmd = CreateValidCommand() with { Keperluan = invalidKeperluan! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    [InlineData("abcd")]
    [InlineData("  ab ")]
    public async Task Handle_WhenKeperluanIsLessThan5Chars_ThrowsArgumentException(string shortKeperluan)
    {
        var cmd = CreateValidCommand() with { Keperluan = shortKeperluan };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*minimal 5 karakter*");
    }
    #endregion
}
