using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class SessionLifecycleCommandsTests
{
    private readonly Mock<IStockOpnameSessionRepo> _sessionRepoMock = new();
    private readonly Mock<ISystemQuantityReconstructionService> _reconstructionServiceMock = new();

    #region TEST HELPERS
    private static StockOpnameSessionModel CreateSampleSession(
        string sessionId = "SES-001",
        string layananId = "FAR01",
        SessionScopeEnum scopeMode = SessionScopeEnum.Full,
        SessionStatusEnum status = SessionStatusEnum.Draft,
        DateTime? cutoffDate = null,
        IEnumerable<StockOpnameScopeItemModel>? scopeItems = null,
        IEnumerable<StockOpnameCountedItemModel>? countedItems = null,
        IEnumerable<StockOpnameVarianceItemModel>? varianceItems = null)
    {
        return new StockOpnameSessionModel(
            sessionId: sessionId,
            layananId: layananId,
            scopeMode: scopeMode,
            status: status,
            cutoffDate: cutoffDate ?? new DateTime(2026, 11, 20, 8, 0, 0),
            createdBy: "USR01",
            createdAt: new DateTime(2026, 11, 20, 8, 0, 0),
            countingSubmittedBy: "",
            countingSubmittedAt: null,
            verifiedBy: "",
            verifiedAt: null,
            approvedBy: "",
            approvedAt: null,
            rejectedBy: "",
            rejectedAt: null,
            cancelledBy: "",
            cancelledAt: null,
            cancelReason: "",
            revisionNotes: "",
            scopeItems: scopeItems,
            countedItems: countedItems,
            varianceItems: varianceItems);
    }
    #endregion

    #region CREATE SESSION COMMAND TESTS
    [Fact]
    public async Task CreateSession_WhenActiveSessionExistsAtLocation_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.HasActiveSessionAtLocation("FAR01")).Returns(true);
        var handler = new CreateSessionHandler(_sessionRepoMock.Object);
        var cmd = new CreateSessionCommand("FAR01", SessionScopeEnum.Full, DateTime.Today, "USR01");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active stock opname session already exists*");
        _sessionRepoMock.Verify(r => r.SaveChanges(It.IsAny<StockOpnameSessionModel>()), Times.Never);
    }

    [Fact]
    public async Task CreateSession_FullSession_ShouldCreateDraftSessionWithoutScopeItems()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.HasActiveSessionAtLocation("FAR01")).Returns(false);
        StockOpnameSessionModel? savedModel = null;
        _sessionRepoMock.Setup(r => r.SaveChanges(It.IsAny<StockOpnameSessionModel>()))
            .Callback<StockOpnameSessionModel>(m => savedModel = m);

        var handler = new CreateSessionHandler(_sessionRepoMock.Object);
        var cmd = new CreateSessionCommand(
            LayananId: "FAR01",
            ScopeMode: SessionScopeEnum.Full,
            CutoffDate: new DateTime(2026, 11, 20, 8, 0, 0),
            CreatedBy: "USR01",
            ScopeItems: null,
            SessionId: "SO-TEST-FULL");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be("SO-TEST-FULL");
        result.LayananId.Should().Be("FAR01");
        result.ScopeMode.Should().Be(SessionScopeEnum.Full);
        result.Status.Should().Be(SessionStatusEnum.Draft);
        result.ScopeItemsCount.Should().Be(0);

        savedModel.Should().NotBeNull();
        savedModel!.SessionId.Should().Be("SO-TEST-FULL");
        savedModel.Status.Should().Be(SessionStatusEnum.Draft);
        savedModel.ScopeItems.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSession_PartialSession_ShouldCreateDraftSessionWithScopeItems()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.HasActiveSessionAtLocation("FAR02")).Returns(false);
        StockOpnameSessionModel? savedModel = null;
        _sessionRepoMock.Setup(r => r.SaveChanges(It.IsAny<StockOpnameSessionModel>()))
            .Callback<StockOpnameSessionModel>(m => savedModel = m);

        var handler = new CreateSessionHandler(_sessionRepoMock.Object);
        var scopeItems = new List<CreateSessionScopeItemRequest>
        {
            new("BRG01", "Paracetamol 500mg"),
            new("BRG02", "Amoxicillin 500mg")
        };
        var cmd = new CreateSessionCommand(
            LayananId: "FAR02",
            ScopeMode: SessionScopeEnum.Partial,
            CutoffDate: new DateTime(2026, 11, 20, 8, 0, 0),
            CreatedBy: "USR02",
            ScopeItems: scopeItems,
            SessionId: "SO-TEST-PARTIAL");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be("SO-TEST-PARTIAL");
        result.ScopeMode.Should().Be(SessionScopeEnum.Partial);
        result.ScopeItemsCount.Should().Be(2);

        savedModel.Should().NotBeNull();
        savedModel!.ScopeItems.Should().HaveCount(2);
        savedModel.ScopeItems.Select(s => s.BrgId).Should().Contain(["BRG01", "BRG02"]);
    }
    #endregion

    #region START COUNTING COMMAND TESTS
    [Fact]
    public async Task StartCounting_WhenSessionInDraft_ShouldTransitionToCounting()
    {
        // Arrange
        var session = CreateSampleSession("SES-DRAFT", status: SessionStatusEnum.Draft);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-DRAFT")))
            .Returns(MayBe.From(session));

        var handler = new StartCountingHandler(_sessionRepoMock.Object);
        var cmd = new StartCountingCommand("SES-DRAFT");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be("SES-DRAFT");
        result.Status.Should().Be(SessionStatusEnum.Counting);
        session.Status.Should().Be(SessionStatusEnum.Counting);
        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }

    [Fact]
    public async Task StartCounting_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.LoadEntity(It.IsAny<IStockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var handler = new StartCountingHandler(_sessionRepoMock.Object);
        var cmd = new StartCountingCommand("SES-UNKNOWN");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SES-UNKNOWN*");
    }

    [Fact]
    public async Task StartCounting_WhenSessionNotInDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession("SES-COUNTING", status: SessionStatusEnum.Counting);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-COUNTING")))
            .Returns(MayBe.From(session));

        var handler = new StartCountingHandler(_sessionRepoMock.Object);
        var cmd = new StartCountingCommand("SES-COUNTING");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot start counting from status Counting*");
    }
    #endregion

    #region SUBMIT COUNTING COMMAND TESTS
    [Fact]
    public async Task SubmitCounting_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.LoadEntity(It.IsAny<IStockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var handler = new SubmitCountingHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new SubmitCountingCommand("SES-UNKNOWN", "USR01");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SES-UNKNOWN*");
    }

    [Fact]
    public async Task SubmitCounting_WhenSessionNotInCounting_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession("SES-DRAFT", status: SessionStatusEnum.Draft);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-DRAFT")))
            .Returns(MayBe.From(session));

        var handler = new SubmitCountingHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new SubmitCountingCommand("SES-DRAFT", "USR01");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot submit counting from status Draft*");
    }

    [Fact]
    public async Task SubmitCounting_ZeroVarianceBypass_WhenAllItemsMatch_ShouldTransitionToWaitingApproval()
    {
        // Arrange
        var counted = new List<StockOpnameCountedItemModel>
        {
            StockOpnameCountedItemModel.Create("CNT-1", "SES-ZV", "BRG01", "B01", "2027-12-31", "TAB", 100, new DateTime(2026, 11, 20, 10, 0, 0))
        };
        var session = CreateSampleSession("SES-ZV", status: SessionStatusEnum.Counting, countedItems: counted);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-ZV")))
            .Returns(MayBe.From(session));

        var systemBalances = new Dictionary<ItemTupleKey, decimal>
        {
            [new ItemTupleKey("BRG01", "B01", "2027-12-31")] = 100m
        };
        _reconstructionServiceMock.Setup(s => s.ReconstructBalancesAsync(
            session.LayananId,
            It.IsAny<DateTime>(),
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemBalances);

        var handler = new SubmitCountingHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new SubmitCountingCommand("SES-ZV", "USR01");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert (BR-SO-010: Zero-Variance Bypass -> WaitingApproval)
        result.Should().NotBeNull();
        result.Status.Should().Be(SessionStatusEnum.WaitingApproval);
        result.HasVariance.Should().BeFalse();
        result.TotalVarianceItems.Should().Be(0);
        result.NetValuation.Should().Be(0m);

        session.Status.Should().Be(SessionStatusEnum.WaitingApproval);
        session.VarianceItems.Should().BeEmpty();
        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }

    [Fact]
    public async Task SubmitCounting_WithDiscrepancy_ShouldTransitionToWaitingVerificationAndCalculateValuations()
    {
        // Arrange
        var counted = new List<StockOpnameCountedItemModel>
        {
            // Physical = 120, System = 100 -> Surplus +20
            StockOpnameCountedItemModel.Create("CNT-1", "SES-VAR", "BRG01", "B01", "2027-12-31", "TAB", 120, new DateTime(2026, 11, 20, 10, 0, 0)),
            // Physical = 80, System = 100 -> Deficit -20
            StockOpnameCountedItemModel.Create("CNT-2", "SES-VAR", "BRG02", "B02", "2027-12-31", "TAB", 80, new DateTime(2026, 11, 20, 10, 0, 0))
        };
        var session = CreateSampleSession("SES-VAR", status: SessionStatusEnum.Counting, countedItems: counted);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-VAR")))
            .Returns(MayBe.From(session));

        var systemBalances = new Dictionary<ItemTupleKey, decimal>
        {
            [new ItemTupleKey("BRG01", "B01", "2027-12-31")] = 100m,
            [new ItemTupleKey("BRG02", "B02", "2027-12-31")] = 100m
        };
        _reconstructionServiceMock.Setup(s => s.ReconstructBalancesAsync(
            session.LayananId,
            It.IsAny<DateTime>(),
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemBalances);

        var hppMap = new Dictionary<string, decimal>
        {
            ["BRG01"] = 1000m, // Surplus 20 * 1000 = +20,000
            ["BRG02"] = 500m   // Deficit 20 * 500 = 10,000
        };

        var handler = new SubmitCountingHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new SubmitCountingCommand("SES-VAR", "USR01", ItemHppMap: hppMap);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(SessionStatusEnum.WaitingVerification);
        result.HasVariance.Should().BeTrue();
        result.TotalVarianceItems.Should().Be(2);
        result.SurplusValuation.Should().Be(20000m);
        result.DeficitValuation.Should().Be(10000m);
        result.NetValuation.Should().Be(10000m);

        session.Status.Should().Be(SessionStatusEnum.WaitingVerification);
        session.VarianceItems.Should().HaveCount(2);
        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }

    [Fact]
    public async Task SubmitCounting_FullSession_UncountedSystemStock_ShouldEvaluateAsMissingStock()
    {
        // Arrange (BR-SO-007: In Full opname, uncounted system stock with positive balance becomes CountQty = 0)
        var counted = new List<StockOpnameCountedItemModel>
        {
            StockOpnameCountedItemModel.Create("CNT-1", "SES-FULL", "BRG01", "B01", "2027-12-31", "TAB", 50, new DateTime(2026, 11, 20, 10, 0, 0))
        };
        var session = CreateSampleSession("SES-FULL", scopeMode: SessionScopeEnum.Full, status: SessionStatusEnum.Counting, countedItems: counted);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-FULL")))
            .Returns(MayBe.From(session));

        var systemBalances = new Dictionary<ItemTupleKey, decimal>
        {
            [new ItemTupleKey("BRG01", "B01", "2027-12-31")] = 50m, // Counted matches system
            [new ItemTupleKey("BRG02", "B02", "2027-12-31")] = 30m  // Uncounted system balance!
        };
        _reconstructionServiceMock.Setup(s => s.ReconstructBalancesAsync(
            session.LayananId,
            It.IsAny<DateTime>(),
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemBalances);

        var hppMap = new Dictionary<string, decimal>
        {
            ["BRG01"] = 1000m,
            ["BRG02"] = 2000m
        };

        var handler = new SubmitCountingHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new SubmitCountingCommand("SES-FULL", "USR01", ItemHppMap: hppMap);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SessionStatusEnum.WaitingVerification);
        result.HasVariance.Should().BeTrue();
        result.TotalVarianceItems.Should().Be(1); // Only BRG02 is variance, BRG01 had 0 variance
        result.DeficitValuation.Should().Be(60000m); // 30 * 2000

        var missingStockItem = session.VarianceItems.FirstOrDefault(v => v.BrgId == "BRG02");
        missingStockItem.Should().NotBeNull();
        missingStockItem!.CountQty.Should().Be(0m);
        missingStockItem.SystemQty.Should().Be(30m);
        missingStockItem.VarianceQty.Should().Be(-30m);
    }

    [Fact]
    public async Task SubmitCounting_PartialSession_ShouldPassScopedBrgIdsToReconstructionService()
    {
        // Arrange
        var scopeItems = new List<StockOpnameScopeItemModel>
        {
            StockOpnameScopeItemModel.Create("SCP-1", "SES-PARTIAL", "BRG01", "Paracetamol")
        };
        var counted = new List<StockOpnameCountedItemModel>
        {
            StockOpnameCountedItemModel.Create("CNT-1", "SES-PARTIAL", "BRG01", "B01", "2027-12-31", "TAB", 10, new DateTime(2026, 11, 20, 10, 0, 0))
        };
        var session = CreateSampleSession("SES-PARTIAL", scopeMode: SessionScopeEnum.Partial, status: SessionStatusEnum.Counting,
            scopeItems: scopeItems, countedItems: counted);

        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-PARTIAL")))
            .Returns(MayBe.From(session));

        IEnumerable<string>? capturedFilter = null;
        _reconstructionServiceMock.Setup(s => s.ReconstructBalancesAsync(
            session.LayananId,
            It.IsAny<DateTime>(),
            It.IsAny<IEnumerable<string>?>(),
            It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, IEnumerable<string>?, CancellationToken>((l, dt, filter, ct) => capturedFilter = filter)
            .ReturnsAsync(new Dictionary<ItemTupleKey, decimal>
            {
                [new ItemTupleKey("BRG01", "B01", "2027-12-31")] = 10m
            });

        var handler = new SubmitCountingHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new SubmitCountingCommand("SES-PARTIAL", "USR01");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Status.Should().Be(SessionStatusEnum.WaitingApproval);
        capturedFilter.Should().NotBeNull();
        capturedFilter.Should().Contain("BRG01");
    }
    #endregion

    #region CANCEL SESSION COMMAND TESTS
    [Fact]
    public async Task CancelSession_WhenSessionIsActive_ShouldCancelSessionAndRecordReasonAndUser()
    {
        // Arrange (BR-SO-018: session cancellation)
        var session = CreateSampleSession("SES-ACTIVE", status: SessionStatusEnum.Counting);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-ACTIVE")))
            .Returns(MayBe.From(session));

        var handler = new CancelSessionHandler(_sessionRepoMock.Object);
        var cancelTime = new DateTime(2026, 11, 20, 15, 30, 0);
        var cmd = new CancelSessionCommand("SES-ACTIVE", "Emergency surgery inventory recall", "MGR01", cancelTime);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be("SES-ACTIVE");
        result.Status.Should().Be(SessionStatusEnum.Cancelled);
        result.CancelReason.Should().Be("Emergency surgery inventory recall");
        result.CancelledBy.Should().Be("MGR01");
        result.CancelledAt.Should().Be(cancelTime);

        session.Status.Should().Be(SessionStatusEnum.Cancelled);
        session.CancelReason.Should().Be("Emergency surgery inventory recall");
        session.CancelledBy.Should().Be("MGR01");
        session.CancelledAt.Should().Be(cancelTime);
        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }

    [Fact]
    public async Task CancelSession_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.LoadEntity(It.IsAny<IStockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var handler = new CancelSessionHandler(_sessionRepoMock.Object);
        var cmd = new CancelSessionCommand("SES-UNKNOWN", "Mistake", "MGR01");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SES-UNKNOWN*");
    }

    [Fact]
    public async Task CancelSession_WhenSessionIsApproved_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession("SES-APP", status: SessionStatusEnum.Approved);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-APP")))
            .Returns(MayBe.From(session));

        var handler = new CancelSessionHandler(_sessionRepoMock.Object);
        var cmd = new CancelSessionCommand("SES-APP", "Cannot cancel approved", "MGR01");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel an approved session*");
    }

    [Fact]
    public async Task CancelSession_WhenSessionIsAlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession("SES-CAN", status: SessionStatusEnum.Cancelled);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-CAN")))
            .Returns(MayBe.From(session));

        var handler = new CancelSessionHandler(_sessionRepoMock.Object);
        var cmd = new CancelSessionCommand("SES-CAN", "Already cancelled", "MGR01");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Session is already cancelled*");
    }

    [Fact]
    public async Task CancelSession_WhenSessionIsRejected_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession("SES-REJ", status: SessionStatusEnum.Rejected);
        _sessionRepoMock.Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-REJ")))
            .Returns(MayBe.From(session));

        var handler = new CancelSessionHandler(_sessionRepoMock.Object);
        var cmd = new CancelSessionCommand("SES-REJ", "Cannot cancel rejected", "MGR01");

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel a rejected session*");
    }
    #endregion
}
