# Stock Ledger Domain

**Artifact status:** Initial canonical business specification

**Bounded context:** Stock Ledger

**Version scope:** Incremental modernization of legacy inventory recording while preserving compatibility with existing stock transactions

**Bahasa Indonesia companion:** `stock-ledger-domain-id.md` — not yet created

---

## 1. Business Overview

### 1.1 Purpose and value

Stock Ledger provides the authoritative record of inventory quantity movement, stock provenance, remaining quantity, and unit valuation across stock locations.

The domain ensures that every accountable inventory quantity can be traced to its original Receipt Source throughout receipt, transfer, consumption, return, correction, and other inventory consequences.

Stock Ledger operates behind business transactions owned by other bounded contexts. Those contexts determine why inventory must move. Stock Ledger determines and records the accountable inventory consequence.

The domain must ensure that:

* every stock quantity retains its Item, Receipt Source, Stock Location, and Unit Valuation;
* movement between locations preserves the original Receipt Source;
* stock consumption follows the applicable inventory consumption policy;
* FIFO consumption remains traceable to the Stock Layers consumed;
* depleted Stock Layers remain part of the authoritative stock position;
* the movement ledger and current stock position can be reconciled within a bounded scope;
* one source business transaction does not produce duplicate stock consequences;
* corrections and reversals preserve the original recorded facts;
* legacy stock provenance may be reconstructed incrementally without requiring full historical migration; and
* reconstructed facts remain distinguishable from facts recorded natively by the new Stock Ledger.

### 1.2 Scope

This context covers:

1. recognition of accountable stock receipts;
2. establishment and maintenance of Stock Layers;
3. stock movement recording;
4. stock transfer between Stock Locations;
5. FIFO stock consumption;
6. stock returns and accountable adjustments;
7. remaining-quantity and unit-valuation tracking;
8. preservation of depleted Stock Layers;
9. reconciliation by Item and Receipt Source;
10. incremental Legacy Stock Reconstruction;
11. detection of duplicate or inconsistent stock consequences; and
12. publication of authoritative stock movement and stock position outcomes.

### 1.3 Business boundaries

Stock Ledger owns:

* Stock Movement;
* Stock Layer;
* Stock Position by Item, Receipt Source, and Stock Location;
* FIFO allocation of outbound quantities;
* provenance continuity;
* inventory quantity consequences;
* unit-valuation continuity within a Stock Layer;
* stock reconciliation;
* stock correction and reversal consequences; and
* Legacy Stock Reconstruction outcomes.

Stock Ledger relies on other bounded contexts without taking over their authority:

* Purchasing or Goods Receipt owns the commercial and operational fact that goods were received;
* Pharmacy, Sales, Clinical Supply, or another fulfillment context owns the fact that goods were supplied or sold;
* Stock Transfer owns the operational intent and authorization to move inventory;
* Stock Opname owns the physical counting activity and observed quantity;
* Return Processing owns the business reason and authorization for a return;
* Product Catalog owns Item identity and unit-of-measure definitions;
* Organizational or Facility Management owns Stock Location identity; and
* Finance or Accounting owns financial accounting consequences beyond Stock Ledger unit valuation.

Stock Ledger does not determine whether a sale, receipt, transfer, return, or adjustment should occur. It records the stock consequence only after receiving an accountable source business fact or authorized request.

### 1.4 Information authority

| Business fact                      | Authoritative owner                  |
| ---------------------------------- | ------------------------------------ |
| Item identity                      | Product Catalog                      |
| Stock Location identity            | Facility or Organizational authority |
| Commercial goods receipt           | Purchasing or Goods Receipt          |
| Source transaction identity        | Originating bounded context          |
| Receipt Source identity            | Originating receipt authority        |
| Requested transfer                 | Stock Transfer                       |
| Sale or supply fulfillment         | Originating fulfillment context      |
| Physical count result              | Stock Opname                         |
| Authorized stock adjustment        | Responsible inventory authority      |
| Stock Movement                     | Stock Ledger                         |
| Stock Layer provenance             | Stock Ledger                         |
| Remaining quantity per Stock Layer | Stock Ledger                         |
| FIFO allocation                    | Stock Ledger                         |
| Stock Position                     | Stock Ledger                         |
| Reconciliation result              | Stock Ledger                         |
| Financial accounting entry         | Finance or Accounting                |

Stock Ledger shall not infer that a business transaction is valid merely because an inventory movement is technically possible.

### 1.5 Central business model

```text
Source Business Fact
  -> Stock Consequence Request
       -> Stock Movement
            -> Stock Layer established, increased, decreased, or transferred
                 -> Stock Position updated
                      -> Reconciliation available
```

The principal provenance relationship is:

```text
Item
  + Receipt Source
      -> one or more Stock Layers
           -> distributed across one or more Stock Locations
                -> consumed or moved while retaining the same provenance
```

A Stock Layer may reach zero Remaining Quantity, but it remains an accountable part of the Stock Position history.

### 1.6 Supporting-domain character

Stock Ledger is a supporting bounded context. It may have no direct user-facing workflow for ordinary transactions.

Its business behavior is primarily triggered by facts and requests from other bounded contexts. Lack of direct user interaction does not reduce its authority over inventory movement, provenance, balance, and reconciliation.

---

## 2. Ubiquitous Language

