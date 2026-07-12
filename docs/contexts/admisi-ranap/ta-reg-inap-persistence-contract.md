# `ta_reg_inap` Persistence Contract — Inpatient Registration

**Status:** Implemented (Step 4B)  
**Date:** 12 July 2026  
**Scope:** Persistence of the inpatient registration extension during new-system Admission processing  
**Audience:** Maintainers of the inpatient registration write path

Related artifacts:

- `docs/contexts/admisi-ranap/admisi-ranap-registration-orchestration.md`
- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/ENGINEERING.md`
- `docs/skills/feature-persistence-generation.md`

---

## Verdict

**Implemented.** New-system Admission processing persists `RegInapModel` (`ta_reg_inap` + `ta_reg_history_dokter`) in the same ambient transaction as Admission, Registration, RegAktif, source transition, and audit.

---

## 1. Current-state flow

```text
Process Opname Request / Reservation
        |
        v
AdmissionModel.Admit(...)                 // owns RegId
        |
        v
RegFactory.CreateRegInapFromAdmission(...) // RegModel, same RegId
        |
        v
RegInapModel.Create(...)                  // same RegId + prosedur + Primary DPJP
        |
        v
RegAktifModel.CreateFromReg(reg)
        |
        v
Fulfill Opname / Realize Reservation
        |
        v
TransHelper.NewScope()
  - AdmissionRepo.SaveChanges
  - RegRepo.SaveChanges          // ta_registrasi + ta_reg_jaminan
  - RegInapRepo.SaveChanges      // ta_reg_inap + ta_reg_history_dokter
  - RegAktifRepo.SaveChanges
  - source repo + audit
  - Complete()
```

What is written on success:

| Artifact | Written? |
|----------|----------|
| `BILRG_AdmAdmission` | Yes |
| `ta_registrasi` | Yes |
| `ta_reg_jaminan` | Yes (via `RegRepo`) |
| `ta_reg_inap` | Yes (via `RegInapRepo`) |
| `ta_reg_history_dokter` | Yes (via `RegInapRepo`) |
| `BILRG_RegAktif` | Yes |
| Source fulfill/realize + audit | Yes |

---

## 2. Persistence path

```text
AdmissionRegistrationOrchestrator
        |
        +--> creates RegModel                          ✅
        |
        +--> creates RegInapModel                      ✅
        |
        +--> RegInapRepo.SaveChanges(regInap)
                |
                +--> Ita_reg_inap_dal Insert/Update
                +--> IRegHistoryDokterDal delete + insert
```

Required end state for successful inpatient registration:

```text
One ta_registrasi row
        ↕ same RegId
Exactly one ta_reg_inap row
```

Doctor assignment history:

```text
ta_reg_history_dokter  // at least active Primary DPJP from RegInapModel.Create
```

---

## 3. Domain ownership

`RegInapModel` is a **separate aggregate root** in `AdmisiContext/RegFeature`, keyed by `RegId`.

| Domain concern | Persistence |
|----------------|-------------|
| `RegInapModel.RegId` | `ta_reg_inap.fs_kd_reg` |
| `RegInapModel.ProsedurMasukInap` | `ta_reg_inap.fs_kd_caramasuk_inap` |
| `RegInapModel.ListDokter` / active DPJP | `ta_reg_history_dokter` (+ primary also mirrored on `ta_registrasi.fs_kd_medis`) |
| Booking-bed reference | `ta_reg_inap.fs_kd_trs_booking_bed` — legacy blank sentinel at create |
| Secondary medic scalar | `ta_reg_inap.fs_kd_medis_sekunder` — legacy blank sentinel at create |

Orchestrator creates the model and calls `IRegInapRepo`. It does not call DAL objects directly.

---

## 4. Repository contract

```csharp
public interface IRegInapRepo :
    ISaveChange<RegInapModel>,
    ILoadEntity<RegInapModel, IRegKey>,
    IDeleteEntity<IRegKey>
{
}
```

`SaveChanges` responsibilities:

1. Upsert exactly one `ta_reg_inap` row by `fs_kd_reg`.
2. Replace `ta_reg_history_dokter` from `ListDokter` (delete + bulk insert).
3. Map blank sentinels (`' '`) for unused booking/secondary columns.

`LoadEntity` responsibilities:

1. Return `MayBe.None` when the header row is absent (legacy-read compatibility).
2. Resolve prosedur from master; do not invent missing values.
3. Rehydrate doctor assignments; fail explicitly when persisted state cannot form a valid model (e.g. missing active Primary DPJP).

`RegRepo` no longer depends on `Ita_reg_inap_dal`.

---

## 5. Initial `RegInapModel` creation

```text
RegInapModel.Create(
    regId:            admission.RegId / reg.RegId,
    prosedurMasukInap: <resolved ProsedurMasukInapType>,
    primaryDpjp:      reg.Dokter,
    assignDate:       DateOnly.FromDateTime(admission.AuditTrail.Created.Timestamp)
)
```

`ProsedurMasukInapId` is mandatory on the process payload and resolved via `IProsedureMasukInapRepo` before persistence.

---

## 6. Field mapping for `ta_reg_inap`

| Column | Initial value |
|--------|---------------|
| `fs_kd_reg` | Shared `RegId` |
| `fs_kd_caramasuk_inap` | Resolved prosedur id |
| `fs_kd_trs_booking_bed` | Legacy blank `' '` |
| `fs_kd_medis_sekunder` | Legacy blank `' '` |

On read, trim and treat `' '` / `''` / `'-'` as empty before interpreting optional concepts.

---

## 7. Transaction and rollback

All of the following participate in one `TransHelper.NewScope()`:

1. Admission
2. Registration (`ta_registrasi` + guarantor)
3. RegInap (`ta_reg_inap` + `ta_reg_history_dokter`)
4. RegAktif
5. Source fulfillment / realization
6. Audit records

When `ta_reg_inap` persistence fails, the exception propagates before `trans.Complete()`, the ambient transaction aborts, and no Admission / Reg / RegAktif / source / audit residue remains committed.

---

## 8. DAL corrections (done)

File: `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/ta_reg_inap_dal.cs`

- Insert `VALUES` uses `@fs_kd_reg`, `@fs_kd_caramasuk_inap`, `@fs_kd_trs_booking_bed`, `@fs_kd_medis_sekunder`.
- Update `SET` excludes the key column; `fs_kd_reg` remains only in `WHERE`.
- DTO helpers map model ↔ row and normalize legacy blanks.

---

## 9. Legacy-data compatibility

| Risk | Handling |
|------|----------|
| Existing `ta_registrasi` RegInap without `ta_reg_inap` | `LoadEntity` returns `None`; unrelated reads must tolerate absence |
| New-system writes | Always enforce extension invariant in the same transaction |
| Duplicate rows | Application upsert by `fs_kd_reg` (get-then-insert/update + history replace) |
| Sentinel mismatch | Explicit `' '` on write; normalize on read |

---

## 10. Tests

Covered by:

- `ta_reg_inap_dal_Test`
- `RegInapRepoTest`
- `AdmissionRegistrationOrchestratorTest` (including RegInap failure short-circuit on mocks)
- `AdmissionRegistrationStep4CDbTest` — real `HOSPITAL_HPL` HTTP+SQL verification (Opname/Reservation happy path, RegInap round-trip, invalid prosedur, ambient rollback via failing `IRegInapRepo`, reprocess rejection); see `docs/contexts/admisi-ranap/admisi-ranap-step-4c-verification-report.md`
- Existing `RegRepoTest` / outpatient / shared-`RegId` regression suite
