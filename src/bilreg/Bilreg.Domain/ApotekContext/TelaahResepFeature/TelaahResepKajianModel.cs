using Ardalis.GuardClauses;

namespace Bilreg.Domain.ApotekContext.TelaahResepFeature;

public class TelaahResepKajianModel
{
    private static readonly HashSet<string> PositivePolarityIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "1.1", "1.2", "1.3", "1.4", "2.1", "2.2", "2.3", "2.4", "3.1"
    };

    public TelaahResepKajianModel(
        int noUrut,
        string kajianResepId,
        string kajianResepName,
        string grupKajianResepId,
        string grupKajianResepName,
        bool isYes,
        string catatanKajian)
    {
        Guard.Against.NegativeOrZero(noUrut, nameof(noUrut));
        Guard.Against.NullOrWhiteSpace(kajianResepId, nameof(kajianResepId));
        NoUrut = noUrut;
        KajianResepId = kajianResepId;
        KajianResepName = kajianResepName ?? string.Empty;
        GrupKajianResepId = grupKajianResepId ?? string.Empty;
        GrupKajianResepName = grupKajianResepName ?? string.Empty;
        IsYes = isYes;
        CatatanKajian = catatanKajian ?? string.Empty;
    }

    public static bool IsPositivePolarity(string kajianResepId)
        => PositivePolarityIds.Contains(kajianResepId?.Trim() ?? string.Empty);

    public static TelaahResepKajianModel Create(
        int noUrut,
        string kajianResepId,
        string kajianResepName,
        string grupKajianResepId,
        string grupKajianResepName)
    {
        var defaultIsYes = IsPositivePolarity(kajianResepId);
        return new TelaahResepKajianModel(
            noUrut,
            kajianResepId,
            kajianResepName,
            grupKajianResepId,
            grupKajianResepName,
            defaultIsYes,
            string.Empty);
    }

    public static TelaahResepKajianModel Rehydrate(
        int noUrut,
        string kajianResepId,
        string kajianResepName,
        string grupKajianResepId,
        string grupKajianResepName,
        bool isYes,
        string catatanKajian)
        => new(noUrut, kajianResepId, kajianResepName, grupKajianResepId, grupKajianResepName, isYes, catatanKajian);

    public TelaahResepKajianModel WithAnswer(bool isYes, string catatanKajian)
        => new(NoUrut, KajianResepId, KajianResepName, GrupKajianResepId, GrupKajianResepName, isYes, catatanKajian ?? string.Empty);

    public static IReadOnlyList<TelaahResepKajianModel> DefaultCatalog() => new List<TelaahResepKajianModel>
    {
        Create(1, "1.1", "Apakah nama, umur, jenis kelamin, berat badan dan tinggi badan pasien sudah sesuai?", "1", "Persyaratan Administrasi"),
        Create(2, "1.2", "Apakah nama, nomor ijin, alamat dan paraf dokter sudah sesuai?", "1", "Persyaratan Administrasi"),
        Create(3, "1.3", "Apakah tanggal resep sudah sesuai?", "1", "Persyaratan Administrasi"),
        Create(4, "1.4", "Apakah ruangan/unit asal resep sudah sesuai?", "1", "Persyaratan Administrasi"),
        Create(5, "2.1", "Apakah nama obat, bentuk dan kekuatan sediaan sudah sesuai?", "2", "Persyaratan Farmasetik"),
        Create(6, "2.2", "Apakah dosis dan jumlah obat sudah sesuai?", "2", "Persyaratan Farmasetik"),
        Create(7, "2.3", "Apakah stabilitas obat sudah sesuai?", "2", "Persyaratan Farmasetik"),
        Create(8, "2.4", "Apakah aturan dan cara penggunaan obat sudah sesuai?", "2", "Persyaratan Farmasetik"),
        Create(9, "3.1", "Apakah ketepatan indikasi, dosis, dan waktu penggunaan obat sudah sesuai?", "3", "Persyaratan Klinis"),
        Create(10, "3.2", "Apakah terdapat duplikasi pengobatan?", "3", "Persyaratan Klinis"),
        Create(11, "3.3", "Apakah terdapat alergi dan reaksi obat yang tidak dikehendaki (ROTD)?", "3", "Persyaratan Klinis"),
        Create(12, "3.4", "Apakah terdapat kontraindikasi pengobatan?", "3", "Persyaratan Klinis"),
        Create(13, "3.5", "Apakah terdapat dampak interaksi obat?", "3", "Persyaratan Klinis")
    };

    public int NoUrut { get; }
    public string KajianResepId { get; }
    public string KajianResepName { get; }
    public string GrupKajianResepId { get; }
    public string GrupKajianResepName { get; }
    public bool IsYes { get; }
    public string CatatanKajian { get; }

    public bool DefaultIsYes => IsPositivePolarity(KajianResepId);
    public bool RequiresNote => IsYes != DefaultIsYes;
    public bool HasRequiredNote => !RequiresNote || !string.IsNullOrWhiteSpace(CatatanKajian);
    public bool IsCompliant => HasRequiredNote;
}
