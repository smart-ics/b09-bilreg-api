# ADR-002 — Manual Override Independence

**Status:** Accepted  
**Date:** 2026-06-23  
**Context:** JadwalPraktekHarian architecture

## Context

When an admin overrides a single day's practice schedule (time change, substitute doctor, cancellation), that change is incidental—not a permanent shift in the weekly template. If template edits automatically propagated to daily rows, operational data (bookings, queue maps) would drift unpredictably.

## Decision

Once a persisted daily schedule has `Source = MANUAL`, it is **fully independent** of future template modifications. The optional `JadwalPraktekId` on the daily row is **traceability only**—not a live sync link. Template create/update/delete commands must never mutate `Source = MANUAL` daily rows.

`Source = GENERATED` rows (optional future batch) may be invalidated by an explicit regeneration command only; `MANUAL` rows are never touched by template edits or batch regeneration.

## Consequences

- **Positive:** Operators can trust that a confirmed daily override will not change when someone edits the weekly template.
- **Positive:** Resolver logic is simple: if an ACTIVE daily row exists for the natural key, use it—never re-derive from template.
- **Trade-off:** Template and daily state may diverge by design; reporting must use effective schedule resolution, not template alone.
- **Operational:** Override and cancellation are blocked in v1 when non-voided bookings or antrian entries exist for the session.
