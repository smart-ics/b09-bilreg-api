# 06 — Tata Rekening Frontend Implementation Plan

**Version:** 1.0  
**Status:** Planning  
**Audience:** Frontend developers and AI agents implementing the Tata Rekening UI  
**Source of Truth:** `01-context.md`, `02-domain.md`, `03-design.md`, `04-sop.md`, `SOP-TR-01` … `SOP-TR-10`, `tata-rekening-api-contract.md`, `swagger.json`, `frontend-integration-guide.md`  
**Frontend Repository:** `c012_myhospital_web`  
**Backend API:** `b09-bilreg-api` — `/api/tatarekening/*` (Phase 5 complete per `frontend-readiness-checklist.md`)

---

## 1. Objective

The Tata Rekening frontend is the **Financial Control workspace** for Verifikator staff. Its purpose is to guide a single patient registration through the full Financial Responsibility lifecycle — from operational freeze through allocation and finalization to settlement handoff — while remaining a **thin client** that defers all business rules to the backend.

The frontend must:

- Load and display the current Financial Truth for a registration (`GET /api/tatarekening/{regId}`).
- Expose SOP-aligned actions (Close, Merge, Verify, Adjust, Allocate, Finalize, Cancel Finalization, Reopen, Settlement Initiation) gated by aggregate status returned from the API.
- Never compute lifecycle transitions, allocation math, or projection regeneration locally.
- Support incremental delivery: each implementation phase produces a usable, testable slice aligned with one SOP.
- Replace the existing mock-driven `RegistrasiKeluar.vue` prototype with API-backed behavior.

**Out of scope for this module:**

- Payment Settlement (Cashier bounded context).
- Merge Request creation (external to this API).
- Charge Source operational workflows.
- Accounting journal display.

---

## 2. Guiding Principles

| Principle | Application in Tata Rekening |
|-----------|------------------------------|
| **Actor-Centric UI** | Primary actor is **Verifikator**. Actions, confirmations, and audit reason fields match SOP responsibilities. Write actions require `TATA-REKENING-WRITE` permission. |
| **Thin Client** | UI renders API DTOs; no local simulation of Close, Merge, Allocate, or Finalize. Client-side validation is limited to form completeness (required reason, non-empty allocation rows). |
| **Backend Is Source of Truth** | `TataRekeningSummaryDto` drives lifecycle badges, enabled actions, and read-only vs editable panels. After every successful mutation, update from `data.summary` (and `data.projection` when returned). |
| **Stateless Page** | No long-lived Pinia store for billing data. TanStack Query holds server state; `regId` is the page key. Navigating away and back refetches Open. |
| **Optimistic Refresh** | Do **not** use optimistic updates for financial mutations. On success: patch query cache from response. On **409**: full reload via GET Open and prompt retry. |
| **Responsive Layout** | Single workspace adapts: patient/summary header fixed; scrollable billing and allocation panels; sticky action toolbar on desktop; collapsible sections on narrow viewports. |
| **Declarative State** | Use `computed()` for action availability from `summary.status` + `financialVerificationStatus` + flags. Use `watch(regId)` to trigger Open fetch. Event handlers set source state only (e.g. open dialog); watchers/mutations handle downstream effects. |
| **SOP Sequencing** | Normal path follows SOP-TR-01 → TR-10. Exception paths (Reopen, Cancel Finalization) are first-class UI flows with explicit user guidance on what to do next. |
| **Compatibility First** | Reuse existing layout patterns from `RegistrasiKeluar.vue` where they align (Jasa/Obat split, allocation grid). Replace mock types with API Zod schemas. |

---

## 3. Screen Overview

### Screens Required

| Screen | Purpose | Route / Entry |
|--------|---------|---------------|
| **Registration Queue** | Search/select active registration to process | Module tab landing (left panel or search overlay) |
| **Tata Rekening Workspace** | Full Financial Control for one `regId` | `/app/tata_rekening` with selected `regId` (query param or workspace state) |
| **Merge Confirmation** | Preview and confirm merge execution | Dialog on workspace |
| **Financial Adjustment** | Form for waive/subsidy/correction/manual charge | Dialog on workspace |
| **Reason Capture** | Reopen / Cancel Finalization audit reason | Dialog on workspace |
| **Settlement Handoff** | Confirm handoff to Cashier | Dialog on workspace |

### Layout Recommendation: **Single Workspace + Tab Layout**

**Recommendation:** Keep Tata Rekening as a **single workspace** within the existing HIS **tab layout** (`HISModule` → one tab `tata_rekening`), not a multi-page app or wizard.

**Reasoning:**

1. **Registration-centric aggregate** — All SOP steps operate on one `regId`. A wizard would force artificial step transitions when Verifikator often needs to review bills while adjusting allocation.
2. **Status-driven visibility** — `summary.status` and `financialVerificationStatus` naturally show/hide action buttons and panels without separate routes.
3. **Exception paths** — Reopen and Cancel Finalization branch back into earlier phases; a linear wizard handles this poorly.
4. **Existing shell** — `RegistrasiKeluar.vue` already uses a three-column workspace (queue | content | actions). Evolve this pattern rather than replace routing architecture.
5. **HIS convention** — Other modules (Admisi, Outpatient) use tab-based workspace navigation at `/app/:screen?/:tab?`.