| Term                         | Definition                                                                                                                                                                   |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Stock Ledger                 | The bounded context that authoritatively records inventory movements, provenance, remaining quantities, and reconciliation outcomes.                                         |
| Item                         | A uniquely identified inventory-managed product.                                                                                                                             |
| Stock Location               | A business location at which inventory is held, such as a warehouse, pharmacy, ward, clinic, or emergency unit.                                                              |
| Receipt Source               | The accountable identity of the event or document through which inventory originally entered the organization. In the current legacy model, this is represented by `KodeDO`. |
| Stock Provenance             | The traceable origin of inventory from its Receipt Source through all subsequent transfers, consumption, returns, and corrections.                                           |
| Stock Layer                  | An accountable quantity of one Item associated with one Receipt Source, one Stock Location, one unit valuation, and one layer-forming movement.                              |
| Layer-Forming Movement       | A Stock Movement that establishes a Stock Layer at a Stock Location.                                                                                                         |
| Initial Quantity             | The quantity held by a Stock Layer when that layer is established.                                                                                                           |
| Remaining Quantity           | The quantity currently available or accountable within a Stock Layer.                                                                                                        |
| Depleted Stock Layer         | A Stock Layer whose Remaining Quantity is zero. It remains part of the authoritative Stock Position and is not discarded.                                                    |
| Unit Valuation               | The inventory value per unit retained by a Stock Layer.                                                                                                                      |
| Stock Position               | The authoritative current quantity representation of Stock Layers for a defined Item, Receipt Source, and Stock Location scope.                                              |
| Stock Movement               | An immutable business fact that inventory quantity entered, left, moved between locations, returned, or was adjusted.                                                        |
| Stock Movement Line          | One quantity consequence within a Stock Movement, associated with an Item, Receipt Source, Stock Location, direction, quantity, and Unit Valuation.                          |
| Source Business Fact         | An authoritative fact from another bounded context that provides the business reason for a stock consequence.                                                                |
| Source Transaction Reference | The stable identity connecting a Stock Movement to the business transaction that caused it.                                                                                  |
| Stock Receipt                | A Stock Movement that recognizes inventory entering Stock Ledger authority from an external source.                                                                          |
| Stock Transfer               | A coordinated outbound and inbound inventory movement between two Stock Locations while preserving Item, Receipt Source, and Unit Valuation.                                 |
| Stock Consumption            | An outbound Stock Movement caused by sale, dispensing, usage, damage, expiry, or another accountable final disposition.                                                      |
| FIFO                         | The policy that consumes eligible Stock Layers in their applicable order from oldest to newest.                                                                              |
| FIFO Allocation              | The accountable distribution of one outbound quantity across one or more eligible Stock Layers.                                                                              |
| Stock Return                 | A movement that returns previously moved or consumed inventory to an accountable Stock Location when permitted.                                                              |
| Stock Adjustment             | An authorized inventory quantity correction resulting from an accountable discrepancy or business decision.                                                                  |
| Stock Correction             | A new accountable fact that corrects an earlier Stock Movement without erasing the original fact.                                                                            |
| Stock Reversal               | A Stock Movement that counteracts a previous Stock Movement while retaining both facts.                                                                                      |
| Stock Reconciliation         | The assessment that Stock Movements and Stock Position agree within a defined Reconciliation Scope.                                                                          |
| Reconciliation Scope         | The bounded set of inventory facts evaluated together. The primary scope is one Item and one Receipt Source across all Stock Locations.                                      |
| Reconciliation Difference    | A quantity or valuation difference found during Stock Reconciliation.                                                                                                        |
| Native Stock Fact            | A Stock Layer or Stock Movement recorded directly under the new Stock Ledger rules.                                                                                          |
| Legacy Stock Fact            | A stock fact originating from the legacy inventory model.                                                                                                                    |
| Legacy Stock Reconstruction  | The accountable reconstruction of missing Stock Layers from available legacy movement history.                                                                               |
| Reconstructed Stock Layer    | A Stock Layer established from Legacy Stock Reconstruction rather than from a native Stock Receipt.                                                                          |
| Reconstruction Scope         | The Item and Receipt Source whose complete legacy provenance is reconstructed across all Stock Locations.                                                                    |
| Reconstruction Trigger       | The first eligible stock processing activity that requires a previously unreconstructed Item and Receipt Source.                                                             |
| Reconstruction Status        | The accountable state indicating whether a Reconstruction Scope is not reconstructed, being reconstructed, reconstructed, or found inconsistent.                             |
| Provenance Continuity        | The rule that Receipt Source and Unit Valuation remain traceable across transfers and consumption.                                                                           |
| Inventory Conservation       | The rule that quantities entering a provenance scope equal quantities remaining plus accountable outbound and adjustment outcomes.                                           |
| Stock Consequence            | The inventory quantity effect produced from an authorized Source Business Fact.                                                                                              |
| Duplicate Stock Consequence  | More than one authoritative Stock Movement created for the same source transaction responsibility.                                                                           |
| Legacy Stock Projection      | A compatibility representation maintained for legacy consumers without replacing Stock Ledger authority.                                                                     |

---

## 3. Business Capabilities

### 3.1 Stock Receipt Recognition

Recognize accountable inventory entering the organization and establish its Receipt Source, Initial Quantity, Unit Valuation, and first Stock Layer.

### 3.2 Stock Provenance Management

Preserve the relationship between inventory quantity and its Receipt Source across every Stock Location and movement.

### 3.3 Stock Layer Management

