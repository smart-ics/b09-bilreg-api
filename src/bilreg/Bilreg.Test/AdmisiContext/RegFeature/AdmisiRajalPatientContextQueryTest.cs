using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
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
    public async Task RegistrationSearch_ReturnsOnlyTheSelectedBusinessDate()
    {
        var regRepo = new Mock<IRegistrationHistoryReader>();
        regRepo.Setup(x => x.Search("001234", new DateOnly(2026, 7, 25)))
            .Returns([Registration("RG-TODAY", "2026-07-25")]);
        var sut = Handler(regRepo: regRepo);

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
            x => x.Search(It.IsAny<string>(), It.IsAny<DateOnly>()),
            Times.Never);
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
        regRepo.Setup(x => x.FindRelated(
                new DateOnly(2026, 7, 25),
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<IReadOnlyCollection<string>>()))
            .Returns([Registration("RG00000001", "2026-07-25")]);
        regRepo.Setup(x => x.GetById("RG00000001"))
            .Returns(Registration("RG00000001", "2026-07-25"));
        var sut = Handler(bookingRepo, regRepo);

        var result = await sut.Handle(
            new AdmisiRajalPatientContextSearchQuery("BO1", "2026-07-25"),
            default);

        result.Bookings.Total.Should().Be(0);
        result.Registrations.Items.Should().ContainSingle(x => x.Id == "RG00000001");
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
            regRepo.Setup(x => x.Search(It.IsAny<string>(), It.IsAny<DateOnly>())).Returns([]);
            regRepo.Setup(x => x.FindRelated(
                    It.IsAny<DateOnly>(),
                    It.IsAny<IReadOnlyCollection<string>>(),
                    It.IsAny<IReadOnlyCollection<string>>()))
                .Returns([]);
        }
        if (pasienRepo is null)
        {
            pasienRepo = new Mock<IPasienRepo>();
            pasienRepo.Setup(x => x.SearchPasien(It.IsAny<string>())).Returns([]);
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
            RegModel.Default.ToReff(),
            new DateOnly(2026, 7, 25),
            new TimeOnly(8, 0),
            new LayananReff("LY1", "Poli"),
            new PpaReff("DR1", "Dokter"),
            1);

    private static RegistrationSearchView Registration(string id, string date) =>
        new(
            id,
            DateOnly.Parse(date),
            new TimeOnly(8, 0),
            "001234",
            "ANI",
            new DateOnly(1990, 1, 2),
            "P",
            "LY1",
            "Poli",
            "DR1",
            "Dokter",
            "Umum",
            null,
            null);
}