**Not recommended:**

- **Multi-page** — Unnecessary route churn; loses context when switching between Verify and Allocate.
- **Wizard** — Implies strict linear progression; conflicts with optional Merge/Adjust and exception loops.

**Optional future enhancement:** Deep-link `?regId=REG-001` for opening workspace directly from Admisi or discharge lists.

---

## 4. Screen Layout

High-level vertical layout for the **Tata Rekening Workspace** (main screen):

```text
┌─────────────────────────────────────────────────────────────────┐
│  Header Bar                                                     │
│  Module title · Reg ID · Lifecycle Status Badge · Refresh       │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────┬──────────────────────────────────────────────┐
│ Registration     │  Patient Summary Card                      │
│ Queue / Search   │  Name · MR · Care type · Insurance · Bed     │
│ (collapsible     ├──────────────────────────────────────────────┤
│  on mobile)      │  Tata Rekening Summary Card                  │
│                  │  status · verification · allocated · settled │
│                  ├──────────────────────────────────────────────┤
│                  │  Merge Request Panel (if pendingMergeRequests) │
│                  ├──────────────────────────────────────────────┤
│                  │  Billing Summary (Jasa / Obat totals)        │
│                  ├──────────────────────────────────────────────┤
│                  │  Billing Detail Grid (TrsBill line items)    │
│                  ├──────────────────────────────────────────────┤
│                  │  Financial Projection Grid (post-allocate)   │
│                  ├──────────────────────────────────────────────┤
│                  │  Financial Responsibility Editor             │
│                  │  (allocation grid — editable when allowed)   │
└──────────────────┴──────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│  Action Toolbar (sticky)                                        │
│  Close · Merge · Verify · Adjust · Allocate · Finalize ·        │
│  Cancel Finalization · Reopen · Settlement Initiation             │
└─────────────────────────────────────────────────────────────────┘
```

**Panel visibility rules (derived from API, not hardcoded step):**

| Panel | Visible when |
|-------|----------------|
| Merge Request Panel | `pendingMergeRequests.length > 0` and `status === Closed` |
| Billing Detail Grid | Always after Open |
| Projection Grid | `projection.length > 0` or `isFinancialResponsibilityAllocated` |
| Allocation Editor | `status === Closed` and verification allows allocation |
| Action Toolbar | Always; individual buttons gated by status (see Section 7) |

**Registration Queue (left column):**

```text
Search input (registration no / patient name)
        ↓
Filter chips (care type, status)
        ↓
Virtual scroll list of registrations
        ↓
Select → sets regId → triggers GET Open
```

Registration list data source is **outside** Tata Rekening Open API — use existing `RegistrasiService` (active registrations) until a dedicated dischargeable-queue endpoint exists.

---

## 5. Frontend Phases

Implementation is divided into **10 phases**, one per SOP. Each phase builds on the previous. Phases 8–9 are exception paths but delivered as dedicated increments for testability.

**Cumulative artifact estimates** (new items per phase; refactor of existing prototype counted where noted):

| Phase | Vue Components | Dialogs | Composables | API Service Methods | Pinia Stores |
|-------|----------------|---------|-------------|---------------------|--------------|
| 1 — Open | 6 | 1 | 3 | 2 (open + keys) | 0 |
| 2 — Close | 1 | 1 | 1 | 1 | 0 |
| 3 — Merge | 2 | 1 | 1 | 1 | 0 |
| 4 — Verify | 2 | 1 | 1 | 1 | 0 |
| 5 — Adjust | 2 | 1 | 1 | 1 | 0 |
| 6 — Allocate | 2 (refactor) | 1 | 2 | 1 | 0 |
| 7 — Finalize | 1 | 1 | 1 | 1 | 0 |
| 8 — Cancel Finalization | 0 | 1 | 0 | 1 | 0 |
| 9 — Reopen | 0 | 1 | 0 | 1 | 0 |
| 10 — Settlement | 1 | 1 | 1 | 1 | 0 |
| **Shared / cross-phase** | 4 | 0 | 2 | 0 | 0 |
| **Total (unique)** | **~17** | **~9** | **~10** | **1 service (~10 methods)** | **0** |

---

### Phase 1 — Open Tata Rekening (SOP-TR-01)

**Goal:** Replace mock data with live Open API; establish workspace shell and registration selection.

**Scope:**

- Create `TataRekeningService` with `useOpenTataRekening(regId)` and `fetchOpenTataRekening(regId)`.
- Define Zod schemas for `OpenTataRekeningApiResponse`, `TataRekeningSummaryDto`, `TrsBillSummaryDto`, `PaymentProjectionDto`, `MergeRequestSummaryDto` in `types/`.
- Refactor `RegistrasiKeluar.vue` → `TataRekeningWorkspace.vue` (or evolve in place).
- Registration queue: integrate `RegistrasiService` for search/list; on select set `regId`.
- Display: Summary Card, Billing Grid (read-only), empty Projection panel, Merge Request panel (read-only list).
- Loading, error (404), and empty states.

