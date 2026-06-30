# 03-design.md — Tata Rekening

## FEATURE NAME

**Tata Rekening**

---

# DESIGN GOAL

Tata Rekening dirancang sebagai **Financial Control** yang berada di antara **Charge Source** dan **Payment Settlement**.

Tujuan desain ini adalah:

- menjaga konsistensi Financial Responsibility;
- mempertahankan kompatibilitas dengan sistem legacy;
- meminimalkan perubahan database;
- memisahkan Business Rule dari Persistence;
- mendukung migrasi bertahap.

---

# DESIGN PRINCIPLES

Seluruh implementasi mengikuti prinsip berikut.

- Clean Architecture.
- Pragmatic Domain Driven Design.
- Registration-centric Aggregate.
- Compatibility First.
- Database Evolution, bukan Database Rewrite.

---

# HIGH LEVEL ARCHITECTURE

```text
                 Client
                    │
                    ▼
              Presentation
                    │
                    ▼
             Application Layer
                    │
                    ▼
               Domain Layer
                    │
                    ▼
          Infrastructure Layer
                    │
                    ▼
               SQL Server
```

Layer Domain tidak bergantung pada Database maupun Framework.

---

# DESIGN ARCHITECTURE

```text
                Charge Source
                     │
                     ▼
            Tata Rekening API
                     │
                     ▼
               Application
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
   Merge Billing  Allocation  Finalization
                     │
                     ▼
                 Domain Model
                     │
                     ▼
              Repository Layer
                     │
                     ▼
                SQL Server
                     │
         ┌───────────┴───────────┐
         ▼                       ▼
     Accounting              Cashier
```

Application Layer mengorkestrasi seluruh Business Use Case.

Domain Layer menyimpan seluruh Business Rules.

Infrastructure bertanggung jawab terhadap Persistence dan Integration.

---

# DOMAIN IMPLEMENTATION

Business Aggregate diimplementasikan menjadi:

| Business | Implementation |
|----------|----------------|
| Tata Rekening | `TrsBillingRegister` |
| TrsBill | `BillingCharge` |
| Financial Projection | Projection Model |
| Merge Request | `MergeRequest` |

Seluruh Aggregate bekerja pada ruang lingkup **satu Registrasi**.

---

# PERSISTENCE DESIGN

Desain persistence mempertahankan struktur database yang telah digunakan sistem legacy.

| Table | Responsibility |
|--------|----------------|
| `BILRG_TrsBillingRegister` | Lifecycle Tata Rekening |
| `ta_trs_billing` | Financial Charge Ledger |
| `ta_trs_billing2` | Financial Projection |

Prinsip utama:

- `ta_trs_billing` adalah sumber kebenaran Financial Charge.
- `ta_trs_billing2` merupakan derived data.
- Projection dapat dihapus dan dibentuk kembali kapan saja sebelum Finalization.

---

# LIFECYCLE IMPLEMENTATION

Billing mengikuti lifecycle berikut.

```text
OPEN
    │
    ▼
CLOSED
    │
    ▼
FINALIZED
    │
    ▼
LUNAS
```

Setiap perubahan lifecycle dilakukan melalui Aggregate Tata Rekening.

---

# FINANCIAL PROJECTION

Financial Projection dibentuk dari hasil Financial Responsibility Allocation.

Projection digunakan sebagai dasar:

- Payment Settlement
- Accounting Projection

Projection **bukan** sumber data utama.

Projection menggunakan strategi:

```text
DELETE
      │
      ▼
REGENERATE
```

Pendekatan ini menjaga konsistensi dan menyederhanakan implementasi.

---

# MERGE BILLING DESIGN

Merge Billing menggunakan dua konsep berbeda.

## Merge Request

Menyimpan permintaan penggabungan Billing.

Status:

- Pending
- Executed
- Cancelled

Merge Request tidak mengubah Billing.

---

## Merge Billing

Merge Billing membaca Merge Request kemudian:

- memindahkan Billing Set;
- memperbarui kepemilikan Registrasi;
- meregenerasi Financial Projection;
- mengirim Transfer Receivable ke Accounting;
- mengubah Merge Request menjadi **Executed**.

Seluruh proses dilakukan dalam satu transaksi aplikasi.

---

# TRANSACTION STRATEGY

Transaction Boundary berada pada tingkat Registrasi.

Satu transaksi dapat mencakup:

- perubahan lifecycle;
- Merge Billing;
- Financial Adjustment;
- Allocation;
- Projection Regeneration.

Apabila salah satu proses gagal, seluruh transaksi dibatalkan.

---

# INTEGRATION DESIGN

```text
Charge Source
      │
      ▼
ta_trs_billing
      │
      ▼
Tata Rekening
      │
      ├────────► Accounting
      │
      ▼
Cashier
```

Integrasi dilakukan secara sinkron.

Tata Rekening tidak mengakses data internal subsystem lain selain melalui kontrak integrasi yang disepakati.

---

# CONCURRENCY

Concurrency dikendalikan pada level Registrasi.

Tujuannya mencegah:

- Finalization ganda;
- Merge Billing bersamaan;
- Reopen setelah Settlement;
- perubahan Financial Responsibility secara bersamaan.

---

# SECURITY

Operasi berikut memerlukan otorisasi Verifikator:

- Close Bill
- Merge Billing
- Financial Adjustment
- Financial Responsibility Allocation
- Finalize Financial Responsibility
- Cancel Finalization
- Reopen Billing
- Settlement Initiation

---

# ERROR HANDLING

Perubahan dibatalkan apabila terjadi kegagalan pada:

- Merge Billing;
- Projection Regeneration;
- Allocation;
- Transfer Receivable;
- Finalization Validation.

Dengan demikian Financial Responsibility dan Accounting tetap konsisten.

---

# DEPLOYMENT STRATEGY

Implementasi dilakukan secara bertahap.

Prinsip migrasi:

- mempertahankan tabel legacy;
- mempertahankan workflow Kasir;
- mempertahankan workflow Accounting;
- menambahkan capability baru tanpa mengubah perilaku lama yang masih digunakan.

---

# RELATED DOCUMENTS

| Document | Description |
|----------|-------------|
| **01-context.md** | Business Context |
| **02-domain.md** | Domain Model |
| **04-sop.md** | Business Workflow & SOP |