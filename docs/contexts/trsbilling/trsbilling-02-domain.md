# TRSBILLING-DOMAIN.md

## 1. Purpose

`TRSBILLING` adalah domain inti financial receivable pada Sistem Informasi Rumah Sakit.

Domain ini bertanggung jawab terhadap:

```text
financial recognition
financial responsibility allocation
financial settlement mutation
```

atas seluruh transaksi operasional yang menimbulkan biaya kepada pasien.

Domain ini BUKAN:

* domain tindakan medis,
* domain inventory,
* domain accounting umum,
* domain payment gateway,
* domain workflow klinis.

TRSBILLING adalah:

# Patient Financial Charge Ledger Domain

---

# 2. Core Responsibility

TRSBILLING bertanggung jawab untuk:

| Responsibility         | Description                                           |
| ---------------------- | ----------------------------------------------------- |
| Charge Recognition     | Mengakui piutang/tagihan akibat aktivitas operasional |
| Pricing Snapshot       | Menyimpan snapshot harga saat transaksi terjadi       |
| Financial Allocation   | Mengalokasikan tanggung jawab pembayaran              |
| Settlement Mutation    | Mencatat perpindahan ownership piutang                |
| Accounting Projection  | Menyediakan accounting-ready mutation entries         |
| Financial Immutability | Menjaga historical financial integrity                |

---

# 3. Domain Positioning

```text
Operational Subsystem
    ↓
Charge Source Transaction
    ↓
TRSBILLING
    ↓
Tata Rekening
    ↓
Payment Settlement
    ↓
Accounting
```

---

# 4. Important Architectural Principle

## Operational Truth ≠ Financial Truth

Subsystem operasional mencatat:

```text
what operationally happened
```

TRSBILLING mencatat:

```text
what financially must be recognized
```

Contoh:

| Operational | Financial       |
| ----------- | --------------- |
| tindakan    | piutang         |
| obat keluar | charge          |
| room usage  | receivable      |
| transport   | financial claim |

---

# 5. Aggregate Structure

TRSBILLING terdiri dari satu aggregate utama:

# BillingCharge Aggregate

---

## Aggregate Root

### `ta_trs_billing`

Merepresentasikan:

```text
authoritative commercial charge snapshot
```

Menyimpan:

* registrasi,
* tarif,
* nilai charge,
* pricing snapshot,
* source transaction,
* operational context.

---

## Child Collection

### `ta_trs_billing2`

Merepresentasikan:

# Financial Mutation Entries

Bukan:

* detail CRUD biasa,
* detail invoice biasa,
* accounting journal umum.

Tetapi:

```text
immutable-ish financial mutation timeline
```

yang:

* append-oriented,
* lifecycle-aware,
* accounting-ready,
* financial-history-aware.

---

# 6. Aggregate Invariants

## Invariant 1 — Authoritative Charge

`ta_trs_billing` adalah authoritative charge amount.

Seluruh mutation pada `ta_trs_billing2` HARUS tetap constrained terhadap nilai charge tersebut.

---

## Invariant 2 — Mutation Integrity

Mutation entry TIDAK boleh berdiri sendiri.

Seluruh mutation:

* HARUS terkait ke BillingCharge,
* HARUS berasal dari lifecycle event valid,
* HARUS mengikuti stage constraint domain.

---

## Invariant 3 — Historical Integrity

Pricing snapshot:

* immutable,
* tidak berubah walaupun tarif berubah,
* tidak berubah walaupun kelas pasien berubah.

---

## Invariant 4 — Post-Payment Immutability

Jika settlement/payment sudah terjadi:

```text
BillingCharge becomes financially immutable
```

Perubahan billing:

* FORBIDDEN,
* wajib melalui adjustment domain di modul keuangan.

---

# 7. Financial Lifecycle

TRSBILLING memiliki lifecycle financial eksplisit.

---

## Stage 1 — Transaction Recognition

Saat transaksi operasional terjadi.

Allowed mutation types:

* PDP
* POT
* BYL
* TAX

State:

* OPEN BILL
* belum balance diperbolehkan.

---

## Stage 2 — Financial Finalization / Close Bill

Saat pasien pulang / billing finalized.

Allowed mutation types:

* Tipe Jaminan
* HUT
* KAS

Responsibility allocation dilakukan berdasarkan:

* `ta_registrasi3`

Allocation:

* proportional per financial component.

---

## Stage 3 — Payment Settlement

Saat pembayaran diterima.

Allowed mutation types:

* Tipe Jaminan
* HUT
* KAS

Payment bukan sekadar:

```text
receive money
```

Tetapi:

```text
ownership settlement transition
```

---

## Stage 4 — Immutable Financial History

Setelah payment:

* billing freeze,
* reopen forbidden,
* correction hanya melalui modul keuangan.

---

# 8. Reopen Rules

Billing boleh REOPEN hanya jika:

| Condition         | Allowed |
| ----------------- | ------- |
| belum ada payment | YES     |
| sudah ada payment | NO      |

Jika sudah ada payment:

* correction dilakukan via accounting adjustment,
* bukan via billing mutation.

---

# 9. Financial Mutation Entries

## `ta_trs_billing2`

Setiap row adalah:

# Financial Mutation Entry

yang merepresentasikan:

* financial ownership mutation,
* allocation mutation,
* settlement mutation,
* accounting projection mutation.

---

# 10. Mutation Batch

