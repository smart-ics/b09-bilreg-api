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
    private readonly Mock<IBangsalRepo> _bangsalRepoMock = new();

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
    public void UT02_GivenValidBangsalId_WhenResolve_ThenReturnsBangsalReff()
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
    public void UT03_GivenHandOver_WhenNotify_ThenDoesNotThrow()
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
        new(_kelasRepoMock.Object, _bangsalRepoMock.Object);
}
