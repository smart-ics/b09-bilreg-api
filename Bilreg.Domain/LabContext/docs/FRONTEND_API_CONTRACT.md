# FRONTEND_API_CONTRACT.md

> **Source of truth:** `Bilreg.Api/Controllers/LabContext/*`, Application use-cases, Domain models, Infrastructure DAL projections (May 2026).  
> **Audience:** Frontend engineers and AI frontend agents building the Laboratory Workflow Feature (LWF) UI.  
> **Note:** Frontend standard files (`API_STANDARD.md`, `FRONTEND_ENGINEERING.md`, etc.) are not in this repository; align naming/query patterns with your frontend repo conventions.  
> **Architectural locks:** `FRONTEND_AGENT_RULES.md`, `DOMAIN.md` §1A — especially `BLOCKED` = HTTP 200 operational outcome.

---

# 1. API Overview

## Architecture

| Aspect | Implementation |
|--------|----------------|
| Base path | `/api/LabContext/{FeatureName}/...` |
| Style | Feature-scoped controllers + MediatR commands/queries |
| Mutations | **Body-command:** `POST` / `PATCH` with JSON body records (`*Cmd`) |
| Reads | `GET` with route params and/or query string (`*Query`) |
| Auth | JWT Bearer (`AddAuthentication` in `PresentationService`) — supply `Authorization` header |
| `userId` | Almost every command requires explicit `userId` / `verifiedUserId` / `amendedBy` in the body (not inferred server-side in handlers) |

## JSend response pattern

Successful JSON endpoints wrap payloads with `JSendOk` (`Nuna.Lib.ActionResultHelper`):

```json
{
  "statusCode": 200,
  "status": "success",
  "data": { }
}
```

Commands with no structured response return:

```json
{
  "statusCode": 200,
  "status": "success",
  "data": "Done"
}
```

**Exceptions**

| Endpoint | Response |
|----------|----------|
| `GET .../LabResultFeature/{orderId}/pdf` | Raw `File(...)` — `Content-Type: application/pdf` (or configured type), **not** JSend |

## Error pattern (`ErrorHandlerMiddleware`)

| Exception | HTTP | JSend `status` |
|-----------|------|----------------|
| `ArgumentException`, `InvalidOperationException` | 400 | `"Bad Request"` |
| `KeyNotFoundException` | 400 | `"Data Not Found"` |
| `TooManyResultsException` | 422 | `"Too Many Results"` |
| Other | 500 | `"Internal Server Error"` |

```json
{
  "statusCode": 400,
  "status": "Bad Request",
  "data": "LabOrder 'LBO...' berstatus Charged; ..."
}
```

`data` is the exception **message string** (Indonesian operational text from domain).

## JSON serialization

- Default ASP.NET Core JSON: **camelCase** property names (`orderId`, `labOrderStatus`, …).
- Enums in API contracts are **integers** unless client configures string enums.
- **Empty / unset dates** in persistence often use sentinel `3000-01-01T00:00:00` — treat as “no value” in UI.

## Body-command philosophy

- Prefer `PATCH /feature/action` + JSON body over RESTful noun updates.
- One command = one workflow transition (charge, collect, verify, release, …).
- Idempotency is **not** guaranteed; rely on domain state checks and error messages.

## Operational workflow philosophy

- Single worklist-driven workspace; drill into order detail panels.
- LWF orchestrates REG (activate deferred), BIL (charge), OWARE (async queue) behind commands.
- **Order status** (`LabOrderStatus`) and **result status** (`LabResultStatus`) are related but separate.
- **Billing release validation** is realtime at `PATCH release` — BIL decides `CLEAR` / `BLOCKED`; LWF does not store financial clearance state.
- **LOCK:** `BLOCKED` is operational business outcome — **HTTP 200** with `released: false`; frontend uses toast/message, not exception flow. See `FRONTEND_AGENT_RULES.md`.

---

# 2. Workflow Summary

## Main happy path

```text
Create (Ordered)
  → [optional] Defer (Deferred)
  → [optional] Activate (Ordered + executionRegId)
  → Charge (Charged)          ← BIL integration
  → [optional] OWARE Enqueue  ← async, after Charged
  → Collect (Collected)
  → Record (Recorded)         ← LabResultDocument
  → Verify (Verified)
  → Release Attempt → Ask BIL (CLEAR / BLOCKED)
  → Release (Released)   ← only when CLEAR
```

## Parallel / optional paths

| Step | Notes |
|------|--------|
| **OWARE** | `enqueue` after Charged; `process` / worker sends payload; order `owareStatus` → Sent/Failed |
| **Record before collect** | Domain allows record from **Charged** or **Collected** (specimen optional in API) |
| **Amend** | From **Verified** or **Released** → order returns to **Recorded**; new result version; re-verify required; next release calls BIL again (no stale approval) |
| **Cancel** | Ordered, Deferred, Charged only |
| **Terminate** | Collected or Recorded only; reason required |

## State transition matrix (order)

| From → To | Trigger API |
|-----------|-------------|
| — → Ordered | `POST fromEmr`, `POST external` |
| Ordered → Deferred | `PATCH defer` |
| Deferred → Ordered | `PATCH activateDeferred` (+ REG) |
| Ordered → Charged | `PATCH charge` (success) |
| Charged → Collected | `PATCH collect` |
| Charged/Collected/Recorded → Recorded | `POST record` (may stay Recorded on update) |
| Recorded → Verified | `PATCH verify` |
| Verified → Released | `PATCH release` (BIL validation = CLEAR) |
| Ordered/Deferred/Charged → Cancelled | `PATCH cancel` |
| Collected/Recorded → Terminated | `PATCH terminate` |
| Verified/Released → Recorded | `PATCH amend` |

## Billing release validation (BIL authority, not LWF state)

| Result | Meaning | When |
|--------|---------|------|
| `CLEAR` | Release allowed | `PATCH release` — BIL approves eligibility |
| `BLOCKED` | Release denied (operational) | `PATCH release` — **HTTP 200**, `released: false`, show `message` toast; order stays **Verified** |

No separate approve/reject endpoints. No `financialClearance` field on order.

**Transport LOCK:** `BLOCKED` is **not** HTTP 400 and **not** an exception workflow — it is a normal successful API response with `released: false`.

## Frontend implications

