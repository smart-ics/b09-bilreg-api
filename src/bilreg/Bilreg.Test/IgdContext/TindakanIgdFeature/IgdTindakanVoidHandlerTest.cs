using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Application.Shared.BillingLockFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class IgdTindakanVoidHandlerTest
{
    private readonly Mock<ITindakanIgdRepo> _tindakanRepo = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IBillingLockValidationService> _billingLockValidator = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly IgdTindakanVoidHandler _sut;

    public IgdTindakanVoidHandlerTest()
    {
        _sut = new IgdTindakanVoidHandler(
            _tindakanRepo.Object,
            _auditRepo.Object,
            _visitRepo.Object,
            new TestTglJamProvider(new DateTime(2026, 1, 1, 10, 0, 0)),
            _billingLockValidator.Object,
            _trsBillingRepo.Object);
    }

    private static IgdVisitModel BuildVisit(string visitId = "IGV0001", string regId = "REG001")
    {
        var audit = new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 0, 0));
        return new IgdVisitModel(
            igdVisitId: visitId,
            daftarDateTime: audit.Timestamp,
            visitor: VisitorType.Default,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Registered,
            reg: new RegReff(regId, "PAS001", "Pasien Tes"),
            redirection: RedirectionType.Default,
            bedId: "-",
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: [],
            hasPendingBilling: false);
    }

    private static TindakanIgdModel BuildTindakan(
        string tindakanId = "TKI001",
        string visitId = "IGV0001",
        string regId = "REG001",
        bool isPosted = false,
        string trsBillId = "",
        string trsTdkId = "")
    {
        var audit = new AuditInfoType("U1", new DateTime(2026, 1, 1, 8, 30, 0));
        return new TindakanIgdModel(
            tindakanIgdId: tindakanId,
            igdVisitId: visitId,
            regId: regId,
            reffId: "TRF001",
            descriptions: "Injeksi Obat",
            qty: 1,
            aktifitas: ActivityTindakanIgd.Tindakan,
            ppa: PpaType.Default.ToReff(),
            audit: audit,
            trsTdkId: trsTdkId,
            trsBillId: trsBillId,
            isPosted: isPosted,
            postedDate: isPosted ? new DateTime(2026, 1, 1, 9, 0, 0) : null);
    }

    [Theory]
    [InlineData("", "U1", "Alasan void")]
    [InlineData("   ", "U1", "Alasan void")]
    [InlineData(null, "U1", "Alasan void")]
    [InlineData("TKI001", "", "Alasan void")]
    [InlineData("TKI001", "   ", "Alasan void")]
    [InlineData("TKI001", null, "Alasan void")]
    [InlineData("TKI001", "U1", "")]
    [InlineData("TKI001", "U1", "   ")]
    [InlineData("TKI001", "U1", null)]
    public async Task Handle_GivenInvalidParameters_ThrowsArgumentException(
        string? tindakanId, string? userId, string? voidReason)
    {
        var cmd = new IgdTindakanVoidCmd(tindakanId!, userId!, voidReason!, "127.0.0.1", "TestAgent");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_TindakanNotFound_ThrowsKeyNotFoundException()
    {
        _tindakanRepo
            .Setup(r => r.LoadEntity(It.IsAny<ITindakanIgdKey>()))
            .Returns(MayBe<TindakanIgdModel>.None);

        var cmd = new IgdTindakanVoidCmd("MISSING", "U1", "Salah input", "127.0.0.1", "TestAgent");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Tindakan IGD MISSING not found*");
    }

    [Fact]
    public async Task Handle_VisitNotFound_ThrowsKeyNotFoundException()
    {
        var tdk = BuildTindakan();
        _tindakanRepo
            .Setup(r => r.LoadEntity(It.IsAny<ITindakanIgdKey>()))
            .Returns(MayBe.From(tdk));
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var cmd = new IgdTindakanVoidCmd(tdk.TindakanIgdId, "U1", "Salah input", "127.0.0.1", "TestAgent");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*IgdVisit 'IGV0001' not found*");
    }

    [Fact]
    public async Task Handle_UnpostedTindakan_VoidsSuccessfully_WithoutBillingValidationOrBillingDeletion()
    {
        var tdk = BuildTindakan(isPosted: false);
        var visit = BuildVisit();

        _tindakanRepo
            .Setup(r => r.LoadEntity(It.IsAny<ITindakanIgdKey>()))
            .Returns(MayBe.From(tdk));
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        var cmd = new IgdTindakanVoidCmd(tdk.TindakanIgdId, "U1", "Batal tindakan", "127.0.0.1", "TestAgent");
        await _sut.Handle(cmd, CancellationToken.None);

        _billingLockValidator.Verify(b => b.EnsureCanModifyBillingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _trsBillingRepo.Verify(b => b.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _tindakanRepo.Verify(r => r.DeleteEntity(tdk), Times.Once);
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
    }

    [Fact]
    public async Task Handle_PostedTindakan_WhenBillingUnlocked_VoidsSuccessfully_AndCascadesDeleteToBilling()
    {
        var tdk = BuildTindakan(
            isPosted: true,
            trsBillId: "TBILL001",
            trsTdkId: "TTDK001",
            regId: "REG001");
        var visit = BuildVisit();

        _tindakanRepo
            .Setup(r => r.LoadEntity(It.IsAny<ITindakanIgdKey>()))
            .Returns(MayBe.From(tdk));
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _billingLockValidator
            .Setup(b => b.EnsureCanModifyBillingAsync("REG001", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new IgdTindakanVoidCmd(tdk.TindakanIgdId, "U1", "Koreksi tindakan posted", "127.0.0.1", "TestAgent");
        await _sut.Handle(cmd, CancellationToken.None);

        _billingLockValidator.Verify(b => b.EnsureCanModifyBillingAsync("REG001", It.IsAny<CancellationToken>()), Times.Once);
        _trsBillingRepo.Verify(b => b.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == "TBILL001")), Times.Once);
        _trsBillingRepo.Verify(b => b.DeleteEntity(It.Is<ITrsBillingKey>(k => k.TrsBillingId == "TTDK001")), Times.Once);
        _tindakanRepo.Verify(r => r.DeleteEntity(tdk), Times.Once);
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<AuditLog>()), Times.Once);
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
    }

    [Fact]
    public async Task Handle_PostedTindakan_WhenBillingLocked_ThrowsAndRejectsVoid_WithoutDeletingEntities()
    {
        var tdk = BuildTindakan(
            isPosted: true,
            trsBillId: "TBILL001",
            trsTdkId: "TTDK001",
            regId: "REG001");
        var visit = BuildVisit();

        _tindakanRepo
            .Setup(r => r.LoadEntity(It.IsAny<ITindakanIgdKey>()))
            .Returns(MayBe.From(tdk));
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _billingLockValidator
            .Setup(b => b.EnsureCanModifyBillingAsync("REG001", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tagihan sudah Final/Lunas, modifikasi ditolak."));

        var cmd = new IgdTindakanVoidCmd(tdk.TindakanIgdId, "U1", "Coba void saat billing lunas", "127.0.0.1", "TestAgent");
        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tagihan sudah Final/Lunas*");

        _trsBillingRepo.Verify(b => b.DeleteEntity(It.IsAny<ITrsBillingKey>()), Times.Never);
        _tindakanRepo.Verify(r => r.DeleteEntity(It.IsAny<ITindakanIgdKey>()), Times.Never);
        _auditRepo.Verify(r => r.SaveChanges(It.IsAny<AuditLog>()), Times.Never);
    }
}
