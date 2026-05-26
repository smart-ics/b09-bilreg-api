using Bilreg.Application.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class TarifProjectionReadRepo : ITarifProjectionReadRepo
{
    private readonly INilaiTarifDal _nilaiTarifDal;
    private readonly INilaiTarifKompDal _nilaiTarifKompDal;
    private readonly ITarifPublishLogDal _tarifPublishLogDal;

    public TarifProjectionReadRepo(
        INilaiTarifDal nilaiTarifDal,
        INilaiTarifKompDal nilaiTarifKompDal,
        ITarifPublishLogDal tarifPublishLogDal)
    {
        _nilaiTarifDal = nilaiTarifDal;
        _nilaiTarifKompDal = nilaiTarifKompDal;
        _tarifPublishLogDal = tarifPublishLogDal;
    }

    public IReadOnlyList<NilaiTarifProjectionRow> ListAllProjection()
    {
        var headers = _nilaiTarifDal.ListAllHeaders()?.ToList() ?? [];
        var komponenByHeader = (_nilaiTarifKompDal.ListAll() ?? [])
            .GroupBy(k => k.NilaiTarifId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return headers.Select(h =>
        {
            var lines = komponenByHeader.TryGetValue(h.NilaiTarifId, out var komp)
                ? komp.Select(k => new NilaiTarifProjectionKomponenRow(k.NoUrut, k.KomponenId, k.Nilai)).ToList()
                : [];

            return new NilaiTarifProjectionRow(
                h.NilaiTarifId,
                h.TarifId,
                h.TipeTarifId,
                h.KelasId,
                h.Nilai,
                h.SourcePolicyId ?? "",
                lines);
        }).ToList();
    }

    public TarifProjectionSummary GetProjectionSummary()
    {
        var row = _nilaiTarifDal.GetProjectionSummary();
        return new TarifProjectionSummary(
            row.TotalVariantCount,
            row.ImportOnlyCount,
            row.PolicySourcedCount);
    }

    public TarifProjectionConsistencyReport GetConsistencyReport()
    {
        var row = _nilaiTarifDal.GetConsistencyCounts();
        var isHealthy = row.DuplicateVariantKeyCount == 0
            && row.HeadersWithoutKomponenCount == 0
            && row.OrphanSourcePolicyIdCount == 0;

        return new TarifProjectionConsistencyReport(
            row.DuplicateVariantKeyCount,
            row.HeadersWithoutKomponenCount,
            row.OrphanSourcePolicyIdCount,
            isHealthy);
    }

    public (DateTime? PublishedAt, string? PolicyId, string? PublishLogId) GetLastPublish()
    {
        var row = _tarifPublishLogDal.GetLastPublish();
        if (row is null)
            return (null, null, null);

        return (row.PublishedDate, row.TarifPolicyId, row.PublishLogId);
    }
}
