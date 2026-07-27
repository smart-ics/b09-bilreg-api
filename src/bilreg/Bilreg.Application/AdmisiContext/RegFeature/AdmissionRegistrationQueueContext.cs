namespace Bilreg.Application.AdmisiContext.RegFeature;

/// <summary>
/// Transient instruction for Registration create orchestration. It is not persisted and does not
/// represent an Admission Queue or workspace state.
/// </summary>
public enum RegistrationAdmissionQueueBehavior
{
    LegacyAutoComplete,
    QueueLinked,
    None
}

public sealed record AdmissionRegistrationQueueContext(
    string AntrianId,
    int NoUrut,
    string LoketKey,
    byte[] ExpectedRowVersion);

public static class AdmissionRegistrationQueueContextResolver
{
    public static bool HasAny(string? antrianId, int? noUrut, string? expectedRowVersion) =>
        !string.IsNullOrWhiteSpace(antrianId) ||
        noUrut.HasValue ||
        !string.IsNullOrWhiteSpace(expectedRowVersion);

    public static bool HasAnyDirectQueueField(
        string? antrianId,
        int? noUrut,
        string? expectedRowVersion) =>
        antrianId is not null || noUrut.HasValue || expectedRowVersion is not null;

    public static RegistrationAdmissionQueueBehavior ResolveBehavior(
        string? antrianId,
        int? noUrut,
        string? expectedRowVersion,
        bool isDirect)
    {
        if (isDirect)
        {
            if (HasAnyDirectQueueField(antrianId, noUrut, expectedRowVersion))
                throw new ArgumentException(
                    "Direct Registration must not include Admission Queue context.");

            return RegistrationAdmissionQueueBehavior.None;
        }

        var hasContext = HasAny(antrianId, noUrut, expectedRowVersion);
        if (!hasContext)
            return RegistrationAdmissionQueueBehavior.LegacyAutoComplete;

        ValidateCompleteContext(antrianId, noUrut, expectedRowVersion);
        return RegistrationAdmissionQueueBehavior.QueueLinked;
    }

    public static AdmissionRegistrationQueueContext? Resolve(
        string? antrianId,
        int? noUrut,
        string? expectedRowVersion,
        string? serverResolvedLoketKey)
    {
        if (!HasAny(antrianId, noUrut, expectedRowVersion))
            return null;

        ValidateCompleteContext(antrianId, noUrut, expectedRowVersion);

        if (string.IsNullOrWhiteSpace(serverResolvedLoketKey))
            throw new ArgumentException("A server-resolved Admission Loket is required.");

        byte[] version;
        try
        {
            version = Convert.FromBase64String(expectedRowVersion!);
        }
        catch (FormatException)
        {
            throw new ArgumentException("AdmissionExpectedRowVersion must be Base64.");
        }

        return new AdmissionRegistrationQueueContext(
            antrianId!.Trim(),
            noUrut!.Value,
            serverResolvedLoketKey.Trim(),
            version);
    }

    private static void ValidateCompleteContext(
        string? antrianId,
        int? noUrut,
        string? expectedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(antrianId) ||
            noUrut is not > 0 ||
            string.IsNullOrWhiteSpace(expectedRowVersion))
        {
            throw new ArgumentException(
                "AdmissionAntrianId, AdmissionNoUrut, and AdmissionExpectedRowVersion must be supplied together.");
        }
    }
}
