using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class PostInternalConsumptionVoidConsequenceHandlerTests
{
    private readonly Mock<IStockBatchRepo> _batchRepo = new();
    private readonly Mock<IStockMutasiRepo> _mutasiRepo = new();
    private readonly Mock<IStockLegacyBindingRepo> _bindingRepo = new();
    private readonly Mock<IStockLegacyScopeRepo> _scopeRepo = new();
    private readonly Mock<ILegacyStockReadPort> _legacyRead = new();
    private readonly Mock<IStockConsequenceUnitOfWork> _uow = new();
    private readonly Mock<IMediator> _mediator = new();

    private const string BrgId = "BRG0001";
    private const string DoId = "DO0001";
    private const string LayananId = "LY001";
    private const string UserId = "tester";
    private static readonly DateTime TglMutasi = new(2026, 7, 1, 10, 0, 0);
    private static readonly DateTime TglVoid = new(2026, 7, 2, 11, 0, 0);
    private static readonly DateTime TglEd = new(2027, 6, 15);

    private PostInternalConsumptionVoidConsequenceHandler CreateHandler(
        EnsureFreshnessOutcomeEnum gateOutcome = EnsureFreshnessOutcomeEnum.Success,
        string inconsistencyReason = "",
        string failedBrgId = "",
        string failedDoId = "")
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<EnsureFreshnessForScopesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnsureFreshnessForScopesResult(
                gateOutcome,
                inconsistencyReason,
                failedBrgId,
                failedDoId));

        var gate = new LegacyFreshnessGate(
            _mediator.Object,
            Options.Create(new StockLedgerCoexistenceOptions { CoexistenceEnabled = true }));

        return new PostInternalConsumptionVoidConsequenceHandler(
            gate,
            _batchRepo.Object,
            _mutasiRepo.Object,
            _bindingRepo.Object,
            _scopeRepo.Object,
            _legacyRead.Object,
            _uow.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_RestoresBalance_AndPreparesDraft()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi, poReffId: "PO01");

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(original.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(original.StokMutasiId, "BKORIG01", "TRSPK01", "LOK01", "STORIG01")));
        _mutasiRepo.Setup(x => x.ExistsReversalFor(original.StokMutasiId)).Returns(false);

        var batch = StockBatchModel.Create(BrgId, DoId, 100m, TglMutasi, poReffId: "PO01");
        batch.IncreaseLokasi(LayananId, TglEd, qty: 6);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId)).Returns(MayBe.From(batch));

        _legacyRead.Setup(x => x.ListBalances(BrgId, DoId))
            .Returns([new LegacyStockBalanceReadModel("STORIG01", BrgId, DoId, LayananId, TglEd, "NB01", 6, 100m, TglMutasi)]);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>())).Returns(MayBe<StockLegacyScopeModel>.None);

        StockConsequenceDraft? committedDraft = null;
        _uow.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>()))
            .Callback<StockConsequenceDraft>(d => committedDraft = d);

        var sut = CreateHandler();
        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.Success);
        result.Lines.Should().ContainSingle();
        var line = result.Lines[0];
        line.OriginalStokMutasiId.Should().Be(original.StokMutasiId);
        line.QtyRestored.Should().Be(4);
        line.LegacyStokId.Should().Be("STORIG01");

        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Once);
        committedDraft.Should().NotBeNull();
        committedDraft!.BatchUpserts.Should().ContainSingle();
        committedDraft.BatchUpserts[0].QtySisa.Should().Be(10); // 6 + 4
        committedDraft.MutasiInserts.Should().ContainSingle(m =>
            m.MovementKind == MovementKindEnum.InternalConsumptionVoid
            && m.QtyIn == 4
            && m.QtyOut == 0
            && m.ReversesMutasiId == original.StokMutasiId);
        committedDraft.BindingInserts.Should().ContainSingle(b =>
            b.LegacyStokId == "STORIG01");
        var revOp = committedDraft.LegacyOperations.OfType<LegacyReverseBukuWriteOperation>().Single();
        revOp.Request.MovementKindString.Should().Be("PK_V");
        revOp.Request.QtyIn.Should().Be(4);

        var restoreOp = committedDraft.LegacyOperations.OfType<LegacyStokRestoreWriteOperation>().Single();
        restoreOp.Request.PreferredLegacyStokId.Should().Be("STORIG01");
        restoreOp.Request.QtyIn.Should().Be(4);
    }

    [Fact]
    public async Task Handle_OriginalNotFound_ReturnsOutcome()
    {
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKMISSING")).Returns([]);

        var sut = CreateHandler();
        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPKMISSING", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.OriginalNotFound);
        result.Lines.Should().BeEmpty();
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Unbound_ReturnsUnboundWithoutCommit()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(original.StokMutasiId))
            .Returns(MayBe<StockLegacyBindingModel>.None);

        var sut = CreateHandler();
        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.Unbound);
        result.UnboundStokMutasiId.Should().Be(original.StokMutasiId);
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyReversed_ReturnsAlreadyReversedWithoutCommit()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(original.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(original.StokMutasiId, "BK01", "TRSPK01", "LOK01", "ST01")));
        _mutasiRepo.Setup(x => x.ExistsReversalFor(original.StokMutasiId)).Returns(true);

        var sut = CreateHandler();
        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.AlreadyReversed);
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GateAbort_ReturnsAbortedInconsistentWithoutCommit()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(original.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(original.StokMutasiId, "BK01", "TRSPK01", "LOK01", "ST01")));
        _mutasiRepo.Setup(x => x.ExistsReversalFor(original.StokMutasiId)).Returns(false);

        var sut = CreateHandler(
            gateOutcome: EnsureFreshnessOutcomeEnum.AbortedInconsistent,
            inconsistencyReason: "scope broken",
            failedBrgId: BrgId,
            failedDoId: DoId);

        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.AbortedInconsistent);
        result.InconsistencyReason.Should().Be("scope broken");
        result.FailedBrgId.Should().Be(BrgId);
        result.FailedBrgMasukReffId.Should().Be(DoId);
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }

    [Fact]
    public async Task Handle_IdempotentRetry_ReturnsPriorVoidLinesWithoutCommit()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi);

        var voidMutasi = StockMovementModel.CreateInbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPKVOD01", MovementKindEnum.InternalConsumptionVoid,
            qtyIn: 4, hpp: 100m, tglMutasi: TglVoid,
            reversesMutasiId: original.StokMutasiId);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([voidMutasi]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(voidMutasi.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(voidMutasi.StokMutasiId, "BKVOD01", "TRSPKVOD01", "LOK01", "STORIG01")));

        var sut = CreateHandler();
        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.Idempotent);
        result.Lines.Should().ContainSingle();
        result.Lines[0].VoidStokMutasiId.Should().Be(voidMutasi.StokMutasiId);
        result.Lines[0].QtyRestored.Should().Be(4);
        result.Lines[0].LegacyBukuId.Should().Be("BKVOD01");
        result.Lines[0].LegacyStokId.Should().Be("STORIG01");
        _uow.Verify(x => x.Commit(It.IsAny<StockConsequenceDraft>()), Times.Never);
    }

    [Fact]
    public async Task Handle_IdempotentReplay_MismatchedOriginalConsumption_Throws()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi);

        var voidMutasi = StockMovementModel.CreateInbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPKVOD01", MovementKindEnum.InternalConsumptionVoid,
            qtyIn: 4, hpp: 100m, tglMutasi: TglVoid,
            reversesMutasiId: "DIFFERENT_ORIGINAL");

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([voidMutasi]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original]);

        var sut = CreateHandler();
        var act = () => sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TRSPKVOD01*TRSPK01*");
    }

    [Fact]
    public async Task Handle_NonOutboundOriginal_Throws()
    {
        var invalidOriginal = StockMovementModel.CreateInbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyIn: 4, hpp: 100m, tglMutasi: TglMutasi);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([invalidOriginal]);

        var sut = CreateHandler();
        var act = () => sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-outbound*");
    }

    [Fact]
    public async Task Handle_MultiLineConsumption_RestoresAllLines()
    {
        var original1 = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, "DO01", LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 6, hpp: 50m, tglMutasi: TglMutasi);
        var original2 = StockMovementModel.CreateOutbound(
            "LOK02", "BATCH02", BrgId, "DO02", LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 60m, tglMutasi: TglMutasi);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original1, original2]);

        _bindingRepo.Setup(x => x.FindByStokMutasiId(original1.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(original1.StokMutasiId, "BK01", "TRSPK01", "LOK01", "ST01")));
        _bindingRepo.Setup(x => x.FindByStokMutasiId(original2.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(original2.StokMutasiId, "BK02", "TRSPK01", "LOK02", "ST02")));

        _mutasiRepo.Setup(x => x.ExistsReversalFor(It.IsAny<string>())).Returns(false);

        var batch1 = StockBatchModel.Create(BrgId, "DO01", 50m, TglMutasi);
        batch1.IncreaseLokasi(LayananId, TglEd, qty: 1);
        var batch2 = StockBatchModel.Create(BrgId, "DO02", 60m, TglMutasi);
        batch2.IncreaseLokasi(LayananId, TglEd, qty: 2);

        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, "DO01")).Returns(MayBe.From(batch1));
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, "DO02")).Returns(MayBe.From(batch2));

        _legacyRead.Setup(x => x.ListBalances(BrgId, "DO01"))
            .Returns([new LegacyStockBalanceReadModel("ST01", BrgId, "DO01", LayananId, TglEd, "NB01", 1, 50m, TglMutasi)]);
        _legacyRead.Setup(x => x.ListBalances(BrgId, "DO02"))
            .Returns([new LegacyStockBalanceReadModel("ST02", BrgId, "DO02", LayananId, TglEd, "NB02", 2, 60m, TglMutasi)]);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>())).Returns(MayBe<StockLegacyScopeModel>.None);

        StockConsequenceDraft? draft = null;
        _uow.Setup(x => x.Commit(It.IsAny<StockConsequenceDraft>())).Callback<StockConsequenceDraft>(d => draft = d);

        var sut = CreateHandler();
        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.Success);
        result.Lines.Should().HaveCount(2);
        result.Lines.Sum(x => x.QtyRestored).Should().Be(10);
        batch1.QtySisa.Should().Be(7); // 1 + 6
        batch2.QtySisa.Should().Be(6); // 2 + 4
        draft.Should().NotBeNull();
        draft!.MutasiInserts.Should().HaveCount(2);
        draft.MutasiInserts.Should().AllSatisfy(m => m.MovementKind.Should().Be(MovementKindEnum.InternalConsumptionVoid));
    }

    [Fact]
    public async Task Handle_LegacyStokNoLongerExists_RecreatesWithNewLegacyStokId()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPK01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKVOD01")).Returns([]);
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPK01")).Returns([original]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(original.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(original.StokMutasiId, "BKORIG01", "TRSPK01", "LOK01", "OLD_STOK")));
        _mutasiRepo.Setup(x => x.ExistsReversalFor(original.StokMutasiId)).Returns(false);

        var batch = StockBatchModel.Create(BrgId, DoId, 100m, TglMutasi);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 1);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId)).Returns(MayBe.From(batch));

        // Balance does not have "OLD_STOK" anymore
        _legacyRead.Setup(x => x.ListBalances(BrgId, DoId)).Returns([]);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>())).Returns(MayBe<StockLegacyScopeModel>.None);

        var sut = CreateHandler();
        var result = await sut.Handle(new PostInternalConsumptionVoidConsequenceCommand("TRSPK01", "TRSPKVOD01", TglVoid, UserId), default);

        result.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.Success);
        result.Lines[0].LegacyStokId.Should().NotBe("OLD_STOK");
        result.Lines[0].LegacyStokId.Should().StartWith("ST");
    }

    [Fact]
    public async Task Handle_SameVoidAndOriginalTrsReffId_WorksAndIsIdempotent()
    {
        var original = StockMovementModel.CreateOutbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPKSAME01", MovementKindEnum.InternalConsumption,
            qtyOut: 4, hpp: 100m, tglMutasi: TglMutasi);

        // When queried with "TRSPKSAME01", return both original and (later) void
        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKSAME01")).Returns([original]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(original.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(original.StokMutasiId, "BK01", "TRSPKSAME01", "LOK01", "ST01")));
        _mutasiRepo.Setup(x => x.ExistsReversalFor(original.StokMutasiId)).Returns(false);

        var batch = StockBatchModel.Create(BrgId, DoId, 100m, TglMutasi);
        batch.IncreaseLokasi(LayananId, TglEd, qty: 1);
        _batchRepo.Setup(x => x.LoadByNaturalKey(BrgId, DoId)).Returns(MayBe.From(batch));
        _legacyRead.Setup(x => x.ListBalances(BrgId, DoId))
            .Returns([new LegacyStockBalanceReadModel("ST01", BrgId, DoId, LayananId, TglEd, "NB01", 0, 100m, TglMutasi)]);
        _scopeRepo.Setup(x => x.LoadEntity(It.IsAny<IStockLegacyScopeKey>())).Returns(MayBe<StockLegacyScopeModel>.None);

        var sut = CreateHandler();
        var cmd = new PostInternalConsumptionVoidConsequenceCommand("TRSPKSAME01", "TRSPKSAME01", TglVoid, UserId);

        var first = await sut.Handle(cmd, default);
        first.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.Success);
        first.Lines.Should().ContainSingle();
        first.Lines[0].QtyRestored.Should().Be(4);

        // Now simulate second call where prior void mutasi is returned by repo
        var voidMutasi = StockMovementModel.CreateInbound(
            "LOK01", "BATCH01", BrgId, DoId, LayananId, TglEd,
            "TRSPKSAME01", MovementKindEnum.InternalConsumptionVoid,
            qtyIn: 4, hpp: 100m, tglMutasi: TglVoid,
            reversesMutasiId: original.StokMutasiId);

        _mutasiRepo.Setup(x => x.ListByTrsReffId("TRSPKSAME01")).Returns([original, voidMutasi]);
        _bindingRepo.Setup(x => x.FindByStokMutasiId(voidMutasi.StokMutasiId))
            .Returns(MayBe.From(StockLegacyBindingModel.CreateMutasiBuku(voidMutasi.StokMutasiId, "BKVOD01", "TRSPKSAME01", "LOK01", "ST01")));

        var second = await sut.Handle(cmd, default);
        second.Outcome.Should().Be(PostInternalConsumptionVoidOutcomeEnum.Idempotent);
        second.Lines.Should().ContainSingle();
        second.Lines[0].VoidStokMutasiId.Should().Be(voidMutasi.StokMutasiId);
        second.Lines[0].QtyRestored.Should().Be(4);
    }
}
