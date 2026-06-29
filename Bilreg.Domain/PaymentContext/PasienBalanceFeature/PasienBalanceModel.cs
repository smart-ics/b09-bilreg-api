using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PaymentContext.PasienBalanceFeature;

/// <summary>
/// Patient-level cumulative outstanding balance across registrations, partitioned by JASA and OBAT.
/// </summary>
public class PasienBalanceModel : IPasienKey
{
    private const string HistoryIdPrefix = "PBH";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    private readonly List<PasienBalanceHistoryType> _listHistory = [];

    private PasienBalanceModel(
        string pasienId,
        decimal currentJasaBalance,
        decimal currentObatBalance,
        string lastHistoryId,
        DateTime updatedAt,
        int version,
        string lastModifiedBy,
        IEnumerable<PasienBalanceHistoryType> listHistory)
    {
        PasienId = pasienId;
        CurrentJasaBalance = currentJasaBalance;
        CurrentObatBalance = currentObatBalance;
        LastHistoryId = lastHistoryId;
        UpdatedAt = updatedAt;
        Version = version;
        LastModifiedBy = lastModifiedBy;
        _listHistory = listHistory.ToList();
        AssertBalanceConsistent();
    }

    #region CREATION

    public static PasienBalanceModel Create(string pasienId)
    {
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));

        return new PasienBalanceModel(
            pasienId,
            currentJasaBalance: 0m,
            currentObatBalance: 0m,
            lastHistoryId: "-",
            updatedAt: EmptyDate,
            version: 0,
            lastModifiedBy: string.Empty,
            listHistory: []);
    }

    public static PasienBalanceModel Default =>
        new("-", 0m, 0m, "-", EmptyDate, 0, string.Empty, []);

    public static IPasienKey Key(string pasienId) =>
        Hydrate(pasienId, 0m, 0m, "-", EmptyDate, 0, string.Empty, []);

    public static PasienBalanceModel Hydrate(
        string pasienId,
        decimal currentJasaBalance,
        decimal currentObatBalance,
        string lastHistoryId,
        DateTime updatedAt,
        int version,
        string lastModifiedBy,
        IEnumerable<PasienBalanceHistoryType> listHistory) =>
        new(pasienId, currentJasaBalance, currentObatBalance, lastHistoryId, updatedAt, version, lastModifiedBy, listHistory);

    #endregion

    #region PROPERTIES

    public string PasienId { get; init; }
    public decimal CurrentJasaBalance { get; private set; }
    public decimal CurrentObatBalance { get; private set; }
    public decimal CurrentBalance => CurrentJasaBalance + CurrentObatBalance;
    public string LastHistoryId { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int Version { get; private set; }
    public string LastModifiedBy { get; private set; }
    public IEnumerable<PasienBalanceHistoryType> ListHistory => _listHistory;

    public IEnumerable<PasienBalanceHistoryType> PendingHistory =>
        _listHistory.Where(x => !x.IsPersisted);

    #endregion

    #region BEHAVIOUR

    public void ApplyCharge(
        decimal jasaAmount,
        decimal obatAmount,
        string regId,
        DateTime trsDate,
        string remarks,
        string createdBy)
    {
        EnsureNonNegativeAmount(jasaAmount, nameof(jasaAmount));
        EnsureNonNegativeAmount(obatAmount, nameof(obatAmount));
        EnsureAtLeastOnePositive(jasaAmount, obatAmount);
        EnsureValidTrsDate(trsDate);
        EnsureValidUser(createdBy);

        AppendHistory(
            regId ?? string.Empty,
            trsDate,
            chargeJasa: jasaAmount,
            chargeObat: obatAmount,
            paymentJasa: 0m,
            paymentObat: 0m,
            remarks,
            createdBy);
    }

    public void ApplyPayment(
        decimal jasaAmount,
        decimal obatAmount,
        string regId,
        DateTime trsDate,
        string remarks,
        string createdBy)
    {
        EnsureNonNegativeAmount(jasaAmount, nameof(jasaAmount));
        EnsureNonNegativeAmount(obatAmount, nameof(obatAmount));
        EnsureAtLeastOnePositive(jasaAmount, obatAmount);
        EnsureValidTrsDate(trsDate);
        EnsureValidUser(createdBy);

        if (jasaAmount > CurrentJasaBalance)
            throw new InvalidOperationException(
                $"Pembayaran JASA ({jasaAmount}) melebihi saldo outstanding JASA ({CurrentJasaBalance}).");

        if (obatAmount > CurrentObatBalance)
            throw new InvalidOperationException(
                $"Pembayaran OBAT ({obatAmount}) melebihi saldo outstanding OBAT ({CurrentObatBalance}).");

        AppendHistory(
            regId ?? string.Empty,
            trsDate,
            chargeJasa: 0m,
            chargeObat: 0m,
            paymentJasa: jasaAmount,
            paymentObat: obatAmount,
            remarks,
            createdBy);
    }

    public void CommitVersionIncrement() => Version++;

    #endregion

    private void AppendHistory(
        string regId,
        DateTime trsDate,
        decimal chargeJasa,
        decimal chargeObat,
        decimal paymentJasa,
        decimal paymentObat,
        string remarks,
        string createdBy)
    {
        var openingJasa = CurrentJasaBalance;
        var openingObat = CurrentObatBalance;
        var closingJasa = openingJasa + chargeJasa - paymentJasa;
        var closingObat = openingObat + chargeObat - paymentObat;

        if (closingJasa < 0m)
            throw new InvalidOperationException(
                "Operasi saldo JASA menghasilkan outstanding negatif yang tidak diizinkan.");

        if (closingObat < 0m)
            throw new InvalidOperationException(
                "Operasi saldo OBAT menghasilkan outstanding negatif yang tidak diizinkan.");

        var historyId = NunaId.New(HistoryIdPrefix);
        var entry = new PasienBalanceHistoryType(
            historyId,
            PasienId,
            regId,
            trsDate,
            openingJasa,
            openingObat,
            chargeJasa,
            chargeObat,
            paymentJasa,
            paymentObat,
            closingJasa,
            closingObat,
            remarks,
            trsDate,
            createdBy,
            isPersisted: false);

        _listHistory.Add(entry);
        CurrentJasaBalance = closingJasa;
        CurrentObatBalance = closingObat;
        LastHistoryId = historyId;
        UpdatedAt = trsDate;
        LastModifiedBy = createdBy;
        AssertBalanceConsistent();
    }

    private void AssertBalanceConsistent()
    {
        if (_listHistory.Count == 0)
        {
            if (CurrentJasaBalance != 0m || CurrentObatBalance != 0m)
                throw new InvalidOperationException(
                    "Invariant PasienBalance dilanggar: saldo partition tidak nol tanpa history.");
            return;
        }

        var latest = _listHistory[^1];

        if (CurrentJasaBalance != latest.ClosingJasaBalance)
            throw new InvalidOperationException(
                "Invariant PasienBalance dilanggar: CurrentJasaBalance tidak sama dengan ClosingJasaBalance history terakhir.");

        if (CurrentObatBalance != latest.ClosingObatBalance)
            throw new InvalidOperationException(
                "Invariant PasienBalance dilanggar: CurrentObatBalance tidak sama dengan ClosingObatBalance history terakhir.");

        if (CurrentBalance != latest.ClosingBalance)
            throw new InvalidOperationException(
                "Invariant PasienBalance dilanggar: CurrentBalance tidak sama dengan ClosingBalance history terakhir.");
    }

    private static void EnsureNonNegativeAmount(decimal amount, string paramName)
    {
        if (amount < 0m)
            throw new ArgumentException("Nilai tidak boleh negatif.", paramName);
    }

    private static void EnsureAtLeastOnePositive(decimal jasaAmount, decimal obatAmount)
    {
        if (jasaAmount == 0m && obatAmount == 0m)
            throw new ArgumentException("Minimal satu nilai JASA atau OBAT harus lebih besar dari nol.");
    }

    private static void EnsureValidTrsDate(DateTime trsDate)
    {
        if (trsDate == EmptyDate || trsDate == DateTime.MinValue)
            throw new ArgumentException("Tanggal transaksi tidak valid.", nameof(trsDate));
    }

    private static void EnsureValidUser(string createdBy)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("User pembuat tidak boleh kosong.", nameof(createdBy));
    }
}
