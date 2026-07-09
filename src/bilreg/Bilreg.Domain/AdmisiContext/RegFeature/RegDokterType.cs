using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public class RegDokterType
{
    public PpaReff Dokter { get; }
    public bool IsPrimer { get; private set; }
    public DateOnly AssignDate { get; }
    public DateOnly? ReleaseDate { get; private set; }

    public bool IsActive => ReleaseDate is null;

    internal RegDokterType(PpaReff dokter, DateOnly assignDate, bool isPrimer = false)
    {
        Dokter = dokter;
        AssignDate = assignDate;
        IsPrimer = isPrimer;
    }

    // BR-REG-009: clear IsPrimer on release so only active assignments can be DPJP.
    // AssignDate/ReleaseDate retain sufficient audit history.
    internal void Release(DateOnly releaseDate)
    {
        ReleaseDate = releaseDate;
        IsPrimer = false;
    }

    internal void SetPrimer(bool isPrimer) => IsPrimer = isPrimer;
}
