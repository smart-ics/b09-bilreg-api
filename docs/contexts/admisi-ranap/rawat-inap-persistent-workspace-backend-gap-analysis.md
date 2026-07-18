# Rawat Inap Persistent Workspace — Backend Gap Analysis

**Audit date:** 11 July 2026  
**Audited scope:** `src/bilreg`  
**Plan:** `D:/Project.Aktif/MyHospitalWeb/c012_myhospital_web/rawat-inap-persistent-workspace-implementation-plan.md`  
**Assessment:** **Partially implemented; not fully complete against the overall plan.** The backend is aligned with the signed first-release capability matrix because most missing write capabilities are explicitly marked `Defer`, but five planned backend capabilities remain unavailable and one required rollback scenario lacks production-like verification.

## Executive conclusion

The backend is sufficient for the first-release, mostly read-only persistent workspace:

- Initial admission orchestration exists for Opname Request and Reservation.
- Admission, legacy Registration, `RegAktif`, source transition, and audit writes share one transaction scope and `RegId`.
- Care Class and Bangsal remain a coupled Admission-owned update.
- Active Waiting List lookup by `RegId` is implemented end-to-end.
- Controllers require authentication and the Admisi Ranap rollout filter.
- No new Case aggregate or Admisi-owned Bed Assignment write surface was introduced.

It is **not sufficient for the plan's full post-admission editing and hardening outcome**. Guarantor, doctor/referral, and narrow guardian corrections are not implemented; cancellation still leaves Registration/`RegAktif` active; authorization is authentication-only; and Admission writes use last-write-wins concurrency. These are documented deferrals rather than hidden discrepancies, but they remain functional gaps against the complete plan.

## Requirement-to-code matrix

| Plan item | Evidence in code | Result |
|---|---|---|
| T2.1 Waiting List by Admission/`RegId` | `WaitingListController.GetByReg`, `AdmGetWaitingListByRegIdQry`, `WaitingListRepo.LoadActiveByRegId`, DAL active-status query, composite SQL index, handler/repository tests | **Implemented** |
| T2.2 preserve and regression-test atomic initial admission | `AdmissionRegistrationOrchestrator` saves Admission, Reg, `RegAktif`, source state, and audit inside `TransHelper.NewScope()`; shared-ID unit assertion exists | **Implemented, verification gap**: no failure-injection integration test proves rollback |
| Existing Care Class + Bangsal update | `AdmUpdateAdmissionCmd` resolves both through Ward gateway and calls `AdmissionModel.Update`; cancelled admission and invalid placement tests exist | **Implemented** |
| T2.3 inpatient financial/guarantor correction | Only pre-admission payload and outpatient `RegJalanUbahJaminanCmd` exist | **Deferred / missing** |
| T2.4 inpatient doctor/referral/visit correction | Values exist at creation; no Admisi Ranap post-admission command/controller endpoint found | **Deferred / missing** |
| T2.5 narrow guardian/address correction | Patient KTP and broad demography commands exist; no narrow case-scoped guardian/address command found | **Deferred / missing** |
| T2.6 coordinated Admission + Reg + `RegAktif` cancellation | `AdmCancelAdmissionHandler` only saves cancelled Admission and its audit | **Deferred / high-impact gap** |
| T2.7 authorization policies | Admisi Ranap controllers have `[Authorize]`; no section-specific policy/role requirement found | **Authentication only; deferred** |
| T2.8 expected-version concurrency | Update/cancel requests and persistence contain no expected version, ETag, or row-version check | **Deferred / missing** |
| T2.9 tests and API documentation | Capability matrix and phase reports exist; focused suite has 80 passing tests | **Mostly implemented**; rollback, authz, and concurrency tests cannot exist until capabilities ship |
| T6.7 optional case-context projection | No projection found | **Correctly not implemented**; proposal is conditional on measured fan-out |

## Detailed gaps

### G-01 — Cancellation leaves a split-brain active episode (high)

`AdmCancelAdmissionHandler` depends only on `IAdmissionRepo` and `IAuditRepo`. It does not void/close the legacy `RegModel` or remove `RegAktif`. The initial-admission guard checks both active Admission and `IRegAktifRepo.IsPasienAktif`; therefore, after Admission cancellation, a patient can remain blocked from re-admission by the stale active registration.

**Plan impact:** T2.6, safe re-admission, Summary action accuracy, and the overall consistency exit criterion are not met.  
**Required decision/fix:** define the cancellation policy, then coordinate Admission, legacy Registration, `RegAktif`, and audit in one transaction. Add rollback and re-admission tests.

### G-02 — No inpatient-owned guarantor correction (high)

The admission process accepts coverage fields only at creation. The remaining mutation is the outpatient `RegJalanUbahJaminanCmd`; the signed capability matrix correctly forbids aliasing it for inpatient use.

