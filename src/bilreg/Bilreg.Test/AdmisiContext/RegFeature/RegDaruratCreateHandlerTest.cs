using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
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
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegDaruratCreateHandlerTest
{
    private readonly Mock<IPasienRepo> _pasienRepo = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepo = new();
    private readonly Mock<ICaraMasukDkRepo> _caraMasukDkRepo = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<IKarcisRepo> _karcisRepo = new();
    private readonly Mock<IPolisRepo> _polisRepo = new();

    private readonly Mock<IRegFactory> _regFactory = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepo = new();

    private readonly Mock<IJaminanRepo> _jaminanRepo = new();
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepo = new();
    private readonly Mock<ITindakanRepo> _tindakanRepo = new();
    private readonly Mock<IKomponenRepo> _komponenRepo = new();
    private readonly Mock<ITarifRepo> _tarifRepo = new();

    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IAddBillAppService> _addBillAppService = new();
    private readonly Mock<IMapJaminanJkRepo> _mapJaminanJkRepo = new();
    private readonly Mock<IJurnalRepo> _jurnalRepo = new();
    private readonly Mock<IAddAntrianEmrByRegService> _addAntrianEmrByRegService = new();
    private readonly Mock<IAdmisiEventPublisher> _publisher = new();

    private readonly RegDaruratCreateHandler _sut;

    public RegDaruratCreateHandlerTest()
    {
        _sut = new RegDaruratCreateHandler(
            _pasienRepo.Object,
            _tipeJaminanRepo.Object,
            _caraMasukDkRepo.Object,
            _layananRepo.Object,
            _ppaRepo.Object,
            _karcisRepo.Object,
            _polisRepo.Object,
            _regFactory.Object,
            _regRepo.Object,
            _regAktifRepo.Object,
            _jaminanRepo.Object,
            _nilaiTarifRepo.Object,
            _tindakanRepo.Object,
            _komponenRepo.Object,
            _tarifRepo.Object,
            _trsBillingRepo.Object,
            _addBillAppService.Object,
            _mapJaminanJkRepo.Object,
            _jurnalRepo.Object,
            _addAntrianEmrByRegService.Object,
            TestTglJamProvider.Instance,
            _publisher.Object);
    }

    [Fact]
    public async Task Handle_WhenDaruratRegistrationSuccess_ThenPublishesRajalCreatedEvent()
    {
        // Arrange
        var cmd = CreateCommand();
        SetupSuccessfulDarurat(cmd, "RG00000001");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.RegId.Should().Be("RG00000001");
        _publisher.Verify(x => x.PublishRajalCreatedAsync("RG00000001", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccursBeforeTransactionCommit_ThenEventIsNotPublished()
    {
        // Arrange: pasien repo throws exception before registration persistence/commit
        var cmd = CreateCommand();
        _pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Throws(new KeyNotFoundException("Pasien not found"));

        // Act
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _publisher.Verify(x => x.PublishRajalCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepoThrowsDuringCommit_ThenEventIsNotPublished()
    {
        // Arrange: setup successful registration but regRepo throws on SaveChanges inside transaction
        var cmd = CreateCommand();
        SetupSuccessfulDarurat(cmd, "RG00000001");
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
        SetupSuccessfulDarurat(cmd, "RG00000001");

        var failingBus = new Mock<MassTransit.IBus>();
        failingBus.Setup(x => x.Publish(It.IsAny<MyHospital.MsgContract.Billing.AdmisiEvents.RegRajalCreatedNotifEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ broker connection refused"));
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<Bilreg.Infrastructure.AdmisiContext.RegFeature.AdmisiEventPublisher>>();
        var realPublisher = new Bilreg.Infrastructure.AdmisiContext.RegFeature.AdmisiEventPublisher(failingBus.Object, mockLogger.Object);

        var sutWithResilientPublisher = new RegDaruratCreateHandler(
            _pasienRepo.Object,
            _tipeJaminanRepo.Object,
            _caraMasukDkRepo.Object,
            _layananRepo.Object,
            _ppaRepo.Object,
            _karcisRepo.Object,
            _polisRepo.Object,
            _regFactory.Object,
            _regRepo.Object,
            _regAktifRepo.Object,
            _jaminanRepo.Object,
            _nilaiTarifRepo.Object,
            _tindakanRepo.Object,
            _komponenRepo.Object,
            _tarifRepo.Object,
            _trsBillingRepo.Object,
            _addBillAppService.Object,
            _mapJaminanJkRepo.Object,
            _jurnalRepo.Object,
            _addAntrianEmrByRegService.Object,
            TestTglJamProvider.Instance,
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

    private static RegDaruratCreateCmd CreateCommand() =>
        new("P01", "USER1", "TJ01", "CM01", "DR01", "LAY01", "KC01", "PJ01");

    private void SetupSuccessfulDarurat(RegDaruratCreateCmd cmd, string regId)
    {
        var person = new PersonInfoType("BUDI", new DateOnly(1990, 1, 1), "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var ktp = new KtpType("3201010101900001", AlamatType.Default, "01", "01", KelurahanType.Default);
        var pasien = new PasienModel(cmd.PasienId, person, "Budi", "Jakarta", GolDarahType.Default, "-",
            ktp, KelurahanType.Default, IdentitasType.Default, [],
            PasienKeluargaType.Default, AgamaType.Default, SukuType.Default,
            StatusKawinDkType.Default, PendidikanDkType.Default, PekerjaanDkType.Default,
            DateTime.Now, true);
        _pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>())).Returns(MayBe.From(pasien));
        _regAktifRepo.Setup(x => x.IsPasienAktif(It.IsAny<IPasienKey>())).Returns(false);

        var tipeJaminan = TipeJaminanType.BayarSendiri;
        _tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>())).Returns(MayBe.From(tipeJaminan));
        _jaminanRepo.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>())).Returns(MayBe.From(JaminanType.Default));

        var caraMasuk = CaraMasukDkType.DatangSendiri;
        _caraMasukDkRepo.Setup(x => x.LoadEntity(It.IsAny<ICaraMasukDkKey>())).Returns(MayBe.From(caraMasuk));

        var layanan = LayananType.Default with { LayananId = cmd.LayananId, LayananName = "IGD" };
        _layananRepo.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));

        var dokter = new PpaType(cmd.DokterId, "dr. Budi", "Dokter IGD", SmfType.Default, GroupSpesialisType.Default, [], [], []);
        _ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(dokter));

        var karcis = KarcisType.Default with { KarcisId = cmd.KarcisId, KarcisName = "Karcis IGD", DefaultTarif = TarifType.Default.ToReff() };
        _karcisRepo.Setup(x => x.LoadEntity(It.IsAny<IKarcisKey>())).Returns(MayBe.From(karcis));

        var tglBerobat = DateOnly.FromDateTime(TestTglJamProvider.Instance.Now);
        var reg = new RegModel(
            regId,
            tglBerobat,
            new AuditInfoType(cmd.UserId, TestTglJamProvider.Instance.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.Darurat,
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
        _regFactory.Setup(x => x.CreateRegDarurat(
            It.IsAny<PasienModel>(), It.IsAny<AuditInfoType>(), It.IsAny<TipeJaminanType>(),
            It.IsAny<PolisModel>(), It.IsAny<CaraMasukDkType>(), It.IsAny<PpaType>(),
            It.IsAny<LayananType>(), It.IsAny<KarcisType>(), It.IsAny<string>()))
            .Returns(reg);

        _addBillAppService.Setup(x => x.FromReg(It.IsAny<RegModel>(), It.IsAny<KarcisType>(), It.IsAny<JaminanType>(), It.IsAny<PpaType>(), It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()))
            .Returns(TrsBillType.Default with { Reg = reg.ToReff() });
        _addBillAppService.Setup(x => x.FromTindakan(It.IsAny<TindakanModel>(), It.IsAny<RegModel>(), It.IsAny<TarifType>(), It.IsAny<JaminanType>(), It.IsAny<IEnumerable<KomponenType>>(), It.IsAny<DateTime>()))
            .Returns(TrsBillType.Default with { Reg = reg.ToReff() });

        _publisher.Setup(x => x.PublishRajalCreatedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
