# Stock Ledger Domain

**Artifact status:** Canonical business specification — Phase 0 architectural baseline **frozen** (2026-08-07)

**Bounded context:** Stock Ledger

**Version scope:** Incremental modernization of legacy inventory recording while preserving compatibility with existing stock transactions

**Bahasa Indonesia companion:** [stok-ledger-domain-id.md](./stok-ledger-domain-id.md)

**Phase 0 freeze:** Coexistence authority (`tb_stok` + `tb_buku`), reconstruction scope (Item + Receipt Source across locations), Freshness Gate, origin labels (`Native` / `Reconstructed` / `LegacySynchronized` ≠ authority), and Stage B coexistence strategy are LOCKED. Implementation ADRs and the Phase 0 Exit Review govern technical realization; do not redesign these domain decisions without an explicit change request. See [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md).

---

## 1. Business Overview

### 1.1 Purpose and value

Stock Ledger defines the target business model for inventory quantity movement, stock provenance, Stock Layers, remaining quantity, and unit valuation across Stock Locations.

The domain ensures that every accountable inventory quantity can be traced to its original Receipt Source throughout receipt, transfer, consumption, return, correction, and other inventory consequences.

Stock Ledger operates behind business transactions owned by other bounded contexts. Those contexts determine why inventory must move. Stock Ledger determines and records the accountable inventory consequence.

During the Coexistence Period, the Legacy Stock Record remains the authoritative persisted stock truth. Stock Ledger maintains a richer Stock Ledger Representation that must remain reconcilable with, and synchronized to, that legacy authority while both systems continue to process stock transactions.

The domain must ensure that:

* every stock quantity retains its Item, Receipt Source, Stock Location, Expiration Date when applicable, and Unit Valuation;
* movement between locations preserves the original Receipt Source;
* stock consumption follows FIFO within the Stock Location requested by the source transaction, unless that transaction explicitly identifies an Expiration Date;
* FIFO consumption remains traceable to the Stock Layers consumed;
* depleted Stock Layers remain part of the Stock Ledger Representation even when the Legacy Stock Record cannot retain them;
* the movement ledger and current stock position can be reconciled within a bounded scope;
* one source business transaction does not produce duplicate stock consequences;
* corrections and reversals preserve the original recorded facts;
* stock reservation is represented as accountable transfer to a Virtual Stock Location;
* legacy stock provenance may be reconstructed incrementally without requiring full historical migration; and
* reconstructed facts remain distinguishable from facts recorded natively by the new Stock Ledger;
* legacy-originated stock changes may continue after reconstruction and must be incorporated through Legacy Synchronization;
* negative Remaining Quantity is prohibited without exception; and
* Stock Ledger performs no additional approval beyond the authority already established by the source transaction.

### 1.2 Scope

This context covers:

1. recognition of accountable stock receipts;
2. establishment and maintenance of Stock Layers;
3. stock movement recording;
4. stock transfer between Stock Locations;
5. FIFO stock consumption and explicit Expiration Date selection;
6. stock reservation through Virtual Stock Locations;
7. stock returns and accountable adjustments;
8. remaining-quantity, Expiration Date, and unit-valuation tracking;
9. preservation of depleted Stock Layers;
10. reconciliation by Item and Receipt Source;
11. incremental Legacy Stock Reconstruction;
12. detection of duplicate or inconsistent stock consequences; and
13. coexistence with continued legacy stock processing and Legacy Synchronization; and
14. publication of accountable stock movement and stock position outcomes.

### 1.3 Business boundaries

Stock Ledger owns:

* Stock Movement;
* Stock Layer;
* Stock Position by Item, Receipt Source, and Stock Location;
* FIFO allocation and explicit Expiration Date selection for outbound quantities;
* provenance continuity;
* inventory quantity consequences;
* Expiration Date and unit-valuation continuity within a Stock Layer;
* reservation movement to and from Virtual Stock Locations;
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

Stock Ledger does not determine whether a sale, receipt, transfer, reservation, disposal, return, or adjustment should occur. It records the stock consequence only after receiving an accountable source business fact or authorized request. Any approval required for that activity belongs to the originating transaction and is complete before Stock Ledger processing begins.

During the Coexistence Period, Stock Ledger does not obtain exclusive ownership of an Item + Receipt Source merely because that scope was processed natively or successfully reconstructed. Legacy-originated stock transactions may continue to affect the same scope.

### 1.4 Information authority

| Business fact | Authoritative owner |
| --- | --- |
| Item identity | Product Catalog |
| Stock Location identity | Facility or Organizational authority |
| Commercial goods receipt | Purchasing or Goods Receipt |
| Source transaction identity | Originating bounded context |
| Receipt Source identity | Originating receipt authority |
| Requested transfer | Stock Transfer |
| Reservation intent | Originating transaction context |
| Disposal, damage, or loss decision | Originating transaction context |
| Sale or supply fulfillment | Originating fulfillment context |
| Physical count result | Stock Opname |
| Authorized stock adjustment | Responsible inventory authority |
| Persisted stock quantity and movement facts during the Coexistence Period | Legacy Stock Authority |
| Stock Layer provenance and retained depleted-layer representation | Stock Ledger |
| FIFO and expiry-constrained allocation performed by Stock Ledger | Stock Ledger |
| Stock Ledger reconciliation result | Stock Ledger |
| Financial accounting entry | Finance or Accounting |

Stock Ledger shall not infer that a business transaction is valid merely because an inventory movement is technically possible.