| Concern | Guidance |
|---------|----------|
| Worklist tabs | Filter `GET worklist?labOrderStatus=` per operational queue |
| Detail panel | `GET {orderId}` + `GET LabResultFeature/{orderId}` |
| Action buttons | Enable/disable from `labOrderStatus`, `owareStatus`, `isVoided` |
| Charge failure | HTTP 200 with `success: false` — show `billingLastError`, allow retry |
| Release queue | `GET releaseWorklist` = **Verified** orders not yet released |
| Release BLOCKED | HTTP **200** + `released: false`, `billingStatus: BLOCKED`, `message` — operational toast; screen stays on workspace; **do not** use error/exception handlers |
| Verification queue | `GET worklist/verification` |
| PDF | Binary download; open blob URL or print dialog |
| Dates | Hide or label sentinel `3000-01-01` as empty |

## Screen expectations (high level)

| Screen | Primary APIs |
|--------|----------------|
| Order worklist | `GET LabOrderFeature/worklist` |
| Order detail / timeline | `GET LabOrderFeature/{orderId}`, mutations by state |
| Collection prep | `GET collectionPreparation?orderId=` |
| Specimen collect | `PATCH collect` |
| Result entry | `GET` result + `POST record` |
| Verification | `GET worklist/verification`, `PATCH verify` |
| Release | `GET releaseWorklist`, `PATCH release` (realtime BIL validation) |
| OWARE monitor | `GET LabOwareFeature/worklist`, `POST enqueue`, `PATCH retry` |
| Amendment | `PATCH amend` then re-record / re-verify flow |

---

# 3. Endpoint Catalog

**Route prefix:** `/api/LabContext/LabOrderFeature`  
**Total LabOrder endpoints:** 13

---

## Create Order From EMR

### Route

```http
POST /api/LabContext/LabOrderFeature/fromEmr
```

### Purpose

Create lab order from EMR context with full patient registration snapshot.

### Frontend Usage

- EMR order acceptance screen
- “Send to lab” integration action

### Request Payload

```json
{
  "userId": "USR001",
  "regId": "REG20260518001",
  "patientId": "MR000123",
  "patientName": "Pasien Tes",
  "birthDateYmd": "1990-05-15",
  "gender": "L",
  "items": [
    {
      "testId": "T-HB",
      "testCode": "HB",
      "testName": "Hemoglobin",
      "tarifId": "TR-HB",
      "tarifCode": "T-HB",
      "tarifName": "Tarif Hemoglobin",
      "tubeType": 1,
      "specimenType": "Blood",
      "requiredTubeCount": 1
    }
  ]
}
```

```ts
interface LabOrderCreateFromEmrRequest {
  userId: string
  regId: string
  patientId: string
  patientName: string
  birthDateYmd: string // yyyy-MM-dd
  gender: string
  items: LabOrderItemInput[]
}

interface LabOrderItemInput {
  testId: string
  testCode: string
  testName: string
  tarifId: string
  tarifCode: string
  tarifName: string
  tubeType: number // VacutainerTypeEnum
  specimenType: string
  requiredTubeCount: number
}
```

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "orderId": "LBO000000001",
    "orderNo": "LAB00000001"
  }
}
```

```ts
interface LabOrderCreateResponse {
  orderId: string
  orderNo: string
}
```

### Validation / Invariant

- `userId`, `regId`, `patientId`, `patientName` required
- `items` non-empty
- Initial `labOrderStatus` = **Ordered (1)**

### Query Invalidations

- `['lab-order-worklist']`
- Optionally navigate to `['lab-order-detail', orderId]`

### Suggested Frontend Query Key

```ts
['lab-order-worklist']
['lab-order-detail', orderId]
```

---

## Create Order External Patient

### Route

```http
POST /api/LabContext/LabOrderFeature/external
```

### Purpose

Walk-in / external patient order; `regId` / `patientId` optional.

### Frontend Usage

- External patient registration form in lab workspace

### Request Payload

```json
{
  "userId": "USR001",
  "regId": null,
  "patientId": null,
  "patientName": "Pasien Luar",
  "birthDateYmd": "1985-01-01",
  "gender": "P",
  "items": [ { "testId": "T1", "testCode": "GLU", "testName": "Glukosa", "tarifId": "TR1", "tarifCode": "GLU", "tarifName": "Tarif Glukosa", "tubeType": 2, "specimenType": "Serum", "requiredTubeCount": 1 } ]
}
```

```ts
interface LabOrderCreateExternalPatientRequest {
  userId: string
  regId?: string | null
  patientId?: string | null
  patientName: string
  birthDateYmd?: string | null
  gender?: string | null
  items: LabOrderItemInput[]
}
```

### Response Payload

Same as **Create Order From EMR** (`LabOrderCreateResponse`).

### Validation / Invariant

- `patientName`, `userId`, `items` required
- `orderSource` = **ExternalPatient (2)**

### Query Invalidations

- `['lab-order-worklist']`

### Suggested Frontend Query Key

```ts
['lab-order-worklist']
```

---

## Order Worklist

### Route

```http
GET /api/LabContext/LabOrderFeature/worklist?labOrderStatus={int?}&searchTerm={string?}&date1={datetime?}&date2={datetime?}
```

### Purpose

Operational order queue with optional status filter, date range, search.

### Frontend Usage

- Main worklist grid
- Tab per status (pass `labOrderStatus`)

### Request Payload

Query only (no body).

| Query | Type | Notes |
|-------|------|-------|
| `labOrderStatus` | int? | `LabOrderStatusEnum` value |
| `searchTerm` | string? | `LIKE` on orderNo, patientName, patientId |
| `date1`, `date2` | datetime? | Filter `crtDate` inclusive start, exclusive end (`date2` + 1 day) — **both required** to apply date filter |

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": [
    {
      "orderId": "LBO000000001",
      "orderNo": "LAB00000001",
      "labOrderStatus": 1,
      "orderSource": 1,
      "patientId": "MR0001",
      "patientName": "Pasien Tes",
      "gender": "L",
      "ageAtOrder": 36,
      "itemCount": 1,
      "owareStatus": 0,
      "crtDate": "2026-05-18T10:00:00",
      "billingTindakanId": "",
      "testNames": ["Hemoglobin"]
    }
  ]
}
```

```ts
interface LabOrderWorklistView {
  orderId: string
  orderNo: string
  labOrderStatus: number
  orderSource: number
  patientId: string
  patientName: string
  gender: string
  ageAtOrder: number
  itemCount: number
  owareStatus: number
  crtDate: string
  billingTindakanId: string
  testNames?: string[] | null
}
```

### Validation / Invariant

- Excludes voided orders (`VodDate = 3000-01-01` sentinel)
- Ordered by `crtDate` DESC