## `FS_KD_TRS_BAYAR`

Merepresentasikan:

# Financial Mutation Batch ID

Contoh:

* TUxxxxx → transaction recognition
* ROxxxxx → close bill
* PLxxxxx → payment settlement

Batch:

* chronological,
* append-oriented,
* lifecycle-aware.

---

# 11. Financial Mutation Semantics

## `FN_TRS_P`

Merepresentasikan:

```text
financial ownership increase
```

---

## `FN_TRS_N`

Merepresentasikan:

```text
financial ownership release
```

Ini BUKAN:

* debit/credit accounting literal,
* positive/negative numeric biasa.

Tetapi:

# Financial Responsibility Flow Direction

---

# 12. JenisBayar Semantics

## `FS_KD_JENIS_BAYAR`

Bukan sekadar payment method.

Tetapi:

# Financial Mutation Channel

yang merepresentasikan:

* mutation semantics,
* responsibility ownership,
* settlement semantics.

---

# 13. JenisBayar Categories

## A. System Predefined Types

| Code | Meaning                        |
| ---- | ------------------------------ |
| PDP  | initial receivable recognition |
| POT  | direct item reduction          |
| BYL  | rounding gain                  |
| TAX  | tax extraction                 |
| KAS  | cash settlement                |
| HUT  | patient receivable ownership   |

Karakteristik:

* system-defined,
* invariant,
* financial semantic vocabulary.

---

## B. User Configurable Types

Representasi:

* BPJS,
* asuransi,
* corporate guarantor,
* institution payer.

Karakteristik:

* external responsibility owner,
* configurable master data,
* payer identity.

---

# 14. Responsibility Allocation

## `ta_registrasi3`

Bukan bagian aggregate mutation entry.

Tetapi:

# Billing Responsibility Allocation Authority

Menyimpan:

* siapa bertanggung jawab,
* total allocation responsibility.

TRSBILLING melakukan:

* proportional decomposition,
* per component financial allocation.

---

# 15. Financial Atomicity

Financial atomicity berbeda berdasarkan billing type.

---

## Service Billing

Menggunakan:

* `FS_KD_DETIL_TARIF`

Karena:

* komponen configurable,
* COA melekat ke komponen tarif.

---

## Inventory / Drug Billing

Menggunakan:

* `FS_KD_GRUP_REK`

Karena:

* accounting grouping lebih fixed,
* inventory accounting lebih standardized.

---

# 16. Accounting Projection Semantics

TRSBILLING bukan accounting journal.

Namun:

# accounting-ready mutation projection source

Karena:

* seluruh mutation membawa accounting snapshot,
* COA disnapshot,
* historical accounting integrity dijaga.

---

# 17. Why COA Snapshot Exists

COA snapshot BUKAN optimization teknis.

Tetapi:

# historical financial immutability strategy

Tujuan:

* journal history tidak rusak,
* perubahan tarif tidak mengubah accounting history,
* perubahan mapping account tidak mengubah historical posting source.

---

# 18. Mutation Persistence Strategy

Mutation entries bersifat:

| Characteristic        | Value |
| --------------------- | ----- |
| append-oriented       | YES   |
| temporal              | YES   |
| historical            | YES   |
| accounting-ready      | YES   |
| mutable after payment | NO    |

---

# 19. Important Anti-Patterns

Agent DILARANG menganggap `ta_trs_billing2` sebagai:

* CRUD detail table biasa,
* invoice detail biasa,
* mutable child collection biasa,
* accounting journal umum.

Karena nature sebenarnya adalah:

# Financial Mutation Timeline

---

# 20. Composite Charge Semantics

Operational atomicity TIDAK selalu sama dengan commercial atomicity.

TRSBILLING HARUS tetap mendukung:

* composite charge,
* bundled charge,
* multi-component charge,
* workflow-compressed commercial representation.

---

# 21. Integration Boundary

Subsystem lain:

* TIDAK boleh mengetahui internal mutation structure billing,
* TIDAK boleh memanipulasi mutation entries langsung,
* TIDAK boleh menjadi owner financial ledger.

Pattern integrasi:

```text
Subsystem
→ Charge Request
→ TRSBILLING
```

BUKAN:

```text
Subsystem Aggregate
→ manipulate Billing Aggregate directly
```

---

# 22. Authority Matrix

| Domain                | Authority                           |
| --------------------- | ----------------------------------- |
| Operational Subsystem | operational truth                   |
| Pricing               | commercial pricing                  |
| TRSBILLING            | financial receivable ledger         |
| Tata Rekening         | financial allocation & verification |
| Payment               | settlement                          |
| Accounting            | GL & financial correction           |

---

# 23. Future Direction

TRSBILLING diposisikan menuju:

# Enterprise Charge Gateway

Pattern:

```text
Subsystem
→ Charge Request
→ Financial Charge Ledger
→ Financial Allocation
→ Settlement
→ Accounting
```

Namun:

* operational ownership tetap di subsystem asal,
* financial ownership berada di TRSBILLING.

---

# 24. Final Domain Positioning

TRSBILLING adalah:

# Patient Financial Charge Ledger

yang:

* menerima financial recognition dari subsystem operasional,
* menyimpan immutable pricing snapshot,
* mengelola financial responsibility mutation,
* menghasilkan accounting-ready mutation projection,
* menjaga historical financial integrity,
* dan menjadi authoritative patient receivable ledger rumah sakit.
