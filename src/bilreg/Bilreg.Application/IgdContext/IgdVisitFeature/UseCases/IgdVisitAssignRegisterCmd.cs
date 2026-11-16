using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
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
    private readonly IEmrLabelGateway _emrLabelGateway;
    private readonly IgdVisitOptions _igdVisitOptions;
    private readonly ITindakanIgdRepo _tindakanIgdRepo;
    private readonly IBhpIgdRepo _bhpIgdRepo;
    private readonly IIgdBillingPostingOrchestrator _billingOrchestrator;

    public IgdVisitAssignRegisterHandler(
        IIgdVisitRepo igdVisitRepo,
        IRegRepo regRepo,
        ITglJamProvider tglJamProvider,
        IIgdVisitSmassTaskRepo smassTaskRepo,
        ISmassAssessmentGateway smassGateway,
        IEmrLabelGateway emrLabelGateway,
        IOptions<IgdVisitOptions> igdVisitOptions,
        ITindakanIgdRepo tindakanIgdRepo,
        IBhpIgdRepo bhpIgdRepo,
        IIgdBillingPostingOrchestrator billingOrchestrator)
    {
        _igdVisitRepo = igdVisitRepo;
        _regRepo = regRepo;
        _tglJamProvider = tglJamProvider;
        _smassTaskRepo = smassTaskRepo;
        _smassGateway = smassGateway;
        _emrLabelGateway = emrLabelGateway;
        _igdVisitOptions = igdVisitOptions.Value;
        _tindakanIgdRepo = tindakanIgdRepo;
        _bhpIgdRepo = bhpIgdRepo;
        _billingOrchestrator = billingOrchestrator;
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

        var listTindakan = new List<TindakanIgdModel>();
        foreach (var item in _tindakanIgdRepo.ListData(visit))
        {
            var modelOpt = _tindakanIgdRepo.LoadEntity(TindakanIgdModel.Key(item.TindakanIgdId));
            if (modelOpt.HasValue)
            {
                modelOpt.Value.AssignRegister(reg.RegId);
                listTindakan.Add(modelOpt.Value);
            }
        }

        var listBhp = new List<BhpIgdModel>();
        foreach (var item in _bhpIgdRepo.ListData(visit))
        {
            var modelOpt = _bhpIgdRepo.LoadEntity(BhpIgdModel.Key(item.BhpIgdId));
            if (modelOpt.HasValue)
            {
                modelOpt.Value.AssignRegister(reg.RegId);
                listBhp.Add(modelOpt.Value);
            }
        }

        using (var trans = TransHelper.NewScope())
        {
            _igdVisitRepo.SaveChanges(visit);
            foreach (var tdk in listTindakan)
            {
                _tindakanIgdRepo.SaveChanges(tdk);
            }
            foreach (var bhp in listBhp)
            {
                _bhpIgdRepo.SaveChanges(bhp);
            }
            trans.Complete();
        }

        try
        {
            var allSuccess = await _billingOrchestrator.SyncAllPendingForVisitAsync(visit, reg, cancellationToken);
            visit.SetPendingBilling(!allSuccess);
            _igdVisitRepo.SaveChanges(visit);
        }
        catch (Exception)
        {
            visit.SetPendingBilling(true);
            _igdVisitRepo.SaveChanges(visit);
        }

        // §6.6 steps 6–9: post-commit SMASS link hook (D-09, BR-16, BR-17). The hook is
        // outside every TransHelper scope, swallows every exception and never affects the
        // committed registration; the response stays unchanged.
        await IgdVisitSmassLinkHook.RunAsync(
            _smassTaskRepo,
            _smassGateway,
            _emrLabelGateway,
            _igdVisitOptions,
            visit.IgdVisitId,
            reg,
            request.UserId,
            cancellationToken);
    }
}
