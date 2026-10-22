using Bilreg.Domain.PaymentContext.TataRekeningFeature;

//  resharper disable InconsistentNaming

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

public record TataRekeningCancellationLogDto(
    string fs_kd_log,
    string fs_kd_reg,
    string fs_user_spv,
    string fs_user_aksi,
    DateTime fd_tgl_batal,
    string fs_alasan_batal,
    string fs_status_sebelum,
    string fs_status_setelah,
    string fs_ip_address)
{
    public static TataRekeningCancellationLogDto FromModel(TataRekeningCancellationLog model)
    {
        return new TataRekeningCancellationLogDto(
            model.LogId,
            model.RegId,
            model.SupervisorUserId,
            model.UserId,
            model.CancelledAt,
            model.Reason,
            model.StatusBefore,
            model.StatusAfter,
            model.ClientIpAddress);
    }
}
