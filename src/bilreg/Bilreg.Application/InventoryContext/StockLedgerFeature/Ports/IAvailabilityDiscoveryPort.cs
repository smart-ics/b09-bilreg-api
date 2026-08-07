using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// G-08 — Availability Discovery contract.
/// Finds Receipt Sources with authoritative available quantity for outbound stock
/// by Item + Stock Location (optional Expiration Date filter).
/// Returns provisional candidates — not final FIFO allocation.
/// <para>
/// Distinct from <see cref="IProvenanceDiscoveryPort"/>.
/// Authoritative coexistence input is Legacy Stock Authority; synchronized Stock Ledger
/// layers may enrich candidates later but must not be treated as authority merely because
/// reconstruction once completed.
/// </para>
/// Phase ownership: behavior in Phase 2/5. Phase 1 defines the contract only.
/// </summary>
public interface IAvailabilityDiscoveryPort
{
    AvailabilityDiscoveryResult Discover(
        IBrgKey item,
        ILayananKey stockLocation,
        DateOnly? expirationDateFilter = null);
}

public enum AvailabilityDiscoveryOutcomeEnum
{
    CandidatesFound = 1,
    InsufficientAuthoritativeStock = 2,
    StaleOrNotCurrent = 3
}

public sealed record AvailabilityDiscoveryResult(
    AvailabilityDiscoveryOutcomeEnum Outcome,
    IReadOnlyList<AvailabilityCandidateType> Candidates,
    string? Explanation);

/// <summary>
/// Provisional available Receipt Source candidate for outbound allocation input.
/// Batch is informational only — not a selection key for FIFO.
/// </summary>
public sealed record AvailabilityCandidateType(
    string ReceiptSourceId,
    string LayananId,
    decimal AvailableQuantity,
    DateOnly? ExpirationDate,
    string? Batch);
