using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public static class InpatientAgePolicy
{
    public const string JENIS_INAP_UMUM = "1";
    public const string JENIS_INAP_OBSTETRI = "2";
    public const string JENIS_INAP_PERINATAL = "3";

    public static bool IsPerinatalAge(DateOnly tglLahir, DateOnly regDate)
    {
        if (tglLahir >= new DateOnly(3000, 1, 1) || regDate < tglLahir)
        {
            return false;
        }

        // Pasien berumur < 1 bulan jika regDate belum mencapai tanggal yang sama di bulan berikutnya
        return regDate < tglLahir.AddMonths(1);
    }

    public static string ResolveJenisInap(DateOnly tglLahir, DateOnly regDate, string? requestedJenisInapId)
    {
        if (IsPerinatalAge(tglLahir, regDate))
        {
            return JENIS_INAP_PERINATAL;
        }

        if (requestedJenisInapId == JENIS_INAP_OBSTETRI)
        {
            return JENIS_INAP_OBSTETRI;
        }

        return JENIS_INAP_UMUM;
    }
}