### Query Invalidations

- Self; any order mutation invalidates worklist

### Suggested Frontend Query Key

```ts
['lab-order-worklist', { labOrderStatus, searchTerm, date1, date2 }]
```

---

## Release Worklist

### Route

```http
GET /api/LabContext/LabOrderFeature/releaseWorklist?searchTerm={string?}&date1={datetime?}&date2={datetime?}
```

### Purpose

Orders ready for **release attempt**: `labOrderStatus = Verified (6)` and not yet **Released**. BIL validation runs on `PATCH release`, not as a pre-filter on this worklist.

### Frontend Usage

- Release officer worklist
- “Ready to deliver” queue

### Request Payload

Query: `searchTerm`, `date1`, `date2` (date filters on **result verified date**).

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": [
    {
      "orderId": "LBO000000001",
      "orderNo": "LAB00000001",
      "patientName": "Pasien Tes",
      "verifiedDate": "2026-05-19T14:00:00",
      "releasedDate": "3000-01-01T00:00:00",
      "labOrderStatus": 6
    }
  ]
}
```

```ts
interface LabReleaseView {
  orderId: string
  orderNo: string
  patientName: string
  verifiedDate: string
  releasedDate: string
  labOrderStatus: number
}
```

### Validation / Invariant

- Joins current `LabResultDocument` (`isCurrentVersion = 1`)
- Does not include already **Released** orders (status filter is Verified only)

### Query Invalidations

- `['lab-release-worklist']`
- After `PATCH release`: worklist + order detail

### Suggested Frontend Query Key

```ts
['lab-release-worklist', { searchTerm, date1, date2 }]
```

---

## Collection Preparation

### Route

```http
GET /api/LabContext/LabOrderFeature/collectionPreparation?orderId={orderId}
```

### Purpose

Specimen collection panel: tests + vacutainer grouping.

### Frontend Usage

- Collection preparation right panel / modal before `PATCH collect`

### Request Payload

Query: `orderId` (required).

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "orderId": "LBO000000001",
    "orderNo": "LAB00000001",
    "patientId": "MR0001",
    "patientName": "Pasien Tes",
    "labOrderStatus": 3,
    "tests": [
      {
        "itemNo": 1,
        "testId": "T-HB",
        "testCode": "HB",
        "testName": "Hemoglobin",
        "tubeType": 1,
        "specimenType": "Blood",
        "requiredTubeCount": 1
      }
    ],
    "vacutainerGroups": [
      { "tubeType": 1, "specimenType": "Blood", "tubeCount": 1 }
    ]
  }
}
```

```ts
interface LabCollectionPreparationView {
  orderId: string
  orderNo: string
  patientId: string
  patientName: string
  labOrderStatus: number
  tests: LabCollectionPreparationTestItem[]
  vacutainerGroups: LabCollectionPreparationVacutainerGroup[]
}

interface LabCollectionPreparationTestItem {
  itemNo: number
  testId: string
  testCode: string
  testName: string
  tubeType: number
  specimenType: string
  requiredTubeCount: number
}

interface LabCollectionPreparationVacutainerGroup {
  tubeType: number
  specimenType: string
  tubeCount: number // max requiredTubeCount per tube+specimen group
}
```

### Validation / Invariant

- Order must exist; unknown `orderId` throws generic `Exception` → likely **500** (not 404)

### Query Invalidations

- `['lab-collection-preparation', orderId]`

### Suggested Frontend Query Key

```ts
['lab-collection-preparation', orderId]
```

---

## Get Order Detail

### Route

```http
GET /api/LabContext/LabOrderFeature/{orderId}
```

### Purpose

Full order aggregate for detail panel / action bar.

### Frontend Usage

- Order detail drawer
- Enable workflow buttons

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "orderId": "LBO000000001",
    "orderNo": "LAB00000001",
    "orderSource": 1,
    "labOrderStatus": 3,
    "owareStatus": 0,
    "regId": "REG001",
    "patientId": "MR0001",
    "patientName": "Pasien Tes",
    "birthDate": "1990-01-01T00:00:00",
    "gender": "L",
    "ageAtOrder": 36,
    "executionRegId": "",
    "deferredReason": "",
    "deferredUntil": "3000-01-01T00:00:00",
    "billingTindakanId": "TDK-FAKE-0001",
    "billingLastError": "",
    "collectedDate": "3000-01-01T00:00:00",
    "collectedUserId": "",
    "collectionNote": "",
    "lastBillingReleaseCheck": {
      "checkedAt": "3000-01-01T00:00:00",
      "billingStatus": "",
      "message": "",
      "requestedByUserId": ""
    },
    "releasedDate": "3000-01-01T00:00:00",
    "releasedUserId": "",
    "releaseNote": "",
    "cancelledReason": "",
    "cancelledDate": "3000-01-01T00:00:00",
    "cancelledUserId": "",
    "terminationReason": "",
    "terminationDate": "3000-01-01T00:00:00",
    "terminationUserId": "",
    "isVoided": false,
    "items": [
      {
        "itemNo": 1,
        "testId": "T-HB",
        "testCode": "HB",
        "testName": "Hemoglobin",
        "tarifId": "TR1",
        "tarifCode": "T-HB",
        "tarifName": "Tarif",
        "tubeType": 1,
        "specimenType": "Blood",
        "requiredTubeCount": 1
      }
    ]
  }
}
```

```ts
interface LabOrderGetResponse {
  orderId: string
  orderNo: string
  orderSource: number
  labOrderStatus: number
  owareStatus: number
  lastBillingReleaseCheck: LabBillingReleaseCheckSnapshot | null
  regId: string
  patientId: string
  patientName: string
  birthDate: string
  gender: string
  ageAtOrder: number
  executionRegId: string
  deferredReason: string
  deferredUntil: string
  billingTindakanId: string
  billingLastError: string
  collectedDate: string
  collectedUserId: string
  collectionNote: string
  releasedDate: string
  releasedUserId: string
  releaseNote: string
  cancelledReason: string
  cancelledDate: string
  cancelledUserId: string
  terminationReason: string
  terminationDate: string
  terminationUserId: string
  isVoided: boolean
  items: LabOrderItemResponse[]
}

interface LabBillingReleaseCheckSnapshot {
  checkedAt: string
  billingStatus: 'CLEAR' | 'BLOCKED' | '' // empty when never checked
  message: string
  requestedByUserId: string
}

