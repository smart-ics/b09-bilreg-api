# R-11 — HiDok Self-Registration Assistance Fallback

## Outcome

Bilreg now exposes `POST /api/v1/admission-queue/booking-assistance` as a narrow ensure operation.
The request accepts BookingId, target ServicePointId, an optional existing failure code, KioskId, and
the current UserId audit field. The backend derives `BOOKING-ASSISTANCE:{BookingId}`; HiDok does not
create a correlation ID or manage queue state.

## Business deduplication

`BILRG_AdmBookingAssistance` is keyed by BookingId and uniquely stores the derived correlation. It
retains the Queue Entry identity, optional failure code, kiosk context, and audit data. An active row
is returned before allocation. Concurrent creation losers roll back their attempted Queue Entry and
reload the winner. Generic `ClientRequestId` is not used.

The correlation becomes inactive when its Queue Entry is withdrawn, redirected, or completed with a
Registration Outcome. A later explicitly invoked fallback conditionally replaces that inactive
latest-state row; an active row can never be replaced.

## Atomic creation

Target session creation, Queue Entry insertion, and correlation insertion run in one local transaction.
Queue numbering remains owned by bounded `ISequencer`; consumed values may gap when a transaction
loses a race or rolls back. Both new and existing responses include QueueLabel.

## HiDok source inventory and external task

The available repositories contain Bilreg’s HiDok Booking create/delete endpoints and mappings, but
not the HiDok Self-Registration application or its definitive assistance-required failure branch.
No existing failure-code payload or output/printing call site could be confirmed. Therefore no
speculative change was made to a success path or generic exception handler.

The remaining HiDok-owned change is external and deliberately small:

1. invoke the new endpoint only from its existing definitive assistance-required branch;
2. do nothing on successful self-registration or generic transient failures;
3. print/display QueueLabel using its existing output mechanism; and
4. treat `Existing=true` the same as a newly created entry.

## Verification

Focused tests cover first fallback creation and derived correlation, retry without reallocation, and
concurrent-loser reload. Database integration tests still require the R-06 through R-11 migrations
and must prove rollback and real unique-key races before rollout. The rebuilt focused Admission Queue
suite passes 42 of 42 tests; the solution build completes with no errors.
