# Patient Tracker — R-04 Cross-Session Active Loket Claim Contract

**Status:** Accepted implementation contract

**Decision date:** 2026-07-23

**Roadmap item:** R-04 — Define the cross-session active Loket claim

**Governing artifacts:** [Domain](./TRACKER-ADMISSION-QUEUE-DOMAIN.md), [Architecture](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md), [Implementation roadmap](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-ROADMAP.md)

## 1. Decision

`BILRG_AdmLoketCurrentCall` is both:

- the authoritative latest current-call projection for passive display recovery; and
- the database-coordinated active ownership claim for one configured physical `LoketKey`.

One `LoketKey` may own at most one active Queue Entry across all Service Points and Queue Sessions. One Queue Entry may be actively owned by at most one `LoketKey`. `Outstanding` and `InService` are both active claim states.

Queue Entry mutation and claim mutation occur inside the same local SQL Server transaction. Conflicts produce `AdmissionQueueConcurrencyException`, which the existing API error middleware maps to HTTP `409 Conflict`.

No Loket master, generic lock table, in-memory lock, lease timeout, or automatic stale-claim expiration is introduced.

## 2. Claim state

Persist `ClaimState` as `INT`:

| Value | Name | Active ownership | Meaning |
|---:|---|---|---|
| 0 | Released | No | No Queue Entry is currently owned by this Loket. The row remains as latest-state/audit evidence and may be replaced by a later Call. |
| 1 | Outstanding | Yes | The Waiting Queue Entry has been called and is owned by this Loket, but service has not started. |
| 2 | InService | Yes | The call was acknowledged and Registration Assistance is in progress at this Loket. |

`ClaimState` is current operational state, not a call-attempt history. A Released row is excluded from active-claim and active-display queries.

## 3. State matrix

| Operation | Required Queue Entry state | Required claim state | Queue Entry result | Claim result | AnnouncementVersion | Conflict conditions |
|---|---|---|---|---|---|---|
| Call | Waiting and not actively claimed | No active row for Queue Entry; target Loket absent or Released | Remains Waiting; CallCount + 1 | Acquire/replace as Outstanding | +1 when audio requested | Loket active; entry active elsewhere; entry not Waiting; stale expected entry state |
| Recall | Waiting | Outstanding at same Loket and same entry | Remains Waiting; CallCount + 1 | Retain Outstanding | +1 when audio requested | Different Loket/entry; claim not Outstanding; entry not Waiting; stale RowVersion |
| Start Service | Waiting | Outstanding at same Loket and same entry | InService; set ServedAt | Retain and change to InService | unchanged | Missing/mismatched call; entry not Waiting; stale RowVersion/CAS |
| No-Show | Waiting | Outstanding at same Loket and same entry | Remains under the separately approved no-show disposition policy; R-04 does not choose Waiting versus Withdrawn | Release | unchanged | Claim/entry mismatch; entry not Waiting; stale RowVersion/CAS |
| Withdraw | Waiting | Outstanding at same Loket when called; no claim is also permitted for an uncalled entry | Withdrawn | Release matching active claim, if any | unchanged | Different active claim; entry not Waiting; stale RowVersion/CAS |
| Redirect | Waiting | Outstanding at same Loket when called; no claim is also permitted for an uncalled entry | Origin Withdrawn; create target Priority entry | Release origin claim, if any | unchanged | Origin transition or release conflict; target creation conflict; any partial operation rolls back |
| Complete | InService | InService at same Loket and same entry | Done; set DoneAt | Release | unchanged | Outcome/entry mismatch; entry not InService; stale RowVersion/CAS |
| Rollback to Waiting | InService | InService at same Loket and same entry | Waiting; clear/reconcile service milestone under the owning recovery use case | Release | unchanged | Claim/entry mismatch; stale RowVersion/CAS |
| Controlled administrative recovery | Waiting or InService as explicitly verified by recovery use case | Matching Outstanding or InService claim | No implicit entry transition; recovery must state and audit its reconciliation action | Release | unchanged | Expected entry/claim/version mismatch |
| Automatic expiration | Any | Any | Prohibited | Prohibited | N/A | Age alone never authorizes release |

### 3.1 Acquire, retain, replace, and release definitions

- **Acquire:** insert a new row for a never-seen Loket or conditionally update its Released row to Outstanding.
- **Retain:** conditionally update the same active row while preserving `LoketKey`, `AntrianId`, and `NoUrut`.
- **Replace:** reuse a Released row for a new Queue Entry. Replacing an Outstanding or InService row is prohibited.
- **Release:** conditionally change the matching active row to Released. Do not delete it and do not increment AnnouncementVersion.

## 4. Persistence contract

### 4.1 Table

