using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.JualBebasFeature;

public class JualBebasModelTest
{
    [Fact]
    public void Accept_requires_regId()
    {
        var act = () => Accept(regId: "   ");
        act.Should().Throw<ArgumentException>().WithParameterName("regId");
    }

    [Fact]
    public void Accept_requires_pasienId()
    {
        var act = () => Accept(pasienId: "");
        act.Should().Throw<ArgumentException>().WithParameterName("pasienId");
    }

    [Fact]
    public void Accept_requires_pasienName()
    {
        var act = () => Accept(pasienName: "  ");
        act.Should().Throw<ArgumentException>().WithParameterName("pasienName");
    }

    [Fact]
    public void Accept_requires_acceptedBy()
    {
        var act = () => Accept(acceptedBy: "");
        act.Should().Throw<ArgumentException>().WithParameterName("acceptedBy");
    }

    [Fact]
    public void Accept_requires_at_least_one_item()
    {
        var act = () => Accept(items: []);
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Jual Bebas requires catalog-backed items.");
    }

    [Fact]
    public void Accept_creates_one_accepted_header_with_version_one()
    {
        var model = Accept();
        model.JualBebasId.Should().StartWith(JualBebasModel.IdPrefix);
        model.RequestStatus.Should().Be(JualBebasRequestStatusEnum.Accepted);
        model.Version.Should().Be(1);
        model.DeclinedBy.Should().BeEmpty();
        model.Items.Should().ContainSingle(x => x.ItemNo == 1 && x.BrgId == "BRG1" && x.Qty == 2m);
    }

    [Fact]
    public void Item_requires_positive_itemNo()
    {
        var act = () => new JualBebasItemModel(0, "BRG1", "Obat", "TAB", 1, "");
        act.Should().Throw<ArgumentException>().WithParameterName("itemNo");
    }

    [Fact]
    public void Item_requires_brgId()
    {
        var act = () => new JualBebasItemModel(1, "  ", "Obat", "TAB", 1, "");
        act.Should().Throw<ArgumentException>().WithParameterName("brgId");
    }

    [Fact]
    public void Item_requires_brgName()
    {
        var act = () => new JualBebasItemModel(1, "BRG1", "", "TAB", 1, "");
        act.Should().Throw<ArgumentException>().WithParameterName("brgName");
    }

    [Fact]
    public void Item_requires_positive_qty()
    {
        var act = () => new JualBebasItemModel(1, "BRG1", "Obat", "TAB", 0, "");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Decline_after_accept_records_actor_timestamp_and_bumps_version()
    {
        var declinedAt = new DateTime(2026, 8, 26, 10, 0, 0);
        var model = Accept();
        model.DeclineAfterAccept("ph1", declinedAt);

        model.RequestStatus.Should().Be(JualBebasRequestStatusEnum.DeclinedAfterAccept);
        model.DeclinedBy.Should().Be("ph1");
        model.DeclinedAt.Should().Be(declinedAt);
        model.Version.Should().Be(2);
    }

    [Fact]
    public void Decline_requires_actor()
    {
        var model = Accept();
        var act = () => model.DeclineAfterAccept(" ", DateTime.Now);
        act.Should().Throw<ArgumentException>().WithParameterName("actorId");
    }

    [Fact]
    public void Decline_rejected_from_converted_state()
    {
        var model = Accept();
        model.MarkConvertedToSalesOrder();
        var act = () => model.DeclineAfterAccept("ph1", DateTime.Now);
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Only an accepted Jual Bebas can be cancelled after accept.");
    }

    [Fact]
    public void Second_decline_rejected()
    {
        var model = Accept();
        model.DeclineAfterAccept("ph1", DateTime.Now);
        var act = () => model.DeclineAfterAccept("ph2", DateTime.Now);
        act.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void Conversion_rejected_from_declined_state()
    {
        var model = Accept();
        model.DeclineAfterAccept("ph1", DateTime.Now);
        var act = () => model.MarkConvertedToSalesOrder();
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Only an accepted Jual Bebas can convert to a Sales Order.");
    }

    [Fact]
    public void Conversion_bumps_version()
    {
        var model = Accept();
        model.MarkConvertedToSalesOrder();
        model.RequestStatus.Should().Be(JualBebasRequestStatusEnum.ConvertedToSalesOrder);
        model.Version.Should().Be(2);
    }

    [Fact]
    public void Stale_expected_version_conflicts()
    {
        var model = Accept();
        var act = () => model.AssertExpectedVersion(99);
        act.Should().Throw<ApotekConcurrencyException>()
            .Where(ex => ex.AggregateId == model.JualBebasId && ex.ExpectedVersion == 99);
    }

    private static JualBebasModel Accept(
        string regId = "R1",
        string pasienId = "P1",
        string pasienName = "Pasien",
        string acceptedBy = "u",
        IEnumerable<JualBebasItemModel>? items = null)
        => JualBebasModel.Accept(regId, pasienId, pasienName, acceptedBy, DateTime.Now,
            items ?? [new JualBebasItemModel(1, "BRG1", "Obat", "TAB", 2, "3x1")]);
}
