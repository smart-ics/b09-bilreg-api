using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitRetryBillingPostingCmd(string IgdVisitId, string UserId = "")
    : IRequest<IgdVisitRetryBillingPostingResponse>, IIgdVisitKey;

public record IgdVisitRetryBillingPostingResponse(
    string IgdVisitId,
    bool AllSuccess,
    bool HasPendingBilling);

public class IgdVisitRetryBillingPostingHandler : IRequestHandler<IgdVisitRetryBillingPostingCmd, IgdVisitRetryBillingPostingResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IRegRepo _regRepo;
    private readonly IIgdBillingPostingOrchestrator _billingOrchestrator;

    public IgdVisitRetryBillingPostingHandler(
        IIgdVisitRepo igdVisitRepo,
        IRegRepo regRepo,
        IIgdBillingPostingOrchestrator billingOrchestrator)
    {
        _igdVisitRepo = igdVisitRepo;
        _regRepo = regRepo;
        _billingOrchestrator = billingOrchestrator;
    }

    public async Task<IgdVisitRetryBillingPostingResponse> Handle(IgdVisitRetryBillingPostingCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));

        var visit = _igdVisitRepo.LoadEntity(request)
            .GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        if (!visit.HasReg)
        {
            throw new InvalidOperationException($"Visit '{visit.IgdVisitId}' belum memiliki registrasi administratif.");
        }

        var reg = _regRepo.LoadEntity(RegModel.Key(visit.Reg.RegId))
            .GetValueOrThrow($"Reg '{visit.Reg.RegId}' not found");

        bool allSuccess;
        try
        {
            allSuccess = await _billingOrchestrator.SyncAllPendingForVisitAsync(visit, reg, cancellationToken);
        }
        catch (Exception)
        {
            allSuccess = false;
        }

        visit.SetPendingBilling(!allSuccess);
        _igdVisitRepo.SaveChanges(visit);

        return new IgdVisitRetryBillingPostingResponse(visit.IgdVisitId, allSuccess, visit.HasPendingBilling);
    }
}
