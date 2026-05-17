# NAMING.md — Naming Engineering Standard

# 1. Core Philosophy

Naming exists to maximize:

* operational readability
* long-term maintainability
* AI-generated code stability
* business clarity
* workflow clarity

Naming is NOT optimized for:

* theoretical purity
* academic DDD correctness
* abbreviation cleverness
* minimal typing

---

# 2. Ubiquitous Language Philosophy

This codebase intentionally uses:

```text id="b9jlwm"
Mixed Indonesian + English
```

Operational Indonesian terms SHOULD be preserved intentionally.

Examples:

```text id="jlwm8"
Pasien
Dokter
Layanan
Antrian
Pemeriksaan
Tindakan
Ruang
Jadwal
```

Purpose:

* operational familiarity
* readability for HIS programmers
* business clarity
* hospital workflow understanding

---

# 3. Naming Priority

Priority order:

```text id="g7jlwm"
1. Operational Readability
2. Maintainability
3. AI Consistency
4. Technical Purity
```

---

# 4. Aggregate Naming

Aggregate Root SHOULD use:

```text id="5wjlwm"
{Name}Model
```

Examples:

```text id="jlwm0"
LabOrderModel
PasienModel
RegModel
AntrianModel
```

---

# 5. Value Object Naming

Value Object SHOULD use:

```text id="vjjlwm"
{Name}Type
```

Examples:

```text id="18jlwm"
MoneyType
AddressType
LabResultValueType
ReferenceRangeType
```

---

# 6. Child Entity Naming

Aggregate member entity SHOULD also use:

```text id="t4jlwm"
{Name}Model
```

Examples:

```text id="2wjlwm"
LabOrderItemModel
ResultComponentModel
AntrianEntryModel
```

---

# 7. Reference Object Naming

Lightweight aggregate reference SHOULD use:

```text id="jlwm2"
{Name}Reff
```

Examples:

```text id="jlwm7"
PasienReff
DokterReff
LayananReff
```

---

## Reff Purpose

Reff is used for:

* lightweight projection
* lookup reference
* dropdown selection
* contextual display

Reff MUST NOT contain:

* business behaviour
* workflow logic
* aggregate consistency

---

# 8. DTO Naming

Persistence DTO MUST use:

```text id="0fjlwm"
{Name}Dto
```

Examples:

```text id="jlwm9"
LabOrderDto
PasienDto
AntrianEntryDto
```

---

# 9. DAL Naming

Persistence DAL MUST use:

```text id="jlwm5"
{Name}Dal
```

Examples:

```text id="v5jlwm"
ILabOrderDal
IPasienDal
IAntrianDal
```

---

# 10. Repository Naming

Repository MUST use:

```text id="jlwm6"
{Name}Repo
```

Examples:

```text id="m6jlwm"
ILabOrderRepo
IPasienRepo
IAntrianRepo
```

---

# 11. Use-Case Naming

## Command

```text id="r0jlwm"
{Name}{Action}Cmd
```

Examples:

```text id="u3jlwm"
LabOrderCreateCmd
LabResultVerifyCmd
RegPasienDaftarCmd
```

---

## Query

```text id="jlwm1"
{Name}{Action}Qry
```

Examples:

```text id="n9jlwm"
LabOrderListQry
AntrianGetQry
PasienSearchQry
```

---

## Response

```text id="jlwm3"
{Name}{Action}Response
```

Examples:

```text id="d1jlwm"
LabOrderCreateResponse
PasienSearchResponse
```

---

## Handler

```text id="o2jlwm"
{Name}{Action}Handler
```

Examples:

```text id="r5jlwm"
LabOrderCreateHandler
LabResultVerifyHandler
```

---

# 12. Enum Naming

Enum Type MUST use:

```text id="e4jlwm"
{Name}Enum
```

Examples:

```text id="f8jlwm"
LabOrderStatusEnum
GenderEnum
AntrianStatusEnum
```

---

## Enum Property Naming

Enum property SHOULD NOT repeat suffix.

