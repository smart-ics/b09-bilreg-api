# ADR-003 — Booking Schedule References

**Status:** Accepted  
**Date:** 2026-06-23  
**Context:** JadwalPraktekHarian architecture

## Context

`BILRG_Booking` stores doctor, visit date, and practice time but not which schedule slot was active at booking time. When templates change or daily overrides are applied, delete/quota workflows re-resolve from current template state—causing drift from the schedule the patient actually booked against.

## Decision

Add two **nullable** columns to `BILRG_Booking`:

| Column | Purpose |
|--------|---------|
| `JadwalPraktekId` | Lineage—which recurring template slot originated the booking |
| `JadwalPraktekHarianId` | Execution—which persisted daily schedule was active (NULL when resolved from template or synthetic) |

Populate both from `JadwalPraktekEffective` at booking creation. **No full schedule snapshot** on the booking row—doctor, date, and time already stored suffice for display.

Delete booking workflow uses stored IDs first; falls back to resolver when NULL (legacy bookings).

## Consequences

- **Positive:** Stable re-resolution for delete, quota, and audit without re-deriving from current template.
- **Positive:** Additive migration—nullable columns, no backfill required.
- **Constraint:** HiDok Effective Schedule API must not expose these internal IDs in its public response.
- **Future:** A "Move Existing Bookings" workflow is explicitly out of scope for v1; override when bookings exist is blocked.
