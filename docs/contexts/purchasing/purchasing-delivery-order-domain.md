# Purchasing — Delivery Order (DO) Domain

**Artifact status:** Design specification (new bounded context; domain skeleton only)

**Bounded context:** Purchasing — DeliveryFeature (Delivery Order / Penerimaan Barang)

**Governing standards:** [DATABASE.md](../../DATABASE.md), [ENGINEERING.md](../../ENGINEERING.md), [NAMING.md](../../NAMING.md)

**Dependent context:** [Stock Ledger](../stok-ledger/stok-ledger-domain.md) — consumes the DO as the Goods Receipt Source (`BrgMasukReffId`)

**Legacy evidence (non-normative):** `docs/stok-ledger/clbGenStokX1.cls` (`GenStokDO`, `GenStokDOVoid`); legacy tables `tb_trs_do` / `tb_trs_do2` (external, not in this repo)

---

## 1. Business Overview

### 1.1 Purpose

A **Delivery Order (DO)** records the receipt of purchased goods from a supplier into hospital stock. In the legacy system the DO document is at the same time the supplier delivery order and the goods receipt — one transaction, transaction prefix `DM`, stock journal kind `DO` (inbound) and `DO_V` (void). The DO identity is the durable **Receipt Source** that Stock Ledger uses to trace every item's provenance (`BrgMasukReffId`).

This model replaces the legacy one-shot full receipt with **partial receipt support**: a DO line may be received in several installments (by line, location, or time) while the DO remains the single provenance identity for the whole purchase delivery.

### 1.2 Scope

In scope:

1. Creation and maintenance of a DO header and lines before receiving begins.
2. Recording of partial or final receipt installments against DO lines.
3. Optional Purchase Order (`PoReffId`) reference.
4. Supplier reference + name snapshot for historical consistency.
5. HPP (unit cost) derivation from line price, discount, and tax using the configured `MetodePersediaanHPP` formula.
6. Void of the DO with an accountable stock reversal consequence.

Out of scope for this artifact (owned by other features/contexts):

- Purchase Order, Purchase Request, Material Request documents (`PurchaseOrderFeature`, `PurchaseReqFeature`, `MaterianReqFeature`).
- Supplier invoice / faktur (`FakturFeature`).
- Faktur-to-DO matching and accounts-payable consequences.
- Stock Ledger consequence execution (in-process integration from the application layer).

### 1.3 Boundaries

| Owner | Responsibility |
|---|---|
| Purchasing / DeliveryFeature | DO document authority: supplier delivery facts, line quantities, pricing, HPP derivation, receipt installments, void authority |
| Stock Ledger | Inventory consequence of each receipt installment (UC-STL-001) and of DO void (reverse journal; future UC) |
| Product Catalog | Item identity and unit conversion (used at consequence posting) |
| Facility / organizational authority | Layanan (receiving location) identity |
| Legacy stock record | Persisted stock authority during coexistence (see Stock Ledger) |

DeliveryFeature does not decide whether stock may be consumed or re-allocated; Stock Ledger does not decide why goods are received.

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Delivery Order (DO) | The document recording receipt of purchased goods from a supplier into hospital stock; the durable Receipt Source for Stock Ledger. |
| DO Number (`DoNo`) | Business document number of the DO (legacy `DM…`); used as provenance (`BrgMasukReffId`) in Stock Ledger. |
| Supplier | The vendor delivering the goods. Referenced by `SupplierId` with a `SupplierName` snapshot. |
| Purchase Order Reference (`PoReffId`) | Optional reference to the related purchase order. |
| DO Line | One item receipt: item, receiving location, ordered quantity, received quantity, unit, price, discount, tax, expiry, and batch. |
| Receipt Installment | One partial or final receive event against a DO line; carries its own transaction id (`TrsReffId`) for Stock Ledger idempotency. |
| HPP | Unit cost derived from line price/discount/tax by the configured `MetodePersediaanHPP` formula. |
| MetodePersediaanHPP | Legacy cost method: `HPP`, `HPP_DIS`, or `HPP_DIS_TAX`. |

---

## 3. Business Capabilities

### 3.1 DO Maintenance

Create a DO header and lines while in `Draft`; add/remove lines, change supplier, PO reference, and notes.

### 3.2 Partial Receipt Recording

Record receipt installments per line; accumulate `QtyReceived`; never exceed `QtyOrder`; advance header state.

