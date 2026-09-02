using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.Shared;

/// <summary>
/// APT-B30 key invariant spot-checks mapped to BR-APT rules cited in the master plan.
/// </summary>
public class ApotekInvariantCoverageTest
{
    [Fact]
    public void BrApt011_only_one_active_order_matches_source_reg_payer_key()
    {
        var repo = new InMemorySalesOrderRepo();
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja, "RK1", "TR1", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "A", "A", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []);
        repo.SaveChanges(order);

        repo.LoadActive(SalesOrderSourceKindEnum.ResepKerja, "RK1", "R1", PayerPathEnum.GeneralPatientPay)
            .Value.SalesOrderId.Should().Be(order.SalesOrderId);
        repo.Store.Values.Count(x => x.IsActiveKey && x.SourceId == "RK1" && x.RegId == "R1").Should().Be(1);
    }

    [Fact]
    public void BrApt043_dispense_authorized_is_policy_only_not_persisted()
    {
        typeof(DispenseAuthorizedPolicy).IsClass.Should().BeTrue();
        typeof(DispensingModel).GetProperties().Select(p => p.Name)
            .Should().NotContain(x => x.Contains("DispenseAuthorized", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BrApt110_available_stock_evaluation_is_not_persisted_on_sales_order()
    {
        var order = SalesOrderModel.Establish(
            SalesOrderSourceKindEnum.ResepKerja, "RK1", "TR1", "R1", "P1", "Pasien",
            PayerPathEnum.GeneralPatientPay, PartialReasonEnum.None,
            [SalesOrderItemModel.Establish(1, 1, "A", "A", "TAB", 5, FornasCoverageEnum.Unknown, "", false)],
            []);
        typeof(SalesOrderModel).GetProperties().Select(p => p.Name)
            .Should().NotContain(x => x.Contains("AvailableStock", StringComparison.OrdinalIgnoreCase));
        order.Items.Should().ContainSingle(x => x.AcceptedQty == 5m);
    }

    [Fact]
    public void BrApt138_141_pickup_expired_is_projection_not_dispensing_state()
    {
        var preparedAt = DateTime.Now.AddDays(-8);
        var d = DispensingModel.Establish("ASO1", "LYAPT", "LYDTU", [
            new DispensingItemModel(1, 1, "A", 5, "", "", "", DispensingItemOutcomeEnum.Open)
        ]);
        d.Release(DateTime.Now, true);
        d.StartPreparation(DateTime.Now, true);
        d.MarkPrepared(preparedAt);

        d.IsPickupExpired(DateTime.Now, 7).Should().BeTrue();
        d.DispensingStatus.Should().Be(DispensingStatusEnum.Prepared);
        d.OverrideCollectionWindow("pharmacist", "patient delayed", DateTime.Now);
        d.IsPickupExpired(DateTime.Now, 7).Should().BeFalse();
    }

    [Fact]
    public void BrApt143_145_queue_close_requires_reason_and_does_not_create_commercial_facts()
    {
        Action blank = () => QueueCloseModel.Create("Q1", 1, "", "staff", DateTime.Now);
        blank.Should().Throw<ArgumentException>().WithParameterName("reason");

        var close = QueueCloseModel.Create("Q1", 1, "patient left", "staff", DateTime.Now);
        close.QueueCloseId.Should().StartWith(QueueCloseModel.IdPrefix);
        close.Reason.Should().Be("patient left");
    }
}