**API used:**

| Action | Endpoint |
|--------|----------|
| Open workspace | `GET /api/tatarekening/{regId}` |

**UI Components:**

| Component | Role |
|-----------|------|
| `TataRekeningWorkspace.vue` | Main shell (refactor from `RegistrasiKeluar.vue`) |
| `RegistrationQueuePanel.vue` | Search + virtual list |
| `TataRekeningSummaryCard.vue` | Status, verification, flags |
| `BillingGrid.vue` | Refactor from `BillTable.vue` — map `TrsBillSummaryDto` |
| `BillingSummaryCard.vue` | Refactor from `BillSummaryTable.vue` — derive Jasa/Obat totals from `bills` |
| `MergeRequestPanel.vue` | List pending merges (read-only in Phase 1) |
| `ProjectionGrid.vue` | Display `projection[]` (read-only) |
| `RegistrationSearchDialog.vue` | Optional mobile search |

**Composables:** `useTataRekeningWorkspace`, `useTataRekeningQueryKeys`, `useRegIdSelection`

**Acceptance Criteria:**

- [ ] Selecting a registration loads data via `GET /api/tatarekening/{regId}`.
- [ ] Summary shows correct `status`, `financialVerificationStatus`, allocation and settlement flags.
- [ ] Bills render with `modulGroup` (Jasa/Obat), `nilaiTotal`, `financialTotal`.
- [ ] Pending merge requests display when present.
- [ ] 404 shows user-friendly message; no mock patients remain.
- [ ] No write actions enabled (pre-Phase 2).

---

### Phase 2 — Close Bill (SOP-TR-02)

**Goal:** Operational freeze — Verifikator closes billing when charges are complete.

**Scope:**

- Close Bill action on toolbar when `status === Opened (0)`.
- Confirmation dialog explaining operational freeze implications.
- Mutation with cache update from `data.summary`.
- Post-close: disable Close; enable Merge/Verify path per status.

**API used:**

| Action | Endpoint |
|--------|----------|
| Close Bill | `POST /api/tatarekening/{regId}/close` |

**UI Components:**

| Component | Role |
|-----------|------|
| `CloseBillDialog.vue` | Confirmation |
| `TataRekeningActionToolbar.vue` | Extract actions from workspace |

**Composables:** `useCloseBillMutation`

**Acceptance Criteria:**

- [ ] Close button visible only when `status === 0`.
- [ ] Successful close updates status badge to Closed (1).
- [ ] 400 shows backend message (e.g. already closed).
- [ ] 403 disables write actions for non-Verifikator users.
- [ ] Bills remain unchanged after close (read-only refresh).

---

### Phase 3 — Merge Billing (SOP-TR-03)

**Goal:** Execute pending merge requests after Close Bill.

**Scope:**

- Enable Merge action when `status === Closed` and pending merges exist.
- Merge dialog: select request, show source → target preview, confirm.
- Handle `MergeBillingApiResponse` — update target workspace; optionally notify if source reg was open in queue.
- Long-running indicator (merge may involve accounting transfer).

**API used:**

| Action | Endpoint |
|--------|----------|
| Execute merge | `POST /api/tatarekening/merge` |

**UI Components:**

| Component | Role |
|-----------|------|
| `MergeBillingDialog.vue` | Select merge request + confirm |
| `MergeRequestRow.vue` | Single pending request row with execute action |

**Composables:** `useMergeBillingMutation`

**Acceptance Criteria:**

- [ ] Merge only offered when `status === Closed` and pending requests exist.
- [ ] Request body sends `mergeRequestId`.
- [ ] On success: merge request removed from pending list; bills refreshed on target reg.
- [ ] Verify blocked while pending merges exist (backend 400) — UI disables Verify with tooltip.
- [ ] 409 triggers full Open reload and retry prompt.

---

### Phase 4 — Financial Verification (SOP-TR-04)

**Goal:** Verifikator confirms billing completeness or flags adjustment required.

**Scope:**

- Verify and Require Adjustment actions when `status === Closed` and no pending merges.
- Verification status badge updates from `financialVerificationStatus`.
- Gate Allocate button on `financialVerificationStatus === Valid (1)`.

**API used:**

| Action | Endpoint |
|--------|----------|
| Mark valid | `POST /api/tatarekening/{regId}/verify` body `{ action: 0 }` |
| Require adjustment | `POST /api/tatarekening/{regId}/verify` body `{ action: 1 }` |

**UI Components:**

| Component | Role |
|-----------|------|
| `VerificationStatusBadge.vue` | NotVerified / Valid / RequiresAdjustment |
| `FinancialVerificationDialog.vue` | Confirm verify or flag adjustment |

**Composables:** `useFinancialVerificationMutation`

**Acceptance Criteria:**

- [ ] Verify sets `financialVerificationStatus` to Valid (1).
- [ ] Require Adjustment sets status to RequiresAdjustment (2) and surfaces Adjust action.
- [ ] Allocate disabled until Valid.
- [ ] 400 when pending merge blocks verification — clear message shown.

---

### Phase 5 — Financial Adjustment (SOP-TR-05)

