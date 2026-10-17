using Ardalis.GuardClauses;

namespace Bilreg.Domain.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasModel : IMutasiBerkasKey
{
    #region PROPERTIES
    public string MutasiBerkasId { get; private set; }
    public string TrsBuatId { get; private set; }
    public string PasienId { get; private set; }
    public DateOnly TglMutasi { get; private set; }
    public TimeOnly JamMutasi { get; private set; }
    public string PetugasId { get; private set; }
    public JadwalPelayananType JadwalPelayanan { get; private set; }
    public MutasiBerkasStatusEnum Status { get; private set; }
    public VolumeBerkasType Volume { get; private set; }
    public CetakTracerInfoType CetakTracerInfo { get; private set; }
    public VoidInfoType VoidInfo { get; private set; }

    public bool IsVoid => VoidInfo.IsVoid || Status == MutasiBerkasStatusEnum.Void;
    #endregion

    #region CREATION
    public MutasiBerkasModel(
        string mutasiBerkasId,
        string trsBuatId,
        string pasienId,
        DateOnly tglMutasi,
        TimeOnly jamMutasi,
        string petugasId,
        JadwalPelayananType jadwalPelayanan,
        MutasiBerkasStatusEnum status,
        VolumeBerkasType volume,
        CetakTracerInfoType cetakTracerInfo,
        VoidInfoType voidInfo)
    {
        MutasiBerkasId = mutasiBerkasId ?? string.Empty;
        TrsBuatId = trsBuatId ?? string.Empty;
        PasienId = pasienId ?? string.Empty;
        TglMutasi = tglMutasi;
        JamMutasi = jamMutasi;
        PetugasId = petugasId ?? string.Empty;
        JadwalPelayanan = jadwalPelayanan ?? JadwalPelayananType.Default;
        Status = status;
        Volume = volume ?? VolumeBerkasType.Default;
        CetakTracerInfo = cetakTracerInfo ?? CetakTracerInfoType.Default;
        VoidInfo = voidInfo ?? VoidInfoType.Default;
    }

    public static MutasiBerkasModel Default => new(
        "-",
        "-",
        "-",
        new DateOnly(3000, 1, 1),
        new TimeOnly(0, 0, 0),
        "-",
        JadwalPelayananType.Default,
        MutasiBerkasStatusEnum.Permintaan,
        VolumeBerkasType.Default,
        CetakTracerInfoType.Default,
        VoidInfoType.Default);

    public static IMutasiBerkasKey Key(string id) => new MutasiBerkasKey(id);

