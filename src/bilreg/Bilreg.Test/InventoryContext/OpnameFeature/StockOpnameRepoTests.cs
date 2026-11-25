using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dals;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Dtos;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Repos;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class StockOpnameRepoTests
{
    private readonly Mock<IStockOpnameSessionDal> _sessionDalMock = new();
    private readonly Mock<IStockOpnameScopeItemDal> _scopeItemDalMock = new();
    private readonly Mock<IStockOpnameCountedItemDal> _countedItemDalMock = new();
    private readonly Mock<IStockOpnameVarianceItemDal> _varianceItemDalMock = new();
    private readonly Mock<IStockOpnameRecountResultDal> _recountResultDalMock = new();
    private readonly Mock<IStockOpnameAdjustmentDal> _adjustmentDalMock = new();

    private StockOpnameSessionRepo CreateSessionRepo() =>
        new(
            _sessionDalMock.Object,
            _scopeItemDalMock.Object,
            _countedItemDalMock.Object,
            _varianceItemDalMock.Object,
            _recountResultDalMock.Object);

    private StockOpnameAdjustmentRepo CreateAdjustmentRepo() =>
        new(_adjustmentDalMock.Object);

    [Fact]
    public void HasActiveSessionAtLocation_ShouldReturnExpectedResult()
    {
        // Arrange
        _sessionDalMock.Setup(d => d.HasActiveSessionAtLocation("FAR01")).Returns(true);
        var sut = CreateSessionRepo();

        // Act
        var result = sut.HasActiveSessionAtLocation("FAR01");

        // Assert
        result.Should().BeTrue();
        _sessionDalMock.Verify(d => d.HasActiveSessionAtLocation("FAR01"), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenSessionNotFound_ShouldReturnNone()
    {
        // Arrange
        _sessionDalMock.Setup(d => d.GetData("SES-001")).Returns((StockOpnameSessionDto?)null);
        var sut = CreateSessionRepo();

        // Act
        var result = sut.LoadEntity(new StockOpnameSessionKey("SES-001"));

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntity_WhenSessionExists_ShouldReturnHydratedAggregate()
    {
        // Arrange
        var sessionDto = new StockOpnameSessionDto(
            SessionId: "SES-001",
            LayananId: "FAR01",
            ScopeType: "Partial",
            Status: "Verifying",
            CutoffTime: new DateTime(2026, 11, 20, 8, 0, 0),
            StartedAt: null,
            CountingFinishedAt: new DateTime(2026, 11, 20, 10, 0, 0),
            VerifiedAt: new DateTime(2026, 11, 20, 11, 0, 0),
            ApprovedAt: null,
            ApprovedBy: "",
            RejectedAt: null,
            RejectedBy: "",
            RejectionNotes: "",
            RejectionDisposition: null,
            CancelledAt: null,
            CancelledBy: "",
            CancellationReason: "",
            CreatedBy: "USR01",
            CreatedAt: new DateTime(2026, 11, 20, 8, 0, 0)
        );

        var scopeDto = new StockOpnameScopeItemDto("SES-001", "BRG01", "Paracetamol");
        var countedDto = new StockOpnameCountedItemDto(
            "SES-001", "BRG01", "BATCH01", "2027-12-31", "TAB", 95, new DateTime(2026, 11, 20, 9, 30, 0));
        var varianceDto = new StockOpnameVarianceItemDto(
            "SES-001", "BRG01", "BATCH01", "2027-12-31", "TAB", 95, 100, -5, "Verified", null);
        var recountDto = new StockOpnameRecountResultDto(
            "REC-001", "SES-001", "BRG01", "BATCH01", "2027-12-31", 98, new DateTime(2026, 11, 20, 11, 0, 0), 100, -2, DiscrepancyReasonEnum.DAMAGED_EXPIRED.ToString(), "Rusak", "SPV01");

        _sessionDalMock.Setup(d => d.GetData("SES-001")).Returns(sessionDto);
        _scopeItemDalMock.Setup(d => d.ListBySession("SES-001")).Returns([scopeDto]);
        _countedItemDalMock.Setup(d => d.ListBySession("SES-001")).Returns([countedDto]);
        _varianceItemDalMock.Setup(d => d.ListBySession("SES-001")).Returns([varianceDto]);
        _recountResultDalMock.Setup(d => d.ListBySession("SES-001")).Returns([recountDto]);

        var sut = CreateSessionRepo();

        // Act
        var result = sut.LoadEntity(new StockOpnameSessionKey("SES-001"));

        // Assert
        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.SessionId.Should().Be("SES-001");
        model.LayananId.Should().Be("FAR01");
        model.ScopeMode.Should().Be(SessionScopeEnum.Partial);
        model.Status.Should().Be(SessionStatusEnum.Verifying);
        model.ScopeItems.Should().HaveCount(1);
        model.ScopeItems[0].BrgId.Should().Be("BRG01");
        model.CountedItems.Should().HaveCount(1);
        model.CountedItems[0].TotalCountQty.Should().Be(95);
        model.VarianceItems.Should().HaveCount(1);
        model.VarianceItems[0].VarianceQty.Should().Be(-5);
        model.VarianceItems[0].RecountResult.Should().NotBeNull();
        model.VarianceItems[0].RecountResult!.FinalVarianceQty.Should().Be(-2);
        model.VarianceItems[0].FinalVarianceQty.Should().Be(-2);
    }

    [Fact]
    public void SaveChanges_WhenSessionIsNew_ShouldInsertHeaderAndChildren()
    {
        // Arrange
        _sessionDalMock.Setup(d => d.GetData("SES-NEW")).Returns((StockOpnameSessionDto?)null);

        var scope = StockOpnameScopeItemModel.Create("SCP-1", "SES-NEW", "BRG01");
        var counted = StockOpnameCountedItemModel.Create("CNT-1", "SES-NEW", "BRG01", "B01", "2027-12-31", "TAB", 10, DateTime.Now);
        var variance = StockOpnameVarianceItemModel.Create("VAR-1", "SES-NEW", "BRG01", "B01", "2027-12-31", "TAB", 10, 12, 1000);
        var recount = StockOpnameRecountResultModel.Create("REC-1", "VAR-1", 11, 12, 1000, DiscrepancyReasonEnum.ADMIN_ERROR, "Salah hitung", "SPV01", DateTime.Now);
        variance.SetRecountResult(recount);

        var session = StockOpnameSessionModel.Create("SES-NEW", "FAR01", SessionScopeEnum.Partial, DateTime.Today, "USR01", DateTime.Now, [scope]);
        session.StartCounting();
        session.SubmitCounting(hasVariance: true);
        var sessionWithItems = StockOpnameSessionModel.Load(
            session.SessionId, session.LayananId, session.ScopeMode, session.Status, session.CutoffDate,
            session.CreatedBy, session.CreatedAt, scopeItems: [scope], countedItems: [counted], varianceItems: [variance]);

        var sut = CreateSessionRepo();

        // Act
        sut.SaveChanges(sessionWithItems);

        // Assert
        _sessionDalMock.Verify(d => d.Insert(It.Is<StockOpnameSessionDto>(x => x.SessionId == "SES-NEW" && x.Status == "WaitingVerification" && x.ScopeType == "Partial")), Times.Once);
        _scopeItemDalMock.Verify(d => d.Insert(It.IsAny<IEnumerable<StockOpnameScopeItemDto>>()), Times.Once);
        _countedItemDalMock.Verify(d => d.Insert(It.IsAny<IEnumerable<StockOpnameCountedItemDto>>()), Times.Once);
        _varianceItemDalMock.Verify(d => d.Insert(It.Is<StockOpnameVarianceItemDto>(x => x.SessionId == "SES-NEW" && x.BrgId == "BRG01" && x.BatchNo == "B01" && x.ExpiredDate == "2027-12-31")), Times.Once);
        _recountResultDalMock.Verify(d => d.Insert(It.Is<StockOpnameRecountResultDto>(x => x.RecountResultId == "REC-1" && x.SessionId == "SES-NEW" && x.BrgId == "BRG01")), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenSessionExists_ShouldUpdateHeaderAndChildren()
    {
        // Arrange
        var existingDto = new StockOpnameSessionDto(
            "SES-001", "FAR01", "Full", "Counting",
            DateTime.Today, null, null, null, null, "", null, "", "", null, null, "", "", "USR01", DateTime.Now);
        _sessionDalMock.Setup(d => d.GetData("SES-001")).Returns(existingDto);

        var existingVarDto = new StockOpnameVarianceItemDto("SES-001", "BRG01", "B01", "2027-12-31", "TAB", 10, 12, -2, "PendingVerification", null);
        _varianceItemDalMock.Setup(d => d.ListBySession("SES-001")).Returns([existingVarDto]);

        var variance = StockOpnameVarianceItemModel.Create("VAR-1", "SES-001", "BRG01", "B01", "2027-12-31", "TAB", 10, 12, 1000);
        var recount = StockOpnameRecountResultModel.Create("REC-1", "VAR-1", 11, 12, 1000, DiscrepancyReasonEnum.MISPLACED, "Catatan", "SPV01", DateTime.Now);
        variance.SetRecountResult(recount);

        var existingRecountDto = new StockOpnameRecountResultDto("REC-1", "SES-001", "BRG01", "B01", "2027-12-31", 10, DateTime.Now, 12, -2, DiscrepancyReasonEnum.MISPLACED.ToString(), "Lama", "SPV01");
        _recountResultDalMock.Setup(d => d.GetByTuple("SES-001", "BRG01", "B01", "2027-12-31")).Returns(existingRecountDto);

        var session = StockOpnameSessionModel.Load(
            "SES-001", "FAR01", SessionScopeEnum.Full, SessionStatusEnum.Verifying, DateTime.Today,
            "USR01", DateTime.Now, varianceItems: [variance]);

        var sut = CreateSessionRepo();

        // Act
        sut.SaveChanges(session);

        // Assert
        _sessionDalMock.Verify(d => d.Update(It.Is<StockOpnameSessionDto>(x => x.SessionId == "SES-001" && x.Status == "Verifying")), Times.Once);
        _varianceItemDalMock.Verify(d => d.Update(It.Is<StockOpnameVarianceItemDto>(x => x.SessionId == "SES-001" && x.BrgId == "BRG01" && x.BatchNo == "B01" && x.ExpiredDate == "2027-12-31")), Times.Once);
        _recountResultDalMock.Verify(d => d.Update(It.Is<StockOpnameRecountResultDto>(x => x.RecountResultId == "REC-1" && x.SessionId == "SES-001")), Times.Once);
    }

    [Fact]
    public void ListData_ShouldPassStatusAsStringToDal()
    {
        // Arrange
        var sessionDto = new StockOpnameSessionDto(
            "SES-001", "FAR01", "Full", "WaitingApproval",
            DateTime.Today, null, null, null, null, "", null, "", "", null, null, "", "", "USR01", DateTime.Now);
        _sessionDalMock.Setup(d => d.ListData("FAR01", "WaitingApproval", null, null))
            .Returns([sessionDto]);
        var sut = CreateSessionRepo();

        // Act
        var result = sut.ListData("FAR01", SessionStatusEnum.WaitingApproval, null, null).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].SessionId.Should().Be("SES-001");
        _sessionDalMock.Verify(d => d.ListData("FAR01", "WaitingApproval", null, null), Times.Once);
    }

    [Fact]
    public void HasTupleBeenCountedByUser_ShouldDelegateToDal()
    {
        // Arrange
        _countedItemDalMock.Setup(d => d.HasTupleBeenCountedByUser("SES-1", "BRG01", "B01", "2027-12-31", "USR01"))
            .Returns(true);
        var sut = CreateSessionRepo();

        // Act
        var result = sut.HasTupleBeenCountedByUser("SES-1", "BRG01", "B01", "2027-12-31", "USR01");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AdjustmentRepo_InsertSingleAndBatch_ShouldDelegateToDal()
    {
        // Arrange
        var sut = CreateAdjustmentRepo();
        var model = new StockOpnameAdjustmentModel(
            "ADJ001", "SES-001", "BRG01", "B01", "2027-12-31", "FAR01",
            5, "TAB", 1500, 7500, "MUT001", "BK001", 16, DateTime.Now, "MGR01");

        // Act
        sut.Insert(model);
        sut.Insert([model]);

        // Assert
        _adjustmentDalMock.Verify(d => d.Insert(It.Is<StockOpnameAdjustmentDto>(x => x.AdjustmentId == "ADJ001")), Times.Once);
        _adjustmentDalMock.Verify(d => d.Insert(It.Is<IEnumerable<StockOpnameAdjustmentDto>>(x => x.Count() == 1)), Times.Once);
    }

    [Fact]
    public void AdjustmentRepo_Queries_ShouldDelegateToDal()
    {
        // Arrange
        var sut = CreateAdjustmentRepo();
        var dto = new StockOpnameAdjustmentDto(
            "ADJ001", "SES-001", "BRG01", "B01", "2027-12-31", "FAR01",
            5, "TAB", 1500, 7500, "MUT001", "BK001", 16, DateTime.Now, "MGR01");

        _adjustmentDalMock.Setup(d => d.ListBySession("SES-001")).Returns([dto]);
        _adjustmentDalMock.Setup(d => d.ExistsForSession("SES-001")).Returns(true);
        _adjustmentDalMock.Setup(d => d.ExistsForTuple("SES-001", "BRG01", "B01", "2027-12-31")).Returns(true);

        // Act
        var list = sut.ListBySessionId("SES-001").ToList();
        var existsSession = sut.ExistsForSession("SES-001");
        var existsTuple = sut.ExistsForTuple("SES-001", "BRG01", "B01", "2027-12-31");

        // Assert
        list.Should().HaveCount(1);
        list[0].AdjustmentId.Should().Be("ADJ001");
        existsSession.Should().BeTrue();
        existsTuple.Should().BeTrue();
    }
}
