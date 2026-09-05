# Outpatient Apotek Implementation Completeness Audit

**Independent implementation-completeness audit · 19 August 2026**

## Answer

**No — the plan was not fully implemented.**

The tracker claims all 39 slices (APT-B00 through APT-B30 and APT-F00 through APT-F07) are implemented. Source-code verification does not support those claims. Only **2 of 39** slices meet their planned capability and evidence requirements. **32** are **PARTIAL** (capability started but important artifacts or acceptance evidence missing). **5** are **NO-GO** (capability not found or implementation contradicts the plan).

Open production release gates were not used to downgrade slices; only implementation and planned evidence gaps were scored.

---

## Verification Summary

| Status | Count |
|--------|------:|
| GO | 2 |
| PARTIAL | 32 |
| NO-GO | 5 |
| **Total** | **39** |

### Executed verification

| Command | Result |
|---------|--------|
| `dotnet build` | Passed |
| `dotnet test ...~ApotekContext` | 38/38 passed |
| `pnpm tc:app` | Passed |
| `vitest outpatient.spec.ts` | 10/10 passed |

Passing available tests does not supply the missing repository, API-contract, SQL fixture, workflow, and UI-path evidence required by the plan.

---

## Slice Verification Table

| Slice | Planned Capability | Evidence Found | Missing Items | Status |
|-------|-------------------|----------------|---------------|--------|
| APT-B00 | Apotek module boundary | Context markers, DI registration, controller, and 3 architecture tests. Solution build passed. | None material for this boundary slice. | GO |
| APT-B01 | Neighbor prerequisites | DispenseIssue enum/handler, location IDs, 7-day parameter seed and tests. | LYAPT/LYDTU and service-point SQL are example-only; no service-point resolution or consequence behavior test. | PARTIAL |
| APT-B02 | Integration Task engine | Task model, SQL, DAL/repo, worker, handlers, retry API, model/worker tests. | No real business-save + task rollback test; no hosted/scheduled invocation of ProcessBatch was found. | PARTIAL |
| APT-B03 | Electronic Resep Kerja intake | Model/items/components, SQL, repo, handler, API, idempotent in-memory scenario. | Repository and API-contract tests; production prescription adapter is fail-closed. | PARTIAL |
| APT-B04 | Physical Resep Kerja intake | Physical model validation, shared SQL/repo, handler, API with BC-13 header. | Physical handler and API-contract tests. | PARTIAL |
| APT-B05 | Jual Bebas acceptance | Supporting model, SQL, repo, accept/decline-after-accept handlers and APIs; Version concurrency; decline actor audit on VodUser/VodDate; 28 dedicated JualBebas tests (model/command/DAL). | None material for this slice; catalog validation deferred downstream (approved interpretation); DAL version predicate deferred to APT-B29. | GO |
| APT-B06 | Telaah Resep | Aggregate, SQL, repo, lifecycle handlers/APIs, state/disposition domain tests. | Repository freeze and API error-contract tests. | PARTIAL |
| APT-B07 | Available Stock fail-closed contract | Port, fail-closed production registration, deterministic fakes, SO consumer, rollback scenario. | Partial and zero providers are not exercised by tests; no dedicated port contract suite. | PARTIAL |
| APT-B08 | Sales Order establishment | Aggregate, filtered-index SQL, repo, handler/API, quantity and fail-closed tests. | Filtered-index/repository and transaction tests; Jual Bebas establishment test. | PARTIAL |
| APT-B09 | Partial fulfillment and Copy Resep | Copy Resep/outcome models, SQL, repos, establishment/unfulfilled handlers and API. | Partial-path and append-only persistence tests; no dedicated Copy Resep API. | PARTIAL |
| APT-B10 | Iter consumption delivery | Iter task enqueue, handler, fail-closed port; shared task SQL/repo. | Iter enqueue, idempotency, and adapter-failure tests. | PARTIAL |
| APT-B11 | Queue mapping and close | Mapping/close models, SQL, repos, handlers/APIs, per-demand summary contract types, QueueFeature tests. | Mapping DAL round-trip deferred until BILRG_AptQueueMapping deployed to local test DB; read API delivery is APT-B24. | GO |
| APT-B12 | Tracker pharmacy adapter | Real Tracker adapter and Served/Done/Withdrawn task handlers. | Adapter contract, concurrency, idempotency, positive-withdraw and F-09 regression tests. | PARTIAL |
| APT-B13 | Invoice establishment and issue | Invoice aggregate, SQL, repo, establish/issue handlers and APIs; no-dual-write guard. | Aggregate/repository and issue+BillingCharge transaction tests; configured price/TR ports fail closed. | PARTIAL |
| APT-B14 | Payment clearance and authorization | Payment handler/API and pure authorization policy used by release/start. | Policy matrix, payment-incomplete, and BillingCharge idempotency tests; payment port is fail-closed. | PARTIAL |
| APT-B15 | Invoice revision/correction correlation | Invoice behavior, SQL field, permission port, revise/correction handlers and APIs. | Allowed/denied transaction and correlation tests; no pending-manual-correction read contract. | PARTIAL |
| APT-B16 | Dispensing through Prepared | Aggregate, SQL, repo, establish/release/start/prepare handlers and APIs; stock/Tracker task enqueue. | Repository, transaction, stock-correlation, Tracker-timing and policy-transition tests. | PARTIAL |
| APT-B17 | Review, education, pickup and handover | Append-only review model/SQL, coordinated pickup and handover handlers/APIs, domain/scenario tests. | Append-only persistence, coordinated multi-dispensing, stock-task and recipient contract tests. | PARTIAL |
| APT-B18 | General-patient end-to-end | One in-memory WF-003 happy-path scenario using existing handlers. | Decline, payment-incomplete, task-failure and API scenarios; configured external ports prevent a live path. | PARTIAL |
| APT-B19 | BPJS invoice-at-handover path | Coverage command, BPJS policy branch, handover invoice creation, happy/no-show/failed-review scenarios. | Policy matrix, coverage-command, WF-004 happy/failure scenario tests; production SEP/price/TR ports fail closed. | GO |
| APT-B20 | Mixed coverage | Coverage can be applied to an already-created Sales Order. | No payer-split orchestration creates independent BPJS and patient-pay orders; no WF-005 tests. | NO-GO |
| APT-B21 | Multiple demands in one queue | Queue mapping lists multiple demands; pickup handler coordinates mapped dispensings. | WF-006 multi-demand concurrency/idempotency and isolation tests. | PARTIAL |
| APT-B22 | Post-establishment shortage | Unfulfilled command/API appends outcome and Copy Resep without changing Accepted Qty. | Quantity/mutation-protection and issued-invoice correction-routing tests. | PARTIAL |
| APT-B23 | Collection window, override and no-show | Expiry policy, override/no-show handlers/APIs, stock-return and Tracker tasks, payer matrix tests. | Deferred SO resolution until StockReturnNoShow succeeds; paid General stays Active; stock-failure test added. | GO |
| APT-B24 | Telaah and Pelayanan read APIs | Query DTOs/handlers, Dapper DAL and two GET endpoints. | Telaah query starts from existing Telaah rows, so unstarted intake is invisible; Pelayanan starts from mapping, not Tracker queue; no fixture/query tests. | NO-GO |
| APT-B25 | Dispensing and Serah read APIs | Dapper projections, handlers and GET endpoints with computed categories. | Remediated ApotekDate sentinel bug; AccountablyResolved projection; category/clock/DAL tests (23). | GO |
| APT-B26 | Medication journey | Journey handler/API groups demand facts with Telaah, payer paths, and integration task id/status per demand. | Multi-demand/mixed journey DAL and handler contract tests (27 WorklistFeature tests). | GO |
| APT-B27 | Unified sales reporting | Read-only APT/legacy DU union query, handler/API, no-write architecture guard; dual-source DAL and handler contract tests. | Remediated fn_grand_total column, PasienName join, DU date/void filters. | GO |
| APT-B28 | Integration failure operations | Failure query/DAL/API and authorized retry command/API. | LastError filter, correlation/SourceKind output, retry audit logging, worker observability logs, ops DAL/command tests. | GO |
| APT-B29 | API/auth/audit/concurrency hardening | Authorized controller, authenticated actor stamping, policy seam, 400/401/409 filter, OpenAPI snapshot, contract tests. | None material; production mutation rollout remains RELEASE-BLOCKED by BC-12. | GO |
| APT-B30 | Final backend verification | Solution builds; 38 filtered Apotek tests pass. | Required WF-001–007, SQL smoke, repository/API contracts and neighbor integration suites are absent; canonical tracker path is missing. | NO-GO |
| APT-F00 | Frontend type/module-shell reset | Canonical DTO schemas/query keys, four routed workbenches, legacy queue/deposit model removed; Ranap kept separate. | None material for this shell slice. | GO |
| APT-F01 | Queue and mapping client | Mapping/close/pickup clients, grouping, 409 refresh test. | No Tracker queue read client or server-sourced lifecycle; multi-demand render test absent. | PARTIAL |
| APT-F02 | Telaah workbench | Worklist and start/item/complete/SO clients with rejected-SO guard. | No line-item read model, substitute/reject reason guard parity, or component tests. | PARTIAL |
| APT-F03 | Pelayanan Penjualan workbench | Mapping, physical intake, Jual Bebas, invoice establish, queue close and retry UI. | No mixed coverage, payment, invoice issue/revise/correction, shortage or decline clients/tests. | PARTIAL |
| APT-F04 | Dispensing workbench | Release/start/prepare clients, state guards for start/prepare, retry visibility. | No dispensing-establish client, release guard/authorization display, or workbench tests. | PARTIAL |
| APT-F05 | Serah/no-show workbench | Pickup/review/education/override/handover/no-show clients and basic guards. | No payer-specific no-show consequences, asOf support, or no-show component test. | PARTIAL |
| APT-F06 | Journey and attention UI | Journey panels, attention filters, pending/failure visibility and retry. | Journey has no payer path; attention behavior lacks component tests. | PARTIAL |
| APT-F07 | Frontend verification and documentation | Type-check passes; 10 targeted tests pass; active README describes four workbenches. | No General/no-show UI-path or component tests; legacy SOP body remains; tracker records full lint failure. | PARTIAL |

