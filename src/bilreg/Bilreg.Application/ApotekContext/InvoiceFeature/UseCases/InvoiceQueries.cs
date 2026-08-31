using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.Shared;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.InvoiceFeature.UseCases;

public record InvoiceCorrectionStatusQuery(string InvoiceId) : IRequest<InvoiceCorrectionStatusResponse>;

public record InvoiceCorrectionStatusResponse(
    string InvoiceId,
    InvoiceStatusEnum Status,
    InvoiceCorrectionDispositionEnum CorrectionDisposition,
    bool AllowsDirectRevision,
    bool RequiresManualTataRekeningCorrection,
    string TataRekeningCorrectionReff,
    int Version);

public class InvoiceCorrectionStatusHandler : IRequestHandler<InvoiceCorrectionStatusQuery, InvoiceCorrectionStatusResponse>
{
    private readonly IInvoiceRepo _repo;
    private readonly ITataRekeningInvoicePermissionPort _permission;

    public InvoiceCorrectionStatusHandler(IInvoiceRepo repo, ITataRekeningInvoicePermissionPort permission)
    {
        _repo = repo;
        _permission = permission;
    }

    public Task<InvoiceCorrectionStatusResponse> Handle(InvoiceCorrectionStatusQuery request, CancellationToken cancellationToken)
    {
        var invoice = _repo.LoadEntity(InvoiceModel.Key(request.InvoiceId))
            .GetValueOrThrow($"Invoice '{request.InvoiceId}' not found");
        var allowsModification = invoice.InvoiceStatus == InvoiceStatusEnum.Established
                                 || _permission.AllowsModification(invoice.TataRekeningChargeId);
        var disposition = invoice.CorrectionDisposition(allowsModification);
        return Task.FromResult(new InvoiceCorrectionStatusResponse(
            invoice.InvoiceId,
            invoice.InvoiceStatus,
            disposition,
            disposition == InvoiceCorrectionDispositionEnum.DirectRevisionAllowed,
            disposition == InvoiceCorrectionDispositionEnum.ManualTataRekeningCorrectionPending,
            invoice.TataRekeningCorrectionReff,
            invoice.Version));
    }
}
