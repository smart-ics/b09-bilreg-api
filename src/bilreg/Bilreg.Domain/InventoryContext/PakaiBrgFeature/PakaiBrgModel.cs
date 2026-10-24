using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.InventoryContext.PakaiBrgFeature;

public class PakaiBrgModel : IPakaiBrgKey
{
    private const int MAX_KETERANGAN_LENGTH = 40;
    private const int MAX_ALASAN_VOID_LENGTH = 40;
    private readonly List<PakaiBrgItemModel> _listItem;

    #region CREATION
    public PakaiBrgModel(
        string pakaiBrgId,
        DateTime tglPakai,
        LayananReff layanan,
        string petugasId,
        string keterangan,
        bool isVoid,
        DateTime tglVoid,
        string petugasVoidId,
        string alasanVoid,
        IEnumerable<PakaiBrgItemModel> listItem)
    {
        Guard.Against.NullOrWhiteSpace(pakaiBrgId, nameof(pakaiBrgId));
        Guard.Against.Null(layanan, nameof(layanan));
        if (string.IsNullOrWhiteSpace(layanan.LayananId) || layanan.LayananId == AppConst.DASH)
            throw new ArgumentException("Layanan pemakai (Cost Center) wajib valid.", nameof(layanan));

        Guard.Against.NullOrWhiteSpace(petugasId, nameof(petugasId));
        Guard.Against.Null(listItem, nameof(listItem));

        var items = listItem.ToList();
        if (items.Count == 0)
            throw new ArgumentException("Dokumen pemakaian barang wajib memiliki minimal 1 item.", nameof(listItem));

        PakaiBrgId = pakaiBrgId;
        TglPakai = tglPakai;
        Layanan = layanan;
        PetugasId = petugasId;
        Keterangan = TruncateSafe(keterangan, MAX_KETERANGAN_LENGTH);
        IsVoid = isVoid;
        TglVoid = tglVoid;
        PetugasVoidId = petugasVoidId?.Trim() ?? string.Empty;
        AlasanVoid = TruncateSafe(alasanVoid, MAX_ALASAN_VOID_LENGTH);
        _listItem = items;
    }

    public static PakaiBrgModel Create(
        DateTime tglPakai,
        LayananReff layanan,
        string petugasId,
        string keterangan,
        IEnumerable<PakaiBrgItemModel> listItem,
        string? pakaiBrgId = null)
    {
        var id = string.IsNullOrWhiteSpace(pakaiBrgId)
            ? NunaId.NewLegacyCompact("PK")
            : pakaiBrgId;

        Guard.Against.Null(listItem, nameof(listItem));
        var items = listItem.ToList();
        if (items.Count == 0)
            throw new ArgumentException("Dokumen pemakaian barang wajib memiliki minimal 1 item.", nameof(listItem));

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].NoUrut <= 0)
                items[i].SetNoUrut(i + 1);
        }

        return new PakaiBrgModel(
            id,
            tglPakai == default ? DateTime.Now : tglPakai,
            layanan,
            petugasId,
            keterangan,
            isVoid: false,
            tglVoid: AppConst.DEF_DATE,
            petugasVoidId: string.Empty,
            alasanVoid: string.Empty,
            items);
    }

    public static PakaiBrgModel Rehydrate(
        string pakaiBrgId,
        DateTime tglPakai,
        LayananReff layanan,
        string petugasId,
        string keterangan,
        bool isVoid,
        DateTime tglVoid,
        string petugasVoidId,
        string alasanVoid,
        IEnumerable<PakaiBrgItemModel> listItem)
    {
        return new PakaiBrgModel(
            pakaiBrgId,
            tglPakai,
            layanan,
            petugasId,
            keterangan,
            isVoid,
            tglVoid,
            petugasVoidId,
            alasanVoid,
            listItem);
    }

    public static IPakaiBrgKey Key(string id) => new PakaiBrgKey(id);
    #endregion

    #region PROPERTIES
    public string PakaiBrgId { get; init; }
    public DateTime TglPakai { get; private set; }
    public LayananReff Layanan { get; private set; }
    public string PetugasId { get; private set; }
    public string Keterangan { get; private set; }
    public bool IsVoid { get; private set; }
    public DateTime TglVoid { get; private set; }
    public string PetugasVoidId { get; private set; }
    public string AlasanVoid { get; private set; }

    public IReadOnlyList<PakaiBrgItemModel> ListItem => _listItem;
    public decimal TotalNilai => _listItem.Sum(x => x.SubTotal);
    #endregion

    #region BEHAVIOR
    public void Void(string petugasVoidId, string alasanVoid, DateTime tglVoid = default)
    {
        if (IsVoid)
            throw new InvalidOperationException($"Dokumen pemakaian '{PakaiBrgId}' sudah dibatalkan (Void) sebelumnya.");

        if (string.IsNullOrWhiteSpace(alasanVoid))
            throw new ArgumentException("Alasan pembatalan (Void) wajib diisi.", nameof(alasanVoid));

        if (string.IsNullOrWhiteSpace(petugasVoidId))
            throw new ArgumentException("Petugas pembatal (Void) wajib diisi.", nameof(petugasVoidId));

        IsVoid = true;
        PetugasVoidId = petugasVoidId.Trim();
        AlasanVoid = TruncateSafe(alasanVoid, MAX_ALASAN_VOID_LENGTH);
        TglVoid = tglVoid == default ? DateTime.Now : tglVoid;

        foreach (var item in _listItem)
        {
            item.Void();
        }
    }

    private static string TruncateSafe(string? value, int maxLength)
    {
        var safe = (value ?? string.Empty).Trim();
        return safe.Length > maxLength ? safe[..maxLength] : safe;
    }
    #endregion
}
