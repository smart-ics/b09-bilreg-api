using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Phase 3 bridge repository — replaced by SQL-backed implementation in Phase 4.
/// </summary>
public sealed class InMemoryMergeRequestRepo : IMergeRequestRepo
{
    private readonly Dictionary<string, MergeRequestModel> _store = new(StringComparer.Ordinal);
    private readonly IRegRepo _regRepo;

    public InMemoryMergeRequestRepo(IRegRepo regRepo)
    {
        _regRepo = regRepo;
    }

    public void SaveChanges(MergeRequestModel model) =>
        _store[model.MergeRequestId] = model;

    public MayBe<MergeRequestModel> LoadEntity(IMergeRequestKey key)
    {
        if (_store.TryGetValue(key.MergeRequestId, out var model))
            return MayBe.From(model);

        return MayBe<MergeRequestModel>.None;
    }

    public IEnumerable<MergeRequestModel> ListPendingByReg(IRegKey regKey) =>
        _store.Values.Where(m =>
            m.Status == MergeRequestStatusEnum.Pending &&
            (string.Equals(m.SourceRegId, regKey.RegId, StringComparison.Ordinal) ||
             string.Equals(m.TargetRegId, regKey.RegId, StringComparison.Ordinal)));

    public IEnumerable<MergeRequestModel> ListPendingByPatient(string pasienId) =>
        _store.Values.Where(m =>
            m.Status == MergeRequestStatusEnum.Pending &&
            string.IsNullOrWhiteSpace(m.TargetRegId) &&
            string.Equals(ResolvePasienId(m.SourceRegId), pasienId, StringComparison.Ordinal));

    private string? ResolvePasienId(string sourceRegId)
    {
        var regKey = RegModel.Key(sourceRegId);
        return _regRepo.LoadEntity(regKey)
            .Match(onSome: r => r.Pasien.PasienId, onNone: () => (string?)null);
    }
}
