# RNA SOP vs Simplified CPOE Domain — Gap Analysis

## 1. Scope and conclusion

This analysis compares the approved RNA SOP set under `docs/contexts/bangsal/rna-sop/` with the current simplified CPOE business truth in `docs/contexts/cpoe/CPOE-DOMAIN.md`. `docs/contexts/bangsal/RNA-DOMAIN.md` and `docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md` were also checked because the SOPs inherit their vocabulary and cross-context assumptions.

The service SOPs already match the simplified CPOE lifecycle in several important respects: an order is prospective intent, issuance does not prove execution, an Active order has exactly one terminal disposition, RNA owns execution evidence, CPOE owns the outcome association, and post-Completion evidence correction does not reopen the order.

The alignment is nevertheless incomplete. Four gaps require a policy/design correction, and three further issues require explicit integration or operational decisions. The highest-risk gaps are the obsolete occurrence model in RNA and the absence of Active-order reconciliation when accommodation responsibility changes.

## 2. Confirmed alignment

| Subject | CPOE business truth | RNA SOP treatment | Result |
|---|---|---|---|
| Intent versus evidence | `BR-CPOE-001`, `019`, `021` | S01 treats the order as instruction and records RNA evidence separately; S02 forbids retrospective Clinical Orders. | Aligned |
| Simplified lifecycle | `Active` to exactly one of `Completed`, `Not Performed`, or `Cancelled` | S01 uses the same three final dispositions and does not introduce acceptance, assignment, clarification, or dispatch states. | Aligned |
| Immutable order identity | `BR-CPOE-009` | S01 requires cancellation and a new order when patient, care context, Order Item, or Destination is wrong. | Aligned |
| Destination ownership | `BR-CPOE-007`, `011` | S01 scopes the worklist and outcome recording to the responsible RNA Destination. | Aligned |
| Completion evidence | `BR-CPOE-012`, `014` | S01 sends actual Performer, performance time, and the RNA evidence reference. | Aligned |
| Post-Completion correction | `BR-CPOE-025`–`032` | S03 keeps the order Completed, sets validity to Corrected or Invalidated, and requires a new order rather than reactivation when necessary. | Aligned |
| Financial boundary | `BR-CPOE-019`, `020`, `032` | S04 publishes execution facts and leaves the financial decision to Tata Rekening. | Aligned |

## 3. Gaps requiring correction

### GAP-CPOE-RNA-01 — RNA still permits multiple occurrences and executions for one Clinical Order

**Severity:** Critical  
**Affected artifacts:** `RNA-DOMAIN.md` (`BR-RNA-042`, `BR-RNA-043`, Planned Occurrence vocabulary and lifecycle), RNA SOP index/workflow references, and any integration contract that carries occurrence identity.

The simplified CPOE domain says one Clinical Order requests exactly one Order Item and creates one obligation for its Destination (`BR-CPOE-002`, `BR-CPOE-010`). Its lifecycle has one final Fulfilment Outcome and deliberately has no recurring-occurrence model. In contrast, `BR-RNA-043` says one Clinical Order may produce multiple RNA executions when multiple Occurrences or repeated performances are required.

Those models cannot both be authoritative. Under the current CPOE domain, every separately requested repeat performance must be a separate Clinical Order. RNA may still use an internal occurrence identity for deduplication of its one execution, but it must not use that identity to multiply one CPOE obligation.

**Required decision:** retire `BR-RNA-043` for native CPOE work and state `1 ClinicalOrderId -> at most 1 valid RNA execution fact -> 1 CPOE Fulfilment Outcome`. Preserve legacy multi-occurrence behavior only behind an explicitly legacy-labelled source contract.

### GAP-CPOE-RNA-02 — Transfer, release, and discharge-like accommodation changes do not reconcile Active orders

**Severity:** Critical  
**Affected SOPs:** A05 and A06; potentially A04 when it changes organizational Destination rather than only bed/room.

CPOE makes Destination immutable (`BR-CPOE-009`) and keeps an Active order outstanding until Completed, Not Performed, or Cancelled (`BR-CPOE-008`). A05 releases the patient to Admisi and later assigns another Ward, while A06 releases accommodation. Neither procedure determines what happens to Active Clinical Orders still owned by the old RNA Destination.

The result can be a stranded obligation: the old Ward still owns the CPOE work, the new Ward cannot take it over by changing Destination, and accommodation responsibility has already moved. Blind cancellation is also unsafe because some orders may remain clinically required.

**Required decision:** add a care-team reconciliation step before or immediately after responsibility changes. Each Active order must be explicitly Completed/Not Performed when factually appropriate, or Cancelled by an authorized actor and replaced with a new order for the new Destination when still required. RNA should expose the affected Active-order list but must not make the clinical replacement decision itself.

### GAP-CPOE-RNA-03 — Financial metadata can block recording clinical execution truth

**Severity:** High  
**Affected SOPs:** S01 step 9 and exception for unavailable Tarif; S02 has the same coupling; inherited from `BR-RNA-026`, `038`, `050`, and `054`.

S01 says that if a billable Service cannot be validated, RNA holds back the execution evidence. This couples authoritative clinical truth to Tarif availability. CPOE explicitly separates Destination-owned execution evidence from financial decisions (`BR-CPOE-014`, `019`–`021`). If the activity actually occurred, losing or delaying its authoritative evidence because Tarif is unavailable creates a larger clinical and audit inconsistency and can leave the Clinical Order Active after performance.

