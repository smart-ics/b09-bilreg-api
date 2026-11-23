using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitVoidTriageCmd(
    string IgdVisitId,
    int NoTriage,
    string UserId,
    string VoidReason)
    : IRequest<IgdVisitVoidTriageResponse>, IIgdVisitKey;

public record IgdVisitVoidTriageResponse(
    string IgdVisitId,
    int NoTriage,
    bool IsVoided,
    bool HasTriage,
    string ActiveTriageLevel,
    string ActiveTriageColor,
    DateTime LastTriageAt,
    DateTime NextReTriageAt,
    string SmassVoidStatus);

public class IgdVisitVoidTriageHandler : IRequestHandler<IgdVisitVoidTriageCmd, IgdVisitVoidTriageResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IIgdVisitSmassTaskRepo _smassTaskRepo;
    private readonly ISmassAssessmentGateway _smassGateway;
    private readonly IgdVisitOptions _igdVisitOptions;

    public IgdVisitVoidTriageHandler(
        IIgdVisitRepo igdVisitRepo,
        ITglJamProvider tglJamProvider,
        IIgdVisitSmassTaskRepo smassTaskRepo,
        ISmassAssessmentGateway smassGateway,
        IOptions<IgdVisitOptions> igdVisitOptions)
    {
        _igdVisitRepo = igdVisitRepo;
        _tglJamProvider = tglJamProvider;
        _smassTaskRepo = smassTaskRepo;
        _smassGateway = smassGateway;
        _igdVisitOptions = igdVisitOptions.Value;
    }

    public async Task<IgdVisitVoidTriageResponse> Handle(IgdVisitVoidTriageCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NegativeOrZero(request.NoTriage, nameof(request.NoTriage));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        if (string.IsNullOrWhiteSpace(request.VoidReason) || request.VoidReason.Trim() == "-")
            throw new ArgumentException("Alasan pembatalan triage wajib diisi.", nameof(request.VoidReason));

        var visit = _igdVisitRepo.LoadEntity(request)
            .GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        visit.VoidTriage(request.NoTriage, request.VoidReason, audit);

        using (var trans = TransHelper.NewScope())
        {
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
        }

        // TD-03 / §10: post-commit SMASS void hook strictly outside TransHelper scope.
        // Never propagates an exception into this handler.
        var smass = await IgdVisitSmassVoidHook.RunAsync(
            _smassTaskRepo,
            _smassGateway,
            _igdVisitOptions,
            visit.IgdVisitId,
            request.NoTriage,
            cancellationToken);

        return new IgdVisitVoidTriageResponse(
            IgdVisitId: visit.IgdVisitId,
            NoTriage: request.NoTriage,
            IsVoided: true,
            HasTriage: visit.HasTriage,
            ActiveTriageLevel: visit.HasTriage ? visit.Triage.Level.ToCode() : "",
            ActiveTriageColor: visit.HasTriage ? visit.TriageColor.ToCode() : "",
            LastTriageAt: visit.LastTriageAt,
            NextReTriageAt: visit.NextReTriageAt,
            SmassVoidStatus: smass.Status);
    }
}
