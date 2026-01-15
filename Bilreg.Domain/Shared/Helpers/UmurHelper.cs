namespace Bilreg.Domain.Shared.Helpers;

public static class UmurHelper
{
    public static string HitungUmur(DateOnly tglLahir, DateOnly? referensi = null)
    {
        var today = referensi ?? DateOnly.FromDateTime(DateTime.Today);

        if (tglLahir > today)
            return "0 tahun, 0 bulan, 0 hari";

        int tahun = today.Year - tglLahir.Year;
        int bulan = today.Month - tglLahir.Month;
        int hari = today.Day - tglLahir.Day;

        if (hari < 0)
        {
            bulan--;
            var prevMonth = today.AddMonths(-1);
            hari += DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        }

        if (bulan < 0)
        {
            tahun--;
            bulan += 12;
        }

        return $"{tahun} tahun, {bulan} bulan, {hari} hari";
    }
}

