using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature.UseCases;

public sealed record AdmListJourneyQry(
    JourneyListScope Scope = JourneyListScope.Active,
    JourneyOperationalStage? Stage = null,
    string? SearchTerm = null,
    string? DokterId = null,
    string? BangsalId = null,
    string? KelasId = null,
    string? TipeJaminanId = null,
    int? Priority = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Cursor = null,
    int PageSize = 50) : IRequest<JourneyListResult>;

public sealed class AdmListJourneyHandler : IRequestHandler<AdmListJourneyQry, JourneyListResult>
{
    private readonly IJourneyDal _dal;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmListJourneyHandler(IJourneyDal dal, ITglJamProvider tglJamProvider) =>
        (_dal, _tglJamProvider) = (dal, tglJamProvider);

    public async Task<JourneyListResult> Handle(AdmListJourneyQry request, CancellationToken cancellationToken)
    {
        var filter = new JourneyListFilter(
            request.Scope,
            request.Stage,
            request.SearchTerm,
            request.DokterId,
            request.BangsalId,
            request.KelasId,
            request.TipeJaminanId,
            request.Priority,
            request.DateFrom,
            request.DateTo,
            request.Cursor,
            request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, 200));

        return await _dal.ListAsync(filter, _tglJamProvider.Now, cancellationToken);
    }
}

public sealed record AdmGetJourneyQry(string JourneyId) : IRequest<JourneyDetailWorkspace?>;

public sealed class AdmGetJourneyHandler : IRequestHandler<AdmGetJourneyQry, JourneyDetailWorkspace?>
{
    private readonly IJourneyDal _dal;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmGetJourneyHandler(IJourneyDal dal, ITglJamProvider tglJamProvider) =>
        (_dal, _tglJamProvider) = (dal, tglJamProvider);

    public async Task<JourneyDetailWorkspace?> Handle(AdmGetJourneyQry request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.JourneyId))
            return null;

        return await _dal.GetByJourneyIdAsync(request.JourneyId.Trim(), _tglJamProvider.Now, cancellationToken);
    }
}

public sealed record AdmResolveJourneyLegacyRecordQry(
    string RecordType,
    string RecordId) : IRequest<JourneyLegacyResolution?>;

public sealed class AdmResolveJourneyLegacyRecordHandler
    : IRequestHandler<AdmResolveJourneyLegacyRecordQry, JourneyLegacyResolution?>
{
    private readonly IJourneyDal _dal;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmResolveJourneyLegacyRecordHandler(IJourneyDal dal, ITglJamProvider tglJamProvider) =>
        (_dal, _tglJamProvider) = (dal, tglJamProvider);

    public async Task<JourneyLegacyResolution?> Handle(
        AdmResolveJourneyLegacyRecordQry request,
        CancellationToken cancellationToken) =>
        await _dal.ResolveLegacyRecordAsync(request.RecordType, request.RecordId, _tglJamProvider.Now, cancellationToken);
}
