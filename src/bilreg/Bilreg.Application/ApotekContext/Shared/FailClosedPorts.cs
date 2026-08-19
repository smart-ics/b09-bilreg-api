using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;

namespace Bilreg.Application.ApotekContext.Shared;

public class FailClosedPrescriptionContractPort : IPrescriptionContractPort
{
    public PrescriptionContract Load(ResepKerjaSourceKindEnum sourceKind, string sourceResepId)
        => throw new ApotekDomainException($"Prescription contract adapter is not configured for {sourceKind}:{sourceResepId}.");
}

public class FailClosedPaymentClearancePort : IPaymentClearancePort
{
    public PaymentClearanceEvidence? Load(string paymentClearanceReff) => null;
}

public class FailClosedTataRekeningChargePort : ITataRekeningChargePort
{
    public string IssueCharge(InvoiceModel invoice)
        => throw new ApotekDomainException("Tata Rekening charge adapter is not configured.");
}

public class DenyTataRekeningInvoicePermissionPort : ITataRekeningInvoicePermissionPort
{
    public bool AllowsModification(string tataRekeningChargeId) => false;
}

public class FailClosedSepFornasPort : ISepFornasPort
{
    public SepFornasEvidence Evaluate(string regId, string brgId)
        => new("", FornasCoverageEnum.Unknown);
}

public class FailClosedIterConsumePort : IIterConsumePort
{
    public string Consume(string sourceResepId, int consumeCount)
        => throw new ApotekDomainException("Iter consume adapter is not configured.");
}

public class FailClosedMedicationPricePort : IMedicationPricePort
{
    public MedicationPrice PriceAt(string brgId, DateTime snapshotAt)
        => throw new ApotekDomainException("Medication price adapter is not configured.");
}

public class StockPharmacyAdapter : IStockPharmacyPort
{
    private readonly IMediator _mediator;

    public StockPharmacyAdapter(IMediator mediator) => _mediator = mediator;

    public string ReserveToTemporaryUnit(string dispensingId, int itemNo, string brgId, decimal qty)
    {
        var result = _mediator.Send(new PostStockTransferConsequenceCommand(
            brgId, qty, ApotekLocationIds.PharmacyUnitLayananId, ApotekLocationIds.DispensingTemporaryUnitLayananId,
            $"{dispensingId}:{itemNo}:R", DateTime.Now, AptIntegrationWorkerUser.Id)).GetAwaiter().GetResult();
        return result.Lines.FirstOrDefault()?.StokMutasiOutId ?? $"{dispensingId}:R{itemNo}";
    }

    public string RemoveOnHandover(string dispensingId, int itemNo, string brgId, decimal qty)
    {
        var result = _mediator.Send(new PostDispenseIssueConsequenceCommand(
            brgId,
            ApotekLocationIds.DispensingTemporaryUnitLayananId,
            qty,
            $"{dispensingId}:{itemNo}:I",
            DateTime.Now,
            AptIntegrationWorkerUser.Id)).GetAwaiter().GetResult();
        return result.Lines.FirstOrDefault()?.StokMutasiId ?? $"{dispensingId}:I{itemNo}";
    }

    public string ReturnOnNoShow(string dispensingId, int itemNo, string brgId, decimal qty)
    {
        var result = _mediator.Send(new PostStockTransferConsequenceCommand(
            brgId, qty, ApotekLocationIds.DispensingTemporaryUnitLayananId, ApotekLocationIds.PharmacyUnitLayananId,
            $"{dispensingId}:{itemNo}:N", DateTime.Now, AptIntegrationWorkerUser.Id)).GetAwaiter().GetResult();
        return result.Lines.FirstOrDefault()?.StokMutasiOutId ?? $"{dispensingId}:N{itemNo}";
    }
}

public static class AptIntegrationWorkerUser
{
    public const string Id = "APT-WORKER";
}
