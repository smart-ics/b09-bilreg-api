using Bilreg.Application.InventoryContext.LogistikMedisFeature;
using Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.InventoryContext.LogistikMedisFeature;

public class LogistikMedisListQueriesTest
{
    private readonly Mock<ILogistikMedisBffDal> _bffDalMock = new();

    [Fact]
    public async Task OrderMutasiList_BatchLoadsItems_ComputesTotalsAndFlags_AndCoversEndOfDay()
    {
        // ARRANGE
        DateTime? capturedTgl1 = null;
        DateTime? capturedTgl2 = null;
        _bffDalMock
            .Setup(x => x.ListOrderMutasiByLayananOrder("LY01", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<string, DateTime, DateTime>((_, t1, t2) =>
            {
                capturedTgl1 = t1;
                capturedTgl2 = t2;
            })
            .Returns(
            [
                new LogistikMedisOrderMutasiHeaderRowDto(
                    "OM-01", new DateTime(2026, 11, 20, 9, 30, 0),
                    OrderMutasiStateEnum.Submitted, OrderMutasiPriorityEnum.Cito,
                    "LY01", "Poli Anak", "GD01", "Gudang Farmasi", "Stok habis"),
                new LogistikMedisOrderMutasiHeaderRowDto(
                    "OM-02", new DateTime(2026, 11, 21, 14, 0, 0),
                    OrderMutasiStateEnum.PartiallyFulfilled, OrderMutasiPriorityEnum.Rutin,
                    "LY01", "Poli Anak", "GD01", "Gudang Farmasi", "Parsial"),
                new LogistikMedisOrderMutasiHeaderRowDto(
                    "OM-DRAFT", new DateTime(2026, 11, 22, 8, 0, 0),
                    OrderMutasiStateEnum.Draft, OrderMutasiPriorityEnum.Rutin,
                    "LY01", "Poli Anak", "GD01", "Gudang Farmasi", "Draft"),
            ]);

        _bffDalMock
            .Setup(x => x.ListOrderMutasiItems(It.IsAny<IEnumerable<string>>()))
            .Returns(
            [
                new LogistikMedisOrderMutasiItemRowDto("OM-01", 1, "BRG01", "Paracetamol", 5m, 20m, 0m, "TAB", "Tablet"),
                new LogistikMedisOrderMutasiItemRowDto("OM-01", 2, "BRG02", "Amoxicillin", 2m, 10m, 0m, "TAB", "Tablet"),
                new LogistikMedisOrderMutasiItemRowDto("OM-02", 1, "BRG03", "Spuit 3cc", 1m, 50m, 30m, "PCS", "Pieces"),
            ]);

        var sut = new LogistikMedisOrderMutasiListHandler(_bffDalMock.Object);

        // ACT
        var result = (await sut.Handle(
            new LogistikMedisOrderMutasiListQuery("LY01", "2026-11-01", "2026-11-27"),
            CancellationToken.None)).ToList();

        // ASSERT
        capturedTgl1.Should().Be(new DateTime(2026, 11, 1, 0, 0, 0));
        capturedTgl2.Should().Be(new DateTime(2026, 11, 27, 23, 59, 59));

        result.Should().HaveCount(2);
        result[0].OrderMutasiId.Should().Be("OM-02");
        result[0].CanCancel.Should().BeFalse();
        result[0].CanForceClose.Should().BeTrue();
        result[0].TotalItem.Should().Be(1);
        result[0].TotalQty.Should().Be(50m);
        result[0].TotalQtyTerpenuhi.Should().Be(30m);
        result[0].ListItem.Should().HaveCount(1);

        result[1].OrderMutasiId.Should().Be("OM-01");
        result[1].Priority.Should().Be("Cito");
        result[1].CanCancel.Should().BeTrue();
        result[1].CanForceClose.Should().BeFalse();
        result[1].TotalItem.Should().Be(2);
        result[1].TotalQty.Should().Be(30m);
        result[1].TotalQtyTerpenuhi.Should().Be(0m);
        result[1].ListItem.Should().HaveCount(2);

        _bffDalMock.Verify(
            x => x.ListOrderMutasiItems(It.Is<IEnumerable<string>>(ids => ids.Count() == 2)),
            Times.Once);
    }

    [Fact]
    public async Task OrderMutasiList_WhenNoHeaders_SkipsBatchItemQuery()
    {
        _bffDalMock
            .Setup(x => x.ListOrderMutasiByLayananOrder("LY01", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns([]);

        var sut = new LogistikMedisOrderMutasiListHandler(_bffDalMock.Object);

        var result = (await sut.Handle(
            new LogistikMedisOrderMutasiListQuery("LY01", "2026-11-01", "2026-11-27"),
            CancellationToken.None)).ToList();

        result.Should().BeEmpty();
        _bffDalMock.Verify(x => x.ListOrderMutasiItems(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task MutasiMasuk_ReturnsInTransitAndReceivedHistoryWithBatchItems()
    {
        // ARRANGE
        DateTime? capturedTgl2 = null;
        _bffDalMock.Setup(x => x.ListMutasiBrgInTransit("LY01")).Returns(
        [
            new LogistikMedisMutasiBrgHeaderRowDto(
                "MT-01", new DateTime(2026, 11, 26, 15, 0, 0), MutasiBrgStateEnum.Dispatched,
                "GD01", "Gudang Farmasi", "LY01", "Poli Anak", "OM-01", "Kirim sore", "USR1", ""),
        ]);
        _bffDalMock.Setup(x => x.ListMutasiBrgItems(It.IsAny<IEnumerable<string>>())).Returns(
        [
            new LogistikMedisMutasiBrgItemRowDto("MT-01", 1, "BRG01", "Paracetamol", "B-01", "2027-05-01", 20m, "TAB", "Tablet"),
        ]);

        _bffDalMock
            .Setup(x => x.ListTerimaMutasi("LY01", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<string, DateTime, DateTime>((_, _, t2) => capturedTgl2 = t2)
            .Returns(
            [
                new LogistikMedisTerimaMutasiHeaderRowDto(
                    "TM-01", new DateTime(2026, 11, 27, 9, 0, 0), TerimaMutasiStateEnum.Received,
                    "MT-00", "OM-00", "GD01", "Gudang Farmasi", "LY01", "Poli Anak", "USR2", "Lengkap"),
            ]);
        _bffDalMock.Setup(x => x.ListTerimaMutasiItems(It.IsAny<IEnumerable<string>>())).Returns(
        [
            new LogistikMedisTerimaMutasiItemRowDto("TM-01", 1, "BRG02", "Spuit 3cc", "S-01", "2028-01-01", 10m, "PCS", "Pieces"),
        ]);

        var sut = new LogistikMedisMutasiMasukHandler(_bffDalMock.Object);

        // ACT
        var result = await sut.Handle(
            new LogistikMedisMutasiMasukQuery("LY01", "2026-11-25", "2026-11-27"),
            CancellationToken.None);

        // ASSERT
        capturedTgl2.Should().Be(new DateTime(2026, 11, 27, 23, 59, 59));

        result.InTransitList.Should().HaveCount(1);
        result.InTransitList[0].MutasiBrgId.Should().Be("MT-01");
        result.InTransitList[0].TotalItem.Should().Be(1);
        result.InTransitList[0].TotalQty.Should().Be(20m);
        result.InTransitList[0].ListItem.Should().HaveCount(1);
        result.InTransitList[0].ListItem[0].BatchNo.Should().Be("B-01");

        result.ReceivedHistoryList.Should().HaveCount(1);
        result.ReceivedHistoryList[0].TerimaMutasiId.Should().Be("TM-01");
        result.ReceivedHistoryList[0].TotalItem.Should().Be(1);
        result.ReceivedHistoryList[0].TotalQty.Should().Be(10m);
        result.ReceivedHistoryList[0].ListItem.Should().HaveCount(1);
        result.ReceivedHistoryList[0].ListItem[0].QtyTerima.Should().Be(10m);
    }

    [Fact]
    public async Task MutasiKeluarList_BatchLoadsItems_AndSetsCanCancelOnlyForDispatched()
    {
        // ARRANGE
        DateTime? capturedTgl2 = null;
        _bffDalMock
            .Setup(x => x.ListMutasiBrgKeluar("LY01", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<string, DateTime, DateTime>((_, _, t2) => capturedTgl2 = t2)
            .Returns(
            [
                new LogistikMedisMutasiBrgHeaderRowDto(
                    "MT-OUT-1", new DateTime(2026, 11, 27, 10, 0, 0), MutasiBrgStateEnum.Dispatched,
                    "LY01", "Poli Anak", "GD01", "Gudang Farmasi", "", "Retur ED", "USR1", ""),
                new LogistikMedisMutasiBrgHeaderRowDto(
                    "MT-OUT-2", new DateTime(2026, 11, 26, 11, 0, 0), MutasiBrgStateEnum.Received,
                    "LY01", "Poli Anak", "LY02", "Poli Umum", "", "Pinjam stok", "USR1", ""),
            ]);

        _bffDalMock.Setup(x => x.ListMutasiBrgItems(It.IsAny<IEnumerable<string>>())).Returns(
        [
            new LogistikMedisMutasiBrgItemRowDto("MT-OUT-1", 1, "BRG01", "Paracetamol", "B-01", "2026-12-01", 5m, "TAB", "Tablet"),
            new LogistikMedisMutasiBrgItemRowDto("MT-OUT-2", 1, "BRG02", "Kassa Steril", "", "", 3m, "BOX", "Box"),
        ]);

        var sut = new LogistikMedisMutasiKeluarListHandler(_bffDalMock.Object);

        // ACT
        var result = (await sut.Handle(
            new LogistikMedisMutasiKeluarListQuery("LY01", "2026-11-20", "2026-11-27"),
            CancellationToken.None)).ToList();

        // ASSERT
        capturedTgl2.Should().Be(new DateTime(2026, 11, 27, 23, 59, 59));

        result.Should().HaveCount(2);
        result[0].MutasiBrgId.Should().Be("MT-OUT-1");
        result[0].CanCancel.Should().BeTrue();
        result[0].TotalQty.Should().Be(5m);
        result[0].ListItem.Should().HaveCount(1);

        result[1].MutasiBrgId.Should().Be("MT-OUT-2");
        result[1].CanCancel.Should().BeFalse();
        result[1].TotalQty.Should().Be(3m);
        result[1].ListItem.Should().HaveCount(1);

        _bffDalMock.Verify(
            x => x.ListMutasiBrgItems(It.Is<IEnumerable<string>>(ids => ids.Count() == 2)),
            Times.Once);
    }

    [Fact]
    public async Task AllListQueries_GivenInvalidDateFormat_ThrowArgumentException()
    {
        var orderSut = new LogistikMedisOrderMutasiListHandler(_bffDalMock.Object);
        var masukSut = new LogistikMedisMutasiMasukHandler(_bffDalMock.Object);
        var keluarSut = new LogistikMedisMutasiKeluarListHandler(_bffDalMock.Object);

        var act1 = () => orderSut.Handle(new LogistikMedisOrderMutasiListQuery("LY01", "2026/11/01", "2026-11-27"), CancellationToken.None);
        var act2 = () => masukSut.Handle(new LogistikMedisMutasiMasukQuery("LY01", "2026-11-01", "invalid-date"), CancellationToken.None);
        var act3 = () => keluarSut.Handle(new LogistikMedisMutasiKeluarListQuery("LY01", "27-11-2026", "2026-11-27"), CancellationToken.None);

        await act1.Should().ThrowAsync<ArgumentException>();
        await act2.Should().ThrowAsync<ArgumentException>();
        await act3.Should().ThrowAsync<ArgumentException>();
    }
}
