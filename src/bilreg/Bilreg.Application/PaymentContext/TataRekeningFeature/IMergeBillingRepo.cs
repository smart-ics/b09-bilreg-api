namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

public interface IMergeBillingRepo
{
    void SaveMergeRelation(string targetRegId, string sourceRegId);
    bool IsMerged(string sourceRegId);
    IEnumerable<string> GetMergedSourceRegIds(string targetRegId);
    string? GetTargetRegId(string sourceRegId);
}