```sql
CREATE TABLE BILRG_AdmLoketCurrentCall
(
    LoketKey            VARCHAR(50) NOT NULL,
    AntrianId           VARCHAR(26) NOT NULL,
    NoUrut              INT NOT NULL,
    ClaimState          INT NOT NULL,
    IsActive            BIT NOT NULL,
    AnnouncementVersion BIGINT NOT NULL,
    CalledAt            DATETIME NOT NULL,
    ServiceStartedAt    DATETIME NOT NULL,
    ReleasedAt          DATETIME NOT NULL,

    CrtUser             VARCHAR(50) NOT NULL,
    CrtDate             DATETIME NOT NULL,
    UpdUser             VARCHAR(50) NOT NULL,
    UpdDate             DATETIME NOT NULL,
    VodUser             VARCHAR(50) NOT NULL,
    VodDate             DATETIME NOT NULL,

    RowVersion          ROWVERSION NOT NULL,

    CONSTRAINT PK_BILRG_AdmLoketCurrentCall
        PRIMARY KEY CLUSTERED (LoketKey),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_ClaimState
        CHECK (ClaimState IN (0, 1, 2)),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_ActiveState
        CHECK (
            (ClaimState = 0 AND IsActive = 0)
            OR (ClaimState IN (1, 2) AND IsActive = 1)),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_Entry
        CHECK (AntrianId <> '' AND NoUrut > 0),
    CONSTRAINT CK_BILRG_AdmLoketCurrentCall_AnnouncementVersion
        CHECK (AnnouncementVersion >= 0)
)
```

Use the project empty-date sentinel `3000-01-01` for a milestone that has not occurred. `VodUser`/`VodDate` remain at their normal empty values because release is a state transition, not a void. No database foreign key is added; `(AntrianId, NoUrut)` is validated by application orchestration and conditional SQL, consistent with `docs/DATABASE.md`.

`LoketKey` maximum length is 50 to match the current audit/user operational identifier convention and must be finalized with the workstation-configuration contract before migration approval. The database compares it using the database collation; the configuration adapter must submit one normalized stable value.

### 4.2 Uniqueness and indexes

```sql
CREATE UNIQUE INDEX UX_BILRG_AdmLoketCurrentCall_ActiveEntry
    ON BILRG_AdmLoketCurrentCall (AntrianId, NoUrut)
    WHERE IsActive = 1

CREATE INDEX IX_BILRG_AdmLoketCurrentCall_ActiveDisplay
    ON BILRG_AdmLoketCurrentCall (IsActive, LoketKey)
    INCLUDE (
        AntrianId, NoUrut, ClaimState, AnnouncementVersion,
        CalledAt, ServiceStartedAt, RowVersion)
```

The check constraint makes `IsActive` a persistence aid derived from the authoritative ClaimState: Released is inactive; Outstanding and InService are active. The primary key enforces at most one row/state per Loket. The filtered unique index enforces at most one active Loket per Queue Entry across all sessions. Released rows may retain duplicate historical entry references because they confer no ownership.

SQL Server unique-key errors caused by these two constraints are translated to the application concurrency conflict; they are not retried with a different business selection.

### 4.3 Version semantics

- `RowVersion` changes on every row update and is required for retain/release expected-state operations.
- `AnnouncementVersion` is a per-Loket monotonically increasing application value.
- Call and audio Recall increment `AnnouncementVersion` inside the mutation.
- Non-audio Recall, Start Service, release, screen reload, and polling do not increment it.
- Replacing a Released row for a later Call increments the prior value; it does not reset to zero.
- Initial Call inserts `AnnouncementVersion = 1` when audio is requested, otherwise `0`.

## 5. Explicit SQL operation contracts

DAL operations return affected-row/result codes. Application repositories expose narrow intent-specific methods; handlers translate a non-success result to `AdmissionQueueConcurrencyException`. Do not expose a generic claim or lock manager.

### 5.1 Acquire or replace on Call

Within the command transaction:

1. conditionally verify/update the Queue Entry as Waiting and increment CallCount;
2. try to update the target Loket row only where `ClaimState = Released`;
3. if no Loket row exists, insert it as Outstanding;
4. rely on the PK and filtered active-entry unique index for races;
5. if the Loket exists active, the Queue Entry is active elsewhere, or the Queue Entry CAS fails, throw concurrency and roll back all changes.

The acquire SQL must lock the target Loket key range while deciding update versus insert, using one explicit transaction and SQL Server locking such as `UPDLOCK, HOLDLOCK`. Do not use `MERGE` and do not perform a check on one connection followed by a write on another transaction.

### 5.2 Recall

Update only where all are true:

```text
LoketKey = expected LoketKey
AntrianId = expected AntrianId
NoUrut = expected NoUrut
ClaimState = Outstanding
RowVersion = expected RowVersion
```

Retain Outstanding, update audit/time, and increment AnnouncementVersion only for an audio recall. Increment the Queue Entry CallCount in the same transaction with its own expected-state predicate. Zero affected rows on either operation rolls back both.

