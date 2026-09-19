using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitReplaceRegisterCmd(
    string IgdVisitId,
    string NewRegId,
    string UserId) : IRequest, IIgdVisitKey;


public class IgdVisitReplaceRegisterHandler : IRequestHandler<IgdVisitReplaceRegisterCmd>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IRegRepo _regRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ITindakanIgdRepo _tindakanIgdRepo;
    private readonly IBhpIgdRepo _bhpIgdRepo;
    private readonly IIgdVisitSmassTaskRepo _smassTaskRepo;
    private readonly ISmassAssessmentGateway _smassGateway;
    private readonly IgdVisitOptions _igdVisitOptions;
    public IgdVisitReplaceRegisterHandler(IIgdVisitRepo igdVisitRepo,
        IRegRepo regRepo,
        ITglJamProvider tglJamProvider,
        ITindakanIgdRepo tindakanIgdRepo,
        IBhpIgdRepo bhpIgdRepo,
        IIgdVisitSmassTaskRepo smassTaskRepo,
        ISmassAssessmentGateway smassGateway,
        IOptions<IgdVisitOptions> igdVisitOptions)
    {
        _igdVisitRepo = igdVisitRepo;
        _regRepo = regRepo;
        _tglJamProvider = tglJamProvider;
        _tindakanIgdRepo = tindakanIgdRepo;
        _bhpIgdRepo = bhpIgdRepo;
        _smassTaskRepo = smassTaskRepo;
        _smassGateway = smassGateway;
        _igdVisitOptions = igdVisitOptions.Value;
    }

    public async Task Handle(IgdVisitReplaceRegisterCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.NewRegId, nameof(request.NewRegId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var igdVisit = LoadAndValidateIgdVisit(request);
        var newReg = LoadAndValidateRegister(request);
        
        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        igdVisit.ReplaceRegister(newReg, audit);

        using (var trans = TransHelper.NewScope())
        {
            _igdVisitRepo.SaveChanges(igdVisit);
            trans.Complete();
        }

        // §6.6 steps 6–9: post-commit SMASS link hook (D-09, BR-16, BR-17 replace-latest).
        // The hook is outside every TransHelper scope, swallows every exception and never
        // affects the committed registration; the response stays unchanged.
        await IgdVisitSmassLinkHook.RunAsync(
            _smassTaskRepo,
            _smassGateway,
            _igdVisitOptions,
            igdVisit.IgdVisitId,
            newReg,
            cancellationToken);
    }

    private RegModel LoadAndValidateRegister(IgdVisitReplaceRegisterCmd request)
    {
        var reg = _regRepo.LoadEntity(RegModel.Key(request.NewRegId))
            .GetValueOrThrow($"Register {request.NewRegId} invalid");
        if(reg.JenisReg != JenisRegEnum.Darurat)
            throw new InvalidOperationException($"Register {request.NewRegId} bukan Register IGD");
        var visitReg = _igdVisitRepo.GetByRegId(request.NewRegId);
        if(visitReg.HasValue)
            throw new InvalidOperationException($"Register baru {request.NewRegId} sudah di link-kan dengan IgdVisit {visitReg.Value.IgdVisitId}");

        return reg;
    }

    private IgdVisitModel LoadAndValidateIgdVisit(IgdVisitReplaceRegisterCmd request)
    {
        var visit = _igdVisitRepo.LoadEntity(request)
            .GetValueOrThrow($"IgdVisit {request.IgdVisitId} invalid");
        if(_tindakanIgdRepo.AnyForVisit(visit))
            throw new InvalidOperationException($"Igd Visit {request.IgdVisitId} sudah dilakukan Transaksi Tindakan Igd, tidak bisa replace Register");
        if (_bhpIgdRepo.AnyForVisit(visit))
            throw new InvalidOperationException($"Igd Visit {request.IgdVisitId} sudah dilakukan Transaksi BHP Igd, tidak bisa replace Register");

        return visit;
    }

    
}