**Goal:** Apply financial corrections without changing operational history.

**Scope:**

- Adjust dialog with type selector (ManualCharge, BillingCorrection, Waive, Subsidy, MergeBillingCorrection).
- Dynamic form fields per `FinancialAdjustmentTypeEnum`.
- Handle `requiresReopen: true` — show guided next step toward Reopen (Phase 9).
- Refresh bills and projection after adjustment.

**API used:**

| Action | Endpoint |
|--------|----------|
| Apply adjustment | `POST /api/tatarekening/{regId}/adjust` |

**UI Components:**

| Component | Role |
|-----------|------|
| `FinancialAdjustmentDialog.vue` | Type + amount + reason + bill selector |
| `AdjustmentTypeSelector.vue` | Enum-driven type picker |

**Composables:** `useFinancialAdjustmentMutation`

**Acceptance Criteria:**

- [ ] Adjust available when `status === Closed` and verification is Valid or RequiresAdjustment.
- [ ] Required fields validated client-side; 422 highlights server validation errors.
- [ ] When `requiresReopen === true`, show banner: "Reopen Billing required" with link to Reopen action.
- [ ] Bills refresh after successful adjustment without local mutation.

---

### Phase 6 — Financial Responsibility Allocation (SOP-TR-06)

**Goal:** Assign payer responsibility and display regenerated projection.

**Scope:**

- Refactor `PaymentAllocationTable.vue` → `AllocationEditor.vue` mapped to `PaymentAllocationInputDto`.
- Client-side sum check: `Σ(nilaiJasa + nilaiObat) === Σ billing totals` (UX hint only; backend enforces).
- Payer picker: integrate `JaminanService` / payment type master for `paymentId`, `paymentName`, `coaId`, `coaName`.
- Allocate mutation; display returned `projection`.
- Lock editor when `status !== Closed` or not verified.

**API used:**

| Action | Endpoint |
|--------|----------|
| Allocate | `POST /api/tatarekening/{regId}/allocate` |

**UI Components:**

| Component | Role |
|-----------|------|
| `AllocationEditor.vue` | Refactor from `PaymentAllocationTable.vue` |
| `PayerPickerDialog.vue` | Add payer row from master data |
| `AllocationBalanceIndicator.vue` | Shows remaining vs total billing |

**Composables:** `useAllocationForm`, `useAllocateMutation`

**Acceptance Criteria:**

- [ ] Allocate sends `payments[]` matching API schema (integer enums, Jasa/Obat split).
- [ ] Successful allocate sets `isFinancialResponsibilityAllocated: true`.
- [ ] Projection grid updates from `data.projection`.
- [ ] 400 on total mismatch shows backend message.
- [ ] Editor read-only when finalized or lunas.

---

### Phase 7 — Finalize Financial Responsibility (SOP-TR-07)

**Goal:** Lock allocation and prepare for settlement.

**Scope:**

- Finalize action when allocated and `status === Closed`.
- Confirmation dialog summarizing projection totals.
- Post-finalize: lock allocation editor; enable Settlement Initiation and Cancel Finalization.

**API used:**

| Action | Endpoint |
|--------|----------|
| Finalize | `POST /api/tatarekening/{regId}/finalize` |

**UI Components:**

| Component | Role |
|-----------|------|
| `FinalizeDialog.vue` | Review totals + confirm |

**Composables:** `useFinalizeMutation`

**Acceptance Criteria:**

- [ ] Finalize only enabled when `isFinancialResponsibilityAllocated` and verification Valid.
- [ ] Status becomes Finalized (2).
- [ ] Allocation editor becomes read-only.
- [ ] 400 shows specific validation failure (e.g. allocation incomplete).

---

### Phase 8 — Cancel Finalization (SOP-TR-08)

**Goal:** Revert Finalized → Closed for re-allocation before payment.

**Scope:**

- Cancel Finalization action when `status === Finalized` and `settlementInitiated === false`.
- Required reason field (min length 1).
- Post-cancel: re-enable allocation editor; guide user to re-allocate and re-finalize.

**API used:**

| Action | Endpoint |
|--------|----------|
| Cancel finalization | `POST /api/tatarekening/{regId}/cancel-finalization` |

**UI Components:**

| Component | Role |
|-----------|------|
| `CancelFinalizationDialog.vue` | Reason capture + confirm |

**Acceptance Criteria:**

- [ ] Only visible when `status === 2` and not yet settled.
- [ ] Reason required; 422 if empty.
- [ ] Status returns to Closed (1); `isFinancialResponsibilityAllocated` becomes false.
- [ ] User guided to Phase 6 flow again.

---

### Phase 9 — Reopen Billing (SOP-TR-09)

**Goal:** Return Closed billing to Open for Charge Source corrections.

**Scope:**

- Reopen action when `status === Closed` (typically after `requiresReopen` from Adjust).
- Required reason dialog.
- Post-reopen: guide user to wait for Charge Source updates, then Close Bill again (Phase 2 loop).
- Reset verification UI state when status returns to Opened.

**API used:**

| Action | Endpoint |
|--------|----------|
| Reopen billing | `POST /api/tatarekening/{regId}/reopen` |

