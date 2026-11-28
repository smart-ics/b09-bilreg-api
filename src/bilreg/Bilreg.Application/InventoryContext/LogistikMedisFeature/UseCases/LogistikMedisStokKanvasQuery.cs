using System.Globalization;
using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;

public record LogistikMedisStokKanvasQuery(string LayananId, string? Keyword = null)
    : IRequest<IEnumerable<LogistikMedisStokKanvasResponse>>;

public record LogistikMedisStokKanvasResponse(
    string BrgId,
    string BrgName,
    decimal TotalStok,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    decimal HppDefault,
    decimal MinStok,
    decimal MaxStok,
    string NearestTglEd,
    decimal PendingRequestQty,
    decimal IncomingMutasiQty,
    IReadOnlyList<LogistikMedisStokKanvasBatchResponse> Batches
);

public record LogistikMedisStokKanvasBatchResponse(
    string NoBatch,
    string TglEd,
    decimal QtySisa,
    decimal Hpp
);

public class LogistikMedisStokKanvasHandler
    : IRequestHandler<LogistikMedisStokKanvasQuery, IEnumerable<LogistikMedisStokKanvasResponse>>
{
    private readonly ILogistikMedisBffDal _bffDal;

    public LogistikMedisStokKanvasHandler(ILogistikMedisBffDal bffDal)
    {
        _bffDal = bffDal;
    }

    public Task<IEnumerable<LogistikMedisStokKanvasResponse>> Handle(
        LogistikMedisStokKanvasQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));

        var layananId = request.LayananId.Trim();
        var stokRows = (_bffDal.ListStokBatch(layananId) ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.BrgId) && x.QtySisa > 0)
            .ToList();
        var barangLayananRows = (_bffDal.ListBarangLayanan(layananId) ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.BrgId))
            .ToList();
        var pendingOrderRows = (_bffDal.ListPendingOrderQty(layananId) ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.BrgId) && x.Qty > 0)
            .ToList();
        var incomingMutasiRows = (_bffDal.ListIncomingMutasiQty(layananId) ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.BrgId) && x.Qty > 0)
            .ToList();

        var batchesByBrg = stokRows
            .GroupBy(x => x.BrgId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var barangLayananByBrg = barangLayananRows
            .GroupBy(x => x.BrgId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var pendingByBrg = pendingOrderRows
            .GroupBy(x => x.BrgId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty), StringComparer.OrdinalIgnoreCase);

        var incomingByBrg = incomingMutasiRows
            .GroupBy(x => x.BrgId.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty), StringComparer.OrdinalIgnoreCase);

        var metadataByBrg = BuildMetadataMap(stokRows, barangLayananRows, pendingOrderRows, incomingMutasiRows);

        var allBrgIds = metadataByBrg.Keys.ToList();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            allBrgIds = allBrgIds
                .Where(id =>
                {
                    var meta = metadataByBrg[id];
                    return meta.BrgName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                        || id.Contains(kw, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        var result = allBrgIds
            .Select(brgId =>
            {
                var meta = metadataByBrg[brgId];
                var rawBatches = batchesByBrg.TryGetValue(brgId, out var list) ? list : [];

                var batches = rawBatches
                    .Select(b => new LogistikMedisStokKanvasBatchResponse(
                        NoBatch: b.NoBatch?.Trim() ?? string.Empty,
                        TglEd: NormalizeTglEd(b.TglEd),
                        QtySisa: b.QtySisa,
                        Hpp: b.Hpp))
                    .OrderBy(b => string.IsNullOrEmpty(b.TglEd) ? "3000-01-01" : b.TglEd, StringComparer.Ordinal)
                    .ToList();

                var totalStok = batches.Sum(b => b.QtySisa);
                var hppDefault = batches.FirstOrDefault(b => b.Hpp > 0)?.Hpp
                    ?? batches.FirstOrDefault()?.Hpp
                    ?? 0m;
                var nearestTglEd = batches.FirstOrDefault(b => !string.IsNullOrEmpty(b.TglEd))?.TglEd ?? string.Empty;

                barangLayananByBrg.TryGetValue(brgId, out var brgLayanan);
                var minStok = brgLayanan?.MinStok ?? 0m;
                var maxStok = brgLayanan?.MaxStok ?? 0m;
                var pendingQty = pendingByBrg.TryGetValue(brgId, out var pQty) ? pQty : 0m;
                var incomingQty = incomingByBrg.TryGetValue(brgId, out var iQty) ? iQty : 0m;

                return new LogistikMedisStokKanvasResponse(
                    BrgId: brgId,
                    BrgName: meta.BrgName,
                    TotalStok: totalStok,
                    SatuanId: meta.SatuanId,
                    SatuanName: meta.SatuanName,
                    GroupRekDkId: meta.GroupRekDkId,
                    HppDefault: hppDefault,
                    MinStok: minStok,
                    MaxStok: maxStok,
                    NearestTglEd: nearestTglEd,
                    PendingRequestQty: pendingQty,
                    IncomingMutasiQty: incomingQty,
                    Batches: batches);
            })
            .OrderBy(x => x.BrgName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.BrgId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IEnumerable<LogistikMedisStokKanvasResponse>>(result);
    }

    private static Dictionary<string, BrgMetadata> BuildMetadataMap(
        IEnumerable<LogistikMedisStokBatchRowDto> stokRows,
        IEnumerable<LogistikMedisBarangLayananRowDto> barangLayananRows,
        IEnumerable<LogistikMedisAggQtyRowDto> pendingOrderRows,
        IEnumerable<LogistikMedisAggQtyRowDto> incomingMutasiRows)
    {
        var map = new Dictionary<string, BrgMetadata>(StringComparer.OrdinalIgnoreCase);

        void Upsert(string brgId, string brgName, string satuanId, string satuanName, string groupRekDkId)
        {
            var id = brgId.Trim();
            if (!map.ContainsKey(id))
            {
                map[id] = new BrgMetadata(
                    brgName?.Trim() ?? string.Empty,
                    satuanId?.Trim() ?? string.Empty,
                    satuanName?.Trim() ?? string.Empty,
                    groupRekDkId?.Trim() ?? string.Empty);
            }
        }

        foreach (var row in stokRows)
            Upsert(row.BrgId, row.BrgName, row.SatuanId, row.SatuanName, row.GroupRekDkId);

        foreach (var row in barangLayananRows)
            Upsert(row.BrgId, row.BrgName, row.SatuanId, row.SatuanName, row.GroupRekDkId);

        foreach (var row in pendingOrderRows)
            Upsert(row.BrgId, row.BrgName, row.SatuanId, row.SatuanName, row.GroupRekDkId);

        foreach (var row in incomingMutasiRows)
            Upsert(row.BrgId, row.BrgName, row.SatuanId, row.SatuanName, row.GroupRekDkId);

        return map;
    }

    private static string NormalizeTglEd(string? rawTglEd)
    {
        var trimmed = rawTglEd?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed) || trimmed == "3000-01-01" || trimmed == "1900-01-01")
            return string.Empty;

        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return trimmed;
    }

    private sealed record BrgMetadata(
        string BrgName,
        string SatuanId,
        string SatuanName,
        string GroupRekDkId
    );
}
