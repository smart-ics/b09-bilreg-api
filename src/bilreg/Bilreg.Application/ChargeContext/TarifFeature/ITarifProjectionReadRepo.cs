using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifProjectionReadRepo
{
    IReadOnlyList<NilaiTarifProjectionRow> ListAllProjection();

    TarifProjectionSummary GetProjectionSummary();

    TarifProjectionConsistencyReport GetConsistencyReport();

    (DateTime? PublishedAt, string? PolicyId, string? PublishLogId) GetLastPublish();
}

public record NilaiTarifProjectionRow(
    string NilaiTarifId,
    string TarifId,
    string TipeTarifId,
    string KelasId,
    decimal Nilai,
    string SourcePolicyId,
    IReadOnlyList<NilaiTarifProjectionKomponenRow> Komponen);

public record NilaiTarifProjectionKomponenRow(
    int NoUrut,
    string KomponenId,
    decimal Nilai);
