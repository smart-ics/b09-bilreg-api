using Ardalis.GuardClauses;

namespace Bilreg.Domain.BrmContext.MutasiBerkasFeature;

public record VoidInfoType
{
    public bool IsVoid { get; init; }
    public string PetugasVoid { get; init; }
    public DateOnly TglVoid { get; init; }
    public TimeOnly JamVoid { get; init; }
    public string AlasanVoid { get; init; }

    public VoidInfoType(bool isVoid, string petugasVoid, DateOnly tglVoid, TimeOnly jamVoid, string alasanVoid = "")
    {
        IsVoid = isVoid;
        PetugasVoid = petugasVoid ?? string.Empty;
        TglVoid = tglVoid;
        JamVoid = jamVoid;
        AlasanVoid = alasanVoid ?? string.Empty;
    }

    public static VoidInfoType Default =>
        new(false, string.Empty, new DateOnly(3000, 1, 1), new TimeOnly(0, 0, 0), string.Empty);

    public static VoidInfoType CreateVoid(string petugasVoid, DateOnly tglVoid, TimeOnly jamVoid, string alasanVoid = "")
    {
        Guard.Against.NullOrWhiteSpace(petugasVoid, nameof(petugasVoid));
        return new VoidInfoType(true, petugasVoid, tglVoid, jamVoid, alasanVoid);
    }

    public static VoidInfoType CreateVoid(string petugasVoid, DateTime timestamp, string alasanVoid = "")
    {
        Guard.Against.NullOrWhiteSpace(petugasVoid, nameof(petugasVoid));
        return CreateVoid(petugasVoid, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp), alasanVoid);
    }
}