**UI Components:**

| Component | Role |
|-----------|------|
| `ReopenBillingDialog.vue` | Reason capture + confirm |

**Acceptance Criteria:**

- [ ] Reopen only when `status === Closed` and not Finalized.
- [ ] Status becomes Opened (0).
- [ ] Informational banner: "Repeat Close Bill after Charge Source updates."
- [ ] Merge/Verify/Allocate disabled until re-close.

---

### Phase 10 — Settlement Initiation (SOP-TR-10)

**Goal:** Hand off finalized billing to Cashier.

**Scope:**

- Settlement Initiation when `status === Finalized` and `settlementInitiated === false`.
- Confirmation dialog; optional link/navigate hint to Cashier module (future integration).
- Post-initiation: workspace becomes read-only for Verifikator; `settlementInitiated: true`.

**API used:**

| Action | Endpoint |
|--------|----------|
| Settlement initiation | `POST /api/tatarekening/{regId}/settlement-initiation` |

**UI Components:**

| Component | Role |
|-----------|------|
| `SettlementInitiationDialog.vue` | Confirm handoff |
| `SettlementHandoffBanner.vue` | Post-initiation read-only state |

**Composables:** `useSettlementInitiationMutation`

**Acceptance Criteria:**

- [ ] Only enabled when `status === 2` and `settlementInitiated === false`.
- [ ] Sets `settlementInitiated: true` without changing status.
- [ ] All write actions disabled after initiation.
- [ ] Clear messaging that Cashier handles payment.

---

## 6. Shared Components

Components reused across multiple phases:

| Component | Purpose | Used in Phases |
|-----------|---------|----------------|
| `TataRekeningSummaryCard` | Lifecycle status, verification, flags | 1–10 |
| `BillingGrid` | `TrsBillSummaryDto[]` tabular display | 1–10 |
| `BillingSummaryCard` | Jasa/Obat subtotals, grand total | 1–10 |
| `ProjectionGrid` | `PaymentProjectionDto[]` read-only | 1, 6–10 |
| `AllocationEditor` | Payer allocation input grid | 6–10 |
| `MergeRequestPanel` | Pending merge list | 1, 3 |
| `TataRekeningActionToolbar` | Status-gated action buttons | 2–10 |
| `LifecycleStatusBadge` | OPEN / CLOSED / FINALIZED / LUNAS | 1–10 |
| `VerificationStatusBadge` | NotVerified / Valid / RequiresAdjustment | 4–10 |
| `ConfirmationDialog` | Generic confirm wrapper (shared `Dialog`) | 2–10 |
| `ReasonCaptureDialog` | Reusable reason textarea (Reopen, Cancel) | 8, 9 |
| `AuditTimeline` | Future: display mutation audit trail from API | Post-MVP |
| `WorkspaceErrorState` | 404 / 500 / offline display | 1–10 |
| `WorkspaceLoadingSkeleton` | Shimmer during Open fetch | 1–10 |

**Prototype reuse map:**

| Existing | Target |
|----------|--------|
| `RegistrasiKeluar.vue` | `TataRekeningWorkspace.vue` |
| `BillTable.vue` | `BillingGrid.vue` |
| `BillSummaryTable.vue` | `BillingSummaryCard.vue` |
| `PaymentAllocationTable.vue` | `AllocationEditor.vue` |
| `PatientCard.vue` | Extend or replace with `PatientSummaryCard` (add RegistrasiService data) |
| `PaymentTypeModal.vue` | `PayerPickerDialog.vue` |
| `useUITataRekeningHelpers.ts` | Keep; extend for lifecycle/verification badge classes |

---

## 7. API Mapping

Complete mapping of UI actions to endpoints:

| UI Action | Condition | HTTP | Endpoint |
|-----------|-----------|------|----------|
| Select registration | User picks reg | GET | `/api/tatarekening/{regId}` |
| Refresh workspace | User clicks refresh / after 409 | GET | `/api/tatarekening/{regId}` |
| **Close Bill** | `status === 0` | POST | `/api/tatarekening/{regId}/close` |
| **Execute Merge** | `status === 1` + pending merge | POST | `/api/tatarekening/merge` |
| **Verify (valid)** | `status === 1`, no pending merge | POST | `/api/tatarekening/{regId}/verify` `{ action: 0 }` |
| **Require Adjustment** | `status === 1` | POST | `/api/tatarekening/{regId}/verify` `{ action: 1 }` |
| **Apply Adjustment** | `status === 1` | POST | `/api/tatarekening/{regId}/adjust` |
| **Allocate** | `status === 1`, verification Valid | POST | `/api/tatarekening/{regId}/allocate` |
| **Finalize** | allocated, `status === 1` | POST | `/api/tatarekening/{regId}/finalize` |
| **Cancel Finalization** | `status === 2`, not settled | POST | `/api/tatarekening/{regId}/cancel-finalization` |
| **Reopen Billing** | `status === 1` | POST | `/api/tatarekening/{regId}/reopen` |
| **Settlement Initiation** | `status === 2`, not initiated | POST | `/api/tatarekening/{regId}/settlement-initiation` |

