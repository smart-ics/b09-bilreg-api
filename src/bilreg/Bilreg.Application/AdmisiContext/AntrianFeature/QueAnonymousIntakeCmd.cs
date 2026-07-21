using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueAnonymousIntakeCmd(
    string ServicePointCode,
    string ServicePointName,
    string? TglYmd = null) : IRequest<QueAnonymousIntakeResponse>;

public record QueAnonymousIntakeResponse(
    string AntrianId,
    int NoUrut,
    DateTime CreatedAt);

public class QueAnonymousIntakeHandler
    : IRequestHandler<QueAnonymousIntakeCmd, QueAnonymousIntakeResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly ITglJamProvider _tglJamProvider;

    public QueAnonymousIntakeHandler(
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        ITglJamProvider tglJamProvider)
    {
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _tglJamProvider = tglJamProvider;
    }

    public Task<QueAnonymousIntakeResponse> Handle(
        QueAnonymousIntakeCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ServicePointCode);
        Guard.Against.NullOrWhiteSpace(request.ServicePointName);

        var occurredAt = _tglJamProvider.Now;
        var businessDate = string.IsNullOrWhiteSpace(request.TglYmd)
            ? DateOnly.FromDateTime(occurredAt)
            : DateOnly.ParseExact(request.TglYmd, "yyyy-MM-dd");

        var servicePoint = new ServicePointType(
            request.ServicePointCode.Trim(),
            request.ServicePointName.Trim());
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, servicePoint);

        var listQue = _antrianRepo.ListData(businessDate);
        var queView = listQue.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var que = queView is null
            ? _antrianFactory.Create(servicePoint, businessDate)
            : _antrianRepo.LoadEntity(queView).Value;

        QueAnonymousIntakeResponse response;
        using (var trans = TransHelper.NewScope())
        {
            var entry = que.AddEntry(occurredAt);
            _antrianRepo.SaveChanges(que);
            trans.Complete();

            response = new QueAnonymousIntakeResponse(
                que.AntrianId,
                entry.NoUrut,
                entry.CreatedAt);
        }

        return Task.FromResult(response);
    }
}