**Plan impact:** T2.3/T5.3 and editable Financial Coverage cannot ship.  
**Required decision/fix:** keep the section read-only, or design an inpatient-specific command with explicit billing and eligibility side effects.

### G-03 — No inpatient-owned doctor/referral correction (high)

Doctor, referral, admission method, service, and ticket are resolved during `AdmissionRegistrationOrchestrator.CreateRegistration`, but there is no post-admission Admisi Ranap correction command.

**Plan impact:** T2.4/T5.4 cannot ship.  
**Required decision/fix:** keep these fields read-only, or add a command whose Registration and downstream effects are explicitly certified for inpatient use.

### G-04 — No optimistic concurrency on long-lived edits (high)

`AdmUpdateAdmissionBody`, `AdmCancelAdmissionBody`, and their commands carry no expected version. Admission persistence performs no compare-and-swap or row-version check.

**Plan impact:** T2.8/T5.6 and the hardening outcome are not met; stale drafts can silently overwrite newer placement data.  
**Required decision/fix:** add an expected version/ETag contract and map stale writes to HTTP 409, or retain last-write-wins and explicitly prevent edit affordances that imply conflict safety.

### G-05 — Authorization is not permission-aware (medium)

`[Authorize]` establishes authentication only. No read/edit permission policies are applied to section mutation endpoints.

**Plan impact:** T2.7/T6.2 role-accurate edit gating is not met.  
**Required decision/fix:** define view/edit policies and enforce them server-side before exposing section Save actions.

### G-06 — Guardian/address correction remains broad (medium)

Patient APIs expose KTP correction and a broad `PasienSetDemografiCommand`; there is no narrow guardian/address command that avoids validating or replacing unrelated demographic fields.

**Plan impact:** T2.5/T5.2 independent guardian correction is unavailable.  
**Required decision/fix:** keep guardian/address read-only or introduce narrow Patient-owned commands.

### G-07 — Atomic rollback is asserted structurally, not demonstrated under failure (medium)

The orchestrator encloses all persistence writes in a transaction scope, and the shared `RegId` golden path is covered. The orchestrator test uses mocked repositories and has no injected failure after one or more writes. The persistence integration test does not exercise the complete orchestrator rollback path.

**Plan impact:** the explicit test strategy requirement “failure rolls back” is not evidenced.  
**Required fix:** add an integration test using the real transaction/DAL stack, inject a deterministic failure after an early write, and assert absence of Admission, Registration, `RegAktif`, source transition, and audit residue.

## Implemented strengths

1. **Atomic structure and ownership are preserved.** Both source paths use the same orchestrator and shared Admission-generated `RegId`; no replacement Case aggregate was introduced.
2. **Placement invariant is correctly coupled.** Care Class and Bangsal are resolved together through the Ward gateway before Admission update.
3. **Waiting List association is authoritative.** `GET api/admisi-ranap/waiting-list/by-reg/{regId}` returns only active `Waiting`/`Accepted` rows or null, and its access path has a `(RegId, WaitingListStatus)` index.
4. **Unsafe outpatient reuse was avoided.** No Admisi Ranap controller aliases `RegJalanUbahJaminanCmd` or `RegJalanUbahKunjunganCmd`.
5. **Deferred risk is documented honestly.** `admisi-ranap-persistent-workspace-capability-matrix.md` explicitly records cancellation, concurrency, authz, and post-admission correction limitations.
6. **Inpatient registration does not use `ta_registrasi2`.** Empty komponen after Rawat Inap registration is expected (table is Rawat Jalan / IGD only); inpatient extension lives in `ta_reg_inap` / `ta_reg_history_dokter`.

## Verification performed

Command:

```text
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRanapContext" --no-restore
```

Result: **Passed — 80 passed, 0 failed, 0 skipped.** The build emitted pre-existing nullable/XML warnings outside the audited behavior.

## Recommended closure order

1. Resolve and implement coordinated cancellation (G-01).
2. Add real rollback integration coverage (G-07).
3. Decide whether Financial Coverage and Visit fields remain read-only; if editable, implement G-02 and G-03 without outpatient command reuse.
4. Add concurrency control before enabling long-lived post-admission edits (G-04).
5. Add server-side permission policies (G-05).
6. Keep guardian/address read-only unless narrow Patient commands are approved (G-06).

## Final status

| Interpretation | Verdict |
|---|---|
| Alignment with the signed **first-release capability matrix** | **Aligned**, with the rollback-test caveat |
| Full implementation of every backend capability proposed by the **overall implementation plan** | **Not complete** |
| Safe backend for initial admission + read-only sections + placement edit + Waiting List link | **Yes**, subject to the documented cancellation behavior |
| Safe backend for all post-admission section edits, concurrent drafts, and role-based editing | **No** |

