using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.Integration;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.Integration;

public class WardAccommodationGatewayTest
{
    private readonly Mock<IKelasRepo> _kelasRepoMock = new();
    private readonly Mock<IKelasDkRepo> _kelasDkRepoMock = new();
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();
    private readonly Mock<IBangsalByKelasDkDal> _bangsalByKelasDkDalMock = new();

    [Fact]
    public void UT01_GivenValidKelasId_WhenResolve_ThenReturnsKelasReff()
    {
        _kelasRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasKey>(k => k.KelasId == "K1")))
            .Returns(MayBe.From(KelasType.Default with { KelasId = "K1", KelasName = "Kelas 1" }));

        var gateway = CreateGateway();
        var result = gateway.ResolveKelas("K1");

        result.KelasId.Should().Be("K1");
        result.KelasName.Should().Be("Kelas 1");
    }

    [Fact]
    public void UT02_GivenValidKelasDkId_WhenResolve_ThenReturnsKelasDkType()
    {
        _kelasDkRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasDkKey>(k => k.KelasDkId == "1")))
            .Returns(MayBe.From(new KelasDkType("1", "Kelas DK 1")));

        var gateway = CreateGateway();
        var result = gateway.ResolveKelasDk("1");

        result.KelasDkId.Should().Be("1");
        result.KelasDkName.Should().Be("Kelas DK 1");
    }

    [Fact]
    public void UT03_GivenValidBangsalId_WhenResolve_ThenReturnsBangsalReff()
    {
        _bangsalRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBangsalKey>(k => k.BangsalId == "B1")))
            .Returns(MayBe.From(new BangsalType(
                "B1",
                "Bangsal A",
                RoomCatType.Default,
                new Bilreg.Domain.AdmisiContext.LayananFeature.LayananReff("-", "-"))));

        var gateway = CreateGateway();
        var result = gateway.ResolveBangsal("B1");

        result.BangsalId.Should().Be("B1");
        result.BangsalName.Should().Be("Bangsal A");
    }

    [Fact]
    public void UT04_GivenEligibleBangsal_WhenResolveForCareClass_ThenReturnsBangsal()
    {
        _kelasDkRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasDkKey>(k => k.KelasDkId == "1")))
            .Returns(MayBe.From(new KelasDkType("1", "Kelas DK 1")));
        _bangsalByKelasDkDalMock
            .Setup(x => x.ListByKelasDkId("1"))
            .Returns([new BangsalReff("B1", "Bangsal A")]);

        var gateway = CreateGateway();
        var result = gateway.ResolveBangsalForCareClass("B1", "1");

        result.BangsalId.Should().Be("B1");
    }

    [Fact]
    public void UT05_GivenIneligibleBangsal_WhenResolveForCareClass_ThenThrows()
    {
        _kelasDkRepoMock
            .Setup(x => x.LoadEntity(It.Is<IKelasDkKey>(k => k.KelasDkId == "1")))
            .Returns(MayBe.From(new KelasDkType("1", "Kelas DK 1")));
        _bangsalByKelasDkDalMock
            .Setup(x => x.ListByKelasDkId("1"))
            .Returns([new BangsalReff("B1", "Bangsal A")]);

        var gateway = CreateGateway();
        var act = () => gateway.ResolveBangsalForCareClass("B9", "1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak memenuhi syarat*");
    }

    [Fact]
    public void UT06_GivenKelasDk_WhenListEligibleBangsal_ThenReturnsDistinctBangsal()
    {
        _bangsalByKelasDkDalMock
            .Setup(x => x.ListByKelasDkId("1"))
            .Returns(
            [
                new BangsalReff("B1", "Bangsal A"),
                new BangsalReff("B1", "Bangsal A"),
                new BangsalReff("B2", "Bangsal B")
            ]);

        var gateway = CreateGateway();
        var result = gateway.ListEligibleBangsal("1");

        result.Should().HaveCount(3);
    }

    [Fact]
    public void UT07_GivenHandOver_WhenNotify_ThenDoesNotThrow()
    {
        var gateway = CreateGateway();
        var act = () => gateway.NotifyHandOver(new WardAccommodationHandOver(
            "WTL00000001",
            "RG00000001",
            "B1",
            "K1",
            WaitingListStatusEnum.Waiting));

        act.Should().NotThrow();
    }

    private WardAccommodationGateway CreateGateway() =>
        new(
            _kelasRepoMock.Object,
            _kelasDkRepoMock.Object,
            _bangsalRepoMock.Object,
            _bangsalByKelasDkDalMock.Object);
}
