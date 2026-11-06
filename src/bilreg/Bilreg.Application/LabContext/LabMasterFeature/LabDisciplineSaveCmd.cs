using Ardalis.GuardClauses;
using Bilreg.Application.Shared;
using Bilreg.Domain.LabContext.LabMasterFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabMasterFeature;

public record LabDisciplineSaveResponse(
    string DisciplineId,
    string DisciplineName);

public record LabDisciplineSaveCmd(
    string DisciplineId,
    string DisciplineName,
    string UserId) : IRequest<LabDisciplineSaveResponse>, ILabDisciplineKey;

public class LabDisciplineSaveHandler
    : IRequestHandler<LabDisciplineSaveCmd, LabDisciplineSaveResponse>
{
    private readonly ILabDisciplineRepo _repo;
    private readonly ITglJamProvider _clock;

    public LabDisciplineSaveHandler(
        ILabDisciplineRepo repo,
        ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<LabDisciplineSaveResponse> Handle(
        LabDisciplineSaveCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DisciplineId, nameof(request.DisciplineId));
        Guard.Against.NullOrWhiteSpace(request.DisciplineName, nameof(request.DisciplineName));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var now = _clock.Now;
        var existing = _repo.LoadEntity(request);
        var model = existing.Match(
            onSome: x => x.Update(request.DisciplineName, request.UserId, now),
            onNone: () => LabDisciplineModel.Create(request.DisciplineId, request.DisciplineName, request.UserId, now));

        _repo.SaveChanges(model);

        return Task.FromResult(new LabDisciplineSaveResponse(model.DisciplineId, model.DisciplineName));
    }
}
