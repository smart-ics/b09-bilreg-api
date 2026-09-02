# DOMAIN.md

# 1. Business Overview

The Product Code Registry domain manages the association between physical product identifiers and inventory Items.

Its purpose is to enable operational activities to identify inventory Items using scannable Product Codes instead of manual Item searches.

The domain supports manufacturer-provided Product Codes, internally generated Product Codes, and future barcode-driven operational processes across inventory and sales activities.

The domain does not own inventory balances, stock adjustments, sales transactions, purchasing transactions, warehouse movements, or stock valuation. It exclusively owns Product Code identification, registration, maintenance, and lookup.

---

# 2. Ubiquitous Language

| Term | Definition |
|--------|--------|
| Item | An inventory product maintained by the organization. |
| Item Code (BrgId) | The internal business identity of an Item. |
| Product Code | A scannable identifier used to recognize an Item. |
| Manufacturer Code | A Product Code provided by a manufacturer or supplier. |
| Internal Code | A Product Code generated internally by the organization. |
| Product Code Registry | The collection of Product Codes associated with an Item. |
| Default Unit | The recommended Unit associated with a Product Code. |
| Unit | A measurable packaging or operational quantity level of an Item. |
| Product Code Registration | The process of associating a Product Code with an Item. |
| Product Code Lookup | The process of identifying an Item from a Product Code. |
| Barcode Label | A printed representation of a Product Code. |
| Product Code State | The lifecycle status of a Product Code. |

---

# 3. Business Capabilities

## 3.1 Product Code Registration

Register Product Codes and associate them with Items.

## 3.2 Product Code Lookup

Identify an Item using a Product Code.

## 3.3 Internal Code Generation

Generate Internal Codes when manufacturer-provided Product Codes are unavailable.

## 3.4 Barcode Label Management

Support the creation and usage of Barcode Labels for operational activities.

## 3.5 Product Code Maintenance

Maintain Product Codes throughout their lifecycle.

## 3.6 Product Code Lifecycle Management

Control activation, deactivation, and retirement of Product Codes.

---

# 4. Actors & Roles

## Inventory Officer

Responsible for Product Code registration, maintenance, correction, and lifecycle management.

## Warehouse Staff

Uses Product Codes during warehouse operations.

## Stock Opname Officer

Uses Product Codes to identify Items during stock counting activities.

## Sales Operator

Uses Product Codes during sales transactions.

## Inventory Manager

Responsible for governance and approval of Product Code maintenance policies.

---

# 5. Domain Objects

## Item

Represents an inventory product.

An Item owns a single Item Code (BrgId) and may have multiple Product Codes.

## Product Code

Represents a scannable identifier associated with an Item.

A Product Code identifies an Item but is not the identity of the Item.

## Unit

Represents a packaging, dispensing, selling, or operational quantity level of an Item.

## Barcode Label

Represents a printed form of a Product Code used in physical operations.

---

# 6. Aggregates

## Item (Aggregate Root)

### Consistency Boundary

- Item
- Product Codes

### Responsibilities

- Own Product Code associations.
- Ensure Product Code uniqueness.
- Maintain Product Code lifecycle.
- Maintain Product Code ownership.
- Maintain Product Code activation status.

---

# 7. Business Rules

## BR-PCR-001

An Item shall have exactly one Item Code (BrgId).

## BR-PCR-002

An Item may have multiple Product Codes.

## BR-PCR-003

A Product Code shall identify exactly one Item.

## BR-PCR-004

A Product Code shall be globally unique within the organization.

## BR-PCR-005

A Product Code may contain a Default Unit recommendation.

## BR-PCR-006

The Default Unit shall be treated as a suggested value and not as a mandatory transaction Unit.

## BR-PCR-007

An Item without a manufacturer-provided Product Code may use an Internal Code.

## BR-PCR-008

An Internal Code shall remain a valid Product Code even when manufacturer-provided Product Codes are later registered.

## BR-PCR-009

Product Code Lookup shall identify the associated Item regardless of Product Code source.

## BR-PCR-010

A Product Code may originate from either a Manufacturer Code or an Internal Code.

## BR-PCR-011

A Product Code shall exist in one Product Code State at a time.

## BR-PCR-012

Only Active Product Codes shall be used for Product Code Lookup.

## BR-PCR-013

Inactive and Retired Product Codes shall remain historically traceable.

## BR-PCR-014

A Product Code shall never be physically deleted once it has reached Active state.

## BR-PCR-015

A Product Code shall always remain associated with its original Item throughout its lifecycle.

## BR-PCR-016

Changing the Default Unit recommendation shall not alter historical operational records.

---

# 8. State Machines & Lifecycles

## Product Code Lifecycle

```text
Draft
  ↓
Active
  ↓
Inactive
  ↓
Retired
```

### State Definitions

#### Draft

Product Code has been created but is not yet available for operational use.

#### Active

Product Code is available for Product Code Lookup and operational usage.

#### Inactive

Product Code is temporarily unavailable for new operational usage but remains historically valid.

#### Retired

Product Code is permanently removed from operational usage and retained only for historical traceability.

---

# 9. Domain Events

## ProductCodeRegistered

A Product Code has been associated with an Item.

## ProductCodeActivated

A Product Code has become available for operational usage.

## ProductCodeInactivated

A Product Code has been removed from active operational usage.

## ProductCodeRetired

A Product Code has been permanently removed from operational usage.

## InternalCodeGenerated

An Internal Code has been generated for an Item.

## BarcodeLabelPrinted

A Barcode Label has been produced.

## DefaultUnitChanged

The Default Unit recommendation associated with a Product Code has changed.

---

# 10. Business Workflows

## Product Code Registration

```text
Product Code Captured
        ↓
Item Identified
        ↓
Default Unit Assigned (Optional)
        ↓
Product Code Registered
        ↓
Product Code Activated
```

---

## Internal Code Creation

```text
Item Identified
        ↓
Internal Code Generated
        ↓
Barcode Label Printed
        ↓
Product Code Activated
```

---

## Product Code Lookup

```text
Product Code Captured
        ↓
Associated Item Identified
        ↓
Operational Process Continues
```

---

## Product Code Retirement

```text
Active Product Code
        ↓
Inactive
        ↓
Retired
```

---

# Business Boundary

The Product Code Registry domain owns:

- Product Code registration
- Product Code lookup
- Product Code lifecycle
- Product Code uniqueness
- Product Code ownership
- Internal Code generation
- Barcode Label management

The Product Code Registry domain does not own:

- Inventory Balance
- Stock Adjustment
- Stock Opname
- Sales Transaction
- Purchasing Transaction
- Warehouse Movement
- Inventory Valuation

These domains consume Product Code Registry as an identification capability.