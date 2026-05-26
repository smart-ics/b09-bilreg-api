# operational-events.md — Event terminology

The codebase uses **"event"** for several different concepts. Use this glossary before implementing or prompting about event behavior.

---

## 1. IGD operational timeline (`IgdVisitEvent`)

| | |
|--|--|
| **Documentation** | [`docs/contexts/igd/igd-02-domain.md`](../contexts/igd/igd-02-domain.md) — DOMAIN EVENT |
| **Code** | `Bilreg.Domain/IgdContext/IgdVisitFeature/IgdVisitEventType.cs` |
| **Persistence** | `Bilreg.Infrastructure/IgdContext/IgdVisitFeature/IgdVisitEvent*` |
| **SQL** | `Bilreg.SqlDb/IgdContext/IgdVisitFeature/BILRG_IgdVisitEvent.sql` |

**Purpose:** Operational audit trail and activity timeline for an IGD visit. **Not** event sourcing.

| Event kind (code) | Typical use |
|-------------------|-------------|
| `TRANSFER_BED` | UC04b — pindah bed dalam observasi; satu transaksi dengan dual-write bed + `PakaiBed` |

---

## 2. Queue / tracker timeline (`PasienTrackerEvent`)

| | |
|--|--|
| **Documentation** | *(no dedicated admisi domain doc yet)* |
| **Code** | `Bilreg.Domain/AdmisiContext/AntrianFeature/PasienTrackerEventType.cs` |
| **Persistence** | `Bilreg.Infrastructure/AdmisiContext/AntrianFeature/PasienTrackerEvent*` |
| **SQL** | `Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_PasienTrackerEvent.sql` |

**Purpose:** Operational tracking events for patient queue / antrian workflow.

When adding Admisi documentation, use `docs/contexts/admisi/` with a prefixed filename (e.g. `admisi-domain.md`), not another root `DOMAIN.md`.

---

## 3. Compliance audit (`AuditLog` / `AuditLogEventSource`)

| | |
|--|--|
| **Documentation** | [`docs/shared/audit-log.md`](../shared/audit-log.md) |
| **Code** | `Bilreg.Domain/Shared/AuditLogFeature/` |

**Purpose:** Append-only cross-cutting audit rows (`BILRG_AuditLog`). Explicit creation from use-cases — not middleware or domain events.

`AuditLogEventSource` names which `AuditTrailType` slot supplied actor/time for overload C — it is **not** a domain event bus.

---

## 4. Domain events (architecture)

| | |
|--|--|
| **Documentation** | [`docs/ENGINEERING.md`](../ENGINEERING.md) §14 Domain Event Philosophy |
| **Code** | Optional; prefer direct orchestration |

**Purpose:** Architectural stance only. This project avoids event-driven overengineering and event sourcing.

---

## Quick decision

| You need… | Read / use |
|-----------|------------|
| IGD patient activity history | `docs/contexts/igd/igd-02-domain.md` + `IgdVisitEvent*` |
| Antrian / tracker history | `PasienTrackerEvent*` (+ future admisi doc) |
| Who changed what for compliance | `audit-log.md` + `AuditLog.Create` |
| Async integration between aggregates | Orchestration in use-cases, not a generic event framework |
