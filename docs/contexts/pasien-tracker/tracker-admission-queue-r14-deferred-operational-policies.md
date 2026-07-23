# R-14 — Deferred Operational Policies

## Governing rule

Do not infer business decisions. If the product owner has not approved a policy, the system requires
explicit user action. V1 must not convert operational signals, elapsed time, Priority, or incomplete
data into an automatic business disposition.

## Approved V1 behavior

| Decision | V1 behavior |
|---|---|
| No-Show | Explicit officer command only; stored as Withdrawn with reason `NoShow` |
| CallCount | Informational; never determines No-Show, postponement, or next-entry selection |
| Service Point | Patient selects one of the offered active Service Points |
| Call selection | Officer selects the Queue Entry; Priority may sort the worklist but never claims it |
| Redirect | Explicit command; preserve origin and create a new Priority replacement entry |
| Registration finalization | Explicit Established or NotEstablished command only |
| Printing | Client performs it only after receiving a successful committed response |
| Backend contract | Client-agnostic; no printing, display-timing, or UI wording policy |

## Deferred policies

- automatic No-Show and queue-aging rules;
- BPJS or other routing policy;
- automatic Service Point selection;
- display-clear, audio, and timing policy;
- print monitoring or reprint telemetry; and
- UI wording and client presentation choices.

## Implementation evidence

The R-08 Application layer exposes explicit `AdmissionQueueNoShowCmd`, `AdmissionQueueCallCmd`, and
`AdmissionQueueRedirectCmd`; there is no scheduled or CallCount-driven disposition path. The officer
worklist orders Priority for visibility only. Redirect persists a replacement Queue Entry with source
provenance. Queue mutations complete their local transaction before publishing refresh hints; no
printing dependency exists in the backend.

Any future automation must be introduced only with an approved policy, identified owner, explicit
inputs, audit evidence, failure behavior, and tests.
