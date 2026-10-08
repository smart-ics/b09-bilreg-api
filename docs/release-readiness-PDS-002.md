# Release-Readiness Record: PDS-002 Dual Attestation General Consent

## 1. Overview & Verification Summary
- **Feature Code**: PDS-002
- **Slice**: P4-S09 (End-to-end contract verification and operational cutover guardrails)
- **Target System**: Hospital Information System (Bilreg API & OFTA & c012 Web)
- **Architecture Reference**: `issues/PDS-002-ARCHITECTURE.md` v1.5 (TD-201 through TD-211)
- **Status**: IMPLEMENTED (Ready for Slice Review)

## 2. Cross-Repository Contract Verification Matrix

| Step | Interaction / Contract Boundary | Participating Nodes | Status / Verified Evidence |
|---|---|---|---|
| 1 | Render & Kirim Submit | c012 Web → Bilreg OFTA Proxy | Verified: Officer TTE executes on Kirim; transient passphrase/OTP cleared from memory. |
| 2 | Ingest & Immediate Execute | Bilreg → OFTA Ingest & Execute | Verified: Direct non-print pipeline; exactly 1 officer signee; disjoint position; provider signed PDF returned. |
| 3 | Signed PDF Handoff & Patient Publish | Bilreg → c012 → PenaEl/HiDok | Verified: c012 publishes the signed PDF returned by Bilreg (not raw PDF); external correlation preserved. |
| 4 | Patient Signing | HiDok App → PenaEl → Bilreg Record | Verified: Patient signing updates `patientSignState = "SIGNED"`; correlation map intact. |
| 5 | Dual-Attestation Status Aggregation | Bilreg Read Model (`AdmGetDigitalSignQry`) | Verified: `CombinedStatus = "Lengkap"` ONLY when both Officer and Patient are Signed; otherwise `Sebagian`. |
| 6 | Autonomous Archive Reconciler | Bilreg Worker → OFTA Archive | Verified: Background worker retrieves 2-signature PDF, archives to OFTA, idempotently records `archiveId`. |

## 3. Operational Cutover & Deployment Guardrails

### 3.1 Preflight Checklist (Deploy-Time Safeguards)
1. **OFTA Configuration**: `Ofta:BaseApiUrl` and `Ofta:ApiKey` (or `Ofta:ServiceToken`) must be configured in `appsettings.json` / environment secrets.
2. **DocType Verification**: Verify OFTA DocType (`GENERAL_CONSENT`) is provisioned as non-print (`RequestAction` must NOT route to `RequestRemoteCetak` or print queues).
3. **Officer Identity & TTE Registration**: Participating admission officers must have registered accounts and active TTE certificates on the configured TTE provider (Tilaka / Vinotek / BSrE).
4. **Preflight Health Endpoint**: `GET /api/admisi-ranap/digital-sign/general-consent/cutover-preflight` checks DocType non-print eligibility and officer readiness before feature flag activation.
5. **Forward-Only Cutover**: Legacy pre-cutover records remain forward-only and are not synthesized into OFTA pairs.

### 3.2 Negative & Idempotency Safeguards Proven
- **Fail-Fast on Missing Registration**: If an officer is unregistered on the TTE provider, OFTA execute fails fast with an actionable error; Bilreg refuses to record or publish to the patient.
- **Print Pipeline Exclusion**: Verified that neither OFTA `DocCloud/upload` nor `sendToOftaSign`/`finishPrint` is called on the General Consent path.
- **Idempotency Replay**: Repeated ingest/execute with identical `(RegId, DokumenId, ExternalDocumentId)` returns existing signed document and does not create duplicate signees or DB records. Re-running archive returns the existing `archiveId` without re-archiving.
- **Zero Secret Leakage**: Asserted that transient passphrases, OTPs, and private tokens are excluded from persistent stores, exception messages, and audit logs.

## 4. Observability & Alerting Playbook

| Metric / Event | Warning Threshold | Critical Threshold | Root Cause / Remediation Runbook |
|---|---|---|---|
| **OFTA Ingest Failure** | Rate > 1% in 5 min | Rate > 5% in 5 min | Check OFTA API service connectivity, auth tokens, or network latency. |
| **Officer Execute Failure** | Count > 3 in 10 min | Count > 10 in 10 min | Check provider TTE uptime or missing officer registration. |
| **Missing Officer TTE Registration** | Count >= 1 | Count >= 5 | Officer account not provisioned in Tilaka/Vinotek. Provision officer TTE certificate. |
| **Correlation Mismatch** | Count >= 1 | Count >= 3 | Conflicting active `externalDocumentId` for same `regId`/`dokumenId`. Inspect client request generation. |
| **Patient Status Lag** | Lag > 15 min | Lag > 60 min | Patient has not signed in HiDok or webhook delayed. Admisi officer can follow up with patient. |
| **Archive Lag** | Unarchived > 30 min | Unarchived > 2 hours | Check `GeneralConsentArchiveWorker` background service logs and OFTA archive storage health. |
