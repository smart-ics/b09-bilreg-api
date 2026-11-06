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

    private static List<PoQuotaDto> CreateSamplePoQuotas()
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
                konversi: 10,
                harga: 50000m,
                diskon: 5000m,
                satuanKecilId: "TAB"),
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
                konversi: 10,
                harga: 75000m,
                diskon: 0m,
                satuanKecilId: "KAP")
        ];
    }

    private List<DeliveryDirectConfirmItemCommand> CreateSampleItems(DateTime? edDate = null, decimal het = 0m)
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
                PoNoUrut: 1,
                Het: het)
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
        savedModel.Confirmed.Timestamp.Should().Be(_now);

        // Valuation check: (50000 - 5000) / 10 = 4500 HPP; QtyPersediaan = 10 * 10 = 100
        sentStockCmd.Should().NotBeNull();
        sentStockCmd!.BrgId.Should().Be("BRG001");
        sentStockCmd.BrgMasukReffId.Should().Be(savedModel.DeliveryOrderId);
        sentStockCmd.LayananId.Should().Be(GudangId);
        sentStockCmd.Qty.Should().Be(100m);
        sentStockCmd.Hpp.Should().Be(4500m);
        sentStockCmd.TglMutasi.Should().Be(_now);
        sentStockCmd.UserId.Should().Be(UserId);
        sentStockCmd.SatuanId.Should().Be("TAB");

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
    public async Task Handle_WhenGudangDifferentFromPo_ConfirmsDocumentSuccessfully()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: "GDLOG",
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems());

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Status.Should().Be("Confirmed");
        savedModel.Should().NotBeNull();
        savedModel!.Gudang.LayananId.Should().Be("GDLOG");
        _repoMock.Verify(r => r.SaveChanges(savedModel), Times.Once);
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
        bonusStockCmd.SatuanId.Should().Be("KAP");

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

    [Fact]
    public async Task Handle_WhenDeliveryOrderDateHasNoTime_CombinesWithCurrentTime()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var dateOnly = new DateTime(2026, 10, 21); // 00:00:00
        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(),
            DeliveryOrderDate: dateOnly);

        // ACT
        await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        savedModel.Should().NotBeNull();
        savedModel!.DeliveryOrderDate.Should().Be(new DateTime(2026, 10, 21, 10, 0, 0));
        savedModel.DeliveryOrderDate.TimeOfDay.Should().Be(_now.TimeOfDay);
    }

    [Fact]
    public async Task Handle_WhenValidDirectConfirmWithHet_PassesHetToDomainModel()
    {
        // ARRANGE
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());
        DeliveryOrderModel? savedModel = null;
        _repoMock.Setup(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()))
            .Callback<DeliveryOrderModel>(m => savedModel = m);

        var command = new DeliveryDirectConfirmCommand(
            SuratJalanNo: SuratJalan,
            PoId: PoId,
            SupplierId: SupplierId,
            GudangId: GudangId,
            Keterangan: Keterangan,
            UserId: UserId,
            Items: CreateSampleItems(het: 80000m),
            SupplierName: SupplierName,
            GudangName: GudangName);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        savedModel.Should().NotBeNull();
        savedModel!.ListItem.Should().HaveCount(1);
        savedModel.ListItem.First().Het.Should().Be(80000m);
    }

    [Fact]
    public async Task Handle_WhenConfirmedAtIsNull_UsesClockProviderNowForConfirmedAtAndTglMutasi()
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
            GudangName: GudangName,
            ConfirmedAt: null);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.Status.Should().Be("Confirmed");

        savedModel.Should().NotBeNull();
        savedModel!.Status.Should().Be(DeliveryOrderStatusEnum.Confirmed);
        savedModel.Confirmed.UserId.Should().Be(UserId);
        savedModel.Confirmed.Timestamp.Should().Be(_now);

        sentStockCmd.Should().NotBeNull();
        sentStockCmd!.TglMutasi.Should().Be(_now);
    }

    [Fact]
    public async Task Handle_WhenConversionEqualsOne_PassesItemSatuanIdToStockConsequenceCommand()
    {
        // ARRANGE
        var quotas = new List<PoQuotaDto>
        {
            new PoQuotaDto(
                poId: PoId,
                noUrut: 1,
                brgId: "BRG001",
                brgName: "Paracetamol Sirup 60ml",
                satuanId: "BTL",
                qtyPo: 10m,
                qtyDo: 0m,
                sisaQty: 10m,
                isClosed: false,
                konversi: 1,
                harga: 25000m,
                diskon: 0m,
                satuanKecilId: "BTL")
        };
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(quotas);

        var items = new List<DeliveryDirectConfirmItemCommand>
        {
            new(
                BrgId: "BRG001",
                SatuanId: "BTL",
                Konversi: 1,
                Harga: 25000m,
                Diskon: 0m,
                TaxPercentage: 11m,
                NoBatch: "BATCH-002",
                TglEd: _now.AddMonths(24),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol Sirup 60ml",
                SatuanName: "Botol",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

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
            Items: items);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        sentStockCmd.Should().NotBeNull();
        sentStockCmd!.SatuanId.Should().Be("BTL");
    }

    [Fact]
    public async Task Handle_WhenConversionGreaterThanOneButSatuanKecilEmpty_FallsBackToItemSatuanId()
    {
        // ARRANGE
        var quotas = new List<PoQuotaDto>
        {
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
                konversi: 10,
                harga: 50000m,
                diskon: 0m,
                satuanKecilId: "")
        };
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
                NoBatch: "BATCH-003",
                TglEd: _now.AddMonths(24),
                QtyKirim: 5m,
                QtyDiterima: 5m,
                QtyDitolak: 0m,
                AlasanTolak: "",
                BrgName: "Paracetamol 500mg",
                SatuanName: "Box",
                IsBonus: false,
                HasJaminanRetur: false,
                NoSuratJaminan: "",
                PoNoUrut: 1)
        };

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
            Items: items);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        sentStockCmd.Should().NotBeNull();
        sentStockCmd!.SatuanId.Should().Be("BOX");
    }
}
