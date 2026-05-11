# FEATURE PERSISTENCE GENERATION SKILL

## PURPOSE

Generate persistence layer for Feature.

Persistence layer consists of:

* DTO
* DAL
* Repository

Persistence resides in Infrastructure Layer.

Prefer:

* explicit mapping
* readable SQL
* deterministic structure
* low hidden behaviour

Avoid:

* ORM magic
* reflection mapping
* speculative abstraction

---

## REQUIRED LIBRARY CONTEXT

This skill depends on NunaLib helper libraries.

Before generating DTO, DAL, or Repository,
AI SHOULD load relevant documentation of Nuna.Lib.NetStandard documentation in this site:
https://gitlab.com/myhospitalcore/a033_nuna_helper_netstandard

| Component       | Documentation                |
| --------------- | ---------------------------- |
| CRUD Interfaces | `docs/data-access-helper.md` |
| Dapper Helper   | `docs/dapper-helper.md`      |
| MayBe Monad     | `docs/maybe-monad.md`        |
| Result Monad    | `docs/result-monad.md`       |
| Nuna ID         | `/docs/nunaid.md`            |
| Repo-DAL interface| `/docs/crud-repository-interface.md`|            |

---

### Usage Rule

#### DTO + DAL generation

Load:

* `data-access-helper.md`
* `dapper-helper.md`

Purpose:

* CRUD interface composition
* SQL execution pattern
* parameter mapping convention

---

#### Repository generation

Load:

* `maybe-monad.md`
* `result-monad.md`
* `data-access-helper.md`

Purpose:

* `LoadEntity()` pattern
* `SaveChanges()` pattern
* `MayBe<T>` orchestration
* `Result<T>` flow

---

### Important Rule

DAL MUST follow:

* `IInsert<>`
* `IUpdate<>`
* `IDelete<>`
* `IGetData<>`
* `IListData<>`

Repository MUST follow:

* `ISaveChange<>`
* `ILoadEntity<>`
* `MayBe<T>`
* `Result<T>`

Use NunaLib helper consistently.

---
# PERSISTENCE FLOW

```text
Model
-> DTO.FromModel()
-> DAL
-> SQL

SQL
-> DTO
-> DTO.ToModel()
-> Repository
-> Model
```

---

# DTO RULE

DTO is persistence transport object.

DTO represents:

* table structure
* SQL projection
* query result

DTO MUST:

* use DB field naming
* provide `FromModel()`
* provide `ToModel()`

DTO MUST NOT:

* contain business logic
* contain orchestration
* know repository

Naming:

```text
{Name}Dto
```

Example:

```csharp
public record PasienDto(
    string fs_kd_pasien,
    string fs_nm_pasien)
{
    public static PasienDto FromModel(PasienType model)
    {
    }

    public PasienType ToModel()
    {
    }
}
```

---

# DTO DETAIL RULE

For header-detail aggregate:

```csharp
ToModel(detail1, detail2)
```

Repository coordinates detail loading.

DTO only performs conversion.

DTO MAY contain:

* joined field
* lookup field
* flattened projection

Example:

```csharp
fs_nm_jaminan
fs_nm_tarif
```

---

# DAL RULE

DAL executes SQL.

DAL responsibilities:

* CRUD execution
* query execution
* bulk persistence
* return DTO

DAL MUST:

* communicate using DTO only
* use explicit SQL
* use explicit parameter mapping

DAL MUST NOT:

* return Model
* reconstruct aggregate
* contain business logic

Naming:

```text
{Name}Dal
```

Preferred contract:

```csharp
public interface IPasienDal :
    IInsert<PasienDto>,
    IUpdate<PasienDto>,
    IDelete<IPasienKey>,
    IGetData<PasienDto, IPasienKey>,
    IListData<PasienDto>
{
}
```

---

# SQL RULE

Use:

* raw SQL
* readable formatting
* deterministic alias

Preferred alias:

```sql
aa
bb
cc
```

Preferred:

* `LEFT JOIN`
* `ISNULL`

Avoid:

* dynamic SQL
* hidden query builder
* ORM tracking

Preferred formatting:

```sql
SELECT
    aa.fs_kd_pasien,
    aa.fs_nm_pasien
FROM
    ta_pasien aa
WHERE
    aa.fb_aktif = 1
```

---

# PARAMETER RULE

Use explicit parameter mapping.

GOOD:

```csharp
dp.AddParam("@fs_kd_pasien",
    dto.fs_kd_pasien,
    SqlDbType.VarChar);
```

Avoid anonymous-object parameter magic.

---

# BULK INSERT RULE

Collection persistence SHOULD use:

```csharp
SqlBulkCopy
```

Preferred flow:

```text
Delete existing detail
Bulk Insert new detail state
```

Aggregate Root owns detail persistence.

Detail table SHOULD expose:

* Insert()
* Delete()
* ListData()

Avoid:

* Update()
* GetData()

---

# REPOSITORY RULE

Repository reconstructs aggregate.

Repository responsibilities:

* coordinate DAL
* reconstruct aggregate
* map DTO <-> Model
* persist aggregate

Repository MAY return:

* Model
* Reff
* View

Repository MUST NOT:

* expose DTO
* contain SQL

Naming:

```text
{Name}Repo
```

---

# REPOSITORY CONTRACT RULE

Repository contract resides in Application Layer.

Example:

```csharp
public interface IPasienRepo :
    ISaveChange<PasienType>,
    ILoadEntity<PasienType, IPasienKey>,
    IDeleteEntity<IPasienKey>,
    IListData<PasienView>
{
}
```

---

# SAVECHANGES RULE

Preferred pattern:

```csharp
LoadEntity(model)
    .Match(
        onSome: _ => Update(...),
        onNone: () => Insert(...)
    );
```

---

# AGGREGATE RECONSTRUCTION RULE

Repository SHOULD:

1. load header DTO
2. load detail DTO
3. convert detail DTO -> Model
4. reconstruct aggregate

Repository owns reconstruction flow.

Avoid reconstructing aggregate inside DAL.

---

# FORBIDDEN PATTERN

Do NOT:

* return Model from DAL
* place business rule in DTO
* place business rule in DAL
* place SQL in Repository
* use AutoMapper
* use EF tracking magic
* use reflection mapping
* use hidden persistence abstraction

Avoid persistence magic.

---

# OUTPUT STYLE

Generate:

* explicit SQL
* deterministic mapping
* readable persistence flow
* production-ready structure

Prefer:

* consistency
* readability
* low abstraction
* explicit conversion

Avoid:

* speculative abstraction
* generic mega repository
* hidden runtime mapping

---

# AI OPTIMIZATION RULE

This skill is optimized for Agentic Coding.

AI SHOULD:

* prioritize explicit mapping
* generate deterministic SQL
* avoid unnecessary abstraction
* keep persistence readable
* keep repository cohesive

Consistency is more important than creativity.

---

# IMPORTANT PRINCIPLE

DTO translates persistence.

DAL executes SQL.

Repository reconstructs aggregate.

