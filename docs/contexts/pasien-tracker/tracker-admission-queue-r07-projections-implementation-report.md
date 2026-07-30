# R-07 — Queue-Only Operational Projections

## Outcome

R-07 adds internal Application queries and direct Infrastructure SQL for the Queue Officer
Worklist and Current Loket Display Snapshot. No production API endpoint is exposed; exposure remains
deferred to R-10/R-14.

## Contracts

- Officer worklist filters by Business Date, Service Point, Queue state, and Loket, with bounded
  paging. Default ordering is Priority descending, creation time, then queue number.
- Priority is returned and used only for ordering. The projection has no mutation, selection, or
  claim operation.
- Current Loket snapshot reloads every active persisted row, optionally for one Loket, including
  display state, `AnnouncementVersion`, and `RowVersion`.
- Queue labels use the immutable session snapshot. Historical sessions return a null label.
- Contracts expose queue-owned state only. They contain no patient name, physician, Registration,
  eligibility, or direct cross-domain DAL enrichment. `PasienTrackerId` is the queue-owned optional
  association reference, not Patient enrichment.

## Persistence prerequisites delivered

The accepted R-03/R-04 read schema required by these projections is included additively:

- Queue Entry Priority, CreationReason, CallCount, and redirect source pair;
- operational worklist index;
- authoritative `BILRG_AdmLoketCurrentCall` table, active-entry uniqueness, RowVersion,
  AnnouncementVersion, and active-display index.

These migrations establish projection storage only. R-08 remains responsible for transactional
claim mutations and lifecycle orchestration.

## Notification and recovery boundary

R-07 reads persisted state and supplies `QueueAnnouncementPolicy`, which permits audio only when a
reloaded version is greater than the last processed version. Reconnect, refresh-hint, and polling
all invoke the same snapshot query. SignalR publishing is intentionally not introduced before the
R-08 write transactions exist; R-08 must publish a best-effort refresh only after commit.

## Verification

- Project build passes with existing API warnings.
- Focused tests cover field boundaries, Priority ordering without selection, announcement-version
  comparison, and repeated authoritative reload behavior.
- SQL integration tests require applying the new R-03/R-04 migrations to the integration database.

## Deployment order

1. Apply `BILRG_AntrianEntry_M2_QueueOperations_Alter.sql`.
2. Apply `BILRG_AdmLoketCurrentCall.sql`.
3. Deploy the internal query contracts and projection reader.
4. Keep HTTP exposure disabled until R-10/R-14.
