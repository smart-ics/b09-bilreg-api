using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.SalesContext.ResepFeature;

public class ResepModel : IResepKey
{
    private readonly List<ResepObatType> _listObat;

    public ResepModel(
        string resepId,
        RegReff reg,
        BodyMetricType bodyMetric,
        DokterReff dokter,
        LayananReff layanan,
        LayananReff layananStok,
        UrgenitasType urgenitas,
        TipeBrgReff tipeBrg,
        int iter,
        string description,
        DateTime tglRencanaPeriksa,
        string resepAsalReffId,
        string voidReason,
        AuditTrailType auditTrail,
        IEnumerable<ResepObatType> listObat)
    {
        if (iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(iter));

        ResepId = resepId ?? string.Empty;
        Register = reg ?? RegModel.Default.ToReff();
        BodyMetric = bodyMetric ?? BodyMetricType.Default();
        Dokter = dokter ?? DokterType.Default.ToReff();
        Layanan = layanan ?? LayananType.Default.ToReff();
        LayananStok = layananStok ?? LayananType.Default.ToReff();
        Urgenitas = urgenitas ?? UrgenitasType.Default;
        TipeBrg = tipeBrg ?? TipeBrgType.Default.ToReff();
        Iter = iter;
        Description = description ?? string.Empty;
        TglRencanaPeriksa = tglRencanaPeriksa;
        ResepAsalReffId = string.IsNullOrWhiteSpace(resepAsalReffId) ? AppConst.DASH : resepAsalReffId;
        VoidReason = string.IsNullOrWhiteSpace(voidReason) ? AppConst.DASH : voidReason;
        AuditTrail = auditTrail ?? AuditTrailType.Default;
        _listObat = listObat?.ToList() ?? new List<ResepObatType>();
    }

    public ResepModel(
        string resepId,
        RegReff reg,
        BodyMetricType bodyMetric,
        DokterReff dokter,
        LayananReff layanan,
        UrgenitasType urgenitas,
        TipeBrgReff tipeBrg,
        int iter,
        string description,
        AuditTrailType auditTrail,
        IEnumerable<ResepObatType> listObat)
        : this(
            resepId,
            reg,
            bodyMetric,
            dokter,
            layanan,
            LayananType.Default.ToReff(),
            urgenitas,
            tipeBrg,
            iter,
            description,
            DateTime.Today,
            AppConst.DASH,
            AppConst.DASH,
            auditTrail,
            listObat)
    {
    }

    public static ResepModel Key(string id) => new(
        id,
        RegModel.Default.ToReff(),
        BodyMetricType.Default(),
        DokterType.Default.ToReff(),
        LayananType.Default.ToReff(),
        LayananType.Default.ToReff(),
        UrgenitasType.Default,
        TipeBrgType.Default.ToReff(),
        0,
        AppConst.DASH,
        DateTime.MinValue,
        AppConst.DASH,
        AppConst.DASH,
        AuditTrailType.Default,
        new List<ResepObatType>());

    public static ResepModel Create(
        RegModel reg,
        BodyMetricType bodyMetric,
        DokterType dokter,
        LayananType layanan,
        UrgenitasType urgenitasType,
        TipeBrgType tipeBrg,
        int iter,
        string description,
        string userId,
        LayananType layananStok = null,
        DateTime? tglRencanaPeriksa = null,
        string resepAsalReffId = AppConst.DASH)
    {
        if (iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(iter));

        var newId = NunaId.NewLegacy("KP", 'A');
        var stok = layananStok?.ToReff() ?? layanan.ToReff();
        var tglRencana = tglRencanaPeriksa ?? DateTime.Today;
        return new ResepModel(
            newId,
            reg.ToReff(),
            bodyMetric,
            dokter.ToReff(),
            layanan.ToReff(),
            stok,
            urgenitasType,
            tipeBrg.ToReff(),
            iter,
            description,
            tglRencana,
            resepAsalReffId ?? AppConst.DASH,
            AppConst.DASH,
            AuditTrailType.Create(userId, DateTime.Now),
            new List<ResepObatType>());
    }

