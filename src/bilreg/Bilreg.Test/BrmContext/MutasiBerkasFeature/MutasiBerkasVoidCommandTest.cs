using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasVoidCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();

    private MutasiBerkasVoidHandler CreateSut() =>
        new(_mutasiRepoMock.Object);

    private static JadwalPelayananType CreateSampleJadwal() =>
        new("POL01", "DR001", new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateSampleKirim(
        string id = "MK26100001",
        string trsBuatId = "RG2610170001",
        DateOnly? tgl = null,
        TimeOnly? jam = null,
        bool isVoid = false)
    {
        var model = MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: id,
            trsBuatId: trsBuatId,
            pasienId: "00-12-34-56",
            tglMutasi: tgl ?? new DateOnly(2026, 10, 17),
            jamMutasi: jam ?? new TimeOnly(8, 0, 0),
            petugasId: "USR_FILING",
            jadwalPelayanan: CreateSampleJadwal(),
            volume: VolumeBerkasType.CreatePinjam(1, 1));

        if (isVoid)
        {
            model.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(8, 15, 0), "Salah kirim");
        }

        return model;
    }

    private static MutasiBerkasModel CreateSampleTerima(
        string id = "MP26100001",
        string trsBuatId = "RG2610170001",
        DateOnly? tgl = null,
        TimeOnly? jam = null)
    {
        var model = CreateSampleKirim("MK_TEMP", trsBuatId, tgl, jam ?? new TimeOnly(8, 0, 0));
        model.SetId(id);
        model.Terima("USR_POLI", tgl ?? new DateOnly(2026, 10, 17), jam ?? new TimeOnly(8, 30, 0));
        return model;
    }

    private static MutasiBerkasModel CreateSampleKembali(
        string id = "MM26100001",
        string trsBuatId = "RG2610170001",
        DateOnly? tgl = null,
        TimeOnly? jam = null)
    {
        var model = CreateSampleTerima("MP_TEMP", trsBuatId, tgl, jam ?? new TimeOnly(8, 30, 0));
        model.SetId(id);
        model.Kembali("USR_RM", tgl ?? new DateOnly(2026, 10, 17), jam ?? new TimeOnly(12, 0, 0), 1, 1);
        return model;
    }

    private static MutasiBerkasModel CreateSampleHilang(
        string id = "MH26100001",
        string trsBuatId = "RG2610170001",
        DateOnly? tgl = null,
        TimeOnly? jam = null)
    {
        var model = CreateSampleKirim("MK_TEMP", trsBuatId, tgl, jam ?? new TimeOnly(8, 0, 0));
        model.SetId(id);
        model.TandaiHilang("USR_RM", tgl ?? new DateOnly(2026, 10, 17), jam ?? new TimeOnly(9, 0, 0));
        return model;
    }

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenSingleKirimVoided_RollsBackToPermintaan_RecordsVoidAudit_AndSavesChanges()
    {
        // Arrange
        var model = CreateSampleKirim("MK26100001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat("RG2610170001"))
            .Returns([model]);

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "MK26100001",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Salah berkas rekam medis",
            TglVoid: new DateOnly(2026, 10, 17),
            JamVoid: new TimeOnly(8, 45, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100001");
        result.StatusSebelumnya.Should().Be((int)MutasiBerkasStatusEnum.Dispatched);
        result.StatusRollback.Should().Be((int)MutasiBerkasStatusEnum.Permintaan); // 0
        result.TrsBuatId.Should().Be("RG2610170001");
        result.PasienId.Should().Be("00-12-34-56");
        result.MrId.Should().Be("00-12-34-56");

        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100001" &&
            m.IsVoid &&
            m.Status == MutasiBerkasStatusEnum.Void &&
            m.VoidInfo.PetugasVoid == "USR_ADMIN" &&
            m.VoidInfo.AlasanVoid == "Salah berkas rekam medis" &&
            m.VoidInfo.TglVoid == new DateOnly(2026, 10, 17) &&
            m.VoidInfo.JamVoid == new TimeOnly(8, 45, 0)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTerimaVoided_RollsBackToDispatched()
    {
        // Arrange
        var kirim = CreateSampleKirim("MK26100001", jam: new TimeOnly(8, 0, 0));
        var terima = CreateSampleTerima("MP26100001", jam: new TimeOnly(8, 30, 0));

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(terima));
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat("RG2610170001"))
            .Returns([kirim, terima]);

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "MP26100001",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Batal terima di poli",
            TglVoid: new DateOnly(2026, 10, 17),
            JamVoid: new TimeOnly(8, 35, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MP26100001");
        result.StatusSebelumnya.Should().Be((int)MutasiBerkasStatusEnum.Received); // 2
        result.StatusRollback.Should().Be((int)MutasiBerkasStatusEnum.Dispatched); // 1
    }

    [Fact]
    public async Task Handle_WhenKembaliVoided_RollsBackToReceived()
    {
        // Arrange
        var kirim = CreateSampleKirim("MK26100001", jam: new TimeOnly(8, 0, 0));
        var terima = CreateSampleTerima("MP26100001", jam: new TimeOnly(8, 30, 0));
        var kembali = CreateSampleKembali("MM26100001", jam: new TimeOnly(12, 0, 0));

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MM26100001")))
            .Returns(MayBe.From(kembali));
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat("RG2610170001"))
            .Returns([kirim, terima, kembali]);

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "MM26100001",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Berkas belum selesai periksa",
            TglVoid: new DateOnly(2026, 10, 17),
            JamVoid: new TimeOnly(12, 5, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MM26100001");
        result.StatusSebelumnya.Should().Be((int)MutasiBerkasStatusEnum.Returned); // 3
        result.StatusRollback.Should().Be((int)MutasiBerkasStatusEnum.Received); // 2
    }

    [Fact]
    public async Task Handle_WhenHilangVoided_RollsBackToDispatched()
    {
        // Arrange
        var kirim = CreateSampleKirim("MK26100001", jam: new TimeOnly(8, 0, 0));
        var hilang = CreateSampleHilang("MH26100001", jam: new TimeOnly(9, 0, 0));

        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MH26100001")))
            .Returns(MayBe.From(hilang));
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat("RG2610170001"))
            .Returns([kirim, hilang]);

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "MH26100001",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Berkas sudah ditemukan di lemari lain",
            TglVoid: new DateOnly(2026, 10, 17),
            JamVoid: new TimeOnly(9, 15, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MH26100001");
        result.StatusSebelumnya.Should().Be((int)MutasiBerkasStatusEnum.Missing); // 4
        result.StatusRollback.Should().Be((int)MutasiBerkasStatusEnum.Dispatched); // 1
    }

    [Fact]
    public async Task Handle_WhenNoActiveMutationsLeft_RollsBackToPermintaan()
    {
        // Arrange
        var model = CreateSampleKirim("MK26100001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat("RG2610170001"))
            .Returns([]);

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "MK26100001",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Pembatalan registrasi");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.StatusRollback.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenTglAndJamVoidNull_UsesCurrentDateAndTime()
    {
        // Arrange
        var model = CreateSampleKirim("MK26100001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat(It.IsAny<string>()))
            .Returns([model]);

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "MK26100001",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Batal");

        var sut = CreateSut();
        var before = DateTime.Now;

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.VoidInfo.IsVoid &&
            m.VoidInfo.TglVoid >= DateOnly.FromDateTime(before.AddMinutes(-1))
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStringConstructorUsed_ParsesDateAndTimeStringCorrectly()
    {
        // Arrange
        var model = CreateSampleKirim("MK26100001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));
        _mutasiRepoMock.Setup(x => x.ListByTrsBuat(It.IsAny<string>()))
            .Returns([]);

        var cmd = new MutasiBerkasVoidCommand(
            "MK26100001",
            "USR_ADMIN",
            "Salah input berkas",
            "2026-10-17",
            "11:22:33");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.VoidInfo.TglVoid == new DateOnly(2026, 10, 17) &&
            m.VoidInfo.JamVoid == new TimeOnly(11, 22, 33)
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStringConstructorWithEmptyDateAndJam_DefaultsToNullInRecord()
    {
        // Arrange
        var cmd = new MutasiBerkasVoidCommand("MK26100001", "USR_ADMIN", "Alasan", "", "");

        // Assert
        cmd.TglVoid.Should().BeNull();
        cmd.JamVoid.Should().BeNull();
    }
    #endregion

    #region REJECTION TESTS
    [Fact]
    public async Task Handle_WhenAlreadyVoided_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateSampleKirim("MK26100001", isVoid: true);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(model));

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "MK26100001",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Batal lagi");

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan (void)*");

        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEntityNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var cmd = new MutasiBerkasVoidCommand(
            MutasiId: "NOT_FOUND",
            PetugasVoid: "USR_ADMIN",
            AlasanVoid: "Batal");

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");

        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Never);
    }
    #endregion

    #region VALIDATION GUARDS
    [Fact]
    public async Task Handle_WhenRequestNull_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = async () => await sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenMutasiIdInvalid_ThrowsArgumentException(string? invalidId)
    {
        var cmd = new MutasiBerkasVoidCommand(invalidId!, "USR_ADMIN", "Alasan");
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenPetugasVoidInvalid_ThrowsArgumentException(string? invalidPetugas)
    {
        var cmd = new MutasiBerkasVoidCommand("MK26100001", invalidPetugas!, "Alasan");
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenAlasanVoidInvalid_ThrowsArgumentException(string? invalidAlasan)
    {
        var cmd = new MutasiBerkasVoidCommand("MK26100001", "USR_ADMIN", invalidAlasan!);
        var sut = CreateSut();
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion
}
