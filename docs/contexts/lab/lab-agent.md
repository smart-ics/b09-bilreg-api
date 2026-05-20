# lab-agent.md — Laboratory Workflow Feature (Backend)

# 1. Mission

Laboratory Workflow Feature (LWF) is:
- operational workflow orchestration feature,
- operational source-of-truth for laboratory workflow,
- NOT a full LIS.

LWF owns:
- operational lifecycle,
- workflow invariant,
- result verification,
- result release.

LWF does NOT own:
- patient registration,
- billing authority,
- analyzer infrastructure,
- LIS infrastructure.

---

# 2. Core Philosophy

Optimize for:
- workflow clarity,
- operational simplicity,
- invariant consistency,
- pragmatic DDD,
- AI readability.

Avoid:
- over-engineering,
- event-driven complexity,
- infrastructure-centric design,
- aggregate monster.

---

# 3. Workflow Authority

EMR only initiates order.

After order creation:
- lifecycle belongs to LWF,
- workflow state belongs to LWF,
- operational invariant belongs to LWF.

EMR must NEVER:
- overwrite LWF state,
- bypass invariant,
- directly modify workflow.

---

# 4. Aggregate Design

Primary Aggregate:

```text
LabOrder
```

Responsibilities:
- workflow lifecycle,
- collection workflow,
- billing orchestration,
- release eligibility,
- integration coordination.

Secondary Aggregate:

```text
LabResultDocument
```

Responsibilities:
- result recording,
- verification,
- amendment/versioning,
- rendering source.

---

# 5. Workflow States

```text
Ordered
Deferred
Charged
Collected
Recorded
Verified
Released
Cancelled
Terminated
```

---

# 6. Workflow Rules

Normal flow:

```text
Ordered
→ Deferred (optional)
→ Charged
→ Collected
→ Recorded
→ Verified
→ Released
```

Rules:
- Deferred blocks collection.
- Charged required before collection.
- Verified locks workflow.
- Released is final operational delivery.

Immutable states:

```text
Verified
Released
```

---

# 7. Cancellation Rules

Allowed:

```text
Ordered → Cancelled
Deferred → Cancelled
Charged → Cancelled
```

Cancellation invalid after collection.

---

# 8. Termination Rules

Termination allowed only after workflow execution started.

Allowed:

```text
Collected → Terminated
Recorded → Terminated
```

Termination requires:
- reason,
- actor,
- timestamp.

Termination invalid after:

```text
Verified
Released
```

---

# 9. Result Rules

1 Order = 1 ResultDocument.

ResultDocument:
- immutable per version,
- uses full snapshot versioning.

Amendment creates:

```text
new immutable version
```

Never mutate verified result.

---

# 10. Verification Rules

Verification:
- only by Clinical Pathologist,
- applies to full ResultDocument,
- no partial verification.

Verification means:

```text
medical validation completed
```

Verification does NOT mean:
- released,
- paid,
- financially cleared.

---

# 11. Release Rules

Release:
- administrative result delivery,
- separate from verification,
- allowed only once.

Release requires:

```text
Financial Clearance = Approved
```

Internal users may still view verified result before release.

---

# 12. Financial Boundary

LWF does NOT own billing.

LWF only:
- requests billing charge,
- receives billing response,
- checks financial clearance.

Billing authority belongs to BIL.

Never:
- calculate payment,
- manage refund,
- manage receivable,
- manage payment settlement.

---

# 13. Integration Pattern

REG and BIL:

```text
Synchronous Actor-Driven Integration
```

Characteristics:
- blocking,
- fail-fast,
- immediate response.

OWR:

```text
Asynchronous Infrastructure Integration
```

Characteristics:
- autonomous,
- retryable,
- eventually processed.

---

# 14. OWARE Rules

OWR is:
- middleware,
- infrastructure bridge,
- NOT source-of-truth.

LWF remains:
- operational truth,
- official result owner.

Result matching uses:

```text
OrderNo
```

Persist:

```text
OwareStatus
```

Example:
- Pending
- Sent
- Failed

---

# 15. Patient Snapshot

Persist patient snapshot inside LabOrder.

Required fields:

```text
RegId
PatientId
MRNumber
PatientName
BirthDate
Gender
AgeAtOrder
```

Purpose:
- historical consistency,
- rendering,
- reporting,
- avoid REG dependency.

---

# 16. External Patient Rules

External Patient may start with:

```text
RegId = null
PatientId = null
```

After REG success:
- update snapshot,
- attach RegId.

---

# 17. Deferred Rules

Deferred means:

```text
waiting patient preparation
```

Example:
- fasting.

During Deferred:
- no billing,
- no collection,
- no result entry.

Deferred execution creates:

```text
ExecutionRegId
```

Do NOT replace original order context.

---

# 18. Specimen Rules

Collection workflow is operational-only.

Do NOT implement:
- accessioning,
- barcode lifecycle,
- analyzer routing,
- tube tracking.

Persist specimen requirement snapshot.

Example:

```text
TubeColor
SpecimenType
RequiredTubeCount
```

---

# 19. Result Source

Persist explicit result source.

Supported:

```text
Manual
Instrument
ExternalLIS
```

Required for:
- audit,
- troubleshooting,
- medico-legal traceability.

---

# 20. Reference Range

Reference range belongs to:

```text
Result Component
```

May depend on:
- gender,
- age,
- instrument/method.

Support auto-flagging:

```text
High
Low
Normal
```

---

# 21. API Philosophy

LWF is API-driven.

Subsystems communicate via API.

Do NOT implement:
- state synchronization,
- cross-module persistence coupling,
- direct DB sharing.

---

# 22. Persistence Philosophy

Transaction tables must store snapshot data.

Never depend on master joins for:
- historical rendering,
- operational workflow,
- result rendering.

LabOrderItem must snapshot:
- test name,
- billing mapping,
- specimen requirement.

---

# 23. Concurrency Philosophy

Multiple actors may work simultaneously.

Examples:
- analyst inputs result,
- pathologist verifies result,
- OWR imports result.

Protect:
- aggregate invariant,
- immutable states,
- result version consistency.

---

# 24. Forbidden Design

Never:
- make OWR source-of-truth,
- mutate verified result,
- sync EMR state into LWF,
- place billing logic in LWF,
- use payment state as workflow invariant,
- create giant aggregate,
- couple workflow to analyzer infrastructure.

---

# 25. Backend Development Style

Preferred:
- rich domain model,
- command-oriented workflow,
- explicit state transition,
- explicit invariant,
- explicit orchestration.

Avoid:
- transaction script style,
- anemic domain model,
- smart repository,
- hidden state mutation.

---

# 26. AI Agent Guidance

When implementing backend:

Always prioritize:
1. workflow correctness,
2. invariant safety,
3. operational clarity,
4. auditability,
5. snapshot consistency.

Never optimize for:
- abstraction purity,
- generic framework reuse,
- premature infrastructure sophistication.

LWF is:

```text
Operational Workflow System
```

not:
- full LIS,
- analyzer middleware,
- financial system.

