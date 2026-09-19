using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitAssignRegisterCmd(
    string IgdVisitId,
    string RegId,
    string UserId)
    : IRequest, IIgdVisitKey, IRegKey;

public class IgdVisitAssignRegisterHandler : IRequestHandler<IgdVisitAssignRegisterCmd>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IRegRepo _regRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IIgdVisitSmassTaskRepo _smassTaskRepo;
    private readonly ISmassAssessmentGateway _smassGateway;
    private readonly IgdVisitOptions _igdVisitOptions;

    public IgdVisitAssignRegisterHandler(IIgdVisitRepo igdVisitRepo, IRegRepo regRepo,
        ITglJamProvider tglJamProvider, IIgdVisitSmassTaskRepo smassTaskRepo,
        ISmassAssessmentGateway smassGateway, IOptions<IgdVisitOptions> igdVisitOptions)
    {
        _igdVisitRepo = igdVisitRepo;
        _regRepo = regRepo;
        _tglJamProvider = tglJamProvider;
        _smassTaskRepo = smassTaskRepo;
        _smassGateway = smassGateway;
        _igdVisitOptions = igdVisitOptions.Value;
    }

    public async Task Handle(IgdVisitAssignRegisterCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Reg '{request.RegId}' not found");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        visit.AssignRegister(reg, audit);

        using (var trans = TransHelper.NewScope())
        {
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
        }

        // §6.6 steps 6–9: post-commit SMASS link hook (D-09, BR-16, BR-17). The hook is
        // outside every TransHelper scope, swallows every exception and never affects the
        // committed registration; the response stays unchanged.
        await IgdVisitSmassLinkHook.RunAsync(
            _smassTaskRepo,
            _smassGateway,
            _igdVisitOptions,
            visit.IgdVisitId,
            reg,
            cancellationToken);
    }
}
