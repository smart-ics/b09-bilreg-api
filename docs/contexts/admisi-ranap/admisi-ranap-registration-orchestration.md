# Admisi Ranap Registration Orchestration

**Status:** LIVE  
**Audience:** AI coding agents, maintainers, and reviewers  
**Scope:** Admission-created inpatient registration integration

## Purpose

Rawat Inap Admission and legacy Registration are separate models used by two systems running in parallel:

- `AdmissionModel` belongs to the new `AdmisiRanapContext` feature.
- `RegModel` represents the legacy `ta_registrasi` record used by downstream hospital modules.

Every new-system inpatient Admission must create a compatible `RegModel`. Both records must use the same `RegId`.

The application layer owns this cross-aggregate workflow through `IAdmissionRegistrationOrchestrator`.

## Core Ownership Rule

For an Admission created by the new system:

```text
AdmissionModel creates RegId
        |
        +--> RegModel uses the same RegId
        |
        +--> RegAktifModel uses the same RegId
```

For a Registration created by the legacy system:

```text
Legacy RegModel creates RegId
        |
        +--> legacy-sync application service loads RegModel
        |
        +--> AdmissionModel.CreateFromLegacyRegistration(reg, userId)
```

The two directions must not generate a second identity. The existing `RegId` is always reused by the side that did not create it.

## Orchestrator Boundary

The orchestrator is an Application-layer service:

```csharp
public interface IAdmissionRegistrationOrchestrator
{
    Task<AdmProcessAdmissionResponse> ProcessOpnameRequest(
        AdmProcessOpnameRequestCmd request,
        CancellationToken cancellationToken);

    Task<AdmProcessAdmissionResponse> ProcessReservation(
        AdmProcessReservationCmd request,
        CancellationToken cancellationToken);
}
```

The MediatR handlers are adapters only. They delegate to the orchestrator and must not duplicate registration creation, master-data resolution, or transaction coordination.

The service is registered with scoped lifetime because it coordinates repositories and a database transaction.

## Request Contract

Both Admission processing commands receive the same registration payload:

```csharp
public record AdmissionRegistrationData(
    string TipeJaminanId,
    string CaraMasukDkId,
    string ProsedurMasukInapId,
    string RujukanId,
    string DokterId,
    string LayananId,
    string KarcisId,
    string PesertaJaminanId);
```

This payload contains values entered or selected during the Admission workflow. It is not added to `AdmissionModel`; these values belong to Registration construction.

`ProsedurMasukInapId` is **mandatory** for new inpatient registration. It is distinct from `CaraMasukDkId`:

| Field | Meaning | Persistence target |
|-------|---------|--------------------|
| `CaraMasukDkId` | Government/reporting entry classification | `ta_registrasi` (via `RegModel`) |
| `ProsedurMasukInapId` | Inpatient operational entry procedure | `ta_reg_inap.fs_kd_caramasuk_inap` (via `RegInapModel` / `IRegInapRepo`) |

The orchestrator validates `ProsedurMasukInapId` is non-empty and resolves it through `IProsedureMasukInapRepo` **before** any persistence begins. Unknown IDs are rejected. Do not hard-code hospital-specific values or infer the procedure from Opname/Reservation.

`KelasDkId` and `BangsalId` remain Admission processing inputs. They are used to create Admission placement and transient Registration enrichment, but they do not populate the legacy `RegModel.Kelas` persistence field in the new flow.

## New-System Workflow

Both source paths follow the same high-level sequence.

```text
Process Opname Request or Reservation
        |
        v
Load and validate source aggregate
        |
        v
Reject active Admission or active legacy registration
        |
        v
Validate registration inputs (including ProsedurMasukInapId)
Resolve ProsedurMasukInap via IProsedureMasukInapRepo
        |
        v
Resolve KelasDk and eligible Bangsal
        |
        v
AdmissionModel.Admit(...)
        |  creates RegId
        v
RegFactory.CreateRegInapFromAdmission(...)
        |  reuses Admission.RegId
        v
RegInapModel.Create(...)
        |  same RegId, resolved ProsedurMasukInap, selected doctor as Primary DPJP
        v
RegAktifModel.CreateFromReg(reg)
        |
        v
Fulfill Opname Request or realize Reservation
        |
        v
Save all records in one transaction
```

The persisted records are:

