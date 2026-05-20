# FRONTEND_AGENT_RULES.md — Laboratory Workflow Feature

> **Read first:** `DOMAIN.md` §1A, `FRONTEND_API_CONTRACT.md` (release section), `FRONTEND_IMPLEMENTATION_PLAN.md` §27.

---

# 1. Financial authority (UI)

- **BIL** decides release eligibility — not the lab UI.
- **No** financial clearance approve/reject screen, badge state machine, or worklist filtered by `financialClearance`.
- **No** `financialClearance` field on order models.

---

# 2. Release action (realtime validation)

Release button on **Verified** orders calls `PATCH release` only.

Flow:

```text
Verify → Release Attempt → Ask BIL → CLEAR / BLOCKED → Release Decision
```

Do **not** document or build:

```text
Verify → Approve Financial Clearance → Release
```

---

# 3. BLOCKED handling (LOCKED)

`BLOCKED` is **operational business outcome**, not API failure.

**Expected response:**

```json
{
  "status": "success",
  "data": {
    "released": false,
    "billingStatus": "BLOCKED",
    "message": "Tagihan pasien belum memenuhi syarat release."
  }
}
```

HTTP status: **200**.

**Frontend must:**

- show operational toast/notification using `message`,
- keep workflow workspace visible and stable,
- refetch order detail / worklist as needed,
- treat mutation as **successful HTTP call** with business `released: false`.

**Frontend must not:**

- route `BLOCKED` through global exception / error boundary handlers,
- show generic “Request failed” for BIL denial,
- navigate away or lock the screen on `BLOCKED`,
- assume HTTP 400 for billing denial.

Use the same mental model as `PATCH charge` with `success: false` — soft operational outcome.

---

# 4. CLEAR handling

When `released: true` and `billingStatus: CLEAR`:

- confirm release success,
- invalidate release worklist + order detail,
- update UI to **Released**.

---

# 5. Amendment

After amend → re-record → re-verify:

- release button calls `PATCH release` again,
- BIL validation runs again — **no** assumption of prior approval.

---

# 6. Read-only audit UI

Optional on order detail:

- `lastBillingReleaseCheck.billingStatus`,
- `lastBillingReleaseCheck.message`,
- `lastBillingReleaseCheck.checkedAt`.

Display as **audit trace** — not as workflow state or approval status.

---

# 7. Forbidden reintroductions

Do **not** build:

- `FinancialClearanceBadge` with Pending/Approved/Rejected lifecycle,
- clearance approval modals or finance-only queues,
- enable/disable release based on cached clearance field,
- error UX for `billingStatus === 'BLOCKED'`.

Preferred components: `ReleaseBlockedAlert`, `BillingReleaseValidationTrace` (read-only).

---

# 8. TypeScript contract

```ts
interface LabOrderReleaseResponse {
  released: boolean
  billingStatus: 'CLEAR' | 'BLOCKED'
  message: string
}
```

Mutation handler pattern:

```ts
const res = await releaseOrder(payload)
if (!res.released && res.billingStatus === 'BLOCKED') {
  toast.warning(res.message) // operational — not catch()
  return
}
```