    public static ResepModel Create(
        RegModel reg,
        BodyMetricType bodyMetric,
        DokterType dokter,
        LayananType layanan,
        LayananReff layananStok,
        UrgenitasType urgenitasType,
        TipeBrgType tipeBrg,
        int iter,
        string description,
        string userId,
        DateTime? tglRencanaPeriksa = null,
        string resepAsalReffId = AppConst.DASH)
    {
        if (iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(iter));

        var newId = NunaId.NewLegacy("KP", 'A');
        var stok = layananStok ?? layanan.ToReff();
        var tglRencana = tglRencanaPeriksa ?? DateTime.Today;
        return new ResepModel(
            newId,
            reg.ToReff(),
            bodyMetric,
            dokter.ToReff(),
            layanan.ToReff(),
            stok,
            urgenitasType,
            tipeBrg.ToReff(),
            iter,
            description,
            tglRencana,
            resepAsalReffId ?? AppConst.DASH,
            AppConst.DASH,
            AuditTrailType.Create(userId, DateTime.Now),
            new List<ResepObatType>());
    }

    public string ResepId { get; private set; }
    public RegReff Register { get; private set; }
    public BodyMetricType BodyMetric { get; private set; }

    public DokterReff Dokter { get; private set; }
    public LayananReff Layanan { get; private set; }
    public LayananReff LayananStok { get; private set; }

    public UrgenitasType Urgenitas { get; private set; }
    public TipeBrgReff TipeBrg { get; private set; }

    public int Iter { get; private set; }
    public string Description { get; private set; }
    public DateTime TglRencanaPeriksa { get; private set; }
    public string ResepAsalReffId { get; private set; }
    public string VoidReason { get; private set; }

    public AuditTrailType AuditTrail { get; private set; }
    public IEnumerable<ResepObatType> ListObat => _listObat;

    public bool IsVoid =>
        AuditTrail.IsVoided ||
        (AuditTrail.Voided.Timestamp.Date != new DateTime(3000, 1, 1) && !string.IsNullOrWhiteSpace(AuditTrail.Voided.UserId)) ||
        (!string.IsNullOrWhiteSpace(VoidReason) && VoidReason != AppConst.DASH);

    public void AddObat(
        IBrg brg,
        SatuanType satuan,
        decimal qty,
        int iter,
        EtiketType etiket,
        bool isRacik = false)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");

        if (iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(iter));

        var isBrgDuplicated = _listObat.Any(x => x.Brg.BrgId == brg.BrgId);
        if (isBrgDuplicated)
            throw new ArgumentException($"Brg sudah ada, tidak bisa duplikasi.\n'{brg}'");

