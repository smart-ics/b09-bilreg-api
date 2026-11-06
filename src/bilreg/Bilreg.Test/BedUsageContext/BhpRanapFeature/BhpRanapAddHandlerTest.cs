using Bilreg.Application.BedUsageContext.BhpRanapFeature;
using Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.BhpRanapFeature;

public class BhpRanapAddHandlerTest
{
    private readonly Mock<IPakaiBedRepo> _pakaiBedRepoMock;
    private readonly Mock<IBhpRanapRepo> _bhpRanapRepoMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly BhpRanapAddHandler _sut;

    public BhpRanapAddHandlerTest()
    {
        _pakaiBedRepoMock = new Mock<IPakaiBedRepo>();
        _bhpRanapRepoMock = new Mock<IBhpRanapRepo>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 2, 11, 10, 0, 0));

        _sut = new BhpRanapAddHandler(
            _pakaiBedRepoMock.Object,
            _bhpRanapRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task Handle_WithPakaiBedInRepo_ShouldSaveAndReturnResponse()
    {
        var pakaiBed = new PakaiBedModel(
            "BED001",
            PeriodePakaiBedType.Default,
            new RegReff("REG001", "MR01", "Pasien A"),
            new LayananReff("3RI02", "Bangsal Melati"),
            new BedReff("B01", "Bed 1", true),
            new TipeKamarReff("TK01", "VIP", true),
            new KelasReff("K01", "Kelas 1"),
            100000m,
            AuditTrailType.Default);

        _pakaiBedRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPakaiBed>(k => k.PakaiBedId == "BED001")))
            .Returns(MayBe.From(pakaiBed));

        var cmd = new BhpRanapAddCmd(
            PakaiBedId: "BED001",
            BhpItemId: "BHP10",
            BhpItemName: "Spuit 10cc",
            Qty: 3,
            Price: 5000m,
            UserId: "USER1");

        var response = await _sut.Handle(cmd, CancellationToken.None);

        response.Should().NotBeNull();
        response.BhpRanapId.Should().StartWith("BHR");
        response.PakaiBedId.Should().Be("BED001");
        response.RegId.Should().Be("REG001");
        response.Subtotal.Should().Be(15000m);

        _bhpRanapRepoMock.Verify(x => x.SaveChanges(It.Is<BhpRanapModel>(b =>
            b.BhpItemId == "BHP10" &&
            b.Qty == 3 &&
            b.Price == 5000m &&
            b.RegId == "REG001" &&
            b.LayananId == "3RI02")), Times.Once);
    }
}