Establish and maintain Stock Layers, including layers whose Remaining Quantity has reached zero.

### 3.4 Stock Movement Recording

Record immutable inbound, outbound, transfer, return, adjustment, correction, and reversal consequences.

### 3.5 FIFO Consumption

Allocate outbound inventory demand to eligible Stock Layers in FIFO order and retain the exact quantity consumed from each layer.

### 3.6 Stock Transfer Coordination

Coordinate outbound and inbound movement between Stock Locations while preserving Receipt Source and Unit Valuation.

### 3.7 Stock Position Management

Maintain authoritative Remaining Quantity by Item, Receipt Source, Stock Location, and Stock Layer.

### 3.8 Stock Reconciliation

Validate that Stock Movements and Stock Position remain quantitatively consistent for one Item and Receipt Source across all Stock Locations.

### 3.9 Stock Correction and Reversal

Correct inventory consequences through new accountable facts without deleting or silently rewriting completed Stock Movements.

### 3.10 Legacy Stock Reconstruction

Incrementally reconstruct previously deleted or unavailable Stock Layers when an Item and Receipt Source first require processing under the new Stock Ledger.

### 3.11 Cross-Context Stock Consequence Coordination

Accept accountable source facts from other bounded contexts and publish authoritative stock outcomes without taking ownership of the originating business transaction.

### 3.12 Legacy Compatibility

Support continued operation of legacy stock consumers while Stock Ledger authority is adopted incrementally.

---

## 4. Actors & Roles

Stock Ledger is primarily triggered by other bounded contexts. Human roles are involved where business authorization or exception decisions are required.

### 4.1 Inventory Officer

Initiates or confirms permitted inventory activities within assigned authority and investigates stock discrepancies.

### 4.2 Receiving Officer

Confirms the physical receipt information required by the responsible Goods Receipt process. The Receiving Officer does not independently define Stock Ledger provenance outside an accountable receipt.

### 4.3 Stock Transfer Officer

Carries out authorized movement between Stock Locations and remains responsible for transfer evidence.

### 4.4 Stock Opname Officer

Performs physical stock counting and supplies the observed quantity to the responsible Stock Opname process.

### 4.5 Inventory Adjustment Authorizer

Approves accountable stock adjustments, corrections, or exceptional quantity resolutions beyond ordinary transaction authority.

### 4.6 Inventory Controller

Reviews reconciliation outcomes, investigates differences, and determines the required accountable follow-up.

### 4.7 System Administrator

May support technical operation but does not own stock quantity, provenance, valuation, reconciliation, or correction decisions.

---

## 5. Domain Objects

### 5.1 Stock Movement

Represents an authoritative inventory quantity consequence.

A Stock Movement identifies:

* its Source Transaction Reference;
* its business movement type;
* its effective business time;
* its responsible source;
* its Stock Movement Lines; and
* any relationship to a correcting or reversed movement.

A completed Stock Movement is immutable. A later correction creates another Stock Movement.

### 5.2 Stock Movement Line

Represents one inbound or outbound inventory effect.

Each line identifies:

* Item;
* Receipt Source;
* Stock Location;
* movement direction;
* quantity;
* Unit Valuation; and
* affected Stock Layer when applicable.

A transfer is represented by coordinated outbound and inbound lines.

### 5.3 Stock Layer

Represents one accountable inventory quantity layer at one Stock Location.

A Stock Layer retains:

* Item;
* Receipt Source;
* Stock Location;
* layer-forming movement;
* Initial Quantity;
* Remaining Quantity;
* Unit Valuation;
* layer ordering information required by FIFO;
* origin classification as native or reconstructed; and
* depletion status.

A Stock Layer remains authoritative when its Remaining Quantity reaches zero.

### 5.4 Stock Position

Represents the current accountable quantities of Stock Layers within a defined scope.

Stock Position may be viewed by:

* Item and Stock Location;
* Item and Receipt Source;
* Item, Receipt Source, and Stock Location; or
* individual Stock Layer.

Stock Position does not replace Stock Movement history.

### 5.5 FIFO Allocation

Represents how one outbound inventory requirement is satisfied from one or more Stock Layers.

It preserves:

* requested outbound quantity;
* Stock Layers selected;
* quantity consumed from each layer;
* Unit Valuation of each consumed quantity; and
* unfulfilled quantity when insufficient eligible stock exists.

### 5.6 Stock Transfer

Represents the coordinated inventory consequence of moving quantity between Stock Locations.

It preserves the same:

* Item;
* Receipt Source;
* Unit Valuation; and
* transferred quantity.

A destination Stock Layer is formed from the transferred provenance. It is not a new Receipt Source.

### 5.7 Stock Reconciliation

Represents one assessment of movement and position consistency within a Reconciliation Scope.

It records:

* Item;
* Receipt Source;
* included Stock Locations;
* total recognized receipt quantity;
* total accountable outbound quantity;
* total remaining quantity;
* total adjustment consequence;
* reconciliation result;
* any difference; and
* effective business time.

### 5.8 Legacy Stock Reconstruction

Represents the accountable reconstruction of Stock Layers for one Item and Receipt Source across all Stock Locations.

It identifies:

* Reconstruction Scope;
* legacy facts considered;
* reconstructed layers;
* unresolved inconsistencies;
* reconstruction result; and
* distinction between reconstructed and native facts.

Legacy Stock Reconstruction does not invent unavailable source facts.

