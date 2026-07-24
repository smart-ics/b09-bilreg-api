using Ardalis.GuardClauses;

namespace Bilreg.Application.AdmisiContext.RegFeature;

/// <summary>
/// External ownership boundary for Registration Outcome ReasonCodes (GAP-AQO-013).
/// Until Operations publishes the approved catalog, the default implementation only
/// rejects null/whitespace and invents no business codes.
/// </summary>
public interface IRegistrationOutcomeReasonCatalog
{
    void EnsureAccepted(string reasonCode);
}

/// <summary>
/// Pass-through catalog: non-empty only. Replace when Ops supplies the approved list.
/// </summary>
public sealed class PassThroughRegistrationOutcomeReasonCatalog : IRegistrationOutcomeReasonCatalog
{
    public void EnsureAccepted(string reasonCode) =>
        Guard.Against.NullOrWhiteSpace(reasonCode, nameof(reasonCode));
}
