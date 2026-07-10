using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Domain.AdmisiContext.RegFeature;

public class RegDokterType
{
    internal RegDokterType(
        PpaReff dokter,
        DokterRoleEnum role,
        DpjpResponsibilityEnum? dpjpResponsibility,
        DateOnly assignDate,
        DateOnly? releaseDate)
    {
        Guard.Against.Null(dokter);
        EnsureDpjpResponsibility(role, dpjpResponsibility);
        if (releaseDate.HasValue)
            EnsureReleaseDateValid(assignDate, releaseDate.Value);

        Dokter = dokter;
        DokterRole = role;
        DpjpResponsibility = dpjpResponsibility;
        AssignDate = assignDate;
        ReleaseDate = releaseDate;
    }

    public PpaReff Dokter { get; init; }
    public DokterRoleEnum DokterRole { get; init; }
    public DpjpResponsibilityEnum? DpjpResponsibility { get; init; }
    public DateOnly AssignDate { get; init; }
    public DateOnly? ReleaseDate { get; private set; }

    public bool IsActive => ReleaseDate is null;

    public static RegDokterType Rehydrate(
        PpaReff dokter,
        DokterRoleEnum role,
        DpjpResponsibilityEnum? dpjpResponsibility,
        DateOnly assignDate,
        DateOnly? releaseDate)
        => new(dokter, role, dpjpResponsibility, assignDate, releaseDate);

    internal void Release(DateOnly releaseDate)
    {
        if (!IsActive)
            throw new InvalidOperationException(
                $"Penugasan dokter {Dokter.PpaName} sudah dilepas.");

        EnsureReleaseDateValid(AssignDate, releaseDate);
        ReleaseDate = releaseDate;
    }

    private static void EnsureDpjpResponsibility(
        DokterRoleEnum role,
        DpjpResponsibilityEnum? dpjpResponsibility)
    {
        if (role == DokterRoleEnum.Dpjp)
        {
            if (dpjpResponsibility is null)
                throw new ArgumentException("DPJP harus memiliki tanggung jawab Primary atau Secondary.");
            return;
        }

        if (dpjpResponsibility is not null)
            throw new ArgumentException(
                $"Peran {role} tidak boleh memiliki tanggung jawab DPJP.");
    }

    private static void EnsureReleaseDateValid(DateOnly assignDate, DateOnly releaseDate)
    {
        if (releaseDate < assignDate)
            throw new ArgumentException("Tanggal lepas tidak boleh lebih awal dari tanggal penugasan.");
    }
}

public enum DokterRoleEnum
{
    Dpjp,
    Konsulen,
    Residen
}

public enum DpjpResponsibilityEnum
{
    Primary,
    Secondary
}
