using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueCompleteHandoverHandlerTest
{
    private readonly Mock<IApotekQueueEntryRepo> _queueEntryRepo = new();
    private readonly Mock<IPharmacyServicePointRepo> _servicePointRepo = new();
    private readonly Mock<IPharmacyQueueMappingRepo> _mappingRepo = new();
    private readonly Mock<IPaymentClearancePort> _paymentClearancePort = new();
    private readonly Mock<IPharmacyQueueOperationRepo> _operationRepo = new();
    private readonly Mock<IPharmacyQueuePublisher> _publisher = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly Mock<ITrackerPharmacyPort> _tracker = new();

    private readonly DateTime _now = new(2026, 10, 18, 11, 0, 0);

    public PharmacyQueueCompleteHandoverHandlerTest()
    {
        _clock.Setup(x => x.Now).Returns(_now);
    }

    private PharmacyQueueCompleteHandoverHandler CreateSut(IAptAuthorizationPolicy? auth = null)
        => new(
            _queueEntryRepo.Object,
            _servicePointRepo.Object,
            _mappingRepo.Object,
            _paymentClearancePort.Object,
            _operationRepo.Object,
            _publisher.Object,
            _clock.Object,
            auth ?? new AllowAllAuth(),
            _tracker.Object);

    [Fact]
    public async Task Handle_WhenPaymentCleared_CompletesHandover_TransitionsToSelesai_AndSyncsTracker()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;
        const string penjualanId = "DU261018-0001";

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now.AddMinutes(-10));
        entry.StartServiceSerah(_now.AddMinutes(-2));

        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        _paymentClearancePort.Setup(x => x.CheckClearance(penjualanId, ""))
            .Returns(new PaymentClearanceCheckResult(true, "CashierSettled", "Tagihan lunas."));

        var mapping = QueueMappingModel.Create(QueueDemandKindEnum.ResepKerja, "RK-01", antrianId, noUrut, "TRK-01", QueueMappingMethodEnum.Manual, "user", DateTime.Now);
        _mappingRepo.Setup(x => x.ListByQueue(antrianId, noUrut)).Returns([mapping]);

        var sp = PharmacyServicePointModel.Create("SP-01", "DEP-01", "Depo Rajal", "A");
        _servicePointRepo.Setup(x => x.Get("SP-01")).Returns(MayBe.From(sp));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            PenjualanId: penjualanId,
            UserId: "apt1");

        // Act
        var res = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        res.Success.Should().BeTrue();
        res.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
        res.DoneAt.Should().Be(_now);
        res.IsPaymentOverridden.Should().BeFalse();

        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
        entry.DoneAt.Should().Be(_now);
        _queueEntryRepo.Verify(x => x.Save(entry), Times.Once);

        _tracker.Verify(x => x.DoneOnce(antrianId, noUrut, "TRK-01", entry.QueueLabel, _now), Times.Once);
        _publisher.Verify(x => x.PublishWorklistRefresh("DEP-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSepValid_CompletesHandover_ForBpjsPatient()
    {
        const string antrianId = "Q-01";
        const int noUrut = 15;
        const string noSep = "0123R0011026V000001";

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);

        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        _paymentClearancePort.Setup(x => x.CheckClearance("", noSep))
            .Returns(new PaymentClearanceCheckResult(true, "BpjsSepValid", "SEP terverifikasi sah."));

        var sp = PharmacyServicePointModel.Create("SP-01", "DEP-01", "Depo Rajal", "A");
        _servicePointRepo.Setup(x => x.Get("SP-01")).Returns(MayBe.From(sp));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            NoSep: noSep,
            UserId: "apt1");

        // Act
        var res = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        res.Success.Should().BeTrue();
        res.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
    }

    [Fact]
    public async Task Handle_WhenPaymentNotClearedAndNoOverride_ThrowsDomainException()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;
        const string penjualanId = "DU261018-0001";

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);

        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        _paymentClearancePort.Setup(x => x.CheckClearance(penjualanId, ""))
            .Returns(new PaymentClearanceCheckResult(false, "Unpaid", "Tagihan belum dibayar di kasir."));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            PenjualanId: penjualanId,
            IsPaymentOverridden: false);

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*gating kasir/SEP belum terpenuhi*");
        _queueEntryRepo.Verify(x => x.Save(It.IsAny<ApotekQueueEntryModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentOverridden_CompletesHandover_WithAuditStamps()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;
        const string reason = "Kedaruratan klinis hemostasis";
        const string pharmacistId = "APT-99";

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);

        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var sp = PharmacyServicePointModel.Create("SP-01", "DEP-01", "Depo Rajal", "A");
        _servicePointRepo.Setup(x => x.Get("SP-01")).Returns(MayBe.From(sp));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            IsPaymentOverridden: true,
            PaymentOverrideReason: reason,
            PaymentOverridePharmacistId: pharmacistId,
            UserId: "apt1");

        // Act
        var res = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        res.Success.Should().BeTrue();
        res.IsPaymentOverridden.Should().BeTrue();
        entry.IsPaymentOverridden.Should().BeTrue();
        entry.PaymentOverrideReason.Should().Be(reason);
        entry.PaymentOverridePharmacistId.Should().Be(pharmacistId);
        entry.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
        _queueEntryRepo.Verify(x => x.Save(entry), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPaymentOverriddenWithoutReason_ThrowsDomainException()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            IsPaymentOverridden: true,
            PaymentOverrideReason: "", // Empty reason!
            PaymentOverridePharmacistId: "APT-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Payment override reason is required*");
    }

    [Fact]
    public async Task Handle_WhenPaymentOverriddenWithoutPharmacistId_ThrowsDomainException()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            IsPaymentOverridden: true,
            PaymentOverrideReason: "Alasan klinis",
            PaymentOverridePharmacistId: ""); // Empty pharmacist ID!

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Pharmacist ID is required*");
    }

    [Fact]
    public async Task Handle_WhenQueueNotInSiapSerahOrSerahObat_ThrowsDomainException()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan(); // Still PENYIAPAN!
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            IsPaymentOverridden: true,
            PaymentOverrideReason: "Reason",
            PaymentOverridePharmacistId: "APT-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Handover requires SIAP_SERAH or SERAH_OBAT*");
    }

    [Fact]
    public async Task Handle_WhenQueueAlreadyTerminal_ThrowsDomainException()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now.AddMinutes(-10));
        entry.CompleteHandover(_now.AddMinutes(-5)); // Already SELESAI!
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            IsPaymentOverridden: true,
            PaymentOverrideReason: "Reason",
            PaymentOverridePharmacistId: "APT-01");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Cannot complete handover when phase is 'SELESAI'*");
    }

    [Fact]
    public async Task Handle_WhenLoketKeyProvided_ReleasesLoketClaim()
    {
        const string antrianId = "Q-01";
        const int noUrut = 12;
        const string loketKey = "APT-SRH-01";

        var entry = ApotekQueueEntryModel.Create(antrianId, noUrut, "SP-01", "A");
        entry.TransitionToPenyiapan();
        entry.ConsolidateToSiapSerah(_now);
        _queueEntryRepo.Setup(x => x.Get(antrianId, noUrut)).Returns(MayBe.From(entry));

        var rv = new byte[] { 1, 2, 3, 4 };
        var loket = new PharmacyLoketSession(
            loketKey, LoketTypeEnum.SerahObat, "Loket 2", "DEP-01",
            antrianId, noUrut, LoketClaimStateEnum.InService, true, 3,
            DateTime.Now, DateTime.Now, ApotekDate.Empty, rv);

        _operationRepo.Setup(x => x.GetLoket(loketKey)).Returns(MayBe.From(loket));

        _paymentClearancePort.Setup(x => x.CheckClearance("DU01", ""))
            .Returns(new PaymentClearanceCheckResult(true, "Cleared", "Lunas"));

        var sp = PharmacyServicePointModel.Create("SP-01", "DEP-01", "Depo Rajal", "A");
        _servicePointRepo.Setup(x => x.Get("SP-01")).Returns(MayBe.From(sp));

        var sut = CreateSut();
        var cmd = new PharmacyQueueCompleteHandoverCmd(
            AntrianId: antrianId,
            NoUrut: noUrut,
            PenjualanId: "DU01",
            LoketKey: loketKey,
            ExpectedLoketRowVersion: rv,
            UserId: "apt1");

        await sut.Handle(cmd, CancellationToken.None);

        _operationRepo.Verify(x => x.TryReleaseLoket(loketKey, rv, "apt1", _now), Times.Once);
        _publisher.Verify(x => x.PublishDisplayRefresh(loketKey, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenAuthPolicyRejects()
    {
        var authMock = new Mock<IAptAuthorizationPolicy>();
        authMock.Setup(x => x.AssertCommandAllowed(nameof(PharmacyQueueCompleteHandoverCmd), "bad_user"))
            .Throws(new UnauthorizedAccessException("Forbidden"));

        var sut = CreateSut(authMock.Object);
        var cmd = new PharmacyQueueCompleteHandoverCmd("Q-01", 1, UserId: "bad_user");

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Forbidden");
    }
}
