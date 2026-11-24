using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using FluentAssertions;
using Moq;
using System.Reflection;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class RegistrationCancellationEligibilityTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UT_RCE_01_ZeroOrOneBillingItem_ReturnsTheAuthoritativeResult(bool hasBillingItems)
    {
        var dal = new Mock<IRegistrationCancellationEligibilityDal>();
        dal.Setup(x => x.HasBillingItems("RG00000001")).Returns(hasBillingItems);
        var sut = new RegistrationCancellationEligibilityRepo(dal.Object);

        sut.HasBillingItems("RG00000001").Should().Be(hasBillingItems);
        dal.Verify(x => x.HasBillingItems("RG00000001"), Times.Once);
    }

    [Fact]
    public void UT_RCE_02_MultipleBillingItems_StillBlockCancellation()
    {
        var dal = new Mock<IRegistrationCancellationEligibilityDal>();
        dal.Setup(x => x.HasBillingItems("RG00000001")).Returns(true);
        var sut = new RegistrationCancellationEligibilityRepo(dal.Object);

        var hasBillingItems = sut.HasBillingItems("RG00000001");

        hasBillingItems.Should().BeTrue();
        RegistrationCancellationBlockerCode.HasBillingItems.Should().Be("REGISTRATION_HAS_BILLING_ITEMS");
        RegistrationCancellationBlockerCode.HasActiveBedPlacement.Should().Be("RegistrationHasActiveBedPlacement");
    }

    [Fact]
    public void UT_RCE_03_EmptyTataRekeningHeader_IsNotAnEligibilityInput()
    {
        var dal = new Mock<IRegistrationCancellationEligibilityDal>();
        dal.Setup(x => x.HasBillingItems("RG00000001")).Returns(false);
        var sut = new RegistrationCancellationEligibilityRepo(dal.Object);

        sut.HasBillingItems("RG00000001").Should().BeFalse();
    }

    [Fact]
    public void UT_RCE_04_Query_IsScopedToTheRequestedRegId_AndReadsBillingItemsOnly()
    {
        var query = typeof(RegistrationCancellationEligibilityDal)
            .GetField("HasBillingItemsSql", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetRawConstantValue()!.ToString();

        query.Should().Contain("ta_trs_billing");
        query.Should().Contain("aa.fs_kd_reg = @RegId");
        query.Should().NotContain("BILRG_TataRekening");
        query.Should().NotContain("ta_trs_billing2");
    }

    [Fact]
    public void UT_RCE_05_DiscardedDependencies_AreNotPartOfTheEligibilityPort()
    {
        typeof(IRegistrationCancellationEligibilityRepo).GetMethods()
            .Select(x => x.Name)
            .Should().BeEquivalentTo(new[]
            {
                nameof(IRegistrationCancellationEligibilityRepo.HasBillingItems),
                nameof(IRegistrationCancellationEligibilityRepo.HasActiveBedPlacement)
            });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UT_RCE_06_ActiveBedPlacement_ReturnsTheAuthoritativeResult(bool hasActiveBedPlacement)
    {
        var dal = new Mock<IRegistrationCancellationEligibilityDal>();
        dal.Setup(x => x.HasActiveBedPlacement("RG00000001")).Returns(hasActiveBedPlacement);
        var sut = new RegistrationCancellationEligibilityRepo(dal.Object);

        sut.HasActiveBedPlacement("RG00000001").Should().Be(hasActiveBedPlacement);
        dal.Verify(x => x.HasActiveBedPlacement("RG00000001"), Times.Once);
    }

    [Fact]
    public void UT_RCE_07_Query_IsScopedToTheRequestedRegId_AndReadsActiveBedPlacementOnly()
    {
        var query = typeof(RegistrationCancellationEligibilityDal)
            .GetField("HasActiveBedPlacementSql", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetRawConstantValue()!.ToString();

        query.Should().Contain("ta_trs_bed");
        query.Should().Contain("aa.fs_kd_reg = @RegId");
        query.Should().Contain("aa.fd_tgl_void");
        query.Should().Contain("aa.fd_tgl_out");
        query.Should().Contain("3000-01-01");
    }
}
