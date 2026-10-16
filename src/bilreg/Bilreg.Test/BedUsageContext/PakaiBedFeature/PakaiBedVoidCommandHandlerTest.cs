using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedVoidCommandHandlerTest
{
    private static readonly DateTime ProposedAt = new(2026, 10, 16, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime StartedAt = new(2026, 10, 16, 8, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime VoidAt = new(2026, 10, 16, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IPakaiBedAlokasiRepo> _alokasiRepoMock = new();
    private readonly Mock<IPakaiBedBillingLockChecker> _billingLockCheckerMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();
    private readonly TestTglJamProvider _tglJamProvider = new(VoidAt);

    private readonly PakaiBedVoidCommandHandler _sut;

    public PakaiBedVoidCommandHandlerTest()
    {
        _sut = new PakaiBedVoidCommandHandler(
            _alokasiRepoMock.Object,
            _billingLockCheckerMock.Object,
            _auditRepoMock.Object,
            _tglJamProvider);
    }

    private static PakaiBedAlokasiModel CreateActiveAllocation(string regId = "REG001")
    {
        var proposed = PakaiBedAlokasiModel.Propose(
            regId, "PAS001", "BG01", "KMR01", "BED01", "WTL001", "REQ001",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "USR01", ProposedAt, ProposedAt.AddMinutes(1), "Initial proposal",
            physicalKelasId: "KL1", billingKelasId: "KL1", tarif: 150000m);

        return proposed.Assign("USR01", StartedAt, StartedAt.AddMinutes(1), "Assignment", "EV001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenPakaiBedIdInvalid_ThrowsArgumentException(string? invalidPakaiBedId)
    {
        // Arrange
        var cmd = new PakaiBedVoidCommand(invalidPakaiBedId!, "USR01", "Batal dirawat", "agent", "127.0.0.1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _alokasiRepoMock.Verify(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()), Times.Never);
        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenUserIdInvalid_ThrowsArgumentException(string? invalidUserId)
    {
        // Arrange
        var cmd = new PakaiBedVoidCommand("PKB001", invalidUserId!, "Batal dirawat", "agent", "127.0.0.1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _alokasiRepoMock.Verify(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()), Times.Never);
        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_WhenVoidReasonInvalid_ThrowsArgumentException(string? invalidReason)
    {
        // Arrange
        var cmd = new PakaiBedVoidCommand("PKB001", "USR01", invalidReason!, "agent", "127.0.0.1");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _alokasiRepoMock.Verify(x => x.LoadEntity(It.IsAny<IPakaiBedAlokasiKey>()), Times.Never);
        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPakaiBedNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new PakaiBedVoidCommand("PKB-MISSING", "USR01", "Batal penempatan", "agent", "127.0.0.1");
        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == "PKB-MISSING")))
            .Returns(MayBe<PakaiBedAlokasiModel>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PKB-MISSING*");
        _billingLockCheckerMock.Verify(x => x.CheckLockStatusAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBillingLockedByRoomCharge_ThrowsInvalidOperationException_FailClosed()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-RC");
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR01", "Salah input pasien", "agent", "127.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BillingLockCheckResult.Locked("Terdapat transaksi sewa kamar aktif pada ta_trs_roomcharge"));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*tagihan terkunci*");
        ex.WithMessage("*ta_trs_roomcharge*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBillingLockedByCashierPayment_ThrowsInvalidOperationException_FailClosed()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-PAY");
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR01", "Batal sewa", "agent", "127.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BillingLockCheckResult.Locked("Terdapat transaksi pembayaran kasir pada ta_trs_billing2"));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*tagihan terkunci*");
        ex.WithMessage("*ta_trs_billing2*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBillingLockedByTataRekening_ThrowsInvalidOperationException_FailClosed()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-TR");
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR01", "Batal sewa", "agent", "127.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BillingLockCheckResult.Locked("Status tata rekening pada BILRG_TataRekening sudah terkunci"));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*tagihan terkunci*");
        ex.WithMessage("*BILRG_TataRekening*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLockCheckerReturnsNullResult_ThrowsInvalidOperationException_FailClosed()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-NULL");
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR01", "Batal sewa", "agent", "127.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingLockCheckResult)null!);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*Fail-Closed*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLockCheckerThrowsException_PropagatesException_FailClosed()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-ERR");
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR01", "Batal sewa", "agent", "127.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database connection timeout during lock check"));

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*Database connection timeout*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAllocationAlreadyCancelled_ThrowsInvalidOperationException()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-ALREADY-VOID")
            .Void(VoidAt.AddMinutes(-10), "Pembatalan awal", "USR00", VoidAt.AddMinutes(-10));
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR01", "Batal ulang", "agent", "127.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BillingLockCheckResult.Unlocked());

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");

        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Never);
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBillingUnlocked_CallsDomainVoid_AndSavesChangesToAlokasiRepo()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-SUCCESS");
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR-NURSE", "Pasien membatalkan rawat inap", "UnitTests", "192.168.1.100");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BillingLockCheckResult.Unlocked());

        PakaiBedAlokasiModel? savedAlokasi = null;
        _alokasiRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()))
            .Callback<PakaiBedAlokasiModel>(model => savedAlokasi = model);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _billingLockCheckerMock.Verify(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()), Times.Once);
        _alokasiRepoMock.Verify(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()), Times.Once);

        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.PakaiBedId.Should().Be(allocation.PakaiBedId);
        savedAlokasi.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Cancelled);
        savedAlokasi.Version.Should().Be(allocation.Version + 1);

        var voidTransition = savedAlokasi.ListTransition.Last();
        voidTransition.PakaiBedTransition.Should().Be(PakaiBedTransisiEnum.Cancelled);
        voidTransition.ActorId.Should().Be("USR-NURSE");
        voidTransition.Reason.Should().Be("Pasien membatalkan rawat inap");
        voidTransition.OccurredAt.Should().Be(VoidAt);
    }

    [Fact]
    public async Task Handle_WhenBillingUnlocked_SavesAuditLogWithPakaiBedModelEntityName()
    {
        // Arrange
        var allocation = CreateActiveAllocation("REG-AUDIT");
        var cmd = new PakaiBedVoidCommand(
            allocation.PakaiBedId,
            "USR-AUDITOR",
            "Kesalahan kamar saat admisi",
            "Mozilla/5.0 TestBrowser",
            "10.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BillingLockCheckResult.Unlocked());

        AuditLog? capturedAudit = null;
        _auditRepoMock.Setup(x => x.SaveChanges(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(audit => capturedAudit = audit);

        // Act
        await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Once);

        capturedAudit.Should().NotBeNull();
        capturedAudit!.ActionType.Should().Be("VOID");
        capturedAudit.EntityName.Should().Be(nameof(PakaiBedModel)); // CRITICAL: Verify entity name is PakaiBedModel, NOT TindakanModel
        capturedAudit.EntityName.Should().NotBe("TindakanModel");
        capturedAudit.EntityId.Should().Be(allocation.PakaiBedId);
        capturedAudit.UserId.Should().Be("USR-AUDITOR");
        capturedAudit.Reason.Should().Be("Kesalahan kamar saat admisi");
        capturedAudit.CorrelationId.Should().Be(allocation.RegId);
        capturedAudit.ClientIpAddress.Should().Be("10.0.0.1");
        capturedAudit.UserAgent.Should().Be("Mozilla/5.0 TestBrowser");
        capturedAudit.OriginalDataJson.Should().NotBeNullOrWhiteSpace();
        capturedAudit.OriginalDataJson.Should().Contain(allocation.PakaiBedId);
    }

    [Fact]
    public async Task Handle_WhenClockReturnsUnspecifiedKind_ConvertsToUtcWithoutThrowing()
    {
        // Arrange
        var unspecifiedTime = new DateTime(2026, 10, 16, 9, 30, 0, DateTimeKind.Unspecified);
        var clock = new Mock<Nuna.Lib.ValidationHelper.ITglJamProvider>();
        clock.Setup(x => x.Now).Returns(unspecifiedTime);

        var handler = new PakaiBedVoidCommandHandler(
            _alokasiRepoMock.Object,
            _billingLockCheckerMock.Object,
            _auditRepoMock.Object,
            clock.Object);

        var allocation = CreateActiveAllocation("REG-UNSPEC");
        var cmd = new PakaiBedVoidCommand(allocation.PakaiBedId, "USR01", "Batal bed", "agent", "127.0.0.1");

        _alokasiRepoMock.Setup(x => x.LoadEntity(It.Is<IPakaiBedAlokasiKey>(k => k.PakaiBedId == allocation.PakaiBedId)))
            .Returns(MayBe.From(allocation));

        _billingLockCheckerMock.Setup(x => x.CheckLockStatusAsync(allocation.PakaiBedId, allocation.RegId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BillingLockCheckResult.Unlocked());

        PakaiBedAlokasiModel? savedAlokasi = null;
        _alokasiRepoMock.Setup(x => x.SaveChanges(It.IsAny<PakaiBedAlokasiModel>()))
            .Callback<PakaiBedAlokasiModel>(model => savedAlokasi = model);

        // Act
        var act = () => handler.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        savedAlokasi.Should().NotBeNull();
        savedAlokasi!.ListTransition.Last().OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}
