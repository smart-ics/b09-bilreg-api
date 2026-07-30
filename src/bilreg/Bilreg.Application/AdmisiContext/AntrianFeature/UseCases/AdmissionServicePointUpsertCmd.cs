using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AdmissionServicePointUpsertCmd(
    string ServicePointId,
    string DisplayName,
    string QueuePrefix,
    bool IsActive)
    : IRequest<AdmissionServicePointUpsertResponse>, IAdmissionServicePointKey;

public record AdmissionServicePointUpsertResponse(
    string ServicePointId,
    string DisplayName,
    string QueuePrefix,
    string Status);

public sealed class AdmissionServicePointUpsertHandler
    : IRequestHandler<AdmissionServicePointUpsertCmd, AdmissionServicePointUpsertResponse>
{
    private readonly IAdmissionServicePointRepo _repo;

    public AdmissionServicePointUpsertHandler(IAdmissionServicePointRepo repo)
    {
        _repo = repo;
    }

    public Task<AdmissionServicePointUpsertResponse> Handle(
        AdmissionServicePointUpsertCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ServicePointId);
        Guard.Against.NullOrWhiteSpace(request.DisplayName);
        Guard.Against.NullOrWhiteSpace(request.QueuePrefix);

        var existing = _repo.LoadEntity(request);
        var model = existing.Match(
            onSome: x => x.Rename(request.DisplayName).ChangePrefix(request.QueuePrefix),
            onNone: () => AdmissionServicePointModel.Create(
                request.ServicePointId, request.DisplayName, request.QueuePrefix));

        if (!request.IsActive)
            model = model.Retire();

        _repo.SaveChanges(model);

        return Task.FromResult(new AdmissionServicePointUpsertResponse(
            model.ServicePointId,
            model.DisplayName,
            model.QueuePrefix,
            model.Status.ToString()));
    }
}
