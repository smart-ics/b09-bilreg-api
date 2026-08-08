using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StokFeature;

namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// G-09 — Provenance Discovery contract.
/// Recovers the original Receipt Source / Stock Layer for return, reversal, correction,
/// or investigation from source transaction lines, legacy writebacks, <c>tb_buku</c>,
/// and Stock Ledger movements.
/// <para>
/// Distinct from <see cref="IAvailabilityDiscoveryPort"/>.
/// Unknown or ambiguous provenance must be returned explicitly — implementations must not
/// invent a historical Receipt Source (KodeDO).
/// </para>
/// Phase ownership: behavior primarily Phase 7 (return families). Phase 1 defines the contract only.
/// </summary>
public interface IProvenanceDiscoveryPort
{
    ProvenanceDiscoveryResult Discover(ProvenanceDiscoveryRequest request);
}

public enum ProvenanceDiscoveryOutcomeEnum
{
    Known = 1,
    Ambiguous = 2,
    Unknown = 3
}

public sealed record ProvenanceDiscoveryRequest(
    ISourceTransactionReferenceKey SourceTransaction,
    IBrgKey Item,
    ILayananKey? StockLocation = null,
    string? SourceLineId = null,
    decimal? Quantity = null);

public sealed record ProvenanceDiscoveryResult(
    ProvenanceDiscoveryOutcomeEnum Outcome,
    string? ReceiptSourceId,
    string? StockLayerId,
    IReadOnlyList<string> CandidateReceiptSourceIds,
    string? Explanation);
