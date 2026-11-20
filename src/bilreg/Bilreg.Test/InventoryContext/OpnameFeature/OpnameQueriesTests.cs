using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Queries;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class OpnameQueriesTests
{
    private readonly Mock<IStockOpnameSessionRepo> _sessionRepoMock = new();

    #region TEST HELPERS
    private static StockOpnameSessionModel CreateSampleSession(
        string sessionId = "SES-001",
        string layananId = "FAR01",
        SessionScopeEnum scopeMode = SessionScopeEnum.Partial,
        SessionStatusEnum status = SessionStatusEnum.Verifying,
        IEnumerable<StockOpnameScopeItemModel>? scopeItems = null,
        IEnumerable<StockOpnameCountedItemModel>? countedItems = null,
        IEnumerable<StockOpnameVarianceItemModel>? varianceItems = null)
    {
        return new StockOpnameSessionModel(
            sessionId: sessionId,
            layananId: layananId,
            scopeMode: scopeMode,
            status: status,
            cutoffDate: new DateTime(2026, 11, 20, 8, 0, 0),
            createdBy: "USR01",
            createdAt: new DateTime(2026, 11, 20, 8, 0, 0),
            countingSubmittedBy: "USR02",
            countingSubmittedAt: new DateTime(2026, 11, 20, 10, 0, 0),
            verifiedBy: "SPV01",
            verifiedAt: new DateTime(2026, 11, 20, 11, 0, 0),
            approvedBy: "MGR01",
            approvedAt: new DateTime(2026, 11, 20, 12, 0, 0),
            rejectedBy: "",
            rejectedAt: null,
            cancelledBy: "",
            cancelledAt: null,
            cancelReason: "",
            revisionNotes: "",
            scopeItems: scopeItems,
            countedItems: countedItems,
            varianceItems: varianceItems
        );
    }
    #endregion

    #region GET SESSION LIST TESTS
    [Fact]
    public async Task GetSessionList_ShouldReturnMappedDtos_WhenSessionsExist()
    {
        // Arrange
        var sessions = new List<StockOpnameSessionModel>
        {
            CreateSampleSession("SES-001", "FAR01", SessionScopeEnum.Partial, SessionStatusEnum.Counting),
            CreateSampleSession("SES-002", "FAR02", SessionScopeEnum.Full, SessionStatusEnum.Approved)
        };

        _sessionRepoMock
            .Setup(r => r.ListData("FAR01", SessionStatusEnum.Counting, null, null))
            .Returns(sessions.Take(1));

        var sut = new GetSessionListHandler(_sessionRepoMock.Object);
        var query = new GetSessionListQuery("FAR01", SessionStatusEnum.Counting);

        // Act
        var result = (await sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].SessionId.Should().Be("SES-001");
        result[0].LayananId.Should().Be("FAR01");
        result[0].ScopeMode.Should().Be(SessionScopeEnum.Partial);
        result[0].Status.Should().Be(SessionStatusEnum.Counting);
        result[0].CreatedBy.Should().Be("USR01");
    }

    [Fact]
    public async Task GetSessionList_ShouldReturnEmpty_WhenNoSessionsFound()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.ListData(It.IsAny<string?>(), It.IsAny<SessionStatusEnum?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns(Enumerable.Empty<StockOpnameSessionModel>());

        var sut = new GetSessionListHandler(_sessionRepoMock.Object);
        var query = new GetSessionListQuery();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
    #endregion

    #region GET SESSION DETAIL TESTS
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSessionDetail_WhenSessionIdIsNullOrEmpty_ShouldReturnNull(string? sessionId)
    {
        // Arrange
        var sut = new GetSessionDetailHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetSessionDetailQuery(sessionId!), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionDetail_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-999")))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new GetSessionDetailHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetSessionDetailQuery("SES-999"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionDetail_PartialSession_ShouldCalculateProgressCorrectly()
    {
        // Arrange
        var scopes = new List<StockOpnameScopeItemModel>
        {
            new("SCP-1", "SES-001", "BRG01", "Amoxicillin"),
            new("SCP-2", "SES-001", "BRG02", "Paracetamol"),
            new("SCP-3", "SES-001", "BRG03", "Ibuprofen"),
            new("SCP-4", "SES-001", "BRG04", "Cetirizine")
        };

        var counts = new List<StockOpnameCountedItemModel>
        {
            new("CNT-1", "SES-001", "BRG01", "B01", "2027-01-01", "TAB", 10, DateTime.UtcNow),
            new("CNT-2", "SES-001", "BRG02", "B02", "2027-01-01", "TAB", 20, DateTime.UtcNow)
        };

        var variances = new List<StockOpnameVarianceItemModel>
        {
            new("VAR-1", "SES-001", "BRG02", "B02", "2027-01-01", "TAB", 20, 25, -5, 1000, -5000, 0)
        };

        var session = CreateSampleSession(
            "SES-001", "FAR01", SessionScopeEnum.Partial, SessionStatusEnum.Counting,
            scopeItems: scopes, countedItems: counts, varianceItems: variances);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-001")))
            .Returns(MayBe.From(session));

        var sut = new GetSessionDetailHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetSessionDetailQuery("SES-001"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be("SES-001");
        result.ScopeMode.Should().Be(SessionScopeEnum.Partial);
        result.Status.Should().Be(SessionStatusEnum.Counting);
        result.ScopeItems.Should().HaveCount(4);

        // ScanProgress verification:
        // TotalCounted = 2, TotalVariance = 1, TotalMatched = 2 - 1 = 1, Progress = 2 / 4 = 50.00%
        result.ScanProgress.TotalCounted.Should().Be(2);
        result.ScanProgress.TotalVariance.Should().Be(1);
        result.ScanProgress.TotalMatched.Should().Be(1);
        result.ScanProgress.ProgressPercentage.Should().Be(50m);
    }

    [Fact]
    public async Task GetSessionDetail_FullSessionInWaitingVerification_ShouldReport100PercentProgress()
    {
        // Arrange
        var counts = new List<StockOpnameCountedItemModel>
        {
            new("CNT-1", "SES-002", "BRG01", "B01", "2027-01-01", "TAB", 10, DateTime.UtcNow)
        };

        var session = CreateSampleSession(
            "SES-002", "FAR01", SessionScopeEnum.Full, SessionStatusEnum.WaitingVerification,
            countedItems: counts);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-002")))
            .Returns(MayBe.From(session));

        var sut = new GetSessionDetailHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetSessionDetailQuery("SES-002"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ScanProgress.ProgressPercentage.Should().Be(100m);
        result.ScanProgress.TotalCounted.Should().Be(1);
    }
    #endregion

    #region GET VARIANCE REVIEW TESTS
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetVarianceReview_WhenSessionIdIsNullOrEmpty_ShouldReturnEmpty(string? sessionId)
    {
        // Arrange
        var sut = new GetVarianceReviewHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetVarianceReviewQuery(sessionId!), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVarianceReview_WhenNotFound_ShouldReturnEmpty()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-999")))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new GetVarianceReviewHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetVarianceReviewQuery("SES-999"), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetVarianceReview_ShouldMapBothUnverifiedAndRecountedVariances()
    {
        // Arrange
        var recountTime = new DateTime(2026, 11, 20, 11, 30, 0);
        var recount = new StockOpnameRecountResultModel(
            "REC-001", "VAR-001", 98, 100, -2, -3000,
            DiscrepancyReasonEnum.DAMAGED_EXPIRED, "Ditemukan pecah di rak", "SPV01", recountTime);

        var var1 = new StockOpnameVarianceItemModel(
            "VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31", "TAB",
            countQty: 95, systemQty: 100, varianceQty: -5, hpp: 1500, varianceValuation: -7500,
            state: 1, recountResult: recount);

        var var2 = new StockOpnameVarianceItemModel(
            "VAR-002", "SES-001", "BRG02", "BATCH02", "2028-06-30", "BTL",
            countQty: 15, systemQty: 10, varianceQty: 5, hpp: 10000, varianceValuation: 50000,
            state: 0, recountResult: null);

        var session = CreateSampleSession("SES-001", varianceItems: [var1, var2]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-001")))
            .Returns(MayBe.From(session));

        var sut = new GetVarianceReviewHandler(_sessionRepoMock.Object);

        // Act
        var result = (await sut.Handle(new GetVarianceReviewQuery("SES-001"), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);

        // Verified item with recount
        var row1 = result.First(r => r.VarianceItemId == "VAR-001");
        row1.PhysicalQty.Should().Be(95);
        row1.InitialSystemQty.Should().Be(100);
        row1.InitialVarianceQty.Should().Be(-5);
        row1.RecountQty.Should().Be(98);
        row1.FinalVarianceQty.Should().Be(-2);
        row1.Hpp.Should().Be(1500);
        row1.FinancialValue.Should().Be(-3000);
        row1.DiscrepancyReason.Should().Be(DiscrepancyReasonEnum.DAMAGED_EXPIRED);
        row1.DiscrepancyReasonName.Should().Be(nameof(DiscrepancyReasonEnum.DAMAGED_EXPIRED));
        row1.Notes.Should().Be("Ditemukan pecah di rak");
        row1.RecountedBy.Should().Be("SPV01");
        row1.RecountTime.Should().Be(recountTime);
        row1.State.Should().Be(1);
        row1.IsVerified.Should().BeTrue();

        // Unverified item without recount
        var row2 = result.First(r => r.VarianceItemId == "VAR-002");
        row2.PhysicalQty.Should().Be(15);
        row2.InitialSystemQty.Should().Be(10);
        row2.InitialVarianceQty.Should().Be(5);
        row2.RecountQty.Should().BeNull();
        row2.FinalVarianceQty.Should().Be(5);
        row2.Hpp.Should().Be(10000);
        row2.FinancialValue.Should().Be(50000);
        row2.DiscrepancyReason.Should().BeNull();
        row2.DiscrepancyReasonName.Should().BeNull();
        row2.Notes.Should().BeEmpty();
        row2.RecountedBy.Should().BeEmpty();
        row2.RecountTime.Should().BeNull();
        row2.State.Should().Be(0);
        row2.IsVerified.Should().BeFalse();
    }
    #endregion

    #region GET SESSION RESULT REPORT TESTS
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSessionResultReport_WhenSessionIdIsNullOrEmpty_ShouldReturnNull(string? sessionId)
    {
        // Arrange
        var sut = new GetSessionResultReportHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetSessionResultReportQuery(sessionId!), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionResultReport_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-999")))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new GetSessionResultReportHandler(_sessionRepoMock.Object);

        // Act
        var result = await sut.Handle(new GetSessionResultReportQuery("SES-999"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSessionResultReport_ShouldComputeFinancialValuationsAndVerificationStatus()
    {
        // Arrange
        // Item 1: Surplus (+5 * 10.000 = +50.000), Verified
        var var1 = new StockOpnameVarianceItemModel(
            "VAR-001", "SES-001", "BRG01", "B01", "2027-12-31", "TAB",
            countQty: 15, systemQty: 10, varianceQty: 5, hpp: 10000, varianceValuation: 50000,
            state: 1);
        var1.SetRecountResult(new StockOpnameRecountResultModel(
            "REC-001", "VAR-001", 15, 10, 5, 50000,
            DiscrepancyReasonEnum.ADMIN_ERROR, "Salah entri sebelumnya", "SPV01", DateTime.UtcNow));

        // Item 2: Defisit (-2 * 20.000 = -40.000), Verified
        var var2 = new StockOpnameVarianceItemModel(
            "VAR-002", "SES-001", "BRG02", "B02", "2027-12-31", "BTL",
            countQty: 8, systemQty: 10, varianceQty: -2, hpp: 20000, varianceValuation: -40000,
            state: 1);
        var2.SetRecountResult(new StockOpnameRecountResultModel(
            "REC-002", "VAR-002", 8, 10, -2, -40000,
            DiscrepancyReasonEnum.DAMAGED_EXPIRED, "Expired dibuang", "SPV01", DateTime.UtcNow));

        var session = CreateSampleSession(
            "SES-001", "FAR01", SessionScopeEnum.Full, SessionStatusEnum.WaitingApproval,
            varianceItems: [var1, var2]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-001")))
            .Returns(MayBe.From(session));

        var sut = new GetSessionResultReportHandler(_sessionRepoMock.Object);

        // Act
        var report = await sut.Handle(new GetSessionResultReportQuery("SES-001"), CancellationToken.None);

        // Assert
        report.Should().NotBeNull();
        report!.SessionId.Should().Be("SES-001");
        report.Status.Should().Be(SessionStatusEnum.WaitingApproval);

        // Financial summary:
        // Surplus: 50.000, Deficit: 40.000, Net: 50.000 - 40.000 = 10.000
        report.TotalSurplusValuation.Should().Be(50000m);
        report.TotalDeficitValuation.Should().Be(40000m);
        report.NetDiscrepancyValuation.Should().Be(10000m);

        // Item counts:
        report.TotalVarianceItems.Should().Be(2);
        report.TotalVerifiedItems.Should().Be(2);
        report.TotalUnverifiedItems.Should().Be(0);
        report.AllVerified.Should().BeTrue();

        report.Items.Should().HaveCount(2);
        report.Items[0].VerificationStatus.Should().Be("Verified");
        report.Items[0].IsVerified.Should().BeTrue();
        report.Items[1].VerificationStatus.Should().Be("Verified");
        report.Items[1].IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetSessionResultReport_WhenItemsUnverified_AllVerifiedShouldBeFalse()
    {
        // Arrange
        var var1 = new StockOpnameVarianceItemModel(
            "VAR-001", "SES-001", "BRG01", "B01", "2027-12-31", "TAB",
            countQty: 10, systemQty: 5, varianceQty: 5, hpp: 1000, varianceValuation: 5000,
            state: 0);

        var session = CreateSampleSession(
            "SES-001", "FAR01", SessionScopeEnum.Full, SessionStatusEnum.Verifying,
            varianceItems: [var1]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.Is<IStockOpnameSessionKey>(k => k.SessionId == "SES-001")))
            .Returns(MayBe.From(session));

        var sut = new GetSessionResultReportHandler(_sessionRepoMock.Object);

        // Act
        var report = await sut.Handle(new GetSessionResultReportQuery("SES-001"), CancellationToken.None);

        // Assert
        report.Should().NotBeNull();
        report!.TotalVarianceItems.Should().Be(1);
        report.TotalVerifiedItems.Should().Be(0);
        report.TotalUnverifiedItems.Should().Be(1);
        report.AllVerified.Should().BeFalse();
        report.Items[0].VerificationStatus.Should().Be("Unverified");
        report.Items[0].IsVerified.Should().BeFalse();
    }
    #endregion
}
