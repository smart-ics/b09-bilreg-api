# StockLedgerFeature (Domain)

Obsolete Stock Ledger **v1** sources (six-table Movement/Line/Layer/Position/Scope/Idempotency model) are not present in this feature tree and must not be reintroduced. This folder holds **v2 greenfield** types under the Stock Batch / Location Stock Balance / Stock Movement model — see `docs/contexts/stok-ledger/stok-ledger-architecture.md` (ADR-STL-001) and the Slice 1 plan `docs/contexts/stok-ledger/stok-ledger-implementation-plan.md`.

**S1-A1 Done** (domain skeleton + §8 DDL). **S1-A2 Done** (DAL/Repo/OCC + UoW contract shell). Next parallel cards: **S1-B** (outbound allocation domain) and **S1-C1** (legacy read + hydrate).
