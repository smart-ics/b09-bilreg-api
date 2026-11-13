using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegJalanUbahKunjunganHandlerTest
{
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepo = new();
    private readonly Mock<IJadwalPraktekRepo> _jadwalPraktekRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<IAntrianMapRepo> _antrianMapRepo = new();
    private readonly Mock<IQueueNumberCompatibilityAdapter> _queueNumberAdapter = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<IKarcisRepo> _karcisRepo = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepo = new();
    private readonly Mock<IKomponenRepo> _komponenRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepo = new();
    private readonly Mock<ITarifRepo> _tarifRepo = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IAddBillAppService> _addBillAppService = new();
    private readonly Mock<ITindakanRepo> _tindakanRepo = new();
    private readonly Mock<IJurnalRepo> _jurnalRepo = new();
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepo = new();
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly Mock<IClinicQueueRefreshPublisher> _clinicQueueRefreshPublisher = new();

    private readonly RegJalanUbahKunjunganHandler _sut;

    public RegJalanUbahKunjunganHandlerTest()
    {
        _featureResolver.Setup(x => x.UseResolver).Returns(false);

        _sut = new RegJalanUbahKunjunganHandler(
            _regRepo.Object,
            _regAktifRepo.Object,
            _jadwalPraktekRepo.Object,
            _ppaRepo.Object,
            _antrianRepo.Object,
            _antrianFactory.Object,
            _antrianMapRepo.Object,
            _queueNumberAdapter.Object,
            _layananRepo.Object,
            _karcisRepo.Object,
            _nilaiTarifRepo.Object,
            _komponenRepo.Object,
            _jaminanRepo.Object,
            _tipeJaminanRepo.Object,
            _tarifRepo.Object,
            _trackerRepo.Object,
            _trsBillingRepo.Object,
            _addBillAppService.Object,
            _tindakanRepo.Object,
            _jurnalRepo.Object,
            _mapJaminanJkRepo.Object,
            _featureResolver.Object,
            TestTglJamProvider.Instance,
            clinicQueueRefreshPublisher: _clinicQueueRefreshPublisher.Object);
    }

    [Fact]
    public async Task Handle_WhenJadwalSama_ShouldRetainExistingQueueNumber_AndNotAllocateNewSlot()
    {
        // Arrange: pasien mengubah karcis saja, dokter & poli & jam tetap sama
        var regDate = new DateOnly(2025, 11, 1);
        var layanan = CreateLayanan("LAY01", "Poli Umum");
        var karcisOld = CreateKarcis("KC01", "Karcis Lama", layanan);
        var karcisNew = CreateKarcis("KC02", "Karcis Baru", layanan);
        var dokter = new PpaType("DR01", "dr. Budi", "Dokter Umum", SmfType.Default, GroupSpesialisType.Default, [], [], []);

        var reg = CreateReg("RG00000001", regDate, "DR01", "dr. Budi", "LAY01", "Poli Umum", "KC01", "Karcis Lama");
        var regAktif = RegAktifModel.CreateFromReg(reg);

        var jadwal = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "J01",
            Hari = regDate.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            Dokter = dokter.ToReff(),
            Layanan = layanan.ToReff()
        };

        var tracker = PasienTrackerModel.Create(reg, TestTglJamProvider.Instance.Now);
        var entry = AntrianEntryModel.Create(5, PersonType.Default, tracker, reg.RegId, "REG", TestTglJamProvider.Instance.Now);
        var antrian = new AntrianModel("AN01", regDate, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "TAG1", "desc", new ServicePointType("TAG1", "desc"), [entry], null!);
        var antrianView = new AntrianView(
            "AN01", (int)AntrianStatusEnum.Waiting, 5, "Budi",
            reg.RegId, "REG", regDate.ToDateTime(TimeOnly.MinValue), "TAG1",
            "DR01", "Poli Umum", new TimeOnly(8, 0), new TimeOnly(12, 0));

        var antrianMapHdrOld = new AntrianMapHdrView(
            "MAP01", "J01", dokter.ToReff(), layanan.ToReff(),
            regDate, new TimeOnly(8, 0), new TimeOnly(8, 0));
        var antrianMapOld = AntrianMapModel.CreateFromJadwal(jadwal, regDate) with { AntrianMapId = "MAP01" };

        SetupCommonMocks(reg, regAktif, dokter, layanan, karcisNew, karcisOld, jadwal, antrian, antrianView, tracker, antrianMapHdrOld, antrianMapOld);

        _queueNumberAdapter
            .Setup(x => x.ProjectSourceReffForRegistration(It.IsAny<AntrianMapModel>(), It.IsAny<int>(), It.IsAny<RegModel>()))
            .Returns(true);

        var cmd = new RegJalanUbahKunjunganCmd(
            RegId: "RG00000001",
            LayananId: "LAY01",
            DokterId: "DR01",
            KarcisId: "KC02",
            JamPraktek: "08:00",
            UserId: "USER1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("RG00000001");
        result.NoAntrian.Should().Be(5); // Retained existing queue number!

        // Did NOT allocate new slot from quota
        _queueNumberAdapter.Verify(
            x => x.ReserveForRegistration(It.IsAny<JadwalPraktekType>(), It.IsAny<DateOnly>(), It.IsAny<RegModel>()),
            Times.Never);

        // Did NOT release the slot
        _queueNumberAdapter.Verify(
            x => x.Release(It.IsAny<AntrianMapModel>(), It.IsAny<int>()),
            Times.Never);

        // Updated metadata on existing slot
        _queueNumberAdapter.Verify(
            x => x.ProjectSourceReffForRegistration(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP01"), 5, It.IsAny<RegModel>()),
            Times.Once);

        _antrianMapRepo.Verify(x => x.SaveChanges(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP01")), Times.Once);
        _regRepo.Verify(x => x.SaveChanges(It.Is<RegModel>(r => r.Karcis.KarcisId == "KC02")), Times.Once);
        _clinicQueueRefreshPublisher.Verify(x => x.PublishRefreshHint("LAY01", "DR01", It.IsAny<CancellationToken>()), Times.Once);
        _clinicQueueRefreshPublisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenJadwalBerbeda_ShouldReleaseOldSlot_AndAllocateNewSlotOnTargetSchedule()
    {
        // Arrange: pasien pindah ke dokter lain (DR01 -> DR02)
        var regDate = new DateOnly(2025, 11, 1);
        var layanan = CreateLayanan("LAY01", "Poli Umum");
        var karcis = CreateKarcis("KC01", "Karcis Umum", layanan);

        var reg = CreateReg("RG00000001", regDate, "DR01", "dr. Budi", "LAY01", "Poli Umum", "KC01", "Karcis Umum");
        var regAktif = RegAktifModel.CreateFromReg(reg);

        var dokterOld = new PpaType("DR01", "dr. Budi", "Dokter Umum", SmfType.Default, GroupSpesialisType.Default, [], [], []);
        var dokterNew = new PpaType("DR02", "dr. Ani", "Dokter Spesialis", SmfType.Default, GroupSpesialisType.Default, [], [], []);

        var jadwalOld = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "J01",
            Hari = regDate.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            Dokter = dokterOld.ToReff(),
            Layanan = layanan.ToReff()
        };

        var jadwalNew = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "J02",
            Hari = regDate.DayOfWeek,
            JamMulai = new TimeOnly(14, 0),
            JamSelesai = new TimeOnly(18, 0),
            Dokter = dokterNew.ToReff(),
            Layanan = layanan.ToReff()
        };

        var tracker = PasienTrackerModel.Create(reg, TestTglJamProvider.Instance.Now);
        var entryOld = AntrianEntryModel.Create(5, PersonType.Default, tracker, reg.RegId, "REG", TestTglJamProvider.Instance.Now);
        var antrianOld = new AntrianModel("AN01", regDate, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "TAG1", "desc", new ServicePointType("TAG1", "desc"), [entryOld], null!);
        var antrianView = new AntrianView(
            "AN01", (int)AntrianStatusEnum.Waiting, 5, "Budi",
            reg.RegId, "REG", regDate.ToDateTime(TimeOnly.MinValue), "TAG1",
            "DR01", "Poli Umum", new TimeOnly(8, 0), new TimeOnly(12, 0));

        var antrianMapHdrOld = new AntrianMapHdrView(
            "MAP01", "J01", dokterOld.ToReff(), layanan.ToReff(),
            regDate, new TimeOnly(8, 0), new TimeOnly(8, 0));
        var antrianMapOld = AntrianMapModel.CreateFromJadwal(jadwalOld, regDate) with { AntrianMapId = "MAP01" };

        var antrianMapNew = AntrianMapModel.CreateFromJadwal(jadwalNew, regDate) with { AntrianMapId = "MAP02" };
        var mapDetilNew = AntrianMapDetilModel.Default with { NoUrut = 1 };
        var reservedNew = ReservedQueueNumber.FromMap(antrianMapNew, mapDetilNew);

        var antrianNew = new AntrianModel("AN02", regDate, new TimeOnly(14, 0), new TimeOnly(18, 0),
            "TAG2", "desc", new ServicePointType("TAG2", "desc"), [], null!);

        SetupCommonMocks(reg, regAktif, dokterNew, layanan, karcis, karcis, jadwalNew, antrianOld, antrianView, tracker, antrianMapHdrOld, antrianMapOld);

        // Track invocation sequence to verify Release-Before-Reserve pattern
        var callSequence = new List<string>();

        _queueNumberAdapter.Setup(x => x.Release(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP01"), 5))
            .Callback(() => callSequence.Add("ReleaseOldSlot"));

        _antrianMapRepo.Setup(x => x.SaveChanges(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP01")))
            .Callback(() => callSequence.Add("SaveOldMapRelease"));

        _queueNumberAdapter
            .Setup(x => x.ReserveForRegistration(It.IsAny<JadwalPraktekType>(), It.IsAny<DateOnly>(), It.IsAny<RegModel>()))
            .Callback(() => callSequence.Add("ReserveNewSlot"))
            .Returns(Result<ReservedQueueNumber>.Success(reservedNew));

        _antrianMapRepo.Setup(x => x.SaveChanges(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP02")))
            .Callback(() => callSequence.Add("SaveNewMapReserve"));

        _antrianRepo.Setup(x => x.ListData(regDate)).Returns([]);
        _antrianFactory.Setup(x => x.Create(regDate, It.IsAny<JadwalPraktekType>())).Returns(antrianNew);

        var cmd = new RegJalanUbahKunjunganCmd(
            RegId: "RG00000001",
            LayananId: "LAY01",
            DokterId: "DR02",
            KarcisId: "KC01",
            JamPraktek: "14:00",
            UserId: "USER1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("RG00000001");
        result.NoAntrian.Should().Be(1); // New queue slot allocated!

        // Release-Before-Reserve sequence assertion
        callSequence.Should().ContainInOrder("ReleaseOldSlot", "SaveOldMapRelease", "ReserveNewSlot", "SaveNewMapReserve");

        _queueNumberAdapter.Verify(x => x.Release(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP01"), 5), Times.Once);
        _antrianMapRepo.Verify(x => x.SaveChanges(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP01")), Times.Once);
        _queueNumberAdapter.Verify(x => x.ReserveForRegistration(It.IsAny<JadwalPraktekType>(), It.IsAny<DateOnly>(), It.IsAny<RegModel>()), Times.Once);
        _antrianMapRepo.Verify(x => x.SaveChanges(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP02")), Times.Once);
        _antrianRepo.Verify(x => x.SaveChanges(antrianNew), Times.Once);
        _clinicQueueRefreshPublisher.Verify(x => x.PublishRefreshHint("LAY01", "DR02", It.IsAny<CancellationToken>()), Times.Once);
        _clinicQueueRefreshPublisher.Verify(x => x.PublishRefreshHint("LAY01", "DR01", It.IsAny<CancellationToken>()), Times.Once);
        _clinicQueueRefreshPublisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenTransferPoliBerbeda_ShouldPublishHintsForBothDestinationAndOriginPoli()
    {
        // Arrange: pasien pindah poli (LAY01 -> LAY02), dokter sama (DR01)
        var regDate = new DateOnly(2025, 11, 1);
        var layananOld = CreateLayanan("LAY01", "Poli Umum");
        var layananNew = CreateLayanan("LAY02", "Poli Gigi");
        var karcisOld = CreateKarcis("KC01", "Karcis Umum", layananOld);
        var karcisNew = CreateKarcis("KC02", "Karcis Gigi", layananNew);

        var reg = CreateReg("RG00000001", regDate, "DR01", "dr. Budi", "LAY01", "Poli Umum", "KC01", "Karcis Umum");
        var regAktif = RegAktifModel.CreateFromReg(reg);

        var dokter = new PpaType("DR01", "dr. Budi", "Dokter Umum", SmfType.Default, GroupSpesialisType.Default, [], [], []);

        var jadwalOld = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "J01",
            Hari = regDate.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            Dokter = dokter.ToReff(),
            Layanan = layananOld.ToReff()
        };

        var jadwalNew = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "J02",
            Hari = regDate.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            Dokter = dokter.ToReff(),
            Layanan = layananNew.ToReff()
        };

        var tracker = PasienTrackerModel.Create(reg, TestTglJamProvider.Instance.Now);
        var entryOld = AntrianEntryModel.Create(5, PersonType.Default, tracker, reg.RegId, "REG", TestTglJamProvider.Instance.Now);
        var antrianOld = new AntrianModel("AN01", regDate, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "TAG1", "desc", new ServicePointType("TAG1", "desc"), [entryOld], null!);
        var antrianView = new AntrianView(
            "AN01", (int)AntrianStatusEnum.Waiting, 5, "Budi",
            reg.RegId, "REG", regDate.ToDateTime(TimeOnly.MinValue), "TAG1",
            "DR01", "Poli Umum", new TimeOnly(8, 0), new TimeOnly(12, 0));

        var antrianMapHdrOld = new AntrianMapHdrView(
            "MAP01", "J01", dokter.ToReff(), layananOld.ToReff(),
            regDate, new TimeOnly(8, 0), new TimeOnly(8, 0));
        var antrianMapOld = AntrianMapModel.CreateFromJadwal(jadwalOld, regDate) with { AntrianMapId = "MAP01" };

        var antrianMapNew = AntrianMapModel.CreateFromJadwal(jadwalNew, regDate) with { AntrianMapId = "MAP02" };
        var mapDetilNew = AntrianMapDetilModel.Default with { NoUrut = 1 };
        var reservedNew = ReservedQueueNumber.FromMap(antrianMapNew, mapDetilNew);

        var antrianNew = new AntrianModel("AN02", regDate, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "TAG2", "desc", new ServicePointType("TAG2", "desc"), [], null!);

        SetupCommonMocks(reg, regAktif, dokter, layananNew, karcisNew, karcisOld, jadwalNew, antrianOld, antrianView, tracker, antrianMapHdrOld, antrianMapOld);

        _queueNumberAdapter
            .Setup(x => x.ReserveForRegistration(It.IsAny<JadwalPraktekType>(), It.IsAny<DateOnly>(), It.IsAny<RegModel>()))
            .Returns(Result<ReservedQueueNumber>.Success(reservedNew));

        _antrianRepo.Setup(x => x.ListData(regDate)).Returns([]);
        _antrianFactory.Setup(x => x.Create(regDate, It.IsAny<JadwalPraktekType>())).Returns(antrianNew);

        var cmd = new RegJalanUbahKunjunganCmd(
            RegId: "RG00000001",
            LayananId: "LAY02",
            DokterId: "DR01",
            KarcisId: "KC02",
            JamPraktek: "08:00",
            UserId: "USER1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("RG00000001");
        _clinicQueueRefreshPublisher.Verify(x => x.PublishRefreshHint("LAY02", "DR01", It.IsAny<CancellationToken>()), Times.Once);
        _clinicQueueRefreshPublisher.Verify(x => x.PublishRefreshHint("LAY01", "DR01", It.IsAny<CancellationToken>()), Times.Once);
        _clinicQueueRefreshPublisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenJadwalBerbeda_DueToDifferentJamPraktek_ShouldReleaseOldSlot_AndAllocateNewSlotOnTargetSchedule()
    {
        // Arrange: dokter & layanan sama, namun jam praktek berbeda (08:00 -> 14:00)
        var regDate = new DateOnly(2025, 11, 1);
        var layanan = CreateLayanan("LAY01", "Poli Umum");
        var karcis = CreateKarcis("KC01", "Karcis Umum", layanan);

        var reg = CreateReg("RG00000001", regDate, "DR01", "dr. Budi", "LAY01", "Poli Umum", "KC01", "Karcis Umum");
        var regAktif = RegAktifModel.CreateFromReg(reg);

        var dokter = new PpaType("DR01", "dr. Budi", "Dokter Umum", SmfType.Default, GroupSpesialisType.Default, [], [], []);

        var jadwalOld = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "J01",
            Hari = regDate.DayOfWeek,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            Dokter = dokter.ToReff(),
            Layanan = layanan.ToReff()
        };

        var jadwalNew = JadwalPraktekType.Default with
        {
            JadwalPraktekId = "J02",
            Hari = regDate.DayOfWeek,
            JamMulai = new TimeOnly(14, 0),
            JamSelesai = new TimeOnly(18, 0),
            Dokter = dokter.ToReff(),
            Layanan = layanan.ToReff()
        };

        var tracker = PasienTrackerModel.Create(reg, TestTglJamProvider.Instance.Now);
        var entryOld = AntrianEntryModel.Create(3, PersonType.Default, tracker, reg.RegId, "REG", TestTglJamProvider.Instance.Now);
        var antrianOld = new AntrianModel("AN01", regDate, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "TAG1", "desc", new ServicePointType("TAG1", "desc"), [entryOld], null!);
        var antrianView = new AntrianView(
            "AN01", (int)AntrianStatusEnum.Waiting, 3, "Budi",
            reg.RegId, "REG", regDate.ToDateTime(TimeOnly.MinValue), "TAG1",
            "DR01", "Poli Umum", new TimeOnly(8, 0), new TimeOnly(12, 0));

        var antrianMapHdrOld = new AntrianMapHdrView(
            "MAP01", "J01", dokter.ToReff(), layanan.ToReff(),
            regDate, new TimeOnly(8, 0), new TimeOnly(8, 0));
        var antrianMapOld = AntrianMapModel.CreateFromJadwal(jadwalOld, regDate) with { AntrianMapId = "MAP01" };

        var antrianMapNew = AntrianMapModel.CreateFromJadwal(jadwalNew, regDate) with { AntrianMapId = "MAP02" };
        var mapDetilNew = AntrianMapDetilModel.Default with { NoUrut = 1 };
        var reservedNew = ReservedQueueNumber.FromMap(antrianMapNew, mapDetilNew);

        var antrianNew = new AntrianModel("AN02", regDate, new TimeOnly(14, 0), new TimeOnly(18, 0),
            "TAG2", "desc", new ServicePointType("TAG2", "desc"), [], null!);

        _jadwalPraktekRepo.Setup(x => x.ListData(It.IsAny<IPpaKey>())).Returns([jadwalOld, jadwalNew]);
        SetupCommonMocks(reg, regAktif, dokter, layanan, karcis, karcis, null, antrianOld, antrianView, tracker, antrianMapHdrOld, antrianMapOld);

        _queueNumberAdapter
            .Setup(x => x.ReserveForRegistration(It.IsAny<JadwalPraktekType>(), It.IsAny<DateOnly>(), It.IsAny<RegModel>()))
            .Returns(Result<ReservedQueueNumber>.Success(reservedNew));

        _antrianRepo.Setup(x => x.ListData(regDate)).Returns([]);
        _antrianFactory.Setup(x => x.Create(regDate, It.IsAny<JadwalPraktekType>())).Returns(antrianNew);

        var cmd = new RegJalanUbahKunjunganCmd(
            RegId: "RG00000001",
            LayananId: "LAY01",
            DokterId: "DR01",
            KarcisId: "KC01",
            JamPraktek: "14:00",
            UserId: "USER1");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.NoAntrian.Should().Be(1);

        _queueNumberAdapter.Verify(x => x.Release(It.Is<AntrianMapModel>(m => m.AntrianMapId == "MAP01"), 3), Times.Once);
        _queueNumberAdapter.Verify(x => x.ReserveForRegistration(It.IsAny<JadwalPraktekType>(), It.IsAny<DateOnly>(), It.IsAny<RegModel>()), Times.Once);
        _clinicQueueRefreshPublisher.Verify(x => x.PublishRefreshHint("LAY01", "DR01", It.IsAny<CancellationToken>()), Times.Once);
        _clinicQueueRefreshPublisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenRegisterNotFoundOrInactive_ThrowsException()
    {
        // Arrange
        _regAktifRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe<RegAktifModel>.None);

        var cmd = new RegJalanUbahKunjunganCmd(
            RegId: "RG_NOT_FOUND",
            LayananId: "LAY01",
            DokterId: "DR01",
            KarcisId: "KC01",
            JamPraktek: "08:00",
            UserId: "USER1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Register tidak ditemukan atau sudah tidak aktif*");
    }

    private void SetupCommonMocks(
        RegModel reg,
        RegAktifModel regAktif,
        PpaType dokter,
        LayananType layanan,
        KarcisType karcisNew,
        KarcisType karcisOld,
        JadwalPraktekType? jadwalPraktek,
        AntrianModel antrianOld,
        AntrianView antrianView,
        PasienTrackerModel tracker,
        AntrianMapHdrView antrianMapHdrOld,
        AntrianMapModel antrianMapOld)
    {
        _regAktifRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(regAktif));
        _regRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(reg));
        _ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(dokter));
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        _karcisRepo.Setup(x => x.LoadEntity(It.Is<IKarcisKey>(k => k.KarcisId == karcisNew.KarcisId))).Returns(MayBe.From(karcisNew));
        _karcisRepo.Setup(x => x.LoadEntity(It.Is<IKarcisKey>(k => k.KarcisId == karcisOld.KarcisId))).Returns(MayBe.From(karcisOld));

        _tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>())).Returns(MayBe.From(TipeJaminanType.Default));
        _jaminanRepo.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>())).Returns(MayBe.From(JaminanType.Default));
        _mapJaminanJkRepo.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>())).Returns(MayBe.From(MapJaminanJkType.Default));

        if (jadwalPraktek is not null)
        {
            _jadwalPraktekRepo.Setup(x => x.ListData(It.IsAny<IPpaKey>())).Returns([jadwalPraktek]);
        }

        var queDate = reg.RegDate.ToDateTime(TimeOnly.MinValue);
        _antrianRepo.Setup(x => x.ListData(queDate)).Returns([antrianView]);
        _antrianRepo.Setup(x => x.LoadEntity(It.Is<IAntrianKey>(k => k.AntrianId == antrianOld.AntrianId))).Returns(MayBe.From(antrianOld));
        _trackerRepo.Setup(x => x.LoadEntity(It.Is<IPasienTrackerKey>(k => k.PasienTrackerId == tracker.PasienTrackerId))).Returns(MayBe.From(tracker));

        _antrianMapRepo.Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.Is<IPpaKey>(k => k.PpaId == reg.Dokter.PpaId), reg.RegDate))
            .Returns([antrianMapHdrOld]);
        _antrianMapRepo.Setup(x => x.LoadEntity(It.Is<IAntrianMapKey>(k => k.AntrianMapId == antrianMapOld.AntrianMapId)))
            .Returns(MayBe.From(antrianMapOld));

        _tindakanRepo.Setup(x => x.ListData(It.IsAny<IRegKey>())).Returns([]);
        _addBillAppService.Setup(x => x.FromReg(It.IsAny<RegModel>(), It.IsAny<KarcisType>(), It.IsAny<JaminanType>(), It.IsAny<PpaType>(), It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()))
            .Returns(TrsBillType.Default);
    }

    private static LayananType CreateLayanan(string id, string name) =>
        LayananType.Default with
        {
            LayananId = id,
            LayananName = name,
            InstalasiDk = InstalasiDkType.RawatJalan
        };

    private static KarcisType CreateKarcis(string id, string name, LayananType layanan) =>
        new(
            karcisId: id,
            karcisName: name,
            isAktif: true,
            instalasiDk: InstalasiDkType.RawatJalan,
            rekapCetak: new RekapCetakReff("-", "-"),
            defaultTarif: TarifType.Default.ToReff(),
            listKomponen: [],
            listLayanan: [layanan.ToReff()]);

    private static RegModel CreateReg(
        string regId, DateOnly regDate, string dokterId, string dokterName,
        string layananId, string layananName, string karcisId, string karcisName) => new(
        regId,
        regDate,
        new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
        AuditInfoType.Default,
        AuditInfoType.Default,
        AuditInfoType.Default,
        JenisRegEnum.RegJalan,
        new PasienReff("P01", "Budi", new DateOnly(1990, 1, 1), "L"),
        TipeJaminanType.Default.ToReff(),
        PolisModel.Default.ToReff(),
        KelasType.Default.ToReff(),
        CaraMasukDkType.Default,
        RujukanType.Default.ToReff(),
        new PpaReff(dokterId, dokterName),
        new LayananReff(layananId, layananName),
        new KarcisReff(karcisId, karcisName),
        RegEligibilityType.Default,
        []);
}
