namespace Bilreg.Domain.PaymentContext.PasienBalanceFeature;

public record PasienBalanceHistoryType
{
    public PasienBalanceHistoryType(
        string historyId,
        string pasienId,
        string regId,
        DateTime trsDate,
        decimal openingJasaBalance,
        decimal openingObatBalance,
        decimal chargeJasa,
        decimal chargeObat,
        decimal paymentJasa,
        decimal paymentObat,
        decimal closingJasaBalance,
        decimal closingObatBalance,
        string remarks,
        DateTime createdAt,
        string createdBy,
        bool isPersisted)
    {
        HistoryId = historyId;
        PasienId = pasienId;
        RegId = regId ?? string.Empty;
        TrsDate = trsDate;
        OpeningJasaBalance = openingJasaBalance;
        OpeningObatBalance = openingObatBalance;
        ChargeJasa = chargeJasa;
        ChargeObat = chargeObat;
        PaymentJasa = paymentJasa;
        PaymentObat = paymentObat;
        ClosingJasaBalance = closingJasaBalance;
        ClosingObatBalance = closingObatBalance;
        Remarks = remarks ?? string.Empty;
        CreatedAt = createdAt;
        CreatedBy = createdBy ?? string.Empty;
        IsPersisted = isPersisted;
    }

    public string HistoryId { get; init; }
    public string PasienId { get; init; }
    public string RegId { get; init; }
    public DateTime TrsDate { get; init; }
    public decimal OpeningJasaBalance { get; init; }
    public decimal OpeningObatBalance { get; init; }
    public decimal ChargeJasa { get; init; }
    public decimal ChargeObat { get; init; }
    public decimal PaymentJasa { get; init; }
    public decimal PaymentObat { get; init; }
    public decimal ClosingJasaBalance { get; init; }
    public decimal ClosingObatBalance { get; init; }
    public decimal ClosingBalance => ClosingJasaBalance + ClosingObatBalance;
    public string Remarks { get; init; }
    public DateTime CreatedAt { get; init; }
    public string CreatedBy { get; init; }
    public bool IsPersisted { get; init; }
}
