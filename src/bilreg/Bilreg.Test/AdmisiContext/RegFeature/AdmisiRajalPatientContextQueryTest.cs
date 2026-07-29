using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class AdmisiRajalPatientContextQueryTest
{
    [Fact]
    public async Task ExactBookingId_RanksBeforeNameAndMasksPhone()
    {
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([
                Booking("BO2", "ANI", "081234567890"),
                Booking("BO1", "BO1", "081111112222")
            ]);
        var sut = Handler(bookingRepo: bookingRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.BestMatch!.Id.Should().Be("BO1");
        result.BestMatch.IsExactMatch.Should().BeTrue();
        result.BestMatch.MaskedPhone.Should().EndWith("2222");
        result.BestMatch.MaskedPhone.Should().NotContain("08111111");
    }

    [Fact]
    public async Task BookingWithRegistration_ExposesRelationshipWarning()
    {
        var bookingRepo = new Mock<IBookingRepo>();
        var booking = Booking("BO1", "ANI", "0812") with
        {
            Reg = new RegReff("RG1", "000001", "ANI")
        };
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([booking]);
        var sut = Handler(bookingRepo: bookingRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.Bookings.Items.Single().RegistrationId.Should().Be("RG1");
        result.Bookings.Items.Single().Warnings.Should()
            .ContainSingle(x => x.Contains("RG1"));
    }

    [Fact]
    public async Task RegistrationSearch_UsesDirectPatientIdsAndSelectedBusinessDate()
    {
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("001234")).Returns([PatientView()]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByPatientIds(
                new DateOnly(2026, 7, 25),
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "001234" }))))
            .Returns([Registration("RG-TODAY", "2026-07-25")]);
        var sut = Handler(regRepo: regRepo, pasienRepo: pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("001234", "2026-07-25"),
            default);

        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG-TODAY");
    }

    [Fact]
    public async Task RegistrationConfirmation_AcceptsADifferentBusinessDate()
    {
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.GetById("RG00000001"))
            .Returns(Registration("RG00000001", "2025-01-01"));
        var sut = Handler(regRepo: regRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextGetQuery(
                PatientContextKind.Registration,
                "RG00000001",
                "2026-07-25"),
            default);

        result.VisitDate.Should().Be("2025-01-01");
    }

    [Fact]
    public async Task ExactRegistrationId_ReturnsHistoricalRegistrationWithoutBooking()
    {
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.GetById("RG00000891"))
            .Returns(Registration("RG00000891", "2025-04-10"));
        var sut = Handler(regRepo: regRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("RG00000891", "2026-07-25"),
            default);

        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG00000891");
        result.Bookings.Items.Should().BeEmpty();
        regRepo.Verify(
            x => x.FindByPatientIds(
                It.IsAny<DateOnly>(),
                It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
        regRepo.Verify(
            x => x.FindByRegistrationIds(It.IsAny<IReadOnlyCollection<string>>()),
            Times.Never);
    }

    [Theory]
    [InlineData(PatientContextScope.All)]
    [InlineData(PatientContextScope.Patient)]
    [InlineData(PatientContextScope.Booking)]
    [InlineData(PatientContextScope.Registration)]
    public async Task ExactRegistrationId_ReturnsOnlyRegistrationRegardlessOfScope(
        PatientContextScope scope)
    {
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.GetById("RG00000891"))
            .Returns(Registration("RG00000891", "2025-04-10"));
        var pasienRepo = new Mock<IPasienRepo>(MockBehavior.Strict);
        var bookingRepo = new Mock<IBookingRepo>(MockBehavior.Strict);
        var sut = Handler(bookingRepo, regRepo, pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery(
                "RG00000891",
                "2026-07-25",
                scope),
            default);

        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG00000891");
        result.Patients.Items.Should().BeEmpty();
        result.Bookings.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task PatientScope_DerivesAllCandidatesButReturnsOnlyPatients()
    {
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("ANI"))
            .Returns([PatientView()]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByPatientIds(
                new DateOnly(2026, 7, 25),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns([Registration("RG00000001", "2026-07-25")]);
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([Booking("BO1", "ANI", "0812")]);
        var sut = Handler(bookingRepo, regRepo, pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery(
                "ANI",
                "2026-07-25",
                PatientContextScope.Patient),
            default);

        result.Patients.Items.Should().ContainSingle();
        result.Bookings.Items.Should().BeEmpty();
        result.Registrations.Items.Should().BeEmpty();
        bookingRepo.Verify(x => x.ListDataTglBerobat(It.IsAny<Periode>()), Times.Once);
        regRepo.Verify(x => x.FindByPatientIds(
            new DateOnly(2026, 7, 25),
            It.IsAny<IReadOnlyCollection<string>>()), Times.Once);
    }

    [Fact]
    public async Task BookingScope_HidesPatientDerivedRegistrationButStillDerivesIt()
    {
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("ANI"))
            .Returns([PatientView()]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>())).Returns([]);
        var sut = Handler(bookingRepo, regRepo, pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery(
                "ANI",
                "2026-07-25",
                PatientContextScope.Booking),
            default);

        result.Patients.Items.Should().ContainSingle();
        result.Registrations.Items.Should().BeEmpty();
        regRepo.Verify(
            x => x.FindByPatientIds(
                new DateOnly(2026, 7, 25),
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("001234"))),
            Times.Once);
    }

    [Theory]
    [InlineData(PatientContextScope.All, 0, 2, 1)]
    [InlineData(PatientContextScope.Patient, 0, 0, 1)]
    [InlineData(PatientContextScope.Booking, 0, 1, 1)]
    [InlineData(PatientContextScope.Registration, 0, 2, 1)]
    public async Task OrdinarySearch_AppliesScopeAfterDerivationAndReconciliation(
        PatientContextScope scope,
        int expectedBookings,
        int expectedRegistrations,
        int expectedPatients)
    {
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("ANI")).Returns([PatientView()]);
        pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe.From(PatientModel()));
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([Booking("BO1", "ANI", "0812") with
            {
                Reg = new RegReff("RG-SUCCESSOR", "001234", "ANI")
            }]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByPatientIds(
                new DateOnly(2026, 7, 25),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns([Registration("RG-DATED", "2026-07-25")]);
        regRepo.Setup(x => x.FindByRegistrationIds(
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns([Registration("RG-SUCCESSOR", "2025-01-01")]);
        var sut = Handler(bookingRepo, regRepo, pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery(
                "ANI",
                "2026-07-25",
                scope),
            default);

        result.Bookings.Total.Should().Be(expectedBookings);
        result.Registrations.Total.Should().Be(expectedRegistrations);
        result.Patients.Total.Should().Be(expectedPatients);
        regRepo.Verify(x => x.FindByPatientIds(
            new DateOnly(2026, 7, 25),
            It.IsAny<IReadOnlyCollection<string>>()), Times.Once);
        regRepo.Verify(x => x.FindByRegistrationIds(
            It.IsAny<IReadOnlyCollection<string>>()), Times.Once);
    }

    [Fact]
    public async Task ExactPhone_ReturnsPatientAndDateBoundedRegistration()
    {
        const string phone = "081234567890";
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien(phone)).Returns([]);
        pasienRepo.Setup(x => x.SearchPasienByPhone(phone))
            .Returns([PatientView(phone: phone)]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByPatientIds(
                new DateOnly(2026, 7, 25),
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("001234"))))
            .Returns([Registration("RG00000001", "2026-07-25")]);
        var sut = Handler(regRepo: regRepo, pasienRepo: pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery(
                phone,
                "2026-07-25",
                PatientContextScope.Registration),
            default);

        result.Patients.Items.Should().ContainSingle(x =>
            x.MatchType == "ExactPhone"
            && x.IsExactMatch
            && x.MaskedPhone!.EndsWith("7890"));
        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG00000001");
    }

    [Fact]
    public async Task NameSearch_ExcludesExternalPatientFromResultsAndRegistrationLookup()
    {
        const string externalPatientId = "3471373X0002291";
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("ANI"))
            .Returns([
                PatientView(),
                PatientView(externalPatientId, "ANI EXTERNAL")
            ]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByPatientIds(
                It.IsAny<DateOnly>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns([]);
        var sut = Handler(regRepo: regRepo, pasienRepo: pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("ANI", "2026-07-25"),
            default);

        result.Patients.Items.Should().ContainSingle(x => x.Id == "001234");
        result.Patients.Items.Should().NotContain(x => x.Id == externalPatientId);
        regRepo.Verify(x => x.FindByPatientIds(
            new DateOnly(2026, 7, 25),
            It.Is<IReadOnlyCollection<string>>(ids =>
                ids.SequenceEqual(new[] { "001234" }))), Times.Once);
    }

    [Fact]
    public async Task BookingSearch_ExcludesBookingForExternalPatient()
    {
        const string externalPatientId = "3471373X0002291";
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([Booking("BO1", "ANI", "0812") with
            {
                Reg = new RegReff("-", externalPatientId, "ANI")
            }]);
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("BO1")).Returns([]);
        var sut = Handler(bookingRepo: bookingRepo, pasienRepo: pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.Bookings.Items.Should().BeEmpty();
        result.Patients.Items.Should().BeEmpty();
        pasienRepo.Verify(
            x => x.LoadEntity(It.IsAny<IPasienKey>()),
            Times.Never);
    }

    [Fact]
    public async Task PatientConfirmation_ExternalPatientId_IsNotFoundWithoutLoadingPatient()
    {
        const string externalPatientId = "3471373X0002291";
        var pasienRepo = new Mock<IPasienRepo>(MockBehavior.Strict);
        var sut = Handler(pasienRepo: pasienRepo);

        var act = () => sut.Handle(
            new AdmisiRajalPatientContextGetQuery(
                PatientContextKind.Patient,
                externalPatientId,
                "2026-07-25"),
            default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        pasienRepo.Verify(
            x => x.LoadEntity(It.IsAny<IPasienKey>()),
            Times.Never);
    }

    [Fact]
    public async Task SamePatientDateAndService_DoesNotSuppressBookingWithoutRegId()
    {
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("ANI")).Returns([PatientView()]);
        var bookingRepo = new Mock<IBookingRepo>();
        var booking = Booking("BO1", "ANI", "0812") with
        {
            Reg = new RegReff("-", "001234", "ANI")
        };
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([booking]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByPatientIds(
                It.IsAny<DateOnly>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns([Registration("RG00000001", "2026-07-25")]);
        var sut = Handler(bookingRepo, regRepo, pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("ANI", "2026-07-25"),
            default);

        result.Bookings.Items.Should().ContainSingle(x => x.Id == "BO1");
        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG00000001");
    }

    [Fact]
    public async Task BookingSearch_HydratesExistingPatient()
    {
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([Booking("BO1", "ANI", "0812") with
            {
                Reg = new RegReff("-", "001234", "ANI")
            }]);
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("BO1")).Returns([]);
        pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe.From(PatientModel()));
        var sut = Handler(bookingRepo: bookingRepo, pasienRepo: pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.Bookings.Items.Should().ContainSingle(x => x.Id == "BO1");
        result.Patients.Items.Should().ContainSingle(x => x.Id == "001234");
    }

    [Fact]
    public async Task BookingSearch_RetainsTransactionWhenPatientDoesNotExist()
    {
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([Booking("BO1", "ANI", "0812") with
            {
                Reg = new RegReff("-", "NOT-IN-MR", "ANI")
            }]);
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("BO1")).Returns([]);
        pasienRepo.Setup(x => x.LoadEntity(It.IsAny<IPasienKey>()))
            .Returns(MayBe<PasienModel>.None);
        var sut = Handler(bookingRepo: bookingRepo, pasienRepo: pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.Bookings.Items.Should().ContainSingle(x => x.Id == "BO1");
        result.Patients.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task BookingSuccessorLookup_IgnoresDateAndSuppressesOnlyMatchingRegId()
    {
        var bookingRepo = new Mock<IBookingRepo>();
        var booking = Booking("BO1", "ANI", "0812") with
        {
            Reg = new RegReff("RG00000001", "001234", "ANI")
        };
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([booking]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByRegistrationIds(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.SequenceEqual(new[] { "RG00000001" }))))
            .Returns([Registration("RG00000001", "2025-01-01", serviceId: "LY2")]);
        var sut = Handler(bookingRepo, regRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.Bookings.Items.Should().BeEmpty();
        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG00000001");
        result.Registrations.Items.Single().VisitDate.Should().Be("2025-01-01");
    }

    [Fact]
    public async Task DischargedRegistration_IsReturnedWithDischargedState()
    {
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.SearchPasien("ANI")).Returns([PatientView()]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByPatientIds(
                new DateOnly(2026, 7, 25),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns([
                Registration(
                    "RG00000001",
                    "2026-07-25",
                    exitDate: new DateOnly(2026, 7, 25))
            ]);
        var sut = Handler(regRepo: regRepo, pasienRepo: pasienRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("ANI", "2026-07-25"),
            default);

        result.Registrations.Items.Should().ContainSingle(x => x.State == "Discharged");
    }

    [Fact]
    public async Task BookingConfirmation_ReturnsRegistrationCreatedAfterSearch()
    {
        var bookingRepo = new Mock<IBookingRepo>();
        bookingRepo.Setup(x => x.LoadEntity(It.IsAny<IBookingKey>()))
            .Returns(MayBe.From(BookingModelForConfirmation()));
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByRegistrationIds(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("RG00000001"))))
            .Returns([Registration("RG00000001", "2026-07-25")]);
        var sut = Handler(bookingRepo, regRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextGetQuery(
                PatientContextKind.Booking,
                "BO1",
                "2026-07-25"),
            default);

        result.Kind.Should().Be(PatientContextKind.Registration);
        result.Id.Should().Be("RG00000001");
        result.MatchType.Should().Be("BookingSuccessor");
    }

    [Fact]
    public async Task LinkedRegistration_SuppressesBookingBeforeTotals()
    {
        var bookingRepo = new Mock<IBookingRepo>();
        var booking = Booking("BO1", "ANI", "0812") with
        {
            Reg = new RegReff("RG00000001", "001234", "ANI")
        };
        bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>()))
            .Returns([booking]);
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.FindByRegistrationIds(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("RG00000001"))))
            .Returns([Registration("RG00000001", "2026-07-25")]);
        regRepo.Setup(x => x.GetById("RG00000001"))
            .Returns(Registration("RG00000001", "2026-07-25"));
        var sut = Handler(bookingRepo, regRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.Bookings.Total.Should().Be(0);
        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG00000001");
        result.BestMatch!.Id.Should().Be("RG00000001");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    public async Task WeakGeneralSearch_IsRejected(string keyword)
    {
        var sut = Handler();

        var act = () => sut.Handle(
            new AdmisiRajalPatientContextSearchQuery(keyword, "2026-07-25"),
            default);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LoggerNeverReceivesRawKeyword()
    {
        var logger = new Mock<ILogger<AdmisiRajalPatientContextHandler>>();
        var pasienRepo = new Mock<IPasienRepo>();
        pasienRepo.Setup(x => x.GetDataByNik(It.IsAny<string>()))
            .Returns(MayBe<PasienPersonView>.None);
        var sut = Handler(pasienRepo: pasienRepo, logger: logger);

        await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("3173010101010001", "2026-07-25"),
            default);

        logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((value, _) =>
                    !value.ToString()!.Contains("3173010101010001")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private static AdmisiRajalPatientContextHandler Handler(
        Mock<IBookingRepo>? bookingRepo = null,
        Mock<IRegistrationHistoryReader>? regRepo = null,
        Mock<IPasienRepo>? pasienRepo = null,
        Mock<ILogger<AdmisiRajalPatientContextHandler>>? logger = null)
    {
        if (bookingRepo is null)
        {
            bookingRepo = new Mock<IBookingRepo>();
            bookingRepo.Setup(x => x.ListDataTglBerobat(It.IsAny<Periode>())).Returns([]);
        }
        if (regRepo is null)
        {
            regRepo = new Mock<IRegistrationHistoryReader>();
            regRepo.Setup(x => x.FindByPatientIds(
                    It.IsAny<DateOnly>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .Returns([]);
            regRepo.Setup(x => x.FindByRegistrationIds(
                    It.IsAny<IReadOnlyCollection<string>>()))
                .Returns([]);
        }
        if (pasienRepo is null)
        {
            pasienRepo = new Mock<IPasienRepo>();
            pasienRepo.Setup(x => x.SearchPasien(It.IsAny<string>())).Returns([]);
            pasienRepo.Setup(x => x.SearchPasienByPhone(It.IsAny<string>())).Returns([]);
        }
        return new AdmisiRajalPatientContextHandler(
            bookingRepo.Object,
            regRepo.Object,
            pasienRepo.Object,
            logger?.Object);
    }

    private static BookingView Booking(string id, string name, string phone) =>
        new(
            id,
            new DateTime(2026, 7, 24),
            new PersonInfoType(
                name,
                new DateOnly(1990, 1, 2),
                "P",
                AlamatType.Default,
                new ContactType(JenisContactEnum.Phone, phone),
                IdentitasType.Default),
            new RegReff("-", "001234", name),
            new DateOnly(2026, 7, 25),
            new TimeOnly(8, 0),
            new LayananReff("LY1", "Poli"),
            new PpaReff("DR1", "Dokter"),
            1);

    private static RegistrationSearchView Registration(
        string id,
        string date,
        string serviceId = "LY1",
        DateOnly? exitDate = null) =>
        new(
            id,
            DateOnly.Parse(date),
            new TimeOnly(8, 0),
            "001234",
            "ANI",
            new DateOnly(1990, 1, 2),
            "P",
            serviceId,
            "Poli",
            "DR1",
            "Dokter",
            "Umum",
            exitDate,
            null);

    private static PasienPersonView PatientView(
        string id = "001234",
        string name = "ANI",
        string phone = "0812") =>
        new(
            id,
            true,
            new PersonInfoType(
                name,
                new DateOnly(1990, 1, 2),
                "P",
                AlamatType.Default,
                new ContactType(JenisContactEnum.Phone, phone),
                IdentitasType.Default));

    private static PasienModel PatientModel() =>
        new(
            "001234",
            PatientView().Person,
            "-",
            "-",
            GolDarahType.Default,
            "-",
            KtpType.Default,
            KelurahanType.Default,
            IdentitasType.Default,
            [],
            PasienKeluargaType.Default,
            AgamaType.Default,
            SukuType.Default,
            StatusKawinDkType.Default,
            PendidikanDkType.Default,
            PekerjaanDkType.Default,
            new DateTime(2020, 1, 1),
            true);

    private static BookingModel BookingModelForConfirmation() =>
        new(
            "BO1",
            new DateTime(2026, 7, 24),
            PatientView().Person,
            "001234",
            new RegReff("RG00000001", "001234", "ANI"),
            new DateOnly(2026, 7, 25),
            new TimeOnly(8, 0),
            new LayananReff("LY1", "Poli"),
            new PpaReff("DR1", "Dokter"),
            1,
            AuditTrailType.Default,
            ExtAppReffType.Default,
            CoverageInfoType.Default);
}
