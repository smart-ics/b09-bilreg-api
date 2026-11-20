using System.ComponentModel.DataAnnotations;
using Bilreg.Application.InventoryContext.OpnameFeature.EventHandlers;
using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Application.InventoryContext.OpnameFeature.UseCases.Commands;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class VerificationAndApprovalCommandsTests
{
    private readonly Mock<IStockOpnameSessionRepo> _sessionRepoMock = new();
    private readonly Mock<ISystemQuantityReconstructionService> _reconstructionServiceMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

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
            varianceItems: varianceItems
        );
    }

    private static StockOpnameVarianceItemModel CreateVarianceItem(
        string varianceItemId = "VAR-001",
        string sessionId = "SES-001",
        string brgId = "BRG01",
        string batchNo = "BATCH01",
        string expiredDate = "2027-12-31",
        string baseSatuanId = "TAB",
        decimal countQty = 90,
        decimal systemQty = 100,
        decimal hpp = 1500)
    {
        return StockOpnameVarianceItemModel.Create(
            varianceItemId,
            sessionId,
            brgId,
            batchNo,
            expiredDate,
            baseSatuanId,
            countQty,
            systemQty,
            hpp);
    }
    #endregion

    #region START VERIFICATION TESTS
    [Fact]
    public async Task StartVerification_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new StartVerificationHandler(_sessionRepoMock.Object);
        var cmd = new StartVerificationCommand("SES-NONEXIST");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task StartVerification_WhenStatusIsDraft_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.Draft);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new StartVerificationHandler(_sessionRepoMock.Object);
        var cmd = new StartVerificationCommand(session.SessionId);

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*WaitingVerification*");
    }

    [Fact]
    public async Task StartVerification_WhenStatusIsWaitingVerification_ShouldTransitionToVerifying()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.WaitingVerification);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new StartVerificationHandler(_sessionRepoMock.Object);
        var cmd = new StartVerificationCommand(session.SessionId);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.SessionId.Should().Be(session.SessionId);
        result.Status.Should().Be(SessionStatusEnum.Verifying);
        session.Status.Should().Be(SessionStatusEnum.Verifying);
        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }
    #endregion

    #region RECORD RECOUNT TESTS
    [Fact]
    public async Task RecordRecount_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new RecordRecountCommand("SES-NONEXIST", "VAR-001", 95, DiscrepancyReasonEnum.DAMAGED_EXPIRED, "Rusak", "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RecordRecount_WhenSessionNotVerifying_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.Counting);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new RecordRecountCommand(session.SessionId, "VAR-001", 95, DiscrepancyReasonEnum.DAMAGED_EXPIRED, "Rusak", "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expected Verifying*");
    }

    [Fact]
    public async Task RecordRecount_WhenVarianceItemNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new RecordRecountCommand(session.SessionId, "VAR-NOTFOUND", 95, DiscrepancyReasonEnum.DAMAGED_EXPIRED, "Rusak", "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*VAR-NOTFOUND*");
    }

    [Fact]
    public async Task RecordRecount_WhenSegregationOfDutiesViolated_ShouldThrowValidationException()
    {
        // Arrange (BR-SO-006 / TD-06)
        var varItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31");
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [varItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        _sessionRepoMock
            .Setup(r => r.HasTupleBeenCountedByUser("SES-001", "BRG01", "BATCH01", "2027-12-31", "STAFF_SAME"))
            .Returns(true);

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new RecordRecountCommand(session.SessionId, "VAR-001", 95, DiscrepancyReasonEnum.DAMAGED_EXPIRED, "Rusak", "STAFF_SAME");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Staf penghitung fisik awal dilarang melakukan verifikasi hitung ulang*");
    }

    [Fact]
    public async Task RecordRecount_WhenVariancePersistsAndDiscrepancyReasonMissing_ShouldThrowValidationException()
    {
        // Arrange (BR-SO-014)
        var varItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31", countQty: 90, systemQty: 100);
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [varItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        _sessionRepoMock
            .Setup(r => r.HasTupleBeenCountedByUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var reconstructedDict = new Dictionary<ItemTupleKey, decimal>
        {
            [new ItemTupleKey("BRG01", "BATCH01", "2027-12-31")] = 100m
        };
        _reconstructionServiceMock
            .Setup(s => s.ReconstructBalancesAsync("FAR01", It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconstructedDict);

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new RecordRecountCommand(session.SessionId, "VAR-001", 95, DiscrepancyReason: null, Notes: "Notes exist", RecountedBy: "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Discrepancy reason is mandatory*");
    }

    [Fact]
    public async Task RecordRecount_WhenVariancePersistsAndNotesMissing_ShouldThrowValidationException()
    {
        // Arrange (BR-SO-014)
        var varItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31", countQty: 90, systemQty: 100);
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [varItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        _sessionRepoMock
            .Setup(r => r.HasTupleBeenCountedByUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var reconstructedDict = new Dictionary<ItemTupleKey, decimal>
        {
            [new ItemTupleKey("BRG01", "BATCH01", "2027-12-31")] = 100m
        };
        _reconstructionServiceMock
            .Setup(s => s.ReconstructBalancesAsync("FAR01", It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconstructedDict);

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new RecordRecountCommand(session.SessionId, "VAR-001", 95, DiscrepancyReason: DiscrepancyReasonEnum.DAMAGED_EXPIRED, Notes: "   ", RecountedBy: "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Investigation notes are mandatory*");
    }

    [Fact]
    public async Task RecordRecount_WhenValidRecountWithPersistentVariance_ShouldMarkVerifiedAndSave()
    {
        // Arrange
        var varItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31", countQty: 90, systemQty: 100, hpp: 2000);
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [varItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        _sessionRepoMock
            .Setup(r => r.HasTupleBeenCountedByUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var reconstructedDict = new Dictionary<ItemTupleKey, decimal>
        {
            [new ItemTupleKey("BRG01", "BATCH01", "2027-12-31")] = 100m
        };
        _reconstructionServiceMock
            .Setup(s => s.ReconstructBalancesAsync("FAR01", It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconstructedDict);

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var recountTime = new DateTime(2026, 11, 20, 11, 30, 0);
        var cmd = new RecordRecountCommand(
            SessionId: session.SessionId,
            VarianceItemId: "VAR-001",
            RecountQty: 96,
            DiscrepancyReason: DiscrepancyReasonEnum.DAMAGED_EXPIRED,
            Notes: "Ditemukan 4 tablet kemasan rusak",
            RecountedBy: "SPV01",
            RecountTime: recountTime);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.VarianceItemId.Should().Be("VAR-001");
        result.RecountQty.Should().Be(96);
        result.ReconstructedSystemQty.Should().Be(100);
        result.FinalVarianceQty.Should().Be(-4);
        result.FinalValuation.Should().Be(-8000);
        result.DiscrepancyReason.Should().Be(DiscrepancyReasonEnum.DAMAGED_EXPIRED);
        result.Notes.Should().Be("Ditemukan 4 tablet kemasan rusak");
        result.IsVerified.Should().BeTrue();

        varItem.IsVerified.Should().BeTrue();
        varItem.RecountResult.Should().NotBeNull();
        varItem.RecountResult!.RecountQty.Should().Be(96);
        varItem.FinalVarianceQty.Should().Be(-4);
        varItem.FinalValuation.Should().Be(-8000);

        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }

    [Fact]
    public async Task RecordRecount_WhenFinalVarianceIsZero_ShouldSucceedWithoutMandatoryReason()
    {
        // Arrange
        var varItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31", countQty: 90, systemQty: 100, hpp: 2000);
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [varItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        _sessionRepoMock
            .Setup(r => r.HasTupleBeenCountedByUser(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var reconstructedDict = new Dictionary<ItemTupleKey, decimal>
        {
            [new ItemTupleKey("BRG01", "BATCH01", "2027-12-31")] = 100m
        };
        _reconstructionServiceMock
            .Setup(s => s.ReconstructBalancesAsync("FAR01", It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reconstructedDict);

        var sut = new RecordRecountHandler(_sessionRepoMock.Object, _reconstructionServiceMock.Object);
        var cmd = new RecordRecountCommand(
            SessionId: session.SessionId,
            VarianceItemId: "VAR-001",
            RecountQty: 100, // Matches reconstructed balance
            DiscrepancyReason: null,
            Notes: "",
            RecountedBy: "SPV01");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.FinalVarianceQty.Should().Be(0);
        result.FinalValuation.Should().Be(0);
        result.IsVerified.Should().BeTrue();
        varItem.IsVerified.Should().BeTrue();
        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }
    #endregion

    #region BULK VERIFY MINOR VARIANCES TESTS
    [Fact]
    public async Task BulkVerifyMinor_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new BulkVerifyMinorVariancesHandler(_sessionRepoMock.Object);
        var cmd = new BulkVerifyMinorVariancesCommand("SES-NONEXIST", 10000, DiscrepancyReasonEnum.ADMIN_ERROR, "Bulk verify", "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task BulkVerifyMinor_WhenSessionNotVerifying_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.WaitingApproval);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new BulkVerifyMinorVariancesHandler(_sessionRepoMock.Object);
        var cmd = new BulkVerifyMinorVariancesCommand(session.SessionId, 10000, DiscrepancyReasonEnum.ADMIN_ERROR, "Bulk verify", "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expected Verifying*");
    }

    [Fact]
    public async Task BulkVerifyMinor_WhenThresholdIsNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = new BulkVerifyMinorVariancesHandler(_sessionRepoMock.Object);
        var cmd = new BulkVerifyMinorVariancesCommand("SES-001", -500, DiscrepancyReasonEnum.ADMIN_ERROR, "Bulk verify", "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Threshold must be non-negative*");
    }

    [Fact]
    public async Task BulkVerifyMinor_WhenVariancesPresent_ShouldVerifyOnlyItemsUnderThreshold()
    {
        // Arrange (TD-07: Minor variances <= threshold verified; larger variances untouched)
        var minorItem1 = CreateVarianceItem("VAR-M1", "SES-001", "BRG01", "BATCH01", "2027-12-31", countQty: 98, systemQty: 100, hpp: 1000); // Diff: -2, Val: -2000
        var minorItem2 = CreateVarianceItem("VAR-M2", "SES-001", "BRG02", "BATCH02", "2027-12-31", countQty: 105, systemQty: 100, hpp: 1500); // Diff: +5, Val: +7500
        var majorItem = CreateVarianceItem("VAR-MAJ", "SES-001", "BRG03", "BATCH03", "2027-12-31", countQty: 50, systemQty: 100, hpp: 5000); // Diff: -50, Val: -250000
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [minorItem1, minorItem2, majorItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new BulkVerifyMinorVariancesHandler(_sessionRepoMock.Object);
        var cmd = new BulkVerifyMinorVariancesCommand(
            SessionId: session.SessionId,
            MaxNominalThreshold: 10000m,
            StandardReason: DiscrepancyReasonEnum.ADMIN_ERROR,
            Notes: "Verifikasi massal selisih kecil",
            VerifiedBy: "SPV01");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.VerifiedCount.Should().Be(2);
        result.VerifiedVarianceItemIds.Should().Contain(["VAR-M1", "VAR-M2"]);
        result.VerifiedVarianceItemIds.Should().NotContain("VAR-MAJ");

        minorItem1.IsVerified.Should().BeTrue();
        minorItem1.RecountResult!.DiscrepancyReason.Should().Be(DiscrepancyReasonEnum.ADMIN_ERROR);
        minorItem1.RecountResult.Notes.Should().Be("Verifikasi massal selisih kecil");

        minorItem2.IsVerified.Should().BeTrue();
        minorItem2.RecountResult!.DiscrepancyReason.Should().Be(DiscrepancyReasonEnum.ADMIN_ERROR);

        majorItem.IsVerified.Should().BeFalse();
        majorItem.RecountResult.Should().BeNull();

        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }
    #endregion

    #region SUBMIT VERIFICATION TESTS
    [Fact]
    public async Task SubmitVerification_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new SubmitVerificationHandler(_sessionRepoMock.Object);
        var cmd = new SubmitVerificationCommand("SES-NONEXIST", "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SubmitVerification_WhenSessionNotVerifying_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.WaitingVerification);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new SubmitVerificationHandler(_sessionRepoMock.Object);
        var cmd = new SubmitVerificationCommand(session.SessionId, "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expected Verifying*");
    }

    [Fact]
    public async Task SubmitVerification_WhenUnverifiedVariancesExist_ShouldThrowInvalidOperationException()
    {
        // Arrange (BR-SO-015)
        var unverifiedItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31");
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [unverifiedItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new SubmitVerificationHandler(_sessionRepoMock.Object);
        var cmd = new SubmitVerificationCommand(session.SessionId, "SPV01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*All variance items must be verified*");
    }

    [Fact]
    public async Task SubmitVerification_WhenAllVariancesVerified_ShouldTransitionToWaitingApproval()
    {
        // Arrange
        var varItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31");
        varItem.MarkVerifiedWithReason(DiscrepancyReasonEnum.ADMIN_ERROR, "Telah diverifikasi", "SPV01", DateTime.Now);

        var session = CreateSampleSession(status: SessionStatusEnum.Verifying, varianceItems: [varItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new SubmitVerificationHandler(_sessionRepoMock.Object);
        var verifiedAt = new DateTime(2026, 11, 20, 13, 0, 0);
        var cmd = new SubmitVerificationCommand(session.SessionId, "SPV01", verifiedAt);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.SessionId.Should().Be(session.SessionId);
        result.Status.Should().Be(SessionStatusEnum.WaitingApproval);
        result.VerifiedBy.Should().Be("SPV01");
        result.VerifiedAt.Should().Be(verifiedAt);

        session.Status.Should().Be(SessionStatusEnum.WaitingApproval);
        session.VerifiedBy.Should().Be("SPV01");
        session.VerifiedAt.Should().Be(verifiedAt);

        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }
    #endregion

    #region APPROVE SESSION TESTS
    [Fact]
    public async Task ApproveSession_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new ApproveSessionHandler(_sessionRepoMock.Object, _mediatorMock.Object);
        var cmd = new ApproveSessionCommand("SES-NONEXIST", "MGR01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ApproveSession_WhenSessionNotWaitingApproval_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new ApproveSessionHandler(_sessionRepoMock.Object, _mediatorMock.Object);
        var cmd = new ApproveSessionCommand(session.SessionId, "MGR01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expected WaitingApproval*");
    }

    [Fact]
    public async Task ApproveSession_WhenWaitingApproval_ShouldTransitionToApprovedAndPublishEvent()
    {
        // Arrange (TD-09)
        var varItemSurplus = CreateVarianceItem("VAR-01", "SES-001", "BRG01", "BATCH01", "2027-12-31", countQty: 110, systemQty: 100, hpp: 1000); // +10, Val: +10,000
        varItemSurplus.MarkVerifiedWithReason(DiscrepancyReasonEnum.ADMIN_ERROR, "Surplus", "SPV01", DateTime.Now);

        var varItemDeficit = CreateVarianceItem("VAR-02", "SES-001", "BRG02", "BATCH02", "2027-12-31", countQty: 95, systemQty: 100, hpp: 2000); // -5, Val: -10,000
        varItemDeficit.MarkVerifiedWithReason(DiscrepancyReasonEnum.DAMAGED_EXPIRED, "Defisit rusak", "SPV01", DateTime.Now);

        var session = CreateSampleSession(
            sessionId: "SES-001",
            layananId: "FAR01",
            status: SessionStatusEnum.WaitingApproval,
            varianceItems: [varItemSurplus, varItemDeficit]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new ApproveSessionHandler(_sessionRepoMock.Object, _mediatorMock.Object);
        var approvedAt = new DateTime(2026, 11, 20, 14, 0, 0);
        var cmd = new ApproveSessionCommand(session.SessionId, "MGR01", approvedAt);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.SessionId.Should().Be("SES-001");
        result.Status.Should().Be(SessionStatusEnum.Approved);
        result.ApprovedBy.Should().Be("MGR01");
        result.ApprovedAt.Should().Be(approvedAt);
        result.SurplusValuation.Should().Be(10000);
        result.DeficitValuation.Should().Be(10000);
        result.NetValuation.Should().Be(0);
        result.ApprovedItemsCount.Should().Be(2);

        session.Status.Should().Be(SessionStatusEnum.Approved);
        session.ApprovedBy.Should().Be("MGR01");
        session.ApprovedAt.Should().Be(approvedAt);

        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);

        // Verify StockOpnameApprovedNotification was published via MediatR
        _mediatorMock.Verify(m => m.Publish(
            It.Is<StockOpnameApprovedNotification>(n =>
                n.SessionId == "SES-001" &&
                n.LayananId == "FAR01" &&
                n.ApprovedBy == "MGR01" &&
                n.Items.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }
    #endregion

    #region REJECT SESSION TESTS
    [Fact]
    public async Task RejectSession_WhenSessionNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe<StockOpnameSessionModel>.None);

        var sut = new RejectSessionHandler(_sessionRepoMock.Object);
        var cmd = new RejectSessionCommand("SES-NONEXIST", RejectionDispositionEnum.Rework, "Periksa ulang", "MGR01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task RejectSession_WhenSessionNotWaitingApproval_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSampleSession(status: SessionStatusEnum.Verifying);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new RejectSessionHandler(_sessionRepoMock.Object);
        var cmd = new RejectSessionCommand(session.SessionId, RejectionDispositionEnum.Rework, "Periksa ulang", "MGR01");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Expected WaitingApproval*");
    }

    [Fact]
    public async Task RejectSession_WhenDispositionIsRework_ShouldReturnToVerifyingAndPreserveRecountDrafts()
    {
        // Arrange (TD-08: Rework returns to Verifying preserving recount drafts)
        var varItem = CreateVarianceItem("VAR-001", "SES-001", "BRG01", "BATCH01", "2027-12-31", countQty: 95, systemQty: 100);
        var recountResult = StockOpnameRecountResultModel.Create(
            "REC-001", "VAR-001", 98, 100, 1500, DiscrepancyReasonEnum.MISPLACED, "Draf recount", "SPV01", DateTime.Now);
        varItem.SetRecountResult(recountResult);

        var session = CreateSampleSession(
            sessionId: "SES-001",
            status: SessionStatusEnum.WaitingApproval,
            varianceItems: [varItem]);

        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new RejectSessionHandler(_sessionRepoMock.Object);
        var cmd = new RejectSessionCommand(
            SessionId: session.SessionId,
            Disposition: RejectionDispositionEnum.Rework,
            Notes: "Harap periksa kembali batch 01",
            RejectedBy: "MGR01");

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.SessionId.Should().Be("SES-001");
        result.Status.Should().Be(SessionStatusEnum.Verifying);
        result.Disposition.Should().Be(RejectionDispositionEnum.Rework);
        result.RevisionNotes.Should().Be("Harap periksa kembali batch 01");

        session.Status.Should().Be(SessionStatusEnum.Verifying);
        session.RevisionNotes.Should().Be("Harap periksa kembali batch 01");

        // Verify recount draft is preserved
        varItem.RecountResult.Should().NotBeNull();
        varItem.RecountResult!.RecountResultId.Should().Be("REC-001");
        varItem.RecountResult.RecountQty.Should().Be(98);

        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }

    [Fact]
    public async Task RejectSession_WhenDispositionIsTerminal_ShouldTransitionToRejected()
    {
        // Arrange (TD-08: Terminal permanently marks Rejected)
        var session = CreateSampleSession(status: SessionStatusEnum.WaitingApproval);
        _sessionRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<StockOpnameSessionKey>()))
            .Returns(MayBe.From(session));

        var sut = new RejectSessionHandler(_sessionRepoMock.Object);
        var rejectedAt = new DateTime(2026, 11, 20, 15, 0, 0);
        var cmd = new RejectSessionCommand(
            SessionId: session.SessionId,
            Disposition: RejectionDispositionEnum.Terminal,
            Notes: "Sesi dibatalkan permanen karena prosedur cacat",
            RejectedBy: "MGR01",
            RejectedAt: rejectedAt);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.SessionId.Should().Be(session.SessionId);
        result.Status.Should().Be(SessionStatusEnum.Rejected);
        result.Disposition.Should().Be(RejectionDispositionEnum.Terminal);
        result.RevisionNotes.Should().Be("Sesi dibatalkan permanen karena prosedur cacat");

        session.Status.Should().Be(SessionStatusEnum.Rejected);
        session.RejectedBy.Should().Be("MGR01");
        session.RejectedAt.Should().Be(rejectedAt);
        session.RevisionNotes.Should().Be("Sesi dibatalkan permanen karena prosedur cacat");

        _sessionRepoMock.Verify(r => r.SaveChanges(session), Times.Once);
    }
    #endregion
}