interface LabOrderItemResponse {
  itemNo: number
  testId: string
  testCode: string
  testName: string
  tarifId: string
  tarifCode: string
  tarifName: string
  tubeType: number
  specimenType: string
  requiredTubeCount: number
}
```

### Validation / Invariant

- Not found → error message via `GetValueOrThrow`

### Query Invalidations

- All order-scoped queries

### Suggested Frontend Query Key

```ts
['lab-order-detail', orderId]
```

---

## Defer Order

### Route

```http
PATCH /api/LabContext/LabOrderFeature/defer
```

### Purpose

Move **Ordered → Deferred** (fasting, next-day prep).

### Frontend Usage

- Defer modal (reason + until date)

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001",
  "reason": "Puasa 12 jam",
  "untilDate": "2026-05-20T08:00:00"
}
```

```ts
interface LabOrderDeferRequest {
  orderId: string
  userId: string
  reason: string
  untilDate: string // ISO datetime
}
```

### Response Payload

`data: "Done"`

### Validation / Invariant

- Only from **Ordered (1)**

### Query Invalidations

- worklist, order detail

### Suggested Frontend Query Key

```ts
['lab-order-worklist']
['lab-order-detail', orderId]
```

---

## Activate Deferred Order

### Route

```http
PATCH /api/LabContext/LabOrderFeature/activateDeferred
```

### Purpose

**Deferred → Ordered**; creates REG execution registration.

### Frontend Usage

- “Patient returned” action on deferred orders

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001"
}
```

```ts
interface LabOrderActivateDeferredRequest {
  orderId: string
  userId: string
}
```

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "executionRegId": "REG-EXEC-0001"
  }
}
```

```ts
interface LabOrderActivateDeferredResponse {
  executionRegId: string
}
```

### Validation / Invariant

- Only from **Deferred (2)**
- Calls `ILabRegIntegration.CreateExecutionRegistration`

### Query Invalidations

- worklist, order detail

### Suggested Frontend Query Key

```ts
['lab-order-worklist']
['lab-order-detail', orderId]
```

---

## Charge Order

### Route

```http
PATCH /api/LabContext/LabOrderFeature/charge
```

### Purpose

Billing charge via BIL; **Ordered → Charged** on success.

### Frontend Usage

- “Charge” button on order detail
- Show billing error + retry when `success: false`

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001"
}
```

```ts
interface LabOrderChargeRequest {
  orderId: string
  userId: string
}
```

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "success": true,
    "billingTindakanId": "TDK-FAKE-0001",
    "billingLastError": null
  }
}
```

Failure (billing exception caught):

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "success": false,
    "billingTindakanId": null,
    "billingLastError": "Tarif tidak ditemukan"
  }
}
```

```ts
interface LabOrderChargeResponse {
  success: boolean
  billingTindakanId: string | null
  billingLastError: string | null
}
```

### Validation / Invariant

- **Not** from Deferred
- Only from **Ordered** without existing `billingTindakanId`
- On failure: order stays Ordered; `billingLastError` persisted (max 200 chars)

### Query Invalidations

- worklist, order detail, collection preparation

### Suggested Frontend Query Key

```ts
['lab-order-detail', orderId]
['lab-order-worklist']
```

---

## Collect Specimen

### Route

```http
PATCH /api/LabContext/LabOrderFeature/collect
```

### Purpose

**Charged → Collected**; records collection metadata.

### Frontend Usage

- Collection confirmation modal

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001",
  "collectionNote": "Lengan kanan",
  "collectedDate": "2026-05-19T09:30:00"
}
```

```ts
interface LabOrderCollectSpecimenRequest {
  orderId: string
  userId: string
  collectionNote?: string | null
  collectedDate?: string | null // defaults to server Now if omitted
}
```

### Response Payload

`data: "Done"`

### Validation / Invariant

- Only from **Charged (3)**
- `collectedDate` required internally (defaults to `DateTime.Now`)

### Query Invalidations

- order detail, worklist, collection preparation

### Suggested Frontend Query Key

```ts
['lab-order-detail', orderId]
['lab-collection-preparation', orderId]
```

---

## Release Order

### Route

```http
PATCH /api/LabContext/LabOrderFeature/release
```

### Purpose

Administrative release: **Verified → Released** after realtime BIL validation.

### Frontend Usage

- Release modal (`releaseNote`) on verified orders
- On **BLOCKED**: HTTP **200** — show operational toast with `message`; **do not** treat as API error; keep workspace active
- No separate financial approval screen

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001",
  "releaseNote": "Diserahkan ke pasien"
}
```

```ts
interface LabOrderReleaseRequest {
  orderId: string
  userId: string
  releaseNote: string // may be empty string; required property
}
```

### Response Payload

**Canonical — release succeeded (`CLEAR`):**

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "released": true,
    "billingStatus": "CLEAR",
    "message": ""
  }
}
```

**Canonical — release blocked by BIL (`BLOCKED`, operational outcome):**

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "released": false,
    "billingStatus": "BLOCKED",
    "message": "Tagihan pasien belum memenuhi syarat release."
  }
}
```

```ts
interface LabOrderReleaseResponse {
  released: boolean
  billingStatus: 'CLEAR' | 'BLOCKED'
  message: string
}
```

> **LOCK:** `BLOCKED` is operational business outcome, not transport failure. Always HTTP **200** for BIL denial. Frontend must **not** route `BLOCKED` through global exception handlers.

HTTP **400** applies only to **invalid release attempts** (e.g. order not Verified, already Released) — **not** when BIL returns `BLOCKED`.

### Validation / Invariant

- Order must be **Verified (6)**; not already **Released**
- Handler calls BIL `ValidateReleaseEligibility` synchronously on every release attempt
- On `CLEAR`: **Verified → Released**; append validation trace row; `released: true`
- On `BLOCKED`: no status change; append validation trace row; **HTTP 200** + `released: false` + `billingStatus: BLOCKED` + `message`
- After amend + re-verify: release calls BIL again — no cached approval
- `releaseNote` max 200 chars

### Query Invalidations

- release worklist, order detail, result views

### Suggested Frontend Query Key

```ts
['lab-release-worklist']
['lab-order-detail', orderId]
```

---

## Cancel Order

### Route

```http
PATCH /api/LabContext/LabOrderFeature/cancel
```

### Purpose

Cancel before collection: **Ordered / Deferred / Charged → Cancelled**.

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001",
  "reason": "Salah order"
}
```

```ts
interface LabOrderCancelRequest {
  orderId: string
  userId: string
  reason: string
}
```

### Response Payload

`data: "Done"`

### Validation / Invariant

