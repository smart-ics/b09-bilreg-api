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

### Create Order

```text id="z8v5k1"
POST /api/lwf/orders
```

---

### Cancel Order

```text id="m1q7w4"
POST /api/lwf/orders/{id}/cancel
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
* dan financial clearance validation.

---

## Integration Type

```text id="r7m2v5"
Synchronous API
```

---

## Main API

### Create Billing Charge

```text id="t2x8w1"
POST /api/bil/charges
```

---

### Check Financial Clearance

```text id="p4v6m9"
GET /api/bil/financial-clearance/{orderNo}
```

---

## Important Rules

LWF:

* tidak memiliki billing logic,
* tidak menyimpan payment state,
* dan bukan financial authority.

Billing authority tetap milik BIL.

---

## Failure Behavior

Jika create billing gagal:

* workflow tidak dilanjutkan,
* user harus memperbaiki data,
* lalu retry charge process.

Contoh:

* tarif tidak aktif,
* mapping tidak ditemukan,
* registrasi invalid.

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
