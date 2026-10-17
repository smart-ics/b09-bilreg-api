using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature;
using Bilreg.Application.BrmContext.MutasiBerkasFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasTransferUnitCommandTest
{
    private readonly Mock<IMutasiBerkasRepo> _mutasiRepoMock = new();
    private readonly Mock<IMutasiBerkasIdGenerator> _idGeneratorMock = new();
    private readonly Mock<IAntrianRepo> _antrianRepoMock = new();

    private MutasiBerkasTransferUnitHandler CreateSut() =>
        new(_mutasiRepoMock.Object, _idGeneratorMock.Object, _antrianRepoMock.Object);

    private static JadwalPelayananType CreateSampleJadwal(string layananId = "POL01", string dokterId = "DR001") =>
        new(layananId, dokterId, new DateOnly(2026, 10, 17), "08:00", "1");

    private static MutasiBerkasModel CreateSampleOriginating(
        string id = "MP26100001",
        MutasiBerkasStatusEnum status = MutasiBerkasStatusEnum.Received,
        string trsBuatId = "RG2610170001",
        bool isVoid = false)
    {
        var model = MutasiBerkasModel.CreateKirim(
            mutasiBerkasId: id,
            trsBuatId: trsBuatId,
            pasienId: "00-12-34-56",
            tglMutasi: new DateOnly(2026, 10, 17),
            jamMutasi: new TimeOnly(8, 0, 0),
            petugasId: "USR_FILING",
            jadwalPelayanan: CreateSampleJadwal(),
            volume: VolumeBerkasType.CreatePinjam(1, 1));

        if (status == MutasiBerkasStatusEnum.Received)
        {
            model.Terima("USR_POLI1", new DateOnly(2026, 10, 17), new TimeOnly(8, 30, 0));
        }
        else if (status == MutasiBerkasStatusEnum.Returned)
        {
            model.Terima("USR_POLI1", new DateOnly(2026, 10, 17), new TimeOnly(8, 30, 0));
            model.Kembali("USR_FILING", new DateOnly(2026, 10, 17), new TimeOnly(9, 0, 0), 1, 1);
        }
        else if (status == MutasiBerkasStatusEnum.Missing)
        {
            model.TandaiHilang("USR_POLI1", new DateOnly(2026, 10, 17), new TimeOnly(8, 45, 0));
        }
        else if (status == MutasiBerkasStatusEnum.Permintaan)
        {
            model = MutasiBerkasModel.CreatePermintaan(
                mutasiBerkasId: id,
                trsBuatId: trsBuatId,
                pasienId: "00-12-34-56",
                tglMutasi: new DateOnly(2026, 10, 17),
                jamMutasi: new TimeOnly(8, 0, 0),
                petugasId: "USR_FILING",
                jadwalPelayanan: CreateSampleJadwal());
        }

        if (isVoid)
        {
            model.Void("USR_ADMIN", new DateOnly(2026, 10, 17), new TimeOnly(9, 30, 0), "Dibatalkan");
        }

        return model;
    }

    #region HAPPY PATH TESTS
    [Fact]
    public async Task Handle_WhenValidCommandFromReceived_GeneratesMKPrefix_PersistsTransferMutasi_TriggersAntrianRepo_AndReturnsResponse()
    {
        // Arrange
        var originating = CreateSampleOriginating("MP26100001", MutasiBerkasStatusEnum.Received, "RG2610170001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(originating));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100002");

        var sequencerMock = new Mock<ISequencer>();
        sequencerMock.Setup(x => x.GetNextNoUrut(It.IsAny<string>(), It.IsAny<int>())).Returns(1);

        var queue = new AntrianModel(
            antrianId: "AN2610170001",
            antrianDate: new DateOnly(2026, 10, 17),
            startTime: new TimeOnly(8, 0),
            endTime: new TimeOnly(12, 0),
            sequenceTag: "AN2610170800_DR002",
            antrianDesc: "Poli Saraf POL02",
            servicePoint: ServicePointType.Default,
            listEntry: new List<AntrianEntryModel>(),
            sequencer: sequencerMock.Object,
            queuePrefixSnapshot: "B");

        var header = new AntrianHeaderView("AN2610170001", "Poli Saraf POL02", new DateOnly(2026, 10, 17), new TimeOnly(8, 0), "AN2610170800_DR002");
        _antrianRepoMock.Setup(x => x.ListData(new DateOnly(2026, 10, 17)))
            .Returns(new List<AntrianHeaderView> { header });
        _antrianRepoMock.Setup(x => x.LoadEntity(header))
            .Returns(MayBe.From(queue));

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "09:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m,
            Catatan: "Alih rawat ke Poli Saraf",
            TglMutasi: new DateOnly(2026, 10, 17),
            JamMutasi: new TimeOnly(8, 45, 0));

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100002");
        result.Status.Should().Be(MutasiBerkasStatusEnum.Dispatched);
        result.TrsBuatId.Should().Be("RG2610170001");
        result.PasienId.Should().Be("00-12-34-56");
        result.LayananTujuanId.Should().Be("POL02");
        result.AntrianNomor.Should().Be("B0001");

        _idGeneratorMock.Verify(x => x.Generate(MutasiBerkasStatusEnum.Dispatched), Times.Once);
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.MutasiBerkasId == "MK26100002" &&
            m.Status == MutasiBerkasStatusEnum.Dispatched &&
            m.PetugasId == "USR_TRANSFER" &&
            m.JadwalPelayanan.LayananId == "POL02" &&
            m.JadwalPelayanan.DokterId == "DR002" &&
            m.JadwalPelayanan.TglPraktek == new DateOnly(2026, 10, 17) &&
            m.JadwalPelayanan.JamPraktek == "09:00" &&
            m.JadwalPelayanan.SesiPoliId == "1" &&
            m.Volume.QtyPinjamRi == 1m &&
            m.Volume.QtyPinjamRj == 0m
        )), Times.Once);

        _antrianRepoMock.Verify(x => x.ListData(new DateOnly(2026, 10, 17)), Times.Once);
        _antrianRepoMock.Verify(x => x.SaveChanges(queue), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidCommandFromDispatched_GeneratesMKPrefix_AndSucceeds()
    {
        // Arrange
        var originating = CreateSampleOriginating("MK26100001", MutasiBerkasStatusEnum.Dispatched, "RG2610170001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MK26100001")))
            .Returns(MayBe.From(originating));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100002");

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MK26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL03",
            DokterTujuanId: "DR003",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "10:00",
            SesiPoliTujuan: "2",
            QtyTransferRi: 0m,
            QtyTransferRj: 1m);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100002");
        result.Status.Should().Be(MutasiBerkasStatusEnum.Dispatched);
        result.LayananTujuanId.Should().Be("POL03");

        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTrsBuatIdIsNotOutpatientVisit_DoesNotTriggerAntrianRepo()
    {
        // Arrange
        var originating = CreateSampleOriginating("MP26100001", MutasiBerkasStatusEnum.Received, "BO2610170001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(originating));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100003");

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "09:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.AntrianNomor.Should().BeEmpty();
        _antrianRepoMock.Verify(x => x.ListData(It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAntrianRepoThrowsException_GracefullyHandlesAndReturnsEmptyAntrianNomor()
    {
        // Arrange
        var originating = CreateSampleOriginating("MP26100001", MutasiBerkasStatusEnum.Received, "RG2610170001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(originating));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100004");

        _antrianRepoMock.Setup(x => x.ListData(It.IsAny<DateOnly>()))
            .Throws(new InvalidOperationException("Antrian service temporarily unavailable"));

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "09:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 1m);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.MutasiId.Should().Be("MK26100004");
        result.AntrianNomor.Should().BeEmpty();
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBerkasModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenStringConstructorOverloadUsed_ParsesCorrectly()
    {
        // Arrange
        var originating = CreateSampleOriginating("MP26100001", MutasiBerkasStatusEnum.Received, "RG2610170001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(originating));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100005");

        var cmd = new MutasiBerkasTransferUnitCommand(
            mutasiIdAsal: "MP26100001",
            petugasTransfer: "USR_TRANSFER",
            layananTujuanId: "POL02",
            dokterTujuanId: "DR002",
            tglPraktek: "2026-10-20",
            jamPraktek: "08:30",
            sesiPoliTujuan: "1",
            qtyTransferRi: 1m,
            qtyTransferRj: 1m,
            catatan: "Transfer via string constructor",
            tglMutasi: "2026-10-18",
            jamMutasi: "11:22:33");

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.MutasiId.Should().Be("MK26100005");
        cmd.TglPraktek.Should().Be(new DateOnly(2026, 10, 20));
        cmd.TglMutasi.Should().Be(new DateOnly(2026, 10, 18));
        cmd.JamMutasi.Should().Be(new TimeOnly(11, 22, 33));
        cmd.Catatan.Should().Be("Transfer via string constructor");
        cmd.MutasiBerkasId.Should().Be("MP26100001");
    }

    [Fact]
    public async Task Handle_WhenDateTimeNotSpecified_UsesCurrentDateTime()
    {
        // Arrange
        var originating = CreateSampleOriginating("MP26100001", MutasiBerkasStatusEnum.Received, "RG2610170001");
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBerkasKey>(k => k.MutasiBerkasId == "MP26100001")))
            .Returns(MayBe.From(originating));
        _idGeneratorMock.Setup(x => x.Generate(MutasiBerkasStatusEnum.Dispatched))
            .Returns("MK26100006");

        var before = DateTime.Now.AddSeconds(-1);
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);
        var after = DateTime.Now.AddSeconds(1);

        // Assert
        result.Should().NotBeNull();
        _mutasiRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBerkasModel>(m =>
            m.TglMutasi >= DateOnly.FromDateTime(before) &&
            m.TglMutasi <= DateOnly.FromDateTime(after)
        )), Times.Once);
    }
    #endregion

    #region REJECTION & ERROR HANDLING TESTS
    [Fact]
    public async Task Handle_WhenOriginatingMutasiNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe<MutasiBerkasModel>.None);

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "NON_EXISTENT",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenOriginatingMutasiIsVoided_ThrowsInvalidOperationException()
    {
        // Arrange
        var originating = CreateSampleOriginating("MP26100001", MutasiBerkasStatusEnum.Received, isVoid: true);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(originating));

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan (void)*");
    }

    [Fact]
    public async Task Handle_WhenOriginatingMutasiIsReturned_ThrowsInvalidOperationException()
    {
        // Arrange
        var originating = CreateSampleOriginating("MM26100001", MutasiBerkasStatusEnum.Returned);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(originating));

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MM26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Hanya berkas berstatus Dispatched atau Received yang dapat ditransfer antar-unit*");
    }

    [Fact]
    public async Task Handle_WhenOriginatingMutasiIsMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var originating = CreateSampleOriginating("MH26100001", MutasiBerkasStatusEnum.Missing);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(originating));

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MH26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Hanya berkas berstatus Dispatched atau Received yang dapat ditransfer antar-unit*");
    }

    [Fact]
    public async Task Handle_WhenOriginatingMutasiIsPermintaan_ThrowsInvalidOperationException()
    {
        // Arrange
        var originating = CreateSampleOriginating("REQ26100001", MutasiBerkasStatusEnum.Permintaan);
        _mutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBerkasKey>()))
            .Returns(MayBe.From(originating));

        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "REQ26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();

        // Act & Assert
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Hanya berkas berstatus Dispatched atau Received yang dapat ditransfer antar-unit*");
    }
    #endregion

    #region VALIDATION GUARDS TESTS
    [Fact]
    public async Task Handle_WhenRequestIsNull_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenMutasiIdAsalIsEmpty_ThrowsArgumentException(string? invalidId)
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: invalidId!,
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenPetugasTransferIsEmpty_ThrowsArgumentException(string? invalidPetugas)
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: invalidPetugas!,
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenLayananTujuanIdIsEmpty_ThrowsArgumentException(string? invalidLayanan)
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: invalidLayanan!,
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenDokterTujuanIdIsEmpty_ThrowsArgumentException(string? invalidDokter)
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: invalidDokter!,
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenJamPraktekIsEmpty_ThrowsArgumentException(string? invalidJam)
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: invalidJam!,
            SesiPoliTujuan: "1",
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Handle_WhenSesiPoliTujuanIsEmpty_ThrowsArgumentException(string? invalidSesi)
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: invalidSesi!,
            QtyTransferRi: 1m,
            QtyTransferRj: 0m);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-0.5, 1)]
    public async Task Handle_WhenTransferVolumeIsNegative_ThrowsArgumentException(decimal qtyRi, decimal qtyRj)
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: qtyRi,
            QtyTransferRj: qtyRj);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak boleh negatif*");
    }

    [Fact]
    public async Task Handle_WhenTotalTransferVolumeIsZero_ThrowsArgumentException()
    {
        var cmd = new MutasiBerkasTransferUnitCommand(
            MutasiIdAsal: "MP26100001",
            PetugasTransfer: "USR_TRANSFER",
            LayananTujuanId: "POL02",
            DokterTujuanId: "DR002",
            TglPraktek: new DateOnly(2026, 10, 17),
            JamPraktek: "08:00",
            SesiPoliTujuan: "1",
            QtyTransferRi: 0m,
            QtyTransferRj: 0m);

        var sut = CreateSut();
        var act = () => sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*harus lebih dari 0*");
    }
    #endregion
}
