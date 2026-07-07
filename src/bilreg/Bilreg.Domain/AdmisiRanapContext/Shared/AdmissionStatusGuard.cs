using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;

namespace Bilreg.Domain.AdmisiRanapContext.Shared;

public static class AdmissionStatusGuard
{
    public static bool CanEnterWaitingList(AdmissionStatusEnum status) =>
        status is AdmissionStatusEnum.Admitted
            or AdmissionStatusEnum.Updated
            or AdmissionStatusEnum.Waiting;

    public static void EnsureCanEnterWaitingList(AdmissionStatusEnum status)
    {
        if (!CanEnterWaitingList(status))
            throw new InvalidOperationException(
                $"Pasien belum dalam status admisi yang valid untuk Waiting List (status saat ini: {status}).");
    }
}