**Action availability matrix (client guards — backend enforces):**

| Action | Opened (0) | Closed (1) | Finalized (2) | Lunas (3) |
|--------|------------|------------|---------------|-----------|
| Close | ✓ | | | |
| Merge | | ✓ (if pending) | | |
| Verify | | ✓ (if no pending merge) | | |
| Adjust | | ✓ | | |
| Allocate | | ✓ (if verified) | | |
| Finalize | | ✓ (if allocated) | | |
| Cancel Finalization | | | ✓ (if not settled) | |
| Reopen | | ✓ | | |
| Settlement | | | ✓ (if not initiated) | |

---

## 8. State Management

### Page State

| State | Storage | Notes |
|-------|---------|-------|
| `regId` | `ref` in workspace; optional URL query `?regId=` | Source trigger for Open query |
| `selectedMergeRequestId` | `ref` in merge dialog | Dialog-local only |
| Allocation draft rows | `ref` in `AllocationEditor` | Dirty until Allocate POST succeeds |
| Adjustment form | `ref` in dialog | Discarded on close |

### Server State (TanStack Query)

```text
queryKey: ['tataRekening', 'open', regId]
queryFn:  GET /api/tatarekening/{regId}
enabled:  !!regId
```

Mutations invalidate or directly `setQueryData` from mutation response:

| Mutation | Cache strategy |
|----------|----------------|
| Close, Verify, Finalize, Cancel, Reopen, Settlement | Set `summary` from `data.summary` |
| Allocate | Set `summary` + `projection` from response |
| Merge | Refetch Open for `regId`; if viewing target, update from `targetSummary` |
| Adjust | Refetch Open (bills may change) |

### Loading State

- Use `isLoading` / `isFetching` from `useQuery` for Open.
- Use `isPending` from each `useMutation` to disable action buttons and show spinner on triggering button.
- Full-page skeleton only on initial load; background refetch uses subtle indicator.

### Dirty State

- `AllocationEditor`: track unsaved changes; warn on `regId` change or navigation if dirty.
- No dirty state for read-only phases (Close, Verify, Finalize use confirmation only).

### Error State

- Query error: `WorkspaceErrorState` with retry.
- Mutation error: toast + dialog for 400; inline for 422 form errors.

### Refresh Strategy

1. **On mount / regId change:** automatic Open fetch.
2. **After successful mutation:** update cache from response; no extra GET unless response lacks full bill list.
3. **On 409:** `fetchOpenTataRekening(regId)` then show "Data changed — review and retry."
4. **Manual refresh:** invalidate `['tataRekening', 'open', regId]`.
5. **No polling** unless long-running merge feedback requires it (prefer mutation `isPending` with timeout message).

### Pinia

**Recommendation: no Pinia store for Tata Rekening billing data.** Follow project declarative pattern — TanStack Query is sufficient. Optional lightweight Pinia store only if registration queue filters must persist across tab switches (prefer `sessionStorage` or URL params).

---

## 9. Error Handling

| HTTP | Meaning | Frontend Behavior |
|------|---------|-------------------|
| **Validation (client)** | Missing reason, empty payments array | Inline field errors; block submit |
| **400** | Business rule violation | Dialog or toast with `message`; keep user on workspace; do not clear forms |
| **401** | Not authenticated | Redirect to login via auth guard; clear pending mutations |
| **403** | Not Verifikator | Hide/disable all POST actions; show permission banner |
| **404** | Reg / Tata Rekening not found | `WorkspaceErrorState`; offer return to registration queue |
| **409** | Optimistic concurrency conflict | Auto GET Open to resync; dialog "Another user modified this registration. Review changes and retry." |
| **422** | Request validation failed | Map `message` to form fields where possible; highlight invalid inputs |
| **500** | Unexpected server error | Generic error toast; offer retry button; log to console |

**JSend envelope:** Always parse `code` and `message` from error response body. Success responses use `data` payload.

**Network offline:** Detect via mutation/query failure; show retry when connectivity returns.

---

## 10. Navigation Flow

### Normal Path

```text
Registration Queue
        ↓
Open Tata Rekening (GET)
        ↓
Close Bill
        ↓
Merge Billing (optional, if pending requests)
        ↓
Financial Verification (action: Verify)
        ↓
Financial Adjustment (optional, if needed)
        ↓
Financial Responsibility Allocation
        ↓
Finalize Financial Responsibility
        ↓
Settlement Initiation
        ↓
[Cashier] Payment Settlement → LUNAS
```

### Exception: Cancel Finalization

```text
Finalize Financial Responsibility
        ↓
Cancel Finalization (reason required)
        ↓
Financial Responsibility Allocation (re-allocate)
        ↓
Finalize Financial Responsibility
        ↓
Settlement Initiation
```

### Exception: Reopen Billing

```text
Financial Adjustment (requiresReopen = true)
   OR user-initiated Reopen
        ↓
Reopen Billing (reason required)
        ↓
[Charge Source updates charges]
        ↓
Close Bill
        ↓
Merge Billing (optional)
        ↓
Financial Verification
        ↓
… continue normal path …
```

