using Ardalis.GuardClauses;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.ApotekContext.InvoiceFeature;

public class InvoiceItemModel
{
    public InvoiceItemModel(
        int itemNo,
        int salesOrderItemNo,
        string brgId,
        string brgName,
        InvoiceItemKindEnum itemKind,
        decimal qty,
        decimal hargaSatuan,
        decimal diskon,
        decimal biaya,
        decimal tax,
        decimal total)
    {
        Guard.Against.NegativeOrZero(itemNo, nameof(itemNo));
        Guard.Against.NegativeOrZero(salesOrderItemNo, nameof(salesOrderItemNo));
        Guard.Against.NullOrWhiteSpace(brgId, nameof(brgId));
        if (qty <= 0)
            throw new ApotekDomainException("Invoice item qty must be greater than zero.");
        ItemNo = itemNo;
        SalesOrderItemNo = salesOrderItemNo;
        BrgId = brgId;
        BrgName = brgName ?? "";
        ItemKind = itemKind;
        Qty = qty;
        HargaSatuan = hargaSatuan;
        Diskon = diskon;
        Biaya = biaya;
        Tax = tax;
        Total = total;
    }

    public int ItemNo { get; }
    public int SalesOrderItemNo { get; }
    public string BrgId { get; }
    public string BrgName { get; }
    public InvoiceItemKindEnum ItemKind { get; }
    public decimal Qty { get; }
    public decimal HargaSatuan { get; }
    public decimal Diskon { get; }
    public decimal Biaya { get; }
    public decimal Tax { get; }
    public decimal Total { get; }
}

public class InvoiceItemChargeModel
{
    public InvoiceItemChargeModel(int itemNo, int chargeNo, string chargeName, decimal amount)
    {
        ItemNo = itemNo;
        ChargeNo = chargeNo;
        ChargeName = chargeName ?? "";
        Amount = amount;
    }

    public int ItemNo { get; }
    public int ChargeNo { get; }
    public string ChargeName { get; }
    public decimal Amount { get; }
}

public class InvoiceModel : IInvoiceKey
{
    public const string IdPrefix = "ASI";
    private readonly List<InvoiceItemModel> _items;
    private readonly List<InvoiceItemChargeModel> _charges;

    private InvoiceModel(
        string invoiceId,
        string salesOrderId,
        PayerPathEnum payerPath,
        InvoiceStatusEnum invoiceStatus,
        DateTime pricingSnapshotAt,
        string tipeJaminanId,
        string tipeJaminanName,
        decimal subTotal,
        decimal sumBiaya,
        decimal sumTax,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan,
        decimal grandTotal,
        string paymentClearanceReff,
        DateTime paymentClearedAt,
        string tataRekeningChargeId,
        string tataRekeningCorrectionReff,
        DateTime establishedAt,
        DateTime issuedAt,
        DateTime clearedAt,
        int version,
        IEnumerable<InvoiceItemModel> items,
        IEnumerable<InvoiceItemChargeModel> charges)
    {
        InvoiceId = invoiceId;
        SalesOrderId = salesOrderId;
        PayerPath = payerPath;
        InvoiceStatus = invoiceStatus;
        PricingSnapshotAt = pricingSnapshotAt;
        TipeJaminanId = tipeJaminanId ?? "";
        TipeJaminanName = tipeJaminanName ?? "";
        SubTotal = subTotal;
        SumBiaya = sumBiaya;
        SumTax = sumTax;
        DiskonLain = diskonLain;
        BiayaLain = biayaLain;
        Pembulatan = pembulatan;
        GrandTotal = grandTotal;
        PaymentClearanceReff = paymentClearanceReff ?? "";
        PaymentClearedAt = paymentClearedAt;
        TataRekeningChargeId = tataRekeningChargeId ?? "";
        TataRekeningCorrectionReff = tataRekeningCorrectionReff ?? "";
        EstablishedAt = establishedAt;
        IssuedAt = issuedAt;
        ClearedAt = clearedAt;
        Version = version;
        _items = items.ToList();
        _charges = charges.ToList();
    }

    public static IInvoiceKey Key(string invoiceId) => new InvoiceKey(invoiceId);

    public static InvoiceModel Establish(
        string salesOrderId,
        PayerPathEnum payerPath,
        DateTime pricingSnapshotAt,
        string tipeJaminanId,
        string tipeJaminanName,
        DateTime establishedAt,
        IEnumerable<InvoiceItemModel> items,
        IEnumerable<InvoiceItemChargeModel> charges,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan)
    {
        Guard.Against.NullOrWhiteSpace(salesOrderId, nameof(salesOrderId));
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Invoice requires Sales Order items.");
        var chargeList = charges.ToList();
        var subTotal = list.Sum(x => x.Total);
        var sumBiaya = list.Sum(x => x.Biaya) + chargeList.Sum(x => x.Amount) + biayaLain;
        var sumTax = list.Sum(x => x.Tax);
        var grand = subTotal + sumBiaya + sumTax - diskonLain + pembulatan;
        return new InvoiceModel(
            NunaId.New(IdPrefix),
            salesOrderId,
            payerPath,
            InvoiceStatusEnum.Established,
            pricingSnapshotAt,
            tipeJaminanId,
            tipeJaminanName,
            subTotal,
            sumBiaya,
            sumTax,
            diskonLain,
            biayaLain,
            pembulatan,
            grand,
            "",
            ApotekDate.Empty,
            "",
            "",
            establishedAt,
            ApotekDate.Empty,
            ApotekDate.Empty,
            1,
            list,
            chargeList);
    }

