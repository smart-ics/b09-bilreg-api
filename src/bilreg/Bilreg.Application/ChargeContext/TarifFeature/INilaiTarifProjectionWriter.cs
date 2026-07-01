using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Application.ChargeContext.TarifFeature;

/// <summary>
/// Persistence helper for Phase-3 publish: upserts operational BILRG_NilaiTarif projection rows
/// without orchestrating policy workflow.
/// </summary>
public interface INilaiTarifProjectionWriter
{
    /// <summary>
    /// Upserts one variant into BILRG_NilaiTarif*; reuses NilaiTarifId when composite key exists.
    /// </summary>
    /// <returns>The NilaiTarifId written.</returns>
    string Upsert(NilaiTarifType projection, string sourcePolicyId);
}
