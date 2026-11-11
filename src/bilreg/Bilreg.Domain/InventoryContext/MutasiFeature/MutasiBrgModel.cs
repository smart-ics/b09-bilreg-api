using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public class MutasiBrgModel : IMutasiBrgKey
{
    private readonly List<MutasiBrgItemModel> _listItem;

    #region CREATION
    public MutasiBrgModel(
        string mutasiBrgId,
        DateTime mutasiBrgDate,
        MutasiBrgStateEnum state,
        LayananReff layananAsal,
        LayananReff layananTujuan,
        string orderMutasiId,
        string dispatchNote,
        string issuerUserId,
        AuditTrailType auditTrail,
        IEnumerable<MutasiBrgItemModel> listItem)
    {
        MutasiBrgId = mutasiBrgId ?? string.Empty;
        MutasiBrgDate = mutasiBrgDate;
        State = state;
        LayananAsal = layananAsal;
        LayananTujuan = layananTujuan;
        OrderMutasiId = orderMutasiId ?? string.Empty;
        DispatchNote = dispatchNote ?? string.Empty;
        IssuerUserId = issuerUserId ?? string.Empty;
        AuditTrail = auditTrail ?? AuditTrailType.Default;
        _listItem = listItem?.ToList() ?? [];
    }

    public static MutasiBrgModel Default => new(
        "-",
        new DateTime(3000, 1, 1),
        MutasiBrgStateEnum.Draft,
        new LayananReff("-", "-"),
        new LayananReff("-", "-"),
        string.Empty,
        string.Empty,
        string.Empty,
        AuditTrailType.Default,
        []);

    public static IMutasiBrgKey Key(string id) => new MutasiBrgKey(id);

    public static MutasiBrgModel Create(
        string id,
        DateTime date,
        LayananReff asal,
        LayananReff tujuan,
        string orderMutasiId = "",
        string dispatchNote = "",
        string userId = "")
    {
        Guard.Against.Null(asal, nameof(asal));
        Guard.Against.Null(tujuan, nameof(tujuan));

        if (string.IsNullOrWhiteSpace(asal.LayananId) || asal.LayananId == "-")
            throw new ArgumentException("LayananAsal invalid", nameof(asal));

        if (string.IsNullOrWhiteSpace(tujuan.LayananId) || tujuan.LayananId == "-")
            throw new ArgumentException("LayananTujuan invalid", nameof(tujuan));

        if (asal.LayananId.Trim().Equals(tujuan.LayananId.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Unit asal dan tujuan tidak boleh sama", nameof(tujuan));

        var mutasiId = string.IsNullOrWhiteSpace(id) ? NunaId.NewLegacy("MT", 'A') : id.Trim();
        var mutasiDate = date == default ? DateTime.Now : date;
        var auditTrail = AuditTrailType.Create(userId, mutasiDate);

        return new MutasiBrgModel(
            mutasiId,
            mutasiDate,
            MutasiBrgStateEnum.Draft,
            asal,
            tujuan,
            orderMutasiId?.Trim() ?? string.Empty,
            dispatchNote?.Trim() ?? string.Empty,
            string.Empty,
            auditTrail,
            []);
    }

    public static MutasiBrgModel Load(
        string mutasiBrgId,
        DateTime mutasiBrgDate,
        MutasiBrgStateEnum state,
        LayananReff layananAsal,
        LayananReff layananTujuan,
        string orderMutasiId,
        string dispatchNote,
        string issuerUserId,
        AuditTrailType auditTrail,
        IEnumerable<MutasiBrgItemModel> listItem)
    {
        return new MutasiBrgModel(
            mutasiBrgId,
            mutasiBrgDate,
            state,
            layananAsal,
            layananTujuan,
            orderMutasiId,
            dispatchNote,
            issuerUserId,
            auditTrail,
            listItem);
    }
    #endregion

    #region PROPERTIES
    public string MutasiBrgId { get; init; }
    public DateTime MutasiBrgDate { get; private set; }
    public MutasiBrgStateEnum State { get; private set; }
    public LayananReff LayananAsal { get; init; }
    public LayananReff LayananTujuan { get; init; }
    public string OrderMutasiId { get; private set; }
    public string DispatchNote { get; private set; }
    public string IssuerUserId { get; private set; }
    public AuditTrailType AuditTrail { get; init; }
    public IReadOnlyList<MutasiBrgItemModel> ListItem => _listItem;
    #endregion

    #region BEHAVIOR
    public void AddItem(MutasiBrgItemModel item)
    {
        GuardDraft();
        Guard.Against.Null(item, nameof(item));

        if (item.QtyKirim <= 0)
            throw new ArgumentException("QtyKirim harus lebih dari 0", nameof(item));

        if (item.NoUrut <= 0)
        {
            var nextNoUrut = _listItem.Count > 0 ? _listItem.Max(x => x.NoUrut) + 1 : 1;
            item.SetNoUrut(nextNoUrut);
        }
        else
        {
            if (_listItem.Any(x => x.NoUrut == item.NoUrut))
                throw new ArgumentException($"NoUrut {item.NoUrut} sudah digunakan", nameof(item));
        }

        _listItem.Add(item);
    }

    public void RemoveItem(int noUrut)
    {
        GuardDraft();

        if (noUrut <= 0)
            throw new ArgumentException("NoUrut invalid", nameof(noUrut));

        var existing = _listItem.FirstOrDefault(x => x.NoUrut == noUrut);
        if (existing is null)
            throw new ArgumentException($"Item dengan NoUrut {noUrut} tidak ditemukan", nameof(noUrut));

        _listItem.Remove(existing);
        Reorder();
    }

    public void Dispatch(string issuerUserId, DateTime? dispatchDate = null)
    {
        GuardDraft();

        if (string.IsNullOrWhiteSpace(issuerUserId))
            throw new ArgumentException("IssuerUserId tidak boleh kosong", nameof(issuerUserId));

        if (_listItem.Count == 0)
            throw new InvalidOperationException("Mutasi barang harus memiliki minimal 1 item untuk di-dispatch");

        State = MutasiBrgStateEnum.Dispatched;
        IssuerUserId = issuerUserId.Trim();
        MutasiBrgDate = dispatchDate ?? DateTime.Now;
        AuditTrail.Modif(issuerUserId, MutasiBrgDate);
    }

    public void Cancel(string userId)
    {
        if (State != MutasiBrgStateEnum.Dispatched)
            throw new InvalidOperationException($"Pembatalan hanya diizinkan jika status Dispatched. Status saat ini: {State}");

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId pembatal tidak boleh kosong", nameof(userId));

        State = MutasiBrgStateEnum.Cancelled;
        AuditTrail.Batal(userId.Trim(), DateTime.Now);
    }

    public void UpdateDispatchNote(string note)
    {
        GuardDraft();
        DispatchNote = note?.Trim() ?? string.Empty;
    }

    public void SetOrderMutasiId(string orderMutasiId)
    {
        GuardDraft();
        OrderMutasiId = orderMutasiId?.Trim() ?? string.Empty;
    }

    private void Reorder()
    {
        var i = 1;
        foreach (var item in _listItem)
            item.SetNoUrut(i++);
    }
    #endregion

    #region GUARD
    private void GuardDraft()
    {
        if (State != MutasiBrgStateEnum.Draft)
            throw new InvalidOperationException($"Operasi hanya diizinkan pada status Draft. Status saat ini: {State}");
    }
    #endregion
}