    public static InvoiceModel Rehydrate(
        string invoiceId,
        string salesOrderId,
        PayerPathEnum payerPath,
        InvoiceStatusEnum invoiceStatus,
        DateTime pricingSnapshotAt,
        string tipeJaminanId,
        string tipeJaminanName,
        decimal subTotal,
        decimal sumBiaya,
        decimal sumTax,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan,
        decimal grandTotal,
        string paymentClearanceReff,
        DateTime paymentClearedAt,
        string tataRekeningChargeId,
        string tataRekeningCorrectionReff,
        DateTime establishedAt,
        DateTime issuedAt,
        DateTime clearedAt,
        int version,
        IEnumerable<InvoiceItemModel> items,
        IEnumerable<InvoiceItemChargeModel> charges)
        => new(invoiceId, salesOrderId, payerPath, invoiceStatus, pricingSnapshotAt, tipeJaminanId, tipeJaminanName,
            subTotal, sumBiaya, sumTax, diskonLain, biayaLain, pembulatan, grandTotal, paymentClearanceReff,
            paymentClearedAt, tataRekeningChargeId, tataRekeningCorrectionReff, establishedAt, issuedAt, clearedAt,
            version, items, charges);

    public void RewriteContent(
        IEnumerable<InvoiceItemModel> items,
        IEnumerable<InvoiceItemChargeModel> charges,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan,
        bool tataRekeningAllows)
    {
        if (InvoiceStatus == InvoiceStatusEnum.Established
            || (InvoiceStatus == InvoiceStatusEnum.Issued && tataRekeningAllows))
        {
            ReplaceContent(items, charges, diskonLain, biayaLain, pembulatan);
            Version++;
            return;
        }

        throw new ApotekDomainException("Invoice content is immutable under current Tata Rekening permission.");
    }

    public void Issue(DateTime issuedAt)
    {
        if (InvoiceStatus != InvoiceStatusEnum.Established)
            throw new ApotekDomainException("Only an Established Invoice can be issued.");
        InvoiceStatus = InvoiceStatusEnum.Issued;
        IssuedAt = issuedAt;
        Version++;
    }

    public void RecordPaymentClearance(string paymentClearanceReff, DateTime paymentClearedAt)
    {
        Guard.Against.NullOrWhiteSpace(paymentClearanceReff, nameof(paymentClearanceReff));
        if (paymentClearanceReff.Length > 26)
            throw new ApotekDomainException("PaymentClearanceReff exceeds PD-03 width 26.");
        PaymentClearanceReff = paymentClearanceReff;
        PaymentClearedAt = paymentClearedAt;
        InvoiceStatus = InvoiceStatusEnum.FinanciallyCleared;
        ClearedAt = paymentClearedAt;
        Version++;
    }

    public void RecordChargeCorrelation(string tataRekeningChargeId)
    {
        if (!string.IsNullOrWhiteSpace(TataRekeningChargeId) && TataRekeningChargeId != tataRekeningChargeId)
            return;
        TataRekeningChargeId = tataRekeningChargeId ?? "";
    }

    public void RecordCorrectionReff(string correctionReff)
    {
        Guard.Against.NullOrWhiteSpace(correctionReff, nameof(correctionReff));
        TataRekeningCorrectionReff = correctionReff;
        Version++;
    }

    public bool HasPaymentClearance =>
        !string.IsNullOrWhiteSpace(PaymentClearanceReff) && !ApotekDate.IsEmpty(PaymentClearedAt);

    public void AssertExpectedVersion(int expectedVersion)
    {
        if (Version != expectedVersion)
            throw new ApotekConcurrencyException(InvoiceId, expectedVersion);
    }

    private void ReplaceContent(
        IEnumerable<InvoiceItemModel> items,
        IEnumerable<InvoiceItemChargeModel> charges,
        decimal diskonLain,
        decimal biayaLain,
        decimal pembulatan)
    {
        var list = items.ToList();
        if (list.Count == 0)
            throw new ApotekDomainException("Invoice requires Sales Order items.");
        var chargeList = charges.ToList();
        _items.Clear();
        _items.AddRange(list);
        _charges.Clear();
        _charges.AddRange(chargeList);
        SubTotal = list.Sum(x => x.Total);
        SumBiaya = list.Sum(x => x.Biaya) + chargeList.Sum(x => x.Amount) + biayaLain;
        SumTax = list.Sum(x => x.Tax);
        DiskonLain = diskonLain;
        BiayaLain = biayaLain;
        Pembulatan = pembulatan;
        GrandTotal = SubTotal + SumBiaya + SumTax - diskonLain + pembulatan;
    }

    public string InvoiceId { get; }
    public string SalesOrderId { get; }
    public PayerPathEnum PayerPath { get; }
    public InvoiceStatusEnum InvoiceStatus { get; private set; }
    public DateTime PricingSnapshotAt { get; }
    public string TipeJaminanId { get; }
    public string TipeJaminanName { get; }
    public decimal SubTotal { get; private set; }
    public decimal SumBiaya { get; private set; }
    public decimal SumTax { get; private set; }
    public decimal DiskonLain { get; private set; }
    public decimal BiayaLain { get; private set; }
    public decimal Pembulatan { get; private set; }
    public decimal GrandTotal { get; private set; }
    public string PaymentClearanceReff { get; private set; }
    public DateTime PaymentClearedAt { get; private set; }
    public string TataRekeningChargeId { get; private set; }
    public string TataRekeningCorrectionReff { get; private set; }
    public DateTime EstablishedAt { get; }
    public DateTime IssuedAt { get; private set; }
    public DateTime ClearedAt { get; private set; }
    public int Version { get; private set; }
    public IReadOnlyList<InvoiceItemModel> Items => _items;
    public IReadOnlyList<InvoiceItemChargeModel> Charges => _charges;

    private sealed record InvoiceKey(string InvoiceId) : IInvoiceKey;
}
