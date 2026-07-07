using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.ReservationFeature;
using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.Shared;

internal static class AdmisiRanapSupport
{
    public static OpnameRequestModel LoadOpnameRequest(IOpnameRequestRepo repo, IOpnameRequestKey key) =>
        repo.LoadEntity(key).GetValueOrThrow($"Opname Request '{key.OpnameRequestId}' tidak ditemukan.");

    public static ReservationModel LoadReservation(IReservationRepo repo, IReservationKey key) =>
        repo.LoadEntity(key).GetValueOrThrow($"Reservation '{key.ReservationId}' tidak ditemukan.");

    public static AdmissionModel LoadAdmission(IAdmissionRepo repo, IRegKey key) =>
        repo.LoadEntity(key).GetValueOrThrow($"Admission '{key.RegId}' tidak ditemukan.");

    public static WaitingListModel LoadWaitingList(IWaitingListRepo repo, IWaitingListKey key) =>
        repo.LoadEntity(key).GetValueOrThrow($"Waiting List '{key.WaitingListId}' tidak ditemukan.");

    public static PasienReff LoadPasienReff(IPasienRepo pasienRepo, string pasienId)
    {
        Guard.Against.NullOrWhiteSpace(pasienId);
        return pasienRepo.LoadEntity(PasienModel.Key(pasienId))
            .GetValueOrThrow($"Pasien '{pasienId}' tidak ditemukan.")
            .ToReff();
    }

    public static PpaReff LoadPpaReff(IPpaRepo ppaRepo, string dokterId)
    {
        Guard.Against.NullOrWhiteSpace(dokterId);
        return ppaRepo.LoadEntity(PpaType.Key(dokterId))
            .GetValueOrThrow($"Dokter '{dokterId}' tidak ditemukan.")
            .ToReff();
    }

    public static KelasReff LoadKelasReff(IKelasRepo kelasRepo, string kelasId)
    {
        Guard.Against.NullOrWhiteSpace(kelasId);
        return kelasRepo.LoadEntity(KelasType.Key(kelasId))
            .GetValueOrThrow($"Kelas '{kelasId}' tidak ditemukan.")
            .ToReff();
    }

    public static BangsalReff LoadBangsalReff(IBangsalRepo bangsalRepo, string bangsalId)
    {
        Guard.Against.NullOrWhiteSpace(bangsalId);
        return bangsalRepo.LoadEntity(BangsalType.Key(bangsalId))
            .GetValueOrThrow($"Bangsal '{bangsalId}' tidak ditemukan.")
            .ToReff();
    }

    public static void EnsureNoActiveAdmission(IAdmissionRepo admissionRepo, string pasienId)
    {
        Guard.Against.NullOrWhiteSpace(pasienId);

        var active = admissionRepo
            .ListData(new AdmissionListFilter(PasienId: pasienId))
            .Where(a => a.AdmissionStatus is not AdmissionStatusEnum.Completed
                and not AdmissionStatusEnum.Cancelled)
            .ToList();

        if (active.Count > 0)
            throw new InvalidOperationException(
                $"Pasien '{pasienId}' masih memiliki admission aktif ({active[0].RegId}).");
    }

    public static void EnsurePasienMatch(PasienReff expected, PasienReff actual, string context)
    {
        if (!string.Equals(expected.PasienId, actual.PasienId, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"{context}: pasien '{actual.PasienId}' tidak sesuai dengan '{expected.PasienId}'.");
    }
}
