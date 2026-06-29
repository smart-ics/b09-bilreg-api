# 04-pasien-balance.md — PasienBalance Aggregate

## 1. Purpose

`PasienBalance` maintains the **current outstanding receivables** belonging to one patient — one entry per registration with unsettled JASA/OBAT amounts.

It does **not** own carry-over decisions, payment allocation, registration workflow, or settlement. Those responsibilities belong to **TataRekening**.

This slice is **foundational only** — no HTTP API, no TataRekening integration yet.

---

## 2. Aggregate Structure

```text
PasienBalanceModel (root)
└── OutstandingEntryType (child — one unsettled receivable per RegId)
```

### Root state

| Field | Meaning |
|-------|---------|
| `PasienId` | Aggregate identity (`IPasienKey`) |
| `Version` | Optimistic concurrency token |
| `OutstandingEntries` | Current unsettled receivables |

### Computed projections (not persisted)

| Property | Formula |
|----------|---------|
| `TotalOutstandingJasa` | Σ `OutstandingJasa` |
| `TotalOutstandingObat` | Σ `OutstandingObat` |
| `TotalOutstanding` | Σ `OutstandingTotal` |

### Outstanding entry

| Field | Meaning |
|-------|---------|
| `EntryId` | App-generated id (`PBO` prefix) |
| `PasienId` | Patient reference (must match root) |
| `RegId` | **Required** registration reference — unique within aggregate |
| `OutstandingJasa` | Unsettled JASA amount |
| `OutstandingObat` | Unsettled OBAT amount |
| `OutstandingTotal` | **Computed:** `OutstandingJasa + OutstandingObat` |
| `LastTransactionDate` | Last financial activity date for this receivable |
| `SourceReference` | Traceability (e.g. legacy `fs_kd_piutang`) |
| `CreatedAt`, `CreatedBy` | Immutable audit |

---

## 3. Invariants

- Every outstanding entry belongs to exactly one patient (`PasienId`).
- Every entry references exactly one registration (`RegId` — required).
- At most one outstanding entry per `RegId` within a patient's aggregate.
- Outstanding partition values cannot be negative.
- `TotalOutstanding` always equals the sum of entry totals.

`PasienBalance` must **not** reference other billing aggregates (`TataRekening`, `TrsBill`, etc.).

---

## 4. Domain Operations

| Method | Effect |
|--------|--------|
| `Create(pasienId)` | Empty aggregate, zero totals |
| `AddOutstanding(regId, jasa, obat, lastTransactionDate, sourceReference, createdBy)` | Add new receivable; rejects duplicate `RegId` |
| `UpdateOutstanding(regId, jasa, obat, lastTransactionDate, sourceReference)` | Update existing entry by `RegId` |
| `RemoveOutstanding(regId)` | Remove entry by `RegId` |
| `ReplaceOutstandingEntries(entries, createdBy)` | Full collection replace (bootstrap); enforces unique `RegId` |

Mutation history concepts (`OpeningBalance`, `ClosingBalance`, `ChargeAmount`, `PaymentAmount`, `MutationType`) are **not** part of this aggregate.

---

## 5. Persistence

### Tables

| Table | Role |
|-------|------|
| `BILRG_TataRekPasienBalance` | Aggregate root (`PasienId`, `Version`, audit columns) |
| `BILRG_TataRekPasienBalanceOutstanding` | Current outstanding entries |

Scripts: `Bilreg.SqlDb/PaymentContext/PasienBalanceFeature/`

Migration path:

- `BILRG_TataRekPasienBalance_M1_JasaObatPartition_Alter.sql` (legacy — superseded)
- `BILRG_TataRekPasienBalance_M2_OutstandingEntry_Redesign_Alter.sql`

Partition totals and `OutstandingTotal` are **not persisted** — only partition columns per entry are stored.

### Concurrency

Header updates use **optimistic concurrency**:

```sql
UPDATE ... WHERE PasienId = @PasienId AND Version = @ExpectedVersion
```

Stale writes throw `InvalidOperationException`.

### Entry persistence

On `SaveChanges`:

1. Insert or update header.
2. Delete all outstanding rows for `PasienId`.
3. Bulk insert current entry collection.

---

## 6. Repository

`IPasienBalanceRepo` (`Bilreg.Application/PaymentContext/PasienBalanceFeature/`):

| Operation | Method |
|-----------|--------|
| Find by patient | `LoadEntity(IPasienKey)` |
| Save | `SaveChanges(PasienBalanceModel)` |

Implementation: `PasienBalanceRepo` in Infrastructure. Auto-registered via Scrutor.

Repository is **persistence only** — no bootstrap logic.

---

## 7. Legacy Bootstrap

During migration, authoritative outstanding data remains in legacy `t_bp_piutang_hdr`. Bootstrap imports one `OutstandingEntry` per unsettled legacy receivable.

### Legacy reader

`IPasienBalanceLegacyReader` → `LegacyOutstandingReceivableReader`

Returns `LegacyOutstandingReceivable` rows using the same filters as `RegHutangDal`:

- `fn_sisa > 0`
- `fd_tgl_void = '3000-01-01'`
- `fs_kd_iii = 'JAMINAN000'`
- `fs_kd_mr = @PasienId`

### Bootstrap service

`PasienBalanceBootstrapService.Bootstrap(IPasienKey)`:

1. Read legacy outstanding receivables.
2. `Create(pasienId)` then `ReplaceOutstandingEntries(...)`.
3. Skip rows with zero JASA + OBAT.

### Loader (transparent bootstrap)

`IPasienBalanceLoader` → `PasienBalanceLoader`

```text
repo.LoadEntity(key)
  → Some: return aggregate
  → None: bootstrap from legacy → repo.SaveChanges → return aggregate
```

Bootstrap is transparent to callers.

---

## 8. Code Layout

```text
Bilreg.Domain/PaymentContext/PasienBalanceFeature/
  PasienBalanceModel.cs
  OutstandingEntryType.cs

Bilreg.Application/PaymentContext/PasienBalanceFeature/
  IPasienBalanceRepo.cs
  IPasienBalanceLegacyReader.cs
  LegacyOutstandingReceivable.cs
  PasienBalanceBootstrapService.cs
  IPasienBalanceLoader.cs
  PasienBalanceLoader.cs

Bilreg.Infrastructure/PaymentContext/PasienBalanceFeature/
  BilrgTataRekPasienBalanceDto.cs
  BilrgTataRekPasienBalanceDal.cs
  BilrgTataRekPasienBalanceOutstandingDto.cs
  BilrgTataRekPasienBalanceOutstandingDal.cs
  LegacyOutstandingReceivableReader.cs
  PasienBalanceRepo.cs

Bilreg.Test/PaymentContext/PasienBalanceFeature/
  PasienBalanceDomainTest.cs
  PasienBalanceRepoTest.cs
  PasienBalanceBootstrapTest.cs
  PasienBalanceLoaderTest.cs
```

---

## 9. Out of Scope

- REST API / controllers
- MediatR handlers / application use cases
- TataRekening orchestration and carry-over workflow
- Payment allocation, accounting, cashier workflow
- Domain events

---

## 10. Future TataRekening Integration

```text
Open Registration
    ↓
Load PasienBalance (via IPasienBalanceLoader)
    ↓
List OutstandingEntries
    ↓
User selects entries to carry over
    ↓
TataRekening performs carry-over
    ↓
Selected entries updated or removed via domain operations
```

TataRekening alone decides which outstanding receivables become part of the new registration. `PasienBalance` remains agnostic about **why** an entry is consumed.