### Exception: Require Adjustment Loop

```text
Financial Verification (action: RequireAdjustment)
        ↓
Financial Adjustment
        ↓
(if requiresReopen) → Reopen path
(if not) → Financial Verification (action: Verify)
        ↓
Allocation → Finalize → Settlement
```

### Module Navigation

- Entry: HIS sidebar → **Tata Rekening** tab.
- `regId` persists in workspace while tab is active.
- Switching to another module clears or preserves `regId` per UX decision (recommend preserve in session for return).
- Deep link from Admisi: navigate to `/app/tata_rekening?regId={id}` (Phase 1+ enhancement).

---

## 11. Technical Recommendation

### Stack Alignment

The implementation plan assumes Vue 3 + TypeScript + Composition API. The **actual** `c012_myhospital_web` codebase uses:

- **UI:** Tailwind CSS v4 + shadcn/vue primitives (`src/shared/components/ui/`) — **not PrimeVue**
- **Server state:** TanStack Query (`@tanstack/vue-query`)
- **Forms:** VeeValidate + Zod (where complex); simple dialogs use controlled refs
- **Routing:** Vue Router workspace pattern `/app/:screen?/:tab?`
- **API:** `ApiService` + `createQueryFn` / `createPostMutationFn` with `apiName: 'bilregApi'`

Use existing shared components (`Button`, `Card`, `Dialog`, `DataTable` patterns) rather than introducing PrimeVue.

### Folder Structure

```text
src/modules/TataRekening/
├── index.ts                          # HISModule export
├── configs/
│   └── tabs.ts                       # Tab → TataRekeningWorkspace.vue
├── types/
│   ├── index.ts                      # Re-export
│   ├── tataRekeningSchemas.ts        # Zod schemas from OpenAPI DTOs
│   └── tataRekeningEnums.ts          # Status enums (mirror backend ints)
├── services/
│   └── TataRekeningService.ts        # Query hooks + fetchers + mutations
├── composables/
│   ├── useTataRekeningWorkspace.ts   # Orchestration: regId, guards, banners
│   ├── useTataRekeningActionGuards.ts# computed enabled actions from summary
│   ├── useAllocationForm.ts          # Draft rows + sum validation hint
│   └── useUITataRekeningHelpers.ts   # Badge/icon classes (existing)
├── components/
│   ├── workspace/                    # Shell panels
│   ├── billing/                      # Grid, summary
│   ├── allocation/                   # Editor, payer picker
│   ├── merge/                        # Merge panel, dialog
│   ├── actions/                      # Toolbar, dialogs per action
│   └── shared/                       # Badges, error/loading states
├── views/
│   └── TataRekeningWorkspace.vue     # Main view (replaces RegistrasiKeluar.vue)
└── __tests__/                        # Colocated specs per AGENTS.md
```

### Component Organization

- **Smart view** (`TataRekeningWorkspace.vue`): wires `regId`, query, toolbar, layout.
- **Dumb panels**: receive props, emit events; no direct API calls.
- **Dialogs**: encapsulate one mutation each; receive `regId` + `summary` as props.

### Service Layer

Single `useTataRekeningService()` factory (mirror `useRegistrasiService`):

| Export | Type | Purpose |
|--------|------|---------|
| `useOpenTataRekening(regId)` | `useQuery` | GET Open |
| `fetchOpenTataRekening(regId)` | async fetcher | For 409 resync, `useQueries` |
| `useCloseBillMutation()` | `useMutation` | POST close |
| `useMergeBillingMutation()` | `useMutation` | POST merge |
| `useVerifyMutation()` | `useMutation` | POST verify |
| `useAdjustMutation()` | `useMutation` | POST adjust |
| `useAllocateMutation()` | `useMutation` | POST allocate |
| `useFinalizeMutation()` | `useMutation` | POST finalize |
| `useCancelFinalizationMutation()` | `useMutation` | POST cancel-finalization |
| `useReopenMutation()` | `useMutation` | POST reopen |
| `useSettlementInitiationMutation()` | `useMutation` | POST settlement-initiation |

Register query keys in `src/core/api/queryConfigs.ts`:

```text
tataRekening: {
  all: ['tataRekening'],
  open: (regId: string) => ['tataRekening', 'open', regId],
}
```

### API Client Organization

- All endpoints under resource prefix `tatarekening` via `bilregApi`.
- JSend unwrap: validate `data` field with Zod; let `ApiService` handle HTTP errors.
- Enum values as **integers** in JSON — Zod `z.nativeEnum` or `z.number()` with const maps.

### DTO Mapping Strategy

1. **Define Zod schemas** from `swagger.json` component schemas (source of truth).
2. **Infer TypeScript types** via `z.infer<typeof schema>`.
3. **Adapter functions** only where UI needs derived display:
   - `billsToJasaObatTotals(bills: TrsBillSummaryDto[])` → summary card
   - `modulGroupLabel(0 | 1)` → "Jasa" | "Obat"
   - `statusLabel(TataRekeningStatusEnum)` → badge text
