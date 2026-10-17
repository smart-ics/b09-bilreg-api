using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryDirectConfirmCommandTest
{
    private const string SuratJalan = "SJ-TEST-12345";
    private const string PoId = "PO26100001";
    private const string SupplierId = "SUP001";
    private const string SupplierName = "PT Kimia Farma";
    private const string GudangId = "GDFAR";
    private const string GudangName = "Gudang Farmasi Utama";
    private const string UserId = "USR01";
    private const string Keterangan = "Direct confirm penerimaan barang";

    private readonly Mock<IDeliveryOrderRepo> _repoMock;
    private readonly Mock<IDeliveryValidationDal> _validationDalMock;
    private readonly Mock<IPoDeliveryTrackingDal> _poTrackingDalMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ITglJamProvider> _clockMock;
    private readonly DateTime _now;
    private readonly DeliveryDirectConfirmHandler _sut;

    public DeliveryDirectConfirmCommandTest()
    {
        _repoMock = new Mock<IDeliveryOrderRepo>();
        _validationDalMock = new Mock<IDeliveryValidationDal>();
        _poTrackingDalMock = new Mock<IPoDeliveryTrackingDal>();
        _mediatorMock = new Mock<IMediator>();
        _clockMock = new Mock<ITglJamProvider>();

        _now = new DateTime(2026, 10, 15, 10, 0, 0);
        _clockMock.Setup(c => c.Now).Returns(_now);

        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(true);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.Success,
                "SM001", "SL001", "SB001", "BK001", "ST001",
                string.Empty, string.Empty, string.Empty));

        _sut = new DeliveryDirectConfirmHandler(
            _repoMock.Object,
            _validationDalMock.Object,
            _poTrackingDalMock.Object,
            _mediatorMock.Object,
            _clockMock.Object);
    }

    private static List<PoQuotaDto> CreateSamplePoQuotas(string layananId = GudangId)
    {
        return
        [
            new PoQuotaDto(
                poId: PoId,
                noUrut: 1,
                brgId: "BRG001",
                brgName: "Paracetamol 500mg",
                satuanId: "BOX",
                qtyPo: 10m,
                qtyDo: 0m,
                sisaQty: 10m,
                isClosed: false,
                layananId: layananId,
                konversi: 10,
                harga: 50000m,
                diskon: 5000m),
            new PoQuotaDto(
                poId: PoId,
                noUrut: 2,
                brgId: "BRG002",
                brgName: "Amoxicillin 500mg",
                satuanId: "BOX",
                qtyPo: 20m,
                qtyDo: 5m,
                sisaQty: 15m,
                isClosed: false,
                layananId: layananId,
                konversi: 10,
                harga: 75000m,
                diskon: 0m)
        ];
    }

    private List<DeliveryDirectConfirmItemCommand> CreateSampleItems(DateTime? edDate = null)
    {
        var ed = edDate ?? _now.AddMonths(24);
        return
        [
            new DeliveryDirectConfirmItemCommand(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 5000m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-001",
                TglEd: ed,
                QtyKirim: 10m,
                QtyDiterima: 10m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        ];
    }

    [Fact]
    public async Task Handle_WhenValidDirectConfirmWithPo_CreatesAndConfirmsDocumentAndPostsStockAndUpdatesPo()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        PostGoodsReceiptConsequenceCommand? sentStockCmd = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostGoodsReceiptConsequenceResult>, CancellationToken>((cmd, _) => sentStockCmd = (PostGoodsReceiptConsequenceCommand)cmd)
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.Success,
                "SM001", "SL001", "SB001", "BK001", "ST001",
                string.Empty, string.Empty, string.Empty));

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            SupplierName: SupplierName,
            GudangName: GudangName);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().StartWith("DM");
        response.Status.Should().Be("Confirmed");

        savedModel.Should().NotBeNull();
        savedModel!.Status.Should().Be(DeliveryOrderStatusEnum.Confirmed);
        savedModel.Confirmed.UserId.Should().Be(UserId);

        // Valuation check: (50000 - 5000) / 10 = 4500 HPP; QtyPersediaan = 10 * 10 = 100
        sentStockCmd.Should().NotBeNull();
        sentStockCmd!.BrgId.Should().Be("BRG001");
        sentStockCmd.BrgMasukReffId.Should().Be(savedModel.DeliveryOrderId);
        sentStockCmd.LayananId.Should().Be(GudangId);
        sentStockCmd.Qty.Should().Be(100m);
        sentStockCmd.Hpp.Should().Be(4500m);
        sentStockCmd.UserId.Should().Be(UserId);

        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 1, 10m), Times.Once);
        _poTrackingDalMock.Verify(p => p.UpdatePoOverallStatus(PoId), Times.Once);
        _repoMock.Verify(r => r.SaveChanges(savedModel), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidDirectConfirmWithoutPo_CreatesAndConfirmsDocumentAndPostsStock()
    {
        // ARRANGE
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var items = CreateSampleItems();
        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: "",
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: items);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Status.Should().Be("Confirmed");
        savedModel.Should().NotBeNull();
        savedModel!.Status.Should().Be(DeliveryOrderStatusEnum.Confirmed);

        _mediatorMock.Verify(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _poTrackingDalMock.Verify(p => p.GetRemainingQuota(It.IsAny<string>()), Times.Never);
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        _poTrackingDalMock.Verify(p => p.UpdatePoOverallStatus(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSuratJalanNotUnique_ThrowsInvalidOperationException()
    {
        // ARRANGE
        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(SupplierId, SuratJalan, null))
            .Returns(false);

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah pernah diterima*");
    }

    [Fact]
    public async Task Handle_WhenItemsEmpty_ThrowsArgumentException()
    {
        // ARRANGE
        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: []);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak boleh kosong*");
    }

    [Fact]
    public async Task Handle_WhenPoNotFound_ThrowsInvalidOperationException()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(new List<PoQuotaDto>());

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Surat Pesanan*tidak ditemukan atau tidak memiliki item aktif*");
    }

    [Fact]
    public async Task Handle_WhenGudangDoesNotMatchPo_ThrowsInvalidOperationException()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas(layananId: "GDFAR_OTHER"));

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak sesuai dengan gudang tujuan PO*");
    }

    [Fact]
    public async Task Handle_WhenReceivingExceedsRemainingQuota_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var quotas = CreateSamplePoQuotas();
        quotas[0].SisaQty = 5m;
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(quotas);

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*melebihi sisa kuota pesanan*");
    }

    [Fact]
    public async Task Handle_WhenMultiBatchExceedsAggregateQuota_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var quotas = CreateSamplePoQuotas();
        quotas[0].SisaQty = 12m;
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(quotas);

        var items = new List<DeliveryDirectConfirmItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-001",
                TglEd: _now.AddMonths(24),
                QtyKirim: 10m,
                QtyDiterima: 10m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1),
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-002",
                TglEd: _now.AddMonths(24),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: items);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*melebihi sisa kuota pesanan*");
    }

    [Fact]
    public async Task Handle_WhenItemIsBonus_PostsStockWithZeroHppAndDoesNotUpdatePoQuota()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        var items = new List<DeliveryDirectConfirmItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 50000m,
                Diskon: 5000m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-001",
                TglEd: _now.AddMonths(24),
                QtyKirim: 10m,
                QtyDiterima: 10m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "BOX",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1),
            new(
                BrgId: "BRG002",
                SatuanId: "BOX",
                Konversi: 10,
                Harga: 75000m,
                Diskon: 0m,
                TaxPercentage: 0m,
                NoBatch: "BATCH-BONUS-01",
                TglEd: _now.AddMonths(24),
                QtyKirim: 2m,
                QtyDiterima: 2m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Amoxicillin 500mg",
                SatuanName: "BOX",
                IsBonus: true,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 0)
        };

        var sentStockCmds = new List<PostGoodsReceiptConsequenceCommand>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostGoodsReceiptConsequenceResult>, CancellationToken>((cmd, _) => sentStockCmds.Add((PostGoodsReceiptConsequenceCommand)cmd))
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.Success,
                "SM001", "SL001", "SB001", "BK001", "ST001",
                string.Empty, string.Empty, string.Empty));

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: items);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Status.Should().Be("Confirmed");
        sentStockCmds.Should().HaveCount(2);

        var bonusStockCmd = sentStockCmds.FirstOrDefault(x => x.BrgId == "BRG002");
        bonusStockCmd.Should().NotBeNull();
        bonusStockCmd!.Hpp.Should().Be(0m);
        bonusStockCmd.Qty.Should().Be(20m);

        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 1, 10m), Times.Once);
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 0, It.IsAny<decimal>()), Times.Never);
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 2, It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStockPostFails_ThrowsInvalidOperationException()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.AbortedInconsistent,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                "Failed to update stock", "BRG001", "DM..."));

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Posting stok untuk barang 'BRG001' gagal: Failed to update stock*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStockPostThrowsException_PropagatesException()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB Deadlock"));

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB Deadlock");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Theory]
    [InlineData("", "SUP001", "GDFAR", "USR01")]
    [InlineData("   ", "SUP001", "GDFAR", "USR01")]
    [InlineData("SJ01", "", "GDFAR", "USR01")]
    [InlineData("SJ01", "SUP001", "", "USR01")]
    [InlineData("SJ01", "SUP001", "GDFAR", "")]
    public async Task Handle_WhenGuardFails_ThrowsArgumentException(string sj, string sup, string gdg, string usr)
    {
        // ARRANGE
        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: sj,
            PoId: PoId,
            SupplierId: sup,
            GudangId: gdg,
            Keterangan: Keterangan,
            UserId: usr,
            Items: CreateSampleItems());

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
