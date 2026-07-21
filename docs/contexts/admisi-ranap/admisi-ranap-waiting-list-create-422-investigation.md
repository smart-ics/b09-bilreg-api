# Waiting List Create 422 — Investigation Report

**Date:** 12 July 2026  
**Scope:** Post-registration Waiting List continuation after successful Opname/Reservation admit  
**Nature:** Gap analysis only — **no fix implemented**  
**Verdict:** **backend fix required**

---

## 1. Current frontend-to-backend Waiting List flow

```text
ProsesAdmisi success phase
  → WaitingListFork ("Buat Waiting List")
  → useProsesAdmisi.createWaitingListFork()
  → resolveHospitalKelasIdForWaitingList(resultKelasDkId)   // kelasDk → hospital kelasId
  → useCreateWaitingList().mutateAsync({ regId, kelasId, bangsalId, priority, userId })
  → POST /api/admisi-ranap/waiting-list
  → WaitingListController.Create(AdmCreateWaitingListCmd)
  → AdmCreateWaitingListHandler
       Guard required strings
       Load Admission by RegId
       HasActiveByRegId(regId)          ← fails here today
       ResolveKelas / ResolveBangsal
       WaitingListModel.Create(...)
       WaitingListRepo.SaveChanges → BILRG_BedWaitingList INSERT
       Audit + NotifyHandOver
```

Relevant frontend files:

- `src/modules/Admisi/composables/useProsesAdmisi.ts` (`createWaitingListFork`)
- `src/modules/Admisi/components/ranap/proses/WaitingListFork.vue`
- `src/modules/Admisi/queries/AdmisiRanapService.ts` (`useCreateWaitingList`)
- `src/modules/Admisi/types/admisiRanap.ts` (`createWaitingListInputSchema`)

Relevant backend files:

- `WaitingListController.cs`
- `AdmCreateWaitingListCmd.cs` / handler
- `WaitingListModel.cs` + `AdmissionStatusGuard`
- `WaitingListRepo.cs` / `WaitingListDal.cs`
- `ErrorHandlerMiddleware.cs` (maps `ArgumentException` → HTTP 422)

---

## 2. Exact endpoint and payload used

**Endpoint:** `POST /api/admisi-ranap/waiting-list`

**Frontend payload shape (sanitized, as sent after Step 6 Opname success):**

```json
{
  "regId": "RGA4LKWO02",
  "kelasId": "K04",
  "bangsalId": "R1",
  "priority": 3,
  "userId": "SPR001"
}
```

Notes:

- `regId` is the **shared registration/admission id** returned by process (`data.regId`), not Opname/Reservation source id.
- `kelasId` is **hospital class** (`ta_kelas` / `KelasType`), obtained by mapping selected Care Class (`kelasDkId=3`) via `listKelas.find(k => k.kelasDk.kelasDkId === kelasDkId)` → first match `K04` (KELAS-I).
- `bangsalId` is the selected destination ward from the registration draft (`R1`).
- `priority` defaults to `3` (Normal) from `PRIORITY_BAND_OPTIONS` (1–4).
- Rapid double-click can send **more than one** POST; both fail the same way today.

**Observed response:**

```json
{
  "status": "Validation Error",
  "code": "422",
  "data": "Value cannot be null. (Parameter 'source')"
}
```

No `BILRG_BedWaitingList` row is inserted for the new `regId`.

---

## 3. Backend request contract

```csharp
public record AdmCreateWaitingListCmd(
    string RegId,
    string KelasId,      // hospital KelasId, NOT kelasDkId
    string BangsalId,
    int Priority,
    string UserId) : IRequest<AdmCreateWaitingListResponse>, IRegKey;
```

Handler rules:

| Check | Behavior if failed |
|-------|--------------------|
| `RegId` / `KelasId` / `BangsalId` / `UserId` non-empty | `ArgumentException` → **422** |
| Admission exists for `RegId` | `KeyNotFoundException` → **404** |
| No active Waiting List for `RegId` | `InvalidOperationException` → **400** |
| `ResolveKelas(KelasId)` | missing → 404 (via `GetValueOrThrow`) |
| `ResolveBangsal(BangsalId)` | missing → 404 |
| Admission status in {Admitted, Updated, Waiting} | `InvalidOperationException` → **400** |

Domain identity: Waiting List is keyed to **`RegId`** (same id as Admission / legacy Registration).

