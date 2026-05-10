using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;

public record IgdTindakanAddCmd(
    string IgdVisitId,
    string TarifId,
    string TarifName,
    int Qty,
    decimal Price,
    string UserId)
    : IRequest<IgdTindakanAddResponse>, IIgdVisitKey;

public record IgdTindakanAddResponse(string TindakanIgdId, string IgdVisitId, decimal Subtotal);

public class IgdTindakanAddHandler : IRequestHandler<IgdTindakanAddCmd, IgdTindakanAddResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly ITindakanIgdRepo _tindakanRepo;

    public IgdTindakanAddHandler(IIgdVisitRepo igdVisitRepo, ITindakanIgdRepo tindakanRepo)
    {
        _igdVisitRepo = igdVisitRepo;
        _tindakanRepo = tindakanRepo;
    }

    public Task<IgdTindakanAddResponse> Handle(IgdTindakanAddCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.TarifId, nameof(request.TarifId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NegativeOrZero(request.Qty, nameof(request.Qty));
        Guard.Against.Negative(request.Price, nameof(request.Price));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        var audit = new AuditInfoType(request.UserId, DateTime.Now);
        var tindakan = TindakanIgdModel.Create(visit, request.TarifId, request.TarifName, request.Qty, request.Price, audit);
        visit.RecordTindakanEvent(tindakan.TindakanIgdId, audit);

        IgdTindakanAddResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _tindakanRepo.SaveChanges(tindakan);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdTindakanAddResponse(tindakan.TindakanIgdId, visit.IgdVisitId, tindakan.Subtotal);
        }

        return Task.FromResult(response);
    }
}
