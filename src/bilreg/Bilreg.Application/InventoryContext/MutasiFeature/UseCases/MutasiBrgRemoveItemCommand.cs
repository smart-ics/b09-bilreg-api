using Ardalis.GuardClauses;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgRemoveItemCommand(
    string MutasiBrgId,
    int NoUrut,
    string UserId = ""
) : IRequest, IMutasiBrgKey;

public class MutasiBrgRemoveItemHandler : IRequestHandler<MutasiBrgRemoveItemCommand>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;

    public MutasiBrgRemoveItemHandler(IMutasiBrgRepo mutasiBrgRepo)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
    }

    public Task Handle(MutasiBrgRemoveItemCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard input
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiBrgId, nameof(request.MutasiBrgId));
        Guard.Against.NegativeOrZero(request.NoUrut, nameof(request.NoUrut));

        // 2. Validate MutasiBrg existence and Draft state
        var mutasiMaybe = _mutasiBrgRepo.LoadEntity(MutasiBrgModel.Key(request.MutasiBrgId));
        if (!mutasiMaybe.HasValue)
            throw new KeyNotFoundException($"MutasiBrg with Id '{request.MutasiBrgId}' not found");

        var mutasi = mutasiMaybe.Value;
        if (mutasi.State != MutasiBrgStateEnum.Draft)
            throw new InvalidOperationException($"MutasiBrg '{request.MutasiBrgId}' is not in Draft state (Current: {mutasi.State})");

        // 3. Remove item and save
        mutasi.RemoveItem(request.NoUrut);
        _mutasiBrgRepo.SaveChanges(mutasi);

        return Task.CompletedTask;
    }
}
