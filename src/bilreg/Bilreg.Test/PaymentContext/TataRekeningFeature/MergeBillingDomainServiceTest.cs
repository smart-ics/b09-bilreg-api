using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

/// <summary>
/// P2-S02 completion criterion 1: patient identity integrity guard in
/// <see cref="MergeBillingDomainService"/> rejects merges when the medical record
/// number (NoRM) differs, throwing <see cref="DomainViolationException"/>.
/// </summary>
public class MergeBillingDomainServicePatientIdentityTest
{
    private static readonly IMergeBillingDomainService Sut = TataRekeningDomainTestHelper.MergeBillingService;

    [Fact]
    public void ValidatePatientIdentity_WhenNoRmMatches_ShouldNotThrow()
    {
        var target = AutoMergeTestData.RegWithNoRm("REG-RANAP-1", "RM-001", JenisRegEnum.RegInap);
        var source = AutoMergeTestData.RegWithNoRm("REG-IGD-1", "RM-001", JenisRegEnum.Darurat);

        var act = () => Sut.ValidatePatientIdentity(source, target);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidatePatientIdentity_WhenNoRmDiffers_ShouldThrowDomainViolationException()
    {
        var target = AutoMergeTestData.RegWithNoRm("REG-RANAP-1", "RM-TARGET", JenisRegEnum.RegInap);
        var source = AutoMergeTestData.RegWithNoRm("REG-IGD-1", "RM-SOURCE", JenisRegEnum.Darurat);

        var act = () => Sut.ValidatePatientIdentity(source, target);

        act.Should().Throw<DomainViolationException>()
            .WithMessage("*Inkonsistensi integritas pasien*")
            .WithMessage("*RM-TARGET*")
            .WithMessage("*RM-SOURCE*")
            .WithMessage("*Auto-Merge dibatalkan*");
    }

    [Fact]
    public void ValidatePatientIdentity_WhenNoRmWhitespace_ShouldThrowDomainViolationException()
    {
        var act = () => Sut.ValidatePatientIdentity("   ", "RM-001");

        act.Should().Throw<DomainViolationException>();
    }

    [Fact]
    public void ValidateAutoMergeIntegrity_WhenNoRmDiffers_ShouldThrowDomainViolationException()
    {
        var target = AutoMergeTestData.RegWithNoRm("REG-RANAP-1", "RM-TARGET", JenisRegEnum.RegInap);
        var source = AutoMergeTestData.RegWithNoRm("REG-IGD-1", "RM-SOURCE", JenisRegEnum.Darurat);

        var act = () => Sut.ValidateAutoMergeIntegrity(source, target);

        act.Should().Throw<DomainViolationException>()
            .WithMessage("*Inkonsistensi integritas pasien*");
    }

    [Fact]
    public void ValidateAutoMergeIntegrity_WhenNoRmMatchesAndStatusesValid_ShouldNotThrow()
    {
        var target = AutoMergeTestData.RegWithNoRm("REG-RANAP-1", "RM-001", JenisRegEnum.RegInap);
        var source = AutoMergeTestData.RegWithNoRm("REG-IGD-1", "RM-001", JenisRegEnum.Darurat);

        var act = () => Sut.ValidateAutoMergeIntegrity(
            source, target, AutoMergeTestData.OpenedTrk("REG-IGD-1"), AutoMergeTestData.OpenedTrk("REG-RANAP-1"));

        act.Should().NotThrow();
    }

    [Fact]
    public void ExecuteWithRegs_WhenNoRmDiffers_ShouldThrowDomainViolationExceptionBeforeMutating()
    {
        var target = AutoMergeTestData.RegWithNoRm("REG-RANAP-1", "RM-TARGET", JenisRegEnum.RegInap);
        var source = AutoMergeTestData.RegWithNoRm("REG-IGD-1", "RM-SOURCE", JenisRegEnum.Darurat);
        var mergeRequest = MergeRequestModel.Create("MR-P2S02", "REG-IGD-1", "REG-RANAP-1");

        var act = () => Sut.Execute(
            mergeRequest,
            AutoMergeTestData.OpenedTrk("REG-IGD-1"),
            AutoMergeTestData.OpenedTrk("REG-RANAP-1"),
            source,
            target);

        act.Should().Throw<DomainViolationException>()
            .WithMessage("*Inkonsistensi integritas pasien*");
        mergeRequest.Status.Should().Be(MergeRequestStatusEnum.Pending,
            "the merge request must not be executed when the identity guard fails");
    }
}
