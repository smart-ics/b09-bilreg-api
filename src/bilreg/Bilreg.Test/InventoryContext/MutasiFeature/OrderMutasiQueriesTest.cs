using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.InventoryContext.StokFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class OrderMutasiQueriesTest
{
    private readonly Mock<IOrderMutasiRepo> _orderMutasiRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();

    #region OrderMutasiGetQuery Tests
    [Fact]
    public async Task OrderMutasiGetHandler_WhenOrderFound_ReturnsDetailedResponseWithPriorityAndBackorder()
    {
        // Arrange
        var orderId = "ORD-001";
        var layananOrder = new LayananReff("L01", "Apotek Rawat Jalan");
        var layananTujuan = new LayananReff("L02", "Gudang Farmasi");
        var brg = new BrgReff("B01", "Paracetamol 500mg");
        var satuan = new SatuanType("S01", "Tablet");
        var item = new OrderMutasiItemModel(1, brg, qty: 10m, satuan, stokCurrent: 15m, qtyTerpenuhi: 4m);

        var model = new OrderMutasiModel(
            orderId,
            new DateTime(2026, 10, 27, 8, 30, 0),
            OrderMutasiStateEnum.PartiallyFulfilled,
            layananOrder,
            layananTujuan,
            new ApprovalType("APPR_USER", new DateTime(2026, 10, 27, 9, 0, 0)),
            ApprovalType.Default,
            "Segera kirim",
            AuditTrailType.Create("USER01", new DateTime(2026, 10, 27, 8, 0, 0)),
            [item],
            OrderMutasiPriorityEnum.Cito);

        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == orderId)))
            .Returns(MayBe.From(model));

        var sut = new OrderMutasiGetHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiGetQuery(orderId);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderMutasiId.Should().Be(orderId);
        result.State.Should().Be(nameof(OrderMutasiStateEnum.PartiallyFulfilled));
        result.Priority.Should().Be(nameof(OrderMutasiPriorityEnum.Cito));
        result.OrderNote.Should().Be("Segera kirim");
        result.Approval.UserId.Should().Be("APPR_USER");
        result.ListItem.Should().HaveCount(1);

        var itemResult = result.ListItem.First();
        itemResult.NoUrut.Should().Be(1);
        itemResult.Brg.BrgId.Should().Be("B01");
        itemResult.StokCurrent.Should().Be(15m);
        itemResult.Qty.Should().Be(10m);
        itemResult.QtyTerpenuhi.Should().Be(4m);
        itemResult.SisaBackorder.Should().Be(6m);
    }

    [Fact]
    public async Task OrderMutasiGetHandler_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderMutasiKey>()))
            .Returns(MayBe<OrderMutasiModel>.None);

        var sut = new OrderMutasiGetHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiGetQuery("NON-EXISTENT");

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*NON-EXISTENT*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OrderMutasiGetHandler_WhenIdNullOrWhitespace_ThrowsArgumentException(string invalidId)
    {
        // Arrange
        var sut = new OrderMutasiGetHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiGetQuery(invalidId);

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion

    #region OrderMutasiDraftStateGetQuery Tests
    [Fact]
    public async Task OrderMutasiDraftStateGetHandler_WhenDraftExists_ReturnsDraftWithItemsAndStokCurrent()
    {
        // Arrange
        var layananId = "L01";
        var layanan = LayananType.Default with { LayananId = layananId, LayananName = "Unit IGD" };
        var brg = new BrgReff("B02", "Cefixime 200mg");
        var satuan = new SatuanType("S02", "Kapsul");
        var item = new OrderMutasiItemModel(1, brg, qty: 20m, satuan, stokCurrent: 5m, qtyTerpenuhi: 0m);

        var draftModel = new OrderMutasiModel(
            "DFT-001",
            new DateTime(2026, 10, 27, 7, 0, 0),
            OrderMutasiStateEnum.Draft,
            layanan.ToReff(),
            LayananType.Default.ToReff(),
            ApprovalType.Default,
            ApprovalType.Default,
            "Draft pesanan",
            AuditTrailType.Create("USER01", new DateTime(2026, 10, 27, 7, 0, 0)),
            [item],
            OrderMutasiPriorityEnum.Rutin);

        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == layananId)))
            .Returns(MayBe.From(layanan));

        var draftHeader = new OrderMutasiHeaderView("DFT-001", DateTime.Now, OrderMutasiStateEnum.Draft, layanan.ToReff(), LayananType.Default.ToReff());
        _orderMutasiRepoMock.Setup(x => x.ListDraftState())
            .Returns([draftHeader]);

        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "DFT-001")))
            .Returns(MayBe.From(draftModel));

        var sut = new OrderMutasiDraftStateGetHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var query = new OrderMutasiDraftStateGetQuery(layananId);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.OrderMutasiId.Should().Be("DFT-001");
        result.State.Should().Be("Draft");
        result.Priority.Should().Be("Rutin");
        result.ListItem.Should().HaveCount(1);

        var itemResult = result.ListItem.First();
        itemResult.Brg.BrgId.Should().Be("B02");
        itemResult.StokCurrent.Should().Be(5m);
        itemResult.Qty.Should().Be(20m);
        itemResult.Satuan.SatuanId.Should().Be("S02");
    }

    [Fact]
    public async Task OrderMutasiDraftStateGetHandler_WhenDraftDoesNotExist_ReturnsGracefulEmptyDefaultResponse()
    {
        // Arrange
        var layananId = "L02";
        var layanan = LayananType.Default with { LayananId = layananId, LayananName = "Unit Rawat Inap" };

        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == layananId)))
            .Returns(MayBe.From(layanan));

        // No draft exists for this unit
        _orderMutasiRepoMock.Setup(x => x.ListDraftState())
            .Returns([]);

        var sut = new OrderMutasiDraftStateGetHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var query = new OrderMutasiDraftStateGetQuery(layananId);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert: must NOT throw KeyNotFoundException, must return default empty response
        result.Should().NotBeNull();
        result.OrderMutasiId.Should().Be("-");
        result.State.Should().Be("Draft");
        result.LayananOrder.LayananId.Should().Be(layananId);
        result.ListItem.Should().BeEmpty();
    }

    [Fact]
    public async Task OrderMutasiDraftStateGetHandler_WhenLayananNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>()))
            .Returns(MayBe<LayananType>.None);

        var sut = new OrderMutasiDraftStateGetHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var query = new OrderMutasiDraftStateGetQuery("UNKNOWN_LYN");

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*UNKNOWN_LYN*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OrderMutasiDraftStateGetHandler_WhenLayananIdNullOrWhitespace_ThrowsArgumentException(string invalidId)
    {
        // Arrange
        var sut = new OrderMutasiDraftStateGetHandler(_orderMutasiRepoMock.Object, _layananRepoMock.Object);
        var query = new OrderMutasiDraftStateGetQuery(invalidId);

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion

    #region OrderMutasiSubmittedListQuery Tests
    [Fact]
    public async Task OrderMutasiSubmittedListHandler_WhenValidDateRange_ReturnsSubmittedOrdersWithPriorityAndNote()
    {
        // Arrange
        var header1 = new OrderMutasiHeaderView(
            "ORD-SUB-01",
            new DateTime(2026, 10, 27, 8, 0, 0),
            OrderMutasiStateEnum.Submitted,
            new LayananReff("L01", "Poli A"),
            new LayananReff("L02", "Gudang B"),
            OrderMutasiPriorityEnum.Cito,
            "Permintaan Cito");

        var header2 = new OrderMutasiHeaderView(
            "ORD-APP-01",
            new DateTime(2026, 10, 27, 9, 0, 0),
            OrderMutasiStateEnum.Approved,
            new LayananReff("L01", "Poli A"),
            new LayananReff("L02", "Gudang B"),
            OrderMutasiPriorityEnum.Rutin,
            "Permintaan Rutin");

        _orderMutasiRepoMock.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns([header1, header2]);

        var sut = new OrderMutasiSubmittedListHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiSubmittedListQuery("2026-10-01", "2026-10-31");

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert: only Submitted items are returned
        result.Should().HaveCount(1);
        result[0].OrderMutasiId.Should().Be("ORD-SUB-01");
        result[0].State.Should().Be(nameof(OrderMutasiStateEnum.Submitted));
        result[0].Priority.Should().Be(nameof(OrderMutasiPriorityEnum.Cito));
        result[0].OrderNote.Should().Be("Permintaan Cito");
    }

    [Theory]
    [InlineData("bad-date", "2026-10-31")]
    [InlineData("2026-10-01", "bad-date")]
    public async Task OrderMutasiSubmittedListHandler_WhenInvalidDateFormat_ThrowsArgumentException(string d1, string d2)
    {
        // Arrange
        var sut = new OrderMutasiSubmittedListHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiSubmittedListQuery(d1, d2);

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion

    #region OrderMutasiApprovedListQuery Tests
    [Fact]
    public async Task OrderMutasiApprovedListHandler_WhenValidDateRange_ReturnsApprovedOrdersWithPriorityAndNote()
    {
        // Arrange
        var header = new OrderMutasiHeaderView(
            "ORD-APP-02",
            new DateTime(2026, 10, 27, 10, 0, 0),
            OrderMutasiStateEnum.Approved,
            new LayananReff("L01", "Poli Gigi"),
            new LayananReff("L03", "Farmasi Pusat"),
            OrderMutasiPriorityEnum.Rutin,
            "Siap antar");

        _orderMutasiRepoMock.Setup(x => x.ListApprovedState(It.IsAny<Periode>()))
            .Returns([header]);

        var sut = new OrderMutasiApprovedListHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiApprovedListQuery("2026-10-01", "2026-10-31");

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderMutasiId.Should().Be("ORD-APP-02");
        result[0].State.Should().Be(nameof(OrderMutasiStateEnum.Approved));
        result[0].Priority.Should().Be(nameof(OrderMutasiPriorityEnum.Rutin));
        result[0].OrderNote.Should().Be("Siap antar");
    }

    [Theory]
    [InlineData("2026/10/01", "2026-10-31")]
    [InlineData("2026-10-01", "")]
    public async Task OrderMutasiApprovedListHandler_WhenInvalidDateFormat_ThrowsArgumentException(string d1, string d2)
    {
        // Arrange
        var sut = new OrderMutasiApprovedListHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiApprovedListQuery(d1, d2);

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion

    #region OrderMutasiListByLayananQuery Tests
    [Fact]
    public async Task OrderMutasiListByLayananHandler_WhenValidLayananAndDateRange_ReturnsLayananOrdersWithPriority()
    {
        // Arrange
        var layananId = "L05";
        var header1 = new OrderMutasiHeaderView(
            "ORD-LYN-01",
            new DateTime(2026, 10, 27, 11, 0, 0),
            OrderMutasiStateEnum.PartiallyFulfilled,
            new LayananReff(layananId, "Unit Laboratorium"),
            new LayananReff("L03", "Gudang Reagen"),
            OrderMutasiPriorityEnum.Cito,
            "Reagen Darurat");

        var header2 = new OrderMutasiHeaderView(
            "ORD-LYN-02",
            new DateTime(2026, 10, 26, 9, 0, 0),
            OrderMutasiStateEnum.Completed,
            new LayananReff(layananId, "Unit Laboratorium"),
            new LayananReff("L03", "Gudang Reagen"),
            OrderMutasiPriorityEnum.Rutin,
            "Reagen Rutin Selesai");

        _orderMutasiRepoMock.Setup(x => x.ListData(It.Is<ILayananKey>(k => k.LayananId == layananId), It.IsAny<Periode>()))
            .Returns([header1, header2]);

        var sut = new OrderMutasiListByLayananHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiListByLayananQuery(layananId, "2026-10-01", "2026-10-31");

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].OrderMutasiId.Should().Be("ORD-LYN-01");
        result[0].Priority.Should().Be("Cito");
        result[0].State.Should().Be("PartiallyFulfilled");
        result[0].OrderNote.Should().Be("Reagen Darurat");

        result[1].OrderMutasiId.Should().Be("ORD-LYN-02");
        result[1].Priority.Should().Be("Rutin");
        result[1].State.Should().Be("Completed");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OrderMutasiListByLayananHandler_WhenLayananIdNullOrWhitespace_ThrowsArgumentException(string invalidId)
    {
        // Arrange
        var sut = new OrderMutasiListByLayananHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiListByLayananQuery(invalidId, "2026-10-01", "2026-10-31");

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("invalid-date", "2026-10-31")]
    [InlineData("2026-10-01", "not-a-date")]
    public async Task OrderMutasiListByLayananHandler_WhenInvalidDateFormat_ThrowsArgumentException(string d1, string d2)
    {
        // Arrange
        var sut = new OrderMutasiListByLayananHandler(_orderMutasiRepoMock.Object);
        var query = new OrderMutasiListByLayananQuery("L01", d1, d2);

        // Act & Assert
        var act = async () => await sut.Handle(query, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
    #endregion
}
