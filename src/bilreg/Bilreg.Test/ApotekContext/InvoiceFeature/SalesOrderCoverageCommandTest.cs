using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.InvoiceFeature;

public class SalesOrderCoverageCommandTest
{
    private readonly InMemorySalesOrderRepo _so = new();
    private readonly AllowAllAuth _auth = new();

    [Fact]
    public async Task Apply_coverage_rejects_fornas_only_without_sep()
    {
        var order = SeedBpjsOrder();
        var port = new FailClosedSepFornasPort();

        var act = () => new SalesOrderApplyCoverageHandler(_so, port, _auth)
            .Handle(new SalesOrderApplyCoverageCmd("u", order.SalesOrderId, order.Version, "", [
                new SalesOrderCoverageItem(1, FornasCoverageEnum.Covered)
            ]), default);

        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Fornas master membership alone is not Coverage Clearance*");
        _so.Store[order.SalesOrderId].Item(1).SepNo.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_coverage_snapshots_sep_and_item_coverage()
    {
        var order = SeedBpjsOrder();
        var port = new RecordingSepPort();

        await new SalesOrderApplyCoverageHandler(_so, port, _auth)
            .Handle(new SalesOrderApplyCoverageCmd("u", order.SalesOrderId, order.Version, "SEP-APT-B19", [
                new SalesOrderCoverageItem(1, FornasCoverageEnum.Covered)
            ]), default);

        var stored = _so.Store[order.SalesOrderId];
        stored.Item(1).SepNo.Should().Be("SEP-APT-B19");
        stored.Item(1).FornasCoverage.Should().Be(FornasCoverageEnum.Covered);
        port.Evaluated.Should().Contain(("R1", "BRG1"));
    }

    private SalesOrderModel SeedBpjsOrder()
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ-B19", "", "R1", "P1", "Pasien",
            PayerPathEnum.Bpjs, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "BRG1", "Obat", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []);
        _so.SaveChanges(order);
        return order;
    }

    private sealed class RecordingSepPort : ISepFornasPort
    {
        public List<(string RegId, string BrgId)> Evaluated { get; } = [];

        public SepFornasEvidence Evaluate(string regId, string brgId)
        {
            Evaluated.Add((regId, brgId));
            return new SepFornasEvidence("", FornasCoverageEnum.Covered);
        }
    }
}