**Required decision:** allow RNA to commit a minimal non-financial execution fact (patient/care context, ClinicalOrderId, Performer, Performed At, description/activity reference) independently of Tarif. Service selection and billable publication may remain pending and recover later. CPOE Completion should use the committed clinical evidence reference, not wait for financial enrichment.

### GAP-CPOE-RNA-04 — S03 makes an external Outcome Review part of RNA procedure completion

**Severity:** High  
**Affected SOP:** S03 steps 11 and completion criteria.

CPOE owns Outcome Review after invalidation (`BR-CPOE-029`, `030`). S03 correctly sends the correction to CPOE, but declares the RNA correction procedure incomplete until the Responsible Clinician completes the CPOE review. This makes an RNA-owned correction workflow depend indefinitely on completion of a CPOE-owned clinical decision.

**Required decision:** split the completion semantics. RNA correction is complete when the correction fact is committed and durably accepted/pending delivery. CPOE then owns a visible `Outcome Review Required` work item to its own completion. RNA may display the external status but should not own or gate it.

## 4. Integration and operational gaps requiring explicit treatment

### GAP-CPOE-RNA-05 — Execution-versus-cancellation race has no terminal reconciliation outcome

**Severity:** High

S01 recognizes the race where RNA has recorded actual execution but the order becomes Cancelled before Completion reaches CPOE. It only hands the case to Clinical Governance. The simplified CPOE lifecycle correctly forbids changing Cancelled to Completed, but no durable cross-context record is specified to associate the conflicting execution fact with the cancelled order or to close the governance case.

**Required decision:** define a durable `DispositionConflict`/reconciliation record outside the Clinical Order lifecycle, including ClinicalOrderId, execution fact reference, competing timestamps, actors, reason, owner, and resolution. It must not rewrite the final order or erase the execution fact.

### GAP-CPOE-RNA-06 — The exactly-once correlation rule is not stated consistently in the SOP

**Severity:** Medium

S01 handles repeated outcome calls, but it does not state the native-CPOE invariant that the same ClinicalOrderId cannot create a second active execution fact. The current RNA Domain expresses deduplication through Planned Occurrence, which is precisely the obsolete model for native CPOE.

**Required decision:** use ClinicalOrderId as the native source-obligation idempotency key in RNA. Corrections/replacements form revisions of that evidence chain, not additional performances of the same order.

### GAP-CPOE-RNA-07 — Historical artifacts still preserve superseded exceptional-order concepts

**Severity:** Medium  
**Affected artifact:** `RNA-DOMAIN.md` execution-authority lifecycle and domain-event list.

`RNA-SOP-GAPS.md` correctly says the old subsequent-authorization, 24-hour deadline, overdue state, and retrospective exceptional-order policy was replaced. However, `RNA-DOMAIN.md` still contains `Subsequent Authorization Required`, `Completed`, and `Became Overdue` lifecycle/event language and a workflow that reports exceptional execution to CPOE when coordination applies. This contradicts both S02 and the simplified CPOE scope, which excludes routine care without an individual prospective order.

**Required decision:** remove those concepts from the canonical RNA domain or explicitly label them as non-CPOE governance records owned by another named context. S02's current handoff to Clinical Governance, without creating a retrospective order, should remain the operational rule.

## 5. Recommended change order

1. Decide and document native CPOE cardinality: one order, one obligation, at most one valid RNA execution fact.
2. Add Active-order reconciliation to inter-Ward transfer and accommodation release procedures.
3. Decouple recording execution truth from Tarif enrichment and financial publication.
4. Split RNA correction completion from CPOE Outcome Review completion.
5. Specify the cancellation/execution race reconciliation record and ownership.
6. Remove obsolete occurrence and subsequent-authorization language from the RNA domain, SOP index, and integration contract.
7. Update `RNA-SOP-GAPS.md` with these open decisions rather than marking the CPOE/RNA alignment fully closed.

## 6. Impact on neighboring contexts

- **CPOE:** no richer lifecycle is needed. The simplified states can remain unchanged; reconciliation and review work must be modeled without reopening final orders.
- **RNA:** source correlation, execution recording, transfer/release SOPs, and correction completion semantics need revision.
- **Admisi:** inter-Ward Waiting List ownership remains unchanged, but transfer orchestration needs evidence that CPOE reconciliation was initiated or completed according to the chosen safety policy.
- **Tarif/Tata Rekening:** financial enrichment and publication must tolerate a clinically committed execution fact that is not yet financially complete.
- **EMR/Clinical Governance:** one of these contexts must own the clinical decision and durable case record for transfer reconciliation and execution-versus-cancellation conflicts; RNA and CPOE should only expose their authoritative facts.

## 7. Sources

- `docs/contexts/cpoe/CPOE-DOMAIN.md`
- `docs/contexts/bangsal/RNA-DOMAIN.md`
- `docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md`
- `docs/contexts/bangsal/rna-sop/RNA-SOP-INDEX.md`
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A05-Transfer-Antar-RNA.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A06-Pelepasan-Akomodasi.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S02-Pelaksanaan-Tindakan-Ad-Hoc-atau-Independen.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S03-Koreksi-RNA-Service-Execution.md`
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md`
