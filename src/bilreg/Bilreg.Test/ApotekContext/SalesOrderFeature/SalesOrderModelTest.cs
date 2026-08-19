using Bilreg.Application.ApotekContext.StockPlanningFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.SalesOrderFeature;

public class SalesOrderModelTest
{
    [Fact]
    public void Quantities_cannot_exceed_accepted_qty()
    {
        var so = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.JualBebas, "ADQ1", "", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "A", "A", "TAB", 10, FornasCoverageEnum.Unknown, "", false)],
            []);
        so.ApplyInvoiceQty(1, 10);
        var over = () => so.ApplyDispenseQty(1, 11);
        over.Should().Throw<ApotekDomainException>();
        so.AppendUnfulfilled(1, 4, UnfulfilledReasonEnum.StockShortageAfterEstablishment, "", "u", DateTime.Now);
        so.Item(1).UnfulfilledQty.Should().Be(4);
        so.Outcomes.Should().HaveCount(1);
    }

    [Fact]
    public void Prescription_path_requires_telaah()
    {
        var act = () => SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja, "ARX1", "", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "A", "A", "TAB", 1, FornasCoverageEnum.Unknown, "", false)],
            []);
        act.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void Fail_closed_available_stock_does_not_evaluate()
    {
        var port = new FailClosedAvailableStockPort();
        var result = port.Evaluate([new AvailableStockRequest("A", "LYAPT", 1)]);
        result.Evaluated.Should().BeFalse();
        result.ErrorCode.Should().Be("PD09_AVAILABLE_STOCK_NOT_CONFIGURED");
    }
}
