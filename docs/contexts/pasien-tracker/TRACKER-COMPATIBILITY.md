# Patient Tracker — Legacy Compatibility Contract

**Artifact status:** Transitional compatibility specification  
**Bounded context:** Patient Tracker / Admisi Antrian  
**Authoritative domain:** [`TRACKER-DOMAIN.md`](TRACKER-DOMAIN.md)  
**Related gap:** F-13 in [`tracker-codebase-gap-report.md`](tracker-codebase-gap-report.md)

## 1. Purpose

During V1 rollout, physician queue numbers must remain compatible with the legacy desktop schema (`ta_no_antrian_map*`) and external EMR payloads while the canonical Queue Session (`BILRG_Antrian*`) becomes the durable owner of queue identity and service milestones.

This document defines the **authority map**, **identity crosswalk**, and **adapter boundary** so dual-write paths do not reinterpret legacy fields as canonical Tracker or queue lifecycle truth.

## 2. Authority map

| Concern | Canonical owner (Tracker domain) | Legacy projection | Transition rule |
|---|---|---|---|
| `TrackerId` | `PasienTracker` / `BILRG_PasienTracker` | not stored in map or EMR today | never encode into `BookingId` / `RegId` |
| Queue number uniqueness | Queue Session (`BILRG_AntrianEntry` per `AntrianId`) | `ta_no_antrian_map.fn_no_antrian` | reserve once, project to both schemas in one transaction |
| Queue milestones (`Waiting` / `In Service` / `Done`) | `AntrianEntryModel` | **not equivalent** to `fb_terpakai`, `fs_flag`, `fn_status_antrian` | never map occupancy flags to canonical status |
| Source reference | Tracker Events + queue `ReffId` / `ReffDesc` | `fs_kd_trs_gen` on map detil | map may update Booking→Reg; Tracker evidence stays append-only |
| Number allocator (today) | legacy `AntrianMap` via compatibility adapter | same | flip via `QueueNumber:Authority` when cutover is approved |
| Number allocator (future) | Queue Session + sequencer | map becomes projection-only | requires parity metrics and desktop/EMR sign-off |

## 3. Identity crosswalk

One physician reservation links these identities:

```text
(Jadwal + SessionDate + JamJadwal, NoUrut)   -- legacy map header/detil key
        ↔  (AntrianId, NoUrut)               -- canonical Queue Session entry
        ↔  PasienTrackerId                   -- logical journey (not on map row)
        ↔  BookingId or RegId                -- source transaction reference
```

Rules:

1. `TrackerId` is logical journey identity; it is not a replacement for `BookingId` or `RegId`.
2. Legacy map rows carry patient/source IDs for desktop display; they do not own journey evidence.
3. EMR outbound today carries source ID + number; `TrackerId` is a follow-up integration item.

## 4. Non-equivalence: `IsTerpakai` vs Waiting / In Service / Done

| Legacy (`AntrianMap` detil) | Meaning | Canonical (`AntrianEntry`) |
|---|---|---|
| `IsTerpakai = true` | slot occupied / reserved for a patient | entry may still be **Waiting** |
| `Flag` (UMUM/BPJS/AUTO) | allocation class | not a service milestone |
| `ReffId` (`fs_kd_trs_gen`) | BookingId or RegId correlation | provenance, not lifecycle |
| `fn_status_antrian` | legacy column, not mapped in Bilreg | do not use |

Valid combined state after booking:

- map: `IsTerpakai = true`, `ReffId = BookingId`
- queue entry: `Status = Waiting`, `ServedAt` / `DoneAt` absent

Service start is recognized only by `QueMulaiPeriksa` → `AntrianEntryModel.Serve`, not by map flags.

## 5. Compatibility adapter

Application port: `IQueueNumberCompatibilityAdapter` (`Bilreg.Application/AdmisiContext/AntrianFeature`).

Responsibilities:

| Operation | Behavior |
|---|---|
| `ReserveForBooking` / `ReserveForRegistration` | legacy map picks `NoUrut` (default authority); returns `ReservedQueueNumber` |
| `ProjectIntoQueueSession` | `AntrianModel.AddEntry(reserved.NoUrut, …)` with uniqueness guard |
| `Release` | void legacy slot; does not delete Tracker evidence (BR-TRK-009c) |
| `ProjectSourceReffForRegistration` | update map `ReffId` Booking→Reg on reg-by-booking |
| `AcceptExternalNumber` | Hidok exception: queue session only, no map projection |

All reserve + project operations for Booking/Reg/Walk-in/Ubah kunjungan run inside the handler's existing database transaction. If `AddEntry` fails, map occupancy rolls back with the transaction.

## 6. Configuration and cutover

```json
"QueueNumber": {
  "Authority": "LegacyMap"
}
```

| Value | Meaning |
|---|---|
| `LegacyMap` (default) | transitional allocator; dual-write to map + Queue Session |
| `QueueSession` | reserved for future cutover; **not enabled** in V1 — adapter rejects until parity is proven |

Future cutover checklist (not in scope for F-13 implementation):

1. Enable `QueueSession` in non-production and measure dual-write parity.
2. Prove desktop/EMR consumers tolerate canonical-first allocation.
3. Set `CutoverAt` per facility or globally.
4. Retire map-as-allocator only after rollback procedure is documented.

## 7. Exception paths

| Path | Map write | Queue Session write | Notes |
|---|---|---|---|
| Booking create (local) | yes | yes | via adapter |
| Walk-in registration | yes | yes | via adapter |
| Reg by booking | reff projection only | retag `BOK`→`REG` | reuses booking number |
| Hidok booking create | no | yes | external number; documented exception |
| `AntrianGenNewNumber` | no | yes | sequencer-only API |

## 8. Related artifacts

- Domain rules: [`TRACKER-DOMAIN.md`](TRACKER-DOMAIN.md) BR-TRK-026–039, BR-TRK-009c
- Gap assessment: [`tracker-codebase-gap-report.md`](tracker-codebase-gap-report.md) §6.3, F-13
- Implementation summary: [`tracker-f13-implementation-report.md`](tracker-f13-implementation-report.md)
- Legacy repair (separate concern): F-14 Taksaka / `ReffId` rewrite
