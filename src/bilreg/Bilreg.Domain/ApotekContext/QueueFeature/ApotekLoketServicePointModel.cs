using Ardalis.GuardClauses;

namespace Bilreg.Domain.ApotekContext.QueueFeature;

public class ApotekLoketServicePointModel : IEquatable<ApotekLoketServicePointModel>
{
    private ApotekLoketServicePointModel(
        string loketKey,
        string servicePointId,
        string crtUser,
        DateTime crtDate)
    {
        LoketKey = loketKey;
        ServicePointId = servicePointId;
        CrtUser = crtUser;
        CrtDate = crtDate;
    }

    public string LoketKey { get; }
    public string ServicePointId { get; }
    public string CrtUser { get; }
    public DateTime CrtDate { get; }

    public static ApotekLoketServicePointModel Create(
        string loketKey,
        string servicePointId,
        string crtUser = "SYSTEM")
    {
        Guard.Against.NullOrWhiteSpace(loketKey, nameof(loketKey));
        Guard.Against.NullOrWhiteSpace(servicePointId, nameof(servicePointId));

        return new ApotekLoketServicePointModel(
            loketKey.Trim(),
            servicePointId.Trim(),
            string.IsNullOrWhiteSpace(crtUser) ? "SYSTEM" : crtUser.Trim(),
            DateTime.Now);
    }

    public static ApotekLoketServicePointModel Rehydrate(
        string loketKey,
        string servicePointId,
        string crtUser,
        DateTime crtDate)
    {
        Guard.Against.NullOrWhiteSpace(loketKey, nameof(loketKey));
        Guard.Against.NullOrWhiteSpace(servicePointId, nameof(servicePointId));

        return new ApotekLoketServicePointModel(
            loketKey.Trim(),
            servicePointId.Trim(),
            string.IsNullOrWhiteSpace(crtUser) ? "SYSTEM" : crtUser.Trim(),
            crtDate);
    }

    public bool Equals(ApotekLoketServicePointModel? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(LoketKey, other.LoketKey, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(ServicePointId, other.ServicePointId, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((ApotekLoketServicePointModel)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            LoketKey.ToUpperInvariant(),
            ServicePointId.ToUpperInvariant());
    }
}