The authority distinction is explicit:

```text
Target business model
= Stock Ledger

Persisted stock source of truth during coexistence
= Legacy Stock Record
```

Successful reconstruction does not change this authority relationship.

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
           -> distributed across physical or Virtual Stock Locations
                -> reserved, consumed, or moved while retaining the same provenance
```

A Stock Layer may reach zero Remaining Quantity, but it remains an accountable part of the Stock Position history.

### 1.6 Supporting-domain character

Stock Ledger is a supporting bounded context. It may have no direct user-facing workflow for ordinary transactions.

Its business behavior is primarily triggered by facts and requests from other bounded contexts. Lack of direct user interaction does not reduce its responsibility for inventory movement, provenance, balance, and reconciliation.

### 1.7 Coexistence authority

The Coexistence Period is a transitional business condition in which legacy and new stock-processing capabilities operate concurrently.

During this period:

- the Legacy Stock Record remains the source of truth for persisted stock quantity and movement facts;
- Stock Ledger may process new transactions using its domain rules while preserving legacy-compatible stock consequences;
- legacy-originated transactions may continue after an Item + Receipt Source has been reconstructed;
- Legacy Stock Reconstruction establishes an initial Stock Ledger baseline, not an authority transfer;
- Legacy Synchronization incorporates subsequent legacy changes into the Stock Ledger Representation; and
- an unexplained difference between the Legacy Stock Record and Stock Ledger Representation makes the affected scope inconsistent until reconciled.

A future final cutover may change runtime authority, but such a cutover is outside the current domain decision.

---

## 2. Ubiquitous Language

| Term                         | Definition                                                                                                                                                                   |
| ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Stock Ledger                 | The bounded context that defines the target inventory movement, provenance, Stock Layer, allocation, and reconciliation model. During the Coexistence Period its representation remains synchronized to the Legacy Stock Authority.                                         |
| Item                         | A uniquely identified inventory-managed product.                                                                                                                             |
| Stock Location               | A business location at which inventory is held, such as a warehouse, pharmacy, ward, clinic, or emergency unit.                                                              |
| Virtual Stock Location       | A logical Stock Location used to separate inventory by business availability, such as reserved stock, without changing its physical facility or provenance.                    |
| Receipt Source               | The accountable identity of the event or document through which inventory originally entered the organization. In the current legacy model, this is represented by `KodeDO`. |
| Stock Provenance             | The traceable origin of inventory from its Receipt Source through all subsequent transfers, consumption, returns, and corrections.                                           |
| Stock Layer                  | An accountable quantity of one Item associated with one Receipt Source, one Stock Location, one unit valuation, and one layer-forming movement.                              |
| Layer-Forming Movement       | A Stock Movement that establishes a Stock Layer at a Stock Location.                                                                                                         |
| Initial Quantity             | The quantity held by a Stock Layer when that layer is established.                                                                                                           |
| Remaining Quantity           | The quantity currently available or accountable within a Stock Layer.                                                                                                        |
| Depleted Stock Layer         | A Stock Layer whose Remaining Quantity is zero. It remains part of the Stock Ledger Representation and is not discarded.                                                    |
| Unit Valuation               | The inventory value per unit retained by a Stock Layer.                                                                                                                      |
| Expiration Date              | The date after which a Stock Layer is no longer eligible for ordinary use. It is retained through transfer, reservation, return, and consumption.                            |
| Explicit Expiry Selection    | A source transaction instruction that limits eligible Stock Layers to a specified Expiration Date before FIFO ordering is applied.                                           |
| Stock Position               | The current Stock Ledger representation of Stock Layers for a defined Item, Receipt Source, and Stock Location scope.                                              |
| Stock Movement               | An immutable business fact that inventory quantity entered, left, moved between locations, returned, or was adjusted.                                                        |
| Stock Movement Line          | One quantity consequence within a Stock Movement, associated with an Item, Receipt Source, Stock Location, direction, quantity, and Unit Valuation.                          |
| Source Business Fact         | An authoritative fact from another bounded context that provides the business reason for a stock consequence.                                                                |
| Source Transaction Reference | The stable identity connecting a Stock Movement to the business transaction that caused it.                                                                                  |
| Stock Receipt                | A Stock Movement that recognizes inventory entering accountable stock control from an external source.                                                                          |
| Stock Transfer               | A coordinated outbound and inbound inventory movement between two Stock Locations while preserving Item, Receipt Source, and Unit Valuation.                                 |
| Stock Consumption            | An outbound Stock Movement caused by sale, dispensing, usage, damage, expiry, or another accountable final disposition.                                                      |
| FIFO                         | The policy that consumes eligible Stock Layers in their applicable order from oldest to newest.                                                                              |
| FIFO Allocation              | The accountable distribution of one outbound quantity across one or more eligible Stock Layers.                                                                              |
| Stock Reservation            | The logical separation of inventory by transferring it from an ordinary Stock Location to a designated Virtual Stock Location.                                               |
| Reservation Release          | The transfer of reserved inventory from its Virtual Stock Location back to the applicable ordinary Stock Location.                                                           |
| Stock Return                 | A movement that restores previously moved or consumed inventory to its original Receipt Source and Stock Layer when traceable. The return transaction remains the Source Transaction Reference. |
| Stock Adjustment             | An authorized inventory quantity correction resulting from an accountable discrepancy or business decision.                                                                  |
| Stock Correction             | A new accountable fact that corrects an earlier Stock Movement without erasing the original fact.                                                                            |
| Stock Reversal               | A Stock Movement that counteracts a previous Stock Movement while retaining both facts.                                                                                      |
| Stock Reconciliation         | The assessment that Stock Movements and Stock Position agree within a defined Reconciliation Scope.                                                                          |
| Reconciliation Scope         | The bounded set of inventory facts evaluated together. The primary scope is one Item and one Receipt Source across all Stock Locations.                                      |
| Reconciliation Difference    | A quantity or valuation difference found during Stock Reconciliation.                                                                                                        |
| Native Stock Fact            | A Stock Layer or Stock Movement recorded directly under the new Stock Ledger rules.                                                                                          |
| Legacy-Synchronized Stock Fact | A Stock Movement, or a Stock Layer established by that movement, recorded in Stock Ledger from a legacy-originated transaction that occurred after the applicable Stock Ledger baseline was established. |
| Legacy Stock Fact            | A stock fact originating from the legacy inventory model.                                                                                                                    |
| Legacy Stock Reconstruction  | The accountable reconstruction of missing Stock Layers from available legacy movement history.                                                                               |
| Reconstructed Stock Layer    | A Stock Layer established from Legacy Stock Reconstruction rather than from a native Stock Receipt.                                                                          |
| Reconstruction Scope         | The Item and Receipt Source whose complete legacy provenance is reconstructed across all Stock Locations.                                                                    |
| Reconstruction Trigger       | The first eligible stock processing activity that requires a previously unreconstructed Item and Receipt Source.                                                             |
| Reconstruction Status        | The accountable state indicating whether a Reconstruction Scope is not reconstructed, being reconstructed, reconstructed, or found inconsistent.                             |
| Provenance Continuity        | The rule that Receipt Source and Unit Valuation remain traceable across transfers and consumption.                                                                           |
| Inventory Conservation       | The rule that quantities entering a provenance scope equal quantities remaining plus accountable outbound and adjustment outcomes.                                           |
| Stock Consequence            | The inventory quantity effect produced from an authorized Source Business Fact.                                                                                              |
| Duplicate Stock Consequence  | More than one accountable Stock Movement created for the same source transaction responsibility.                                                                           |
| Coexistence Period           | The transitional period in which legacy and new stock-processing capabilities operate concurrently while the Legacy Stock Record remains the authoritative persisted stock truth. |
| Legacy Stock Authority       | The business authority of the Legacy Stock Record during the Coexistence Period. |
| Legacy Stock Record          | The legacy current-stock and movement-history representation that remains authoritative during coexistence. In the current legacy system this is represented by `tb_stok` and `tb_buku`. |
| Stock Ledger Representation  | The richer Stock Ledger view of stock movement, provenance, Stock Layers, depleted layers, and reconciliation facts. |
| Legacy Synchronization       | The accountable incorporation of legacy stock changes recorded after initial reconstruction into the Stock Ledger Representation. |
| Synchronization Position     | The known point through which applicable legacy stock facts have already been reflected in the Stock Ledger Representation. It expresses freshness, not technical storage mechanics. |

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

### 3.5 Expiry-Constrained FIFO Consumption

Allocate outbound inventory demand within the Stock Location requested by the source transaction. Apply Explicit Expiry Selection when supplied; otherwise apply FIFO, retaining the exact quantity consumed from each layer.

### 3.6 Stock Reservation Management

Reserve and release inventory through accountable transfers between ordinary and Virtual Stock Locations while preserving provenance.

### 3.7 Stock Transfer Coordination

Coordinate outbound and inbound movement between Stock Locations while preserving Receipt Source and Unit Valuation.

### 3.8 Stock Position Management

Maintain the Stock Ledger Representation of Remaining Quantity by Item, Receipt Source, Stock Location, and Stock Layer.

### 3.9 Stock Reconciliation

Validate that Stock Movements and Stock Position remain quantitatively consistent for one Item and Receipt Source across all Stock Locations.

### 3.10 Stock Correction and Reversal

Correct inventory consequences through new accountable facts without deleting or silently rewriting completed Stock Movements.

### 3.11 Legacy Stock Reconstruction

Incrementally reconstruct previously deleted or unavailable Stock Layers when an Item and Receipt Source first require processing under the new Stock Ledger.

### 3.12 Cross-Context Stock Consequence Coordination

Accept accountable source facts from other bounded contexts and publish accountable stock outcomes without taking ownership of the originating business transaction.

### 3.13 Legacy Coexistence and Synchronization

Support continued legacy stock processing while maintaining a richer Stock Ledger Representation synchronized to the Legacy Stock Authority during the Coexistence Period.

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

### 4.5 Inventory Controller

Reviews reconciliation outcomes and investigates differences. Approval of adjustment, disposal, damage, loss, destruction, correction, or reversal remains owned by the originating transaction context.

### 4.6 System Administrator

May support technical operation but does not own stock quantity, provenance, valuation, reconciliation, or correction decisions.


---

## 5. Domain Objects

### 5.1 Stock Movement

Represents an accountable inventory quantity consequence within the Stock Ledger model.

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
* Expiration Date when applicable;
* Effective Receipt Time;
* Stock Layer identity used as the final deterministic ordering key;
* layer ordering information required by FIFO;
* origin classification as native, reconstructed, or legacy-synchronized; and
* depletion status.

A Stock Layer remains retained in the Stock Ledger Representation when its Remaining Quantity reaches zero.

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
* requested Stock Location;
* explicitly requested Expiration Date when supplied;
* Stock Layers selected;
* quantity consumed from each layer;
* Unit Valuation of each consumed quantity; and
* unfulfilled quantity when insufficient eligible stock exists.

### 5.6 Stock Transfer

Represents the coordinated inventory consequence of moving quantity between Stock Locations.

It preserves the same:

* Item;
* Receipt Source;
* Unit Valuation;
* Expiration Date when applicable; and
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

The original movement remains visible as an accountable historical fact.

### 5.10 Stock Consequence Request

Represents the business instruction supplied by another bounded context requesting an inventory consequence.

It identifies:

* source business responsibility;
* Source Transaction Reference;
* requested consequence;
* Item;
* quantity;
* applicable Stock Location;
* Expiration Date when explicitly known;
* Receipt Source when already determined; and
* effective business time.

Acceptance of a Stock Consequence Request does not transfer ownership of the source business transaction to Stock Ledger.

### 5.11 Legacy Stock Synchronization

Represents the business relationship that keeps a previously reconstructed or natively recorded Stock Ledger scope current with later stock facts recorded under Legacy Stock Authority.

It identifies:

* the affected Item and Receipt Source;
* the Synchronization Position;
* legacy stock facts not yet reflected in Stock Ledger;
* the resulting Stock Ledger changes; and
* any inconsistency that prevents the representation from being considered current.

Legacy Stock Synchronization updates the Stock Ledger Representation without transferring runtime authority away from the Legacy Stock Record during the Coexistence Period.

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
* Expiration Date;
* Effective Receipt Time;
* deterministic Stock Layer ordering;
* FIFO ordering;
* native, reconstructed, or legacy-synchronized origin;
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

A Stock Reconciliation reads the applicable Stock Movements and Stock Position but does not own either.

Legacy Stock Reconstruction establishes previously missing Stock Layers and marks them as reconstructed. During the Coexistence Period, subsequent native Stock Movements may continue from that baseline only after applicable legacy changes have been synchronized. Legacy-originated Stock Movements may also continue and must be reflected through Legacy Synchronization.

Cross-aggregate coordination must preserve:

* one accountable consequence per source responsibility;
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
* **BR-STL-004** — One source transaction responsibility shall produce at most one active accountable Stock Consequence of the same type.
* **BR-STL-005** — Repeating the same source responsibility shall not duplicate inventory quantity.
* **BR-STL-006** — Stock Ledger shall reject or identify a requested consequence whose Item, quantity, location, or provenance conflicts with the authoritative source facts supplied by the responsible source context or, during coexistence, the Legacy Stock Record.

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
* **BR-STL-015** — Remaining Quantity shall not exceed Initial Quantity except through an accountable return or quantity correction processed from an already-authorized source transaction.
* **BR-STL-016** — Remaining Quantity shall not become negative under any transaction or care setting.
* **BR-STL-017** — A Stock Layer whose Remaining Quantity becomes zero shall remain part of the Stock Ledger Representation.
* **BR-STL-018** — A Depleted Stock Layer shall not be silently deleted or reused as a different layer.
* **BR-STL-019** — Unit Valuation shall represent value per inventory unit, not total layer value.
* **BR-STL-020** — Unit Valuation shall remain unchanged for quantity preserving the same provenance and shall not be corrected independently from the quantity and source transaction consequence.
* **BR-STL-021** — A new Receipt Source with a different Unit Valuation shall establish a separate Stock Layer.

### 7.4 Stock movement

* **BR-STL-022** — A completed Stock Movement shall be immutable.
* **BR-STL-023** — A Stock Movement shall record positive quantities and express direction separately as inbound or outbound.
* **BR-STL-024** — An outbound Stock Movement shall identify the Stock Layers from which quantity was taken.
* **BR-STL-025** — An inbound Stock Movement shall identify whether it establishes a new Stock Layer, restores an original Stock Layer and Receipt Source, or results from a transfer.
* **BR-STL-026** — Movement effective business time and recording time shall remain distinguishable when they differ.
* **BR-STL-027** — Every material Stock Movement shall retain responsible source and effective business time.
* **BR-STL-028** — A completed Stock Movement shall not be erased because its quantity has been fully consumed.

### 7.5 FIFO consumption

* **BR-STL-029** — Outbound consumption shall use FIFO within the Stock Location specified by the originating transaction unless that transaction supplies an Explicit Expiry Selection.
* **BR-STL-030** — Stock Layer eligibility shall be limited to the Item and Stock Location requested by the originating transaction; Stock Ledger shall not select stock from another Stock Location implicitly.
* **BR-STL-031** — When an Expiration Date is explicitly supplied, only Stock Layers with that Expiration Date shall be eligible before FIFO ordering is applied.
* **BR-STL-032** — Eligible Stock Layers shall be ordered first by Effective Receipt Time and then by Stock Layer identity when Effective Receipt Time is equal.
* **BR-STL-033** — A single outbound requirement may consume quantities from multiple Stock Layers.
* **BR-STL-034** — Every quantity consumed from a Stock Layer shall retain that layer's Receipt Source, Expiration Date, and Unit Valuation.
* **BR-STL-035** — When one outbound requirement consumes multiple Stock Layers, Stock Ledger shall record a separate accountable Stock Movement Line for each consumed layer.
* **BR-STL-036** — Stock Ledger shall not consume more than the eligible Remaining Quantity.
* **BR-STL-037** — Insufficient eligible stock shall produce an explicit unfulfilled quantity or rejection; it shall not produce negative stock.
* **BR-STL-038** — Depleted Stock Layers shall be excluded from subsequent allocation but retained for traceability and reconciliation.

### 7.6 Stock transfer

* **BR-STL-039** — Every completed Stock Transfer shall contain equal outbound and inbound quantities.
* **BR-STL-040** — A Stock Transfer shall identify one source Stock Location and one destination Stock Location.
* **BR-STL-041** — Source and destination Stock Locations shall not be the same for an ordinary Stock Transfer.
* **BR-STL-042** — A Stock Transfer may consume multiple source Stock Layers and establish corresponding destination Stock Layers.
* **BR-STL-043** — Each destination Stock Layer shall remain traceable to the source Stock Layer quantity from which it was formed.
* **BR-STL-044** — Transfer completion shall preserve total quantity for each Item and Receipt Source.
* **BR-STL-045** — A transfer discrepancy shall receive an accountable exception, adjustment, loss, return, or correction outcome.

### 7.7 Inventory conservation and reconciliation

* **BR-STL-046** — Reconciliation Scope shall primarily be defined by one Item and one Receipt Source across all Stock Locations.
* **BR-STL-047** — Reconciliation shall include depleted Stock Layers.
* **BR-STL-048** — Reconciliation shall include every accountable movement associated with the applicable Item and Receipt Source.
* **BR-STL-049** — Total quantity recognized for one Item and Receipt Source shall equal total Remaining Quantity plus all accountable final outbound quantities and net adjustment consequences.
* **BR-STL-050** — Movement between Stock Locations shall not change total quantity within the same Item and Receipt Source scope.
* **BR-STL-051** — The sum of Remaining Quantity across all Stock Layers in a Reconciliation Scope shall equal the current Stock Ledger Position for that scope; during coexistence that position must reconcile to the applicable Legacy Stock Record.
* **BR-STL-052** — A Reconciliation Difference shall be recorded explicitly and shall not be resolved by silently rewriting completed movements.
* **BR-STL-053** — A reconciliation result shall identify its scope, effective time, compared totals, and outcome.
* **BR-STL-054** — A successful reconciliation shall not prove that the originating commercial or operational transaction was otherwise correct.

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

* **BR-STL-055** — A completed Stock Movement shall be corrected through a new Stock Correction or Stock Reversal.
* **BR-STL-056** — A Stock Correction shall reference the movement or source fact being corrected.
* **BR-STL-057** — A Stock Reversal shall preserve the original Stock Movement and record the counteracting quantity consequence.
* **BR-STL-058** — Correction and reversal shall preserve Receipt Source and Unit Valuation unless the correction specifically addresses incorrect provenance or valuation.
* **BR-STL-059** — A correction that changes provenance shall retain traceability to both the previously recorded and corrected provenance.
* **BR-STL-060** — A correction shall not cause Remaining Quantity to become negative.
* **BR-STL-061** — Corrected facts and original facts shall remain separately visible for reconciliation.

### 7.9 Legacy stock reconstruction

* **BR-STL-062** — Legacy Stock Reconstruction shall be triggered only when an unreconstructed Item and Receipt Source require processing under the new Stock Ledger.
* **BR-STL-063** — Reconstruction Scope shall include the Item and Receipt Source across all Stock Locations.
* **BR-STL-064** — Reconstruction shall not be limited to the Stock Location that triggered it.
* **BR-STL-065** — Legacy Stock Reconstruction shall use available accountable legacy movement facts and shall not invent unavailable historical identities.
* **BR-STL-066** — A legacy Stock Layer identity that no longer exists shall not be recreated as though its original identifier were known.
* **BR-STL-067** — Reconstructed Stock Layers shall receive new accountable identities while retaining the available Item, Receipt Source, Stock Location, Unit Valuation, and movement provenance.
* **BR-STL-068** — Reconstructed Stock Layers with zero Remaining Quantity shall be retained.
* **BR-STL-069** — Reconstructed Stock Facts shall remain distinguishable from Native Stock Facts and Legacy-Synchronized Stock Facts.
* **BR-STL-070** — One Item and Receipt Source shall have at most one completed baseline reconstruction outcome for the same reconstruction basis.
* **BR-STL-071** — Repeated reconstruction processing shall not duplicate Stock Layers or quantities.
* **BR-STL-072** — Native Stock Movements shall not proceed against an unreconstructed Item and Receipt Source when doing so would create incomplete provenance or reconciliation.
* **BR-STL-073** — A reconstruction inconsistency shall be recorded and surfaced for accountable resolution rather than silently balanced.
* **BR-STL-074** — Reconstruction completion shall establish the baseline from which subsequent native Stock Ledger processing and Legacy Synchronization continue.
* **BR-STL-075** — Legacy reconstruction shall not require migration of all historical inventory before the new Stock Ledger may operate.

### 7.10 Legacy coexistence

* **BR-STL-076** — During the Coexistence Period, the Legacy Stock Record shall remain the authoritative persisted source of stock quantity and movement truth.
* **BR-STL-077** — Successful reconstruction or native Stock Ledger processing shall not by itself transfer runtime authority for an Item and Receipt Source away from the Legacy Stock Record.
* **BR-STL-078** — Legacy-originated stock transactions may continue after an Item and Receipt Source has been reconstructed or processed natively.
* **BR-STL-079** — Before Stock Ledger relies on its representation for a subsequent stock decision during coexistence, applicable legacy stock changes after the Synchronization Position shall be incorporated or the scope shall be treated as not current.
* **BR-STL-080** — A representational difference caused solely by the Legacy Stock Record being unable to retain Stock Ledger detail, such as a depleted layer, shall not by itself be treated as a quantity inconsistency.

### 7.11 Completion and traceability

* **BR-STL-081** — Every stock quantity shall remain traceable from its Receipt Source to its current Stock Layer or accountable final outbound outcome.
* **BR-STL-082** — Every outbound quantity shall identify the Stock Layer quantities consumed.
* **BR-STL-083** — Every transferred quantity shall identify both source and destination Stock Locations.
* **BR-STL-084** — Every reconstructed quantity shall identify its Reconstruction Scope and reconstruction outcome.
* **BR-STL-085** — Every correction shall preserve the original fact and the correcting fact.
* **BR-STL-086** — Stock Ledger shall retain sufficient provenance to explain current quantity and unit valuation without requiring organization-wide replay of all historical inventory movements.

### 7.12 Receipt completion and returns

* **BR-STL-087** — A completed Receipt Source shall not receive additional quantity as an ordinary receipt.
* **BR-STL-088** — A later quantity correction related to a completed Receipt Source shall be recorded as a correction or adjustment, not as an additional ordinary receipt.
* **BR-STL-089** — A returned quantity that can be traced to its original Stock Layer shall restore that Stock Layer and retain its original Receipt Source, Expiration Date, and Unit Valuation.
* **BR-STL-090** — The transaction that triggers a return shall be retained as the Source Transaction Reference and shall not replace the original Receipt Source.
* **BR-STL-091** — When the original Stock Layer cannot be identified from legacy facts, the return shall establish an accountable new Stock Layer without inventing an unknown historical identity.

### 7.13 Reservation and stock eligibility

* **BR-STL-092** — Stock Reservation shall be represented as a Stock Transfer from an ordinary Stock Location to a designated Virtual Stock Location.
* **BR-STL-093** — Reservation Release shall be represented as a Stock Transfer from the Virtual Stock Location back to the applicable ordinary Stock Location.
* **BR-STL-094** — Reservation and release shall preserve Item, Receipt Source, Expiration Date, Unit Valuation, and quantity.
* **BR-STL-095** — Stock held in a Virtual Stock Location shall not be selected for a transaction requesting another Stock Location.
* **BR-STL-096** — A source transaction shall determine the Stock Location from which inventory may be selected. Stock Ledger shall not move or consume stock from another location implicitly.
* **BR-STL-097** — Disposal, damage, loss, destruction, and other exceptional dispositions shall originate from already-authorized source transactions. Stock Ledger shall apply no additional approval.

### 7.14 Source request cardinality

* **BR-STL-098** — One source transaction line shall normally produce one Stock Consequence Request for one Item, requested quantity, and requested Stock Location.
* **BR-STL-099** — The source transaction line is not required to identify Receipt Source or Stock Layer unless it explicitly identifies an Expiration Date or another permitted provenance constraint.
* **BR-STL-100** — One Stock Consequence Request may produce multiple Stock Movement Lines because of FIFO allocation, explicit Expiration Date selection, transfer pairing, or multiple affected Stock Layers.
* **BR-STL-101** — Multiple Stock Movement Lines produced from one Stock Consequence Request shall remain traceable to the same source transaction line.

### 7.15 Reconstruction completeness and ambiguity

* **BR-STL-102** — Reconstruction may complete when Remaining Quantity can be determined for each Item and Receipt Source even if original legacy Stock Layer identifiers are no longer known.
* **BR-STL-103** — When only total Item quantity can be determined but its distribution by Receipt Source cannot be determined, reconstruction shall be `Inconsistent` and native processing shall not continue for the affected scope.
* **BR-STL-104** — When legacy movement order is ambiguous but does not change quantity, Receipt Source, Expiration Date, or Unit Valuation outcomes, reconstruction may use a deterministic fallback order and shall identify that ordering as reconstructed rather than proven historical fact.
* **BR-STL-105** — When ambiguous legacy ordering changes quantity, Receipt Source, Expiration Date, or Unit Valuation outcomes, reconstruction shall be `Inconsistent`.
* **BR-STL-106** — Successful Legacy Stock Reconstruction shall establish a balanced Stock Ledger baseline for the applicable Item and Receipt Source. It shall not transfer runtime authority away from the Legacy Stock Record during the Coexistence Period.
* **BR-STL-107** — `Legacy Stock Reconstruction Completed` is an internal domain outcome triggered during the first stock movement request for an unreconstructed Item and Receipt Source; no user-facing transition transaction is required, and the outcome does not imply authority transfer.

### 7.16 Stock Opname and coexistence synchronization

* **BR-STL-108** — Stock Opname supplies observed physical quantity only and does not determine Receipt Source or Unit Valuation.
* **BR-STL-109** — Any quantity difference identified from Stock Opname shall be resolved through an already-authorized source adjustment transaction before Stock Ledger records its consequence.
* **BR-STL-110** — A Legacy Stock Record may omit detail that its model cannot represent, but such omission shall not erase the richer Stock Ledger fact or be interpreted as a quantity difference when the legacy quantity consequence remains correct.
* **BR-STL-111** — During the Coexistence Period, reconciliation between Legacy Stock Record and Stock Ledger Representation shall treat the Legacy Stock Record as the persisted source of truth while preserving Stock Ledger-only provenance detail that the legacy representation cannot express.
* **BR-STL-112** — Legacy Stock Reconstruction establishes the initial Stock Ledger baseline; subsequent legacy stock changes shall be incorporated through Legacy Synchronization rather than requiring full reconstruction again when the prior baseline remains valid.
* **BR-STL-113** — Native Stock Facts, Reconstructed Stock Facts, and Legacy-Synchronized Stock Facts describe the origin of Stock Ledger facts and shall not be interpreted as authority states during the Coexistence Period.
* **BR-STL-114** — When a Legacy Stock Record and Stock Ledger Representation differ in quantity or other material facts beyond an explainable synchronization delay or representational limitation, the affected scope shall be `Inconsistent` until reconciled.
* **BR-STL-115** — A stock transaction originating from the new system may use Stock Ledger rules to determine its consequence, but during coexistence its resulting stock facts shall remain compatible with the authoritative Legacy Stock Record.

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
| Proposed  | An accountable source has requested a stock consequence, but no Stock Ledger movement has been recorded. |
| Recorded  | The Stock Movement is recorded in Stock Ledger and immutable within that ledger.                                                        |
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
| Depleted    | Remaining Quantity is zero. The layer remains retained in Stock Ledger.                               |
| Corrected   | A later accountable correction changed the quantity, provenance, or valuation consequence. |

A Depleted Stock Layer is not deleted. A traceable return may restore positive Remaining Quantity to the original Stock Layer while retaining its original Receipt Source, Expiration Date, and Unit Valuation.

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
| Uninitialized  | No Stock Ledger position has yet been established for the Item and Receipt Source. |
| Established    | The provenance position has been recognized.                                              |
| Active         | At least one Stock Layer has positive Remaining Quantity.                                 |
| Fully Depleted | Every Stock Layer has zero Remaining Quantity, but the Stock Position remains retained.   |
| Inconsistent   | A reconciliation or reconstruction difference requires accountable resolution.            |

A completed Receipt Source shall not receive additional quantity through an ordinary receipt. Any later quantity change related to that Receipt Source shall be represented through an accountable correction, adjustment, return, or other permitted stock consequence.

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
| Reconstruction Required | A new Stock Ledger activity requires a usable provenance baseline to be established.                         |
| Reconstructing          | Available legacy facts are being interpreted within the complete Reconstruction Scope.                   |
| Reconstructed           | A balanced reconstructed Stock Ledger baseline is available for the Item and Receipt Source. During coexistence, legacy authority remains unchanged and later legacy activity may require synchronization. |
| Inconsistent            | Available legacy facts cannot produce a complete balanced reconstruction without accountable resolution. |

### 8.6 Legacy Synchronization lifecycle

```text
Current
  -> Legacy Change Pending
       -> Synchronization Required
            -> Current
            -> Inconsistent