### 3.3 HPP Derivation

Derive unit cost per line using the configured `MetodePersediaanHPP` formula.

### 3.4 DO Void

Void the DO from any non-void state and require an accountable stock reversal consequence (reverse journal `DO_V`) so previously received stock is restored.

---

## 4. Actors & Roles

| Role | Business involvement |
|---|---|
| Purchasing / Receiving Officer | Creates and maintains the DO, records receipt installments. |
| Inventory Steward | Reviews voids and reconciliation with Stock Ledger. |
| Stock Ledger (integration) | Receives goods-receipt consequences per installment and reversal consequences on void. |

---

## 5. Domain Objects

### 5.1 Delivery Order (aggregate root)

Header of the DO: `DeliveryOrderId`, `DoNo`, `Supplier` (`SupplierId` + `SupplierName`), `PoReffId`, `DoDate`, `State`, `Notes`, audit trail, and owned lines.

### 5.2 Delivery Order Item (child entity)

One item receipt: `ItemNo`, `BrgId`, `LayananId`, `QtyOrder`, `QtyReceived`, `SatuanId`, `Harga`, `Diskon`, `Tax`, `TglEd` (sentinel `3000-01-01` when absent), `NoBatch`, `State`.

### 5.3 Supplier Reff

Lightweight reference value: `SupplierId` + `SupplierName`.

---

## 6. Aggregate

**Aggregate root:** `DeliveryOrderModel` — `IDeliveryOrderKey`.

Consistency boundary: one DO document and its lines.

The aggregate keeps mutually consistent:

- header state and line states;
- `QtyReceived ≤ QtyOrder` on every line;
- `IsFullyReceived` (all lines `Received`) driving the header to `Received`;
- non-void document identity once receiving has started;
- audit trail (`Crt*`, `Upd*`, `Vod*`).

---

## 7. Business Rules

- **BR-DLV-001** — A DO must have at least one line before receiving.
- **BR-DLV-002** — `QtyOrder` on a line must be positive.
- **BR-DLV-003** — `QtyReceived` must never exceed `QtyOrder` on a line.
- **BR-DLV-004** — A receipt installment quantity must be positive.
- **BR-DLV-005** — Line `Harga`, `Diskon`, and `Tax` must not be negative.
- **BR-DLV-006** — HPP is derived by the configured `MetodePersediaanHPP`; the DO model computes it, the application layer posts it.
- **BR-DLV-007** — Header and line mutations other than receiving and void are allowed only in `Draft`.
- **BR-DLV-008** — Receiving is allowed only when the header is `Draft` or `Open`.
- **BR-DLV-009** — Void is allowed from `Draft`, `Open`, or `Received`; a voided DO must produce a Stock Ledger reversal consequence (`DO_V`) for every receipt already posted.
- **BR-DLV-010** — `DoNo` is unique and immutable after creation.
- **BR-DLV-011** — A DO line may be received into only one `LayananId`; multiple locations are modeled as separate lines.

---

## 8. State Machines & Lifecycles

### 8.1 Header lifecycle

```text
Draft   (created; no receiving yet)
  -> Open      (first installment received; some lines not fully received)
  -> Received  (all lines fully received)
  -> Void      (cancelled; from Draft / Open / Received)
```

### 8.2 Line lifecycle

```text
Open     (no installment yet)
  -> Partial   (0 < QtyReceived < QtyOrder)
  -> Received  (QtyReceived == QtyOrder)
```

Line void is not modeled separately; voiding is a header decision and voids every posted installment of the DO.

---

## 9. Domain Events

Optional / documentation-only (see [operational-events.md](../../concepts/operational-events.md) §4 — this project prefers direct orchestration):

| Event | Meaning |
|---|---|
| DeliveryOrderCreated | Draft DO created. |
| DeliveryOrderLineReceived | A receipt installment was recorded against a line. |
| DeliveryOrderReceived | All lines fully received. |
| DeliveryOrderVoided | DO voided; reversal consequence required. |

These events are candidates for audit/telemetry, not an event bus.

---

## 10. Business Workflows

### 10.1 Create DO

```text
Enter header (DoNo, Supplier, optional PoReffId, DoDate, Notes)
  -> add lines (Item, Layanan, QtyOrder, Satuan, Harga, Diskon, Tax, TglEd, NoBatch)
  -> header Draft
```

