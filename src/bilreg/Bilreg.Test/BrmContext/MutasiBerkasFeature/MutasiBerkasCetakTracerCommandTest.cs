using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasCetakTracerCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();

    private MutasiBerkasCetakTracerHandler CreateSut() =>
        new(_mutasiRepoMock.Object);

    private static JadwalPelayananType CreateSampleJadwal() =>
        new("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateSampleMutasi(
        string id = "MK26100001",
        bool isVoid = false)
    {
        var model = MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: id,
            trsBuatId: "RG2610170001",
            pasienId: "00-12-34-56",
            tglMutasi: new DateOnly(2026, 10, 17),
            jamMutasi: new TimeOnly(8, 0, 0),
            petugasId: "USR_FILING",
            jadwalPelayanan: CreateSampleJadwal(),
            volume: VolumeBerkasType.CreatePinjam(1, 1));

        if (isVoid)
        {
            model.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(8, 15, 0), "Salah cetak");
        }

        return model;
    }

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenValidCommand_IncrementsFrekuensi_AndReturnsResponse()
    {
        // Arrange
        var model = CreateSampleMutasi("MK26100001");
        model.CetakTracerInfo.FrekuensiCetak.Should().Be(0);

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasCetakTracerCommand(
            MutasiId: "MK26100001",
            PetugasCetak: "USR_CETAK",
            TglCetak: new DateOnly(2026, 10, 17),
            JamCetak: new TimeOnly(8, 30, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100001");
        result.FrekuensiCetak.Should().Be(1);
        result.PetugasCetak.Should().Be("USR_CETAK");
        result.TglCetak.Should().Be(new DateOnly(2026, 10, 17));
        result.JamCetak.Should().Be(new TimeOnly(8, 30, 0));

        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100001" &&
            m.CetakTracerInfo.FrekuensiCetak == 1 &&
            m.CetakTracerInfo.PetugasCetak == "USR_CETAK" &&
            m.CetakTracerInfo.TglCetak == new DateOnly(2026, 10, 17) &&
            m.CetakTracerInfo.JamCetak == new TimeOnly(8, 30, 0)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSubsequentPrint_IncrementsFrekuensiAgain()
    {
        // Arrange
        var model = CreateSampleMutasi("MK26100001");
        model.CetakTracer("USR_PREV", new DateOnly(2026, 10, 17), new TimeOnly(8, 10, 0));
        model.CetakTracerInfo.FrekuensiCetak.Should().Be(1);

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasCetakTracerCommand(
            MutasiId: "MK26100001",
            PetugasCetak: "USR_CETAK2");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.FrekuensiCetak.Should().Be(2);
        result.PetugasCetak.Should().Be("USR_CETAK2");

        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.CetakTracerInfo.FrekuensiCetak == 2 &&
            m.CetakTracerInfo.PetugasCetak == "USR_CETAK2"
        )), Times.Once);
    }

    [Fact]
    public void Constructor_WithStringTglAndJam_ParsesCorrectly()
    {
        var cmd = new MutasiBerkasCetakTracerCommand(
            "MK26100001", "USR_CETAK", "2026-10-17", "09:15:00");

        cmd.MutasiId.Should().Be("MK26100001");
        cmd.PetugasCetak.Should().Be("USR_CETAK");
        cmd.TglCetak.Should().Be(new DateOnly(2026, 10, 17));
        cmd.JamCetak.Should().Be(new TimeOnly(9, 15, 0));
    }
    #endregion

    #region REJECTION TESTS
    [Fact]
    public async Task Handle_WhenMutasiNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var cmd = new MutasiBerkasCetakTracerCommand("MK99999999", "USR_CETAK");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*MK99999999*tidak ditemukan*");

        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMutasiIsVoided_ThrowsInvalidOperationException()
    {
        // Arrange
        var voidedModel = CreateSampleMutasi("MK26100001", isVoid: true);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(voidedModel));

        var cmd = new MutasiBerkasCetakTracerCommand("MK26100001", "USR_CETAK");
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*dibatalkan*");

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
    public async Task Handle_WhenMutasiIdIsWhitespace_ThrowsArgumentException(string? invalidMutasiId)
    {
        var cmd = new MutasiBerkasCetakTracerCommand(invalidMutasiId!, "USR_CETAK");
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenPetugasCetakIsWhitespace_ThrowsArgumentException(string? invalidPetugas)
    {
        var cmd = new MutasiBerkasCetakTracerCommand("MK26100001", invalidPetugas!);
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion
}
