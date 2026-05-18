using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabCollectionPreparationQuery(string OrderId) : IRequest<LabCollectionPreparationView>;

public class LabCollectionPreparationHandler : IRequestHandler<LabCollectionPreparationQuery, LabCollectionPreparationView>
{
    private readonly ILabCollectionPreparationDal _dal;

    public LabCollectionPreparationHandler(ILabCollectionPreparationDal dal)
    {
        _dal = dal;
    }

    public Task<LabCollectionPreparationView> Handle(
        LabCollectionPreparationQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));

        var view = _dal.Get(request.OrderId);
        if (view is null)
            throw new Exception($"LabOrder '{request.OrderId}' not found");

        return Task.FromResult(view);
    }
}
