using Ardalis.GuardClauses;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.PaymentContext.PasienBalanceFeature;

/// <summary>
/// Patient-level collection of unsettled receivables, one entry per registration.
/// </summary>
public class PasienBalanceModel : IPasienKey
{
    private const string EntryIdPrefix = "PBO";
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    private readonly List<OutstandingEntryType> _outstandingEntries = [];

    private PasienBalanceModel(
        string pasienId,
        int version,
        IEnumerable<OutstandingEntryType> outstandingEntries)
    {
        PasienId = pasienId;
        Version = version;
        _outstandingEntries = outstandingEntries.ToList();
        AssertInvariants();
    }

    #region CREATION

    public static PasienBalanceModel Create(string pasienId)
    {
        Guard.Against.NullOrWhiteSpace(pasienId, nameof(pasienId));
        return new PasienBalanceModel(pasienId, version: 0, outstandingEntries: []);
    }

    public static PasienBalanceModel Default =>
        new("-", 0, []);

    public static IPasienKey Key(string pasienId) =>
        Hydrate(pasienId, 0, []);

    public static PasienBalanceModel Hydrate(
        string pasienId,
        int version,
        IEnumerable<OutstandingEntryType> outstandingEntries) =>
        new(pasienId, version, outstandingEntries);

    #endregion

    #region PROPERTIES

    public string PasienId { get; init; }
    public int Version { get; private set; }
    public IEnumerable<OutstandingEntryType> OutstandingEntries => _outstandingEntries;

    public decimal TotalOutstandingJasa =>
        _outstandingEntries.Sum(x => x.OutstandingJasa);

    public decimal TotalOutstandingObat =>
        _outstandingEntries.Sum(x => x.OutstandingObat);

    public decimal TotalOutstanding =>
        _outstandingEntries.Sum(x => x.OutstandingTotal);

    #endregion

    #region BEHAVIOUR

    public void AddOutstanding(
        string regId,
        decimal outstandingJasa,
        decimal outstandingObat,
        DateTime lastTransactionDate,
        string sourceReference,
        string createdBy)
    {
        EnsureValidRegId(regId);
        EnsureNonNegativeAmount(outstandingJasa, nameof(outstandingJasa));
        EnsureNonNegativeAmount(outstandingObat, nameof(outstandingObat));
        EnsureValidTransactionDate(lastTransactionDate);
        EnsureValidUser(createdBy);

        if (_outstandingEntries.Any(x => x.RegId == regId))
            throw new InvalidOperationException(
                $"Outstanding untuk registrasi {regId} sudah ada.");

        var now = lastTransactionDate;
        var entry = new OutstandingEntryType(
            NunaId.New(EntryIdPrefix),
            PasienId,
            regId,
            outstandingJasa,
            outstandingObat,
            lastTransactionDate,
            sourceReference ?? string.Empty,
            now,
            createdBy,
            isPersisted: false);

        _outstandingEntries.Add(entry);
        AssertInvariants();
    }

    public void UpdateOutstanding(
        string regId,
        decimal outstandingJasa,
        decimal outstandingObat,
        DateTime lastTransactionDate,
        string sourceReference)
    {
        EnsureValidRegId(regId);
        EnsureNonNegativeAmount(outstandingJasa, nameof(outstandingJasa));
        EnsureNonNegativeAmount(outstandingObat, nameof(outstandingObat));
        EnsureValidTransactionDate(lastTransactionDate);

        var index = FindEntryIndex(regId);
        var existing = _outstandingEntries[index];

        _outstandingEntries[index] = existing with
        {
            OutstandingJasa = outstandingJasa,
            OutstandingObat = outstandingObat,
            LastTransactionDate = lastTransactionDate,
            SourceReference = sourceReference ?? string.Empty,
            IsPersisted = false
        };

        AssertInvariants();
    }

    public void RemoveOutstanding(string regId)
    {
        EnsureValidRegId(regId);

        var index = FindEntryIndex(regId);
        _outstandingEntries.RemoveAt(index);
        AssertInvariants();
    }

