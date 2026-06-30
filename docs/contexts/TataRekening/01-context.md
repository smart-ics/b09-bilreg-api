# 01-context.md — Tata Rekening

## FEATURE NAME

**Tata Rekening**

---

## PURPOSE

Tata Rekening adalah **Bounded Context** yang bertanggung jawab mengelola **Financial Responsibility** untuk setiap Registrasi Pasien.

Tata Rekening menjadi penghubung antara proses operasional pelayanan dan proses pembayaran, sehingga seluruh tagihan yang akan dibayarkan telah diverifikasi, dialokasikan, dan difinalisasi secara konsisten.

Tata Rekening **bukan** bertanggung jawab atas:

- pelayanan medis;
- pembentukan Financial Charge;
- proses pembayaran;
- pembentukan jurnal akuntansi.

---

## WHY TATA REKENING EXISTS

Aktivitas operasional dan aktivitas finansial memiliki tujuan yang berbeda.

Charge Source mencatat **Operational Truth**, sedangkan Tata Rekening mengelola **Financial Truth**.

```text
Operational Truth
        ≠
Financial Truth
```

Dengan pemisahan ini:

- proses operasional dapat berjalan secara independen;
- proses finansial dapat diverifikasi secara terpusat;
- proses pembayaran dilakukan berdasarkan Financial Responsibility yang telah difinalisasi.

---

## RELATED BOUNDED CONTEXTS

```text
               Tarif
                 │
                 ▼
Charge Source ───────► Tata Rekening
                           │
              ┌────────────┴────────────┐
              ▼                         ▼
         Accounting                 Cashier
```

| Bounded Context | Responsibility |
|-----------------|----------------|
| Charge Source | Membentuk Financial Charge |
| Tata Rekening | Mengelola Financial Responsibility |
| Cashier | Payment Settlement |
| Accounting | Journal & General Ledger |
| Tarif | Pricing Policy |

---

## CORE BUSINESS CONCEPTS

### Billing Set

Sekumpulan Financial Charge yang dimiliki oleh satu Registrasi.

Billing Set menjadi objek utama yang dikelola selama proses Tata Rekening.

---

### Financial Responsibility

Pembagian tanggung jawab pembayaran terhadap Billing Set kepada satu atau lebih Payer.

Contoh Payer:

- Pasien
- BPJS
- Asuransi
- Perusahaan
- Subsidi Rumah Sakit

---

### Financial Projection

Representasi hasil Financial Responsibility yang digunakan sebagai dasar:

- Payment Settlement
- Accounting Projection

Financial Projection merupakan data turunan (derived data) yang dapat diregenerasi kapan saja sebelum Finalization.

---

### Merge Request

Permintaan untuk menggabungkan Billing Set dari Registrasi sumber ke Registrasi tujuan.

Merge Request hanya mencatat **niat (intent)** penggabungan Billing dan belum menghasilkan perubahan finansial.

Status Merge Request:

- Pending
- Executed
- Cancelled

---

### Merge Billing

Proses pemindahan Billing Set berdasarkan Merge Request.

Merge Billing:

- memindahkan kepemilikan Billing Set;
- meregenerasi Financial Projection;
- mengirim Transfer Receivable ke Accounting;
- mengubah Merge Request menjadi **Executed**.

---

## FINANCIAL LIFECYCLE

Seluruh Registrasi mengikuti lifecycle berikut.

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

| Status | Description |
|---------|-------------|
| OPEN | Charge Source masih dapat membentuk Financial Charge |
| CLOSED | Billing dibekukan untuk proses Financial Control |
| FINALIZED | Financial Responsibility telah dikunci |
| LUNAS | Seluruh kewajiban finansial telah diselesaikan |

---

## FINANCIAL WORKFLOW

Workflow Tata Rekening terdiri dari tiga fase.

```text
Operational Freeze
        │
        ▼
Financial Control
        │
        ▼
Settlement
```

Tahapan utamanya adalah:

```text
Open Tata Rekening
        │
        ▼
Close Bill
        │
        ▼
Merge Billing (Optional)
        │
        ▼
Financial Verification
        │
        ▼
Financial Adjustment (Optional)
        │
        ▼
Financial Responsibility Allocation
        │
        ▼
Finalize Financial Responsibility
        │
        ▼
Settlement Initiation
        │
        ▼
Payment Settlement
```

---

## RESPONSIBILITY BOUNDARY

### Charge Source

Bertanggung jawab terhadap:

- aktivitas operasional;
- pembentukan Financial Charge;
- koreksi operasional.

---

### Tata Rekening

Bertanggung jawab terhadap:

- Billing Set;
- Merge Billing;
- Financial Verification;
- Financial Adjustment;
- Financial Responsibility Allocation;
- Finalization.

---

### Cashier

Bertanggung jawab terhadap:

- Payment Settlement.

---

### Accounting

Bertanggung jawab terhadap:

- Transfer Receivable;
- Journal Posting;
- General Ledger.

---

## DESIGN PRINCIPLES

Tata Rekening dibangun berdasarkan prinsip berikut.

- Registration-centric Financial Responsibility.
- Operational Truth dipisahkan dari Financial Truth.
- Billing Set dapat berubah sebelum Finalization.
- Financial Responsibility dikunci setelah Finalization.
- Financial Projection merupakan derived data.
- Merge Billing merupakan proses eksplisit.
- Accounting tetap menjadi bounded context terpisah.
- Kompatibel dengan mekanisme Billing, Kasir, dan Accounting yang sudah ada.

---

## OUT OF SCOPE

Tata Rekening tidak mencakup:

- pelayanan medis;
- workflow operasional;
- manajemen stok;
- pricing management;
- payment gateway;
- jurnal akuntansi;
- general ledger;
- BPJS claim engine.

---

## RELATED DOCUMENTS

Dokumen ini memberikan gambaran konseptual mengenai Tata Rekening.

Penjelasan lebih rinci tersedia pada dokumen berikut:

| Document | Description |
|----------|-------------|
| **02-domain.md** | Domain Model, Aggregate, Entity, Value Object, Business Rules |
| **03-design.md** | Clean Architecture, Persistence, Integration, Implementation |
| **04-sop.md** | Business Workflow dan Standard Operating Procedure |