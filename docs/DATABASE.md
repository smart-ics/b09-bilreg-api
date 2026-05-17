# DATABASE.md — SQL Persistence Engineering Standard

# 1. Philosophy

This project treats SQL Server as:

```text
Durable Operational State Storage
```

and:

```text
Optimized Operational Memory
```

Database is optimized for:

* operational workflow
* transactional durability
* persistence clarity
* explicit SQL access
* historical consistency
* predictable performance

Database is NOT treated as:

* business logic engine
* domain model
* orchestration layer
* rule engine

---

# 2. Core Principle

Business logic belongs in:

* Domain Model
* Use-Case
* Application Layer

Database responsibility is:

* persist operational state
* provide durable storage
* support efficient query access
* preserve historical data

---

# 3. SQL Philosophy

Prefer:

* explicit SQL
* readable query
* deterministic structure
* operational performance
* explicit indexing

Avoid:

* trigger-based business logic
* orchestration stored procedure
* hidden mutation
* ORM tracking magic
* dynamic SQL generation
* reflection mapping

Database should remain:

```text
predictable
explicit
operational
maintainable
```

---

# 4. Table Naming Convention

## Format

```text
{MODULE}_{ENTITY}
```

Examples:

```text
BILRG_Booking
LAB_Order
REG_Pasien
```

---

## Rules

* Use PascalCase
* Prefix MUST represent module/domain
* Prefix length usually 3–5 characters
* Table name SHOULD represent aggregate/entity meaning

---

# 5. Column Naming Convention

Use PascalCase.

Examples:

```text
PasienId
DokterId
BookingDate
ResultStatus
```

Avoid:

```text
pasien_id
booking_date
```

---

# 6. Primary Key Standard

## Primary Key Type

Default:

```sql
VARCHAR(12)
```

unless explicitly required otherwise.

---

## Philosophy

Primary key is:

* application generated
* globally unique enough for operational scope
* opaque identity
* persistence identity only

Avoid:

* IDENTITY auto increment
* composite key for aggregate root
* business meaning in primary key

---

# 7. Composite Primary Key Rule

Composite PK ONLY allowed for:

```text
Detail Table
```

Example:

```sql
PRIMARY KEY (OrderId, ItemNo)
```

Purpose:

* natural aggregate ownership
* deterministic ordering
* efficient bulk persistence

Avoid composite PK for aggregate root.

---

# 8. Foreign Key Philosophy

## NO DATABASE FK CONSTRAINT

This project intentionally avoids database FK constraint.

Consistency is enforced by:

* application layer
* aggregate consistency
* domain workflow
* repository orchestration

---

## Purpose

Avoid:

* deployment rigidity
* migration complexity
* unnecessary locking
* operational deadlock risk
* historical snapshot restriction

Database relationship remains:

```text
logical relationship
```

not physical FK enforcement.

---

# 9. Snapshot Philosophy

Transaction table MAY store snapshot data.

Snapshot is used selectively.

Examples:

```text
PasienName
DokterName
AsuransiName
```

Purpose:

* preserve historical consistency
* reduce dependency to master join
* optimize operational read
* prevent master mutation side effect

---

## Important Rule

Snapshot should ONLY store:

* operationally important data
* historically relevant data
* frequently queried display data

Avoid unnecessary duplication.

---

# 10. Null Philosophy

## Avoid NULL aggressively

Default rule:

```text
Column MUST NOT allow NULL
```

unless truly optional.

---

## Preferred Default Value

### String

```sql
DEFAULT('')
```

### Integer

```sql
DEFAULT(0)
```

### DateTime

```sql
DEFAULT('3000-01-01')
```

---

# 11. Empty Date Philosophy

Empty date sentinel:

```sql
'3000-01-01'
```

represents:

```text
Not Assigned
Not Occurred
Empty Date
```

Avoid nullable DATETIME whenever possible.

---

# 12. Audit Column Standard

ALL transaction tables MUST contain:

```sql
CrtUser VARCHAR(50)
CrtDate DATETIME

UpdUser VARCHAR(50)
UpdDate DATETIME

VodUser VARCHAR(50)
VodDate DATETIME
```

---

## Purpose

Support:

* operational audit
* troubleshooting
* historical traceability
* void lifecycle

---

# 13. Void Philosophy

This project uses:

```text
Void Lifecycle
```

instead of hard delete.

Record remains historically preserved.

Void operation indicated by:

```sql
VodUser
VodDate
```

---

## Important Rule

Avoid physical DELETE for transaction table.

DELETE only allowed for:

* temporary table
* staging table
* cache table
* detail rewrite persistence

---

# 14. Enum Philosophy

Workflow status and enum SHOULD use:

```sql
INT
```

Examples:

```sql
OrderStatus INT
ResultStatus INT
Hari INT
```

Avoid VARCHAR enum storage.

Purpose:

* performance
* storage efficiency
* deterministic comparison

---

# 15. Index Philosophy

Index optimized for:

* operational read
* queue scanning
* workflow filtering
* transactional performance

---

## Important Rule

Indexing strategy is contextual.

Do NOT blindly generate:

```sql
FILLFACTOR=90
```

AI SHOULD determine fillfactor based on:

* update frequency
* insert pattern
* fragmentation risk
* operational workload

---

# 16. Historical Philosophy

Prefer:

```text
append + status transition
```

over destructive overwrite.

UPDATE should be minimized.

Historical state should remain observable whenever possible.

---

# 17. Detail Table Philosophy

Detail table belongs to aggregate ownership.

Preferred structure:

```sql
OrderId VARCHAR(12)
ItemNo INT
```

Primary Key:

```sql
PRIMARY KEY (OrderId, ItemNo)
```

---

## Purpose

Supports:

* deterministic ordering
* aggregate reconstruction
* bulk delete + insert persistence
* simpler repository orchestration

---

# 18. SQL Formatting Standard

Preferred alias:

```sql
aa
bb
cc
```

Preferred formatting:

```sql
SELECT
    aa.BookingId,
    aa.PasienName
FROM
    BILRG_Booking aa
WHERE
    aa.BookingDate >= @StartDate
```

---

# 19. Persistence Philosophy

Database stores:

* operational state
* workflow state
* historical state
* integration state

Database does NOT represent:

* full domain behaviour
* aggregate invariant
* orchestration logic

---

# 20. Performance Philosophy

Priority order:

```text
1. Operational Performance
2. Readability
3. Simplicity
4. Normalization
```

---

# 21. AI Persistence Generation Rule

AI SHOULD generate:

* explicit SQL
* deterministic structure
* readable query
* snapshot-aware persistence
* operationally optimized index

AI MUST avoid:

* ORM-style thinking
* FK constraint generation
* nullable-everywhere schema
* over-normalization
* hidden persistence magic

---

# 22. Important Principle

Database persists operational truth.

Application protects business truth.