```

| State | Business meaning |
| --- | --- |
| Current | The Stock Ledger Representation reflects applicable legacy stock facts through its Synchronization Position. |
| Legacy Change Pending | New legacy stock facts exist beyond the known Synchronization Position. |
| Synchronization Required | Stock Ledger must incorporate those facts before relying on its representation for a subsequent stock decision. |
| Inconsistent | The legacy and Stock Ledger representations cannot be reconciled from the available facts. |

Reconstruction establishes the initial baseline. Legacy Synchronization keeps that baseline current while coexistence continues.

### 8.7 FIFO allocation lifecycle

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

### 8.8 Stock Reservation lifecycle

```text
Available at Ordinary Location
  -> Reserved at Virtual Location
       -> Released to Ordinary Location
       -> Consumed from Virtual Location by an explicitly permitted transaction
```

| State | Business meaning |
|---|---|
| Available at Ordinary Location | Quantity may be selected by transactions requesting the ordinary Stock Location. |
| Reserved at Virtual Location | Quantity is logically separated and unavailable to transactions requesting the ordinary Stock Location. |
| Released to Ordinary Location | Reserved quantity was transferred back and is eligible again at the ordinary Stock Location. |
| Consumed from Virtual Location | A source transaction explicitly requested and consumed quantity from the Virtual Stock Location. |

Reservation does not change Receipt Source, Expiration Date, Unit Valuation, or physical ownership.

---

## 9. Domain Events

| Domain Event                               | Business meaning                                                                          |
| ------------------------------------------ | ----------------------------------------------------------------------------------------- |
| Stock Consequence Requested                | An accountable source requested an inventory quantity effect.                             |
| Stock Receipt Recorded                     | Inventory entering the organization was recognized by Stock Ledger.                       |
| Stock Layer Established                    | A new Stock Layer was formed with accountable provenance.                                 |
| Stock Layer Depleted                       | A Stock Layer reached zero Remaining Quantity.                                            |
| Stock Position Established                 | A Stock Ledger position became available for an Item and Receipt Source.                       |
| Stock Movement Recorded                    | An inventory inbound or outbound consequence was recorded in Stock Ledger.                        |
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
| Stock Reconciliation Difference Identified | A difference was found between applicable movement and position facts.                 |
| Stock Reconciliation Difference Resolved   | An identified difference received an accountable resolution.                              |
| Legacy Stock Reconstruction Required       | A previously unreconstructed Item and Receipt Source were required for native processing. |
| Legacy Stock Reconstruction Started        | Reconstruction began across the complete Item and Receipt Source scope.                   |
| Legacy Stock Layer Reconstructed           | A Stock Layer was established from available legacy history.                              |
| Legacy Stock Reconstruction Completed      | A balanced reconstructed Stock Ledger baseline became available without changing coexistence authority.                                   |
| Legacy Stock Synchronization Required      | Applicable legacy stock facts exist beyond the Stock Ledger Synchronization Position.                    |
| Legacy Stock Synchronization Completed     | Applicable legacy stock facts were incorporated and the Stock Ledger Representation became current again. |
| Legacy Stock Reconstruction Failed         | Available legacy facts could not produce an accountable reconstruction.                   |
| Duplicate Stock Consequence Detected       | More than one consequence was requested or found for the same source responsibility.      |
| Legacy Stock Difference Identified         | A material difference was found between the Legacy Stock Record and Stock Ledger Representation beyond an explainable representational limitation or synchronization delay.            |
| Stock Reserved                             | Inventory quantity was transferred to a designated Virtual Stock Location.                 |
| Stock Reservation Released                 | Reserved quantity was transferred back to the applicable ordinary Stock Location.          |
| Stock Disposal Recorded                    | An already-authorized disposal or destruction transaction produced a final outbound consequence. |

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
  -> identify Item and requested Stock Location
  -> apply requested Expiration Date when supplied
  -> identify eligible Stock Layers only in that location and expiry group
  -> order layers by Effective Receipt Time, then Stock Layer identity
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
  -> identify original movement, Stock Layer, and Receipt Source
  -> validate permitted return quantity
  -> retain the return transaction as Source Transaction Reference
  -> record inbound Stock Movement
  -> restore the original Stock Layer when traceable
  -> otherwise establish an accountable new Stock Layer without inventing provenance
  -> update Stock Position
  -> publish Stock Returned
```

