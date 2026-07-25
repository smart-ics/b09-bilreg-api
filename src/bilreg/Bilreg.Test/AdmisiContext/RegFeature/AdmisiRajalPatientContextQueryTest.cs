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
        Mock<IRegAktifRepo>? regRepo = null,
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
            regRepo = new Mock<IRegAktifRepo>();
            regRepo.Setup(x => x.ListData(It.IsAny<string>())).Returns([]);
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
}
