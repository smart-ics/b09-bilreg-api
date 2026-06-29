# 04-pasien-balance.md — PasienBalance Aggregate

## 1. Purpose

`PasienBalance` maintains a **patient-level cumulative outstanding balance** across all registrations, partitioned by **JASA** and **OBAT** to align with TataRekening modul groups.

Registration-scoped `TataRekening` remains the financial authority per visit. `PasienBalance` is the **cross-registration ledger** that TataRekening use cases will orchestrate later.

This slice is **foundational only** — no HTTP API, no application commands, no TataRekening integration yet.

---

## 2. Aggregate Structure

```text
PasienBalanceModel (root)
└── PasienBalanceHistoryType (append-only child)
```

### Root state

| Field | Meaning |
|-------|---------|
| `PasienId` | Aggregate identity (`IPasienKey`) |
| `CurrentJasaBalance` | Outstanding JASA (persisted) |
| `CurrentObatBalance` | Outstanding OBAT (persisted) |
| `CurrentBalance` | **Computed:** `CurrentJasaBalance + CurrentObatBalance` |
| `LastHistoryId` | Latest history entry id |
| `UpdatedAt` | Last mutation timestamp |
| `Version` | Optimistic concurrency token |

### History entry

| Field | Meaning |
|-------|---------|
| `HistoryId` | App-generated id (`PBH` prefix) |
| `PasienId` | Patient reference |
| `RegId` | Optional registration reference (empty when N/A) |
| `TrsDate` | Transaction date |
| `OpeningJasaBalance`, `OpeningObatBalance` | Partition opening |
| `ChargeJasa`, `ChargeObat` | Increase per modul |
| `PaymentJasa`, `PaymentObat` | Decrease per modul |
| `ClosingJasaBalance`, `ClosingObatBalance` | Partition closing (persisted) |
| `ClosingBalance` | **Computed:** `ClosingJasaBalance + ClosingObatBalance` |
| `Remarks` | Free text |
| `CreatedAt`, `CreatedBy` | Immutable audit |

---

## 3. Invariants

```text
CurrentJasaBalance == LatestHistory.ClosingJasaBalance   (or 0 when no history)
CurrentObatBalance == LatestHistory.ClosingObatBalance   (or 0 when no history)
CurrentBalance     == CurrentJasaBalance + CurrentObatBalance

ClosingJasaBalance = OpeningJasaBalance + ChargeJasa - PaymentJasa
ClosingObatBalance = OpeningObatBalance + ChargeObat - PaymentObat
ClosingBalance     = ClosingJasaBalance + ClosingObatBalance
```

Rules:

- Every balance modification creates **exactly one** history entry.
- History is **append-only** — no update, no delete.
- No partition closing may go negative.
- `ApplyCharge` / `ApplyPayment` require at least one of JASA or OBAT amount > 0.
- Payment is validated **per partition** (JASA payment cannot exceed JASA balance, same for OBAT).
- `PasienBalance` must **not** reference other billing aggregates (`TataRekening`, `TrsBill`, etc.).

---

## 4. Domain Operations

| Method | Effect |
|--------|--------|
| `Create(pasienId)` | Zero both partitions, no history |
| `ApplyCharge(jasaAmount, obatAmount, regId, trsDate, remarks, createdBy)` | Increases partition balances |
| `ApplyPayment(jasaAmount, obatAmount, regId, trsDate, remarks, createdBy)` | Decreases partition balances; rejects over-payment per modul |

`AdjustBalance` has been **removed**. Manual corrections use explicit JASA/OBAT charge or payment amounts.

`AppendHistory` is private — all mutations flow through the public methods above.

### Example

```text
RG-007: charge jasa 300k + obat 200k, pay jasa 200k + obat 100k → jasa 100k, obat 100k, total 200k
RG-008: charge jasa 150k + obat 100k, pay jasa 100k + obat 150k → jasa 150k, obat 50k, total 200k
```

---

## 5. Persistence

### Tables

| Table | Role |
|-------|------|
| `BILRG_TataRekPasienBalance` | Aggregate root state (`CurrentJasaBalance`, `CurrentObatBalance`) |
| `BILRG_TataRekPasienBalanceHistory` | Append-only ledger with partition columns |

