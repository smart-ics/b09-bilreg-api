using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Domain.ApotekContext.TelaahResepFeature;

public class TelaahResepItemModel
{
    public TelaahResepItemModel(
        int itemNo,
        int resepKerjaItemNo,
        TelaahDispositionEnum disposition,
        string acceptedBrgId,
        string acceptedBrgName,
        decimal acceptedQty,
        string reason,
        string pharmacistId)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        ItemNo = itemNo;
        ResepKerjaItemNo = resepKerjaItemNo;
        Disposition = disposition;
        AcceptedBrgId = acceptedBrgId ?? "";
        AcceptedBrgName = acceptedBrgName ?? "";
        AcceptedQty = acceptedQty;
        Reason = reason ?? "";
        PharmacistId = pharmacistId ?? "";
    }

    public static TelaahResepItemModel Pending(int itemNo, int resepKerjaItemNo, string brgId, string brgName, decimal qty)
        => new(itemNo, resepKerjaItemNo, TelaahDispositionEnum.Pending, brgId, brgName, qty, "", "");

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
            ResepKerjaItemNo,
            disposition,
            acceptedBrgId,
            acceptedBrgName,
            acceptedQty,
            reason,
            pharmacistId);
    }

    public bool IsTerminal => Disposition != TelaahDispositionEnum.Pending;
    public bool IsAccepted =>
        Disposition is TelaahDispositionEnum.AcceptedAsPrescribed or TelaahDispositionEnum.AcceptedSubstitute;

    public int ItemNo { get; }
    public int ResepKerjaItemNo { get; }
    public TelaahDispositionEnum Disposition { get; }
    public string AcceptedBrgId { get; }
    public string AcceptedBrgName { get; }
    public decimal AcceptedQty { get; }
    public string Reason { get; }
    public string PharmacistId { get; }
}