---

## Five NO-GO Slices

| Slice | Reason |
|-------|--------|
| **APT-B20** | Lacks mixed payer-split orchestration that creates independent BPJS and patient-pay orders. |
| **APT-B23** | ~~Resolves Sales Order before stock-return task succeeds~~ Remediated in review round 2 → GO. |
| **APT-B24** | Does not source Pelayanan from Tracker queue; cannot surface unstarted Telaah work. |
| **APT-B26** | ~~Omits planned troubleshooting facts (Telaah, payer identity); misclassifies integration task states.~~ Remediated in review round 2 → GO. |
| **APT-B30** | Lacks required final workflow, SQL, repository, API, and integration evidence suites. |

---

## Missing Items Report

### Missing or non-deployable SQL

- APT-B01: production Pharmacy Unit / DTU seed (only `BILRG_Apt_Seed_Layanan.example.sql` exists).
- APT-B01: production pharmacy service-point seed (only `BILRG_Apt_Seed_ServicePoint.example.sql` exists).
- APT-B30: SQL smoke fixtures and schema verification suite.

### Missing APIs or API clients

- APT-B09: dedicated Copy Resep command/API promised by the target area.
- APT-B15: pending/manual Tata Rekening correction read contract.
- APT-F01: Tracker pharmacy queue read client.
- APT-F03: coverage, unfulfilled, invoice issue/payment/revise/correction and decline clients.
- APT-F04: dispensing-establish client.

### Missing repository evidence

- Repository interfaces and implementations exist for planned backend persistence.
- No Apotek SQL-backed repository round-trip/fixture test suite exists.
- No filtered-index behavior tests exist for Resep Kerja, Sales Order or queue uniqueness.

### Missing tests

- No Apotek API-contract/OpenAPI/auth/audit/concurrency test suite.
- No SQL fixture/query-contract tests for worklists, journey or unified reporting.
- No complete WF-001, WF-002, WF-005, WF-006 or WF-007 scenario suites.
- No mixed-coverage orchestration tests and no stock-return-failure/no-show tests.
- No frontend view/component tests; General and no-show UI paths are absent.

---

## Audit Notes

- **Sources:** `apotek-implementation-master-plan.md`, `outpatient-apotek-progress-tracker.md` (working copy), backend and frontend source trees.
- **Method:** Each slice was verified against planned acceptance criteria and expected artifacts (domain model, aggregate, repository, SQL, application handler, API endpoint, test). Artifacts marked N/A only when the plan does not require them for that slice type.
- **Limitation:** The canonical tracker path listed in the documentation index was absent; the working tracker copy was read as the untrusted claim source.
- **Scope:** This audit verifies implementation completeness only. It does not evaluate design quality or suggest redesign.
