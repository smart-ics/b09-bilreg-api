# lab-domain.md — Laboratory Workflow Feature

## 1. Overview

Laboratory Workflow Feature (LWF) adalah operational workflow subsystem untuk pemeriksaan laboratorium rumah sakit.

LWF bukan LIS (Laboratory Information System) penuh.

LWF fokus pada:

* operational workflow pemeriksaan laboratorium,
* orchestration antar subsystem,
* result validation,
* dan result release.

LWF tidak menangani:

* analyzer interfacing langsung,
* QC management,
* reagent management,
* accessioning penuh,
* maupun infrastructure machine management.

---

# 2. External Subsystems

LWF terintegrasi dengan:

| Subsystem | Responsibility                       |
| --------- | ------------------------------------ |
| EMR       | Clinical ordering                    |
| REG       | Patient registration                 |
| BIL       | Financial & billing authority        |
| OWR       | Laboratory infrastructure middleware |

---

# 3. Operational Philosophy

LWF menggunakan prinsip:

```text id="s7m2x9"
Operational Workflow First
```

LWF menjadi:

* operational source-of-truth,
* workflow authority,
* dan orchestration layer
  untuk seluruh proses pemeriksaan laboratorium.

---

# 4. Workflow Lifecycle

## Main Workflow

```text id="x4k8w2"
Ordered
→ Deferred (optional)
→ Charged
→ Collected
→ Recorded
→ Verified
→ Released
```

---

# 5. Workflow State Meaning

| State     | Meaning                                   |
| --------- | ----------------------------------------- |
| Ordered   | Order diterima LWF                        |
| Deferred  | Menunggu persiapan pasien (contoh: puasa) |
| Charged   | Billing charge berhasil dibuat            |
| Collected | Specimen sudah diambil                    |
| Recorded  | Hasil sudah direkam                       |
| Verified  | Diverifikasi dokter patologi klinik       |
| Released  | Hasil resmi diserahkan                    |

---

# 6. Alternate Workflow

## Cancellation

Valid hanya sebelum specimen collection.

```text id="z5d2n7"
Ordered → Cancelled
Deferred → Cancelled
Charged → Cancelled
```

---

## Termination

Workflow sudah berjalan tetapi dihentikan.

```text id="k9v1r3"
Collected → Terminated
Recorded → Terminated
```

Termination wajib memiliki alasan.

---

## Immutable State

State berikut immutable:

```text id="w2t7f6"
Verified
Released
```

Tidak boleh:

* cancel,
* terminate,
* rollback.

Verified/Released workflow states are operationally final.

Exception:
Result amendment creates new ResultDocument version and moves LabOrderStatus back to Recorded for re-verification workflow, while preserving all previous verified/released versions immutably.

---

# 7. Release vs Verification

Verification dan Release adalah dua proses berbeda.

| Process      | Responsibility                 |
| ------------ | ------------------------------ |
| Verification | Medical validation             |
| Release      | Administrative result delivery |

Result:

* boleh sudah verified,
* tetapi belum boleh released.

---

# 8. Financial Clearance

Financial clearance digunakan untuk menentukan apakah hasil boleh dirilis ke pasien.

Financial clearance:

* bukan payment status,
* bukan billing ownership,
* dan bukan workflow status.

Financial clearance hanya menentukan:

```text id="j4m8p1"
Result Release Eligibility
```

---

# 9. Aggregate Design

## Primary Aggregate

```text id="h7n3x5"
LabOrder
```

Responsibilities:

* operational workflow lifecycle
* billing orchestration
* collection workflow
* release eligibility
* external integration coordination

---

## Secondary Aggregate

```text id="y1q6m4"
LabResultDocument
```

Responsibilities:

* laboratory result
* result verification
* amendment/versioning
* result rendering source

---

# 10. Aggregate: LabOrder

## Example Structure

```text id="p8v2w6"
LabOrder
 ├── OrderId
 ├── OrderNo
 ├── OrderSource
 ├── WorkflowStatus
 ├── FinancialClearance
 ├── OwareStatus
 ├── PatientSnapshot
 ├── CollectionInfo
 ├── DeferredInfo
 ├── List<LabOrderItem>
 └── AuditTrail
```

---

# 11. Patient Snapshot

LabOrder menyimpan snapshot patient data.

## Snapshot Fields

```text id="x6c1m9"
RegId
PatientId
MRNumber
PatientName
BirthDate
Gender
AgeAtOrder
```

Snapshot digunakan untuk:

* historical consistency
* result rendering
* reporting
* menghindari dependency ke REG

---

# 12. External Patient Flow

Untuk External Patient:

```text id="u3w8k5"
RegId = null
PatientId = null
```

sementara saat order dibuat.

Setelah REG berhasil membuat registrasi:

* snapshot patient diupdate,
* RegId diattach ke order.

---

# 13. Order Source

Order source wajib explicit.

## Supported Sources

```text id="d2n7f4"
EMR
ExternalPatient
```

---

# 14. Deferred Workflow

Deferred digunakan untuk:

* puasa,
* pemeriksaan hari berikutnya,
* atau persiapan medis lainnya.

Saat Deferred:

* specimen belum boleh diambil,
* billing belum boleh diproses,
* result belum boleh direkam.

---

# 15. Deferred Execution

Saat pasien kembali:

* LWF membuat registrasi baru ke REG,
* attach ExecutionRegId,
* lalu workflow diaktifkan kembali.

---

# 16. Billing Integration

LWF tidak memiliki billing.

