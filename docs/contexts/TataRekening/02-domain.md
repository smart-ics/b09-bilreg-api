# 02-domain.md — Tata Rekening

## FEATURE NAME

**Tata Rekening**

---

## DOMAIN PURPOSE

Tata Rekening mengelola **Financial Responsibility** untuk satu Registrasi Pasien.

Domain ini bertanggung jawab memastikan bahwa seluruh Billing telah:

- diverifikasi;
- dialokasikan kepada Payer;
- difinalisasi;
- siap diselesaikan melalui proses Payment Settlement.

---

## UBIQUITOUS LANGUAGE

| Term | Description |
|-------|-------------|
| Tata Rekening | Aggregate Root yang mengelola Financial Responsibility suatu Registrasi |
| Billing Set | Sekumpulan Financial Charge milik satu Registrasi |
| TrsBill | Financial Charge individual |
| Merge Request | Permintaan penggabungan Billing antar Registrasi |
| Merge Billing | Proses penggabungan Billing berdasarkan Merge Request |
| Financial Responsibility | Tanggung jawab pembayaran suatu Billing |
| Financial Projection | Hasil Allocation yang digunakan untuk Settlement |
| Verifikator | Aktor yang menjalankan proses Tata Rekening |

---

## AGGREGATE

Hanya terdapat satu Aggregate Root.

```text
Tata Rekening
│
├── Billing Set
│     ├── TrsBill
│     ├── TrsBill
│     └── TrsBill
│
├── Financial Responsibility
│
└── Financial Projection
```

Satu Aggregate mewakili **satu Registrasi**.

---

## ENTITY

### Tata Rekening (Aggregate Root)

Mengelola seluruh Financial Responsibility pada satu Registrasi.

Responsibilities:

- Close Bill
- Merge Billing
- Financial Verification
- Financial Adjustment
- Financial Responsibility Allocation
- Finalize Financial Responsibility
- Cancel Finalization
- Reopen Billing
- Settlement Initiation

---

### TrsBill

Merepresentasikan satu Financial Charge.

TrsBill menyimpan:

- Pricing Snapshot
- Accounting Snapshot
- Charge Amount
- Charge Source Reference

TrsBill tidak dapat:

- melakukan Finalization;
- melakukan Allocation;
- melakukan Payment.

Seluruh keputusan tersebut berada pada Aggregate Tata Rekening.

---

### Merge Request

Merepresentasikan permintaan penggabungan Billing.

Attributes:

- SourceReg
- TargetReg (nullable)
- Status

Status:

- Pending
- Executed
- Cancelled

Merge Request bukan bagian dari Aggregate Tata Rekening, tetapi menjadi input bagi proses Merge Billing.

---

## VALUE OBJECT

### Financial Responsibility

Menyatakan siapa yang bertanggung jawab membayar Billing.

Contoh:

- Pasien
- BPJS
- Asuransi
- Perusahaan
- Subsidi Rumah Sakit

---

### Financial Projection

Representasi hasil Allocation.

Digunakan sebagai dasar:

- Payment Settlement
- Accounting Projection

Financial Projection merupakan derived data yang dapat diregenerasi.

---

## LIFECYCLE

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

| Status | Meaning |
|---------|---------|
| OPEN | Charge Source masih dapat menghasilkan Financial Charge |
| CLOSED | Billing dibekukan untuk Financial Control |
| FINALIZED | Financial Responsibility telah dikunci |
| LUNAS | Seluruh kewajiban finansial telah diselesaikan |

---

## DOMAIN INVARIANTS

### Billing Set

- Seluruh TrsBill harus dimiliki tepat oleh satu Registrasi.
- Billing Set selalu dimiliki oleh satu Aggregate Tata Rekening.

---

### Merge Billing

- Hanya dapat dilakukan pada Billing berstatus **CLOSED**.
- Hanya dapat dilakukan berdasarkan Merge Request berstatus **Pending**.
- Seluruh Billing Set Registrasi sumber dipindahkan sebagai satu kesatuan.
- Merge Billing tidak mengubah Financial Charge.
- Merge Billing tidak mengubah Pricing Snapshot.

---

### Financial Responsibilities

- Seluruh Billing harus memiliki Payer.
- Total Allocation harus sama dengan total Billing.

```text
Σ Allocation = Σ Billing
```

---

### Finalization

- Billing harus berstatus **CLOSED**.
- Financial Verification harus selesai.
- Financial Responsibility harus lengkap.
- Financial Projection harus tersedia.

Setelah Finalization:

- Financial Responsibility terkunci.
- Billing tidak dapat diubah tanpa Cancel Finalization atau Reopen Billing sesuai Business Rules.

---

### Settlement

Settlement hanya dapat dilakukan terhadap Billing yang telah berstatus **FINALIZED**.

---

## DOMAIN SERVICES

Tata Rekening menggunakan beberapa Domain Service.

| Service | Responsibility |
|---------|----------------|
| Merge Billing | Menggabungkan Billing Set antar Registrasi |
| Financial Verification | Memvalidasi Financial Truth |
| Allocation | Menghitung Financial Responsibility |
| Projection | Membentuk Financial Projection |

---

## DOMAIN EVENTS

Peristiwa bisnis utama:

- Bill Closed
- Merge Billing Executed
- Financial Adjusted
- Financial Responsibility Allocated
- Financial Responsibility Finalized
- Finalization Cancelled
- Billing Reopened
- Settlement Initiated

---

## RELATED DOCUMENTS

| Document | Description |
|----------|-------------|
| **[01-context.md](01-context.md)** | Business Context |
| **[03-design.md](03-design.md)** | Architecture & Persistence |
| **[04-sop.md](04-sop.md)** | Business Workflow |
| **[tata-rekening-domain-gap-analysis-report.md](tata-rekening-domain-gap-analysis-report.md)** | Domain gap analysis (implementation vs artifact) |
