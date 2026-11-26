using Bilreg.Domain.InventoryContext.OpnameFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class StockOpnameSessionModelTests
{
    private const string DefaultSessionId = "OPN-202611-0001";
    private const string DefaultLayananId = "FAR01";
    private const string DefaultUser = "USER-SPV";
    private readonly DateTime _baseDate = new(2026, 11, 20, 8, 0, 0);

    private static StockOpnameSessionModel CreateSut(
        string sessionId = DefaultSessionId,
        string layananId = DefaultLayananId,
        SessionScopeEnum scopeMode = SessionScopeEnum.Full,
        string createdBy = DefaultUser)
    {
        return StockOpnameSessionModel.Create(
            sessionId,
            layananId,
            scopeMode,
            new DateTime(2026, 11, 20, 7, 0, 0),
            createdBy);
    }

    private static StockOpnameVarianceItemModel CreateVarianceItem(
        string varianceId = "VAR-001",
        string brgId = "BRG-001",
        decimal countQty = 10m,
        decimal systemQty = 12m,
        decimal hpp = 5000m,
        bool markVerified = false)
    {
        var item = StockOpnameVarianceItemModel.Create(
            varianceId,
            DefaultSessionId,
            brgId,
            "BATCH-01",
            "2027-12-31",
            "TAB",
            countQty,
            systemQty,
            hpp);

        if (markVerified)
        {
            item.MarkVerifiedWithReason(
                DiscrepancyReasonEnum.ADMIN_ERROR,
                "Penyesuaian kartu stok",
                DefaultUser,
                new DateTime(2026, 11, 20, 10, 0, 0));
        }

        return item;
    }

    #region ENUM & VALUE OBJECT TESTS
    [Fact]
    public void SessionStatusEnum_ShouldMatchSpecifiedValues()
    {
        ((int)SessionStatusEnum.Draft).Should().Be(0);
        ((int)SessionStatusEnum.Counting).Should().Be(1);
        ((int)SessionStatusEnum.WaitingVerification).Should().Be(2);
        ((int)SessionStatusEnum.Verifying).Should().Be(3);
        ((int)SessionStatusEnum.WaitingApproval).Should().Be(4);
        ((int)SessionStatusEnum.Approved).Should().Be(5);
        ((int)SessionStatusEnum.Rejected).Should().Be(6);
        ((int)SessionStatusEnum.Cancelled).Should().Be(7);
    }

    [Fact]
    public void DiscrepancyReasonEnum_ShouldMatchSpecifiedValues()
    {
        ((int)DiscrepancyReasonEnum.DAMAGED_EXPIRED).Should().Be(1);
        ((int)DiscrepancyReasonEnum.MISPLACED).Should().Be(2);
        ((int)DiscrepancyReasonEnum.ADMIN_ERROR).Should().Be(3);
        ((int)DiscrepancyReasonEnum.UNKNOWN_THEFT).Should().Be(4);
    }

    [Fact]
    public void SessionScopeEnum_ShouldMatchSpecifiedValues()
    {
        ((int)SessionScopeEnum.Full).Should().Be(1);
        ((int)SessionScopeEnum.Partial).Should().Be(2);
    }

    [Fact]
    public void RejectionDispositionEnum_ShouldMatchSpecifiedValues()
    {
        ((int)RejectionDispositionEnum.Rework).Should().Be(1);
        ((int)RejectionDispositionEnum.Terminal).Should().Be(2);
    }

    [Fact]
    public void ItemTupleKey_ShouldProvideValueEquality()
    {
        var key1 = new ItemTupleKey("BRG-01", "BATCH-01", "2027-01-01");
        var key2 = new ItemTupleKey("BRG-01", "BATCH-01", "2027-01-01");
        var key3 = new ItemTupleKey("BRG-01", "BATCH-02", "2027-01-01");

        key1.Should().Be(key2);
        key1.Should().NotBe(key3);
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }
    #endregion

    #region CREATION TESTS
    [Fact]
    public void Create_ValidParameters_ShouldInitializeDraftState()
    {
        var sut = CreateSut();

        sut.SessionId.Should().Be(DefaultSessionId);
        sut.LayananId.Should().Be(DefaultLayananId);
        sut.ScopeMode.Should().Be(SessionScopeEnum.Full);
        sut.Status.Should().Be(SessionStatusEnum.Draft);
        sut.CreatedBy.Should().Be(DefaultUser);
        sut.ScopeItems.Should().BeEmpty();
        sut.CountedItems.Should().BeEmpty();
        sut.VarianceItems.Should().BeEmpty();
        sut.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "FAR01", "USER")]
    [InlineData("   ", "FAR01", "USER")]
    [InlineData("OPN-1", "", "USER")]
    [InlineData("OPN-1", "FAR01", "")]
    [InlineData("OPN-1", "FAR012", "USER")]
    [InlineData("SO123456789012345678901", "FAR01", "USER")]
    public void Create_InvalidParameters_ShouldThrowArgumentException(string sessionId, string layananId, string createdBy)
    {
        var act = () => StockOpnameSessionModel.Create(
            sessionId,
            layananId,
            SessionScopeEnum.Full,
            DateTime.Now,
            createdBy);

        act.Should().Throw<ArgumentException>();
    }
    #endregion

    #region START COUNTING TESTS
    [Fact]
    public void StartCounting_FromDraft_ShouldTransitionToCounting()
    {
        var sut = CreateSut();

        sut.StartCounting();

        sut.Status.Should().Be(SessionStatusEnum.Counting);
    }

    [Fact]
    public void StartCounting_NotFromDraft_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSut();
        sut.StartCounting(); // Now in Counting

        var act = () => sut.StartCounting();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot start counting from status Counting*");
    }
    #endregion

    #region SUBMIT COUNTING & ZERO-VARIANCE BYPASS TESTS
    [Fact]
    public void SubmitCounting_WithVariance_ShouldTransitionToWaitingVerification()
    {
        var sut = CreateSut();
        sut.StartCounting();

        sut.SubmitCounting(hasVariance: true, submittedBy: "STAFF-01", submittedAt: _baseDate);

        sut.Status.Should().Be(SessionStatusEnum.WaitingVerification);
        sut.CountingSubmittedBy.Should().Be("STAFF-01");
        sut.CountingSubmittedAt.Should().Be(_baseDate);
    }

    [Fact]
    public void SubmitCounting_WithoutVariance_ShouldTransitionToWaitingApproval_ZeroVarianceBypass()
    {
        var sut = CreateSut();
        sut.StartCounting();

        // BR-SO-010: Zero-Variance Verification Bypass
        sut.SubmitCounting(hasVariance: false, submittedBy: "STAFF-01", submittedAt: _baseDate);

        sut.Status.Should().Be(SessionStatusEnum.WaitingApproval);
        sut.CountingSubmittedBy.Should().Be("STAFF-01");
        sut.CountingSubmittedAt.Should().Be(_baseDate);
    }

    [Fact]
    public void SubmitCounting_NotFromCounting_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSut(); // In Draft

        var act = () => sut.SubmitCounting(hasVariance: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot submit counting from status Draft*");
    }
    #endregion

    #region VERIFICATION TESTS
    [Fact]
    public void StartVerification_FromWaitingVerification_ShouldTransitionToVerifying()
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: true);

        sut.StartVerification();

        sut.Status.Should().Be(SessionStatusEnum.Verifying);
    }

    [Fact]
    public void StartVerification_NotFromWaitingVerification_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSut(); // In Draft

        var act = () => sut.StartVerification();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot start verification from status Draft*");
    }

    [Fact]
    public void SubmitVerification_AllItemsVerified_ShouldTransitionToWaitingApproval()
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: true);
        sut.StartVerification();

        var item1 = CreateVarianceItem("V1", "B1", 10, 12, 5000, markVerified: true);
        var item2 = CreateVarianceItem("V2", "B2", 15, 10, 3000, markVerified: true);
        sut.AddVarianceItem(item1);
        sut.AddVarianceItem(item2);

        sut.SubmitVerification(verifiedBy: "SPV-01", verifiedAt: _baseDate);

        sut.Status.Should().Be(SessionStatusEnum.WaitingApproval);
        sut.VerifiedBy.Should().Be("SPV-01");
        sut.VerifiedAt.Should().Be(_baseDate);
    }

    [Fact]
    public void SubmitVerification_WithUnverifiedItems_ShouldThrowInvalidOperationException_BR_SO_015()
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: true);
        sut.StartVerification();

        var verifiedItem = CreateVarianceItem("V1", "B1", 10, 12, 5000, markVerified: true);
        var unverifiedItem = CreateVarianceItem("V2", "B2", 15, 10, 3000, markVerified: false);
        sut.AddVarianceItem(verifiedItem);
        sut.AddVarianceItem(unverifiedItem);

        var act = () => sut.SubmitVerification(verifiedBy: "SPV-01");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*All variance items must be verified before submitting verification*");
    }

    [Fact]
    public void SubmitVerification_NotFromVerifying_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: true); // In WaitingVerification

        var act = () => sut.SubmitVerification();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot submit verification from status WaitingVerification*");
    }
    #endregion

    #region APPROVAL TESTS & DOMAIN EVENT
    [Fact]
    public void Approve_FromWaitingApproval_ShouldTransitionToApprovedAndRaiseEvent()
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: true);
        sut.StartVerification();

        var itemSurplus = CreateVarianceItem("V1", "BRG-PLUS", 15, 10, 4000, markVerified: true); // FinalVariance +5
        var itemDeficit = CreateVarianceItem("V2", "BRG-MIN", 8, 10, 6000, markVerified: true);  // FinalVariance -2
        var itemZero = CreateVarianceItem("V3", "BRG-ZERO", 10, 10, 2000, markVerified: true);   // FinalVariance 0
        sut.AddVarianceItem(itemSurplus);
        sut.AddVarianceItem(itemDeficit);
        sut.AddVarianceItem(itemZero);

        sut.SubmitVerification("SPV-01");

        var evt = sut.Approve("MGR-01", _baseDate);

        sut.Status.Should().Be(SessionStatusEnum.Approved);
        sut.ApprovedBy.Should().Be("MGR-01");
        sut.ApprovedAt.Should().Be(_baseDate);

        // Domain event verification
        evt.Should().NotBeNull();
        evt.SessionId.Should().Be(DefaultSessionId);
        evt.LayananId.Should().Be(DefaultLayananId);
        evt.ApprovedBy.Should().Be("MGR-01");
        evt.ApprovedAt.Should().Be(_baseDate);
        evt.Items.Should().HaveCount(2); // Only items with non-zero variance

        var plusItem = evt.Items.First(x => x.BrgId == "BRG-PLUS");
        plusItem.AdjustmentQty.Should().Be(5);
        plusItem.Hpp.Should().Be(4000);

        var minItem = evt.Items.First(x => x.BrgId == "BRG-MIN");
        minItem.AdjustmentQty.Should().Be(-2);
        minItem.Hpp.Should().Be(6000);

        sut.DomainEvents.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public void Approve_NotFromWaitingApproval_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSut(); // In Draft

        var act = () => sut.Approve("MGR-01");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot approve session from status Draft*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Approve_InvalidApprover_ShouldThrowArgumentException(string approver)
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: false); // In WaitingApproval

        var act = () => sut.Approve(approver);

        act.Should().Throw<ArgumentException>();
    }
    #endregion

    #region REJECTION TESTS
    [Fact]
    public void Reject_ReworkDisposition_ShouldTransitionBackToVerifying_PreservingRecounts()
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: true);
        sut.StartVerification();

        var item = CreateVarianceItem("V1", "B1", 10, 12, 5000, markVerified: true);
        sut.AddVarianceItem(item);
        sut.SubmitVerification("SPV-01");

        // Act - Manager rejects with Rework
        sut.Reject(RejectionDispositionEnum.Rework, "Hitung ulang SKU B1 kembali", "MGR-01");

        sut.Status.Should().Be(SessionStatusEnum.Verifying);
        sut.RevisionNotes.Should().Be("Hitung ulang SKU B1 kembali");
        sut.VarianceItems.Should().HaveCount(1);
        sut.VarianceItems[0].RecountResult.Should().NotBeNull(); // Preserved
    }

    [Fact]
    public void Reject_TerminalDisposition_ShouldTransitionToRejected()
    {
        var sut = CreateSut();
        sut.StartCounting();
        sut.SubmitCounting(hasVariance: true);
        sut.StartVerification();

        var item = CreateVarianceItem("V1", "B1", 10, 12, 5000, markVerified: true);
        sut.AddVarianceItem(item);
        sut.SubmitVerification("SPV-01");

        // Act - Manager rejects permanently
        sut.Reject(RejectionDispositionEnum.Terminal, "Proses opname dibatalkan permanen", "MGR-01", _baseDate);

        sut.Status.Should().Be(SessionStatusEnum.Rejected);
        sut.RejectedBy.Should().Be("MGR-01");
        sut.RejectedAt.Should().Be(_baseDate);
        sut.RevisionNotes.Should().Be("Proses opname dibatalkan permanen");
    }

    [Fact]
    public void Reject_NotFromWaitingApproval_ShouldThrowInvalidOperationException()
    {
        var sut = CreateSut(); // In Draft

        var act = () => sut.Reject(RejectionDispositionEnum.Rework, "Notes", "MGR-01");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot reject session from status Draft*");
    }
    #endregion

    #region CANCELLATION TESTS
    [Theory]
    [InlineData(SessionStatusEnum.Draft)]
    [InlineData(SessionStatusEnum.Counting)]
    [InlineData(SessionStatusEnum.WaitingVerification)]
    [InlineData(SessionStatusEnum.Verifying)]
    [InlineData(SessionStatusEnum.WaitingApproval)]
    public void Cancel_FromActiveStates_ShouldTransitionToCancelled(SessionStatusEnum state)
    {
        var sut = StockOpnameSessionModel.Load(
            DefaultSessionId,
            DefaultLayananId,
            SessionScopeEnum.Full,
            state,
            DateTime.Now,
            "CREATOR",
            DateTime.Now);

        sut.Cancel("Gudang darurat banjir", "SPV-01", _baseDate);

        sut.Status.Should().Be(SessionStatusEnum.Cancelled);
        sut.CancelReason.Should().Be("Gudang darurat banjir");
        sut.CancelledBy.Should().Be("SPV-01");
        sut.CancelledAt.Should().Be(_baseDate);
    }

    [Fact]
    public void Cancel_FromApproved_ShouldThrowInvalidOperationException()
    {
        var sut = StockOpnameSessionModel.Load(
            DefaultSessionId,
            DefaultLayananId,
            SessionScopeEnum.Full,
            SessionStatusEnum.Approved,
            DateTime.Now,
            "CREATOR",
            DateTime.Now);

        var act = () => sut.Cancel("Reason", "USER");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel an approved session*");
    }

    [Fact]
    public void Cancel_FromCancelled_ShouldThrowInvalidOperationException()
    {
        var sut = StockOpnameSessionModel.Load(
            DefaultSessionId,
            DefaultLayananId,
            SessionScopeEnum.Full,
            SessionStatusEnum.Cancelled,
            DateTime.Now,
            "CREATOR",
            DateTime.Now);

        var act = () => sut.Cancel("Reason", "USER");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Session is already cancelled*");
    }

    [Fact]
    public void Cancel_FromRejected_ShouldThrowInvalidOperationException()
    {
        var sut = StockOpnameSessionModel.Load(
            DefaultSessionId,
            DefaultLayananId,
            SessionScopeEnum.Full,
            SessionStatusEnum.Rejected,
            DateTime.Now,
            "CREATOR",
            DateTime.Now);

        var act = () => sut.Cancel("Reason", "USER");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot cancel a rejected session*");
    }
    #endregion

    #region FINANCIAL VALUATIONS TESTS
    [Fact]
    public void Valuations_WithSurplusAndDeficit_ShouldCalculateCorrectly()
    {
        var sut = CreateSut();

        // Surplus item: Count 15, System 10, Variance +5, Hpp 10,000 -> Valuation +50,000
        var item1 = CreateVarianceItem("V1", "B1", countQty: 15, systemQty: 10, hpp: 10_000, markVerified: true);

        // Deficit item: Count 7, System 10, Variance -3, Hpp 20,000 -> Valuation -60,000
        var item2 = CreateVarianceItem("V2", "B2", countQty: 7, systemQty: 10, hpp: 20_000, markVerified: true);

        // Zero variance item: Count 10, System 10, Variance 0, Hpp 5,000 -> Valuation 0
        var item3 = CreateVarianceItem("V3", "B3", countQty: 10, systemQty: 10, hpp: 5_000, markVerified: true);

        sut.AddVarianceItem(item1);
        sut.AddVarianceItem(item2);
        sut.AddVarianceItem(item3);

        sut.SurplusValuation.Should().Be(50_000m);
        sut.DeficitValuation.Should().Be(60_000m);
        sut.NetValuation.Should().Be(-10_000m);
    }

    [Fact]
    public void Valuations_EmptyVariance_ShouldBeZero()
    {
        var sut = CreateSut();

        sut.SurplusValuation.Should().Be(0m);
        sut.DeficitValuation.Should().Be(0m);
        sut.NetValuation.Should().Be(0m);
    }
    #endregion

    #region COLLECTION MANAGEMENT TESTS
    [Fact]
    public void CollectionMethods_ShouldAddAndClearCorrectly()
    {
        var sut = CreateSut();

        var scope = StockOpnameScopeItemModel.Create("SC-01", sut.SessionId, "BRG-01", "Amoxicillin");
        sut.AddScopeItem(scope);
        sut.ScopeItems.Should().ContainSingle().Which.BrgId.Should().Be("BRG-01");

        var count = StockOpnameCountedItemModel.Create(
            "CNT-01", sut.SessionId, "BRG-01", "BATCH-01", "2027-01-01", "TAB", 50, DateTime.Now);
        sut.AddCountedItem(count);
        sut.CountedItems.Should().ContainSingle().Which.TotalCountQty.Should().Be(50);

        var variance = CreateVarianceItem("VAR-01", "BRG-01", 50, 45, 1000);
        sut.AddVarianceItem(variance);
        sut.VarianceItems.Should().ContainSingle();

        sut.SetVarianceItems(new[]
        {
            CreateVarianceItem("VAR-02", "BRG-02", 20, 25, 2000),
            CreateVarianceItem("VAR-03", "BRG-03", 30, 30, 3000)
        });
        sut.VarianceItems.Should().HaveCount(2);

        sut.ClearEvents();
        sut.DomainEvents.Should().BeEmpty();
    }
    #endregion
}
