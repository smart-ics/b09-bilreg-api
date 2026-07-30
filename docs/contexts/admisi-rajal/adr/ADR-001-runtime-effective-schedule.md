# ADR-001 — Runtime Effective Schedule

**Status:** Accepted  
**Date:** 2026-06-23  
**Context:** JadwalPraktekHarian architecture

## Context

Operational modules (booking, queue, walk-in, quota, reports, HiDok) currently resolve practice schedules by duplicating template lookup logic (`DayOfWeek` + `JamMulai` filtering) across six or more handlers. The business also requires date-specific overrides (leave, substitute doctor, one-day time changes) that a weekly template cannot represent without polluting the planning layer.

## Decision

Introduce `JadwalPraktekEffective` as a **runtime-only** value object projected by `IJadwalPraktekResolver`. It is never persisted as a table or aggregate root. All operational consumers migrate to depend exclusively on this projection.

Resolution order: ACTIVE daily row (MANUAL or GENERATED) beats template; template beats synthetic (walk-in only); cancelled schedules reject bookable operations.

## Consequences

- **Positive:** Single authority eliminates drift between booking, walk-in, and quota paths. External APIs (HiDok) see one stable shape without internal source distinctions.
- **Positive:** Planning (`JadwalPraktekType`) and execution (`JadwalPraktekHarianType`) remain separate aggregates with explicit lifecycles.
- **Negative:** Transitional period requires feature toggle (`EnableDailyScheduleResolver`) while handlers migrate.
- **Constraint:** Code review must reject new private schedule lookup methods outside the resolver and template admin commands.
