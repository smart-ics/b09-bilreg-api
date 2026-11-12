using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PurchaseContext.DeliveryFeature;
using Bilreg.Application.PurchaseContext.DeliveryFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PurchaseContext.DeliveryFeature;

public class DeliveryConfirmCommandTest
{
    private const string DoId = "DM0000001A";
    private const string SuratJalan = "SJ-TEST-12345";
    private const string PoId = "PO26100001";
    private const string SupplierId = "SUP001";
    private const string GudangId = "GDFAR";
    private const string UserId = "USR01";

    private readonly Mock<IDeliveryOrderRepo> _repoMock;
    private readonly Mock<IDeliveryValidationDal> _validationDalMock;
    private readonly Mock<IPoDeliveryTrackingDal> _poTrackingDalMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ITglJamProvider> _clockMock;
    private readonly DateTime _now;
    private readonly DeliveryConfirmHandler _sut;

    public DeliveryConfirmCommandTest()
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

        _sut = new DeliveryConfirmHandler(
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

    private DeliveryOrderModel CreateDraftModel(string poId = PoId, string layananId = GudangId, bool withBonus = false)
    {
        var supplier = new PartnerReff(SupplierId, "PT Kimia Farma");
        var gudang = new LayananReff(layananId, "Gudang Farmasi Utama");

        var model = DeliveryOrderModel.Create(
            SuratJalan,
            poId,
            supplier,
            gudang,
            "Test DO Keterangan",
            UserId,
            _now);

        // Reflectively set DoId to our test DoId for deterministic assertion if needed, or use model.DeliveryOrderId
        var prop = typeof(DeliveryOrderModel).GetProperty(nameof(DeliveryOrderModel.DeliveryOrderId));
        prop?.SetValue(model, DoId);

        var inspection1 = DeliveryInspectionType.Create(
            qtyKirim: 10m,
            qtyDiterima: 10m,
            qtyDitolak: 0m,
            alasanTolak: "",
            isBonus: false,
            hasJaminanRetur: false,
            noSuratJaminan: "",
            poId: poId,
            poNoUrut: 1);

        model.AddItem(
            new BrgReff("BRG001", "Paracetamol 500mg"),
            new SatuanType("BOX", "BOX"),
            konversi: 10,
            harga: 50000m,
            diskon: 5000m,
            taxPercentage: 11m,
            noBatch: "BATCH-001",
            tglEd: _now.AddMonths(24),
            inspection: inspection1,
            referenceDate: _now);

        if (withBonus)
        {
            var inspectionBonus = DeliveryInspectionType.Create(
                qtyKirim: 2m,
                qtyDiterima: 2m,
                qtyDitolak: 0m,
                alasanTolak: "",
                isBonus: true,
                hasJaminanRetur: false,
                noSuratJaminan: "",
                poId: poId,
                poNoUrut: 0);

            model.AddItem(
                new BrgReff("BRG002", "Amoxicillin 500mg"),
                new SatuanType("BOX", "BOX"),
                konversi: 10,
                harga: 75000m,
                diskon: 0m,
                taxPercentage: 0m,
                noBatch: "BATCH-BONUS-01",
                tglEd: _now.AddMonths(24),
                inspection: inspectionBonus,
                referenceDate: _now);
        }

        return model;
    }

    [Fact]
    public async Task Handle_WhenValidDraftWithPo_ConfirmsDocumentAndPostsStockAndUpdatesPo()
    {
        // ARRANGE
        var model = CreateDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
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

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        response.DeliveryOrderId.Should().Be(DoId);
        response.Status.Should().Be("Confirmed");

        savedModel.Should().NotBeNull();
        savedModel!.Status.Should().Be(DeliveryOrderStatusEnum.Confirmed);
        savedModel.Confirmed.UserId.Should().Be(UserId);
        savedModel.Confirmed.Timestamp.Should().Be(_now);

        // Valuation check: (50000 - 5000) / 10 = 4500 HPP; QtyPersediaan = 10 * 10 = 100
        sentStockCmd.Should().NotBeNull();
        sentStockCmd!.BrgId.Should().Be("BRG001");
        sentStockCmd.BrgMasukReffId.Should().Be(DoId);
        sentStockCmd.LayananId.Should().Be(GudangId);
        sentStockCmd.Qty.Should().Be(100m);
        sentStockCmd.Hpp.Should().Be(4500m);
        sentStockCmd.TglMutasi.Should().Be(_now);
        sentStockCmd.UserId.Should().Be(UserId);
        sentStockCmd.SatuanId.Should().Be("TAB");

        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 1, 10m), Times.Once);
        _poTrackingDalMock.Verify(p => p.UpdatePoOverallStatus(PoId), Times.Once);
        _repoMock.Verify(r => r.SaveChanges(model), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidDraftWithoutPo_ConfirmsDocumentAndPostsStockWithoutPoUpdates()
    {
        // ARRANGE
        var model = CreateDraftModel(poId: "");
        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Status.Should().Be("Confirmed");
        _mediatorMock.Verify(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _poTrackingDalMock.Verify(p => p.GetRemainingQuota(It.IsAny<string>()), Times.Never);
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        _poTrackingDalMock.Verify(p => p.UpdatePoOverallStatus(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDocumentNotFound_ThrowsKeyNotFoundException()
    {
        // ARRANGE
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.None);

        var command = new DeliveryConfirmCommand("DM-NONEXIST", UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenDocumentAlreadyConfirmed_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var model = CreateDraftModel();
        model.Confirm("EXISTING_USER", _now.AddHours(-1));
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah berstatus Confirmed*");
    }

    [Fact]
    public async Task Handle_WhenDocumentVoided_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var model = CreateDraftModel();
        model.Void("EXISTING_USER", _now.AddHours(-1));
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Dokumen yang sudah dibatalkan tidak dapat dikonfirmasi*");
    }

    [Fact]
    public async Task Handle_WhenSuratJalanNotUnique_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var model = CreateDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

        _validationDalMock
            .Setup(v => v.IsSuratJalanUnique(SupplierId, SuratJalan, DoId))
            .Returns(false);

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah pernah diterima*");
    }

    [Fact]
    public async Task Handle_WhenPoNotFoundOrNoActiveItems_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var model = CreateDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(new List<PoQuotaDto>());

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Surat Pesanan*tidak ditemukan atau tidak memiliki item aktif*");
    }

    [Fact]
    public async Task Handle_WhenGudangDifferentFromPo_ConfirmsDocumentSuccessfully()
    {
        // ARRANGE
        var model = CreateDraftModel(layananId: "GDLOG");
        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId))
            .Returns(CreateSamplePoQuotas());

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Status.Should().Be("Confirmed");
        _repoMock.Verify(r => r.SaveChanges(model), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReceivingExceedsRemainingQuota_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var model = CreateDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

        var quotas = CreateSamplePoQuotas();
        quotas[0].SisaQty = 5m; // Item 1 only has 5 remaining, but model has 10 accepted
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(quotas);

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*melebihi sisa kuota pesanan*");
    }

    [Fact]
    public async Task Handle_WhenMultiBatchExceedsAggregateQuota_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var model = CreateDraftModel();

        // Add 2nd batch for the same PO item 1
        var inspectionBatch2 = DeliveryInspectionType.Create(
            qtyKirim: 5m,
            qtyDiterima: 5m,
            qtyDitolak: 0m,
            alasanTolak: "",
            isBonus: false,
            hasJaminanRetur: false,
            noSuratJaminan: "",
            poId: PoId,
            poNoUrut: 1);

        model.AddItem(
            new BrgReff("BRG001", "Paracetamol 500mg"),
            new SatuanType("BOX", "BOX"),
            konversi: 10,
            harga: 50000m,
            diskon: 5000m,
            taxPercentage: 11m,
            noBatch: "BATCH-002",
            tglEd: _now.AddMonths(24),
            inspection: inspectionBatch2,
            referenceDate: _now);

        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

        var quotas = CreateSamplePoQuotas();
        quotas[0].SisaQty = 12m; // Total accepted is 10 + 5 = 15 > 12
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(quotas);

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*melebihi sisa kuota pesanan*");
    }