### 5.3 Start Service

1. CAS Queue Entry from Waiting to InService and set ServedAt.
2. CAS the matching claim from Outstanding to InService using RowVersion.
3. Preserve AnnouncementVersion.
4. Commit only when both operations affect exactly one row.

Any ordering is acceptable inside the same transaction because failure throws and rolls back the earlier write. The repository contract must not expose a partially successful result.

### 5.4 Release

Release only the expected active claim:

```text
LoketKey = expected LoketKey
AntrianId = expected AntrianId
NoUrut = expected NoUrut
ClaimState = expected Outstanding or InService
RowVersion = expected RowVersion
```

Set `ClaimState = Released`, `IsActive = 0`, `ReleasedAt`, `UpdUser`, and `UpdDate`. Acquire/replace and retain operations set or preserve `IsActive = 1`. Preserve the entry identity, CalledAt, ServiceStartedAt, and AnnouncementVersion as latest-state evidence. Active display queries immediately exclude the row.

The matching Queue Entry business transition and release must both succeed in the same transaction. If a permitted Withdraw targets an uncalled entry, first prove no active claim exists for that entry; no release write is required.

### 5.5 Controlled administrative recovery

Recovery is an explicit future use case, not a scheduled cleanup. It must require:

- the exact Loket and Queue Entry identity;
- expected ClaimState and RowVersion;
- current Queue Entry state verification;
- an accountable reason and current audit convention; and
- a defined reconciliation outcome.

R-04 provides no “release older than” query or operation.

## 6. Transaction boundary

Use the existing application-owned `TransHelper.NewScope()` transaction pattern. Every connection opened by the explicit Dapper DAL operations must enlist in that ambient transaction.

The following are indivisible:

- Call: Queue Entry CallCount/current-call state + claim acquire/replace.
- Recall: Queue Entry CallCount + claim/version update.
- Start: Queue Entry Waiting→InService + claim Outstanding→InService.
- No-Show/Withdraw: Queue Entry disposition + matching claim release.
- Redirect: origin disposition + origin release + target entry creation.
- Complete: final queue completion preconditions + Queue Entry InService→Done + claim release.
- Rollback: Queue Entry recovery transition + claim release.

Best-effort SignalR refresh occurs only after commit and is never part of claim consistency.

## 7. Application and API conflict contract

Expected-state misses, stale RowVersion, active-Loket conflicts, active-entry conflicts, and relevant duplicate-key errors are business concurrency conflicts.

They must:

1. abort the local transaction;
2. raise `AdmissionQueueConcurrencyException` with the affected Loket/entry identity but no sensitive payload;
3. map to HTTP `409 Conflict` through the existing error middleware; and
4. instruct the client to reload the worklist/current state before making another operator-selected choice.

The server must not automatically choose or claim another Queue Entry.

## 8. Integration test matrix

Tests require real SQL Server behavior, not mocks only.

| Test | Expected result |
|---|---|
| Two sessions call different entries at the same Loket concurrently | One commit; one 409; one active row for Loket |
| Two Loket call the same Queue Entry concurrently | One commit; one 409 from active-entry uniqueness/CAS |
| Two Loket call different entries concurrently | Both commit |
| Call on an already active Loket | 409; existing claim unchanged |
| Recall with current RowVersion | CallCount and AnnouncementVersion increment atomically |
| Recall with stale RowVersion | 409; neither counter changes |
| Start with matching Outstanding claim | Entry and claim both become InService |
| Start after another transition | 409; no partial entry/claim change |
| Complete InService | Entry Done and claim Released in one commit |
| Release race with Recall | Exactly one wins; other receives 409 |
| Redirect failure while creating target | Origin disposition and claim release roll back |
| Exception after claim write before transaction completion | Claim and Queue Entry changes roll back |
| Long-running InService claim | Remains active; no age-based release |
| Controlled recovery with stale version | 409; claim remains active |
| New Call after explicit release | Released row is replaced; version continues monotonically |

## 9. Implementation allocation

This contract closes the R-04 design decision. Runtime delivery remains split across existing roadmap items:

- **R-03:** additive table/index migration, DTO/DAL/repository mapping, rollback script, and schema tests.
- **R-08:** Domain/Application Call, Recall, Start, No-Show, Withdraw, Redirect, Complete/recovery orchestration and API behavior.
- **R-07:** active current-display query and refresh/poll recovery.
- **R-12:** trusted LoketKey distribution and cross-workstation duplicate-configuration detection. R-04 prevents two active claims under one key; it cannot prove that two machines are not configured with the same key.

## 10. Deferred scope

- automatic stale-claim expiration;
- scheduled claim cleanup;
- Loket lease/heartbeat;
- Loket master or assignment model;
- generic locking infrastructure;
- detailed call-attempt history; and
- platform authentication/authorization changes deferred under R-02.
