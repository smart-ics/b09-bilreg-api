using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Farinv.Application.SalesContext.AntrianFeature;
using MediatR;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public class AppendTrackerEvidenceService : IAppendTrackerEvidenceService
{
    private readonly IMediator _mediator;

    public AppendTrackerEvidenceService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public AppendPharmacyEvidenceRequest Execute(
        AppendPharmacyEvidenceRequest request)
    {
        _mediator.Send(new TrkAppendPharmacyEvidenceCmd(
            request.PasienTrackerId,
            request.EventName,
            request.ReffId,
            request.OccurredAt))
        .GetAwaiter()
        .GetResult();

        return request;
    }
}
