using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.DispensingFeature;

public class DispensingModelTest
{
    [Fact]
    public void Prepared_does_not_handover_without_pickup_education_and_pass_review()
    {
        var d = DispensingModel.Establish("ASO1", "LYAPT", "LYDTU", [
            new DispensingItemModel(1, 1, "A", 2, "", "", "", DispensingItemOutcomeEnum.Open)
        ]);
        d.Release(DateTime.Now, true);
        d.StartPreparation(DateTime.Now, true).Should().BeTrue();
        d.StartPreparation(DateTime.Now, true).Should().BeFalse();
        d.MarkPrepared(DateTime.Now);
        var handover = () => d.Handover(DateTime.Now, "", "", false, false);
        handover.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void Failed_review_returns_to_preparing_and_keeps_attempts()
    {
        var d = Prepared();
        d.AppendFinalReview(FinalReviewOutcomeEnum.Fail, "wrong strength", "pharm", DateTime.Now);
        d.DispensingStatus.Should().Be(DispensingStatusEnum.Preparing);
        d.MarkPrepared(DateTime.Now);
        d.AppendFinalReview(FinalReviewOutcomeEnum.Pass, "", "pharm", DateTime.Now);
        d.Reviews.Should().HaveCount(2);
        d.Reviews[0].Outcome.Should().Be(FinalReviewOutcomeEnum.Fail);
    }

    [Fact]
    public void Pickup_expired_blocks_ordinary_handover()
    {
        var d = Prepared();
        d.RecordPickupCall(DateTime.Now);
        d.RecordEducation("pharm", DateTime.Now, "");
        d.AppendFinalReview(FinalReviewOutcomeEnum.Pass, "", "pharm", DateTime.Now);
        d.IsPickupExpired(DateTime.Now.AddDays(8), 7).Should().BeTrue();
        var act = () => d.Handover(DateTime.Now, "", "", true, false);
        act.Should().Throw<ApotekDomainException>();
        d.OverrideCollectionWindow("pharm", "patient delayed", DateTime.Now);
        d.Handover(DateTime.Now, "0812", "spouse", true, true);
        d.DispensingStatus.Should().Be(DispensingStatusEnum.Completed);
    }

    [Fact]
    public void Invoice_issue_is_purchase_confirmation_and_bpjs_timing_is_independent()
    {
        var invoice = InvoiceModel.Establish("ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "A", "A", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)], [], 0, 0, 0);
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.Established);
        invoice.Issue(DateTime.Now);
        invoice.InvoiceStatus.Should().Be(InvoiceStatusEnum.Issued);
        var deny = () => invoice.RewriteContent(invoice.Items, invoice.Charges, 0, 0, 0, tataRekeningAllows: false);
        deny.Should().Throw<ApotekDomainException>();
    }

    private static DispensingModel Prepared()
    {
        var d = DispensingModel.Establish("ASO1", "LYAPT", "LYDTU", [
            new DispensingItemModel(1, 1, "A", 2, "", "", "", DispensingItemOutcomeEnum.Open)
        ]);
        d.Release(DateTime.Now, true);
        d.StartPreparation(DateTime.Now, true);
        d.MarkPrepared(DateTime.Now);
        return d;
    }
}
