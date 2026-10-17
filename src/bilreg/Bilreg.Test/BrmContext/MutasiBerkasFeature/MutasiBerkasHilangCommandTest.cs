using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasHilangCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();
    private readonly Mock<IMutasiBerkasIdGenerator> _idGeneratorMock = new();

    private MutasiBerkasHilangHandler CreateSut() =>
        new(_mutasiRepoMock.Object, _idGeneratorMock.Object);

    private static JadwalPelayananType CreateSampleJadwal() =>
        new("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateSampleMutasi(
        string id = "MK26100001",
        MutasiBerkasStatusEnum status = MutasiBerkasStatusEnum.Dispatched,
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

        if (status == MutasiBerkasStatusEnum.Received)
        {
            model.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 30, 0));
        }
        else if (status == MutasiBerkasStatusEnum.Returned)
        {
            model.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(12, 0, 0), 1, 1);
        }
        else if (status == MutasiBerkasStatusEnum.Missing)
        {
            model.TandaiHilang("USR_PETUGAS", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0));
        }

        if (isVoid)
        {
            model.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(8, 15, 0), "Dibatalkan");
        }

        return model;
    }

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenDispatched_TransitionsToMissing_GeneratesMHPrefix_AndReturnsResponse()
    {
        // Arrange
        var model = CreateSampleMutasi("MK26100001", MutasiBerkasStatusEnum.Dispatched);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Missing))
            .Returns("MH26100001");

        var cmd = new MutasiBerkasHilangCommand(
            MutasiId: "MK26100001",
            Petugas: "USR_PETUGAS",
            TglMutasi: new DateOnly(2026, 10, 17),
            JamMutasi: new TimeOnly(9, 30, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MH26100001");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Missing);
        result.TrsBuatId.Should().Be("RG2610170001");
        result.MrId.Should().Be("00-12-34-56");
        result.Petugas.Should().Be("USR_PETUGAS");

        _idGeneratorMock.Verify(x => x.Generate(MutasiBerkasStatusEnum.Missing), Times.Once);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MH26100001" &&
            m.Status == MutasiBerkasStatusEnum.Missing &&
            m.PetugasId == "USR_PETUGAS" &&
            m.TglMutasi == new DateOnly(2026, 10, 17) &&
            m.JamMutasi == new TimeOnly(9, 30, 0)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPermintaan_TransitionsToMissing_Successfully()
    {
        // Arrange
        var model = MutasiBerkasModel.CreatePermintaan(
            "MM00000000", "RG01", "00123456", new DateOnly(2026, 10, 17), new TimeOnly(8, 0), "USR01", CreateSampleJadwal());
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MM00000000")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Missing))
            .Returns("MH26100002");

        var cmd = new MutasiBerkasHilangCommand("MM00000000", "USR_PETUGAS");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MH26100002");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Missing);
    }

    [Fact]
    public async Task Handle_WhenReceived_TransitionsToMissing_Successfully()
    {
        // Arrange
        var model = CreateSampleMutasi("MP26100001", MutasiBerkasStatusEnum.Received);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Missing))
            .Returns("MH26100003");

        var cmd = new MutasiBerkasHilangCommand("MP26100001", "USR_PETUGAS");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MH26100003");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Missing);
    }

    [Fact]
    public async Task Handle_WhenDateTimeNotSpecified_UsesCurrentDateTime()
    {
        // Arrange
        var model = CreateSampleMutasi("MK26100001", MutasiBerkasStatusEnum.Dispatched);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Missing))
            .Returns("MH26100004");

        var cmd = new MutasiBerkasHilangCommand("MK26100001", "USR_PETUGAS");
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MH26100004");
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithStringDates_ParsesCorrectly()
    {
        var cmd = new MutasiBerkasHilangCommand(
            "MK26100001", "USR_PETUGAS", "2026-10-17", "10:00:00");

        cmd.MutasiId.Should().Be("MK26100001");
        cmd.Petugas.Should().Be("USR_PETUGAS");
        cmd.TglMutasi.Should().Be(new DateOnly(2026, 10, 17));
        cmd.JamMutasi.Should().Be(new TimeOnly(10, 0, 0));
        cmd.MutasiBerkasId.Should().Be("MK26100001");
    }
    #endregion

    #region REJECTION & INVALID STATE TESTS
    [Fact]
    public async Task Handle_WhenStatusReturned_ThrowsInvalidOperationException()
    {
        var model = CreateSampleMutasi("MM26100001", MutasiBerkasStatusEnum.Returned);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasHilangCommand("MM26100001", "USR_PETUGAS");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dikembalikan*");
    }

    [Fact]
    public async Task Handle_WhenAlreadyMissing_ThrowsInvalidOperationException()
    {
        var model = CreateSampleMutasi("MH26100001", MutasiBerkasStatusEnum.Missing);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasHilangCommand("MH26100001", "USR_PETUGAS");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah berstatus hilang*");
    }

    [Fact]
    public async Task Handle_WhenMutasiIsVoid_ThrowsInvalidOperationException()
    {
        var model = CreateSampleMutasi("MK26100001", MutasiBerkasStatusEnum.Dispatched, isVoid: true);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasHilangCommand("MK26100001", "USR_PETUGAS");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsKeyNotFoundException()
    {
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var cmd = new MutasiBerkasHilangCommand("NOT_FOUND", "USR_PETUGAS");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }
    #endregion

    #region GUARD & VALIDATION TESTS
    [Fact]
    public async Task Handle_WhenNullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenMutasiIdNullOrWhitespace_ThrowsArgumentException(string? mutasiId)
    {
        var sut = CreateSut();
        var cmd = new MutasiBerkasHilangCommand(mutasiId!, "USR_PETUGAS");
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenPetugasNullOrWhitespace_ThrowsArgumentException(string? petugas)
    {
        var sut = CreateSut();
        var cmd = new MutasiBerkasHilangCommand("MK26100001", petugas!);
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion
}
