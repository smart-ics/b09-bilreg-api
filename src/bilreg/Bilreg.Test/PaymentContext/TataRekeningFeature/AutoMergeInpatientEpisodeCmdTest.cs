using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

/// <summary>
/// P2-S02 completion criteria 2 &amp; 3: the <see cref="AutoMergeInpatientEpisodeCmdHandler"/>
/// validates the existence of both registrations, enforces the patient identity guard,
/// and records the relation (<c>fs_kd_reg_merge</c> = inpatient, <c>fs_kd_reg_asal</c> = preceding).
/// </summary>
public class AutoMergeInpatientEpisodeCmdTest
{
    private const string InpatientRegId = "REG-RANAP-001";
    private const string PrecedingRegId = "REG-IGD-001";

    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<IMergeBillingRepo> _mergeBillingRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();
    private readonly Mock<IAuditRepo> _auditRepo = new();

    public AutoMergeInpatientEpisodeCmdTest()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);
    }

    private AutoMergeInpatientEpisodeCmdHandler CreateHandler() =>
        new(
            _regRepo.Object,
            _tataRekeningRepo.Object,
            _mergeBillingRepo.Object,
            TataRekeningDomainTestHelper.MergeBillingService,
            _unitOfWork.Object,
            TestTglJamProvider.Instance,
            _auditRepo.Object);

    private void SetupReg(string regId, string noRm, JenisRegEnum jenisReg)
    {
        var reg = AutoMergeTestData.RegWithNoRm(regId, noRm, jenisReg);
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(reg));
    }

    private void SetupTrk(string regId)
    {
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == regId)))
            .Returns(MayBe.From(AutoMergeTestData.OpenedTrk(regId)));
    }

    [Fact]
    public async Task Handle_WhenSameRegistration_ShouldReject()
    {
        var act = async () => await CreateHandler()
            .Handle(new AutoMergeInpatientEpisodeCmd(InpatientRegId, InpatientRegId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak boleh sama*");
    }

    [Fact]
    public async Task Handle_WhenInpatientRegMissing_ShouldReject()
    {
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == InpatientRegId)))
            .Returns(MayBe<RegModel>.None);
        SetupReg(PrecedingRegId, "RM-001", JenisRegEnum.Darurat);

        var act = async () => await CreateHandler()
            .Handle(new AutoMergeInpatientEpisodeCmd(InpatientRegId, PrecedingRegId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"*Registrasi rawat inap '{InpatientRegId}' tidak ditemukan*");
        _mergeBillingRepo.Verify(r => r.SaveMergeRelation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPrecedingRegMissing_ShouldReject()
    {
        SetupReg(InpatientRegId, "RM-001", JenisRegEnum.RegInap);
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == PrecedingRegId)))
            .Returns(MayBe<RegModel>.None);

        var act = async () => await CreateHandler()
            .Handle(new AutoMergeInpatientEpisodeCmd(InpatientRegId, PrecedingRegId), CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage($"*Registrasi asal '{PrecedingRegId}' tidak ditemukan*");
        _mergeBillingRepo.Verify(r => r.SaveMergeRelation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoRmDiffers_ShouldThrowDomainViolationAndNotPersist()
    {
        SetupReg(InpatientRegId, "RM-TARGET", JenisRegEnum.RegInap);
        SetupReg(PrecedingRegId, "RM-SOURCE", JenisRegEnum.Darurat);
        SetupTrk(InpatientRegId);
        SetupTrk(PrecedingRegId);

        var act = async () => await CreateHandler()
            .Handle(new AutoMergeInpatientEpisodeCmd(InpatientRegId, PrecedingRegId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainViolationException>()
            .WithMessage("*Inkonsistensi integritas pasien*");
        _mergeBillingRepo.Verify(r => r.SaveMergeRelation(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoRmIdentical_ShouldRecordRelationToTrsMerge()
    {
        SetupReg(InpatientRegId, "RM-001", JenisRegEnum.RegInap);
        SetupReg(PrecedingRegId, "RM-001", JenisRegEnum.Darurat);
        SetupTrk(InpatientRegId);
        SetupTrk(PrecedingRegId);

        var result = await CreateHandler()
            .Handle(new AutoMergeInpatientEpisodeCmd(InpatientRegId, PrecedingRegId, "USER-01"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.InpatientRegId.Should().Be(InpatientRegId);
        result.PrecedingRegId.Should().Be(PrecedingRegId);
        result.NoRM.Should().Be("RM-001");

        // Merge target = inpatient main registration; merge source = preceding registration.
        _mergeBillingRepo.Verify(
            r => r.SaveMergeRelation(InpatientRegId, PrecedingRegId), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
        _auditRepo.Verify(a => a.SaveChanges(It.IsAny<Bilreg.Domain.Shared.AuditLogFeature.AuditLog>()), Times.Once);
    }
}