### 5.9 Stock Correction

Represents the relationship between an incorrect or incomplete Stock Movement and the later movement that corrects it.

The original movement remains visible and authoritative as an historical fact.

### 5.10 Stock Consequence Request

Represents the business instruction supplied by another bounded context requesting an inventory consequence.

It identifies:

* source business responsibility;
* Source Transaction Reference;
* requested consequence;
* Item;
* quantity;
* applicable Stock Location;
* Receipt Source when already determined; and
* effective business time.

Acceptance of a Stock Consequence Request does not transfer ownership of the source business transaction to Stock Ledger.

---

## 6. Aggregates

### 6.1 Stock Movement Aggregate

**Aggregate Root:** `Stock Movement`

The aggregate owns Stock Movement Lines and keeps the following facts mutually consistent:

* Source Transaction Reference;
* movement type;
* movement direction;
* affected quantities;
* affected Stock Locations;
* Receipt Source;
* Unit Valuation;
* transfer pairing;
* correction or reversal relationship; and
* movement completion.

For a transfer, the aggregate ensures that outbound and inbound quantities are equal and that provenance is preserved.

A completed Stock Movement is immutable.

### 6.2 Stock Position Aggregate

**Aggregate Root:** `Stock Position`

The aggregate represents one Item and Receipt Source as its primary provenance scope.

It owns the Stock Layers for that provenance across Stock Locations and keeps mutually consistent:

* Initial Quantity;
* Remaining Quantity;
* Stock Location;
* Unit Valuation;
* FIFO ordering;
* native or reconstructed origin;
* depletion state; and
* total remaining quantity.

The aggregate retains depleted Stock Layers.

A Stock Position does not own the source commercial transaction, Product Catalog, or Stock Location master data.

### 6.3 Stock Reconciliation Aggregate

**Aggregate Root:** `Stock Reconciliation`

The aggregate evaluates one Item and Receipt Source across all Stock Locations.

It owns:

* reconciliation inputs;
* calculated movement totals;
* Stock Position totals;
* identified differences;
* reconciliation outcome;
* responsible reviewer when applicable; and
* resolution reference when a difference requires correction.

A reconciliation result does not silently alter Stock Movement or Stock Position.

### 6.4 Legacy Stock Reconstruction Aggregate

**Aggregate Root:** `Legacy Stock Reconstruction`

The aggregate coordinates reconstruction for one Item and Receipt Source.

It owns:

* Reconstruction Scope;
* Reconstruction Status;
* reconstructed provenance interpretation;
* reconstructed Stock Layers;
* unresolved ambiguities;
* reconstruction completion outcome; and
* provenance classification.

Only one active reconstruction may exist for the same Item and Receipt Source.

### 6.5 Cross-aggregate relationships

A Stock Movement may establish, increase, decrease, transfer, deplete, or correct Stock Layers within one or more Stock Positions.

A Stock Reconciliation reads authoritative Stock Movements and the applicable Stock Position but does not own either.

Legacy Stock Reconstruction establishes previously missing Stock Layers and marks them as reconstructed. Subsequent native Stock Movements continue from the reconstructed Stock Position.

Cross-aggregate coordination must preserve:

* one authoritative consequence per source responsibility;
* Item and Receipt Source consistency;
* quantity conservation;
* Unit Valuation continuity; and
* correction traceability.

---

## 7. Business Rules

### 7.1 Source authority and stock consequences

* **BR-STL-001** — Every Stock Movement shall originate from exactly one accountable Source Business Fact, authorized adjustment, reconstruction outcome, correction, or reversal.
* **BR-STL-002** — Stock Ledger shall not create the business reason for a receipt, sale, dispensing, transfer, return, or Stock Opname.
* **BR-STL-003** — Every Stock Movement shall retain a Source Transaction Reference sufficient to trace it to its originating business responsibility.
* **BR-STL-004** — One source transaction responsibility shall produce at most one active authoritative Stock Consequence of the same type.
* **BR-STL-005** — Repeating the same source responsibility shall not duplicate inventory quantity.
* **BR-STL-006** — Stock Ledger shall reject or identify a requested consequence whose Item, quantity, location, or provenance conflicts with the authoritative source facts.

### 7.2 Receipt Source and provenance

* **BR-STL-007** — Every inventory quantity recognized by Stock Ledger shall have exactly one Receipt Source.
* **BR-STL-008** — Receipt Source shall remain unchanged throughout transfer, consumption, return, correction, and reconciliation.
* **BR-STL-009** — A Stock Transfer shall not create a new Receipt Source.
* **BR-STL-010** — A destination Stock Layer created by transfer shall retain the Receipt Source and Unit Valuation of the source quantity.
* **BR-STL-011** — Quantities from different Receipt Sources shall remain separately accountable even when they represent the same Item at the same Stock Location.
* **BR-STL-012** — Stock Ledger shall not merge Stock Layers when doing so would remove Receipt Source, Unit Valuation, FIFO, or movement traceability.

### 7.3 Stock Layer

