using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.BhpIgdFeature.UseCases;

public record IgdBhpAddCmd(
    string IgdVisitId,
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    string UserId)
    : IRequest<IgdBhpAddResponse>, IIgdVisitKey;

public record IgdBhpAddResponse(string BhpIgdId, string IgdVisitId, decimal Subtotal);

public class IgdBhpAddHandler : IRequestHandler<IgdBhpAddCmd, IgdBhpAddResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly IBhpIgdRepo _bhpRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegRepo _regRepo;
    private readonly IIgdBillingPostingOrchestrator _billingOrchestrator;

    public IgdBhpAddHandler(
        IIgdVisitRepo igdVisitRepo, 
        IBhpIgdRepo bhpRepo,
        ITglJamProvider tglJamProvider,
        IRegRepo regRepo,
        IIgdBillingPostingOrchestrator billingOrchestrator)
    {
        _igdVisitRepo = igdVisitRepo;
        _bhpRepo = bhpRepo;
        _tglJamProvider = tglJamProvider;
        _regRepo = regRepo;
        _billingOrchestrator = billingOrchestrator;
    }

    public async Task<IgdBhpAddResponse> Handle(IgdBhpAddCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.BhpItemId, nameof(request.BhpItemId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NegativeOrZero(request.Qty, nameof(request.Qty));
        Guard.Against.Negative(request.Price, nameof(request.Price));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        var bhp = BhpIgdModel.Create(visit, request.BhpItemId, request.BhpItemName, request.Qty, request.Price, audit);
        visit.RecordBhpEvent(bhp.BhpIgdId, audit);

        IgdBhpAddResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _bhpRepo.SaveChanges(bhp);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdBhpAddResponse(bhp.BhpIgdId, visit.IgdVisitId, bhp.Subtotal);
        }

        if (visit.HasReg)
        {
            var reg = _regRepo.LoadEntity(visit.Reg).GetValueOrThrow($"Reg '{visit.Reg.RegId}' not found");
            try
            {
                var posted = await _billingOrchestrator.PostBhpAsync(visit, reg, bhp, cancellationToken);
                if (!posted)
                {
                    visit.SetPendingBilling(true);
                    _igdVisitRepo.SaveChanges(visit);
                }
            }
            catch (Exception)
            {
                visit.SetPendingBilling(true);
                _igdVisitRepo.SaveChanges(visit);
            }
        }

        return response;
    }
}
