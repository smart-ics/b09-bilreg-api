using Bilreg.Application.PaymentContext.TataRekeningFeature;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Persists and reads the registration merge relation in <c>ta_trs_merge</c>.
/// Merge target = inpatient main registration (<c>fs_kd_reg_merge</c>),
/// merge source = preceding registration (<c>fs_kd_reg_asal</c>).
/// </summary>
public sealed class MergeBillingRepo : IMergeBillingRepo
{
    private const string SystemActor = "SYSTEM";

    private readonly IBilrgTrsMergeDal _dal;

    public MergeBillingRepo(IBilrgTrsMergeDal dal)
    {
        _dal = dal;
    }

    public void SaveMergeRelation(string targetRegId, string sourceRegId)
    {
        if (string.IsNullOrWhiteSpace(targetRegId))
            throw new ArgumentException("Target registrasi (RegRanap) wajib diisi.", nameof(targetRegId));

        if (string.IsNullOrWhiteSpace(sourceRegId))
            throw new ArgumentException("Registrasi asal (RegIGD/RegPoli) wajib diisi.", nameof(sourceRegId));

        if (_dal.Exists(targetRegId, sourceRegId))
            return;

        var now = DateTime.Now;
        _dal.Insert(new BilrgTrsMergeDto(
            MergeRegId: targetRegId,
            AsalRegId: sourceRegId,
            CrtUser: SystemActor,
            CrtDate: now,
            UpdUser: SystemActor,
            UpdDate: now));
    }

    public bool IsMerged(string sourceRegId) =>
        !string.IsNullOrWhiteSpace(_dal.GetMergeRegId(sourceRegId));

    public IEnumerable<string> GetMergedSourceRegIds(string targetRegId) =>
        _dal.ListAsalRegId(targetRegId) ?? [];

    public string? GetTargetRegId(string sourceRegId) =>
        _dal.GetMergeRegId(sourceRegId);
}