* **BR-STL-013** — Every Stock Layer shall identify one Item, one Receipt Source, one Stock Location, one layer-forming movement, and one Unit Valuation.
* **BR-STL-014** — Initial Quantity shall be positive when a Stock Layer is established.
* **BR-STL-015** — Remaining Quantity shall not exceed Initial Quantity except through an accountable inbound correction, return, or adjustment that explicitly increases the layer.
* **BR-STL-016** — Remaining Quantity shall not become negative.
* **BR-STL-017** — A Stock Layer whose Remaining Quantity becomes zero shall remain part of the authoritative Stock Position.
* **BR-STL-018** — A Depleted Stock Layer shall not be silently deleted or reused as a different layer.
* **BR-STL-019** — Unit Valuation shall represent value per inventory unit, not total layer value.
* **BR-STL-020** — Unit Valuation shall remain unchanged for quantity preserving the same provenance unless an accountable valuation correction is recorded.
* **BR-STL-021** — A new Receipt Source with a different Unit Valuation shall establish a separate Stock Layer.

### 7.4 Stock movement

* **BR-STL-022** — A completed Stock Movement shall be immutable.
* **BR-STL-023** — A Stock Movement shall record positive quantities and express direction separately as inbound or outbound.
* **BR-STL-024** — An outbound Stock Movement shall identify the Stock Layers from which quantity was taken.
* **BR-STL-025** — An inbound Stock Movement shall identify whether it establishes a new Stock Layer, restores an existing provenance, or results from a transfer.
* **BR-STL-026** — Movement effective business time and recording time shall remain distinguishable when they differ.
* **BR-STL-027** — Every material Stock Movement shall retain responsible source and effective business time.
* **BR-STL-028** — A completed Stock Movement shall not be erased because its quantity has been fully consumed.

### 7.5 FIFO consumption

* **BR-STL-029** — Outbound consumption shall use FIFO unless an authoritative policy explicitly permits another allocation.
* **BR-STL-030** — FIFO eligibility shall be evaluated within the applicable Item and Stock Location.
* **BR-STL-031** — FIFO shall select the oldest eligible Stock Layer before a newer eligible Stock Layer.
* **BR-STL-032** — A single outbound requirement may consume quantities from multiple Stock Layers.
* **BR-STL-033** — Every quantity consumed from a Stock Layer shall retain that layer's Receipt Source and Unit Valuation.
* **BR-STL-034** — When one outbound requirement consumes multiple Stock Layers, Stock Ledger shall record a separate accountable quantity consequence for each consumed layer.
* **BR-STL-035** — Stock Ledger shall not consume more than the eligible Remaining Quantity.
* **BR-STL-036** — Insufficient eligible stock shall produce an explicit unfulfilled quantity or rejection; it shall not produce negative stock.
* **BR-STL-037** — Depleted Stock Layers shall be excluded from subsequent FIFO selection but retained for traceability and reconciliation.

### 7.6 Stock transfer

* **BR-STL-038** — Every completed Stock Transfer shall contain equal outbound and inbound quantities.
* **BR-STL-039** — A Stock Transfer shall identify one source Stock Location and one destination Stock Location.
* **BR-STL-040** — Source and destination Stock Locations shall not be the same for an ordinary Stock Transfer.
* **BR-STL-041** — A Stock Transfer may consume multiple source Stock Layers and establish corresponding destination Stock Layers.
* **BR-STL-042** — Each destination Stock Layer shall remain traceable to the source Stock Layer quantity from which it was formed.
* **BR-STL-043** — Transfer completion shall preserve total quantity for each Item and Receipt Source.
* **BR-STL-044** — A transfer discrepancy shall receive an accountable exception, adjustment, loss, return, or correction outcome.

### 7.7 Inventory conservation and reconciliation

* **BR-STL-045** — Reconciliation Scope shall primarily be defined by one Item and one Receipt Source across all Stock Locations.
* **BR-STL-046** — Reconciliation shall include depleted Stock Layers.
* **BR-STL-047** — Reconciliation shall include every accountable movement associated with the applicable Item and Receipt Source.
* **BR-STL-048** — Total quantity recognized for one Item and Receipt Source shall equal total Remaining Quantity plus all accountable final outbound quantities and net adjustment consequences.
* **BR-STL-049** — Movement between Stock Locations shall not change total quantity within the same Item and Receipt Source scope.
* **BR-STL-050** — The sum of Remaining Quantity across all Stock Layers in a Reconciliation Scope shall equal the authoritative current Stock Position for that scope.
* **BR-STL-051** — A Reconciliation Difference shall be recorded explicitly and shall not be resolved by silently rewriting completed movements.
* **BR-STL-052** — A reconciliation result shall identify its scope, effective time, compared totals, and outcome.
* **BR-STL-053** — A successful reconciliation shall not prove that the originating commercial or operational transaction was otherwise correct.

The primary quantity invariant is:

```text
Recognized Receipt Quantity
+ Accountable Inbound Adjustments
=
Remaining Quantity Across All Locations
+ Final Outbound Quantity
+ Accountable Outbound Adjustments
```

Internal transfers are excluded from both sides of this equation because they preserve total quantity within the Receipt Source.

### 7.8 Correction and reversal

* **BR-STL-054** — A completed Stock Movement shall be corrected through a new Stock Correction or Stock Reversal.
* **BR-STL-055** — A Stock Correction shall reference the movement or source fact being corrected.
* **BR-STL-056** — A Stock Reversal shall preserve the original Stock Movement and record the counteracting quantity consequence.
* **BR-STL-057** — Correction and reversal shall preserve Receipt Source and Unit Valuation unless the correction specifically addresses incorrect provenance or valuation.
* **BR-STL-058** — A correction that changes provenance shall retain traceability to both the previously recorded and corrected provenance.
* **BR-STL-059** — A correction shall not cause Remaining Quantity to become negative.
* **BR-STL-060** — Corrected facts and original facts shall remain separately visible for reconciliation.

