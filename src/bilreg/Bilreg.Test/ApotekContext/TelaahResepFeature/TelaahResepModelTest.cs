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
}
