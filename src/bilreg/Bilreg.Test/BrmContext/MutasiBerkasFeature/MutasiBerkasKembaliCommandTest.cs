using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasKembaliCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();
    private readonly Mock<IMutasiBerkasIdGenerator> _idGeneratorMock = new();

    private MutasiBerkasKembaliHandler CreateSut() =>
        new(_mutasiRepoMock.Object, _idGeneratorMock.Object);

    private static JadwalPelayananType CreateSampleJadwal() =>
        new("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateSampleDispatched(
        string id = "MK26100001",
        decimal qtyRi = 1,
        decimal qtyRj = 1,
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
            volume: VolumeBerkasType.CreatePinjam(qtyRi, qtyRj));

        if (isVoid)
        {
            model.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(8, 15, 0), "Dibatalkan");
        }

        return model;
    }

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenDispatched_TransitionsToReturned_GeneratesMMPrefix_AndUpdatesVolume()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001", qtyRi: 1, qtyRj: 1);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Returned))
            .Returns("MM26100001");

        var cmd = new MutasiBerkasKembaliCommand(
            MutasiId: "MK26100001",
            PetugasKembali: "USR_FILING",
            QtyKembaliRi: 1,
            QtyKembaliRj: 1,
            TglKembali: new DateOnly(2026, 10, 17),
            JamKembali: new TimeOnly(12, 0, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MM26100001");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Returned);
        result.Volume.Should().NotBeNull();
        result.Volume.TotalKembali.Should().Be(2);
        result.Volume.SisaPinjam.Should().Be(0);
        result.Volume.IsLengkapKembali.Should().BeTrue();
        result.PetugasKembali.Should().Be("USR_FILING");

        _idGeneratorMock.Verify(x => x.Generate(MutasiBerkasStatusEnum.Returned), Times.Once);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MM26100001" &&
            m.Status == MutasiBerkasStatusEnum.Returned &&
            m.PetugasId == "USR_FILING" &&
            m.Volume.TotalKembali == 2 &&
            m.Volume.IsLengkapKembali
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReceived_TransitionsToReturned_Successfully()
    {
        // Arrange
        var model = CreateSampleDispatched("MP26100001", qtyRi: 0, qtyRj: 1);
        model.Terima("USR_POLI", new DateOnly(2026, 10, 17), new TimeOnly(8, 30, 0));
        model.Status.Should().Be(MutasiBerkasStatusEnum.Received);

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Returned))
            .Returns("MM26100002");

        var cmd = new MutasiBerkasKembaliCommand(
            MutasiId: "MP26100001",
            PetugasKembali: "USR_FILING",
            QtyKembaliRi: 0,
            QtyKembaliRj: 1);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MM26100002");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Returned);
        result.Volume.IsLengkapKembali.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenPartialReturn_ReturnsVolumeWithRemainingBalance()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001", qtyRi: 1, qtyRj: 2);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Returned))
            .Returns("MM26100003");

        var cmd = new MutasiBerkasKembaliCommand(
            MutasiId: "MK26100001",
            PetugasKembali: "USR_FILING",
            QtyKembaliRi: 0,
            QtyKembaliRj: 1);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Volume.TotalKembali.Should().Be(1);
        result.Volume.SisaPinjam.Should().Be(2);
        result.Volume.IsLengkapKembali.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenDateTimeNotSpecified_UsesCurrentDateTime()
    {
        // Arrange
        var model = CreateSampleDispatched("MK26100001", qtyRi: 0, qtyRj: 1);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Returned))
            .Returns("MM26100004");

        var cmd = new MutasiBerkasKembaliCommand(
            MutasiId: "MK26100001",
            PetugasKembali: "USR_FILING",
            QtyKembaliRi: 0,
            QtyKembaliRj: 1);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MM26100004");
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithStringDates_ParsesCorrectly()
    {
        var cmd = new MutasiBerkasKembaliCommand(
            "MK26100001", "USR_FILING", 1, 0, "2026-10-17", "11:30:00");

        cmd.MutasiId.Should().Be("MK26100001");
        cmd.PetugasKembali.Should().Be("USR_FILING");
        cmd.QtyKembaliRi.Should().Be(1);
        cmd.QtyKembaliRj.Should().Be(0);
        cmd.TglKembali.Should().Be(new DateOnly(2026, 10, 17));
        cmd.JamKembali.Should().Be(new TimeOnly(11, 30, 0));
        cmd.MutasiBerkasId.Should().Be("MK26100001");
    }
    #endregion

    #region VOLUME VALIDATION TESTS
    [Fact]
    public async Task Handle_WhenReturnRiExceedsBorrow_ThrowsArgumentException()
    {
        var model = CreateSampleDispatched("MK26100001", qtyRi: 0, qtyRj: 1);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasKembaliCommand("MK26100001", "USR_FILING", QtyKembaliRi: 1, QtyKembaliRj: 1);
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*rawat inap kembali melebihi jumlah pinjam*");
    }

    [Fact]
    public async Task Handle_WhenReturnRjExceedsBorrow_ThrowsArgumentException()
    {
        var model = CreateSampleDispatched("MK26100001", qtyRi: 1, qtyRj: 0);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasKembaliCommand("MK26100001", "USR_FILING", QtyKembaliRi: 1, QtyKembaliRj: 1);
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*rawat jalan kembali melebihi jumlah pinjam*");
    }

    [Fact]
    public async Task Handle_WhenNegativeVolume_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = new MutasiBerkasKembaliCommand("MK26100001", "USR_FILING", QtyKembaliRi: -1, QtyKembaliRj: 0);
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak boleh negatif*");
    }

    [Fact]
    public async Task Handle_WhenZeroTotalVolume_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = new MutasiBerkasKembaliCommand("MK26100001", "USR_FILING", QtyKembaliRi: 0, QtyKembaliRj: 0);
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*lebih dari 0*");
    }
    #endregion

    #region REJECTION & INVALID STATE TESTS
    [Fact]
    public async Task Handle_WhenStatusPermintaan_ThrowsInvalidOperationException()
    {
        var model = MutasiBerkasModel.CreatePermintaan(
            "MM00000000", "RG01", "00123456", new DateOnly(2026, 10, 17), new TimeOnly(8, 0), "USR01", CreateSampleJadwal());
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasKembaliCommand("MM00000000", "USR_FILING", QtyKembaliRi: 0, QtyKembaliRj: 1);
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*harus Dispatched atau Received*");
    }

    [Fact]
    public async Task Handle_WhenMutasiIsVoid_ThrowsInvalidOperationException()
    {
        var model = CreateSampleDispatched("MK26100001", isVoid: true);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasKembaliCommand("MK26100001", "USR_FILING", QtyKembaliRi: 1, QtyKembaliRj: 1);
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

        var cmd = new MutasiBerkasKembaliCommand("NOT_FOUND", "USR_FILING", QtyKembaliRi: 0, QtyKembaliRj: 1);
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
        var cmd = new MutasiBerkasKembaliCommand(mutasiId!, "USR_FILING", QtyKembaliRi: 0, QtyKembaliRj: 1);
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenPetugasKembaliNullOrWhitespace_ThrowsArgumentException(string? petugas)
    {
        var sut = CreateSut();
        var cmd = new MutasiBerkasKembaliCommand("MK26100001", petugas!, QtyKembaliRi: 0, QtyKembaliRj: 1);
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion
}
