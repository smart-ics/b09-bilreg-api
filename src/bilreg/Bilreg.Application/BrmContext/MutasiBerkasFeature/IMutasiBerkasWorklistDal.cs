namespace Bilreg.Application.BrmContext.MutasiBerkasFeature;

public record MutasiBerkasWorklistFilter(
    string? TglPraktek = null,
    string? LayananId = null,
    string? SesiPoli = null,
    int? Status = null,
    string? GedungLantai = null
);

public record MutasiBerkasWorklistItemDto
{
    public string MrId { get; init; } = string.Empty;
    public string PasienName { get; init; } = string.Empty;
    public string AlamatPasien { get; init; } = string.Empty;
    public string TglLahir { get; init; } = string.Empty;
    public string JenisKelamin { get; init; } = string.Empty;
    public string TeleponPasien { get; init; } = string.Empty;
    public string MutasiId { get; init; } = string.Empty;
    public string TrsBuatId { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public string TglMutasi { get; init; } = string.Empty;
    public string JamMutasi { get; init; } = string.Empty;
    public string DokterId { get; init; } = string.Empty;
    public string DokterName { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string LayananName { get; init; } = string.Empty;
    public string TglPraktek { get; init; } = string.Empty;
    public string JamPraktek { get; init; } = string.Empty;
    public string SesiPoliId { get; init; } = string.Empty;
    public string SesiPoliName { get; init; } = string.Empty;
    public int Status { get; init; }
    public int FrekuensiCetak { get; init; }
    public int NoAntrian { get; init; }
    public string LokasiBerkasId { get; init; } = string.Empty;
    public string LokasiBerkasName { get; init; } = string.Empty;
}

public record MutasiBerkasHistoriItemDto
{
    public string MutasiId { get; init; } = string.Empty;
    public string TrsBuatId { get; init; } = string.Empty;
    public string MrId { get; init; } = string.Empty;
    public string TglMutasi { get; init; } = string.Empty;
    public string JamMutasi { get; init; } = string.Empty;
    public string DokterId { get; init; } = string.Empty;
    public string DokterName { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string LayananName { get; init; } = string.Empty;
    public string TglPraktek { get; init; } = string.Empty;
    public string JamPraktek { get; init; } = string.Empty;
    public string SesiPoliId { get; init; } = string.Empty;
    public int Status { get; init; }
    public string PetugasId { get; init; } = string.Empty;
    public int FrekuensiCetak { get; init; }
    public string PetugasCetak { get; init; } = string.Empty;
    public string TglCetak { get; init; } = string.Empty;
    public string JamCetak { get; init; } = string.Empty;
    public bool IsVoid { get; init; }
    public string TglVoid { get; init; } = string.Empty;
    public string JamVoid { get; init; } = string.Empty;
    public string PetugasVoid { get; init; } = string.Empty;
}

public interface IMutasiBerkasWorklistDal
{
    IEnumerable<MutasiBerkasWorklistItemDto> QueryWorklist(MutasiBerkasWorklistFilter filter);
    IEnumerable<MutasiBerkasHistoriItemDto> QueryHistori(string mrId);
}