### 7.9 Legacy stock reconstruction

* **BR-STL-061** — Legacy Stock Reconstruction shall be triggered only when an unreconstructed Item and Receipt Source require processing under the new Stock Ledger.
* **BR-STL-062** — Reconstruction Scope shall include the Item and Receipt Source across all Stock Locations.
* **BR-STL-063** — Reconstruction shall not be limited to the Stock Location that triggered it.
* **BR-STL-064** — Legacy Stock Reconstruction shall use available accountable legacy movement facts and shall not invent unavailable historical identities.
* **BR-STL-065** — A legacy Stock Layer identity that no longer exists shall not be recreated as though its original identifier were known.
* **BR-STL-066** — Reconstructed Stock Layers shall receive new accountable identities while retaining the available Item, Receipt Source, Stock Location, Unit Valuation, and movement provenance.
* **BR-STL-067** — Reconstructed Stock Layers with zero Remaining Quantity shall be retained.
* **BR-STL-068** — Reconstructed facts shall remain distinguishable from Native Stock Facts.
* **BR-STL-069** — One Item and Receipt Source shall have at most one completed authoritative reconstruction outcome.
* **BR-STL-070** — Repeated reconstruction processing shall not duplicate Stock Layers or quantities.
* **BR-STL-071** — Native Stock Movements shall not proceed against an unreconstructed Item and Receipt Source when doing so would create incomplete provenance or reconciliation.
* **BR-STL-072** — A reconstruction inconsistency shall be recorded and surfaced for accountable resolution rather than silently balanced.
* **BR-STL-073** — Reconstruction completion shall establish the baseline from which subsequent native movements continue.
* **BR-STL-074** — Legacy reconstruction shall not require migration of all historical inventory before the new Stock Ledger may operate.

### 7.10 Legacy compatibility

* **BR-STL-075** — Legacy stock representations may continue to be supplied while required by existing consumers.
* **BR-STL-076** — A Legacy Stock Projection shall not override authoritative Stock Ledger facts.
* **BR-STL-077** — Differences between Stock Ledger and a Legacy Stock Projection shall be detectable and accountably resolved.
* **BR-STL-078** — Compatibility requirements shall not require deletion of Depleted Stock Layers from Stock Ledger.
* **BR-STL-079** — The inability of a legacy representation to preserve a fact shall not remove that fact from Stock Ledger authority.

### 7.11 Completion and traceability

* **BR-STL-080** — Every stock quantity shall remain traceable from its Receipt Source to its current Stock Layer or accountable final outbound outcome.
* **BR-STL-081** — Every outbound quantity shall identify the Stock Layer quantities consumed.
* **BR-STL-082** — Every transferred quantity shall identify both source and destination Stock Locations.
* **BR-STL-083** — Every reconstructed quantity shall identify its Reconstruction Scope and reconstruction outcome.
* **BR-STL-084** — Every correction shall preserve the original fact and the correcting fact.
* **BR-STL-085** — Stock Ledger shall retain sufficient provenance to explain current quantity and unit valuation without requiring organization-wide replay of all historical inventory movements.

---

## 8. State Machines & Lifecycles

### 8.1 Stock Movement lifecycle

```text
Proposed
  -> Recorded
       -> Reversed
       -> Corrected
```

| State     | Business meaning                                                                                          |
| --------- | --------------------------------------------------------------------------------------------------------- |
| Proposed  | An accountable source has requested a stock consequence, but no authoritative movement has been recorded. |
| Recorded  | The Stock Movement is authoritative and immutable.                                                        |
| Reversed  | A later Stock Reversal counteracted the movement while preserving it.                                     |
| Corrected | A later Stock Correction amended its business consequence while preserving the original movement.         |

`Reversed` and `Corrected` describe the movement's later disposition. They do not modify its original recorded content.

### 8.2 Stock Layer lifecycle

```text
Established
  -> Active
       -> Depleted

Active or Depleted
  -> Corrected
```

| State       | Business meaning                                                                           |
| ----------- | ------------------------------------------------------------------------------------------ |
| Established | The Stock Layer was formed with an Initial Quantity and provenance.                        |
| Active      | Remaining Quantity is greater than zero.                                                   |
| Depleted    | Remaining Quantity is zero. The layer remains authoritative.                               |
| Corrected   | A later accountable correction changed the quantity, provenance, or valuation consequence. |

A Depleted Stock Layer is not deleted. An accountable return or correction may restore positive Remaining Quantity when business policy permits.

### 8.3 Stock Position lifecycle

```text
Uninitialized
  -> Established
       -> Active
       -> Fully Depleted
       -> Inconsistent
```

| State          | Business meaning                                                                          |
| -------------- | ----------------------------------------------------------------------------------------- |
| Uninitialized  | No authoritative native or reconstructed position exists for the Item and Receipt Source. |
| Established    | The provenance position has been recognized.                                              |
| Active         | At least one Stock Layer has positive Remaining Quantity.                                 |
| Fully Depleted | Every Stock Layer has zero Remaining Quantity, but the Stock Position remains retained.   |
| Inconsistent   | A reconciliation or reconstruction difference requires accountable resolution.            |

A later receipt under the same Receipt Source is permitted only when the source authority confirms that it belongs to the same accountable receipt responsibility.

