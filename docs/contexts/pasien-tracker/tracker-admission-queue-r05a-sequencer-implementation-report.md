# Patient Tracker — R-05A Sequencer Implementation Report

**Roadmap item:** R-05A — Verify bounded `ISequencer` allocation

**Status:** Complete

**Completed:** 2026-07-23

## 1. Investigation result

The existing `Sequencer` used one SQL Server sequence named `sq_{SequenceTag.ToLower()}` and allocated through `NEXT VALUE FOR`. SQL Server sequence allocation is atomic across concurrent sessions and values are consumed outside transaction rollback, so uniqueness is supported and gaps are expected.

The original admission behavior was not release-ready:

- lazy `CREATE SEQUENCE` had no `MINVALUE`, `MAXVALUE`, or explicit `NO CYCLE` clause;
- two first callers could both observe a missing sequence and race to create it;
- exhaustion was not translated to a business-readable failure; and
- admission and non-admission consumers shared only the unbounded method.

## 2. Canonical Admission Queue SequenceTag

The existing codebase convention remains authoritative:

```text
AN{BusinessDate:yyMMdd}{StartTime:HHmm}_{ServicePointCode}
```

Pragmatic V1 admission sessions always start at `00:00`, producing:

```text
AN{BusinessDate:yyMMdd}0000_{ServicePointCode}
```

Examples:

```text
AN2607230000_BPJS
AN2607230000_UMUM
AN2607240000_BPJS
```

The existing `AntrianModel.GenSequenceTag(DateOnly, TimeOnly, ServicePointType)` and `AntrianFactory.Create(ServicePointType, DateOnly)` remain the single tag construction path.

## 3. Implementation

- Added bounded `ISequencer.GetNextNoUrut(sequenceTag, maxValue)`; no allocator was added.
- Admission allocation calls the overload with maximum `9999`.
- Serialized lazy create/configuration with `sp_getapplock` owned by the SQL transaction.
- Created new admission sequences as `INT`; configured new or existing admission sequences with `INCREMENT BY 1`, `MINVALUE 1`, `MAXVALUE 9999`, and `NO CYCLE` while preserving an existing sequence's SQL data type.
- Used parameterized sequence metadata and `QUOTENAME` for dynamic SQL identifiers.
- Preserved `NEXT VALUE FOR` as the allocation authority.
- Translated SQL exhaustion to `SequenceExhaustedException`.
- Added defensive Domain rejection for any returned value outside 1–9999.
- Routed anonymous intake and legacy admission create-on-registration through explicit `AddAdmissionEntry` behavior.
- Left existing unbounded sequencer consumers unchanged.

No `LastQueueNumber`, counter table, secondary allocator, or cycling/restart behavior was introduced.

## 4. Concurrency and gap behavior

The application lock serializes first-create/configuration for one physical SQL sequence. Once recognized, `NEXT VALUE FOR` supplies atomic unique values. Different canonical tags remain independent.

Sequence consumption is non-transactional. A value consumed by a request that later rolls back may be absent from Queue Entries. This gap is accepted; numbers must be unique and bounded, not contiguous.

Allocation of 9999 succeeds. The next allocation raises `SequenceExhaustedException`; the implementation never restarts, cycles, creates a same-day replacement session, or emits 10000.

## 5. Verification

Focused command:

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore --filter "FullyQualifiedName~AdmissionQueueCompleteTest|FullyQualifiedName~AdmissionQueueNumberAllocationTest|FullyQualifiedName~SequencerAdmissionAllocationTest|FullyQualifiedName~QueAnonymousIntakeHandlerTest|FullyQualifiedName~AdmissionQueueLifecyclePersistenceTest|FullyQualifiedName~AdmissionQueueRegistrationResolverTest"
```

Result:

```text
Passed:  19
Failed:  0
Skipped: 0
Total:   19
```

Real SQL Server tests verify:

- first allocation and sequence metadata;
- allocation of 9999 followed by explicit exhaustion;
- 24 concurrent allocations during lazy sequence creation, producing unique 1–24; and
- independent allocation for Service Point and Business Date tags.

Domain tests verify canonical tag use, accepted gaps, 9999 acceptance, and defensive rejection above 9999.

Existing compiler warnings remain outside R-05A; there were no build or test failures.

### 5.1 Broader Antrian feature qualification

The broader `FullyQualifiedName~AdmisiContext.AntrianFeature` selection was also executed after correcting the existing legacy create-on-registration mock for the bounded overload:

```text
Passed:  156
Failed:  30
Skipped: 0
Total:   186
```

All R-05A and Admission Queue tests in that run passed. The remaining failures are confined to existing `AntrianMapModelTest` seeding expectations and `AntrianMapDalTest` null fixture data; none of their production or test files is changed by R-05A. They remain an existing broader-suite qualification and are not silently included in this slice.

## 6. R-05B deferral

No `ClientRequestId` code, column, index, API field, or lookup was added. Current Kiosk V1 intake remains non-idempotent because the client does not yet persist and reuse one request identity for an uncertain interaction.

Reliable retry deduplication requires an end-to-end Kiosk reliability contract and is deferred as non-blocking R-05B.
