# USECASE GENERATION SKILL

## PURPOSE

Generate pragmatic Use-Case implementation using:

* Clean Architecture
* Tactical DDD
* MediatR
* Rich Domain Model
* Explicit orchestration

Use-Case resides in Application Layer.

The purpose of Use-Case is:

* orchestrate application flow
* coordinate aggregate interaction
* execute business scenario
* persist aggregate changes

Use-Case is NOT responsible for:

* SQL
* persistence mapping
* DTO manipulation
* domain persistence detail

---

## CORE PHILOSOPHY

Use-Case orchestrates.

Model owns business behaviour.

Repository reconstructs aggregate.

DAL executes SQL.

Prefer:

* explicit orchestration
* readable flow
* pragmatic decomposition
* low cognitive load

Avoid:

* ceremonial abstraction
* overengineering
* speculative flexibility

Optimize for:

* maintainability
* business clarity
* token efficiency
* AI-assisted generation

---

## USE-CASE STRUCTURE

Use-Case consists of:

1. Command / Query
2. Response (optional)
3. Handler

Standard naming:

| Type     | Naming                      |
| -------- | --------------------------- |
| Command  | `{Feature}{Action}Cmd`      |
| Query    | `{Feature}{Action}Qry`      |
| Response | `{Feature}{Action}Response` |
| Handler  | `{Feature}{Action}Handler`  |

Examples:

* `PasienDaftarCmd`
* `RegJalanBatalCmd`
* `KarcisListQry`

---

## COMMAND / QUERY RULE

Command / Query MUST:

* be immutable record
* contain request data only
* contain no business logic

Example:

```csharp
public record RegJalanBatalCmd(
    string RegId,
    string UserId
) : IRequest;
```

If request acts as aggregate key:

* command MAY implement key interface

Example:

```csharp
public record RegJalanBatalCmd(
    string RegId,
    string UserId
) : IRequest, IRegKey;
```

---

## RESPONSE RULE

Use Response ONLY when:

* returning multiple fields
* returning structured data
* returning projection result

Do NOT create Response:

* for void command
* for primitive single value unless clarity improves

Response MUST:

* be immutable record
* contain projection data only
* contain no behaviour

---

## HANDLER RULE

Handler is application orchestrator.

Handler responsibilities:

* validate request
* load aggregate
* coordinate business flow
* persist aggregate
* return response

Handler MUST NOT:

* contain SQL
* manipulate DTO
* know persistence detail
* contain heavy business rule

Business rule SHOULD live in Model whenever possible.

---

## HANDLER STRUCTURE

Preferred structure:

```text
1. Validate request
2. Load aggregate(s)
3. Validate business state
4. Execute model behaviour
5. Persist aggregate
6. Return response
```

Keep flow:

* linear
* explicit
* readable

---

## VALIDATION RULE

Use Guard clause for:

* null
* empty
* invalid primitive input

Example:

```csharp
Guard.Against.NullOrWhiteSpace(request.RegId);
```

Business validation:

* SHOULD happen in Model
* MAY happen in Handler if orchestration-specific

---

## REPOSITORY USAGE RULE

Use-Case communicates with persistence ONLY through Repository.

Handler MUST NOT:

* access DAL
* access DTO
* write SQL

---

## DOMAIN BEHAVIOUR RULE

Business manipulation SHOULD happen in Model.

GOOD:

```csharp
reg.BatalBerobat(userId);
que.RemoveEntry(noUrut);
bill.Void(userId);
```

BAD:

```csharp
reg.IsAktif = false;
que.List.Remove(...);
```

Model should expose behaviour, not state mutation.

---

## TRANSACTION RULE

Use transaction ONLY when:

* multiple aggregate persistence involved
* consistency boundary required

Preferred:

```csharp
using(var trans = TransHelper.NewScope())
{
    ...
    trans.Complete();
}
```

Keep transaction scope:

* small
* explicit
* orchestration-focused

---

## LOADING RULE

Complex aggregate loading MAY be extracted into helper method.

Preferred private helper naming:

* `LoadReg()`
* `LoadBilling()`
* `LoadAntrianContext()`

Private helper SHOULD:

* improve readability
* reduce orchestration noise
* remain local to handler

DO NOT extract helper prematurely.

---

## PRAGMATIC SRP RULE

Handler decomposition is OPTIONAL.

Do NOT extract class merely to satisfy theoretical SRP.

Extract ONLY when:

* orchestration becomes difficult to read
* business selection is reusable
* branching complexity increases
* multiple use-cases share logic

Prioritize:

* readability
* maintainability
* orchestration clarity

Over-abstraction is forbidden.

---

## ALLOWED PATTERN

Use these patterns pragmatically.

---

### RESOLVER

Purpose:
Resolve aggregate or dependency.

GOOD FOR:

* reusable loading logic
* fallback resolution
* contextual aggregate retrieval

Example:

```csharp
public interface IJaminanResolver
{
    JaminanType Resolve(string jaminanId);
}
```

---

### SELECTOR

Purpose:
Select business policy or strategy.

GOOD FOR:

* tarif selection
* policy branching
* decision orchestration

Example:

```csharp
public interface ITarifSelector
{
    TarifType Select(PasienType pasien, LayananType layanan);
}
```

---

### POLICY

Purpose:
Reusable business rule.

Example:

```csharp
public interface IRegPolicy
{
    bool CanVoid(RegModel reg);
}
```

---

### DOMAIN SERVICE

Use ONLY when:

* logic belongs to multiple aggregates
* pure business calculation
* orchestration-independent rule

Example:

```csharp
public interface ITarifCalculator
{
    decimal Calculate(...);
}
```

---

### FORBIDDEN PATTERN

Do NOT create:

* helper explosion
* manager class
* util class
* abstract orchestrator
* generic handler base
* generic selector framework
* generic resolver framework
* service locator
* reflection orchestration

Avoid Java/Clean-Code ceremony syndrome.

---

## PRIVATE HELPER RULE

Private helper method is encouraged when:

* improves readability
* separates orchestration step
* reduces visual complexity

Preferred sectioning:

```csharp
#region LOAD-DATA
#region VALIDATION
#region PROCESS
#region SAVE
```

Private helper SHOULD remain:

* cohesive
* local
* readable

Avoid helper nesting explosion.

---

## QUERY USE-CASE RULE

Query Use-Case SHOULD:

* return View projection
* avoid loading unnecessary aggregate
* prefer lightweight projection

Repository MAY expose:

* View
* Reff
* lightweight query projection

Avoid reconstructing aggregate for simple listing.

---

## ERROR HANDLING RULE

Use:

* `ArgumentException`
* `InvalidOperationException`
* `KeyNotFoundException`

Exception message SHOULD:

* explicit
* business-readable
* deterministic

---

## OUTPUT STYLE

Generate:

* explicit code
* minimal abstraction
* production-ready structure
* readable orchestration

Prefer:

* deterministic naming
* cohesive methods
* explicit dependency

Avoid:

* magic flow
* hidden orchestration
* speculative architecture

---

## AI OPTIMIZATION RULE

This architecture is optimized for Agentic Coding.

AI SHOULD:

* prioritize readability
* prefer explicit orchestration
* avoid unnecessary indirection
* avoid abstraction explosion
* keep handler cohesive
* keep business meaning visible

Feature generation is deterministic.

Use-Case generation is pragmatic.

Do not force symmetry between them.

---

## IMPORTANT PRINCIPLE

Use-Case coordinates.

Model decides.

Repository persists.

DAL executes SQL.

