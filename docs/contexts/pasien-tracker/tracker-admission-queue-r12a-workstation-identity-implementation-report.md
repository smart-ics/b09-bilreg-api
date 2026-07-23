# R-12A — Workstation Identity Validation

## Outcome

Admission Queue Loket mutations now require both `X-Workstation-Key` and `X-Loket-Key`. Static
`AdmissionQueueApi:Workstations` configuration maps each workstation key to exactly one LoketKey.
The API accepts a mutation only when the request payload, Loket header, and configured mapping agree.

## Configuration and runtime behavior

At startup, configuration validation rejects blank keys, keys over 50 characters, duplicate
workstation keys, and duplicate Loket keys (case-insensitively). A deployment must provision one
unique entry per physical Loket workstation. Empty configuration is permitted so non-queue
deployments can start, but Loket mutation requests are rejected until the installation supplies a
mapping.

At runtime, an unmapped workstation or mapping mismatch receives the normal invalid-request
response. A second active Call for the same configured Loket remains rejected by the authoritative
R-04 `BILRG_AdmLoketCurrentCall` claim, returning the existing 409 concurrency behavior. No lease,
heartbeat, registration endpoint, Loket master, generic lock, or distributed coordination has been
added.

## Boundary

The server can validate only the static mapping it receives. Two machines intentionally configured
with the same workstation identity, or independently configured API installations with contradictory
mappings, cannot be reliably detected without a shared identity/coordination service; that is outside
R-12A and the stated non-goals. Deployment must prevent header spoofing and provision unique static
keys.

## R-12B

SignalR scaling, Redis backplane, reverse proxy, display hosting, firewall, monitoring, health
checks, and topology are classified as a later operations deployment guide. They are not part of the
Admission Queue domain implementation.

## Verification

Tests cover valid unique mappings plus duplicate workstation and duplicate Loket rejection, and an
API mutation with a workstation mapped to a different Loket. Existing R-04 operational concurrency
tests continue to cover active-claim rejection. The focused API/configuration suite passes 9 of 9
tests after rebuilding the solution.
