# Admisi Ranap — Persistent Workspace Capability Matrix

**Status:** Signed for Phase 2 scoping (T1.5)  
**Date:** 11 July 2026  
**Parent FE contract:** `c012_myhospital_web/docs/modules/admisi-ranap/persistent-workspace-phase-1-contract.md`  
**Plan reference:** `rawat-inap-persistent-workspace-implementation-plan.md` §7

---

## Status legend

| Status | Meaning for Phase 2–5 |
| ------ | --------------------- |
| **Ready** | Ship / preserve; frontend may enable Save |
| **Propose** | Design + implement in Phase 2 before FE Save |
| **Defer** | Explicitly out of first release; FE read-only / no fake Save |

---

## Capability matrix

| Capability | Current state | Required action | Status | Blocking frontend feature |
| ---------- | ------------- | --------------- | ------ | ------------------------- |
| Initial admission | Orchestrator + `POST .../from-opname-request` + `POST .../from-reservation`; shared `RegId` transaction | Preserve; regression-test only | **Ready** | Pre-admission Registration submit |
| Patient identity correction | `PATCH /api/Pasien/ktp` | Certify fields allowed in workspace when product enables Patient section edit | **Ready** (API) / FE gate **Defer** for first release | Post-admission Patient Information edit |
| Guardian correction | Bundled in `PasienSetDemografi` | Narrow command **or** keep demografi / read-only UX | **Defer** | Independent guardian Save |
| Guarantor / episode eligibility correction | Pre-admission via process payload; post-admission only via outpatient `Reg/ubahJaminan` | Do **not** reuse `RegJalanUbahJaminan` until validated; inpatient-specific command or remain read-only | **Defer** (Propose if product prioritises) | Post-admission Financial Coverage Save |
| Care Class and Bangsal update | `PUT admission/{id}` coupled; Admission SoT; does not sync `RegModel` placement | Keep coupled invariant; document RegModel non-sync | **Ready** | Post-admission Admission Details placement edit |
| Doctor / referral / visit update | Create-time on Registration only | Inpatient visit correction command **or** read-only | **Defer** | Post-admission doctor/referral edit |
| Waiting List lookup by Admission | FE stub always false; `HasActiveByRegId` exists in DAL only | Expose `GET` by `regId` (active WL or empty) | **Propose** | Suppress duplicate WL; Admission→WL thread; ActiveWaitingListCard |
| Admission cancellation consistency | Admission-only cancel; Reg / `RegAktif` may remain | Coordinated cancel policy **or** document deferral | **Defer** (Propose if re-admit-after-cancel required) | Safe re-admission after cancel |
| Authorization beyond authentication | Controllers `[Authorize]` only | Permission model if section edit gates required | **Defer** | Role-accurate edit/read-only UX |
| Concurrency protection | No expected-version / ETag on inspected writes | Expected-version / conflict **or** accept last-write-wins with documented risk | **Defer** (Propose for long-lived section drafts) | Safe long-lived section drafts |

---

## Phase 2 work items derived from this matrix

| Task | From status | Notes |
| ---- | ----------- | ----- |
| T2.1 Waiting List by `regId` | **Propose** | Minimum new read for honest WL UX |
| T2.2 Orchestrator regression | **Ready** | No behaviour change |
| T2.3 Inpatient financial correction | **Defer** | Do not alias `RegJalanUbahJaminan` |
| T2.4 Inpatient visit correction | **Defer** | Do not alias `RegJalanUbahKunjungan` |
| T2.5 Narrow guardian/address | **Defer** | |
| T2.6 Coordinated cancel | **Defer** | Document residual split-brain until scoped |
| T2.7 Authz | **Defer** | |
| T2.8 Concurrency tokens | **Defer** | Document last-write-wins residual |
| T2.9 Docs/tests | Follow Ready + Propose only | |

---

## Explicit non-goals

- New Case write aggregate.
- Bed / Room assignment commands under Admisi.
- Combined Opname+Reservation process endpoint (Deferred per D-TL-02).
- Breaking changes to process-admission request/response shapes.

---

## Sign-off

Signed as the backend scoping contract for the persistent workspace initiative on **11 July 2026**. Changing a row from Defer → Ready requires an updated revision of this matrix and the FE Phase 1 contract.
