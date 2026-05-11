# FEATURE MODEL GENERATION SKILL

## PURPOSE

Generate Rich Domain Model for Feature.

Model represents:

- Aggregate Root
- Domain Entity
- Business Behaviour
- Aggregate Consistency

Model resides in Domain Layer.
Model MUST NOT know:

- SQL
- DTO
- DAL
- Repository
- persistence detail

Prefer:

- explicit structure
- immutable state
- behaviour-oriented design
- low cognitive load

Avoid:

- anemic model
- hidden mutation
- persistence concern

---

## MODEL STRUCTURE

Preferred structure:

```csharp
public record XxxType : IXxxKey
{
    #region FIELD
    #endregion

    #region CREATION
    #endregion

    #region PROPERTIES
    #endregion

    #region BEHAVIOUR
    #endregion
}
```

Keep structure deterministic.

---

## NAMING RULE

| Type          | Convention   |
| ------------- | ------------ |
| Model         | `{Name}Type` |
| Reff          | `{Name}Reff` |
| Key Interface | `I{Name}Key` |

Examples:

- `PasienType`
- `RegType`
  - `KarcisType`
  
---

## MODEL RULE

Model MUST:

- use immutable `record`
- contain business behaviour
- protect aggregate consistency
- expose meaningful operation

Model MUST NOT:

- expose persistence logic
- expose DTO conversion
- expose SQL concern

---

## KEY INTERFACE RULE

Every aggregate MUST define key interface.
Example:

```csharp
public interface IPasienKey
{
    string PasienId { get; }
}
```

Purpose:

- lightweight repository lookup
- avoid primitive obsession
- standardize aggregate identity

---

## CREATION RULE

Preferred section:

```csharp
#region CREATION
#endregion
```

Model SHOULD provide:

- constructor
- `Create()`
- `Default`
- `Key()`

---

## STATIC FACTORY RULE

Use static factory for aggregate creation.
Example:

```csharp
public static PasienType Create(...)
```

Factory SHOULD:

- simplify orchestration
- enforce invariant
- hide creation complexity
  
Avoid complex constructor usage in Use-Case.

---

## DEFAULT RULE

Every aggregate SHOULD provide:

```csharp
Default
```

Purpose:

- null safety
- fallback object
- deterministic loading

Example:

```csharp
public static PasienType Default =>
    new("-", "-", true);
```

---

## KEY FACTORY RULE

Every aggregate SHOULD provide:

```csharp
Key(string id)
```

Example:

```csharp
public static IPasienKey Key(string id)
    => Default with { PasienId = id };
```

Purpose:

- lightweight lookup
- repository query consistency

---

## REFF RULE

Use Reff for lightweight aggregate reference.
Naming:

```text
{Name}Reff
```

Reff SHOULD contain:

- Id
- Name

Reff MUST NOT contain:

- business behaviour
- aggregate logic

Example:

```csharp
public record PasienReff(
    string PasienId,
    string PasienName);
```

---

## TOREFF RULE

Aggregate SHOULD provide:

```csharp
ToReff()
```

Example:

```csharp
public PasienReff ToReff()
    => new(PasienId, PasienName);
```

---

## PROPERTY RULE

Properties SHOULD use:

```csharp
{ get; init; }
```

Avoid mutable setter.
GOOD:

```csharp
public string PasienName { get; init; }
```

BAD:

```csharp
public string PasienName { get; set; }
```

---

## COLLECTION RULE

Mutable collection MUST be private.
Expose ONLY as:

```csharp
IEnumerable<T>
```

GOOD:

```csharp
private readonly List<ItemType> _listItem;
public IEnumerable<ItemType> ListItem => _listItem;
```

BAD:

```csharp
public List<ItemType> ListItem { get; set; }
```

Purpose:

- protect aggregate consistency
- prevent external mutation
  
---

## DERIVED PROPERTY RULE

Computed value belongs in Model.

GOOD:

```csharp
public decimal Total => ListItem.Sum(x => x.Nilai);
```

Avoid derived calculation in Use-Case.

---

## DOMAIN BEHAVIOUR RULE

Business operation MUST happen in Model whenever possible.

GOOD:

```csharp
reg.BatalBerobat(userId);
que.RemoveEntry(noUrut);
```

BAD:

```csharp
reg.IsAktif = false;
```

Prefer:

- intention revealing behaviour
- business operation method

Avoid direct state mutation.

---

## AGGREGATE RULE

Aggregate Root owns:

- child consistency
- child collection
- aggregate invariant

Child mutation SHOULD happen through aggregate behaviour.

Example:

```csharp
AddItem()
RemoveItem()
Void()
Activate()
Deactivate()
```

Avoid exposing mutable child collection.

---

## IMMUTABILITY RULE

Model SHOULD prefer immutable state.

Use:

- `record`
- `init`
- encapsulated collection

Avoid:

- mutable public state
- external aggregate mutation

---

## DEFAULT VALUE RULE

Use deterministic default object.
Preferred:

- "-"
- empty collection
  - safe primitive value
  
Avoid:

- null
- partially initialized aggregate
  
---

## RULE PRECEDENCE

1. Aggregate Consistency > Static Factory Simplicity
2. Immutability > Convenience
3. Behaviour Cohesion > DRY
4. Explicit Structure > Brevity

---

## BEHAVIOUR STYLE

Behaviour SHOULD:

1. reveal business meaning
2. be intention-oriented
3. remain cohesive

GOOD:

```csharp
Void()
Register()
Transfer()
Approve()
Reject()
```

BAD:

```csharp
SetStatus()
UpdateFlag()
ChangeValue()
```

Prefer business language.

---

## FORBIDDEN PATTERN

Do NOT:

- expose mutable List
- place SQL in Model
- place DTO conversion in Model
- create anemic model
- expose persistence state manipulation
- use public setter
- expose persistence concern

Avoid infrastructure leakage into Domain.

---

## VALIDATION RULE

Model MUST protect its own state.
- Use `Throw if Invalid` pattern inside Behaviour.
- Invariants SHOULD be checked in the Static Factory.
- Constructor treated as Hydration Process, not creation.

---

## OUTPUT STYLE

Generate:

- explicit structure
- deterministic naming
- cohesive behaviour
- immutable aggregate
- readable domain language

Prefer:

- business clarity
- low hidden behaviour
- low cognitive load

Avoid:

- speculative abstraction
- framework-driven design
- magic behaviour

---

## AI OPTIMIZATION RULE

This skill is optimized for Agentic Coding.

AI SHOULD:

- generate deterministic structure
- prioritize behaviour-oriented design
- avoid persistence concern
- avoid unnecessary abstraction
- keep aggregate cohesive

Consistency is more important than creativity.

---

## IMPORTANT PRINCIPLE

Model owns business meaning.
Model protects aggregate consistency.
Model exposes behaviour, not mutation.
