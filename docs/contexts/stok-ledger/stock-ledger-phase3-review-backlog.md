# Stock Ledger Phase 3 Review Backlog

## Purpose

This document tracks cross-slice engineering findings that must be remembered during the remaining Phase 3 implementation.

It is **not** a bug list.

It is **not** a TODO list.

It contains only implementation-review findings that affect future slices.

---

## R-001

Source:
P3-S1 Review

Status:
Open

Target Slice:
P3-S4

Priority:
Required

Description:

Baseline reconstruction does not yet establish the complete Legacy Change Discovery identity-key set. Until that set exists, fingerprint mismatch correctly fails closed to scoped re-derive and cannot rely on set-diff for void/update classification.

Required Action:

Ensure the first synchronization establishes the complete baseline identity set before relying on set-diff for incremental discovery.

Resolution:

—

---

## R-002

Source:
P3-S1 Review

Status:
Open

Target Slice:
P3-S4

Priority:
Required

Description:

Discovery identity keys embed fingerprint material into `BILRG_StokSourceIdempotency.IdempotencyKey`, which is currently sized for shorter keys. Catch-up persistence of the P3-S1 key format risks truncation or insert failure at production field widths.

Required Action:

Confirm durable storage capacity for the discovery identity-key format before catch-up writes those keys, and adjust persistence only if capacity is insufficient.

Resolution:

—