    public void ReplaceOutstandingEntries(
        IEnumerable<OutstandingEntryType> entries,
        string createdBy)
    {
        EnsureValidUser(createdBy);

        var list = entries.ToList();
        var duplicateRegIds = list
            .GroupBy(x => x.RegId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateRegIds.Count > 0)
            throw new InvalidOperationException(
                $"RegId duplikat tidak diizinkan: {string.Join(", ", duplicateRegIds)}.");

        foreach (var entry in list)
        {
            if (entry.PasienId != PasienId)
                throw new InvalidOperationException(
                    $"Entry {entry.EntryId} tidak milik pasien {PasienId}.");

            EnsureNonNegativeAmount(entry.OutstandingJasa, nameof(entry.OutstandingJasa));
            EnsureNonNegativeAmount(entry.OutstandingObat, nameof(entry.OutstandingObat));
            EnsureValidRegId(entry.RegId);
            EnsureValidTransactionDate(entry.LastTransactionDate);
        }

        _outstandingEntries.Clear();

        var now = DateTime.Now;
        foreach (var entry in list)
        {
            _outstandingEntries.Add(new OutstandingEntryType(
                string.IsNullOrWhiteSpace(entry.EntryId) ? NunaId.New(EntryIdPrefix) : entry.EntryId,
                PasienId,
                entry.RegId,
                entry.OutstandingJasa,
                entry.OutstandingObat,
                entry.LastTransactionDate,
                entry.SourceReference,
                entry.CreatedAt == EmptyDate || entry.CreatedAt == DateTime.MinValue ? now : entry.CreatedAt,
                string.IsNullOrWhiteSpace(entry.CreatedBy) ? createdBy : entry.CreatedBy,
                isPersisted: false));
        }

        AssertInvariants();
    }

    public void CommitVersionIncrement() => Version++;

    #endregion

    private int FindEntryIndex(string regId)
    {
        var index = _outstandingEntries.FindIndex(x => x.RegId == regId);
        if (index < 0)
            throw new InvalidOperationException(
                $"Outstanding untuk registrasi {regId} tidak ditemukan.");
        return index;
    }

    private void AssertInvariants()
    {
        if (_outstandingEntries.Any(x => x.PasienId != PasienId))
            throw new InvalidOperationException(
                "Invariant PasienBalance dilanggar: entry tidak milik pasien aggregate.");

        var duplicateRegIds = _outstandingEntries
            .GroupBy(x => x.RegId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateRegIds.Count > 0)
            throw new InvalidOperationException(
                "Invariant PasienBalance dilanggar: RegId duplikat dalam aggregate.");

        if (_outstandingEntries.Any(x => x.OutstandingJasa < 0m || x.OutstandingObat < 0m))
            throw new InvalidOperationException(
                "Invariant PasienBalance dilanggar: outstanding tidak boleh negatif.");

        var expectedTotal = _outstandingEntries.Sum(x => x.OutstandingTotal);
        if (TotalOutstanding != expectedTotal)
            throw new InvalidOperationException(
                "Invariant PasienBalance dilanggar: TotalOutstanding tidak sama dengan jumlah entry.");
    }

    private static void EnsureValidRegId(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            throw new ArgumentException("RegId tidak boleh kosong.", nameof(regId));
    }

    private static void EnsureNonNegativeAmount(decimal amount, string paramName)
    {
        if (amount < 0m)
            throw new ArgumentException("Nilai tidak boleh negatif.", paramName);
    }

    private static void EnsureValidTransactionDate(DateTime lastTransactionDate)
    {
        if (lastTransactionDate == EmptyDate || lastTransactionDate == DateTime.MinValue)
            throw new ArgumentException("Tanggal transaksi tidak valid.", nameof(lastTransactionDate));
    }

    private static void EnsureValidUser(string createdBy)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("User pembuat tidak boleh kosong.", nameof(createdBy));
    }
}