- Not after Collected
- Reason max 200 chars

### Query Invalidations

- worklist, order detail

### Suggested Frontend Query Key

```ts
['lab-order-worklist']
['lab-order-detail', orderId]
```

---

## Terminate Order

### Route

```http
PATCH /api/LabContext/LabOrderFeature/terminate
```

### Purpose

Stop in-flight workflow: **Collected / Recorded → Terminated**.

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001",
  "reason": "Pasien tidak kooperatif"
}
```

```ts
interface LabOrderTerminateRequest {
  orderId: string
  userId: string
  reason: string
}
```

### Response Payload

`data: "Done"`

### Validation / Invariant

- Only Collected or Recorded
- Reason max 200 chars

### Query Invalidations

- worklist, order detail, result worklist

### Suggested Frontend Query Key

```ts
['lab-order-worklist']
['lab-order-detail', orderId]
```

---

# LabResultFeature

**Route prefix:** `/api/LabContext/LabResultFeature`  
**Total LabResult endpoints:** 6

---

## Record Result

### Route

```http
POST /api/LabContext/LabResultFeature/record
```

### Purpose

Create/update current result document; set order **Recorded**.

### Frontend Usage

- Result entry form (grid of components)
- Re-save while in Recorded state

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "LABTECH01",
  "resultSource": 1,
  "items": [
    {
      "testId": "T-HB",
      "testName": "Hemoglobin",
      "componentCode": "",
      "componentName": "",
      "resultType": 1,
      "numericValue": 13.0,
      "textValue": null,
      "optionValue": null,
      "narrativeValue": null,
      "unit": "g/dL",
      "referenceRangeText": "12-16"
    }
  ]
}
```

```ts
interface LabResultRecordRequest {
  orderId: string
  userId: string
  resultSource: number // LabResultSourceEnum
  items: LabResultRecordItemDto[]
}

interface LabResultRecordItemDto {
  testId: string
  testName: string
  componentCode: string
  componentName: string
  resultType: number // LabResultTypeEnum
  numericValue: number
  textValue?: string | null
  optionValue?: string | null
  narrativeValue?: string | null
  unit?: string | null
  referenceRangeText?: string | null
}
```

### Response Payload

`data: "Done"`

### Validation / Invariant

- Order: **Charged, Collected, or Recorded**
- `items` non-empty
- Valid `resultSource` and each `resultType`
- Replaces all result items; auto-computes `flagStatus` for numeric types
- Cannot record on **Verified** document

### Query Invalidations

- `['lab-result', orderId]`
- `['lab-order-detail', orderId]`
- `['lab-result-verification-worklist']`

### Suggested Frontend Query Key

```ts
['lab-result', orderId]
['lab-result-verification-worklist']
```

---

## Verify Result

### Route

```http
PATCH /api/LabContext/LabResultFeature/verify
```

### Purpose

Pathologist verification: result **Recorded → Verified**, order **Recorded → Verified**.

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "verifiedUserId": "DR-PK-01",
  "verifiedDate": "2026-05-19T14:00:00"
}
```

```ts
interface LabResultVerifyRequest {
  orderId: string
  verifiedUserId: string
  verifiedDate: string // must be valid; not default or 3000-01-01
}
```

### Response Payload

`data: "Done"`

### Validation / Invariant

- Order **Recorded**
- Document exists and not already Verified
- `verifiedDate` required (valid calendar date)

### Query Invalidations

- result detail, verification worklist, order detail, release worklist pipeline

### Suggested Frontend Query Key

```ts
['lab-result', orderId]
['lab-result-verification-worklist']
['lab-order-detail', orderId]
```

---

## Verification Worklist

### Route

```http
GET /api/LabContext/LabResultFeature/worklist/verification?searchTerm={string?}&date1={datetime?}&date2={datetime?}
```

### Purpose

Queue of results awaiting verification (order Recorded + result Recorded).

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": [
    {
      "orderId": "LBO000000001",
      "orderNo": "LAB00000001",
      "patientId": "MR0001",
      "patientName": "Pasien Tes",
      "recordedDate": "2026-05-19T11:00:00",
      "resultStatus": 2,
      "recordedUserId": "LABTECH01"
    }
  ]
}
```

```ts
interface LabResultVerificationWorklistView {
  orderId: string
  orderNo: string
  patientId: string
  patientName: string
  recordedDate: string
  resultStatus: number
  recordedUserId: string
}
```

### Validation / Invariant

- Current result version only
- Date filter on `recordedDate` when both dates provided

### Suggested Frontend Query Key

```ts
['lab-result-verification-worklist', { searchTerm, date1, date2 }]
```

---

## Get Result

### Route

```http
GET /api/LabContext/LabResultFeature/{orderId}
```

### Purpose

Current result document projection for entry/review UI.

### Response Payload (has document)

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "hasDocument": true,
    "resultDocumentId": "LRD000000001",
    "orderId": "LBO000000001",
    "versionNo": 1,
    "isCurrentVersion": true,
    "resultSource": 1,
    "resultStatus": 2,
    "recordedDate": "2026-05-19T11:00:00",
    "recordedUserId": "LABTECH01",
    "verifiedDate": "3000-01-01T00:00:00",
    "verifiedUserId": "",
    "items": [
      {
        "itemNo": 1,
        "testId": "T-HB",
        "testName": "Hemoglobin",
        "componentCode": "",
        "componentName": "",
        "resultType": 1,
        "numericValue": 13.0,
        "textValue": "",
        "optionValue": "",
        "narrativeValue": "",
        "unit": "g/dL",
        "referenceRangeText": "12-16",
        "flagStatus": 1
      }
    ]
  }
}
```

Empty (no document yet):

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "hasDocument": false,
    "resultDocumentId": "",
    "orderId": "LBO000000001",
    "versionNo": 0,
    "isCurrentVersion": false,
    "resultSource": 0,
    "resultStatus": 0,
    "recordedDate": "3000-01-01T00:00:00",
    "recordedUserId": "",
    "verifiedDate": "3000-01-01T00:00:00",
    "verifiedUserId": "",
    "items": []
  }
}
```

```ts
interface LabResultView {
  hasDocument: boolean
  resultDocumentId: string
  orderId: string
  versionNo: number
  isCurrentVersion: boolean
  resultSource: number
  resultStatus: number
  recordedDate: string
  recordedUserId: string
  verifiedDate: string
  verifiedUserId: string
  items: LabResultItemView[]
}

interface LabResultItemView {
  itemNo: number
  testId: string
  testName: string
  componentCode: string
  componentName: string
  resultType: number
  numericValue: number
  textValue: string
  optionValue: string
  narrativeValue: string
  unit: string
  referenceRangeText: string
  flagStatus: number
}
```

