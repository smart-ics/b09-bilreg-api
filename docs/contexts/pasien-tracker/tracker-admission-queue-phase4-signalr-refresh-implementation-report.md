# Phase 4 — SignalR refresh-hint backend adapter

**Status:** Implemented in source (2026-07-23)  
**Plan:** [TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md) Phase 4  
**Prerequisite:** Phase 1 Slice 1 real-SQL gate; existing post-commit `IAdmissionQueueRefreshPublisher` call sites

## Outcome

Bilreg.Api now hosts a minimal authenticated SignalR hub and publisher behind
`IAdmissionQueueRefreshPublisher`. Committed Call/Recall/Start/Withdraw/No-Show/Redirect and
Registration Outcome finalization can emit a non-authoritative `RefreshHint`. Rollback/conflict
paths still do not publish. Disabling the adapter (`AdmissionQueueApi:SignalRRefreshEnabled=false`)
binds the no-op publisher and does not change queue write truth. Persisted
`GET /api/v1/admission-queue/displays/current` remains the recovery path.

## What shipped

### 1. Hub and wire contract

| Artifact | Role |
|----------|------|
| `AdmissionQueueRefreshHub` | `[Authorize]` hub at `/hubs/admission-queue` |
| `AdmissionQueueRefreshContracts` | Stable path + `RefreshHint` event name |
| `AdmissionQueueRefreshHint` | Payload `{ loketKey }` only |

### 2. Publisher adapter

| Artifact | Role |
|----------|------|
| `SignalRAdmissionQueueRefreshPublisher` | Implements `IAdmissionQueueRefreshPublisher`; `Clients.All`; swallows transport errors |
| `NullAdmissionQueueRefreshPublisher` | Retained for disable-mode and tests |

Existing Application handlers already publish only after `TransHelper.Complete()`; no handler changes
were required.

### 3. Composition

- `AddSignalR()` in presentation DI
- JWT `OnMessageReceived` accepts `access_token` query for the hub path
- `MapHub<AdmissionQueueRefreshHub>("/hubs/admission-queue")` in `Program.cs`
- DI selects SignalR vs Null publisher from `AdmissionQueueApi:SignalRRefreshEnabled` (default true)

### 4. Documentation

[`TRACKER-ADMISSION-QUEUE-API-V1.md`](./TRACKER-ADMISSION-QUEUE-API-V1.md) Display recovery section now
documents hub URL, event, payload, auth, disable flag, and non-authority rules.

## Explicitly not done (by design)

- Queue Display client, reconnect/polling UX, audio timing/clear rules
- Durable notification outbox
- Redis / SignalR scale-out backplane (R-12B)
- Caller-selected SignalR groups or managed display master
- AnnouncementVersion or display snapshot fields inside the SignalR payload
- Taksaka Operations hub reuse
- Intake / booking-assistance refresh publish paths

## Phase 4 exit checklist

| Criterion | Result |
|-----------|--------|
| Committed mutations can emit refresh hints | Yes (SignalR adapter behind existing post-commit port) |
| Rollback / conflict emits none | Yes (existing handler tests unchanged) |
| Transport failure does not fail queue writes | Yes (publisher swallow + unit test) |
| Disabling SignalR does not change queue write truth | Yes (`SignalRRefreshEnabled=false` → Null publisher) |
| Hub access documented relative to `[Authorize]` | Yes (API-V1 + this report) |
| Focused tests green | Yes (see evidence below) |

## Test evidence

Focused filter (2026-07-23):

`SignalRAdmissionQueueRefreshPublisherTest|AdmissionQueueOperationalCommandsTest|RegistrationOutcomeTest|AdmissionQueueApiContractTest`

**Passed: 25, Failed: 0**

Coverage includes:

- Stable hub path / event name
- Publisher sends `{ loketKey }` (including null)
- Publisher swallows transport failure
- Call publishes after success; StartService conflict never publishes
- Registration outcome finalize publishes after success; concurrent finalize never publishes
- Hub `[Authorize]`; `SignalRRefreshEnabled` defaults true

## Suggested commit message

```
feat(admission-queue): Phase 4 SignalR refresh-hint adapter

Bind IAdmissionQueueRefreshPublisher to a minimal authenticated hub at
/hubs/admission-queue; keep hints non-authoritative and disable-safe.
```
