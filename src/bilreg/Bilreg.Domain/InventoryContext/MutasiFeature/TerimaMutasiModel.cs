using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.InventoryContext.MutasiFeature;

public class TerimaMutasiModel : ITerimaMutasiKey
{
    private readonly List<TerimaMutasiItemModel> _listItem;

    #region CREATION
    private TerimaMutasiModel(
        string terimaMutasiId,
        DateTime terimaMutasiDate,
        TerimaMutasiStateEnum state,
        string mutasiBrgId,
        string orderMutasiId,
        LayananReff layananAsal,
        LayananReff layananTujuan,
        string receiverUserId,
        string note,
        string rejectionReason,
        AuditTrailType auditTrail,
        IEnumerable<TerimaMutasiItemModel> listItem)
    {
        TerimaMutasiId = terimaMutasiId ?? string.Empty;
        TerimaMutasiDate = terimaMutasiDate;
        State = state;
        MutasiBrgId = mutasiBrgId ?? string.Empty;
        OrderMutasiId = orderMutasiId ?? string.Empty;
        LayananAsal = layananAsal;
        LayananTujuan = layananTujuan;
        ReceiverUserId = receiverUserId ?? string.Empty;
        Note = note ?? string.Empty;
        RejectionReason = rejectionReason ?? string.Empty;
        AuditTrail = auditTrail ?? AuditTrailType.Default;
        _listItem = listItem?.ToList() ?? [];
    }

    public static TerimaMutasiModel Default => new(
        "-",
        new DateTime(3000, 1, 1),
        TerimaMutasiStateEnum.Received,
        "-",
        string.Empty,
        new LayananReff("-", "-"),
        new LayananReff("-", "-"),
        string.Empty,
        string.Empty,
        string.Empty,
        AuditTrailType.Default,
        []);

    public static ITerimaMutasiKey Key(string id) => new TerimaMutasiKey(id);

    public static TerimaMutasiModel CreateConfirmed(
        string mutasiBrgId,
        string orderMutasiId,
        LayananReff layananAsal,
        LayananReff layananTujuan,
        string receiverUserId,
        IEnumerable<TerimaMutasiItemModel> listItem,
        string note = "",
        DateTime? receiveDate = null,
        string id = "")
    {
        ValidateHeaderInputs(mutasiBrgId, layananAsal, layananTujuan, receiverUserId);

        Guard.Against.Null(listItem, nameof(listItem));
        var rawItems = listItem.ToList();
        if (rawItems.Count == 0)
            throw new ArgumentException("Item penerimaan tidak boleh kosong", nameof(listItem));

        var normalizedItems = new List<TerimaMutasiItemModel>(rawItems.Count);
        for (var i = 0; i < rawItems.Count; i++)
        {
            var item = rawItems[i];
            Guard.Against.Null(item, nameof(listItem));

            if (item.QtyTerimaBaik < 0)
                throw new ArgumentException("QtyTerimaBaik tidak boleh negatif", nameof(listItem));

            if (item.QtySelisih < 0)
                throw new ArgumentException("QtySelisih tidak boleh negatif", nameof(listItem));

            if (item.QtyTerimaBaik + item.QtySelisih != item.QtyKirim)
                throw new ArgumentException("QtyTerimaBaik + QtySelisih harus sama dengan QtyKirim", nameof(listItem));

            if (item.QtySelisih > 0 && string.IsNullOrWhiteSpace(item.AlasanSelisih))
                throw new ArgumentException("AlasanSelisih wajib diisi jika QtySelisih > 0", nameof(listItem));

            normalizedItems.Add(item.NoUrut <= 0 ? item.WithNoUrut(i + 1) : item);
        }

        var hasDiscrepancy = normalizedItems.Any(x => x.QtySelisih > 0);
        var state = hasDiscrepancy
            ? TerimaMutasiStateEnum.ReceivedWithDiscrepancy
            : TerimaMutasiStateEnum.Received;

        var terimaMutasiId = string.IsNullOrWhiteSpace(id) ? NunaId.NewLegacy("TM", 'A') : id.Trim();
        var trxDate = receiveDate ?? DateTime.Now;
        var cleanUserId = receiverUserId.Trim();
        var auditTrail = AuditTrailType.Create(cleanUserId, trxDate);

        return new TerimaMutasiModel(
            terimaMutasiId,
            trxDate,
            state,
            mutasiBrgId.Trim(),
            orderMutasiId?.Trim() ?? string.Empty,
            layananAsal,
            layananTujuan,
            cleanUserId,
            note?.Trim() ?? string.Empty,
            string.Empty,
            auditTrail,
            normalizedItems);
    }

