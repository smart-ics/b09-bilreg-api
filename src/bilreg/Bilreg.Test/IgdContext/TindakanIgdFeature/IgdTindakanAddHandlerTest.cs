using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.TindakanIgdFeature;

public class IgdTindakanAddHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<ITindakanIgdRepo> _tindakanRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IIgdBillingPostingOrchestrator> _billingOrchestrator = new();

    private const string IgdVisitId = "IGV-2026-0001";
    private const string RegId = "REG-001";
    private const string PetugasMedisId = "DK1";
    private const string UserId = "USER-007";

    private IgdTindakanAddHandler CreateSut()
    {
        return new IgdTindakanAddHandler(
            _visitRepo.Object,
            _tindakanRepo.Object,
            _ppaRepo.Object,
            TestTglJamProvider.Instance,
            _regRepo.Object,
            _billingOrchestrator.Object);
    }

    private static PpaType TestDokter()
    {
        var smf = SmfType.Default;
        var groupSpesialis = GroupSpesialisType.Default;
        var satTugas = new SatTugasType("ST1", "Dokter", ProfesiType.Dokter);
        var listSatTugas = new[] { new PpaSatTugasType(satTugas, true) };
        return new PpaType(PetugasMedisId, "Dr. Tes", "Tes", smf, groupSpesialis,
            listLayanan: [], listSatTugas: listSatTugas, listContact: []);
    }

    private static RegModel CreateTestReg(string regId = RegId)
    {
        var regDate = new DateOnly(2026, 9, 28);
        return new RegModel(
            regId,
            regDate,
            new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.Darurat,
            PasienModel.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.TipeJaminanType.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.PolisModel.Default.ToReff(),
            Domain.BedUsageContext.WardFeature.KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            Domain.AdmisiContext.RujukanFeature.RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff("1GD01", "IGD"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    private static IgdVisitModel CreateTestVisit(string igdVisitId = IgdVisitId, bool withReg = false)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        var visit = new IgdVisitModel(
            igdVisitId: igdVisitId,
            daftarDateTime: audit.Timestamp,
            visitor: visitor,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Daftar,
            reg: RegModel.Default.ToReff(),
            redirection: RedirectionType.Default,
            bedId: "-",
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: []);
        if (withReg)
        {
            visit.AssignRegister(CreateTestReg(), audit);
        }
        return visit;
    }

    [Fact]
    public async Task Handle_PreRegistration_SavesTindakanAsBuffer_WithIsPostedFalse_AndDoesNotPostToBilling()
    {
        // Arrange - visit without registration (pra-registrasi)
        var visit = CreateTestVisit(withReg: false);
        visit.HasReg.Should().BeFalse();

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));
        _ppaRepo
            .Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(TestDokter()));

        var cmd = new IgdTindakanAddCmd(
            IgdVisitId, PetugasMedisId, "TDK001", "Injeksi Obat", 1, 0, UserId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TindakanIgdId.Should().StartWith("TKI");
        result.IgdVisitId.Should().Be(IgdVisitId);

        _tindakanRepo.Verify(r => r.SaveChanges(It.Is<TindakanIgdModel>(t =>
            t.RegId == "-" &&
            !t.IsPosted &&
            t.ReffId == "TDK001")), Times.Once);
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
        _billingOrchestrator.Verify(o => o.PostTindakanAsync(
            It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<TindakanIgdModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PostRegistration_SavesTindakan_AndDirectlyPostsToBilling()
    {
        // Arrange - visit with registration (pasca-registrasi)
        var visit = CreateTestVisit(withReg: true);
        var reg = CreateTestReg();
        visit.HasReg.Should().BeTrue();

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));
        _ppaRepo
            .Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(TestDokter()));

        _billingOrchestrator
            .Setup(o => o.PostTindakanAsync(visit, reg, It.IsAny<TindakanIgdModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cmd = new IgdTindakanAddCmd(
            IgdVisitId, PetugasMedisId, "TDK001", "Injeksi Obat", 1, 0, UserId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _tindakanRepo.Verify(r => r.SaveChanges(It.Is<TindakanIgdModel>(t =>
            t.RegId == RegId &&
            t.ReffId == "TDK001")), Times.Once);
        _billingOrchestrator.Verify(o => o.PostTindakanAsync(
            visit, reg, It.Is<TindakanIgdModel>(t => t.ReffId == "TDK001"), It.IsAny<CancellationToken>()), Times.Once);
        visit.HasPendingBilling.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_PostRegistration_WhenBillingPostingFails_SetsHasPendingBillingTrue()
    {
        // Arrange - visit with registration, but posting fails
        var visit = CreateTestVisit(withReg: true);
        var reg = CreateTestReg();

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));
        _ppaRepo
            .Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(TestDokter()));

        _billingOrchestrator
            .Setup(o => o.PostTindakanAsync(It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<TindakanIgdModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var cmd = new IgdTindakanAddCmd(
            IgdVisitId, PetugasMedisId, "TDK001", "Injeksi Obat", 1, 0, UserId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        visit.HasPendingBilling.Should().BeTrue();
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v => v.HasPendingBilling)), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_PostRegistration_WhenBillingPostingThrows_CatchesAndSetsHasPendingBillingTrue()
    {
        // Arrange - visit with registration, posting throws
        var visit = CreateTestVisit(withReg: true);
        var reg = CreateTestReg();

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(reg));
        _ppaRepo
            .Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe.From(TestDokter()));

        _billingOrchestrator
            .Setup(o => o.PostTindakanAsync(It.IsAny<IgdVisitModel>(), It.IsAny<RegModel>(), It.IsAny<TindakanIgdModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        var cmd = new IgdTindakanAddCmd(
            IgdVisitId, PetugasMedisId, "TDK001", "Injeksi Obat", 1, 0, UserId);
        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert - does not throw to caller, sets pending billing
        await act.Should().NotThrowAsync();
        visit.HasPendingBilling.Should().BeTrue();
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v => v.HasPendingBilling)), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenVisitNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var cmd = new IgdTindakanAddCmd(
            IgdVisitId, PetugasMedisId, "TDK001", "Injeksi Obat", 1, 0, UserId);
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{IgdVisitId}*");
    }

    [Fact]
    public async Task Handle_WhenPpaNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var visit = CreateTestVisit();
        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));
        _ppaRepo
            .Setup(r => r.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe<PpaType>.None);

        var cmd = new IgdTindakanAddCmd(
            IgdVisitId, PetugasMedisId, "TDK001", "Injeksi Obat", 1, 0, UserId);
        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{PetugasMedisId}*");
    }

    [Theory]
    [InlineData("", "DK1", "TDK001", "Desc", 1, "U1")]
    [InlineData("   ", "DK1", "TDK001", "Desc", 1, "U1")]
    [InlineData("IGV-1", "", "TDK001", "Desc", 1, "U1")]
    [InlineData("IGV-1", "   ", "TDK001", "Desc", 1, "U1")]
    [InlineData("IGV-1", "DK1", "", "Desc", 1, "U1")]
    [InlineData("IGV-1", "DK1", "   ", "Desc", 1, "U1")]
    [InlineData("IGV-1", "DK1", "TDK001", "", 1, "U1")]
    [InlineData("IGV-1", "DK1", "TDK001", "   ", 1, "U1")]
    [InlineData("IGV-1", "DK1", "TDK001", "Desc", 1, "")]
    [InlineData("IGV-1", "DK1", "TDK001", "Desc", 1, "   ")]
    [InlineData("IGV-1", "DK1", "TDK001", "Desc", 0, "U1")]
    [InlineData("IGV-1", "DK1", "TDK001", "Desc", -1, "U1")]
    public async Task Handle_InvalidParameters_ThrowsArgumentException(
        string visitId, string ppaId, string reffId, string desc, int qty, string userId)
    {
        var cmd = new IgdTindakanAddCmd(visitId, ppaId, reffId, desc, qty, 0, userId);
        var sut = CreateSut();

        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
