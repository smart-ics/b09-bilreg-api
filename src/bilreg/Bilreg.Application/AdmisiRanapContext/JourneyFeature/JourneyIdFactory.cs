using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;

namespace Bilreg.Application.AdmisiRanapContext.JourneyFeature;

/// <summary>
/// Deterministic opaque JourneyId. Format is an implementation detail; clients must not parse it.
/// </summary>
public static class JourneyIdFactory
{
    public const string OpnamePrefix = "opn:";
    public const string ReservationPrefix = "rsv:";
    public const string RegPrefix = "reg:";

    public static string FromOpnameRequest(string opnameRequestId)
    {
        EnsureId(opnameRequestId, nameof(opnameRequestId));
        return OpnamePrefix + opnameRequestId.Trim();
    }

    public static string FromReservation(string reservationId)
    {
        EnsureId(reservationId, nameof(reservationId));
        return ReservationPrefix + reservationId.Trim();
    }

    public static string FromRegId(string regId)
    {
        EnsureId(regId, nameof(regId));
        return RegPrefix + regId.Trim();
    }

    /// <summary>
    /// Derives the canonical JourneyId for valid data. On integrity failure involving a conflicting
    /// Admission, falls back to <c>reg:{RegId}</c> so journeys are not silently merged.
    /// </summary>
    public static string Derive(JourneyNormalizedFacts facts, JourneyIntegrityResult integrity)
    {
        if (integrity.HasBlockingIssues)
        {
            var primaryAdmission = SelectPrimaryAdmission(facts);
            if (primaryAdmission is not null)
                return FromRegId(primaryAdmission.RegId);
        }

        return facts.OriginKind switch
        {
            JourneyOriginKind.OpnameRequest when facts.OpnameRequest is not null
                => FromOpnameRequest(facts.OpnameRequest.OpnameRequestId),
            JourneyOriginKind.Reservation when facts.Reservation is not null
                => FromReservation(facts.Reservation.ReservationId),
            JourneyOriginKind.DirectOrLegacyAdmission when SelectPrimaryAdmission(facts) is { } admission
                => FromRegId(admission.RegId),
            JourneyOriginKind.OpnameRequest when SelectPrimaryAdmission(facts) is { } a
                && HasValue(a.OpnameRequestId)
                => FromOpnameRequest(a.OpnameRequestId),
            JourneyOriginKind.Reservation when SelectPrimaryAdmission(facts) is { } a
                && HasValue(a.ReservationId)
                => FromReservation(a.ReservationId),
            _ when SelectPrimaryAdmission(facts) is { } fallback
                => FromRegId(fallback.RegId),
            _ => throw new InvalidOperationException(
                "Cannot derive JourneyId: facts contain neither a prospective source nor an Admission.")
        };
    }

    /// <summary>
    /// Resolves a legacy aggregate deep-link to a JourneyId when the related facts are already known.
    /// Persistence lookup belongs to Phase B2/B3.
    /// </summary>
    public static string ResolveLegacyRecord(
        string recordType,
        string recordId,
        JourneyNormalizedFacts? knownFacts = null,
        JourneyIntegrityResult? integrity = null)
    {
        EnsureId(recordId, nameof(recordId));
        var type = (recordType ?? string.Empty).Trim().ToLowerInvariant();

        return type switch
        {
            "opnamerequest" or "opname" or "opn" => FromOpnameRequest(recordId),
            "reservation" or "rsv" => FromReservation(recordId),
            "admission" or "registration" or "reg" =>
                knownFacts is null
                    ? FromRegId(recordId)
                    : Derive(knownFacts with
                    {
                        // Prefer source-stable ID when the Admission facts are supplied.
                        Admissions = knownFacts.Admissions.Count > 0
                            ? knownFacts.Admissions
                            : new[]
                            {
                                new AdmissionJourneyFact(
                                    recordId,
                                    AdmissionStatusEnum.Admitted,
                                    "-",
                                    "-")
                            }
                    }, integrity ?? JourneyIntegrityResult.Empty),
            "waitinglist" or "wl" or "wtl" =>
                knownFacts is null
                    ? throw new InvalidOperationException(
                        "Waiting List legacy resolution requires known journey facts (Admission/source).")
                    : Derive(knownFacts, integrity ?? JourneyIntegrityValidator.Validate(knownFacts)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(recordType),
                recordType,
                "Supported legacy types: opnameRequest, reservation, admission, registration, waitingList.")
        };
    }

    public static bool TryParse(string journeyId, out string prefix, out string domainId)
    {
        prefix = string.Empty;
        domainId = string.Empty;
        if (string.IsNullOrWhiteSpace(journeyId))
            return false;

        if (journeyId.StartsWith(OpnamePrefix, StringComparison.Ordinal))
        {
            prefix = OpnamePrefix;
            domainId = journeyId[OpnamePrefix.Length..];
            return domainId.Length > 0;
        }

        if (journeyId.StartsWith(ReservationPrefix, StringComparison.Ordinal))
        {
            prefix = ReservationPrefix;
            domainId = journeyId[ReservationPrefix.Length..];
            return domainId.Length > 0;
        }

        if (journeyId.StartsWith(RegPrefix, StringComparison.Ordinal))
        {
            prefix = RegPrefix;
            domainId = journeyId[RegPrefix.Length..];
            return domainId.Length > 0;
        }

        return false;
    }

    internal static AdmissionJourneyFact? SelectPrimaryAdmission(JourneyNormalizedFacts facts)
    {
        if (facts.Admissions.Count == 0)
            return null;

        if (facts.Admissions.Count == 1)
            return facts.Admissions[0];

        // Prefer non-terminal when multiple exist; integrity already flagged the conflict.
        return facts.Admissions.FirstOrDefault(a =>
                   a.Status is not AdmissionStatusEnum.Cancelled
                       and not AdmissionStatusEnum.Completed)
               ?? facts.Admissions[0];
    }

    internal static bool HasValue(string? id) =>
        !string.IsNullOrWhiteSpace(id) && id != "-";

    private static void EnsureId(string id, string paramName)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Trim() == "-")
            throw new ArgumentException("Identifier is required.", paramName);
    }
}
