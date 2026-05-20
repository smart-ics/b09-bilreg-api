# BACKEND_AGENT.md — Laboratory Workflow Feature

> **Read first:** `DOMAIN.md` §1A, `AGENT.md`. This file locks backend behavior for AI agents implementing LWF.

---

# 1. Canonical ownership

| Owner | Responsibility |
|-------|----------------|
| **BIL** | Financial authority; release eligibility (`CLEAR` / `BLOCKED`) |
| **LWF** | Release orchestration; operational workflow state; validation audit trace only |

LWF must **not**:

- own financial lifecycle or approval state,
- persist authoritative financial truth on `LabOrder`,
- implement `FinancialClearanceEnum`, approve/reject clearance commands, or pre-approval fields.

---

# 2. Realtime billing release validation

Validation runs **only** inside `LabOrderReleaseCmd` (release attempt):

```text
Release Attempt → ILabBillingIntegration.ValidateReleaseEligibility → CLEAR | BLOCKED → Release Decision
```

- **Not** a separate pre-approval step or background sync.
- Every release attempt calls BIL — including after amendment + re-verification.

---

# 3. BLOCKED response (LOCKED)

`BLOCKED` is an **operational business outcome**, not a technical failure.

**Required API behavior:**

```http
HTTP 200
```

```json
{
  "released": false,
  "billingStatus": "BLOCKED",
  "message": "Tagihan pasien belum memenuhi syarat release."
}
```

**Do not:**

- throw for BIL `BLOCKED`,
- return HTTP 400 for BIL denial,
- use exception middleware path for `BLOCKED`.

HTTP **400** is reserved for **invalid workflow transitions** (e.g. release when not `Verified`, already `Released`) — same as other domain guards.

On `BLOCKED`:

- order status stays **Verified**,
- append row to `BILRG_LabBillingReleaseCheck` (audit trace),
- return structured 200 response above.

On `CLEAR`:

- transition **Verified → Released**,
- append audit row,
- return `released: true`, `billingStatus: CLEAR`.

---

# 4. Persistence

| Allowed | Forbidden |
|---------|-----------|
| `BILRG_LabBillingReleaseCheck` append-only trace | `FinancialClearance` column/enum on order |
| `lastBillingReleaseCheck` projection on GET (optional) | Cached “approved for release” flag |
| Request/response snapshot per attempt | Financial state machine in LWF |

---

# 5. Amendment

After `PATCH amend` → re-record → re-verify:

- next `PATCH release` **must** call BIL again,
- no assumption that a prior `CLEAR` still applies.

---

# 6. Forbidden reintroductions

AI agents must **not** reintroduce:

- `approveFinancialClearance` / `rejectFinancialClearance` endpoints,
- `FinancialClearanceEnum` on `LabOrderModel`,
- `UpdateFinancialClearance(status)` on aggregate,
- HTTP 400 for BIL `BLOCKED`,
- saga/workflow engine for release validation,
- event-driven pre-clearance sync from BIL into LWF state.

---

# 7. Reference implementation order

See `IMPLEMENTATION_PLAN.md` phase 8 / M8: `LabOrderReleaseCmd` + `ValidateReleaseEligibility` + `LabOrderReleaseResponse` with locked payload shape.