### 8.4 Stock Reconciliation lifecycle

```text
Requested
  -> Evaluating
       -> Balanced
       -> Difference Identified
            -> Resolved
```

| State                 | Business meaning                                                                     |
| --------------------- | ------------------------------------------------------------------------------------ |
| Requested             | A reconciliation is required for a defined Item and Receipt Source.                  |
| Evaluating            | Movement and position facts are being compared.                                      |
| Balanced              | No quantity or applicable valuation difference was found.                            |
| Difference Identified | An accountable discrepancy exists.                                                   |
| Resolved              | The difference received an authorized correction, explanation, or final disposition. |

A reconciliation may be performed again after resolution. Earlier reconciliation outcomes remain historical facts.

### 8.5 Legacy Stock Reconstruction lifecycle

```text
Not Reconstructed
  -> Reconstruction Required
       -> Reconstructing
            -> Reconstructed
            -> Inconsistent
```

| State                   | Business meaning                                                                                         |
| ----------------------- | -------------------------------------------------------------------------------------------------------- |
| Not Reconstructed       | The Item and Receipt Source still rely solely on legacy representation.                                  |
| Reconstruction Required | A new Stock Ledger activity requires authoritative provenance to be established.                         |
| Reconstructing          | Available legacy facts are being interpreted within the complete Reconstruction Scope.                   |
| Reconstructed           | The authoritative reconstructed baseline is available for native processing.                             |
| Inconsistent            | Available legacy facts cannot produce a complete balanced reconstruction without accountable resolution. |

### 8.6 FIFO allocation lifecycle

```text
Requested Quantity
  -> Allocated
       -> Fully Allocated
       -> Partially Allocated
       -> Not Allocated
```

| State               | Business meaning                                                   |
| ------------------- | ------------------------------------------------------------------ |
| Requested Quantity  | An outbound inventory consequence requires Stock Layer allocation. |
| Allocated           | One or more eligible Stock Layers have been selected.              |
| Fully Allocated     | The entire requested quantity is supported.                        |
| Partially Allocated | Only part of the requested quantity is supported.                  |
| Not Allocated       | No eligible quantity is available.                                 |

---

## 9. Domain Events

| Domain Event                               | Business meaning                                                                          |
| ------------------------------------------ | ----------------------------------------------------------------------------------------- |
| Stock Consequence Requested                | An accountable source requested an inventory quantity effect.                             |
| Stock Receipt Recorded                     | Inventory entering the organization was recognized by Stock Ledger.                       |
| Stock Layer Established                    | A new Stock Layer was formed with accountable provenance.                                 |
| Stock Layer Depleted                       | A Stock Layer reached zero Remaining Quantity.                                            |
| Stock Position Established                 | An authoritative Item and Receipt Source position became available.                       |
| Stock Movement Recorded                    | An inventory inbound or outbound consequence became authoritative.                        |
| FIFO Allocation Completed                  | An outbound quantity was allocated to one or more Stock Layers.                           |
| FIFO Allocation Partially Completed        | Only part of the requested quantity was supported by eligible layers.                     |
| Stock Consumption Recorded                 | Inventory quantity was removed from one or more Stock Layers.                             |
| Stock Transfer Recorded                    | Equal outbound and inbound quantities were recorded across two Stock Locations.           |
| Stock Returned                             | Inventory quantity was returned to an accountable Stock Location.                         |
| Stock Adjustment Recorded                  | An authorized quantity difference was recognized.                                         |
| Stock Movement Reversed                    | A new movement counteracted a previously recorded movement.                               |
| Stock Movement Corrected                   | A new accountable fact corrected an earlier movement consequence.                         |
| Stock Reconciliation Requested             | A defined Item and Receipt Source scope was selected for validation.                      |
| Stock Reconciliation Balanced              | Movement and Stock Position totals agreed within the scope.                               |
| Stock Reconciliation Difference Identified | A difference was found between authoritative movement and position facts.                 |
| Stock Reconciliation Difference Resolved   | An identified difference received an accountable resolution.                              |
| Legacy Stock Reconstruction Required       | A previously unreconstructed Item and Receipt Source were required for native processing. |
| Legacy Stock Reconstruction Started        | Reconstruction began across the complete Item and Receipt Source scope.                   |
| Legacy Stock Layer Reconstructed           | A Stock Layer was established from available legacy history.                              |
| Legacy Stock Reconstruction Completed      | A balanced reconstructed baseline became authoritative.                                   |
| Legacy Stock Reconstruction Failed         | Available legacy facts could not produce an accountable reconstruction.                   |
| Duplicate Stock Consequence Detected       | More than one consequence was requested or found for the same source responsibility.      |
| Legacy Projection Difference Detected      | A compatibility representation differed from authoritative Stock Ledger facts.            |

---

## 10. Business Workflows

### 10.1 Recognize Stock Receipt

```text
Goods Receipt Confirmed
  -> validate accountable source
  -> establish Receipt Source
  -> record Stock Receipt
  -> establish Stock Layer
  -> establish or update Stock Position
  -> publish Stock Receipt Recorded
```

**Outcome:** Inventory entering the organization becomes traceable by Item, Receipt Source, Stock Location, Initial Quantity, Remaining Quantity, and Unit Valuation.

### 10.2 Transfer Stock Between Locations

