using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public record ExecutionAuthorityType(
    string AuthorityBasisReference,
    bool RequiresSubsequentAuthorization,
    DateTime? AuthorizationDueAt,
    SubsequentAuthorizationStatusEnum SubsequentAuthorizationStatus)
{
    public static ExecutionAuthorityType Create(
        string authorityBasisReference,
        bool requiresSubsequentAuthorization,
        DateTime? authorizationDueAt)
    {
        Guard.Against.NullOrWhiteSpace(authorityBasisReference);
        if (authorizationDueAt.HasValue && authorizationDueAt.Value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("AuthorizationDueAt harus menggunakan DateTimeKind.Utc.", nameof(authorizationDueAt));
        if (requiresSubsequentAuthorization && !authorizationDueAt.HasValue)
            throw new ArgumentException(
                "AuthorizationDueAt wajib diisi bila subsequent authorization diperlukan.",
                nameof(authorizationDueAt));

        return new ExecutionAuthorityType(
            authorityBasisReference,
            requiresSubsequentAuthorization,
            authorizationDueAt,
            requiresSubsequentAuthorization
                ? SubsequentAuthorizationStatusEnum.Required
                : SubsequentAuthorizationStatusEnum.NotRequired);
    }

    public static ExecutionAuthorityType Default => new(
        "-", false, null, SubsequentAuthorizationStatusEnum.NotRequired);

    public ExecutionAuthorityType AssociateStatus(SubsequentAuthorizationStatusEnum status)
    {
        if (!RequiresSubsequentAuthorization)
            throw new InvalidOperationException("Execution ini tidak memerlukan subsequent authorization.");
        if (status is SubsequentAuthorizationStatusEnum.NotRequired or SubsequentAuthorizationStatusEnum.Required)
            throw new ArgumentOutOfRangeException(nameof(status), status, null);

        return this with { SubsequentAuthorizationStatus = status };
    }
}
