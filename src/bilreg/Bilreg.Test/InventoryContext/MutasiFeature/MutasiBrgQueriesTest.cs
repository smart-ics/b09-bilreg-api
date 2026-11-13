using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgQueriesTest
{
    private readonly Mock<IMutasiBrgRepo> _mutasiBrgRepoMock = new();

    private static MutasiBrgModel CreateSampleModel(
        string id = "MT2600001A",
        MutasiBrgStateEnum state = MutasiBrgStateEnum.Draft,
        DateTime? date = null,
        int itemCount = 2)
    {
        var items = new List<MutasiBrgItemModel>();
        for (var i = 1; i <= itemCount; i++)
        {
            var brg = new BrgReff($"B00{i}", $"Obat Paracetamol {i}");
            var satuan = new SatuanType($"S00{i}", "Tablet");
            items.Add(new MutasiBrgItemModel(i, brg, $"BATCH{i}", "2027-12-31", i * 15m, satuan));
        }

        var crt = new AuditInfoType("USR01", new DateTime(2026, 11, 10, 8, 0, 0));
        var upd = new AuditInfoType("USR02", new DateTime(2026, 11, 10, 8, 30, 0));
        var vod = new AuditInfoType("", new DateTime(3000, 1, 1));
        var audit = new AuditTrailType(crt, upd, vod);

        return new MutasiBrgModel(
            id,
            date ?? new DateTime(2026, 11, 10, 9, 15, 30),
            state,
            new LayananReff("L01", "Gudang Farmasi Sentral"),
            new LayananReff("L02", "Depo Farmasi Rawat Inap"),
            "OM202611001",
            "Pengiriman darurat",
            "USR01",
            audit,
            items
        );
    }

    private static MutasiBrgHeaderView CreateSampleHeaderView(
        string id = "MT2600001A",
        MutasiBrgStateEnum state = MutasiBrgStateEnum.Dispatched,
        DateTime? date = null)
    {
        return new MutasiBrgHeaderView(
            id,
            date ?? new DateTime(2026, 11, 10, 10, 0, 0),
            state,
            new LayananReff("L01", "Gudang Farmasi Sentral"),
            new LayananReff("L02", "Depo Farmasi Rawat Inap"),
            "OM202611001",
            "Catatan Kirim",
            "USR01"
        );
    }

    #region GET QUERY TESTS
    [Fact]
    public async Task GetQuery_GivenValidId_ReturnsFullDetailWithItems()
    {
        // ARRANGE
        var model = CreateSampleModel("MT2600001A");
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT2600001A")))
            .Returns(MayBe.From(model));

        var handler = new MutasiBrgGetHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgGetQuery("MT2600001A");

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.Should().NotBeNull();
        result.MutasiBrgId.Should().Be("MT2600001A");
        result.MutasiBrgDate.Should().Be("2026-11-10 09:15:30");
        result.State.Should().Be("Draft");
        result.LayananAsal.LayananId.Should().Be("L01");
        result.LayananAsal.LayananName.Should().Be("Gudang Farmasi Sentral");
        result.LayananTujuan.LayananId.Should().Be("L02");
        result.LayananTujuan.LayananName.Should().Be("Depo Farmasi Rawat Inap");
        result.OrderMutasiId.Should().Be("OM202611001");
        result.DispatchNote.Should().Be("Pengiriman darurat");
        result.IssuerUserId.Should().Be("USR01");

        var items = result.ListItem.ToList();
        items.Should().HaveCount(2);

        items[0].NoUrut.Should().Be(1);
        items[0].BrgId.Should().Be("B001");
        items[0].BrgName.Should().Be("Obat Paracetamol 1");
        items[0].BatchNo.Should().Be("BATCH1");
        items[0].ExpiredDate.Should().Be("2027-12-31");
        items[0].QtyKirim.Should().Be(15m);
        items[0].SatuanId.Should().Be("S001");
        items[0].SatuanName.Should().Be("Tablet");

        items[1].NoUrut.Should().Be(2);
        items[1].BrgId.Should().Be("B002");
        items[1].BrgName.Should().Be("Obat Paracetamol 2");
        items[1].BatchNo.Should().Be("BATCH2");
        items[1].ExpiredDate.Should().Be("2027-12-31");
        items[1].QtyKirim.Should().Be(30m);
        items[1].SatuanId.Should().Be("S002");
        items[1].SatuanName.Should().Be("Tablet");
    }

    [Fact]
    public async Task GetQuery_GivenDispatchedState_MapsStateToStringCorrectly()
    {
        // ARRANGE
        var model = CreateSampleModel("MT2600001A", MutasiBrgStateEnum.Dispatched);
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IMutasiBrgKey>()))
            .Returns(MayBe.From(model));

        var handler = new MutasiBrgGetHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgGetQuery("MT2600001A");

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.State.Should().Be("Dispatched");
    }

    [Fact]
    public async Task GetQuery_GivenCancelledState_MapsStateToStringCorrectly()
    {
        // ARRANGE
        var model = CreateSampleModel("MT2600001A", MutasiBrgStateEnum.Cancelled);
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IMutasiBrgKey>()))
            .Returns(MayBe.From(model));

        var handler = new MutasiBrgGetHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgGetQuery("MT2600001A");

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.State.Should().Be("Cancelled");
    }

    [Fact]
    public async Task GetQuery_GivenDefaultDate_ReturnsEmptyDateString()
    {
        // ARRANGE
        var model = new MutasiBrgModel(
            "MT2600001A",
            default,
            MutasiBrgStateEnum.Draft,
            new LayananReff("L01", "Gudang Farmasi Sentral"),
            new LayananReff("L02", "Depo Farmasi Rawat Inap"),
            string.Empty,
            string.Empty,
            string.Empty,
            AuditTrailType.Default,
            []
        );
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IMutasiBrgKey>()))
            .Returns(MayBe.From(model));

        var handler = new MutasiBrgGetHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgGetQuery("MT2600001A");

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.MutasiBrgDate.Should().BeEmpty();
    }

    [Fact]
    public async Task GetQuery_GivenNonExistingId_ThrowsKeyNotFoundException()
    {
        // ARRANGE
        _mutasiBrgRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IMutasiBrgKey>()))
            .Returns(MayBe<MutasiBrgModel>.None);

        var handler = new MutasiBrgGetHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgGetQuery("NONEXIST");

        // ACT & ASSERT
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NONEXIST*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetQuery_GivenInvalidId_ThrowsArgumentException(string? invalidId)
    {
        // ARRANGE
        var handler = new MutasiBrgGetHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgGetQuery(invalidId!);

        // ACT & ASSERT
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetQuery_GivenNullRequest_ThrowsArgumentNullException()
    {
        // ARRANGE
        var handler = new MutasiBrgGetHandler(_mutasiBrgRepoMock.Object);

        // ACT & ASSERT
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
    #endregion

    #region LIST QUERY TESTS
    [Fact]
    public async Task ListQuery_GivenValidPeriodStrings_CallsRepoAndReturnsHeaders()
    {
        // ARRANGE
        var item1 = CreateSampleHeaderView("MT2600001A", MutasiBrgStateEnum.Dispatched, new DateTime(2026, 11, 2));
        var item2 = CreateSampleHeaderView("MT2600002A", MutasiBrgStateEnum.Draft, new DateTime(2026, 11, 5));

        _mutasiBrgRepoMock
            .Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns([item1, item2]);

        var handler = new MutasiBrgListHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgListQuery("2026-11-01", "2026-11-10");

        // ACT
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(2);
        result[0].MutasiBrgId.Should().Be("MT2600001A");
        result[0].State.Should().Be(MutasiBrgStateEnum.Dispatched);
        result[1].MutasiBrgId.Should().Be("MT2600002A");
        result[1].State.Should().Be(MutasiBrgStateEnum.Draft);

        _mutasiBrgRepoMock.Verify(x => x.ListData(It.IsAny<Periode>()), Times.Once);
    }

    [Fact]
    public void ListQuery_ConstructedWithPeriode_InitializesPropertiesCorrectly()
    {
        // ARRANGE
        var periode = new Periode(new DateTime(2026, 11, 1), new DateTime(2026, 11, 15));

        // ACT
        var query = new MutasiBrgListQuery(periode);

        // ASSERT
        query.Tgl1.Should().Be("2026-11-01");
        query.Tgl2.Should().Be("2026-11-15");
        query.TglYmd1.Should().Be("2026-11-01");
        query.TglYmd2.Should().Be("2026-11-15");
    }

    [Fact]
    public void ListQuery_ConstructedWithDateTimes_InitializesPropertiesCorrectly()
    {
        // ARRANGE
        var tgl1 = new DateTime(2026, 11, 1);
        var tgl2 = new DateTime(2026, 11, 30);

        // ACT
        var query = new MutasiBrgListQuery(tgl1, tgl2);

        // ASSERT
        query.Tgl1.Should().Be("2026-11-01");
        query.Tgl2.Should().Be("2026-11-30");
        query.TglYmd1.Should().Be("2026-11-01");
        query.TglYmd2.Should().Be("2026-11-30");
    }

    [Theory]
    [InlineData("invalid", "2026-11-10")]
    [InlineData("2026-11-01", "invalid")]
    [InlineData("2026/11/01", "2026-11-10")]
    [InlineData("01-11-2026", "2026-11-10")]
    public async Task ListQuery_GivenInvalidDateFormat_ThrowsArgumentException(string tgl1, string tgl2)
    {
        // ARRANGE
        var handler = new MutasiBrgListHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgListQuery(tgl1, tgl2);

        // ACT & ASSERT
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("", "2026-11-10")]
    [InlineData("   ", "2026-11-10")]
    [InlineData("2026-11-01", "")]
    [InlineData("2026-11-01", "   ")]
    [InlineData(null, "2026-11-10")]
    [InlineData("2026-11-01", null)]
    public async Task ListQuery_GivenNullOrWhiteSpaceDate_ThrowsArgumentException(string? tgl1, string? tgl2)
    {
        // ARRANGE
        var handler = new MutasiBrgListHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgListQuery(tgl1!, tgl2!);

        // ACT & ASSERT
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ListQuery_GivenNullRequest_ThrowsArgumentNullException()
    {
        // ARRANGE
        var handler = new MutasiBrgListHandler(_mutasiBrgRepoMock.Object);

        // ACT & ASSERT
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ListQuery_GivenNoRecords_ReturnsEmptyCollection()
    {
        // ARRANGE
        _mutasiBrgRepoMock
            .Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns([]);

        var handler = new MutasiBrgListHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgListQuery("2026-11-01", "2026-11-10");

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.Should().BeEmpty();
    }
    #endregion

    #region IN-TRANSIT LIST QUERY TESTS
    [Fact]
    public async Task InTransitListQuery_GivenValidLayananTujuanId_CallsRepoAndReturnsHeaders()
    {
        // ARRANGE
        var item1 = CreateSampleHeaderView("MT2600001A", MutasiBrgStateEnum.Dispatched);
        var item2 = CreateSampleHeaderView("MT2600002A", MutasiBrgStateEnum.Dispatched);

        _mutasiBrgRepoMock
            .Setup(x => x.ListInTransit("L02"))
            .Returns([item1, item2]);

        var handler = new MutasiBrgInTransitListHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgInTransitListQuery("L02");

        // ACT
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(2);
        result[0].MutasiBrgId.Should().Be("MT2600001A");
        result[0].State.Should().Be(MutasiBrgStateEnum.Dispatched);
        result[1].MutasiBrgId.Should().Be("MT2600002A");
        result[1].State.Should().Be(MutasiBrgStateEnum.Dispatched);

        _mutasiBrgRepoMock.Verify(x => x.ListInTransit("L02"), Times.Once);
    }

    [Fact]
    public void InTransitListQuery_ExposesILayananKeyInterface()
    {
        // ARRANGE & ACT
        var query = new MutasiBrgInTransitListQuery("L02");

        // ASSERT
        query.LayananTujuanId.Should().Be("L02");
        query.LayananId.Should().Be("L02");
        (query as ILayananKey)!.LayananId.Should().Be("L02");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task InTransitListQuery_GivenInvalidLayananTujuanId_ThrowsArgumentException(string? invalidId)
    {
        // ARRANGE
        var handler = new MutasiBrgInTransitListHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgInTransitListQuery(invalidId!);

        // ACT & ASSERT
        var act = () => handler.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task InTransitListQuery_GivenNullRequest_ThrowsArgumentNullException()
    {
        // ARRANGE
        var handler = new MutasiBrgInTransitListHandler(_mutasiBrgRepoMock.Object);

        // ACT & ASSERT
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InTransitListQuery_GivenNoRecords_ReturnsEmptyCollection()
    {
        // ARRANGE
        _mutasiBrgRepoMock
            .Setup(x => x.ListInTransit(It.IsAny<string>()))
            .Returns([]);

        var handler = new MutasiBrgInTransitListHandler(_mutasiBrgRepoMock.Object);
        var query = new MutasiBrgInTransitListQuery("L02");

        // ACT
        var result = await handler.Handle(query, CancellationToken.None);

        // ASSERT
        result.Should().BeEmpty();
    }
    #endregion
}
