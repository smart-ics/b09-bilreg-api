using System.Text.Json;
using Bilreg.Application.ApotekContext.IntegrationFeature.Handlers;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class BillingChargeHandlerTest
{
    private readonly InMemoryInvoiceRepo _invoice = new();
    private readonly FakeTataRekeningChargePort _charge = new();

    [Fact]
    public void First_delivery_issues_charge_and_records_correlation()
    {
        var invoice = SeedIssuedInvoice();
        var task = BillingTask(invoice.InvoiceId);

        var result = CreateHandler().Handle(task);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("TR-CHG-1");
        _charge.IssueCount.Should().Be(1);
        _invoice.Store[invoice.InvoiceId].TataRekeningChargeId.Should().Be("TR-CHG-1");
    }

    [Fact]
    public void Duplicate_delivery_returns_existing_correlation_without_reissuing()
    {
        var invoice = SeedIssuedInvoice();
        invoice.RecordChargeCorrelation("TR-CHG-EXISTING");
        _invoice.SaveChanges(invoice);
        var task = BillingTask(invoice.InvoiceId);

        var result = CreateHandler().Handle(task);

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("TR-CHG-EXISTING");
        _charge.IssueCount.Should().Be(0);
    }

    private BillingChargeHandler CreateHandler() => new(_charge, _invoice);

    private InvoiceModel SeedIssuedInvoice()
    {
        var invoice = InvoiceModel.Establish(
            "ASO1", PayerPathEnum.GeneralPatientPay, DateTime.Now, "UMUM", "Umum", DateTime.Now,
            [new InvoiceItemModel(1, 1, "BRG1", "Obat", InvoiceItemKindEnum.Medication, 1, 100, 0, 0, 0, 100)],
            [], 0, 0, 0);
        invoice.Issue(DateTime.Now);
        _invoice.SaveChanges(invoice);
        return invoice;
    }

    private static AptIntegrationTaskModel BillingTask(string invoiceId) =>
        AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.BillingCharge,
            AptIntegrationSourceKindEnum.Invoice,
            invoiceId,
            $"{invoiceId}:BILL",
            AptIntegrationDestinationEnum.TataRekening,
            JsonSerializer.Serialize(new { InvoiceId = invoiceId, GrandTotal = 100m }));
}
