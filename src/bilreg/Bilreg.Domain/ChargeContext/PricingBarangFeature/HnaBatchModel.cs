using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ChargeContext.PricingBarangFeature;

/// <summary>
/// Aggregate root transaksi batch penerbitan HNA (tabel legacy <c>tb_trs_hna</c>).
/// Menghimpun rincian baris HNA per barang (<see cref="HnaBatchItemModel"/>) dengan
/// penomoran transaksi legacy berawalan <c>HN</c>.
/// </summary>
public class HnaBatchModel : IHnaBatchKey
{
    private const string ID_PREFIX = "HN";
    private readonly List<HnaBatchItemModel> _listItem;

    #region CREATE
    public HnaBatchModel(
        string hnaBatchId,
        DateTime tglTrs,
        string petugasId,
        AuditTrailType auditTrail,
        IEnumerable<HnaBatchItemModel> listItem)
    {
        HnaBatchId = hnaBatchId;
        TglTrs = tglTrs;
        PetugasId = petugasId;
        AuditTrail = auditTrail;
        _listItem = listItem?.ToList() ?? [];
    }

    public static HnaBatchModel Default => new(
        AppConst.DASH,
        DateTime.MinValue,
        AppConst.DASH,
        AuditTrailType.Default,
        []);

    public static HnaBatchModel Key(string id) => new(
        id,
        DateTime.MinValue,
        AppConst.DASH,
        AuditTrailType.Default,
        []);

    /// <summary>
    /// Menerbitkan batch HNA baru dengan nomor transaksi legacy berawalan <c>HN</c>.
    /// </summary>
    public static HnaBatchModel Create(string petugasId, DateTime tglTrs)
    {
        if (string.IsNullOrWhiteSpace(petugasId))
            throw new ArgumentException("Petugas ID penerbit batch HNA harus diisi.", nameof(petugasId));

        var newId = NunaId.NewLegacy(ID_PREFIX, 'A');

        return new HnaBatchModel(
            newId,
            tglTrs,
            petugasId,
            AuditTrailType.Create(petugasId, DateTime.Now),
            []);
    }
    #endregion

    #region PROPERTIES
    /// <summary>Nomor transaksi batch HNA berawalan <c>HN</c> (<c>fs_kd_trs</c>).</summary>
    public string HnaBatchId { get; private set; }

    /// <summary>Tanggal pembuatan dokumen HNA (<c>fd_tgl_trs</c>).</summary>
    public DateTime TglTrs { get; private set; }

    /// <summary>User ID staf yang menerbitkan batch HNA (<c>fs_kd_petugas</c>).</summary>
    public string PetugasId { get; private set; }

    public AuditTrailType AuditTrail { get; private set; }

    public IReadOnlyList<HnaBatchItemModel> ListItem => _listItem;

    public bool IsVoid => AuditTrail.IsVoided;
    #endregion

    #region BEHAVIOR
    /// <summary>
    /// Menambahkan baris HNA untuk satu barang dengan variasi HNA pengadaan.
    /// Baris baru langsung berstatus aktif (<c>fd_tgl_expired = '3000-01-01'</c>).
    /// </summary>
    public void AddItem(
        string barangId,
        decimal hnaAkhir,
        decimal hnaRata,
        decimal hnaMax,
        decimal hnaGen,
        decimal diskonGen,
        DateTime tglBerlaku,
        decimal hnaGenRi = 0m)
    {
        EnsureNotVoided();

        if (_listItem.Any(x => x.BarangId == barangId && !x.IsVoid))
            throw new ArgumentException($"Barang sudah ada pada batch HNA, tidak bisa duplikasi.\n'{barangId}'");

        var noUrut = _listItem.Select(x => x.NoUrut).DefaultIfEmpty(0).Max() + 1;

        _listItem.Add(HnaBatchItemModel.Create(
            HnaBatchId,
            noUrut,
            barangId,
            hnaAkhir,
            hnaRata,
            hnaMax,
            hnaGen,
            diskonGen,
            tglBerlaku,
            hnaGenRi: hnaGenRi));
    }

    public void Modify(string userId)
    {
        EnsureNotVoided();
        AuditTrail.Modif(userId, DateTime.Now);
    }

    public void Void(string userId, DateTime at)
    {
        EnsureNotVoided();
        AuditTrail.Batal(userId, at);
        foreach (var item in _listItem.Where(x => !x.IsVoid))
            item.VoidLine();
    }

    /// <summary>
    /// Menegakkan invarian batch: minimal satu baris barang dan seluruh baris valid.
    /// </summary>
    public void Validate()
    {
        if (!_listItem.Any())
            throw new InvalidOperationException("Batch HNA harus memiliki minimal 1 item barang.");

        foreach (var item in _listItem)
            item.Validate();
    }

    private void EnsureNotVoided()
    {
        if (AuditTrail.IsVoided)
            throw new InvalidOperationException("Batch HNA sudah void, tidak bisa diubah.");
    }
    #endregion
}