### Suggested Frontend Query Key

```ts
['lab-result', orderId]
```

---

## Amend Result

### Route

```http
PATCH /api/LabContext/LabResultFeature/amend
```

### Purpose

Retire verified version; create new version in **Recorded**; order returns to **Recorded**.

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "reason": "Kesalahan input nilai",
  "amendedBy": "DR-PK-01"
}
```

```ts
interface LabResultAmendRequest {
  orderId: string
  reason: string
  amendedBy: string
}
```

### Response Payload

`data: "Done"` — **does not return** new `resultDocumentId` / `versionNo`

### Validation / Invariant

- Order **Verified** or **Released**
- Current document **Verified**
- After amend: must **re-verify**; release again runs realtime BIL validation (no stored clearance state)

### Query Invalidations

- all result + order queries for `orderId`

### Suggested Frontend Query Key

```ts
['lab-result', orderId]
['lab-order-detail', orderId]
```

---

## Download Result PDF

### Route

```http
GET /api/LabContext/LabResultFeature/{orderId}/pdf
```

### Purpose

On-demand PDF for current verified (or operational) result — binary download.

### Frontend Usage

- Print / download button
- Use `fetch` + blob; not TanStack Query JSON parser

### Response Payload

- **Not JSend**
- Headers: `Content-Type` from handler, `Content-Disposition` filename from renderer
- Requires order + current result document (handler validates)

### Suggested Frontend Query Key

```ts
// Prefer imperative download, not cached JSON query:
['lab-result-pdf', orderId] // metadata only if needed
```

---

# LabOwareFeature

**Route prefix:** `/api/LabContext/LabOwareFeature`  
**Total LabOware endpoints:** 4

---

## Enqueue OWARE

### Route

```http
POST /api/LabContext/LabOwareFeature/enqueue
```

### Purpose

Build outbound payload, create queue row **Pending**, set order `owareStatus` Pending.

### Request Payload

```json
{
  "orderId": "LBO000000001",
  "userId": "USR001"
}
```

```ts
interface LabOwareQueueEnqueueRequest {
  orderId: string
  userId: string
}
```

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "queueId": "LOQ000000001"
  }
}
```

```ts
interface LabOwareQueueEnqueueResponse {
  queueId: string
}
```

### Validation / Invariant

- Order must be **Charged or later** (not Ordered/Deferred/Cancelled/Terminated)
- Payload built server-side (`LabOwarePayloadBuilder`) — frontend does not send payload JSON

### Query Invalidations

- `['lab-oware-worklist']`
- `['lab-order-detail', orderId]`

### Suggested Frontend Query Key

```ts
['lab-oware-worklist']
['lab-order-detail', orderId]
```

---

## Retry OWARE Queue Item

### Route

```http
PATCH /api/LabContext/LabOwareFeature/retry
```

### Purpose

Manually process one **Failed** queue item.

### Request Payload

```json
{
  "queueId": "LOQ000000001",
  "userId": "USR001"
}
```

```ts
interface LabOwareRetryRequest {
  queueId: string
  userId: string
}
```

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "success": true,
    "errorMessage": null
  }
}
```

```ts
interface LabOwareRetryResponse {
  success: boolean
  errorMessage: string | null
}
```

### Validation / Invariant

- Queue status must be **Failed (3)**

### Query Invalidations

- oware worklist, order detail (`owareStatus`)

### Suggested Frontend Query Key

```ts
['lab-oware-worklist']
['lab-order-detail', orderId]
```

---

## Process OWARE Batch

### Route

```http
POST /api/LabContext/LabOwareFeature/process?batchSize={int?}&userId={string?}
```

### Purpose

Worker/cron: process up to N pending queue items (default batch 20).

### Frontend Usage

- Admin/ops tool or backend scheduler — typically not end-user UI

### Request Payload

Query: `batchSize` (default 20 if null/<1), `userId` (defaults to `OWARE-WORKER`).

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": {
    "processedCount": 5,
    "succeededCount": 4,
    "failedCount": 1
  }
}
```

```ts
interface LabOwareProcessBatchResult {
  processedCount: number
  succeededCount: number
  failedCount: number
}
```

### Validation / Invariant

- Updates queue + order `owareStatus` per item

### Suggested Frontend Query Key

```ts
['lab-oware-worklist']
```

---

## OWARE Worklist

### Route

```http
GET /api/LabContext/LabOwareFeature/worklist?queueStatus={int?}&searchTerm={string?}&date1={datetime?}&date2={datetime?}
```

### Purpose

Monitor outbound integration queue.

### Response Payload

```json
{
  "statusCode": 200,
  "status": "success",
  "data": [
    {
      "queueId": "LOQ000000001",
      "orderNo": "LAB00000001",
      "queueStatus": 0,
      "retryCount": 0,
      "lastError": "",
      "crtDate": "2026-05-19T08:00:00",
      "processedDate": "3000-01-01T00:00:00"
    }
  ]
}
```

```ts
interface LabOwareQueueWorklistView {
  queueId: string
  orderNo: string
  queueStatus: number
  retryCount: number
  lastError: string
  crtDate: string
  processedDate: string
}
```

### Validation / Invariant

- `searchTerm` only matches **orderNo** (not patient name)
- Date filter on queue `crtDate`

### Suggested Frontend Query Key

```ts
['lab-oware-worklist', { queueStatus, searchTerm, date1, date2 }]
```

---

# 4. Worklist Queries

| Worklist | Endpoint | Default filter (server) | Client filters |
|----------|----------|-------------------------|----------------|
| Order | `GET LabOrderFeature/worklist` | Non-void orders | `labOrderStatus`, `searchTerm`, `date1`+`date2` |
| Release | `GET LabOrderFeature/releaseWorklist` | Verified, not released | search, verified date range |
| Verification | `GET LabResultFeature/worklist/verification` | Order Recorded + result Recorded + current version | search, recorded date range |
| OWARE | `GET LabOwareFeature/worklist` | All queue rows | `queueStatus`, search (orderNo only), crt date range |
| Collection prep | `GET LabOrderFeature/collectionPreparation` | N/A (single order) | `orderId` |

### Projection field usage

