using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Domain.ApotekContext.TelaahResepFeature;

public class TelaahResepItemModel
{
    public TelaahResepItemModel(
        int itemNo,
        int resepItemNo,
        TelaahDispositionEnum disposition,
        string acceptedBrgId,
        string acceptedBrgName,
        decimal acceptedQty,
        string reason,
        string pharmacistId,
        string prescribedBrgId = "",
        string prescribedBrgName = "",
        decimal prescribedQty = 0)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        ItemNo = itemNo;
        ResepItemNo = resepItemNo;
        Disposition = disposition;
        AcceptedBrgId = acceptedBrgId ?? "";
        AcceptedBrgName = acceptedBrgName ?? "";
        AcceptedQty = acceptedQty;
        Reason = reason ?? "";
        PharmacistId = pharmacistId ?? "";
        PrescribedBrgId = prescribedBrgId ?? "";
        PrescribedBrgName = prescribedBrgName ?? "";
        PrescribedQty = prescribedQty;
    }

    public TelaahResepItemModel(
        int itemNo,
        int resepItemNo,
        string prescribedBrgId,
        string prescribedBrgName,
        decimal prescribedQty,
        TelaahDispositionEnum disposition,
        string acceptedBrgId,
        string acceptedBrgName,
        decimal acceptedQty,
        string reason,
        string pharmacistId)
        : this(itemNo, resepItemNo, disposition, acceptedBrgId, acceptedBrgName, acceptedQty, reason, pharmacistId, prescribedBrgId, prescribedBrgName, prescribedQty)
    {
    }

    public static TelaahResepItemModel Pending(int itemNo, int resepItemNo, string brgId, string brgName, decimal qty)
        => new(itemNo, resepItemNo, TelaahDispositionEnum.Pending, brgId, brgName, qty, "", "", brgId, brgName, qty);

    public TelaahResepItemModel WithDisposition(
        TelaahDispositionEnum disposition,
        string acceptedBrgId,
        string acceptedBrgName,
        decimal acceptedQty,
        string reason,
        string pharmacistId)
    {
        Guard.Against.NullOrWhiteSpace(pharmacistId, nameof(pharmacistId));
        if (disposition is TelaahDispositionEnum.AcceptedSubstitute or TelaahDispositionEnum.Rejected
            && string.IsNullOrWhiteSpace(reason))
            throw new ApotekDomainException("Substitute and reject dispositions require a reason.");
        if (disposition is TelaahDispositionEnum.AcceptedAsPrescribed or TelaahDispositionEnum.AcceptedSubstitute
            && acceptedQty <= 0)
            throw new ApotekDomainException("Accepted quantity must be greater than zero.");
        if (disposition == TelaahDispositionEnum.Rejected)
            acceptedQty = 0;

        return new TelaahResepItemModel(
            ItemNo,
            ResepItemNo,
            disposition,
            acceptedBrgId,
            acceptedBrgName,
            acceptedQty,
            reason,
            pharmacistId,
            PrescribedBrgId,
            PrescribedBrgName,
            PrescribedQty);
    }

    public bool IsTerminal => Disposition != TelaahDispositionEnum.Pending;
    public bool IsAccepted =>
        Disposition is TelaahDispositionEnum.AcceptedAsPrescribed or TelaahDispositionEnum.AcceptedSubstitute;

    public int ItemNo { get; }
    public int ResepItemNo { get; }
    [Obsolete("Use ResepItemNo")]
    public int ResepKerjaItemNo => ResepItemNo;
    public string PrescribedBrgId { get; }
    public string PrescribedBrgName { get; }
    public decimal PrescribedQty { get; }
    public TelaahDispositionEnum Disposition { get; }
    public string AcceptedBrgId { get; }
    public string AcceptedBrgName { get; }
    public decimal AcceptedQty { get; }
    public string Reason { get; }
    public string PharmacistId { get; }
}