---

## 4. Field-by-field contract comparison

| Field | Frontend sends | Backend expects | Required | Result |
|-------|----------------|-----------------|----------|--------|
| `regId` | Process response `regId` | `RegId` (Admission/Registration shared id) | Yes | **Correct identity** |
| `kelasId` | Hospital `kelasId` mapped from Care Class | Hospital `KelasId` (`ResolveKelas`) | Yes | **Contract-aligned** (first match for `kelasDkId=3` → `K04`) |
| `bangsalId` | Draft bangsal (`R1`) | `BangsalId` (`ResolveBangsal`) | Yes | **Correct** |
| `priority` | int 1–4 (default 3) | `int Priority` (no range guard in domain) | Yes | **Acceptable** |
| `userId` | `authStore.user.pegId` (e.g. `SPR001`) | non-empty `UserId` | Yes | **Correct** |
| `kelasDkId` | not sent | not expected on create | — | OK (backend uses hospital kelas) |
| Opname/Reservation id | not sent | not required | — | OK |
| reason / source type | not sent | not in command | — | OK |

No frontend identity confusion between Admission ID and Registration ID: they share `RegId` by design.

---

## 5. Exact source of HTTP 422

Layer: **DAL → LINQ**, surfaced as validation by middleware.

1. Handler calls `_waitingListRepo.HasActiveByRegId(request.RegId)`.
2. Repo → `WaitingListDal.GetActiveByRegId(regId)`.
3. DAL executes:

```csharp
return conn.Read<WaitingListDto>(sql, dp).FirstOrDefault();
```

4. When no active Waiting List rows exist, Nuna `conn.Read<T>(...)` returns **`null`** (not empty sequence).
5. `Enumerable.FirstOrDefault(this IEnumerable<T> source)` throws:

```text
ArgumentNullException: Value cannot be null. (Parameter 'source')
```

6. `ErrorHandlerMiddleware` maps `ArgumentException` (base of `ArgumentNullException`) to:

```text
HTTP 422 / status "Validation Error"
```

This happens **before** `ResolveKelas`, `WaitingListModel.Create`, and **before any INSERT**.

Contrast with safe sibling pattern in `AdmissionDal.ListData`:

```csharp
return conn.Read<AdmissionDto>(sql, dp) ?? [];
```

and `RegAktifDal`:

```csharp
var results = conn.Read<RegAktifDto>(sql) ?? [];
```

---

## 6. Failed validation condition and involved values

| Item | Value |
|------|-------|
| Condition | `conn.Read(...)` returned `null`; `.FirstOrDefault()` invoked on null `source` |
| Not a domain invariant failure | Admission was Admitted (0) and loadable |
| Not missing RegId/KelasId/BangsalId | Guards passed |
| Not “already has active WL” | That path would be HTTP 400 with a clear message |
| Involved regId (repro) | `RGA4LKWO02` (cleaned after repro) |
| Payload | `{ regId, kelasId: K04\|K09\|K10, bangsalId: R1, priority: 3, userId: SPR001 }` |
| DB before | Admission present; no active WL for that regId |
| DB after | unchanged (no WL row) |

Reproduction also showed: even deliberately invalid `kelasId` yields the same `source` 422, because the crash occurs on `HasActiveByRegId` **before** kelas resolution.

---

## 7. Whether the frontend uses the correct identity

**Yes.** Frontend uses the process-returned **`regId`**.

Backend Waiting List aggregate stores and looks up by `RegId`. Admission load uses `IRegKey.RegId`. This matches the shared-`RegId` orchestration model.

No evidence that OpnameRequestId / ReservationId is required or wrongly substituted in the create payload.

---

## 8. Whether accommodation data is correctly sourced

| Data | Source in FE | Backend use | Assessment |
|------|--------------|-------------|------------|
| Care Class selection | Admission Details draft (`kelasDkId`) | Mapped to hospital `kelasId` before POST | Correct direction |
| Hospital `kelasId` | First `listKelas` row with matching `kelasDkId` | `ResolveKelas` | Works for `3→K04`; fragile if multiple hospital classes share one Care Class (`K04`,`K09`,`K10`,`K11`,`K12`) |
| Bangsal | Draft `bangsalId` retained as `resultBangsalId` | `ResolveBangsal` | Correct |
| Priority | UI select on success fork | Stored as-is | Correct |