### 10.2 Receive installment (partial or final)

```text
Select line + installment Qty (≤ QtyRemaining)
  -> line QtyReceived += Qty; line state Partial/Received
  -> header state Open/Received
  -> application layer posts PostGoodsReceiptConsequenceCommand
       BrgId = line.BrgId
       BrgMasukReffId = DoNo          // provenance stays the DO
       LayananId = line.LayananId
       Qty = installment Qty
       Hpp = line.ComputeHpp(MetodePersediaanHPP)
       TrsReffId = <receipt event id> // unique per installment (NOT DoNo)
       TglMutasi = receipt datetime
       TglEd / NoBatch / PoReffId = line values
```

### 10.3 Void DO

```text
Authorize void (Draft/Open/Received)
  -> header Void; Vod* audit filled
  -> for every posted installment, post Stock Ledger receipt-void consequence
       reverse journal (DO_V), restore stock, keep originals (see §11)
```

---

## 11. Stock Ledger Integration Contract

DeliveryFeature is the originating authority; Stock Ledger is the consequence sink (in-process, same transaction).

| Direction | Command | Notes |
|---|---|---|
| Receive installment | `PostGoodsReceiptConsequenceCommand` (UC-STL-001) | Idempotent on `(TrsReffId, MovementKind, StokLokasiId)`. |
| Void | Future `PostGoodsReceiptVoidConsequenceCommand` (UC-STL-001-void) | Reverse journal `DO_V`; modeled on `PostSaleVoidConsequenceCommand`; **currently a documented Stock Ledger gap** — must be implemented before DO void goes live. |

### 11.1 Per-installment TrsReffId (key decision)

`BrgMasukReffId` (Receipt Source / provenance) is always the `DoNo` — the whole purchase delivery shares one Stock Batch per item.

`TrsReffId` must be a **unique id per receipt installment**, not the `DoNo`. Because Stock Ledger's native idempotency key is `UX (TrsReffId, MovementKind, StokLokasiId)`, reusing `DoNo` for a second installment of the same line+location would be treated as a duplicate and silently dropped.

### 11.2 MovementKind mapping

`MovementKindEnum.GoodsReceipt (1)` ↔ legacy `"DO"`; future `GoodsReceiptVoid` ↔ legacy `"DO_V"` (additive value; see Stock Ledger GAP-STL-003).

---

## 12. Persistence

Schema skeleton under `Bilreg.SqlDb/PurchaseContext/DeliveryFeature/`:

- `BILRG_DeliveryOrder` — header (PK `DeliveryOrderId VARCHAR(12)`, `UX DoNo`, `IX DoDate`).
- `BILRG_DeliveryOrderItem` — detail (composite PK `(DeliveryOrderId, ItemNo)`; `IX BrgId`).

Conventions: PascalCase, no DB FK constraints (logical FK only), `DEFAULT('')` for strings, `DEFAULT('3000-01-01')` for empty dates, `INT` enums, full `Crt*/Upd*/Vod*` audit columns (this feature is not exempted like Stock Ledger's ADR-STL-002).

---

## 13. Open Items and Follow-ups

| ID | Item | Owner | Blocks |
|---|---|---|---|
| GAP-PUR-001 | Stock Ledger `PostGoodsReceiptVoidConsequenceCommand` + `GoodsReceiptVoid` MovementKind | Stock Ledger | DO void go-live |
| GAP-PUR-002 | Supplier master existence and `SupplierId` format | Product / master data | Supplier validation |
| GAP-PUR-003 | Unit conversion (`Brg.ConvToTerkecil`) availability for HPP in smallest unit | Product Catalog | Exact legacy HPP parity |
| GAP-PUR-004 | `MetodePersediaanHPP` system parameter wiring | Engineering | HPP derivation at runtime |
| GAP-PUR-005 | Indonesian companion document (`purchasing-delivery-order-domain-id.md`) | Docs | Bilingual parity |

---

## 14. Prohibitions for Agents

- Do not move DO document authority into Stock Ledger; Stock Ledger records consequences only.
- Do not reuse `DoNo` as the per-installment `TrsReffId`.
- Do not delete receipt rows on void; void is reverse journal + `Vod*`.
- Do not invent Purchase Order, Faktur, or accounts-payable workflows inside DeliveryFeature.
- Do not add HTTP product API unless a later ADR says so.