Scripts: `Bilreg.SqlDb/PaymentContext/PasienBalanceFeature/`

Migration from initial schema:

- `BILRG_TataRekPasienBalance_M1_JasaObatPartition_Alter.sql`
- `BILRG_TataRekPasienBalanceHistory_M1_JasaObatPartition_Alter.sql`

`CurrentBalance` and scalar history totals are **not persisted** — only partition columns are stored.

### Concurrency

Header updates use **optimistic concurrency**:

```sql
UPDATE ... WHERE PasienId = @PasienId AND Version = @ExpectedVersion
```

Stale writes throw `InvalidOperationException`.

### History persistence

- **Insert only** for new pending history rows on `SaveChanges`.
- No DELETE or UPDATE on history table.

---

## 6. Repository

`IPasienBalanceRepo` (`Bilreg.Application/PaymentContext/PasienBalanceFeature/`):

| Operation | Method |
|-----------|--------|
| Find by patient | `LoadEntity(IPasienKey)` |
| Save | `SaveChanges(PasienBalanceModel)` |
| List history | `ListHistory(IPasienKey)` |

Implementation: `PasienBalanceRepo` in Infrastructure. Auto-registered via Scrutor.

Repository remains **persistence only** — no reconstruction logic.

---

## 7. Reconstruction Strategy (future)

During legacy migration, authoritative financial data remains in the legacy system. `PasienBalance` must be **fully reconstructable**.

### Contract (defined, not implemented)

`IPasienBalanceRebuilder` in Application layer:

| Method | Purpose |
|--------|---------|
| `RebuildPatient(IPasienKey)` | Rebuild one patient from legacy source |
| `RebuildAll()` | Batch rebuild all patients |
| `ValidatePatient(IPasienKey)` | Compare stored vs legacy partition balances |

Result type: `PasienBalanceValidationResult` (legacy vs stored JASA/OBAT comparison).

### Future flow

```text
Delete existing PasienBalance (+ history)
    ↓
Read authoritative legacy financial data
    ↓
Reconstruct aggregate (Hydrate / replay domain operations)
    ↓
Rebuild history
    ↓
Recalculate partition balances
    ↓
SaveChanges
```

Notes:

- Rebuilder orchestrates **outside** the repository.
- Future implementation will require **delete-capable DAL methods** (not yet added).
- `PasienBalanceModel.Hydrate` supports full in-memory regeneration with complete history list.

No DI registration or implementation class exists yet.

---

## 8. Code Layout

```text
Bilreg.Domain/PaymentContext/PasienBalanceFeature/
  PasienBalanceModel.cs
  PasienBalanceHistoryType.cs

Bilreg.Application/PaymentContext/PasienBalanceFeature/
  IPasienBalanceRepo.cs
  IPasienBalanceRebuilder.cs
  PasienBalanceValidationResult.cs

Bilreg.Infrastructure/PaymentContext/PasienBalanceFeature/
  BilrgTataRekPasienBalanceDto.cs
  BilrgTataRekPasienBalanceDal.cs
  BilrgTataRekPasienBalanceHistoryDto.cs
  BilrgTataRekPasienBalanceHistoryDal.cs
  PasienBalanceRepo.cs

Bilreg.Test/PaymentContext/PasienBalanceFeature/
  PasienBalanceDomainTest.cs
  PasienBalanceRepoTest.cs
```

---

## 9. Out of Scope

- REST API / controllers
- MediatR handlers / application use cases
- TataRekening orchestration
- Rebuilder implementation
- Legacy queries, batch workers, scheduled jobs
- Payment allocation, accounting, cashier workflow
- Domain events
- Data migration from legacy tables

---

## 10. Future Integration

TataRekening use cases will:

1. Load or create `PasienBalance` by `PasienId`.
2. Call `ApplyCharge(jasa, obat, ...)` / `ApplyPayment(jasa, obat, ...)` when registration-level financial events affect patient outstanding.
3. Persist via `IPasienBalanceRepo.SaveChanges`.

Partition structure matches TataRekening JASA/OBAT responsibility — no further structural change expected.
