using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasTerimaCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();
    private readonly Mock<IMutasiBerkasIdGenerator> _idGeneratorMock = new();

    private MutasiBerkasTerimaHandler CreateSut() =>
        new(_mutasiRepoMock.Object, _idGeneratorMock.Object);

    private static JadwalPelayananType CreateSampleJadwal() =>
        new("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateSampleDispatched(
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
            model.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(8, 15, 0), "Dibatalkan");
        }

        return model;
    }

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenValidCommand_TransitionsToReceived_GeneratesMPPrefix_AndReturnsResponse()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Received))
            .Returns("MP26100001");

        var cmd = new MutasiBerkasTerimaCommand(
            MutasiId: "MK26100001",
            PetugasTerima: "USR_POLI",
            TglTerima: new DateOnly(2026, 10, 17),
            JamTerima: new TimeOnly(8, 45, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MP26100001");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Received);
        result.TrsBuatId.Should().Be("RG2610170001");
        result.MrId.Should().Be("00-12-34-56");
        result.PetugasTerima.Should().Be("USR_POLI");

        _idGeneratorMock.Verify(x => x.Generate(MutasiBerkasStatusEnum.Received), Times.Once);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MP26100001" &&
            m.Status == MutasiBerkasStatusEnum.Received &&
            m.PetugasId == "USR_POLI" &&
            m.TglMutasi == new DateOnly(2026, 10, 17) &&
            m.JamMutasi == new TimeOnly(8, 45, 0)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDateTimeNotSpecified_UsesCurrentDateTime()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Received))
            .Returns("MP26100002");

        var cmd = new MutasiBerkasTerimaCommand(
            MutasiId: "MK26100001",
            PetugasTerima: "USR_POLI");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MP26100002");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Received);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MP26100002" &&
            m.Status == MutasiBerkasStatusEnum.Received
        )), Times.Once);
    }

    [Fact]
    public void Constructor_WithStringDates_ParsesCorrectly()
    {
        var cmd = new MutasiBerkasTerimaCommand(
            "MK26100001", "USR_POLI", "2026-10-17", "08:45:00");

        cmd.MutasiId.Should().Be("MK26100001");
        cmd.PetugasTerima.Should().Be("USR_POLI");
        cmd.TglTerima.Should().Be(new DateOnly(2026, 10, 17));
        cmd.JamTerima.Should().Be(new TimeOnly(8, 45, 0));
        cmd.MutasiBerkasId.Should().Be("MK26100001");
    }

    [Fact]
    public async Task Handle_WhenMutasiIdEmpty_FallsBackToMrIdLookup()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001");
        _mutasiRepoMock.Setup(x => x.GetLatestActiveByMr("00-12-34-56"))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Received))
            .Returns("MP26100003");

        var cmd = new MutasiBerkasTerimaCommand(
            MutasiId: "",
            PetugasTerima: "USR_POLI",
            MrId: "00-12-34-56");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MP26100003");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Received);
        _mutasiRepoMock.Verify(x => x.GetLatestActiveByMr("00-12-34-56"), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenMutasiIdAndMrIdEmpty_FallsBackToTrsBuatIdLookup()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001");
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat("RG2610170001"))
            .Returns(new[] { model });
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Received))
            .Returns("MP26100004");

        var cmd = new MutasiBerkasTerimaCommand(
            MutasiId: "",
            PetugasTerima: "USR_POLI",
            TrsBuatId: "RG2610170001");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MP26100004");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Received);
        _mutasiRepoMock.Verify(x => x.ListByTrsBuat("RG2610170001"), Times.Once);
    }
    #endregion

    #region REJECTION & INVALID STATE TESTS
    [Fact]
    public async Task Handle_WhenStatusNotDispatched_ThrowsInvalidOperationException()
    {
        // Arrange - model is Permintaan (status 0)
        var model = MutasiBerkasModel.CreatePermintaan(
            "MM00000000", "RG01", "00123456", new DateOnly(2026, 10, 17), new TimeOnly(8, 0), "USR01", CreateSampleJadwal());

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MM00000000")))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasTerimaCommand("MM00000000", "USR_POLI");
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*harus Dispatched*");
        _idGeneratorMock.Verify(x => x.Generate(It.IsAny<MutasiBerkasStatusEnum>()), Times.Never);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStatusAlreadyReceived_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001");
        model.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 30));

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasTerimaCommand("MK26100001", "USR_POLI2");
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*harus Dispatched*");
    }

    [Fact]
    public async Task Handle_WhenMutasiIsVoid_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001", isVoid: true);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasTerimaCommand("MK26100001", "USR_POLI");
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public async Task Handle_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var cmd = new MutasiBerkasTerimaCommand("NOT_FOUND", "USR_POLI");
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
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

    [Fact]
    public async Task Handle_WhenAllIdentifiersEmpty_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = new MutasiBerkasTerimaCommand("", "USR_POLI");
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*harus diisi*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenPetugasTerimaNullOrWhitespace_ThrowsArgumentException(string? petugas)
    {
        var sut = CreateSut();
        var cmd = new MutasiBerkasTerimaCommand("MK26100001", petugas!);
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion
}
