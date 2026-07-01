using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.IgdContext.TindakanIgdFeature.UseCases;

public record IgdTindakanAddCmd(
    string IgdVisitId,
    string PetugasMedisId,
    string ReffId,
    string Descriptions,
    int Qty,
    int Aktifitas,
    string UserId)
    : IRequest<IgdTindakanAddResponse>, IIgdVisitKey;

public record IgdTindakanAddResponse(string TindakanIgdId, string IgdVisitId);

public class IgdTindakanAddHandler : IRequestHandler<IgdTindakanAddCmd, IgdTindakanAddResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly ITindakanIgdRepo _tindakanRepo;
    private readonly IPpaRepo _ppaRepo;

    public IgdTindakanAddHandler(IIgdVisitRepo igdVisitRepo, 
        ITindakanIgdRepo tindakanRepo, 
        IPpaRepo ppaRepo)
    {
        _igdVisitRepo = igdVisitRepo;
        _tindakanRepo = tindakanRepo;
        _ppaRepo = ppaRepo;
    }

    public Task<IgdTindakanAddResponse> Handle(IgdTindakanAddCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.PetugasMedisId, nameof(request.PetugasMedisId));
        Guard.Against.NullOrWhiteSpace(request.ReffId, nameof(request.ReffId));
        Guard.Against.NullOrWhiteSpace(request.Descriptions, nameof(request.Descriptions));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NegativeOrZero(request.Qty, nameof(request.Qty));
        Guard.Against.Null(request.Aktifitas, nameof(request.Aktifitas));

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");
        var ppa = _ppaRepo.LoadEntity(PpaType.Key(request.PetugasMedisId)).GetValueOrThrow($"Petugas Medis {request.PetugasMedisId} not found");

        var tindakan = TindakanIgdModel.Create
            (visit, request.ReffId, request.Descriptions, request.Qty, 
            (ActivityTindakanIgd)request.Aktifitas, ppa, request.UserId);
        visit.RecordTindakanEvent(tindakan, tindakan.Audit);

        IgdTindakanAddResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _tindakanRepo.SaveChanges(tindakan);
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
            response = new IgdTindakanAddResponse(tindakan.TindakanIgdId, visit.IgdVisitId);
        }

        return Task.FromResult(response);
    }
}