**Outcome:** Returned quantity is reintroduced without losing its original provenance or movement relationship.

### 10.5 Record Stock Adjustment

```text
Authorized Adjustment Transaction Received
  -> identify Item, Receipt Source, Stock Location, and affected quantity
  -> record adjustment Stock Movement
  -> update Stock Layer and Stock Position
  -> preserve reason and responsible authority
  -> publish Stock Adjustment Recorded
```

**Outcome:** A quantity difference from an already-authorized transaction receives an accountable consequence rather than a silent Stock Position rewrite.

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
Stock Processing Requested
  -> detect unreconstructed Item and Receipt Source
  -> establish Reconstruction Required
  -> collect available legacy movement facts
  -> reconstruct provenance across all Stock Locations
  -> establish active and depleted Reconstructed Stock Layers
  -> reconcile Remaining Quantity per Item and Receipt Source
       -> Reconstructed baseline available
       -> Inconsistent
  -> during coexistence, preserve Legacy Stock Authority
  -> synchronize later legacy activity before relying on the Stock Ledger Representation
```

**Outcome:** Legacy provenance becomes available incrementally for the Item and Receipt Source actually being processed, without requiring complete migration of decades of inventory history and without transferring runtime authority away from the Legacy Stock Record during coexistence.


### 10.9 Coordinate Cross-Context Stock Consequence

```text
Source Business Fact Published
  -> validate source responsibility and reference
  -> detect duplicate consequence
  -> determine applicable stock behavior
  -> record Stock Movement
  -> update Stock Position
  -> publish stock outcome to the source context
