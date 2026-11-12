using Ardalis.GuardClauses;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;

namespace Bilreg.Application.SalesContext.ResepFeature.UseCases;

public interface IResepVoidDal
{
    bool HasBeenProcessed(string resepId);
}

public record ResepVoidCommand(
    string ResepId,
    string UserId,
    string VoidReason) : IRequest<ResepVoidResponse>, IResepKey;

public record ResepVoidResponse(
    string ResepId,
    string Status);

public class ResepVoidHandler : IRequestHandler<ResepVoidCommand, ResepVoidResponse>
{
    private readonly IResepRepo _resepRepo;
    private readonly IResepVoidDal _resepDal;

    public ResepVoidHandler(IResepRepo resepRepo, IResepVoidDal resepDal)
    {
        _resepRepo = resepRepo;
        _resepDal = resepDal;
    }

    public Task<ResepVoidResponse> Handle(ResepVoidCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.ResepId, nameof(request.ResepId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.VoidReason, nameof(request.VoidReason));

        var resep = _resepRepo.LoadEntity(ResepModel.Key(request.ResepId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Resep with id '{request.ResepId}' not found.")
            );

        if (resep.IsVoid)
            throw new InvalidOperationException($"Resep '{request.ResepId}' sudah berstatus void sebelumnya.");

        if (_resepDal.HasBeenProcessed(request.ResepId))
            throw new InvalidOperationException($"Resep '{request.ResepId}' telah diproses/didispensasi farmasi sehingga tidak dapat dibatalkan.");

        resep.Void(request.UserId, request.VoidReason, DateTime.Now);
        _resepRepo.SaveChanges(resep);

        return Task.FromResult(new ResepVoidResponse(resep.ResepId, "Voided"));
    }
}
