using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasKirimCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();
    private readonly Mock<IMutasiBerkasIdGenerator> _idGeneratorMock = new();

    private MutasiBerkasKirimHandler CreateSut() =>
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

    private static MutasiBerkasKirimCommand CreateValidCommand(
        decimal qtyRi = 1m,
        decimal qtyRj = 1m) =>
        new(
            TrsBuatId: "RG2610170002",
            MrId: "00-12-34-56",
            DokterId: "DR001",
            LayananId: "POL01",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoli: "1",
            PetugasKirim: "USR_FILING",
            QtyPinjamRi: qtyRi,
            QtyPinjamRj: qtyRj,
            TglMutasi: new DateOnly(2026, 10, 17),
            JamMutasi: new TimeOnly(8, 30, 0)
        );

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenValidCommand_ReturnsResponse_AndPersistsDispatchedMutasi()
    {
        // Arrange
        var cmd = CreateValidCommand(qtyRi: 1m, qtyRj: 2m);
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
        result.TrsBuatId.Should().Be("RG2610170002");
        result.MrId.Should().Be("00-12-34-56");
        result.Status.Should().Be((int)MutasiBerkasStatusEnum.Dispatched);
        result.TotalPinjam.Should().Be(3m);

        _idGeneratorMock.Verify(x => x.Generate(MutasiBerkasStatusEnum.Dispatched), Times.Once);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100001" &&
            m.TrsBuatId == "RG2610170002" &&
            m.PasienId == "00-12-34-56" &&
            m.Status == MutasiBerkasStatusEnum.Dispatched &&
            m.PetugasId == "USR_FILING" &&
            m.Volume.QtyPinjamRi == 1m &&
            m.Volume.QtyPinjamRj == 2m &&
            m.Volume.TotalPinjam == 3m &&
            m.JadwalPelayanan.LayananId == "POL01" &&
            m.JadwalPelayanan.DokterId == "DR001" &&
            m.JadwalPelayanan.JamPraktek == "08:00" &&
            m.JadwalPelayanan.SesiPoliId == "1"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPreviousMutasiWasReturned_SuccessfullyDispatchesNewMutasi()
    {
        // Arrange
        var cmd = CreateValidCommand(qtyRi: 0m, qtyRj: 1m);
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
    public async Task Handle_WhenPreviousMutasiWasMissing_SuccessfullyDispatchesNewMutasi()
    {
        // Arrange
        var cmd = CreateValidCommand(qtyRi: 1m, qtyRj: 0m);
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
            m.MutasiBerkasId == "MK26100003"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPreviousMutasiWasVoided_SuccessfullyDispatchesNewMutasi()
    {
        // Arrange
        var cmd = CreateValidCommand(qtyRi: 1m, qtyRj: 0m);
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
    public void Constructor_WithStringTglPraktek_ParsesDateCorrectly()
    {
        var cmd = new MutasiBerkasKirimCommand(
            "RG01", "MR01", "DR01", "POL01", "2026-10-17", "08:00", "1", "USR01", 1, 0);

        cmd.TglPraktek.Should().Be(new DateOnly(2026, 10, 17));
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
    public async Task Handle_WhenTrsBuatIdIsWhitespace_ThrowsArgumentException(string? invalidTrsBuatId)
    {
        var cmd = CreateValidCommand() with { TrsBuatId = invalidTrsBuatId! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
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
    public async Task Handle_WhenDokterIdIsWhitespace_ThrowsArgumentException(string? invalidDokterId)
    {
        var cmd = CreateValidCommand() with { DokterId = invalidDokterId! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenLayananIdIsWhitespace_ThrowsArgumentException(string? invalidLayananId)
    {
        var cmd = CreateValidCommand() with { LayananId = invalidLayananId! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenJamPraktekIsWhitespace_ThrowsArgumentException(string? invalidJamPraktek)
    {
        var cmd = CreateValidCommand() with { JamPraktek = invalidJamPraktek! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenSesiPoliIsWhitespace_ThrowsArgumentException(string? invalidSesiPoli)
    {
        var cmd = CreateValidCommand() with { SesiPoli = invalidSesiPoli! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenPetugasKirimIsWhitespace_ThrowsArgumentException(string? invalidPetugasKirim)
    {
        var cmd = CreateValidCommand() with { PetugasKirim = invalidPetugasKirim! };
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenQtyPinjamRiIsNegative_ThrowsArgumentException()
    {
        var cmd = CreateValidCommand(qtyRi: -1m, qtyRj: 1m);
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak boleh negatif*");
    }

    [Fact]
    public async Task Handle_WhenQtyPinjamRjIsNegative_ThrowsArgumentException()
    {
        var cmd = CreateValidCommand(qtyRi: 1m, qtyRj: -1m);
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak boleh negatif*");
    }

    [Fact]
    public async Task Handle_WhenTotalQtyPinjamIsZero_ThrowsArgumentException()
    {
        var cmd = CreateValidCommand(qtyRi: 0m, qtyRj: 0m);
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*lebih dari 0*");
    }
    #endregion
}
