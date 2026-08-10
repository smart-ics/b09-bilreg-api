# StockLedgerFeature (Domain)

Obsolete Stock Ledger **v1** sources (six-table Movement/Line/Layer/Position/Scope/Idempotency model) are not present in this feature tree and must not be reintroduced. This folder holds **v2 greenfield** types under the Stock Batch / Location Stock Balance / Stock Movement model — see `docs/contexts/stok-ledger/stok-ledger-architecture.md` (ADR-STL-001) and the Slice 1 plan `docs/contexts/stok-ledger/stok-ledger-implementation-plan.md`.

**S1-A1 Done** (domain skeleton + §8 DDL). **S1-A2 Done** (DAL/Repo/OCC + UoW contract shell). **S1-B Done** (outbound allocation FEFO/FIFO/Explicit ED — pure domain). **S1-C1 Done** (legacy read + UC-STL-010 hydrate). **S1-C2 Done** (catch-up UC-STL-011 + freshness gate UC-STL-012). Next: **S1-D1** (legacy writer + atomic consequence UoW); allocator consumed by S1-E / S1-F*.
