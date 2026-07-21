using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class JourneyAllowedActionEvaluatorTest
{
    private static JourneyAllowedAction CancelAdmission(bool canExecute = true) =>
        new(JourneyActionCode.CancelAdmission, "Batalkan admisi", canExecute, null, null);

    private static JourneyAllowedAction UpdateAdmission() =>
        new(JourneyActionCode.UpdateAdmission, "Perbarui admisi", true, null, null);

    [Fact]
    public void BillingItemsPresent_BlocksCancelAdmission_Only()
    {
        var candidates = new[] { CancelAdmission(), UpdateAdmission() };

        var allowed = JourneyAllowedActionEvaluator.Evaluate(
            candidates,
            "RG1",
            _ => true);

        var cancel = allowed.Single(a => a.Code == JourneyActionCode.CancelAdmission);
        cancel.CanExecute.Should().BeFalse();
        cancel.BlockedReason.Should().Be(JourneyAllowedActionEvaluator.RegistrationHasBillingItemsBlockedReason);
        cancel.BlockedReason.Should().Contain("Tata Rekening");

        allowed.Single(a => a.Code == JourneyActionCode.UpdateAdmission).CanExecute.Should().BeTrue();
        RegistrationCancellationBlockerCode.HasBillingItems.Should().Be("REGISTRATION_HAS_BILLING_ITEMS");
    }

    [Fact]
    public void NoBillingItems_KeepsCancelAdmissionExecutable()
    {
        var allowed = JourneyAllowedActionEvaluator.Evaluate(
            [CancelAdmission()],
            "RG1",
            _ => false);

        allowed.Single().CanExecute.Should().BeTrue();
        allowed.Single().BlockedReason.Should().BeNull();
    }

    [Fact]
    public void MissingRegId_DoesNotAdvertiseCancelAsExecutable()
    {
        var allowed = JourneyAllowedActionEvaluator.Evaluate(
            [CancelAdmission()],
            regId: null,
            hasBillingItems: _ => false);

        allowed.Single().CanExecute.Should().BeFalse();
    }

    [Fact]
    public void NoEligibilityChecker_DoesNotAdvertiseCancelAsExecutable()
    {
        var allowed = JourneyAllowedActionEvaluator.Evaluate(
            [CancelAdmission()],
            "RG1",
            (IRegistrationCancellationEligibilityRepo?)null);

        allowed.Single().CanExecute.Should().BeFalse();
    }

    [Fact]
    public void DoesNotInventUnimplementedDependencyBlockers()
    {
        var allowed = JourneyAllowedActionEvaluator.Evaluate(
            [CancelAdmission()],
            "RG1",
            _ => false);

        allowed.Single().BlockedReason.Should().BeNull();
    }
}
