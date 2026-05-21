# lab-workflow.md — Laboratory Workflow Feature

# 1. Overview

Laboratory Workflow Feature (LWF) adalah operational workflow subsystem untuk pemeriksaan laboratorium rumah sakit.

LWF bertanggung jawab terhadap:

* operational lifecycle pemeriksaan,
* specimen collection workflow,
* result recording,
* result verification,
* dan result release.

LWF bukan:

* LIS penuh,
* billing system,
* registration system,
* maupun analyzer system.

---

# 2. Workflow Lifecycle

## Main Workflow

```text id="q8m2v7"
Ordered
→ Deferred (optional)
→ Charged
→ Collected
→ Recorded
→ Verified
→ Released
```

---

# 3. Workflow State

| State     | Meaning                             |
| --------- | ----------------------------------- |
| Ordered   | Order diterima LWF                  |
| Deferred  | Menunggu persiapan pasien           |
| Charged   | Billing charge berhasil dibuat      |
| Collected | Specimen sudah diambil              |
| Recorded  | Hasil sudah direkam                 |
| Verified  | Diverifikasi dokter patologi klinik |
| Released  | Hasil resmi diserahkan              |

---

# 4. Deferred Workflow

Deferred digunakan untuk:

* puasa,
* pemeriksaan hari berikutnya,
* atau persiapan medis lainnya.

Saat Deferred:

* specimen belum boleh diambil,
* billing belum boleh dibuat,
* result belum boleh direkam.

Saat pasien kembali:

* LWF membuat registrasi baru ke REG,
* lalu workflow diaktifkan kembali.

---

# 5. Cancellation & Termination

## Cancelled

Hanya valid sebelum specimen collection.

```text id="g7x3m1"
Ordered → Cancelled
Deferred → Cancelled
Charged → Cancelled
```

---

## Terminated

Workflow sudah berjalan tetapi dihentikan.

```text id="m4v8p2"
Collected → Terminated
Recorded → Terminated
```

Termination wajib memiliki alasan.

---

# 6. Immutable State

State berikut immutable:

```text id="r9k2w5"
Verified
Released
```

Tidak boleh:

* cancel,
* terminate,
* rollback.

---

# 7. Verification vs Release

Verification dan Release adalah proses berbeda.

| Process      | Responsibility          |
| ------------ | ----------------------- |
| Verification | Medical validation      |
| Release      | Administrative delivery |

Result:

* boleh sudah verified,
* tetapi belum boleh dirilis.

---

# 8. Billing Release Validation

Saat user melakukan **release attempt**, LWF memanggil modul BIL (in-process) untuk validasi realtime.

| Outcome | Behavior |
| ------- | -------- |
| **CLEAR** | LWF mengeksekusi `Released`; persist audit trace |
| **BLOCKED** | HTTP 200; `released=false`, `billingStatus=BLOCKED`; order tetap **Verified** |
| **Infrastructure failure** | Normal error path (BIL unavailable) — distinct from BLOCKED |

Hasil **verified** tetap visible internal meskipun BLOCKED.

LWF **tidak** menyimpan approval lifecycle (`approve`/`reject` clearance) — **OBSOLETE**.

---

# 9. Integration Responsibility

| System | Responsibility                 |
| ------ | ------------------------------ |
| EMR    | Clinical order                 |
| REG    | Patient registration           |
| BIL    | Billing authority              |
| OWR    | Analyzer/LIS integration       |
| LWF    | Operational workflow authority |

---

# 10. Integration Pattern

## Synchronous Integration

### REG

* Create Registration

### BIL

* Create Billing Charge

Jika gagal:

* workflow tidak dilanjutkan,
* user harus memperbaiki data lalu retry.

---

## Asynchronous Integration

### OWR

* Send order to infrastructure
* Receive result from infrastructure

OWR communication:

* queue-based,
* cronjob/worker based,
* asynchronous.

---

# 11. Result Source

Result dapat berasal dari:

```text id="z6p4x8"
Manual
Instrument
ExternalLIS
```

Result source wajib tercatat untuk audit.

---

# 12. Result Verification

Verification hanya boleh dilakukan oleh:

```text id="j2m7v9"
Dokter Patologi Klinik
```

Verification dilakukan terhadap:

```text id="c5x1p4"
LabResultDocument
```

bukan LabOrder.

---

# 13. Amendment Workflow

Jika hasil salah setelah verified:

* result tidak diubah,
* tetapi dibuat versi baru.

Versioning menggunakan:

* immutable snapshot,
* full result copy per version.

---

# 14. OWARE Status

Karena komunikasi asynchronous, maka order memiliki OwareStatus.

## Example

```text id="n7v2m5"
Pending
Sent
Failed
```

---

# 15. Operational UX Principle

Semua aktivitas dilakukan dalam:

* single operational workspace,
* tanpa perpindahan antar subsystem.

LWF menjadi orchestration layer:

* REG,
* BIL,
* dan OWR
  di belakang layar.

---

# 16. Important Operational Rules

## Rule 1

Setelah specimen collected:

* order tidak boleh diubah.

---

## Rule 2

Verified result tidak boleh dimutasi.

Perubahan harus melalui amendment.

---

## Rule 3

LWF adalah operational source-of-truth.

EMR hanya:

* membuat order,
* dan request cancellation.

---

## Rule 4

OWR bukan source-of-truth hasil.

LWF tetap menjadi:

* official result owner,
* dan verification authority.