```text
Stock Transfer Authorized
  -> identify eligible source Stock Layers
  -> allocate transfer quantity
  -> record outbound Stock Movement
  -> establish destination Stock Layers
  -> record equal inbound Stock Movement
  -> preserve Receipt Source and Unit Valuation
  -> publish Stock Transfer Recorded
```

**Outcome:** Inventory moves between Stock Locations without changing total quantity or provenance.

### 10.3 Consume Stock Using FIFO

```text
Outbound Stock Consequence Requested
  -> identify Item and Stock Location
  -> identify eligible Stock Layers
  -> order layers by FIFO
  -> allocate quantity across one or more layers
  -> reduce Remaining Quantity
  -> retain depleted layers
  -> record Stock Movement Lines per consumed layer
  -> publish Stock Consumption Recorded
```

**Outcome:** The outbound quantity remains traceable to every Receipt Source and Unit Valuation consumed.

### 10.4 Record Stock Return

```text
Stock Return Authorized
  -> identify original movement and provenance
  -> validate permitted return quantity
  -> record inbound Stock Movement
  -> restore or establish accountable Stock Layer
  -> update Stock Position
  -> publish Stock Returned
```

**Outcome:** Returned quantity is reintroduced without losing its original provenance or movement relationship.

### 10.5 Record Stock Adjustment

```text
Stock Discrepancy Confirmed
  -> obtain adjustment authorization
  -> identify Item, Receipt Source, Stock Location, and affected quantity
  -> record adjustment Stock Movement
  -> update Stock Layer and Stock Position
  -> preserve reason and responsible authority
  -> publish Stock Adjustment Recorded
```

**Outcome:** A quantity difference receives an accountable consequence rather than a silent Stock Position rewrite.

### 10.6 Correct or Reverse Stock Movement

```text
Incorrect Stock Consequence Identified
  -> retain original Stock Movement
  -> authorize correction or reversal
  -> record correcting Stock Movement
  -> update affected Stock Layers
  -> reconcile affected Item and Receipt Source
  -> publish correction or reversal event
```

**Outcome:** The original and correcting facts remain visible and quantitatively accountable.

### 10.7 Reconcile Item and Receipt Source

```text
Reconciliation Requested
  -> select one Item and Receipt Source
  -> include all Stock Locations
  -> include active and depleted Stock Layers
  -> total accountable Stock Movements
  -> compare with Stock Position
       -> Balanced
       -> Difference Identified
  -> publish reconciliation outcome
```

**Outcome:** Inventory consistency can be validated without replaying unrelated Items or Receipt Sources from the beginning of organizational history.

### 10.8 Reconstruct Legacy Stock on First Use

```text
Native Stock Processing Requested
  -> detect unreconstructed Item and Receipt Source
  -> establish Reconstruction Required
  -> collect available legacy movement facts
  -> reconstruct provenance across all Stock Locations
  -> establish active and depleted Reconstructed Stock Layers
  -> reconcile reconstructed scope
       -> Reconstructed
       -> Inconsistent
  -> continue or block native processing according to outcome
```

**Outcome:** Legacy provenance becomes available incrementally for the Item and Receipt Source actually being processed, without requiring complete migration of decades of inventory history.

### 10.9 Coordinate Cross-Context Stock Consequence

```text
Source Business Fact Published
  -> validate source responsibility and reference
  -> detect duplicate consequence
  -> determine applicable stock behavior
  -> record authoritative Stock Movement
  -> update Stock Position
  -> publish stock outcome to the source context
```

**Outcome:** Other bounded contexts receive an authoritative inventory consequence while retaining ownership of their original business transaction.

### 10.10 Maintain Legacy Compatibility

```text
Authoritative Stock Ledger Outcome
  -> derive required Legacy Stock Projection
  -> supply compatibility representation
  -> compare legacy and authoritative positions
       -> Consistent
       -> Difference Detected
  -> retain Stock Ledger as business authority
```

**Outcome:** Existing legacy consumers may continue operating during migration without forcing the new Stock Ledger to repeat the legacy deletion and traceability limitations.

---

## 11. Open Business Questions

This section is temporary and should be removed or resolved before the artifact becomes fully canonical.

1. What exact ordering fact determines FIFO when two Stock Layers have the same effective receipt time?
2. Can stock be returned to an already Depleted Stock Layer, or must a new return layer always be established?
3. May one Receipt Source receive additional quantities after its original receipt was completed?
4. Which stock outcomes require Inventory Adjustment Authorizer approval?
5. Is Unit Valuation correction permitted independently from quantity correction?
6. How should legacy history be treated when movement ordering is ambiguous?
7. Can native processing continue when reconstruction is quantitatively balanced but some historical layer identities remain unknown?
8. Which bounded context owns expired, damaged, lost, or destroyed stock disposition?
9. Is negative stock categorically prohibited for all contexts, including emergency supply?
10. Which business authority determines FIFO eligibility when stock is reserved, quarantined, expired, damaged, or otherwise unavailable?
11. Does Stock Opname reconcile only quantity, or also Receipt Source and Unit Valuation?
12. How are stock reservations represented relative to Remaining Quantity and available quantity?
13. Does one source transaction line always map to exactly one Stock Consequence Request, or may one line intentionally produce multiple consequences?
14. Which facts must be published when a Legacy Stock Projection cannot represent authoritative Stock Ledger detail?
15. What business event marks the transition from legacy authority to Stock Ledger authority for a reconstructed Item and Receipt Source?