    [Fact]
    public async Task Handle_WhenItemIsBonus_PostsStockWithZeroHppAndDoesNotUpdatePoQuota()
    {
        // ARRANGE
        var model = CreateDraftModel(withBonus: true);
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        var sentStockCmds = new List<PostGoodsReceiptConsequenceCommand>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostGoodsReceiptConsequenceResult>, CancellationToken>((cmd, _) => sentStockCmds.Add((PostGoodsReceiptConsequenceCommand)cmd))
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.Success,
                "SM001", "SL001", "SB001", "BK001", "ST001",
                string.Empty, string.Empty, string.Empty));

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Status.Should().Be("Confirmed");
        sentStockCmds.Should().HaveCount(2);

        var bonusStockCmd = sentStockCmds.FirstOrDefault(x => x.BrgId == "BRG002");
        bonusStockCmd.Should().NotBeNull();
        bonusStockCmd!.Hpp.Should().Be(0m); // Bonus valuation rule: HPP = 0
        bonusStockCmd.Qty.Should().Be(20m); // 2 * 10 konversi
        bonusStockCmd.SatuanId.Should().Be("KAP");

        // Only line 1 (non-bonus) should increment PO quota
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 1, 10m), Times.Once);
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 0, It.IsAny<decimal>()), Times.Never);
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(PoId, 2, It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenItemHasZeroQtyAccepted_DoesNotPostStockAndDoesNotUpdatePoQuota()
    {
        // ARRANGE
        var supplier = new PartnerReff(SupplierId, "PT Kimia Farma");
        var gudang = new LayananReff(GudangId, "Gudang Farmasi Utama");

        var model = DeliveryOrderModel.Create(SuratJalan, PoId, supplier, gudang, "Test", UserId, _now);
        var prop = typeof(DeliveryOrderModel).GetProperty(nameof(DeliveryOrderModel.DeliveryOrderId));
        prop?.SetValue(model, DoId);

        // Item rejected completely
        var rejectedInspection = DeliveryInspectionType.Create(
            qtyKirim: 10m,
            qtyDiterima: 0m,
            qtyDitolak: 10m,
            alasanTolak: "Kemasan Rusak",
            isBonus: false,
            hasJaminanRetur: false,
            noSuratJaminan: "",
            poId: PoId,
            poNoUrut: 1);

        model.AddItem(
            new BrgReff("BRG001", "Paracetamol 500mg"),
            new SatuanType("BOX", "BOX"),
            konversi: 10,
            harga: 50000m,
            diskon: 0m,
            taxPercentage: 11m,
            noBatch: "BATCH-001",
            tglEd: _now.AddMonths(24),
            inspection: rejectedInspection,
            referenceDate: _now);

        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Status.Should().Be("Confirmed");
        _mediatorMock.Verify(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _poTrackingDalMock.Verify(p => p.IncrementReceivedQty(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStockPostFailsWithAbortedInconsistent_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var model = CreateDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.AbortedInconsistent,
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                "Inconsistent stock ledger balance detected", "BRG001", DoId));

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Posting stok untuk barang 'BRG001' gagal: Inconsistent stock ledger balance detected*");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenStockPostThrowsException_PropagatesException()
    {
        // ARRANGE
        var model = CreateDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.IsAny<IDeliveryOrderKey>()))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
        _poTrackingDalMock.Setup(p => p.GetRemainingQuota(PoId)).Returns(CreateSamplePoQuotas());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection timeout to SQL database"));

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Connection timeout to SQL database");

        _repoMock.Verify(r => r.SaveChanges(It.IsAny<DeliveryOrderModel>()), Times.Never);
    }

    [Theory]
    [InlineData("", "USR01")]
    [InlineData("   ", "USR01")]
    [InlineData("DM0000001A", "")]
    [InlineData("DM0000001A", "   ")]
    public async Task Handle_WhenGuardFails_ThrowsArgumentException(string doId, string userId)
    {
        // ARRANGE
        var command = new DeliveryConfirmCommand(doId, userId);

        // ACT & ASSERT
        var act = () => _sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenConfirmedAtIsNull_UsesClockProviderNowForConfirmedAtAndTglMutasi()
    {
        // ARRANGE
        var model = CreateDraftModel();
        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));
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

        // When ConfirmedAt is null (the new frontend behavior)
        var command = new DeliveryConfirmCommand(DoId, UserId, ConfirmedAt: null);

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
        var supplier = new PartnerReff(SupplierId, "PT Kimia Farma");
        var gudang = new LayananReff(GudangId, "Gudang Farmasi Utama");

        var model = DeliveryOrderModel.Create(SuratJalan, PoId, supplier, gudang, "Test DO", UserId, _now);
        var prop = typeof(DeliveryOrderModel).GetProperty(nameof(DeliveryOrderModel.DeliveryOrderId));
        prop?.SetValue(model, DoId);

        var inspection = DeliveryInspectionType.Create(
            qtyKirim: 5m,
            qtyDiterima: 5m,
            qtyDitolak: 0m,
            alasanTolak: "",
            isBonus: false,
            hasJaminanRetur: false,
            noSuratJaminan: "",
            poId: PoId,
            poNoUrut: 1);

        model.AddItem(
            new BrgReff("BRG001", "Paracetamol 500mg"),
            new SatuanType("BTL", "Botol"),
            konversi: 1,
            harga: 25000m,
            diskon: 0m,
            taxPercentage: 11m,
            noBatch: "BATCH-002",
            tglEd: _now.AddMonths(24),
            inspection: inspection,
            referenceDate: _now);

        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

        var quotas = new List<PoQuotaDto>
        {
            new PoQuotaDto(
                poId: PoId,
                noUrut: 1,
                brgId: "BRG001",
                brgName: "Paracetamol 500mg",
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

        PostGoodsReceiptConsequenceCommand? sentStockCmd = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostGoodsReceiptConsequenceResult>, CancellationToken>((cmd, _) => sentStockCmd = (PostGoodsReceiptConsequenceCommand)cmd)
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.Success,
                "SM001", "SL001", "SB001", "BK001", "ST001",
                string.Empty, string.Empty, string.Empty));

        var command = new DeliveryConfirmCommand(DoId, UserId);

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
        var supplier = new PartnerReff(SupplierId, "PT Kimia Farma");
        var gudang = new LayananReff(GudangId, "Gudang Farmasi Utama");

        var model = DeliveryOrderModel.Create(SuratJalan, PoId, supplier, gudang, "Test DO", UserId, _now);
        var prop = typeof(DeliveryOrderModel).GetProperty(nameof(DeliveryOrderModel.DeliveryOrderId));
        prop?.SetValue(model, DoId);

        var inspection = DeliveryInspectionType.Create(
            qtyKirim: 5m,
            qtyDiterima: 5m,
            qtyDitolak: 0m,
            alasanTolak: "",
            isBonus: false,
            hasJaminanRetur: false,
            noSuratJaminan: "",
            poId: PoId,
            poNoUrut: 1);

        model.AddItem(
            new BrgReff("BRG001", "Paracetamol 500mg"),
            new SatuanType("BOX", "Box"),
            konversi: 10,
            harga: 50000m,
            diskon: 0m,
            taxPercentage: 11m,
            noBatch: "BATCH-003",
            tglEd: _now.AddMonths(24),
            inspection: inspection,
            referenceDate: _now);

        _repoMock.Setup(r => r.LoadEntity(It.Is<IDeliveryOrderKey>(k => k.DeliveryOrderId == DoId)))
            .Returns(MayBe<DeliveryOrderModel>.Some(model));

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

        PostGoodsReceiptConsequenceCommand? sentStockCmd = null;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<PostGoodsReceiptConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostGoodsReceiptConsequenceResult>, CancellationToken>((cmd, _) => sentStockCmd = (PostGoodsReceiptConsequenceCommand)cmd)
            .ReturnsAsync(new PostGoodsReceiptConsequenceResult(
                PostGoodsReceiptOutcomeEnum.Success,
                "SM001", "SL001", "SB001", "BK001", "ST001",
                string.Empty, string.Empty, string.Empty));

        var command = new DeliveryConfirmCommand(DoId, UserId);

        // ACT
        var response = await _sut.Handle(command, CancellationToken.None);

        // ASSERT
        response.Should().NotBeNull();
        sentStockCmd.Should().NotBeNull();
        sentStockCmd!.SatuanId.Should().Be("BOX");
    }
}