1. `BILRG_AdmAdmission`
2. `ta_registrasi` through `RegRepo` and `RegDto` (including guarantor via `ta_reg_jaminan`)
3. `ta_reg_inap` and `ta_reg_history_dokter` through `IRegInapRepo` / `RegInapRepo`
4. `BILRG_RegAktif` through `RegAktifRepo`
5. Source aggregate state transition (Opname fulfill / Reservation realize)
6. Audit log rows

### Persistence ownership (do not conflate)

| Concern | Table(s) | Used by |
|---------|----------|---------|
| Registration header | `ta_registrasi` | All jenis reg |
| Registration components (karcis breakdown) | `ta_registrasi2` | **Rawat Jalan and IGD only** |
| Inpatient extension | `ta_reg_inap` | Rawat Inap |
| Guarantor | `ta_reg_jaminan` | Via `RegRepo` for all jenis that persist jaminan |
| Doctor history / DPJP | `ta_reg_history_dokter` | Rawat Inap via `RegInapRepo` |

**Confirmed rule:** Rawat Inap does **not** create `ta_registrasi2`. An empty `ta_registrasi2` result after successful inpatient registration is **expected behavior**, not a persistence defect. Do not add komponen rows for inpatient, and do not treat their absence as a gap.

Write order inside `TransHelper.NewScope()`:

1. Admission
2. Registration and guarantor (`RegRepo` clears any stray `ta_registrasi2` for inpatient; does not insert komponen)
3. `RegInapModel` (`ta_reg_inap` + doctor history)
4. RegAktif
5. Source fulfillment or realization
6. Audit records

If any construction or persistence step fails — including `RegInapRepo.SaveChanges` — the ambient transaction must not leave an Admission without its Registration, and must not leave an inpatient Registration without its `ta_reg_inap` extension.

## Domain Factory Rules

`IRegFactory.CreateRegInapFromAdmission(...)` is the only intended constructor path for a RegModel created by the new Admission flow.

It must:

- use `admission.RegId`;
- never call the legacy registration sequencer;
- set `JenisReg = JenisRegEnum.RegInap`;
- set persisted `RegModel.Kelas` to `KelasType.Default.ToReff()`;
- keep legacy Bed data empty/default because `ta_registrasi` does not own the new Admission placement;
- copy `admission.KelasDk` and `admission.Bangsal` into transient RegModel enrichment properties;
- apply insurance, policy, entry method, referral, doctor, Layanan, Karcis, and eligibility;
- assign only an inpatient Layanan.

`RegModel.AssignInpatientVisitTo(...)` enforces:

- `Layanan.InstalasiDk == InstalasiDkType.RawatInap`;
- the selected `Karcis` supports the selected Layanan;
- doctor, Layanan, and Karcis are populated;
- `ListKomponen` stays empty (Rawat Inap does not own `ta_registrasi2`).

The existing outpatient / IGD `AssignVisitTo(...)` behavior must remain unchanged (it still builds `ListKomponen` from karcis for Rawat Jalan and IGD).

## Legacy Compatibility

The legacy persistence shape is protected:

- do not add columns to `ta_registrasi` for Admission-only fields;
- do not change `RegDto` write parameters to persist `KelasDk` or `Bangsal`;
- do not make legacy code depend on an Admission navigation property;
- do not replace `RegModel.Kelas` with `KelasDkType`.

### `ta_reg_inap` read compatibility

- Historical inpatient `ta_registrasi` rows may lack a `ta_reg_inap` extension. `IRegInapRepo.LoadEntity` returns `MayBe.None` in that case and must not crash unrelated registration reads.
- New-system Admission processing always writes `ta_reg_inap` (and Primary DPJP history in `ta_reg_history_dokter`) in the same transaction as Registration.
- Do not invent missing procedure or doctor values when rehydrating.

`RegModel` has read-side enrichment fields:

```csharp
public KelasDkType KelasDk { get; init; }
public BangsalReff Bangsal { get; init; }
```

`RegRepo.LoadEntity(...)` enriches these fields without changing the legacy write shape:

- resolve the full `KelasType` from `IKelasRepo` and copy `KelasType.KelasDk`;
- for `RegInap`, resolve Bangsal from `RegModel.Layanan` through `IBangsalRepo.ListData(ILayananKey)`;
- require exactly one Bangsal for legacy Admission synchronization;
- allow default `KelasDk` when the new-system Registration intentionally persisted an empty legacy `Kelas`.