GOOD:

```csharp
public LabOrderStatusEnum LabOrderStatus { get; init; }
```

BAD:

```csharp
public LabOrderStatusEnum LabOrderStatusEnum { get; init; }
```

---

# 13. Boolean Naming

Boolean property SHOULD use:

```text id="0zjlwm"
Is
Has
Can
```

Examples:

```text id="d9jlwm"
IsVerified
HasResult
CanCancel
IsVoid
```

Avoid:

```text id="r3jlwm"
VerifyFlag
ResultFlag
```

---

# 14. Method Naming Philosophy

Method MUST use:

```text id="m0jlwm"
Intention-Revealing Business Behaviour
```

Method SHOULD describe:

* operational meaning
* business action
* workflow intention

---

## GOOD Examples

```text id="n2jlwm"
Verify()
Cancel()
CollectSpecimen()
RecordResult()
Void()
Transfer()
Approve()
Reject()
```

---

## BAD Examples

```text id="v1jlwm"
UpdateStatus()
SetFlag()
Process()
Handle()
Execute()
ChangeValue()
```

---

# 15. Namespace Philosophy

Namespace SHOULD follow:

```text id="s7jlwm"
Folder Structure
```

Avoid namespace structure that differs from physical project structure.

Purpose:

* developer predictability
* AI consistency
* easier navigation

---

# 16. Database Naming Symmetry

Code naming SHOULD preserve database naming symmetry whenever possible.

Database:

```text id="w0jlwm"
PasienId
DokterId
BookingDate
```

Code:

```text id="j9jlwm"
PasienId
DokterId
BookingDate
```

Avoid unnecessary translation.

---

# 17. Abbreviation Philosophy

Only approved abbreviations SHOULD be used consistently.

---

## Approved Abbreviations

| Full Term    | Standard    |
| ------------ | ----------- |
| Registration | Reg         |
| Reference    | Reff        |
| Queue        | Antrian     |
| Doctor       | Dokter      |
| Patient      | Pasien      |
| Laboratory   | Lab         |
| Schedule     | Jadwal      |
| Insurance    | Asuransi    |
| Verification | Verify      |
| Void         | Void        |
| Examination  | Pemeriksaan |

---

## Important Rule

Avoid introducing new abbreviation casually.

Consistency is more important than brevity.

---

# 18. Naming Stability Philosophy

Naming SHOULD remain stable long-term.

Avoid renaming merely for:

* stylistic preference
* theoretical correctness
* trend alignment

Stable naming improves:

* operational familiarity
* AI generation consistency
* onboarding
* maintainability

---

# 19. Service Naming Philosophy

Terms such as:

```text id="6hjlwm"
Service
Manager
Processor
Coordinator
Helper
Util
```

are NOT forbidden.

However:

* responsibility MUST be explicit
* naming MUST remain cohesive
* vague orchestration MUST be avoided

---

## GOOD Examples

```text id="jlwmq"
TarifCalculator
ReferenceRangeResolver
BillingChargeService
```

---

## BAD Examples

```text id="jlwmr"
DataManager
GlobalHelper
ProcessCoordinator
```

---

# 20. View Naming

Projection / Read Model SHOULD use:

```text id="jlwms"
{Name}View
```

Examples:

```text id="jlwmt"
LabOrderView
AntrianView
PasienView
```

---

# 21. Key Interface Naming

Aggregate key interface SHOULD use:

```text id="jlwmu"
I{Name}Key
```

Examples:

```text id="jlwmv"
ILabOrderKey
IPasienKey
IRegKey
```

---

# 22. AI Naming Rule

AI SHOULD prioritize:

* deterministic naming
* operational readability
* naming symmetry
* workflow clarity

AI MUST avoid:

* synonym drift
* inconsistent abbreviation
* architectural renaming
* speculative terminology

Consistency is more important than creativity.

---

# 23. Important Principle

Naming should help:

* humans understand workflow
* AI generate stable code
* developers navigate quickly
* business meaning remain visible
