using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

/// <summary>
/// Infrastructure adapter implementing <see cref="IRegRelationPolicy"/> by querying
/// the authoritative Admission context (<see cref="IRegRepo"/>).
/// </summary>
public sealed class RegRelationPolicyAdapter : IRegRelationPolicy
{
    private readonly IRegRepo _regRepo;

    public RegRelationPolicyAdapter(IRegRepo regRepo)
    {
        _regRepo = regRepo ?? throw new ArgumentNullException(nameof(regRepo));
    }

    public bool IsRelated(string sourceRegId, string targetRegId)
    {
        if (string.IsNullOrWhiteSpace(sourceRegId) || string.IsNullOrWhiteSpace(targetRegId))
            return false;

        var cleanSourceId = sourceRegId.Trim();
        var cleanTargetId = targetRegId.Trim();

        if (string.Equals(cleanSourceId, cleanTargetId, StringComparison.OrdinalIgnoreCase))
            return false;

        var sourceMayBe = _regRepo.LoadEntity(RegModel.Key(cleanSourceId));
        var targetMayBe = _regRepo.LoadEntity(RegModel.Key(cleanTargetId));

        return sourceMayBe.Match(
            onSome: source => targetMayBe.Match(
                onSome: target =>
                {
                    // Both registrations must belong to the same patient
                    if (string.IsNullOrWhiteSpace(source.Pasien.PasienId) ||
                        source.Pasien.PasienId == "-" ||
                        string.IsNullOrWhiteSpace(target.Pasien.PasienId) ||
                        target.Pasien.PasienId == "-" ||
                        !string.Equals(source.Pasien.PasienId, target.Pasien.PasienId, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    // Neither registration may be voided/canceled
                    if (source.RegVoidAudit != AuditInfoType.Default ||
                        target.RegVoidAudit != AuditInfoType.Default)
                    {
                        return false;
                    }

                    return true;
                },
                onNone: () => false),
            onNone: () => false);
    }
}