```

**Outcome:** Other bounded contexts receive an accountable inventory consequence while retaining ownership of their original business transaction.

### 10.10 Reserve and Release Stock

```text
Reservation Transaction Confirmed
  -> request transfer from ordinary Stock Location
  -> allocate eligible Stock Layers
  -> transfer quantity to designated Virtual Stock Location
  -> preserve Receipt Source, Expiration Date, and Unit Valuation
  -> publish Stock Reserved

Reservation Release Confirmed
  -> request transfer from Virtual Stock Location
  -> transfer quantity back to ordinary Stock Location
  -> preserve provenance
  -> publish Stock Reservation Released
```

**Outcome:** Reserved quantity is logically unavailable to ordinary-location transactions without introducing a separate reservation balance model.

### 10.11 Synchronize Continued Legacy Stock Activity

```text
Reconstructed, Native, or Legacy-Synchronized Stock Ledger Representation
  -> legacy stock transaction occurs
  -> applicable legacy facts move beyond Synchronization Position
  -> Legacy Synchronization Required
  -> incorporate legacy stock consequence
  -> reconcile Legacy Stock Record and Stock Ledger Representation
       -> Current
       -> Inconsistent
```

**Outcome:** Stock Ledger remains usable as a richer domain representation while legacy stock transactions continue during coexistence.

### 10.12 Maintain Legacy Coexistence

```text
During Coexistence

Legacy Stock Record
  = authoritative persisted stock truth

Stock Ledger Representation
  = richer provenance and Stock Layer representation

New-system stock transaction
  -> apply Stock Ledger business rules
  -> preserve legacy-compatible stock consequence
  -> keep Stock Ledger Representation synchronized

Legacy-system stock transaction
  -> update Legacy Stock Record
  -> require later Legacy Synchronization
```

**Outcome:** Legacy and new stock-processing capabilities may operate concurrently without per-Item or per-Receipt-Source authority cutover. The Legacy Stock Record remains the source of truth for persisted stock quantity and movement facts until a separate future cutover decision is made.

---
