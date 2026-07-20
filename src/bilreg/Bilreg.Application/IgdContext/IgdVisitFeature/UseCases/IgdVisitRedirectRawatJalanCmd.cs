using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.RedirectRajalFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.RedirectRajalFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitRedirectRawatJalanCmd(
    string IgdVisitId,
    string Reason,
    string UserId)
    : IRequest<IgdVisitRedirectRawatJalanResponse>, IIgdVisitKey;

public record IgdVisitRedirectRawatJalanResponse(string IgdVisitId, string RedirectRajalId);

public class IgdVisitRedirectRawatJalanHandler : IRequestHandler<IgdVisitRedirectRawatJalanCmd, IgdVisitRedirectRawatJalanResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IRedirectRajalRepo _redirectRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public IgdVisitRedirectRawatJalanHandler(
        IIgdVisitRepo igdVisitRepo,
        IRedirectRajalRepo redirectRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _igdVisitRepo = igdVisitRepo;
        _redirectRepo = redirectRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<IgdVisitRedirectRawatJalanResponse> Handle(IgdVisitRedirectRawatJalanCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        var redirect = RedirectRajalModel.Create(visit, request.Reason, audit);
        visit.RedirectToRawatJalan(redirect.RedirectRajalId, request.Reason ?? "-", audit);

        IgdVisitRedirectRawatJalanResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _redirectRepo.SaveChanges(redirect);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdVisitRedirectRawatJalanResponse(visit.IgdVisitId, redirect.RedirectRajalId);
        }
        return Task.FromResult(response);
    }
}
