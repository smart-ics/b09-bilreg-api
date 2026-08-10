namespace Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;

/// <summary>
/// Dual-write inserts/updates against legacy <c>tb_buku</c> / <c>tb_stok</c>.
/// Void = insert reverse buku (+ stok adjust as needed); never delete buku (ADR-STL-002).
/// </summary>
public interface ILegacyStockWriterPort
{
    LegacyInboundWriteResult InsertInbound(LegacyInboundWriteRequest request);

    LegacyOutboundWriteResult InsertOutboundBuku(LegacyOutboundBukuWriteRequest request);

    void DepleteStok(LegacyStokDepleteRequest request);

    LegacyReverseWriteResult InsertReverseBuku(LegacyReverseBukuWriteRequest request);
}

/// <summary>
/// Discriminated legacy write ops for <see cref="StockConsequenceDraft"/>.
/// </summary>
public abstract record LegacyStockWriteOperation;

public sealed record LegacyInboundWriteOperation(LegacyInboundWriteRequest Request)
    : LegacyStockWriteOperation;

public sealed record LegacyOutboundBukuWriteOperation(LegacyOutboundBukuWriteRequest Request)
    : LegacyStockWriteOperation;

public sealed record LegacyStokDepleteWriteOperation(LegacyStokDepleteRequest Request)
    : LegacyStockWriteOperation;

public sealed record LegacyReverseBukuWriteOperation(LegacyReverseBukuWriteRequest Request)
    : LegacyStockWriteOperation;

/// <summary>
/// Receipt / inbound: INSERT <c>tb_buku</c> then INSERT <c>tb_stok</c>.
/// Optional pre-assigned ids (ADR-STL-005) so callers can build Binding rows before Commit.
/// </summary>
public sealed record LegacyInboundWriteRequest(
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    decimal Qty,
    decimal Hpp,
    DateTime TglEd,
    DateTime TglMasuk,
    DateTime TglMutasi,
    string TrsReffId,
    string MovementKindString,
    string? PoReffId = null,
    string? NoBatch = null,
    string? SatuanId = null,
    string? LegacyBukuId = null,
    string? LegacyStokId = null);

public sealed record LegacyInboundWriteResult(string LegacyBukuId, string LegacyStokId);

/// <summary>Outbound journal line: INSERT <c>tb_buku</c> only (stok deplete is separate).</summary>
public sealed record LegacyOutboundBukuWriteRequest(
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    decimal QtyOut,
    decimal Hpp,
    DateTime TglEd,
    DateTime TglMutasi,
    string TrsReffId,
    string MovementKindString,
    string? PoReffId = null,
    string? NoBatch = null,
    string? SatuanId = null,
    string? LegacyBukuId = null);

public sealed record LegacyOutboundWriteResult(string LegacyBukuId);

/// <summary>
/// UPDATE <c>fn_qty</c> or DELETE <c>tb_stok</c> when remaining qty is zero.
/// Never touches <c>tb_buku</c>.
/// </summary>
public sealed record LegacyStokDepleteRequest(string LegacyStokId, decimal QtyOut);

/// <summary>
/// Compensating void buku insert (reverse journal). Never deletes original buku.
/// </summary>
public sealed record LegacyReverseBukuWriteRequest(
    string BrgId,
    string BrgMasukReffId,
    string LayananId,
    decimal QtyIn,
    decimal QtyOut,
    decimal Hpp,
    DateTime TglEd,
    DateTime TglMutasi,
    string TrsReffId,
    string MovementKindString,
    string? PoReffId = null,
    string? NoBatch = null,
    string? SatuanId = null,
    string? LegacyBukuId = null);

public sealed record LegacyReverseWriteResult(string LegacyBukuId);
