# ENGINEERING.md — Engineering Architecture Philosophy

## 1. Core Philosophy

This architecture prioritizes:

* operational maintainability
* long-lived evolution
* low cognitive load
* workflow clarity
* explicit structure
* AI generation stability

Architecture is NOT optimized for:

* theoretical purity
* abstraction maximalism
* framework worship
* speculative flexibility

---

## 2. Engineering Philosophy

This project uses:

* Pragmatic Tactical DDD
* Operational Workflow Architecture
* Explicit Persistence Architecture

The system is optimized for:

* transactional operational system
* workflow orchestration
* operational coordination
* long-lived enterprise evolution

---

## 3. Architectural Style

This project uses:

```text
Pragmatic Layer Isolation
```

Purpose:

* maintain dependency clarity
* reduce architectural leakage
* preserve operational readability
* avoid ceremonial complexity

---

## 4. Layer Structure

Preferred structure:

```text
Domain/
Application/
Infrastructure/
WebApi/
```

Each layer has explicit responsibility.

Layer responsibility detail belongs to dedicated artifact.

---

## 5. Dependency Direction

Allowed dependency:

```text
WebApi
→ Application
→ Domain

Infrastructure
→ Application
→ Domain
```

Forbidden:

```text
Domain → Infrastructure
Application → SQL
Application → DAL
```

Dependency flow MUST remain explicit and predictable.

---

## 6. Aggregate Philosophy

Aggregate is responsible for:

* business invariant
* workflow consistency
* child consistency

Aggregate owns business behaviour.

Aggregate SHOULD expose behaviour, not state mutation.

---

## 7. Repository Philosophy

Repository primarily owns:

```text
Transactional Aggregate Persistence
```

Repository acts as:

```text
Application Persistence Gateway
```

Repository coordinates persistence access while preserving application boundary consistency.

Repository implementation detail belongs to dedicated persistence artifact.

---

## 8. Projection Query Philosophy

Operational query SHOULD avoid unnecessary aggregate reconstruction.

System is optimized for:

* operational read performance
* workflow visibility
* queue scanning
* lightweight projection

Projection SHOULD reuse stable query shape whenever operationally reasonable.

Purpose:

* reduce SQL variation
* stabilize SQL Server execution plan cache
* reduce operational query fragmentation

---

## 9. Persistence Philosophy

Persistence architecture prioritizes:

* explicit persistence flow
* deterministic mapping
* operational readability
* predictable reconstruction

Persistence concern belongs to:

* DTO mapping
* repository reconstruction
* persistence orchestration

NOT domain model.

Detailed persistence implementation belongs to dedicated persistence artifact.

---

## 10. Use-Case Philosophy

Use-Case coordinates application flow.

Use-Case is responsible for:

* orchestration
* workflow coordination
* aggregate interaction
* persistence coordination

Business behaviour SHOULD remain inside Model whenever possible.

Detailed orchestration implementation belongs to dedicated use-case artifact.

---

## 11. Validation Philosophy

Validation uses:

```text
Layered Validation
```

Primitive validation belongs to orchestration boundary.

Business validation belongs to domain model.

---

## 12. Transaction Philosophy

Transaction boundary belongs to:

```text
Application Orchestration
```

Transaction SHOULD remain:

* explicit
* small
* orchestration-focused

---

## 13. Async Philosophy

Async SHOULD be used only where operationally meaningful.

Avoid async pollution for pure in-memory business behaviour.

---

## 14. Domain Event Philosophy

Domain Event is:

```text
Allowed But Optional
```

Prefer:

```text
Direct Orchestration First
```

Avoid event-driven overengineering.

---

## 15. Exception Philosophy

Architecture uses:

```text
Mixed Failure Strategy
```

Expected operational failure SHOULD remain explicit.

Unexpected/system failure MAY use exception.

---

## 16. SQL Server Philosophy

SQL Server is treated as:

```text
Durable Operational State Storage
```

NOT:

* business logic engine
* workflow engine
* orchestration engine

Business logic belongs in:

* Domain
* Application

---

## 17. Infrastructure Philosophy

Infrastructure SHOULD remain:

```text
Simple
Explicit
Predictable
```

Infrastructure exists to support:

* persistence
* external integration
* operational infrastructure concern

Infrastructure MUST avoid business intelligence leakage.

---

## 18. Abstraction Philosophy

Prefer:

```text
Explicit Structure
```

over speculative abstraction.

This architecture allows:

```text
Operationally Useful Abstraction
```

while avoiding:

```text
Speculative Business Abstraction
```

Good abstraction SHOULD be:

* explicit
* stable
* low magic
* operationally predictable
* cognitively lightweight

---

## 19. Shared Library Philosophy

This project uses shared operational engineering toolkit such as:

```text
NunaLib
```

Purpose:

* reduce repetitive infrastructure boilerplate
* standardize operational utility
* improve engineering consistency
* improve AI generation stability

Shared library exists to support operational infrastructure,
NOT replace business architecture.

---

## 20. Functional Flow Philosophy

This architecture allows:

```text
Lightweight Functional Flow
```

for orchestration clarity.

Purpose:

* reduce nested branching
* improve orchestration readability
* improve operational flow visibility

Functional helper exists to support readable operational flow,
NOT functional programming purity.

---

## 21. Logging Philosophy

Logging prioritizes:

* operational traceability
* business auditability
* workflow investigation
* operational troubleshooting

---

## 22. Testing Philosophy

Testing prioritizes:

1. Domain Model Testing
2. Use-Case Orchestration Testing
3. UI Testing
4. SQL Testing

Purpose:

* protect business behaviour
* protect workflow consistency
* preserve operational stability

---

## 23. Refactoring Philosophy

Refactoring prioritizes:

```text
Operational Stability
```

Avoid refactor merely for:

* trend alignment
* architectural beauty
* abstraction purity

---

## 24. AI Engineering Philosophy

Architecture is optimized for:

```text
AI-Assisted Development
```

AI SHOULD prioritize:

* deterministic structure
* operational readability
* stable naming
* explicit orchestration
* low hidden behaviour

Consistency is more important than creativity.

---

## 25. Important Principle

Software should:

* remain operationally maintainable
* evolve safely long-term
* preserve workflow clarity
* minimize cognitive load
* remain predictable for both humans and AI