LWF hanya:

* request billing charge,
* dan menerima hasil billing processing.

Billing authority tetap milik BIL.

---

# 17. Billing Failure

Billing failure bersifat workflow-blocking.

Contoh:

* tarif tidak aktif,
* mapping tidak ditemukan,
* registrasi invalid.

Workflow tidak boleh lanjut sebelum diperbaiki.

---

# 18. Collection Workflow

Collection workflow bersifat operational workflow sederhana.

Tidak termasuk:

* barcode specimen tracking,
* accessioning,
* analyzer routing,
* tube tracking penuh.

---

# 19. Specimen Requirement Snapshot

LabOrderItem wajib menyimpan snapshot specimen requirement.

## Example

```text id="r1k5v8"
TubeColor
SpecimenType
RequiredTubeCount
```

Tujuan:

* historical consistency
* vacutainer preparation
* operational worklist

---

# 20. Vacutainer Rule

Jika beberapa test menggunakan specimen requirement yang sama:

```text id="f9m3q7"
CBC → EDTA
HbA1c → EDTA
```

maka cukup:

* 1 vacutainer EDTA.

---

# 21. OWARE Integration

OWR digunakan sebagai:

* middleware integration,
* bridge ke LIS,
* atau bridge ke analyzer infrastructure.

OWR bukan source-of-truth.

LWF tetap menjadi:

* operational truth,
* medical validation truth,
* official result owner.

---

# 22. OWARE Communication

Komunikasi LWF → OWR bersifat asynchronous.

Diproses melalui:

* queue,
* cronjob,
* atau autonomous worker.

---

# 23. OWARE Status

Karena asynchronous, maka perlu persisted integration status.

## Example

```text id="c8w4t2"
Pending
Sent
Failed
```

---

# 24. Result Entry

Result dapat berasal dari:

```text id="a5n9v3"
Manual
Instrument
ExternalLIS
```

Result source wajib dicatat untuk audit.

---

# 25. Result Structure

## 1 Order = 1 ResultDocument

Result document:

* immutable per version,
* dan menggunakan full snapshot versioning.

---

# 26. Multiple Components

1 test dapat memiliki banyak component.

## Example

```text id="z4k2r8"
Hematologi Lengkap
 ├── Hb
 ├── Leukosit
 ├── Hematokrit
 └── Trombosit
```

---

# 27. Result Type

Supported result type:

```text id="m2x6p1"
Numeric
Text
Option
Narrative
```

---

# 28. Reference Range

Reference range disimpan pada component.

Reference range dapat tergantung:

* gender,
* usia,
* instrument/method.

---

# 29. Auto Flagging

Sistem mendukung auto-flagging:

```text id="b7n3k9"
High
Low
Normal
```

berdasarkan reference range.

---

# 30. Result Verification

Verification hanya boleh dilakukan oleh:

```text id="j8v1f6"
Dokter Patologi Klinik
```

Verification dilakukan terhadap:

```text id="n5w2r4"
LabResultDocument
```

bukan LabOrder.

---

# 31. Amendment

Jika hasil salah setelah verified:

* result tidak diubah,
* tetapi dibuat versi baru.

## Example

```text id="t4q7m1"
Version 1 → Initial Result
Version 2 → Corrected Result
```

---

# 32. Amendment Strategy

Amendment menggunakan:

```text id="g9x4p2"
Full Immutable Snapshot Versioning
```

Setiap versi:

* menyimpan seluruh result,
* bukan partial diff.

---

# 33. Result Rendering

PDF hasil:

* generated on-demand,
* bukan persisted final document.

Tujuan:

* amendment otomatis reflected,
* rendering menggunakan current operational version
(IsCurrentVersion = true)
* Jika versi hasil belum diverifikasi ulang setelah amendment,
maka PDF harus menampilkan status hasil secara explicit.

---

# 34. Internal Visibility

Jika financial clearance belum approved:

* hasil tetap boleh dilihat internal RS,
* tetapi tidak boleh dirilis ke pasien.

---

# 35. API Communication Philosophy

Komunikasi antar subsystem bersifat API-driven.

LWF bukan projection dari subsystem lain.

LWF memiliki:

* aggregate sendiri,
* workflow sendiri,
* invariant sendiri.

---

# 36. EMR Integration

EMR tidak melakukan sync state ke LWF.

EMR hanya:

* request create order,
* request cancellation.

Setelah order diterima:

* lifecycle sepenuhnya milik LWF.

---

# 37. Cancellation Authority

| Source           | Authority |
| ---------------- | --------- |
| EMR Order        | Doctor    |
| External Patient | Lab Staff |

---

# 38. Operational Workspace Philosophy

Actor tidak berpindah antar subsystem.

Semua aktivitas dilakukan melalui:

* single operational screen,
* dengan orchestration dilakukan oleh LWF di belakang layar.

---

# 39. Explicitly Out Of Scope

Hal berikut tidak termasuk scope:

* full LIS
* analyzer management
* reagent management
* QC management
* pathology workflow
* microbiology workflow
* accessioning penuh
* specimen barcode lifecycle
* analyzer routing
* instrument middleware
* delta check
* machine calibration

---

# 40. Future Development

Planned future capabilities:

```text id="v8r2k4"
Priority/Urgency
- STAT
- CITO
- Routine
```

Potential future workflow:

* multi-release authorization
* police/legal release SOP
* audit-grade distribution tracking
* advanced LIS integration
* mobile collection workflow
* barcode collection workflow
* analyzer bidirectional communication
