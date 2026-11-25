using Bilreg.Application.InventoryContext.OpnameFeature.EventHandlers;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class PostStockAdjustmentConsequenceHandlerTests
{
    private readonly Mock<IStockBatchRepo> _batchRepoMock;
    private readonly Mock<IStockMutasiRepo> _mutasiRepoMock;
    private readonly Mock<IStockLegacyBindingRepo> _bindingRepoMock;
    private readonly Mock<IStockLegacyScopeRepo> _scopeRepoMock;
    private readonly Mock<ILegacyStockReadPort> _legacyReadMock;
    private readonly Mock<IStockConsequenceUnitOfWork> _uowMock;
    private readonly Mock<IStockOpnameAdjustmentRepo> _adjustmentRepoMock;
    private readonly PostStockAdjustmentConsequenceHandler _sut;

    public PostStockAdjustmentConsequenceHandlerTests()
    {
        _batchRepoMock = new Mock<IStockBatchRepo>();
        _mutasiRepoMock = new Mock<IStockMutasiRepo>();
        _bindingRepoMock = new Mock<IStockLegacyBindingRepo>();
        _scopeRepoMock = new Mock<IStockLegacyScopeRepo>();
        _legacyReadMock = new Mock<ILegacyStockReadPort>();
        _uowMock = new Mock<IStockConsequenceUnitOfWork>();
        _adjustmentRepoMock = new Mock<IStockOpnameAdjustmentRepo>();

        _sut = new PostStockAdjustmentConsequenceHandler(
            _batchRepoMock.Object,
            _mutasiRepoMock.Object,
            _bindingRepoMock.Object,
            _scopeRepoMock.Object,
            _legacyReadMock.Object,
            _uowMock.Object,
            _adjustmentRepoMock.Object);
    }

    [Fact]
    public async Task Handle_SurplusAdjustment_PostsAdjustmentPlus_AndCreatesAudit()
    {
        // Arrange
        const string sessionId = "SO-SESSION-001";
        const string layananId = "FAR-01";
        const string brgId = "BRG-001";
        const string batchNo = "BATCH-A";
        const string expDate = "2027-12-31";
        const decimal adjQty = 10m;
        const decimal hpp = 2500m;
        const string satuanId = "TAB";
        const string executedBy = "USER-SPV";

        _batchRepoMock.Setup(x => x.ListAllocationCandidates(brgId, layananId))
            .Returns([]);
        _batchRepoMock.Setup(x => x.LoadByNaturalKey(brgId, It.IsAny<string>()))
            .Returns(MayBe<StockBatchModel>.None);
        _scopeRepoMock.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe<StockLegacyScopeModel>.None);

        StockConsequenceDraft? capturedDraft = null;
        _uowMock.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Callback<StockConsequenceDraft>(d => capturedDraft = d);

        IEnumerable<StockOpnameAdjustmentModel>? capturedAdjustments = null;
        _adjustmentRepoMock.Setup(x => x.Insert(It.IsAny<IEnumerable<StockOpnameAdjustmentModel>>()))
            .Callback<IEnumerable<StockOpnameAdjustmentModel>>(a => capturedAdjustments = a);

        var command = new PostStockAdjustmentConsequenceCommand(
            sessionId,
            layananId,
            [
                new StockAdjustmentItemRequest(brgId, batchNo, expDate, adjQty, hpp, satuanId, executedBy)
            ]);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(PostStockAdjustmentOutcomeEnum.Success);
        result.SessionId.Should().Be(sessionId);
        result.Adjustments.Should().HaveCount(1);

        _adjustmentRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StockOpnameAdjustmentModel>>()), Times.Once);
        capturedAdjustments.Should().NotBeNull();
        var audit = capturedAdjustments!.Single();
        audit.SessionId.Should().Be(sessionId);
        audit.AdjustmentId.Should().StartWith("AJ").And.HaveLength(10);
        audit.BrgId.Should().Be(brgId);
        audit.BatchNo.Should().Be(batchNo);
        audit.AdjustmentQty.Should().Be(10m);
        audit.MovementKind.Should().Be((int)MovementKindEnum.AdjustmentPlus);
        audit.NilaiFinansial.Should().Be(25000m);

        _uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Once);
        capturedDraft.Should().NotBeNull();
        capturedDraft!.MutasiInserts.Should().ContainSingle(m =>
            m.MovementKind == MovementKindEnum.AdjustmentPlus
            && m.QtyIn == 10m
            && m.QtyOut == 0m
            && m.BrgId == brgId
            && m.TrsReffId == audit.AdjustmentId);

        capturedDraft.LegacyOperations.OfType<LegacyInboundWriteOperation>().Should().ContainSingle(inOp =>
            inOp.Request.MovementKindString == "AJ_PLUS"
            && inOp.Request.Qty == 10m
            && inOp.Request.TrsReffId == audit.AdjustmentId);
    }

    [Fact]
    public async Task Handle_DeficitAdjustment_WithSufficientBatchBalance_DeductsFromSpecifiedBatch()
    {
        // Arrange
        const string sessionId = "SO-SESSION-002";
        const string layananId = "FAR-01";
        const string brgId = "BRG-002";
        const string batchNo = "BATCH-B1";
        const string expDate = "2027-06-30";
        var parsedEd = DateTime.Parse(expDate);
        const decimal adjQty = -5m; // Deficit of 5
        const decimal hpp = 5000m;
        const string satuanId = "BTL";
        const string executedBy = "USER-MGR";

        var batch = StockBatchModel.Create(brgId, "DO-001", hpp, new DateTime(2026, 1, 1));
        var lokasi = batch.IncreaseLokasi(layananId, parsedEd, 20m, batchNo);

        _batchRepoMock.Setup(x => x.ListAllocationCandidates(brgId, layananId))
            .Returns([lokasi]);
        _batchRepoMock.Setup(x => x.LoadByNaturalKey(brgId, "DO-001"))
            .Returns(MayBe.From(batch));
        _scopeRepoMock.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>()))
            .Returns(MayBe<StockLegacyScopeModel>.None);

        _legacyReadMock.Setup(x => x.ListBalances(brgId, "DO-001"))
            .Returns([
                new LegacyStockBalanceReadModel("ST001", brgId, "DO-001", layananId, parsedEd, batchNo, 20m, hpp, new DateTime(2026, 1, 1))
            ]);

        StockConsequenceDraft? capturedDraft = null;
        _uowMock.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Callback<StockConsequenceDraft>(d => capturedDraft = d);

        IEnumerable<StockOpnameAdjustmentModel>? capturedAdjustments = null;
        _adjustmentRepoMock.Setup(x => x.Insert(It.IsAny<IEnumerable<StockOpnameAdjustmentModel>>()))
            .Callback<IEnumerable<StockOpnameAdjustmentModel>>(a => capturedAdjustments = a);

        var command = new PostStockAdjustmentConsequenceCommand(
            sessionId,
            layananId,
            [
                new StockAdjustmentItemRequest(brgId, batchNo, expDate, adjQty, hpp, satuanId, executedBy)
            ]);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(PostStockAdjustmentOutcomeEnum.Success);
        result.Adjustments.Should().HaveCount(1);

        lokasi.QtySisa.Should().Be(15m); // 20 - 5 = 15

        _adjustmentRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StockOpnameAdjustmentModel>>()), Times.Once);
        capturedAdjustments.Should().NotBeNull();
        var audit = capturedAdjustments!.Single();
        audit.SessionId.Should().Be(sessionId);
        audit.AdjustmentId.Should().StartWith("AJ").And.HaveLength(10);
        audit.AdjustmentQty.Should().Be(-5m);
        audit.MovementKind.Should().Be((int)MovementKindEnum.AdjustmentMinus);
        audit.NilaiFinansial.Should().Be(-25000m);

        _uowMock.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Once);
        capturedDraft.Should().NotBeNull();
        capturedDraft!.MutasiInserts.Should().ContainSingle(m =>
            m.MovementKind == MovementKindEnum.AdjustmentMinus
            && m.QtyOut == 5m
            && m.QtyIn == 0m
            && m.TrsReffId == audit.AdjustmentId);

        capturedDraft.LegacyOperations.OfType<LegacyOutboundBukuWriteOperation>().Should().Contain(outOp =>
            outOp.Request.MovementKindString == "AJ_MIN"
            && outOp.Request.QtyOut == 5m
            && outOp.Request.TrsReffId == audit.AdjustmentId);

        capturedDraft.LegacyOperations.OfType<LegacyStokDepleteWriteOperation>().Should().Contain(depOp =>
            depOp.Request.LegacyStokId == "ST001"
            && depOp.Request.QtyOut == 5m);
    }

    [Fact]
    public async Task Handle_DeficitAdjustment_WithInsufficientBatchBalance_FallsBackToFEFO()
    {
        // Arrange: TD-10 Tuple Preferred with FEFO Fallback
        const string sessionId = "SO-SESSION-003";
        const string layananId = "FAR-01";
        const string brgId = "BRG-003";
        const string targetBatchNo = "BATCH-TARGET";
        const string targetExpDate = "2028-12-31"; // Late expiry
        var targetEd = DateTime.Parse(targetExpDate);
        const decimal adjQty = -10m; // Need 10 units deficit
        const decimal hpp = 1000m;

        // Preferred batch has only 3 units
        var batchTarget = StockBatchModel.Create(brgId, "DO-TARGET", hpp, new DateTime(2026, 1, 1));
        var lokasiTarget = batchTarget.IncreaseLokasi(layananId, targetEd, 3m, targetBatchNo);

        // Fallback Batch 1 has earliest expiry (2026-06-01), 4 units
        var edFallback1 = new DateTime(2026, 6, 1);
        var batchFb1 = StockBatchModel.Create(brgId, "DO-FB1", hpp, new DateTime(2026, 1, 1));
        var lokasiFb1 = batchFb1.IncreaseLokasi(layananId, edFallback1, 4m, "BATCH-FB1");

        // Fallback Batch 2 has mid expiry (2027-01-01), 10 units
        var edFallback2 = new DateTime(2027, 1, 1);
        var batchFb2 = StockBatchModel.Create(brgId, "DO-FB2", hpp, new DateTime(2026, 1, 1));
        var lokasiFb2 = batchFb2.IncreaseLokasi(layananId, edFallback2, 10m, "BATCH-FB2");

        // Candidates returned in non-FEFO order
        _batchRepoMock.Setup(x => x.ListAllocationCandidates(brgId, layananId))
            .Returns([lokasiTarget, lokasiFb2, lokasiFb1]);

        _batchRepoMock.Setup(x => x.LoadByNaturalKey(brgId, "DO-TARGET"))
            .Returns(MayBe.From(batchTarget));
        _batchRepoMock.Setup(x => x.LoadByNaturalKey(brgId, "DO-FB1"))
            .Returns(MayBe.From(batchFb1));
        _batchRepoMock.Setup(x => x.LoadByNaturalKey(brgId, "DO-FB2"))
            .Returns(MayBe.From(batchFb2));

        _legacyReadMock.Setup(x => x.ListBalances(brgId, "DO-TARGET"))
            .Returns([new LegacyStockBalanceReadModel("ST-TGT", brgId, "DO-TARGET", layananId, targetEd, targetBatchNo, 3m, hpp, new DateTime(2026, 1, 1))]);
        _legacyReadMock.Setup(x => x.ListBalances(brgId, "DO-FB1"))
            .Returns([new LegacyStockBalanceReadModel("ST-FB1", brgId, "DO-FB1", layananId, edFallback1, "BATCH-FB1", 4m, hpp, new DateTime(2026, 1, 1))]);
        _legacyReadMock.Setup(x => x.ListBalances(brgId, "DO-FB2"))
            .Returns([new LegacyStockBalanceReadModel("ST-FB2", brgId, "DO-FB2", layananId, edFallback2, "BATCH-FB2", 10m, hpp, new DateTime(2026, 1, 1))]);

        StockConsequenceDraft? capturedDraft = null;
        _uowMock.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Callback<StockConsequenceDraft>(d => capturedDraft = d);

        IEnumerable<StockOpnameAdjustmentModel>? capturedAdjustments = null;
        _adjustmentRepoMock.Setup(x => x.Insert(It.IsAny<IEnumerable<StockOpnameAdjustmentModel>>()))
            .Callback<IEnumerable<StockOpnameAdjustmentModel>>(a => capturedAdjustments = a);

        var command = new PostStockAdjustmentConsequenceCommand(
            sessionId,
            layananId,
            [
                new StockAdjustmentItemRequest(brgId, targetBatchNo, targetExpDate, adjQty, hpp, "TAB", "SPV")
            ]);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Outcome.Should().Be(PostStockAdjustmentOutcomeEnum.Success);

        // Target depleted from 3 to 0 (3 allocated)
        lokasiTarget.QtySisa.Should().Be(0m);
        // Fallback 1 (earliest ED: 2026-06-01) depleted from 4 to 0 (4 allocated)
        lokasiFb1.QtySisa.Should().Be(0m);
        // Fallback 2 (next ED: 2027-01-01) depleted from 10 to 7 (3 allocated)
        lokasiFb2.QtySisa.Should().Be(7m);
        // Total allocated: 3 + 4 + 3 = 10 units!

        _adjustmentRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StockOpnameAdjustmentModel>>()), Times.Once);
        capturedAdjustments.Should().NotBeNull();
        var audit = capturedAdjustments!.Single();
        audit.SessionId.Should().Be(sessionId);
        audit.AdjustmentId.Should().StartWith("AJ").And.HaveLength(10);
        audit.AdjustmentQty.Should().Be(-10m);

        capturedDraft.Should().NotBeNull();
        capturedDraft!.MutasiInserts.Should().HaveCount(3);
        capturedDraft.MutasiInserts.Sum(m => m.QtyOut).Should().Be(10m);
        capturedDraft.MutasiInserts.Should().OnlyContain(m => m.TrsReffId == audit.AdjustmentId);

        capturedDraft.LegacyOperations.OfType<LegacyOutboundBukuWriteOperation>().Should().HaveCount(3);
        capturedDraft.LegacyOperations.OfType<LegacyOutboundBukuWriteOperation>().Should().OnlyContain(outOp =>
            outOp.Request.MovementKindString == "AJ_MIN"
            && outOp.Request.TrsReffId == audit.AdjustmentId);
    }

    [Fact]
    public async Task Handle_FailureDuringExecution_RollsBackAtomically()
    {
        // Arrange
        const string sessionId = "SO-SESSION-ROLLBACK";
        const string layananId = "FAR-01";
        const string brgId = "BRG-004";

        _batchRepoMock.Setup(x => x.ListAllocationCandidates(brgId, layananId))
            .Returns([]);
        _batchRepoMock.Setup(x => x.LoadByNaturalKey(brgId, It.IsAny<string>()))
            .Returns(MayBe<StockBatchModel>.None);

        // UoW failure throws exception
        _uowMock.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Throws(new InvalidOperationException("Simulated UoW SQL Failure"));

        var command = new PostStockAdjustmentConsequenceCommand(
            sessionId,
            layananId,
            [
                new StockAdjustmentItemRequest(brgId, "BATCH-X", "2027-01-01", 10m, 100m, "TAB", "SPV")
            ]);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Simulated UoW SQL Failure");

        // Adjustment repo should NOT have been called after UoW failure
        _adjustmentRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<StockOpnameAdjustmentModel>>()), Times.Never);
    }

    [Fact]
    public async Task StockOpnameApprovedEventHandler_TransformsEventAndDispatchesCommand()
    {
        // Arrange
        var mediatorMock = new Mock<IMediator>();
        var eventHandler = new StockOpnameApprovedEventHandler(mediatorMock.Object);

        var domainEvent = new StockOpnameApprovedEvent(
            SessionId: "SESSION-EVT-01",
            LayananId: "LY-01",
            ApprovedBy: "MGR-01",
            ApprovedAt: DateTime.Now,
            Items:
            [
                new StockOpnameApprovedItem("BRG-A", "BATCH-1", "2026-12-31", 5m, 1000m, "TAB"),
                new StockOpnameApprovedItem("BRG-B", "BATCH-2", "2027-06-30", -3m, 2000m, "AMP"),
                new StockOpnameApprovedItem("BRG-C", "BATCH-3", "2027-01-01", 0m, 500m, "BTL") // Zero variance, should be filtered
            ]);

        PostStockAdjustmentConsequenceCommand? capturedCmd = null;
        mediatorMock.Setup(x => x.Send(It.IsAny<PostStockAdjustmentConsequenceCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PostStockAdjustmentConsequenceResult>, CancellationToken>((cmd, _) =>
                capturedCmd = (PostStockAdjustmentConsequenceCommand)cmd)
            .ReturnsAsync(new PostStockAdjustmentConsequenceResult(PostStockAdjustmentOutcomeEnum.Success, "SESSION-EVT-01", []));

        // Act
        await eventHandler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        mediatorMock.Verify(x => x.Send(It.IsAny<PostStockAdjustmentConsequenceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        capturedCmd.Should().NotBeNull();
        capturedCmd!.SessionId.Should().Be("SESSION-EVT-01");
        capturedCmd.LayananId.Should().Be("LY-01");
        capturedCmd.Items.Should().HaveCount(2); // Only non-zero variance items

        var itemA = capturedCmd.Items.Single(x => x.BrgId == "BRG-A");
        itemA.AdjustmentQty.Should().Be(5m);
        itemA.ExecutedBy.Should().Be("MGR-01");

        var itemB = capturedCmd.Items.Single(x => x.BrgId == "BRG-B");
        itemB.AdjustmentQty.Should().Be(-3m);
        itemB.ExecutedBy.Should().Be("MGR-01");
    }
}