    public static TerimaMutasiModel CreateRejected(
        string mutasiBrgId,
        string orderMutasiId,
        LayananReff layananAsal,
        LayananReff layananTujuan,
        string receiverUserId,
        string rejectionReason,
        IEnumerable<MutasiBrgItemModel> listItem,
        string note = "",
        DateTime? rejectDate = null,
        string id = "")
    {
        ValidateHeaderInputs(mutasiBrgId, layananAsal, layananTujuan, receiverUserId);

        Guard.Against.NullOrWhiteSpace(rejectionReason, nameof(rejectionReason), "Alasan penolakan tidak boleh kosong");
        var cleanReason = rejectionReason.Trim();
        if (cleanReason.Length < 5)
            throw new ArgumentException("Alasan penolakan minimal 5 karakter", nameof(rejectionReason));

        Guard.Against.Null(listItem, nameof(listItem));
        var sourceItems = listItem.ToList();
        if (sourceItems.Count == 0)
            throw new ArgumentException("Item penolakan tidak boleh kosong", nameof(listItem));

        var rejectedItems = new List<TerimaMutasiItemModel>(sourceItems.Count);
        for (var i = 0; i < sourceItems.Count; i++)
        {
            var src = sourceItems[i];
            Guard.Against.Null(src, nameof(listItem));

            var noUrut = src.NoUrut > 0 ? src.NoUrut : i + 1;
            rejectedItems.Add(TerimaMutasiItemModel.CreateRejected(
                noUrut,
                src.Brg,
                src.BatchNo,
                src.ExpiredDate,
                src.QtyKirim,
                src.Satuan,
                cleanReason));
        }

        var terimaMutasiId = string.IsNullOrWhiteSpace(id) ? NunaId.NewLegacy("TM", 'A') : id.Trim();
        var trxDate = rejectDate ?? DateTime.Now;
        var cleanUserId = receiverUserId.Trim();
        var auditTrail = AuditTrailType.Create(cleanUserId, trxDate);

        return new TerimaMutasiModel(
            terimaMutasiId,
            trxDate,
            TerimaMutasiStateEnum.Rejected,
            mutasiBrgId.Trim(),
            orderMutasiId?.Trim() ?? string.Empty,
            layananAsal,
            layananTujuan,
            cleanUserId,
            note?.Trim() ?? string.Empty,
            cleanReason,
            auditTrail,
            rejectedItems);
    }

    public static TerimaMutasiModel Load(
        string terimaMutasiId,
        DateTime terimaMutasiDate,
        TerimaMutasiStateEnum state,
        string mutasiBrgId,
        string orderMutasiId,
        LayananReff layananAsal,
        LayananReff layananTujuan,
        string receiverUserId,
        string note,
        string rejectionReason,
        AuditTrailType auditTrail,
        IEnumerable<TerimaMutasiItemModel> listItem)
    {
        return new TerimaMutasiModel(
            terimaMutasiId,
            terimaMutasiDate,
            state,
            mutasiBrgId,
            orderMutasiId,
            layananAsal,
            layananTujuan,
            receiverUserId,
            note,
            rejectionReason,
            auditTrail,
            listItem);
    }
    #endregion

    #region PROPERTIES
    public string TerimaMutasiId { get; init; }
    public DateTime TerimaMutasiDate { get; init; }
    public TerimaMutasiStateEnum State { get; init; }
    public string MutasiBrgId { get; init; }
    public string OrderMutasiId { get; init; }
    public LayananReff LayananAsal { get; init; }
    public LayananReff LayananTujuan { get; init; }
    public string ReceiverUserId { get; init; }
    public string Note { get; init; }
    public string RejectionReason { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public IReadOnlyList<TerimaMutasiItemModel> ListItem => _listItem.AsReadOnly();
    #endregion

    #region PRIVATE HELPERS
    private static void ValidateHeaderInputs(
        string mutasiBrgId,
        LayananReff layananAsal,
        LayananReff layananTujuan,
        string receiverUserId)
    {
        if (string.IsNullOrWhiteSpace(mutasiBrgId) || mutasiBrgId.Trim() == "-")
            throw new ArgumentException("MutasiBrgId invalid", nameof(mutasiBrgId));

        Guard.Against.Null(layananAsal, nameof(layananAsal));
        if (string.IsNullOrWhiteSpace(layananAsal.LayananId) || layananAsal.LayananId == "-")
            throw new ArgumentException("LayananAsal invalid", nameof(layananAsal));

        Guard.Against.Null(layananTujuan, nameof(layananTujuan));
        if (string.IsNullOrWhiteSpace(layananTujuan.LayananId) || layananTujuan.LayananId == "-")
            throw new ArgumentException("LayananTujuan invalid", nameof(layananTujuan));

        Guard.Against.NullOrWhiteSpace(receiverUserId, nameof(receiverUserId));
    }
    #endregion
}
