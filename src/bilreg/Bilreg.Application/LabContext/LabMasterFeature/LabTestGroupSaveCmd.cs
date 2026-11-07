using Ardalis.GuardClauses;
using Bilreg.Application.Shared;
using Bilreg.Domain.LabContext.LabMasterFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabTestGroupSaveResponse(
    string TestGroupId,
    string TestGroupName);

public record LabTestGroupSaveCmd(
    string TestGroupId,
    string TestGroupName,
    string UserId) : IRequest<LabTestGroupSaveResponse>, ILabTestGroupKey;

public class LabTestGroupSaveHandler
    : IRequestHandler<LabTestGroupSaveCmd, LabTestGroupSaveResponse>
{
    private readonly ILabTestGroupRepo _repo;
    private readonly ITglJamProvider _clock;

    public LabTestGroupSaveHandler(
        ILabTestGroupRepo repo,
        ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<LabTestGroupSaveResponse> Handle(
        LabTestGroupSaveCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TestGroupId, nameof(request.TestGroupId));
        Guard.Against.NullOrWhiteSpace(request.TestGroupName, nameof(request.TestGroupName));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var now = _clock.Now;
        var existing = _repo.LoadEntity(request);
        var model = existing.Match(
            onSome: x => x.Update(request.TestGroupName, request.UserId, now),
            onNone: () => LabTestGroupModel.Create(request.TestGroupId, request.TestGroupName, request.UserId, now));

        _repo.SaveChanges(model);

        return Task.FromResult(new LabTestGroupSaveResponse(model.TestGroupId, model.TestGroupName));
    }
}
