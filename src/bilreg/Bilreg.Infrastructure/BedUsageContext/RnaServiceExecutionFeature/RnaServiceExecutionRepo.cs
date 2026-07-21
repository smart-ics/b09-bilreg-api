using Bilreg.Application.BedUsageContext.RnaServiceExecutionFeature;
using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;

public sealed class RnaServiceExecutionRepo : IRnaServiceExecutionRepo
{
    private readonly IRnaServiceExecutionDal _header;
    private readonly IServiceWorkSourceRevisionDal _revisions;
    private readonly IServiceExecutionFactDal _facts;
    private readonly IExecutionCorrectionDal _corrections;

    public RnaServiceExecutionRepo(IRnaServiceExecutionDal header, IServiceWorkSourceRevisionDal revisions,
        IServiceExecutionFactDal facts, IExecutionCorrectionDal corrections) =>
        (_header, _revisions, _facts, _corrections) = (header, revisions, facts, corrections);

    public void SaveChanges(RnaServiceExecutionModel model)
    {
        var dto = RnaServiceExecutionDto.FromModel(model);
        var stored = _header.GetData(model);

        MayBe.From(stored)
            .Match(
                onSome: current => UpdateHeader(dto, current),
                onNone: () => _header.Insert(dto));

        SaveDetails(model);
    }

    public MayBe<RnaServiceExecutionModel> LoadEntity(IRnaServiceExecutionKey key)
    {
        var header = _header.GetData(key);
        if (header is null)
            return MayBe<RnaServiceExecutionModel>.None;

        try
        {
            var revisions = (_revisions.ListData(key) ?? []).Select(x => x.ToModel()).ToList();
            var facts = (_facts.ListData(key) ?? []).Select(x => x.ToModel()).ToList();
            var corrections = (_corrections.ListData(key) ?? []).Select(x => x.ToModel()).ToList();

            Validate(header, revisions, facts, corrections);
            return MayBe.From(header.ToModel(revisions, facts, corrections));
        }
        catch (RnaServiceExecutionPersistenceException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw RnaServiceExecutionPersistenceException.Integrity(
                $"Aggregate {key.ServiceExecutionId} tidak dapat direkonstruksi: {ex.Message}");
        }
    }

    public void DeleteEntity(IRnaServiceExecutionKey key)
    {
        _corrections.Delete(key);
        _facts.Delete(key);
        _revisions.Delete(key);
        _header.Delete(key);
    }

    private void UpdateHeader(RnaServiceExecutionDto dto, RnaServiceExecutionDto stored)
    {
        if (dto.Version != stored.Version + 1)
            throw RnaServiceExecutionPersistenceException.Concurrency("Version aggregate tidak berurutan.");

        if (_header.UpdateConditional(dto, stored.Version) != 1)
            throw RnaServiceExecutionPersistenceException.Concurrency("Aggregate telah diubah proses lain.");
    }

    private void SaveDetails(RnaServiceExecutionModel model)
    {
        var revisions = model.ListSourceRevision
            .Select(x => ServiceWorkSourceRevisionDto.FromModel(model.ServiceExecutionId, x))
            .ToList();
        var facts = model.ListExecutionFact
            .Select(x => ServiceExecutionFactDto.FromModel(model.ServiceExecutionId, x))
            .ToList();
        var corrections = model.ListCorrection
            .Select(x => ExecutionCorrectionDto.FromModel(model.ServiceExecutionId, x))
            .ToList();

        _corrections.Delete(model);
        _facts.Delete(model);
        _revisions.Delete(model);

        _revisions.Insert(revisions);
        _facts.Insert(facts);
        _corrections.Insert(corrections);
    }

    private static void Validate(RnaServiceExecutionDto header, IReadOnlyCollection<ServiceWorkSourceRevisionType> revisions,
        IReadOnlyCollection<ServiceExecutionFactType> facts, IReadOnlyCollection<ExecutionCorrectionModel> corrections)
    {
        if (revisions.Any(x => x.SourceContext != header.SourceContext) ||
            revisions.Any(x => x.SourceRevision > header.SourceRevision))
            throw RnaServiceExecutionPersistenceException.Integrity("Source revision tidak sesuai header.");

        if (facts.GroupBy(x => x.ExecutionRevision).Any(x => x.Count() > 1))
            throw RnaServiceExecutionPersistenceException.Integrity("Execution revision duplikat.");

        var ids = facts.Select(x => x.ServiceExecutionFactId).ToHashSet(StringComparer.Ordinal);
        if (corrections.Any(x => !ids.Contains(x.OriginalServiceExecutionFactId) ||
            (!string.IsNullOrWhiteSpace(x.ReplacementServiceExecutionFactId) &&
             !ids.Contains(x.ReplacementServiceExecutionFactId))))
            throw RnaServiceExecutionPersistenceException.Integrity("Correction mengacu ke execution fact yang tidak ada.");

        if (corrections.GroupBy(x => x.CorrectionRevision).Any(x => x.Count() > 1))
            throw RnaServiceExecutionPersistenceException.Integrity("Correction revision duplikat.");
    }
}

public sealed class RnaServiceExecutionPersistenceException : InvalidOperationException
{
    private RnaServiceExecutionPersistenceException(string code, string message) : base(message) => Code = code;

    public string Code { get; }

    public static RnaServiceExecutionPersistenceException Integrity(string message) => new("INTEGRITY_CONFLICT", message);

    public static RnaServiceExecutionPersistenceException Concurrency(string message) => new("CONCURRENCY_CONFLICT", message);
}