After the DAL null-guard is fixed, a **non-blocking** contract follow-up remains: prefer a deterministic hospital-class mapping (or let backend accept `kelasDkId` / resolve from Admission), instead of arbitrary `.find` first match.

---

## 9. Whether persistence code is reachable and valid

| Question | Answer |
|----------|--------|
| Is INSERT implemented? | Yes — `WaitingListDal.Insert` into `BILRG_BedWaitingList` |
| Is repo wired? | Yes — `WaitingListRepo.SaveChanges` insert/update |
| Is table ready? | Rollout reports `BILRG_BedWaitingList` ready; other WL rows exist in DB |
| Is persistence reached on current failure? | **No** — fails in `GetActiveByRegId` pre-check |
| Does 422 mask a DB constraint? | **No** — exception is LINQ null `source`, not SQL |

---

## 10. Root cause

**Root cause (blocking):**  
`WaitingListDal.GetActiveByRegId` does not null-coalesce `conn.Read(...)` before `.FirstOrDefault()`. For a newly admitted registration with **no** active Waiting List, `Read` returns `null`, LINQ throws `ArgumentNullException(paramName: "source")`, and middleware reports HTTP **422 Validation Error**. Persistence never runs.

This is a backend DAL defect in the create pre-check path, not a frontend payload/identity defect, and not an Admission/Registration orchestration defect.

---

## 11. Recommended fix (by layer)

### Backend (required)

In `WaitingListDal.GetActiveByRegId`:

```csharp
return (conn.Read<WaitingListDto>(sql, dp) ?? []).FirstOrDefault();
```

(or equivalent empty-sequence guard). Align with `AdmissionDal` / `RegAktifDal`.

Optionally audit other `conn.Read(...).FirstOrDefault()` call sites for the same hazard (e.g. `TarifOperationalStateDal`).

### Frontend (non-blocking follow-up)

- Keep sending `regId` + hospital `kelasId` + `bangsalId` + `priority` + `userId`.
- Improve Care Class → hospital `kelasId` selection (prefer bangsal-compatible / explicit user choice; avoid silent first match among `K04/K09/K10/...`).
- Disable Waiting List button while pending; treat 422 message if still opaque after backend fix.

### Domain / contract (optional alignment)

- Document that Waiting List create consumes **hospital `KelasId`**, while Admission process consumes **`KelasDkId`**.
- Consider resolving kelas/bangsal from Admission inside the handler to reduce FE remapping (larger contract change; not required to clear this 422).

### Database

- No schema change required for this defect.

---

## 12. Focused implementation steps

1. Patch `WaitingListDal.GetActiveByRegId` null-coalesce.
2. Add DAL/integration test: admitted reg with **zero** WL rows → `HasActiveByRegId == false` (must not throw).
3. Add API/handler integration test: create Waiting List immediately after admit → 200 + one `BILRG_BedWaitingList` row.
4. Re-run browser Scenario 10 (success fork → Buat Waiting List).
5. (Follow-up) Harden FE hospital-class mapping / UX pending state.

---

## 13. Required tests

| Test | Expectation |
|------|-------------|
| `WaitingListDal.GetActiveByRegId` with no rows | returns `null`, no exception |
| `WaitingListRepo.HasActiveByRegId` with no rows | `false` |
| `POST /waiting-list` after Opname process | 200, `waitingListId`, row count 1 |
| Duplicate create while active | 400 “sudah memiliki Waiting List aktif” |
| Cancelled admission create | 400 status guard |
| Browser E2E Scenario 10 | success UI → one WL POST → one DB row; second click blocked/rejected cleanly |

---

## Runtime reproduction summary

| Item | Value |
|------|-------|
| Source | Opname Request (fresh) |
| Process | `POST .../admission/from-opname-request` → `regId=RGA4LKWO02`, status Admitted |
| Admission GET | 200 with pasien/kelasDk/bangsal |
| WL POST | 422 `Value cannot be null. (Parameter 'source')` for `K04`/`K09`/`K10` |
| WL table | no new row |
| Cleanup | test admission/opname removed after repro |

---

## Verdict

**backend fix required**

The frontend handoff identity and payload contract are sufficient for create. The HTTP 422 is produced by a null-unsafe `conn.Read(...).FirstOrDefault()` in `WaitingListDal.GetActiveByRegId`, invoked on every create before persistence. Fix the DAL null-guard; then re-verify Waiting List continuation end-to-end.
