# lab-integration.md — Laboratory Workflow Feature

# 1. Overview

LWF terintegrasi dengan beberapa subsystem external:

| System | Responsibility          |
| ------ | ----------------------- |
| EMR    | Clinical ordering       |
| REG    | Patient registration    |
| BIL    | Billing authority       |
| OWR    | Analyzer/LIS middleware |

LWF adalah:

* operational workflow authority,
* dan orchestration layer
  untuk proses pemeriksaan laboratorium.

---

# 2. Integration Philosophy

LWF menggunakan 2 pola integrasi:

| Pattern      | Type                       |
| ------------ | -------------------------- |
| Synchronous  | Actor-driven integration   |
| Asynchronous | Infrastructure integration |

---

# 3. EMR Integration

## Purpose

EMR digunakan untuk:

* membuat order pemeriksaan laboratorium,
* dan request cancellation.

---

## Integration Type

```text id="n4x2m8"
Synchronous API
```

---

## Main API

Canonical contract (pre-release, Tarif-only target): [`LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md`](LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md) **§7.3** and [`LAB_API_CONTRACT.md`](LAB_API_CONTRACT.md).

### Create Order

```text
POST /api/LabContext/LabOrderFeature/fromEmr
```

**EMR sends:**

```text
EmrOrderId
Items[]: { TarifId, TarifName? }
+ patient snapshot fields (RegId, PatientId, PatientName, BirthDateYmd, Gender)
```

**EMR does not send:** `TestId`, `TestCode`, `TestName`, tube/specimen/count, or component lists. LWF resolves `LabTestDefinition`, specimen, vacutainer, and persists immutable `LabOrderItem` + `LabOrderItemComponent` snapshots.

**EMR response:** `EmrOrderId`, `LabOrderStatus`, `OrderNo` — EMR must not depend on internal `LabOrderId`.

### Status lookup

```text
GET /api/LabContext/LabOrderFeature/byEmrOrderId/{emrOrderId}
```

Returns workflow summary for EMR correlation (`LabOrderStatus`, `OrderNo`, billing/cancel fields).

### Cancel Order

```text
POST /api/LabContext/LabOrderFeature/emr/cancel
```

---

## Important Rules

EMR:

* bukan source-of-truth workflow,
* tidak melakukan state synchronization,
* dan tidak boleh bypass workflow invariant.

Setelah order diterima:

* lifecycle sepenuhnya milik LWF.

---

# 4. REG Integration

## Purpose

REG digunakan untuk:

* membuat registrasi pasien external,
* dan deferred execution registration.

---

## Integration Type

```text id="j3v8m2"
Synchronous API
```

---

## Trigger

REG integration dilakukan:

* saat actor klik Save,
* dan actor menunggu response.

---

## Main API

### Create Registration

```text id="x6k1p9"
POST /api/reg/registrations
```

---

## Failure Behavior

Jika REG gagal:

* workflow tidak dilanjutkan,
* order dianggap gagal diproses,
* user harus memperbaiki data lalu retry.

---

# 5. BIL Integration

## Purpose

BIL digunakan untuk:

* membuat billing charge,
* dan **release eligibility validation** (financial authority).

---

## Integration Type

```text id="r7m2v5"
Synchronous in-process module orchestration (same backend / modular monolith)
```

**Do not** use HTTP, message broker, or remote API **between LWF and BIL inside this solution**.

Application boundary: `ILabBillingIntegration` in `Bilreg.Application`.

---

## Main operations

| Operation | Contract | Notes |
| --------- | -------- | ----- |
| Create charge | `CreateTindakan(LabBillingChargeRequest)` | Actor-driven; failure blocks charge transition |
| Release validation | `ValidateReleaseEligibility(LabBillingReleaseValidationRequest)` | Called on every release attempt; returns `CLEAR` or `BLOCKED` |

External HTTP examples (e.g. `POST /api/bil/charges`) describe **other systems** talking to BIL — not LWF→BIL inside Bilreg.

---

## Important Rules

LWF:

* tidak memiliki billing logic,
* tidak menyimpan payment state,
* tidak menjalankan approve/reject clearance workflow (**OBSOLETE**),
* hanya menyimpan **LastBillingRelease\*** audit trace after each check.

Billing authority tetap milik BIL.

---

## Failure Behavior

**Charge failure:** workflow tidak advance; user retry setelah perbaikan data.

**Release BLOCKED:** HTTP 200 operational response — bukan error UX.

**BIL infrastructure failure:** `LabBillingReleaseValidationException` — distinct from BLOCKED.

---

# 6. OWR Integration

## Purpose

OWR digunakan untuk:

* bridge ke LIS,
* bridge ke analyzer,
* dan exchange result data.

---

## Integration Type

```text id="g5x1m7"
Asynchronous
```

---

## Communication Pattern

OWR communication menggunakan:

* queue,
* cronjob,
* autonomous worker,
* atau scheduled background process.

---

# 7. OWR Outbound Flow

## Send Order To OWR

```text id="b8m4v2"
LWF → OWR
```

Order dikirim berdasarkan:

* pending queue,
* retry policy,
* dan integration status.

---

## OwareStatus

```text id="n6p2x5"
Pending
Sent
Failed
```

---

# 8. OWR Inbound Flow

## Receive Result From OWR

```text id="y3v7m1"
OWR → LWF
```

Result matching menggunakan:

```text id="k1x8p4"
OrderNo
```

---

# 9. Result Ownership

OWR bukan source-of-truth hasil.

LWF tetap menjadi:

* official result owner,
* verification authority,
* dan rendering authority.

---

# 10. Result Source

Result source wajib dicatat.

## Supported Source

```text id="w7m2v8"
Manual
Instrument
ExternalLIS
```

---

# 11. Integration Retry Strategy

## REG & BIL

Tidak menggunakan background retry.

Jika gagal:

* user harus memperbaiki,
* lalu retry manual.

---

## OWR

Menggunakan retry asynchronous:

* queue retry,
* worker retry,
* atau scheduled retry.

---

# 12. Operational Integration Rule

## Rule 1

EMR hanya:

* initiator,
* bukan workflow owner.

---

## Rule 2

LWF adalah:

* operational source-of-truth,
* workflow authority.

---

## Rule 3

OWR hanya:

* middleware,
* integration bridge,
* bukan owner workflow.

---

## Rule 4

Financial clearance hanya menentukan:

* apakah hasil boleh dirilis.

Financial clearance:

* bukan payment state,
* bukan workflow state.

---

# 13. Common Failure Scenario

| Scenario                    | Behavior             |
| --------------------------- | -------------------- |
| REG failed                  | Workflow blocked     |
| Billing failed              | Workflow blocked     |
| OWR failed                  | Retry asynchronous   |
| Financial clearance pending | Release blocked      |
| Result import mismatch      | Manual investigation |
