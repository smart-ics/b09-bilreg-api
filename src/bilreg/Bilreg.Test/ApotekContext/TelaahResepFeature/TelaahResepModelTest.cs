using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature;

public class TelaahResepModelTest
{
    [Fact]
    public void Completes_approved_partial_and_rejected_from_explicit_dispositions()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [
            TelaahResepItemModel.Pending(1, 1, "A", "A", 10),
            TelaahResepItemModel.Pending(2, 2, "B", "B", 5)
        ]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateItem(telaah.Items[1].WithDisposition(TelaahDispositionEnum.Rejected, "B", "B", 0, "allergy", "pharm"));
        telaah.Complete(DateTime.Now);
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.PartiallyApproved);
        telaah.AcceptedItems().Should().HaveCount(1);
        telaah.CanEstablishSalesOrder.Should().BeTrue();
    }

    [Fact]
    public void Rejected_review_cannot_establish_sales_order()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(TelaahDispositionEnum.Rejected, "A", "A", 0, "unsafe", "pharm"));
        telaah.Complete(DateTime.Now);
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Rejected);
        telaah.CanEstablishSalesOrder.Should().BeFalse();
    }

    [Fact]
    public void Substitute_requires_reason()
    {
        var pending = TelaahResepItemModel.Pending(1, 1, "A", "A", 10);
        var act = () => pending.WithDisposition(TelaahDispositionEnum.AcceptedSubstitute, "B", "B", 10, "", "pharm");
        act.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void Reject_requires_reason()
    {
        var pending = TelaahResepItemModel.Pending(1, 1, "A", "A", 10);
        var act = () => pending.WithDisposition(TelaahDispositionEnum.Rejected, "A", "A", 0, "  ", "pharm");
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Substitute and reject dispositions require a reason.");
    }

    [Fact]
    public void Complete_rejected_when_any_line_still_pending()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [
            TelaahResepItemModel.Pending(1, 1, "A", "A", 10),
            TelaahResepItemModel.Pending(2, 2, "B", "B", 5)
        ]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));

        var act = () => telaah.Complete(DateTime.Now);

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Every line must have an explicit disposition before completion.");
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.UnderReview);
    }

    [Fact]
    public void UpdateItem_rejected_after_terminal_complete()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [TelaahResepItemModel.Pending(1, 1, "A", "A", 10)]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.Complete(DateTime.Now);

        var act = () => telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.Rejected, "A", "A", 0, "late", "pharm"));

        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Telaah items can change only while Under Review.");
        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
    }

    [Fact]
    public void Completes_fully_approved_when_all_lines_accepted()
    {
        var telaah = TelaahResepModel.Open("ARX1", "R1", [
            TelaahResepItemModel.Pending(1, 1, "A", "A", 10),
            TelaahResepItemModel.Pending(2, 2, "B", "B", 5)
        ]);
        telaah.Start("pharm", DateTime.Now);
        telaah.UpdateItem(telaah.Items[0].WithDisposition(
            TelaahDispositionEnum.AcceptedAsPrescribed, "A", "A", 10, "", "pharm"));
        telaah.UpdateItem(telaah.Items[1].WithDisposition(
            TelaahDispositionEnum.AcceptedSubstitute, "C", "C", 5, "stock alt", "pharm"));
        telaah.Complete(DateTime.Now);

        telaah.TelaahStatus.Should().Be(TelaahStatusEnum.Approved);
        telaah.AcceptedItems().Should().HaveCount(2);
        telaah.CanEstablishSalesOrder.Should().BeTrue();
    }
}
