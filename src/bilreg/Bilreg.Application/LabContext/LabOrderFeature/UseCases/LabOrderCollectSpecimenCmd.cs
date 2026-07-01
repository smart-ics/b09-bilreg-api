using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCollectSpecimenCmd(
    string OrderId,
    string UserId,
    string? CollectionNote,
    DateTime? CollectedDate)
    : IRequest, ILabOrderKey;

public class LabOrderCollectSpecimenHandler : IRequestHandler<LabOrderCollectSpecimenCmd>
{
    private readonly ILabOrderRepo _labOrderRepo;

    public LabOrderCollectSpecimenHandler(ILabOrderRepo labOrderRepo)
    {
        _labOrderRepo = labOrderRepo;
    }

    public Task Handle(LabOrderCollectSpecimenCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        var collectedAt = request.CollectedDate ?? DateTime.Now;
        var info = new CollectionInfoType(
            collectedAt,
            request.UserId,
            request.CollectionNote ?? "");

        order.CollectSpecimen(request.UserId, info);

        using var trans = TransHelper.NewScope();
        _labOrderRepo.SaveChanges(order);
        trans.Complete();
        return Task.CompletedTask;
    }
}