    public static MutasiBerkasModel CreatePermintaan(
        string mutasiBerkasId,
        string trsBuatId,
        string pasienId,
        DateOnly tglMutasi,
        TimeOnly jamMutasi,
        string petugasId,
        JadwalPelayananType jadwalPelayanan,
        VolumeBerkasType? volume = null)
    {
        Guard.Against.NullOrWhiteSpace(mutasiBerkasId, nameof(mutasiBerkasId));
        Guard.Against.NullOrWhiteSpace(trsBuatId, nameof(trsBuatId));
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));
        Guard.Against.Null(jadwalPelayanan, nameof(jadwalPelayanan));

        return new MutasiBerkasModel(
            mutasiBerkasId,
            trsBuatId,
            pasienId,
            tglMutasi,
            jamMutasi,
            petugasId,
            jadwalPelayanan,
            MutasiBerkasStatusEnum.Permintaan,
            volume ?? VolumeBerkasType.Default,
            CetakTracerInfoType.Default,
            VoidInfoType.Default);
    }

    public static MutasiBerkasModel CreateKirim(
        string mutasiBerkasId,
        string trsBuatId,
        string pasienId,
        DateOnly tglMutasi,
        TimeOnly jamMutasi,
        string petugasId,
        JadwalPelayananType jadwalPelayanan,
        VolumeBerkasType volume)
    {
        Guard.Against.NullOrWhiteSpace(mutasiBerkasId, nameof(mutasiBerkasId));
        Guard.Against.NullOrWhiteSpace(trsBuatId, nameof(trsBuatId));
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));
        Guard.Against.Null(jadwalPelayanan, nameof(jadwalPelayanan));
        Guard.Against.Null(volume, nameof(volume));

        if (volume.TotalPinjam <= 0)
            throw new ArgumentException("Jumlah berkas yang dipinjam harus lebih dari 0.", nameof(volume));

        return new MutasiBerkasModel(
            mutasiBerkasId,
            trsBuatId,
            pasienId,
            tglMutasi,
            jamMutasi,
            petugasId,
            jadwalPelayanan,
            MutasiBerkasStatusEnum.Dispatched,
            volume,
            CetakTracerInfoType.Default,
            VoidInfoType.Default);
    }
    #endregion

    #region BEHAVIOUR
    public void SetId(string mutasiBerkasId)
    {
        Guard.Against.NullOrWhiteSpace(mutasiBerkasId, nameof(mutasiBerkasId));
        MutasiBerkasId = mutasiBerkasId;
    }

    public void UpdateJadwalPelayanan(JadwalPelayananType jadwal)
    {
        Guard.Against.Null(jadwal, nameof(jadwal));
        if (IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        JadwalPelayanan = jadwal;
    }

    public void Kirim(string petugasId, DateOnly tglMutasi, TimeOnly jamMutasi, VolumeBerkasType volume)
    {
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));
        Guard.Against.Null(volume, nameof(volume));

        if (IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        if (Status != MutasiBerkasStatusEnum.Permintaan && Status != MutasiBerkasStatusEnum.Missing)
            throw new InvalidOperationException($"Mutasi berkas tidak dapat dikirim dari status {Status}. Status harus Permintaan atau Missing.");

        if (volume.TotalPinjam <= 0)
            throw new ArgumentException("Jumlah volume berkas yang dikirim harus lebih dari 0.", nameof(volume));

        PetugasId = petugasId;
        TglMutasi = tglMutasi;
        JamMutasi = jamMutasi;
        Volume = volume;
        Status = MutasiBerkasStatusEnum.Dispatched;
    }

    public void Kirim(string petugasId, DateTime timestamp, VolumeBerkasType volume) =>
        Kirim(petugasId, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp), volume);

    public void Terima(string petugasId, DateOnly tglMutasi, TimeOnly jamMutasi)
    {
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));

        if (IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        if (Status != MutasiBerkasStatusEnum.Dispatched)
            throw new InvalidOperationException($"Mutasi berkas tidak dapat diterima karena status saat ini {Status} (harus Dispatched).");

        PetugasId = petugasId;
        TglMutasi = tglMutasi;
        JamMutasi = jamMutasi;
        Status = MutasiBerkasStatusEnum.Received;
    }

    public void Terima(string petugasId, DateTime timestamp) =>
        Terima(petugasId, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp));

    public void Kembali(string petugasId, DateOnly tglMutasi, TimeOnly jamMutasi, decimal qtyKembaliRi, decimal qtyKembaliRj)
    {
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));

        if (IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        if (Status != MutasiBerkasStatusEnum.Dispatched && Status != MutasiBerkasStatusEnum.Received)
            throw new InvalidOperationException($"Mutasi berkas tidak dapat dikembalikan karena status saat ini {Status} (harus Dispatched atau Received).");

        if (qtyKembaliRi < 0 || qtyKembaliRj < 0)
            throw new ArgumentException("Jumlah volume berkas kembali tidak boleh negatif.");

        if (qtyKembaliRi + qtyKembaliRj <= 0)
            throw new ArgumentException("Jumlah volume berkas kembali harus lebih dari 0.");

        if (qtyKembaliRi > Volume.QtyPinjamRi)
            throw new ArgumentException("Jumlah berkas rawat inap kembali melebihi jumlah pinjam.", nameof(qtyKembaliRi));

        if (qtyKembaliRj > Volume.QtyPinjamRj)
            throw new ArgumentException("Jumlah berkas rawat jalan kembali melebihi jumlah pinjam.", nameof(qtyKembaliRj));

        PetugasId = petugasId;
        TglMutasi = tglMutasi;
        JamMutasi = jamMutasi;
        Volume = new VolumeBerkasType(Volume.QtyPinjamRi, Volume.QtyPinjamRj, qtyKembaliRi, qtyKembaliRj);
        Status = MutasiBerkasStatusEnum.Returned;
    }

    public void Kembali(string petugasId, DateOnly tglMutasi, TimeOnly jamMutasi, VolumeBerkasType volumeKembali)
    {
        Guard.Against.Null(volumeKembali, nameof(volumeKembali));
        Kembali(petugasId, tglMutasi, jamMutasi, volumeKembali.QtyKembaliRi, volumeKembali.QtyKembaliRj);
    }

    public void Kembali(string petugasId, DateTime timestamp, decimal qtyKembaliRi, decimal qtyKembaliRj) =>
        Kembali(petugasId, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp), qtyKembaliRi, qtyKembaliRj);

    public void TandaiHilang(string petugasId, DateOnly tglMutasi, TimeOnly jamMutasi)
    {
        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));

        if (IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        if (Status == MutasiBerkasStatusEnum.Returned)
            throw new InvalidOperationException("Berkas sudah dikembalikan ke ruang rekam medis, tidak dapat ditandai hilang.");

        if (Status == MutasiBerkasStatusEnum.Missing)
            throw new InvalidOperationException("Berkas sudah berstatus hilang.");

        PetugasId = petugasId;
        TglMutasi = tglMutasi;
        JamMutasi = jamMutasi;
        Status = MutasiBerkasStatusEnum.Missing;
    }

    public void TandaiHilang(string petugasId, DateTime timestamp) =>
        TandaiHilang(petugasId, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp));

    public void Void(string petugasVoid, DateOnly tglVoid, TimeOnly jamVoid, string alasanVoid = "")
    {
        Guard.Against.NullOrWhiteSpace(petugasVoid, nameof(petugasVoid));

        if (IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void).");

        VoidInfo = VoidInfoType.CreateVoid(petugasVoid, tglVoid, jamVoid, alasanVoid);
        Status = MutasiBerkasStatusEnum.Void;
    }

    public void Void(string petugasVoid, DateTime timestamp, string alasanVoid = "") =>
        Void(petugasVoid, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp), alasanVoid);

    public void CetakTracer(string petugasCetak, DateOnly tglCetak, TimeOnly jamCetak)
    {
        Guard.Against.NullOrWhiteSpace(petugasCetak, nameof(petugasCetak));

        if (IsVoid)
            throw new InvalidOperationException("Mutasi berkas sudah dibatalkan (void), tidak dapat mencetak tracer.");

        CetakTracerInfo = CetakTracerInfo.Cetak(petugasCetak, tglCetak, jamCetak);
    }

    public void CetakTracer(string petugasCetak, DateTime timestamp) =>
        CetakTracer(petugasCetak, DateOnly.FromDateTime(timestamp), TimeOnly.FromDateTime(timestamp));

    public void CatatCetakTracer(string petugasCetak, DateOnly tglCetak, TimeOnly jamCetak) =>
        CetakTracer(petugasCetak, tglCetak, jamCetak);

    public void CatatCetakTracer(string petugasCetak, DateTime timestamp) =>
        CetakTracer(petugasCetak, timestamp);
    #endregion
}