| Field | UI use |
|-------|--------|
| `testNames` | Order worklist subtitle |
| `billingTindakanId` | Charge confirmation / billing link |
| `lastBillingReleaseCheck` | Read-only audit on order detail (optional) |
| `owareStatus` | Integration badge on detail |
| `itemCount` | Worklist column |
| `retryCount` / `lastError` | OWARE failure diagnostics |

### Operational screen mapping

| Screen | Worklist API | Status filter hint |
|--------|--------------|-------------------|
| New orders | order worklist | `labOrderStatus=1` |
| Deferred | order worklist | `labOrderStatus=2` |
| Awaiting charge | order worklist | `labOrderStatus=1` (Ordered, not charged) — also check `billingTindakanId` empty |
| Collection | order worklist | `labOrderStatus=3` |
| Result entry | order worklist | `3,4,5` or dedicated filter |
| Verification | verification worklist | (server pre-filtered) |
| Release | release worklist | Verified orders awaiting release |
| OWARE | oware worklist | `queueStatus=3` for failures |

---

# 5. DTO Discovery

## Request DTOs (commands)

| Type | Endpoint |
|------|----------|
| `LabOrderCreateFromEmrCmd` | POST fromEmr |
| `LabOrderCreateExternalPatientCmd` | POST external |
| `LabOrderDeferCmd` | PATCH defer |
| `LabOrderActivateDeferredCmd` | PATCH activateDeferred |
| `LabOrderChargeCmd` | PATCH charge |
| `LabOrderCollectSpecimenCmd` | PATCH collect |
| `LabOrderReleaseCmd` | PATCH release |
| `LabOrderCancelCmd` | PATCH cancel |
| `LabOrderTerminateCmd` | PATCH terminate |
| `LabResultRecordCmd` + `LabResultRecordItemDto` | POST record |
| `LabResultVerifyCmd` | PATCH verify |
| `LabResultAmendCmd` | PATCH amend |
| `LabOwareQueueEnqueueCmd` | POST enqueue |
| `LabOwareRetryCmd` | PATCH retry |

## Response DTOs (structured `data`)

| Type | Endpoint |
|------|----------|
| `LabOrderCreateResponse` | create |
| `LabOrderActivateDeferredResponse` | activateDeferred |
| `LabOrderChargeResponse` | charge |
| `LabOrderReleaseResponse` | release |
| `LabOrderGetResponse` + `LabOrderItemResponse` | GET order |
| `LabOrderWorklistView` | worklist |
| `LabReleaseView` | releaseWorklist |
| `LabCollectionPreparationView` + children | collectionPreparation |
| `LabResultView` + `LabResultItemView` | GET result |
| `LabResultVerificationWorklistView` | verification worklist |
| `LabOwareQueueEnqueueResponse` | enqueue |
| `LabOwareRetryResponse` | retry |
| `LabOwareProcessBatchResult` | process |
| `LabOwareQueueWorklistView` | oware worklist |

## View / projection DTOs (read models only)

Listed above in worklist and GET sections.

## Shared nested types

| Type | Usage |
|------|--------|
| `LabOrderItemInput` | Create order items |
| `LabOrderItemResponse` | Order detail items |

## Internal / not exposed via API

| Type | Note |
|------|------|
| `LabOwareOutboundPayload` | Built server-side for queue |
| `LabResultPdfView` | Used inside PDF renderer, not returned as JSON |

---

# 6. Enum Catalog

## LabOrderStatusEnum

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 1 | Ordered | Dipesan |
| 2 | Deferred | Ditunda |
| 3 | Charged | Dicharge |
| 4 | Collected | Spesimen diambil |
| 5 | Recorded | Hasil direkam |
| 6 | Verified | Diverifikasi |
| 7 | Released | Dirilis |
| 8 | Cancelled | Dibatalkan |
| 9 | Terminated | Dihentikan |

## BillingStatus (BIL response in `PATCH release`, not LWF workflow enum)

| Value | Meaning | HTTP | UI |
|-------|---------|------|-----|
| `CLEAR` | Release allowed | 200 | Success; `released: true` |
| `BLOCKED` | Release denied (operational) | **200** | Toast with `message`; `released: false`; **not** exception flow |

Persisted on LWF as audit trace only (`lastBillingReleaseCheck` / history table) — not workflow state. No pre-approval field on order.

## OwareStatusEnum (on order)

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 0 | Pending | OWARE menunggu |
| 1 | Sent | OWARE terkirim |
| 2 | Failed | OWARE gagal |

## LabOrderSourceEnum

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 1 | Emr | EMR |
| 2 | ExternalPatient | Pasien luar |

## VacutainerTypeEnum

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 1 | Edta | EDTA |
| 2 | Serum | Serum |
| 3 | Citrate | Citrate |
| 4 | Heparin | Heparin |

## LabResultStatusEnum

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 1 | Draft | Draft |
| 2 | Recorded | Direkam |
| 3 | Verified | Diverifikasi |

## LabResultSourceEnum

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 1 | Manual | Manual |
| 2 | Instrument | Alat |
| 3 | ExternalLis | LIS eksternal |

## LabResultTypeEnum

| Value | Name | UI control |
|-------|------|------------|
| 1 | Numeric | Number input |
| 2 | Text | Text input |
| 3 | Option | Select / radio |
| 4 | Narrative | Textarea |

## LabResultFlagEnum

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 1 | Normal | Normal (green) |
| 2 | High | Tinggi (red) |
| 3 | Low | Rendah (blue) |

Auto-flag only for **Numeric** with parseable `referenceRangeText`.

## LabOwareQueueStatusEnum

| Value | Name | Display suggestion |
|-------|------|-------------------|
| 0 | Pending | Antrian |
| 1 | Processing | Memproses |
| 2 | Succeeded | Berhasil |
| 3 | Failed | Gagal |

---

# 7. Frontend Screen Mapping

| Screen | APIs | Notes |
|--------|------|-------|
| Order worklist | `GET worklist` | Tabs by `labOrderStatus` |
| Create EMR order | `POST fromEmr` | May be EMR-hosted |
| Create external order | `POST external` | Full form + items |
| Order detail | `GET {orderId}`, `GET LabResult/{orderId}` | Action bar from status |
| Defer | `PATCH defer` | Modal |
| Activate deferred | `PATCH activateDeferred` | Shows `executionRegId` |
| Charge | `PATCH charge` | Handle `success: false` |
| Collection prep panel | `GET collectionPreparation` | Before collect |
| Collect | `PATCH collect` | Date + note |
| OWARE send | `POST enqueue` | After charged |
| OWARE monitor | `GET LabOware/worklist`, `PATCH retry` | Admin |
| Result entry | `GET result`, `POST record` | Dynamic grid by `resultType` |
| Verification list | `GET worklist/verification` | |
| Verify action | `PATCH verify` | Date + pathologist id |
| Release list | `GET releaseWorklist` | Verified, not released |
| Release action | `PATCH release` | Realtime BIL validation; handle BLOCKED toast |
| Cancel | `PATCH cancel` | Reason modal |
| Terminate | `PATCH terminate` | Reason modal |
| Amend | `PATCH amend` | Then re-record + re-verify |
| PDF print | `GET {orderId}/pdf` | Blob download |