`AdmissionModel.CreateFromLegacyRegistration(reg, userId)` remains the validation boundary for the reverse direction. It accepts only `JenisRegEnum.RegInap` and rejects missing/default `KelasDk` or `Bangsal`.

## Source Tracking

Every Admission records who initiated the shared identity:

```csharp
public enum AdmissionSourceEnum
{
    Admission = 0,
    Legacy = 1
}
```

- `Admission` means the new Admission flow generated `RegId`.
- `Legacy` means an existing legacy `RegModel` generated `RegId` and the new system synchronized from it.

Existing `BILRG_AdmAdmission` rows default to `Admission`.

## Invariants for AI Changes

When modifying this feature, preserve these invariants:

- New Admission and new RegModel have exactly one shared `RegId`.
- `AdmissionModel` remains the source of `RegId` for new-system creation.
- Legacy RegModel persistence remains backward compatible.
- Only `JenisRegEnum.RegInap` can create or synchronize an Admission.
- A patient cannot receive a new Admission while an active Admission or active legacy registration exists.
- Opname Request fulfillment and Reservation realization occur in the same transaction as Admission and Registration creation.
- Domain rules stay in `AdmissionModel` and `RegModel`; the orchestrator coordinates them.
- Repositories persist aggregates; they do not initiate cross-aggregate workflows.
- Do not introduce domain events solely to replace this direct transactional orchestration.

## Current Scope and Deferred Work

The orchestrator currently creates Admission, legacy Registration, inpatient extension (`RegInapModel`), and active-registration state. Deferred capabilities that are still out of scope:

- billing records;
- journals;
- tindakan;
- EMR queue entries;
- bed or room assignments.

Those capabilities may be added as separate application steps, but they must preserve the same transaction and shared `RegId` rules where atomicity is required.

## AI Implementation Guidance

Before changing this feature, read this artifact together with:

- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/ENGINEERING.md`
- `docs/DATABASE.md`

When adding another Admission creation endpoint:

1. Add endpoint-specific source loading and source-state validation only where needed.
2. Reuse the orchestrator's registration construction and transaction boundary.
3. Add a dedicated orchestrator entry point or a small source adapter; do not copy the existing handler workflow.
4. Add tests proving shared `RegId`, source transition, rollback behavior, and legacy persistence compatibility.

When implementing legacy-to-Admission synchronization:

1. Load `RegModel` through `IRegRepo` so read-side enrichment is available.
2. Exit without creating Admission when `JenisReg != RegInap`.
3. Call `AdmissionModel.CreateFromLegacyRegistration(reg, auditUserId)`.
4. Make synchronization idempotent by checking whether the Admission for `RegId` already exists.
5. Persist the Admission with `AdmissionSourceEnum.Legacy` in the application transaction.

## Implementation References

- `src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IRegInapRepo.cs`
- `src/bilreg/Bilreg.Domain/AdmisiContext/RegFeature/RegFactory.cs`
- `src/bilreg/Bilreg.Domain/AdmisiContext/RegFeature/RegModel.cs`
- `src/bilreg/Bilreg.Domain/AdmisiContext/RegFeature/RegInapModel.cs`
- `src/bilreg/Bilreg.Domain/AdmisiRanapContext/AdmissionFeature/AdmissionModel.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegInapRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/ta_reg_inap_dal.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/AdmissionDto.cs`
- `docs/contexts/admisi-ranap/ta-reg-inap-persistence-contract.md`

## Verification References

Focused tests cover:

- Admission source preservation;
- legacy Registration enrichment;
- inpatient factory identity and validation;
- inpatient factory leaving `ListKomponen` empty (no `ta_registrasi2` model data);
- handler delegation;
- Opname Request / Reservation orchestration;
- shared `RegId` across Admission, RegModel, RegInapModel, and RegAktifModel;
- `ta_reg_inap` DAL insert/get/update/delete;
- `RegInapRepo` idempotent save and load, including Primary DPJP history;
- RegInap persistence failure rolling back the complete workflow;
- `RegRepo.SaveChanges` never inserting `ta_registrasi2` for Rawat Inap while preserving Rawat Jalan / IGD komponen replace behavior.

The affected solution must build successfully after changes.
