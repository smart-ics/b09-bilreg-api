using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegJalanBatalHandlerTest
{
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepo = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();
    private readonly Mock<ITindakanRepo> _tdkRepo = new();
    private readonly Mock<ITrsBillingRepo> _billingRepo = new();
    private readonly Mock<IAntrianMapRepo> _antrianMapRepo = new();
    private readonly Mock<IPasienTrackerRepo> _trackerRepo = new();
    private readonly Mock<IJurnalRepo> _jurnalRepo = new();
    private readonly Mock<IDashboardEmrRemoveRegService> _dashboard = new();
    private readonly Mock<IBookingRepo> _bookingRepo = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly RegJalanBatalHandler _sut;

    public RegJalanBatalHandlerTest()
    {
        _sut = new RegJalanBatalHandler(
            _regRepo.Object,
            _regAktifRepo.Object,
            _antrianRepo.Object,
            _tdkRepo.Object,
            _billingRepo.Object,
            _antrianMapRepo.Object,
            _trackerRepo.Object,
            _jurnalRepo.Object,
            _dashboard.Object,
            _bookingRepo.Object,
            _auditRepo.Object,
            TestTglJamProvider.Instance);
    }

    [Fact]
    public async Task Handle_WhenRegistrationCancelled_ThenRetainsTrackerAndAppendsRegisterCancelled()
    {
        // Arrange
        var regDate = new DateOnly(2025, 11, 1);
        var reg = CreateReg(regDate);
        var booking = CreateBooking(regDate);
        var tracker = PasienTrackerModel.Create(booking, new DateTime(2025, 10, 10, 9, 0, 0));
        var stableId = tracker.PasienTrackerId;

        var entry = AntrianEntryModel.Create(1, tracker.Person, tracker, reg.RegId, "REG",
            new DateTime(2025, 11, 1, 8, 0, 0));
        var antrian = new AntrianModel("AN1", regDate, new TimeOnly(8, 0), new TimeOnly(12, 0),
            "TAG1", "desc", new ServicePointType("TAG1", "desc"), [entry], null!);
        var antrianView = new AntrianView(
            "AN1", (int)AntrianStatusEnum.Waiting, 1, tracker.Person.PersonName,
            reg.RegId, "REG", regDate.ToDateTime(TimeOnly.MinValue), "TAG1",
            reg.Dokter.PpaId, "desc", new TimeOnly(8, 0), new TimeOnly(12, 0));

        _regRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(reg));
        _bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns(Enumerable.Empty<BookingView>());
        _tdkRepo.Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns(Enumerable.Empty<TindakanView>());
        _billingRepo.Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns(Enumerable.Empty<TrsBillView>());
        _antrianRepo.Setup(x => x.ListData(It.IsAny<DateTime>())).Returns([antrianView]);
        _antrianRepo.Setup(x => x.LoadEntity(It.IsAny<IAntrianKey>())).Returns(MayBe.From(antrian));
        _antrianMapRepo.Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<IPpaKey>(), regDate))
            .Returns(Enumerable.Empty<AntrianMapHdrView>());
        _trackerRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienTrackerKey>())).Returns(MayBe.From(tracker));

        PasienTrackerModel? saved = null;
        _trackerRepo.Setup(x => x.SaveChanges(It.IsAny<PasienTrackerModel>()))
            .Callback<PasienTrackerModel>(m => saved = m);

        // Act
        await _sut.Handle(new RegJalanBatalCmd(reg.RegId, "U1", "batal", "127.0.0.1", "test"),
            CancellationToken.None);

        // Assert
        _trackerRepo.Verify(x => x.DeleteEntity(It.IsAny<IPasienTrackerKey>()), Times.Never);
        _antrianRepo.Verify(x => x.SaveChanges(It.IsAny<AntrianModel>()), Times.Once);
        _regRepo.Verify(x => x.SaveChanges(It.IsAny<RegModel>()), Times.Once);
        saved.Should().NotBeNull();
        saved!.PasienTrackerId.Should().Be(stableId);
        saved.ListEvent.Should().Contain(x =>
            x.EventName == "REGISTER_CANCELLED" && x.ReffId == reg.RegId);
        _auditRepo.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
    }

    private static RegModel CreateReg(DateOnly regDate)
    {
        return new RegModel(
            "RG00000001",
            regDate,
            new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static BookingModel CreateBooking(DateOnly tglBerobat)
    {
        var person = new PersonInfoType("A", new DateOnly(2000, 1, 2), "P",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var jadwal = JadwalPraktekType.Default with { Hari = tglBerobat.DayOfWeek };
        return BookingModel.CreateLocal(person, tglBerobat, jadwal, "-");
    }
}
