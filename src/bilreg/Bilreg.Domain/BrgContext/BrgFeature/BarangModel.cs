using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;

namespace Bilreg.Domain.BrgContext.BrgFeature;

public record BarangModel : IBrg
{
    #region CREATION
    public BarangModel(
        string brgId,
        string brgName,
        bool isAktif,
        string ketBarang,
        GroupRekReff groupRek,
        GroupRekDkType groupRekDk,
        PabrikReff pabrik,
        SatuanType satuanKecil,
        SatuanType satuanBesar,
        int konversi)
    {
        BrgId = brgId;
        BrgName = brgName;
        IsAktif = isAktif;
        KetBarang = ketBarang ?? string.Empty;
        GroupRek = groupRek;
        GroupRekDk = groupRekDk;
        Pabrik = pabrik;
        SatuanKecil = satuanKecil;
        SatuanBesar = satuanBesar;
        Konversi = konversi;
    }

    public static BarangModel Create(
        string brgId,
        string brgName,
        bool isAktif,
        string ketBarang,
        GroupRekReff groupRek,
        GroupRekDkType groupRekDk,
        PabrikReff pabrik,
        SatuanType satuanKecil,
        SatuanType satuanBesar,
        int konversi)
    {
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        Guard.Against.NullOrWhiteSpace(brgName, nameof(brgName));
        Guard.Against.Null(groupRek, nameof(groupRek));
        Guard.Against.Null(groupRekDk, nameof(groupRekDk));
        Guard.Against.Null(pabrik, nameof(pabrik));
        Guard.Against.Null(satuanKecil, nameof(satuanKecil));
        Guard.Against.Null(satuanBesar, nameof(satuanBesar));

        if (konversi < 1)
        {
            throw new ArgumentException("Konversi harus >= 1.", nameof(konversi));
        }

        bool isSameSatuan = string.Equals(satuanKecil.SatuanId, satuanBesar.SatuanId, StringComparison.OrdinalIgnoreCase);

        if (isSameSatuan && konversi != 1)
        {
            throw new ArgumentException("Jika SatuanBesar sama dengan SatuanKecil, konversi harus 1.", nameof(konversi));
        }

        if (!isSameSatuan && konversi <= 1)
        {
            throw new ArgumentException("Jika SatuanBesar berbeda dari SatuanKecil, konversi harus > 1.", nameof(konversi));
        }

        return new BarangModel(
            brgId,
            brgName,
            isAktif,
            ketBarang ?? string.Empty,
            groupRek,
            groupRekDk,
            pabrik,
            satuanKecil,
            satuanBesar,
            konversi);
    }

    public static BarangModel Default => new(
        "-",
        "-",
        false,
        "-",
        GroupRekType.Default.ToReff(),
        GroupRekDkType.Default,
        new PabrikReff("-", "-"),
        SatuanType.Default,
        SatuanType.Default,
        1);

    public static IBrgKey Key(string id) => new BrgKey(id);
    #endregion

    #region PROPERTIES
    public string BrgId { get; init; }
    public string BrgName { get; init; }
    public bool IsAktif { get; init; }
    public string KetBarang { get; init; }
    public GroupRekReff GroupRek { get; init; }
    public GroupRekDkType GroupRekDk { get; init; }
    public PabrikReff Pabrik { get; init; }
    public SatuanType SatuanKecil { get; init; }
    public SatuanType SatuanBesar { get; init; }
    public int Konversi { get; init; }

    public IEnumerable<BrgSatuanType> ListSatuan
    {
        get
        {
            if (string.Equals(SatuanKecil.SatuanId, SatuanBesar.SatuanId, StringComparison.OrdinalIgnoreCase))
            {
                return new[] { new BrgSatuanType(SatuanKecil, 1) };
            }

            return new[]
            {
                new BrgSatuanType(SatuanKecil, 1),
                new BrgSatuanType(SatuanBesar, Konversi)
            };
        }
    }
    #endregion

    #region BEHAVIOUR
    public BrgReff ToReff() => new(BrgId, BrgName);

    public BarangModel SetAktif(bool isAktif) => this with { IsAktif = isAktif };
    #endregion
}
