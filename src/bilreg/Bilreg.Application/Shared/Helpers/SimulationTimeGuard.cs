using Bilreg.Application.Shared;
using Microsoft.Extensions.Logging;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.Shared.Helpers;

/// <summary>
/// Helper untuk menjaga integritas waktu transaksi saat sistem berjalan dalam Mode Simulasi (BusinessDateMode.Fixed).
/// Jika waktu kiriman klien memiliki tanggal kalender yang berbeda dengan tanggal simulasi aktif,
/// waktu transaksi dikoreksi secara otomatis ke _tglJamProvider.Now dan peringatan dicatat pada log.
/// </summary>
public static class SimulationTimeGuard
{
    public static DateTime Resolve(
        DateTime? requestTime,
        ITglJamProvider provider,
        ILogger? logger = null,
        string operationName = "Transaction")
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        // Jika requestTime null atau default (DateTime.MinValue), gunakan waktu bisnis saat ini
        if (!requestTime.HasValue || requestTime.Value == default)
        {
            return provider.Now;
        }

        // Cek apakah provider mendukung pengecekan status simulasi
        if (provider is IBusinessDateStatus status && status.IsSimulation)
        {
            var fixedDate = status.FixedDate;
            if (fixedDate.HasValue)
            {
                var clientDate = DateOnly.FromDateTime(requestTime.Value);
                if (clientDate != fixedDate.Value)
                {
                    logger?.LogWarning(
                        "[{Operation}] Waktu kiriman klien ({ClientTime:yyyy-MM-dd HH:mm:ss}) berbeda dengan tanggal simulasi aktif ({SimDate:yyyy-MM-dd}). Mengoreksi ke waktu bisnis provider ({ResolvedTime:yyyy-MM-dd HH:mm:ss}).",
                        operationName,
                        requestTime.Value,
                        fixedDate.Value,
                        provider.Now);

                    return provider.Now;
                }
            }
        }

        return requestTime.Value;
    }
}
