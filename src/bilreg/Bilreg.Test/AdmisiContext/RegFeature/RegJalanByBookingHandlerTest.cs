using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Application.AdmisiContext.RemoteCetakFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RemotCetakFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegJalanByBookingHandlerTest
{
    private readonly Mock<IBookingRepo> _bookingRepo = new();
    private readonly Mock<IPasienRepo> _pasienRepo = new();
    private readonly Mock<IRegFactory> _regFactory = new();
    private readonly Mock<IPpaRepo> _dokterRepo = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<IKarcisRepo> _karcisRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepo = new();
    private readonly Mock<ICaraMasukDkRepo> _caraMasukDkRepo = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepo = new();
    private readonly Mock<IPolisRepo> _polisRepo = new();
    private readonly Mock<IRujukanRepo> _rujukanRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();
    private readonly Mock<ITarifRepo> _tarifRepo = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepo = new();
    private readonly Mock<ITipeTarifRepo> _tipeTarifRepo = new();
    private readonly Mock<ITindakanRepo> _tindakanRepo = new();
    private readonly Mock<IKomponenRepo> _komponenRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IAddBillAppService> _addBillAppService = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<IAntrianFactory> _antrianFactory = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepo = new();
    private readonly Mock<IJurnalRepo> _jurnalRepo = new();
    private readonly Mock<IRemoteCetakRepo> _remoteCetakRepo = new();
    private readonly Mock<IGetAppSettingService> _getAppSettingSvc = new();
    private readonly Mock<IEmrAntrianOutboundQueueRepo> _emrQueueRepo = new();
    private readonly Mock<IAntrianMapRepo> _antrianMapRepo = new();
    private readonly Mock<IQueueNumberCompatibilityAdapter> _queueNumberAdapter = new();
    private readonly Mock<IAdmissionServicePointResolver> _admissionServicePointResolver = new();
    private readonly Mock<IAdmisiEventPublisher> _publisher = new();

    private readonly RegJalanByBookingHandler _sut;

    public RegJalanByBookingHandlerTest()
    {
        var emrEnqueue = new EmrAntrianOutboundEnqueueService(_emrQueueRepo.Object);
        _sut = new RegJalanByBookingHandler(
            _bookingRepo.Object,
            _pasienRepo.Object,
            _regFactory.Object,
            _dokterRepo.Object,
            _layananRepo.Object,
            _karcisRepo.Object,
            _regRepo.Object,
            _regAktifRepo.Object,
            _caraMasukDkRepo.Object,
            _tipeJaminanRepo.Object,
            _polisRepo.Object,
            _rujukanRepo.Object,
            _jaminanRepo.Object,
            _tarifRepo.Object,
            _nilaiTarifRepo.Object,
            _tipeTarifRepo.Object,
            _tindakanRepo.Object,
            _komponenRepo.Object,
            _trsBillingRepo.Object,
            _addBillAppService.Object,
            _antrianRepo.Object,
            _antrianFactory.Object,
            _trackerRepo.Object,
            _mapJaminanJkRepo.Object,
            _jurnalRepo.Object,
            _remoteCetakRepo.Object,
            _getAppSettingSvc.Object,
            emrEnqueue,
            _antrianMapRepo.Object,
            _queueNumberAdapter.Object,
            TestTglJamProvider.Instance,
            _admissionServicePointResolver.Object,
            _publisher.Object);
    }

    [Fact]
    public async Task Handle_WhenBookingRegistrationSuccess_ThenPublishesRajalCreatedEvent()
    {
        // Arrange
        var cmd = CreateCommand();
        SetupSuccessfulBooking(cmd, "RG00000001");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.RegId.Should().Be("RG00000001");
        result.NoAntrian.Should().Be(1);
        _publisher.Verify(x => x.PublishRajalCreatedAsync("RG00000001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccursBeforeTransactionCommit_ThenEventIsNotPublished()
    {
        // Arrange: booking already registered throws InvalidOperationException before commit
        var cmd = CreateCommand();
        var person = new PersonInfoType("BUDI", new DateOnly(1990, 1, 1), "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var booking = new BookingModel(
            cmd.BookingId,
            DateTime.Now,
            person,
            "P01",
            new RegReff("RG_EXISTING", "P01", "BUDI"),
            DateOnly.FromDateTime(TestTglJamProvider.Instance.Now),
            new TimeOnly(8, 0),
            LayananType.Default.ToReff(),
            PpaType.Default.ToReff(),
            1,
            AuditTrailType.Default,
            ExtAppReffType.Default,
            CoverageInfoType.Default);

        _bookingRepo.Setup(x => x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(booking));

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _publisher.Verify(x => x.PublishRajalCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepoThrowsDuringCommit_ThenEventIsNotPublished()
    {
        // Arrange: setup successful booking but regRepo throws on SaveChanges inside transaction
        var cmd = CreateCommand();
        SetupSuccessfulBooking(cmd, "RG00000001");
        _regRepo.Setup(x => x.SaveChanges(It.IsAny<RegModel>()))
            .Throws(new InvalidOperationException("DB error"));

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _publisher.Verify(x => x.PublishRajalCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRabbitMqBrokerFails_ThenRegistrationStillSucceedsWithoutThrowing()
    {
        // Arrange
        var cmd = CreateCommand();
        SetupSuccessfulBooking(cmd, "RG00000001");

        var failingBus = new Mock<MassTransit.IBus>();
        failingBus.Setup(x => x.Publish(It.IsAny<MyHospital.MsgContract.Billing.AdmisiEvents.RegRajalCreatedNotifEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ broker connection refused"));
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<Bilreg.Infrastructure.AdmisiContext.RegFeature.AdmisiEventPublisher>>();
        var realPublisher = new Bilreg.Infrastructure.AdmisiContext.RegFeature.AdmisiEventPublisher(failingBus.Object, mockLogger.Object);

        var emrEnqueue = new EmrAntrianOutboundEnqueueService(_emrQueueRepo.Object);
        var sutWithResilientPublisher = new RegJalanByBookingHandler(
            _bookingRepo.Object,
            _pasienRepo.Object,
            _regFactory.Object,
            _dokterRepo.Object,
            _layananRepo.Object,
            _karcisRepo.Object,
            _regRepo.Object,
            _regAktifRepo.Object,
            _caraMasukDkRepo.Object,
            _tipeJaminanRepo.Object,
            _polisRepo.Object,
            _rujukanRepo.Object,
            _jaminanRepo.Object,
            _tarifRepo.Object,
            _nilaiTarifRepo.Object,
            _tipeTarifRepo.Object,
            _tindakanRepo.Object,
            _komponenRepo.Object,
            _trsBillingRepo.Object,
            _addBillAppService.Object,
            _antrianRepo.Object,
            _antrianFactory.Object,
            _trackerRepo.Object,
            _mapJaminanJkRepo.Object,
            _jurnalRepo.Object,
            _remoteCetakRepo.Object,
            _getAppSettingSvc.Object,
            emrEnqueue,
            _antrianMapRepo.Object,
            _queueNumberAdapter.Object,
            TestTglJamProvider.Instance,
            _admissionServicePointResolver.Object,
            realPublisher);

        // Act
        var result = await sutWithResilientPublisher.Handle(cmd, CancellationToken.None);

        // Assert: Registration succeeds, RegId returned, broker error isolated and logged
        result.Should().NotBeNull();
        result.RegId.Should().Be("RG00000001");
        failingBus.Verify(x => x.Publish(It.IsAny<MyHospital.MsgContract.Billing.AdmisiEvents.RegRajalCreatedNotifEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        mockLogger.Verify(x => x.Log(
            Microsoft.Extensions.Logging.LogLevel.Error,
            It.IsAny<Microsoft.Extensions.Logging.EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    private static RegJalanByBookingCmd CreateCommand() =>
        new("BK01", "USER1", "KC01", "CM01", "RUJ01", "TJ01", "PJ01");

    private void SetupSuccessfulBooking(RegJalanByBookingCmd cmd, string regId)
    {
        var tglBerobat = DateOnly.FromDateTime(TestTglJamProvider.Instance.Now);
        var jamPraktek = new TimeOnly(8, 0);

        var person = new PersonInfoType("BUDI", new DateOnly(1990, 1, 1), "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var ktp = new KtpType("3201010101900001", AlamatType.Default, "01", "01", KelurahanType.Default);
        var pasien = new PasienModel("P01", person, "Budi", "Jakarta", GolDarahType.Default, "-",
            ktp, KelurahanType.Default, IdentitasType.Default, [],
            PasienKeluargaType.Default, AgamaType.Default, SukuType.Default,
            StatusKawinDkType.Default, PendidikanDkType.Default, PekerjaanDkType.Default,
            DateTime.Now, true);

        var layanan = LayananType.Default with { LayananId = "LAY01", LayananName = "Poli Umum" };
        var dokter = new PpaType("DR01", "dr. Budi", "Dokter Umum", SmfType.Default, GroupSpesialisType.Default, [], [], []);

        var booking = new BookingModel(
            cmd.BookingId,
            DateTime.Now,
            person,
            pasien.PasienId,
            RegModel.Default.ToReff(),
            tglBerobat,
            jamPraktek,
            layanan.ToReff(),
            dokter.ToReff(),
            1,
            AuditTrailType.Default,
            ExtAppReffType.Default,
            CoverageInfoType.Default);

        _bookingRepo.Setup(x => x.LoadEntity(It.IsAny<IBookingKey>())).Returns(MayBe.From(booking));
        _pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(pasien));
        _dokterRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(dokter));
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));

        var karcis = KarcisType.Default with { KarcisId = cmd.KarcisId, KarcisName = "Karcis Umum" };
        _karcisRepo.Setup(x => x.LoadEntity(It.IsAny<IKarcisKey>())).Returns(MayBe.From(karcis));

        var caraMasuk = CaraMasukDkType.DatangSendiri;
        _caraMasukDkRepo.Setup(x => x.LoadEntity(It.IsAny<ICaraMasukDkKey>())).Returns(MayBe.From(caraMasuk));

        var tipeJaminan = TipeJaminanType.BayarSendiri;
        _tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>())).Returns(MayBe.From(tipeJaminan));
        _jaminanRepo.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>())).Returns(MayBe.From(JaminanType.Default));

        _regAktifRepo.Setup(x => x.IsPasienAktif(It.IsAny<IPasienKey>())).Returns(false);
        _regRepo.Setup(x => x.IsPasienAktifReg(It.IsAny<IPasienKey>())).Returns(false);

        var reg = new RegModel(
            regId,
            tglBerobat,
            new AuditInfoType(cmd.UserId, TestTglJamProvider.Instance.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            pasien.ToReff(),
            tipeJaminan.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            caraMasuk,
            RujukanType.Default.ToReff(),
            dokter.ToReff(),
            layanan.ToReff(),
            karcis.ToReff(),
            RegEligibilityType.Default,
            []);
        _regFactory.Setup(x => x.CreateRegRajal(
            It.IsAny<PasienModel>(), It.IsAny<AuditInfoType>(), It.IsAny<TipeJaminanType>(),
            It.IsAny<PolisModel>(), It.IsAny<CaraMasukDkType>(), It.IsAny<RujukanType>(),
            It.IsAny<PpaType>(), It.IsAny<LayananType>(), It.IsAny<KarcisType>(), It.IsAny<string>()))
            .Returns(reg);

        var tracker = PasienTrackerModel.Create(booking, TestTglJamProvider.Instance.Now);
        var entry = AntrianEntryModel.Create(1, person, tracker, booking.BookingId, "BOOKING", TestTglJamProvider.Instance.Now);
        var sequenceTag = AntrianModel.GenSequenceTag(tglBerobat, jamPraktek, dokter);
        var queue = new AntrianModel("AN01", tglBerobat, jamPraktek, new TimeOnly(12, 0),
            sequenceTag, "Poli Umum", new ServicePointType("TAG1", "desc"), [entry], null!);
        var header = new AntrianHeaderView("AN01", "Poli Umum", tglBerobat, jamPraktek, sequenceTag);

        _antrianRepo.Setup(x => x.ListData(tglBerobat)).Returns([header]);
        _antrianRepo.Setup(x => x.LoadEntity(It.Is<IAntrianKey>(k => k.AntrianId == "AN01"))).Returns(MayBe.From(queue));
        _trackerRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>())).Returns(MayBe.From(tracker));

        _antrianMapRepo.Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), It.IsAny<DateOnly>()))
            .Returns(Enumerable.Empty<AntrianMapHdrView>());
        _queueNumberAdapter.Setup(x => x.ProjectSourceReffForRegistration(It.IsAny<AntrianMapModel>(), It.IsAny<int>(), It.IsAny<RegModel>()))
            .Returns(false);

        _addBillAppService.Setup(x => x.FromReg(It.IsAny<RegModel>(), It.IsAny<KarcisType>(), It.IsAny<JaminanType>(), It.IsAny<PpaType>(), It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()))
            .Returns(TrsBillType.Default with { Reg = reg.ToReff() });
        _addBillAppService.Setup(x => x.FromTindakan(It.IsAny<TindakanModel>(), It.IsAny<RegModel>(), It.IsAny<TarifType>(), It.IsAny<JaminanType>(), It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()))
            .Returns(TrsBillType.Default with { Reg = reg.ToReff() });

        _getAppSettingSvc.Setup(x => x.Execute())
            .Returns(new AppSetting(new RemoteCetakSetting("OFF")));

        _publisher.Setup(x => x.PublishRajalCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
