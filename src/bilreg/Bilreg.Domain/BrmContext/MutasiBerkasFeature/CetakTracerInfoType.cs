using Ardalis.GuardClauses;

namespace Bilreg.Domain.BrmContext.MutasiBerkasFeature;

public record CetakTracerInfoType
{
    public int FrekuensiCetak { get; init; }
    public string PetugasCetak { get; init; }
    public DateOnly TglCetak { get; init; }
    public TimeOnly JamCetak { get; init; }

    public bool IsPrinted => FrekuensiCetak > 0;

    public CetakTracerInfoType(int frekuensiCetak, string petugasCetak, DateOnly tglCetak, TimeOnly jamCetak)
    {
        if (frekuensiCetak < 0)
            throw new ArgumentException("Frekuensi cetak tidak boleh negatif.", nameof(frekuensiCetak));

        FrekuensiCetak = frekuensiCetak;
        PetugasCetak = petugasCetak ?? string.Empty;
        TglCetak = tglCetak;
        JamCetak = jamCetak;
    }

    public static CetakTracerInfoType Default =>
        new(0, string.Empty, new DateOnly(3000, 1, 1), new TimeOnly(0, 0, 0));

    public CetakTracerInfoType Cetak(string petugasCetak, DateOnly tglCetak, TimeOnly jamCetak)
    {
        Guard.Against.NullOrWhiteSpace(petugasCetak, nameof(petugasCetak));
        return new CetakTracerInfoType(FrekuensiCetak + 1, petugasCetak, tglCetak, jamCetak);
    }

    public CetakTracerInfoType Cetak(string petugasCetak, DateTime timestamp)
    {
        Guard.Against.NullOrWhiteSpace(petugasCetak, nameof(petugasCetak));
        return Cetak(petugasCetak, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp));
    }
}