---

# 8. Frontend Form Discovery

| Form | Fields | Validation |
|------|--------|------------|
| Create external order | patientName, optional reg/patient/birth/gender, items[] | items required; birth `yyyy-MM-dd` |
| Create item row | test*, tarif*, tubeType, specimenType, requiredTubeCount | tubeType 1–4 |
| Defer | reason, untilDate | reason required |
| Charge | (confirm only) | confirm Ordered, not deferred |
| Collect | collectedDate, collectionNote | status Charged |
| Record result | resultSource, items[] per type | non-empty items; numeric fields |
| Verify | verifiedUserId, verifiedDate | valid date |
| Release | releaseNote | Verified; handle BLOCKED in response |
| Cancel / terminate | reason | required, max 200 |
| Amend | reason | required |
| OWARE enqueue | (confirm orderId) | status ≥ Charged |

### Modal / dialog candidates

- Defer, Collect, Cancel, Terminate, Release, Amend, Charge retry, OWARE retry

### Batch operation candidates

- OWARE `POST process` (ops only)
- Worklist multi-select **not supported** by API — loop single-order commands if needed

---

# 9. Suggested Frontend Query Architecture

Pragmatic alignment with TanStack Query + body-command services:

```
features/lab/
  api/
    labOrderService.ts      # POST/PATCH/GET wrappers
    labResultService.ts
    labOwareService.ts
  queries/
    useLabOrderWorklist.ts
    useLabOrderDetail.ts
    useLabResult.ts
    ...
  composables/
    useLabOrderActions.ts   # mutations + invalidate helpers
```

### Query ownership

| Query key prefix | Owner hook | Stale on |
|------------------|------------|----------|
| `lab-order-worklist` | worklist screens | any order mutation |
| `lab-order-detail` | detail panel | order/result/release |
| `lab-collection-preparation` | collection UI | collect, charge |
| `lab-result` | result panel | record, verify, amend |
| `lab-result-verification-worklist` | verification screen | record, verify |
| `lab-release-worklist` | release screen | release |
| `lab-oware-worklist` | integration monitor | enqueue, retry, process |

### Mutation pattern

```ts
// After successful PATCH/POST:
queryClient.invalidateQueries({ queryKey: ['lab-order-detail', orderId] })
queryClient.invalidateQueries({ queryKey: ['lab-order-worklist'] })
```

### Service layer

- One function per endpoint; accept typed request objects matching backend records exactly.
- Map auth user → `userId` in service (single place).
- Treat charge `success: false` as business result, not HTTP error.

Do **not** over-abstract query keys — a flat `lab-*` namespace is sufficient for LWF scope.

---

# 10. Missing/Unclear Contracts

| Issue | Severity | Detail |
|-------|----------|--------|
| Release BLOCKED UX | Low | **LOCKED:** HTTP 200 + toast on `message`; same pattern as charge soft-failure — never exception handler |
| Full billing release check history API | Low | V1 may expose only `lastBillingReleaseCheck` on order GET |
| `PATCH amend` returns `"Done"` only | Medium | Frontend must refetch result to get new `versionNo` / `resultDocumentId` |
| No result version history API | Medium | Only current version in `GET result` |
| `collectionPreparation` not found → 500 | Medium | Throws bare `Exception`, not `KeyNotFoundException` |
| `IMPLEMENTATION_PLAN` lists `POST emr/cancel` | Low | **Not implemented** in `LabOrderController` |
| OWARE plan routes vs actual | Low | Actual: `enqueue`, `retry`, `process`, `worklist` |
| PDF not JSend | Low | Separate download handling |
| Many mutations return `"Done"` | Low | No updated entity in response — always refetch detail |
| `userId` in every body | Medium | Must wire from auth context consistently |
| Record allowed from **Charged** without collect | Medium | UI may still enforce collect-first |
| OWARE worklist search | Low | OrderNo only |
| Date filters require **both** dates | Medium | Single-date filter not supported |
| Empty date sentinel `3000-01-01` | Medium | UI formatting helpers needed |
| Error HTTP 400 for not found | Low | `GetValueOrThrow` message in `data` string |
| `LabResultView` omits amendment metadata | Low | `amendmentReason` not in GET projection |
| No OpenAPI examples in repo | Low | Use this document + Scalar at `/openapi` |

---

## Discovery summary

| Metric | Count |
|--------|------:|
| **Total endpoints discovered** | **23** |
| LabOrderFeature | 13 |
| LabResultFeature | 6 |
| LabOwareFeature | 4 |

### Missing contracts (documented above)

- Billing release validation history list endpoint (optional V2)
- Result version history list
- EMR cancel endpoint (planned only)
- Amend response metadata

### Inconsistent contracts

- Error shape vs HTTP status (400 for not found)
- Charge uses 200 + `success` flag vs exceptions for other state errors
- PDF vs JSend JSON
- `"Done"` string vs structured DTO responses

### Frontend risk areas

1. ~~BLOCKED as 400~~ — **locked to HTTP 200**; ensure mutation hooks check `released` / `billingStatus`  
2. Sentinel dates breaking date pickers  
3. Charge failure UX (soft failure in 200 response)  
4. Amend → refetch → re-verify → release (BIL validation again)  
5. Explicit `userId` wiring / audit consistency  
6. Collection preparation 500 on bad id  

### Recommended frontend implementation order

1. Order worklist + detail (`GET worklist`, `GET {orderId}`)  
2. Create external + EMR hook (if lab UI creates orders)  
3. Defer + activate deferred  
4. Charge + billing error display  
5. Collection preparation + collect  
6. Result GET + record form  
7. Verification worklist + verify  
8. Release worklist + release (realtime BIL validation, BLOCKED handling)  
9. Cancel + terminate  
10. PDF download  
11. OWARE enqueue + worklist + retry  
12. Amend flow (advanced)  

---

*Generated from backend implementation analysis. Controllers: `Bilreg.Api/Controllers/LabContext/`.*
