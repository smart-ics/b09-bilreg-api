using Bilreg.Application.BedUsageContext.BedOperationalFeature;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public sealed class BedOperationalRepo : IBedOperationalRepo
{
    private readonly IBedOperationalDal _headerDal;
    private readonly IBedReadinessTransactionDal _transactionDal;
    private readonly IBedReadinessCorrectionDal _correctionDal;

    public BedOperationalRepo(
        IBedOperationalDal headerDal,
        IBedReadinessTransactionDal transactionDal,
        IBedReadinessCorrectionDal correctionDal)
    {
        _headerDal = headerDal;
        _transactionDal = transactionDal;
        _correctionDal = correctionDal;
    }

    public void SaveChanges(BedOperationalModel model)
    {
        var storedHeader = _headerDal.GetData(model);
        var headerDto = BedOperationalDto.FromModel(model);

        if (storedHeader is not null && model.Version != storedHeader.Version + 1)
            throw BedOperationalPersistenceException.Concurrency(
                $"Versi Bed operational {model.BedId} harus " +
                $"{storedHeader.Version + 1}, bukan {model.Version}.");

        if (storedHeader is null)
            _headerDal.Insert(headerDto);
        else if (_headerDal.UpdateConditional(headerDto, storedHeader.Version) != 1)
            throw BedOperationalPersistenceException.Concurrency(
                $"Bed operational {model.BedId} telah diubah oleh proses lain.");

        InsertNewTransactions(model);
        InsertNewCorrections(model);
    }

    public MayBe<BedOperationalModel> LoadEntity(IBedOperationalKey key)
    {
        var header = _headerDal.GetData(key);
        if (header is null)
            return MayBe<BedOperationalModel>.None;

        try
        {
            var transactions = (_transactionDal.ListData(key) ?? [])
                .Select(x => x.ToModel())
                .ToList();
            var corrections = (_correctionDal.ListData(key) ?? [])
                .Select(x => x.ToModel())
                .ToList();

            ValidateStoredHistory(header, transactions, corrections);
            return MayBe.From(header.ToModel(transactions, corrections));
        }
        catch (BedOperationalPersistenceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw BedOperationalPersistenceException.Integrity(
                $"Data Bed operational {key.BedId} tidak dapat direkonstruksi. {ex.Message}");
        }
    }

    private void InsertNewTransactions(BedOperationalModel model)
    {
        var existingIds = (_transactionDal.ListData(model) ?? [])
            .Select(x => x.TransactionId)
            .ToHashSet(StringComparer.Ordinal);
        var rows = model.ListReadinessTransaction
            .Where(x => !existingIds.Contains(x.TransactionId))
            .Select(BedReadinessTransactionDto.FromModel);
        _transactionDal.Insert(rows);
    }

    private void InsertNewCorrections(BedOperationalModel model)
    {
        var existingIds = (_correctionDal.ListData(model) ?? [])
            .Select(x => x.CorrectionId)
            .ToHashSet(StringComparer.Ordinal);
        var rows = model.ListReadinessCorrection
            .Where(x => !existingIds.Contains(x.CorrectionId))
            .Select(BedReadinessCorrectionDto.FromModel);
        _correctionDal.Insert(rows);
    }

    private static void ValidateStoredHistory(
        BedOperationalDto header,
        IReadOnlyCollection<BedReadinessTransactionModel> transactions,
        IReadOnlyCollection<BedReadinessCorrectionModel> corrections)
    {
        if (transactions.Any(x => x.BedId != header.BedId) ||
            corrections.Any(x => x.BedId != header.BedId))
            throw BedOperationalPersistenceException.Integrity(
                $"Riwayat Bed operational {header.BedId} berisi BedId yang berbeda.");

        var transactionIds = transactions
            .Select(x => x.TransactionId)
            .ToHashSet(StringComparer.Ordinal);
        if (corrections.Any(x =>
                !transactionIds.Contains(x.OriginalTransactionId) ||
                !transactionIds.Contains(x.ReplacementTransactionId)))
            throw BedOperationalPersistenceException.Integrity(
                $"Koreksi Bed operational {header.BedId} memiliki referensi transaksi yang hilang.");

        if (corrections
            .GroupBy(x => x.OriginalTransactionId, StringComparer.Ordinal)
            .Any(x => x.Count() > 1))
            throw BedOperationalPersistenceException.Integrity(
                $"Riwayat Bed operational {header.BedId} mengoreksi transaksi yang sama lebih dari sekali.");

        var correctedIds = corrections
            .Select(x => x.OriginalTransactionId)
            .ToHashSet(StringComparer.Ordinal);
        var latest = transactions
            .Where(x => !correctedIds.Contains(x.TransactionId))
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.TransactionId, StringComparer.Ordinal)
            .LastOrDefault();

        if (latest is null)
        {
            if (header.CurrentReadiness is not null ||
                !string.IsNullOrEmpty(header.LatestReadinessTransactionId) ||
                header.IsBlocked)
                throw BedOperationalPersistenceException.Integrity(
                    $"Proyeksi Bed operational {header.BedId} tidak sesuai dengan riwayat kosong.");
            return;
        }

        var expectedBlocked = latest.NewStatus != BedReadinessStatusEnum.Ready;
        if (header.LatestReadinessTransactionId != latest.TransactionId ||
            header.CurrentReadiness != (int)latest.NewStatus ||
            header.IsBlocked != expectedBlocked ||
            header.CurrentRestrictionType != (int)latest.RestrictionType ||
            header.CurrentBlockerReason != (expectedBlocked ? latest.Reason : string.Empty) ||
            header.CurrentBlockerTransactionId !=
                (expectedBlocked ? latest.TransactionId : string.Empty))
            throw BedOperationalPersistenceException.Integrity(
                $"Proyeksi current-state Bed operational {header.BedId} tidak sesuai dengan riwayat.");
    }
}

public sealed class BedOperationalPersistenceException : InvalidOperationException
{
    private BedOperationalPersistenceException(string code, string message)
        : base(message) =>
        Code = code;

    public string Code { get; }

    public static BedOperationalPersistenceException Integrity(string message) =>
        new("INTEGRITY_CONFLICT", message);

    public static BedOperationalPersistenceException Concurrency(string message) =>
        new("CONCURRENCY_CONFLICT", message);
}
