using Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record TindakanVoidCmd(string TindakanId, string UserId) : IRequest, ITindakanKey;

public class TindakanVoidHandler : IRequestHandler<TindakanVoidCmd>
{
    private readonly ITindakanRepo _tdkRepo;
    public TindakanVoidHandler(ITindakanRepo tdkRepo)
    {
        _tdkRepo = tdkRepo;
    }

    public Task Handle(TindakanVoidCmd request, CancellationToken cancellationToken)
    {
        var tdk = _tdkRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tindakan {request.TindakanId} not found") 
            );
        
        tdk.Void(request.UserId);
        _tdkRepo.SaveChanges(tdk);

        return Task.CompletedTask;
    }
}