4. **Do not map** to legacy `PatientBilling` / `PaymentRow` mock types — deprecate `types/index.ts` mock schemas after Phase 1.
5. **Allocation editor** maps between `PaymentProjectionDto` (display) and `PaymentAllocationInputDto` (submit) with explicit form model.

### Authentication

- GET Open: no auth header required (anonymous).
- All POST: `Authorization: Bearer` from `AuthStore`; guard write UI by role `VERIF-SPV` / `VERIF-USR` or permission `TATA-REKENING-WRITE`.
- Do not send verifier identity in request bodies.

### Testing Strategy

| Layer | Focus |
|-------|-------|
| Zod schemas | Valid/invalid API payloads |
| `useTataRekeningActionGuards` | Action enabled/disabled per status matrix |
| Components | Render from fixture `OpenTataRekeningApiResponse` |
| Workspace integration | MSW or mocked `TataRekeningService` — mutation → cache update |
| E2E (Playwright) | Happy path Phase 1→7 with test API |

### Documentation

Update `c012_myhospital_web/docs/modules/tata-rekening/` per phase (README, features, api.md, sop alignment) as implementation progresses.

---

## 12. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Long-running Merge Billing** | User assumes freeze; double-submit | Disable merge button during `isPending`; show progress message; timeout with retry guidance |
| **Concurrent updates (409)** | Stale UI leads to failed mutations | Standard 409 handler: auto GET Open + retry dialog; never optimistic-update financial state |
| **Refresh after Merge** | Target reg bills change substantially | Refetch Open for current `regId`; if merge moves bills away from source, show notification |
| **Partial API responses** | Mutation returns `summary` only, not full bills | After Adjust/Merge, always refetch Open or invalidate query |
| **Registration queue gap** | No dedicated dischargeable-list API | Phase 1 uses `RegistrasiService` active list; document filter limitations; plan queue endpoint later |
| **Legacy prototype confusion** | Team reuses `isRegOut` boolean semantics | Remove `isRegOut` and mock types in Phase 1; status enum only |
| **Allocation sum mismatch** | User frustration on 400 | Live client-side sum indicator (non-blocking); clear error from backend |
| **requiresReopen flow** | User stuck after Adjust | Prominent banner + guided Reopen → Close loop |
| **Integer enums in JSON** | Zod parse failures if expecting strings | Schemas use `z.number()`; document in types |
| **Anonymous GET / authenticated POST** | Dev environment auth mismatch | Test write actions only with Verifikator JWT; feature-flag write toolbar in dev |
| **Cashier handoff** | User expects payment in Tata Rekening | Clear copy in Settlement dialog; link to Cashier module when integrated |
| **Virtual scroll for bill list** | Performance with large billing sets | Use `useVirtualList` when `bills.length > 50` per AGENTS.md |
| **Shared lib coupling** | `billing.ts` imports deprecated TataRekening types | Refactor shared billing utils to accept generic shapes or move TR-specific helpers into module |

---

## Appendix A — Migration from Prototype

| Remove / Deprecate | Replace With |
|--------------------|--------------|
| Mock `patients` ref in `RegistrasiKeluar.vue` | `RegistrationQueuePanel` + `RegistrasiService` |
| `isRegOut` boolean | `summary.status` (TataRekeningStatusEnum) |
| `executeRegOut()` / `cancelRegOut()` | `useCloseBillMutation` / `useReopenMutation` |
| `RegOutInputSchema` / `RegOutOutputSchema` | Remove from `types/index.ts` |
| Local `bills` ref + manual bill modal (local only) | Phase 5 `FinancialAdjustment` API for manual charge |
| `createDefaultPaymentRows()` on patient select | Load `projection` from Open; init allocation draft from projection or empty |

---

## Appendix B — Phase Dependency Graph

```text
Phase 1 (Open)
    ↓
Phase 2 (Close)
    ↓
Phase 3 (Merge) ──────────────────────────┐
    ↓                                       │
Phase 4 (Verify)                            │
    ↓                                       │
Phase 5 (Adjust) ──► Phase 9 (Reopen) ─────┤──► back to Phase 2
    ↓                                       │
Phase 6 (Allocate)                          │
    ↓                                       │
Phase 7 (Finalize) ──► Phase 8 (Cancel) ───┘──► back to Phase 6
    ↓
Phase 10 (Settlement)
```

Phases 8 and 9 can be developed in parallel after Phase 7 and Phase 5 respectively, but integration testing requires the full graph.

---

## Appendix C — Related Documents

| Document | Path |
|----------|------|
| API Contract | `tata-rekening-api-contract.md` |
| Frontend Integration Guide | `frontend-integration-guide.md` |
| OpenAPI Spec | `swagger.json` |
| Frontend Readiness Checklist | `frontend-readiness-checklist.md` |
| Frontend Gap Analysis | `c012_myhospital_web/tata-rekening-frontend-gap-analysis.md` |
| Data Fetching Guide | `c012_myhospital_web/docs/guides/data-fetching.md` |
| State Management Guide | `c012_myhospital_web/docs/guides/state-management.md` |

---

*This document is the master guideline for incremental Tata Rekening frontend implementation. Do not implement UI in this artifact — execute one phase at a time, validating against SOP acceptance criteria before proceeding.*