        var noUrut = ListObat.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;
        var newObat = ResepObatType.Create(noUrut, brg, satuan, qty, iter, etiket, isRacik);
        _listObat.Add(newObat);
    }

    public void AddObat(
        IBrg brg,
        SatuanType satuan,
        decimal qty,
        int iter,
        string signa,
        string instruction,
        string note)
    {
        AddObat(brg, satuan, qty, iter, EtiketType.Create(signa, instruction, note), isRacik: false);
    }

    public void AddObat(
        IBrg brg,
        SatuanType satuan,
        decimal qty,
        int iter,
        string signa,
        string instruction,
        int? frequency,
        decimal? unitDose,
        string route = AppConst.DASH,
        string mealRelation = AppConst.DASH,
        string uddTime = AppConst.DASH,
        string note = AppConst.DASH)
    {
        var etiket = EtiketType.Create(signa, instruction, frequency, unitDose, route, mealRelation, uddTime, note);
        AddObat(brg, satuan, qty, iter, etiket, isRacik: false);
    }

    public void AddRacik(
        IBrg brg,
        SatuanType satuan,
        decimal qty,
        int iter,
        EtiketType etiket)
    {
        AddObat(brg, satuan, qty, iter, etiket, isRacik: true);
    }

    public void AddRacik(
        IBrg brg,
        SatuanType satuan,
        decimal qty,
        int iter,
        string signa,
        string instruction,
        string note = AppConst.DASH)
    {
        AddObat(brg, satuan, qty, iter, EtiketType.Create(signa, instruction, note), isRacik: true);
    }

    public void AddRacik(
        IBrg brg,
        SatuanType satuan,
        decimal qty,
        int iter,
        string signa,
        string instruction,
        int? frequency,
        decimal? unitDose,
        string route = AppConst.DASH,
        string mealRelation = AppConst.DASH,
        string uddTime = AppConst.DASH,
        string note = AppConst.DASH)
    {
        var etiket = EtiketType.Create(signa, instruction, frequency, unitDose, route, mealRelation, uddTime, note);
        AddObat(brg, satuan, qty, iter, etiket, isRacik: true);
    }

    public void RemoveObat(IBrg brg)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");

        _listObat.RemoveAll(x => x.Brg.BrgId == brg.BrgId);
        var i = 1;
        foreach (var item in _listObat)
        {
            item.SetNoUrut(i);
            i++;
        }
    }

    public void AddItemRacik(
        IBrg obatRacik,
        IBrg itemRacik,
        SatuanType satuan,
        decimal qty,
        decimal dosis,
        string dosisTxt)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");

        var obat = _listObat.FirstOrDefault(x => x.Brg.BrgId == obatRacik.BrgId);
        if (obat is null)
            throw new KeyNotFoundException($"Obat Racik tidak ditemukan.\n'{obatRacik}'");

        obat.AddItemRacik(itemRacik, satuan, qty, dosis, dosisTxt);
    }

    public void AddItemRacik(
        string obatRacikId,
        IBrg itemRacik,
        SatuanType satuan,
        decimal qty,
        decimal dosis,
        string dosisTxt)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");

        var obat = _listObat.FirstOrDefault(x => x.Brg.BrgId == obatRacikId);
        if (obat is null)
            throw new KeyNotFoundException($"Obat Racik tidak ditemukan.\n'{obatRacikId}'");

        obat.AddItemRacik(itemRacik, satuan, qty, dosis, dosisTxt);
    }

    public void AddItemRacik(IBrg obatRacik, ResepItemRacikType itemRacik)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");

        var obat = _listObat.FirstOrDefault(x => x.Brg.BrgId == obatRacik.BrgId);
        if (obat is null)
            throw new KeyNotFoundException($"Obat Racik tidak ditemukan.\n'{obatRacik}'");

        obat.AddItemRacik(itemRacik);
    }

    public void RemoveItemRacik(IBrg obatRacik, IBrg itemRacik)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");

        var obat = _listObat.FirstOrDefault(x => x.Brg.BrgId == obatRacik.BrgId);
        if (obat is null)
            return;

        obat.RemoveItemRacik(itemRacik);
    }

    public void ValidateRacik()
    {
        foreach (var obat in _listObat.Where(x => x.IsRacik))
        {
            obat.ValidateRacik();
        }
    }

    public void Validate()
    {
        if (Iter < 0)
            throw new ArgumentException("Iter tidak boleh bernilai negatif.", nameof(Iter));

        if (!_listObat.Any())
            throw new InvalidOperationException("Resep harus memiliki minimal 1 item obat.");

        foreach (var obat in _listObat)
        {
            if (obat.Iter < 0)
                throw new ArgumentException($"Iter obat '{obat.Brg.BrgName}' tidak boleh bernilai negatif.");
        }

        ValidateRacik();
    }

    public void Modify(string userId)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat diubah.");
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Void(string userId, string reason, DateTime at)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep sudah pernah dibatalkan.");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID pembatal harus diisi.", nameof(userId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Alasan pembatalan harus diisi.", nameof(reason));

        AuditTrail.Batal(userId, at);
        VoidReason = reason;
    }

    public void Void(string userId) => Void(userId, "Dibatalkan", DateTime.Now);

    public void SetLayananStok(LayananReff layananStok)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");
        LayananStok = layananStok ?? LayananType.Default.ToReff();
    }

    public void SetTglRencanaPeriksa(DateTime tglRencanaPeriksa)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");
        TglRencanaPeriksa = tglRencanaPeriksa;
    }

    public void SetResepAsalReffId(string resepAsalReffId)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");
        ResepAsalReffId = string.IsNullOrWhiteSpace(resepAsalReffId) ? AppConst.DASH : resepAsalReffId;
    }

    public void SetDescription(string description)
    {
        if (IsVoid)
            throw new InvalidOperationException("Resep yang sudah dibatalkan tidak dapat dimodifikasi.");
        Description = description ?? string.Empty;
    }
}