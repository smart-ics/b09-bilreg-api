using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.WaitingListFeature;

public class AdmGetWaitingListByRegIdHandlerTest
{
    private readonly Mock<IWaitingListRepo> _waitingListRepoMock = new();

    [Fact]
    public async Task UT01_GivenActiveWaitingList_WhenGetByRegId_ThenReturnsResponse()
    {
        var waitingList = CreateActiveWaitingList();
        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("RG00001234"))
            .Returns(MayBe.From(waitingList));

        var handler = new AdmGetWaitingListByRegIdHandler(_waitingListRepoMock.Object);
        var result = await handler.Handle(
            new AdmGetWaitingListByRegIdQry("RG00001234"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.WaitingListId.Should().Be(waitingList.WaitingListId);
        result.RegId.Should().Be("RG00001234");
        result.WaitingListStatus.Should().Be(WaitingListStatusEnum.Waiting);
        result.Priority.Should().Be(5);
        result.Pasien.PasienId.Should().Be("P0001");
        result.KelasRawat.KelasId.Should().Be("K01");
        result.Bangsal.BangsalId.Should().Be("B001");
    }

    [Fact]
    public async Task UT02_GivenNoActiveWaitingList_WhenGetByRegId_ThenReturnsNull()
    {
        _waitingListRepoMock
            .Setup(x => x.LoadActiveByRegId("RG00009999"))
            .Returns(MayBe<WaitingListModel>.None);

        var handler = new AdmGetWaitingListByRegIdHandler(_waitingListRepoMock.Object);
        var result = await handler.Handle(
            new AdmGetWaitingListByRegIdQry("RG00009999"),
            CancellationToken.None);

        result.Should().BeNull();
    }

    private static WaitingListModel CreateActiveWaitingList() =>
        new(
            "WTL00000001",
            WaitingListStatusEnum.Waiting,
            "RG00001234",
            new PasienReff("P0001", "Pasien Test", new DateOnly(1990, 5, 15), "L"),
            new KelasReff("K01", "Kelas 1"),
            new BangsalReff("B001", "Bangsal A"),
            5,
            AuditTrailType.Create("user1", new DateTime(2026, 7, 7)));
}
