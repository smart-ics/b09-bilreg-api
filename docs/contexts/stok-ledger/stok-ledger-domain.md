# Stock Ledger Domain

**Artifact status:** Canonical business specification

**Bounded context:** Stock Ledger

**Version scope:** Target inventory-consequence business model for hospital stock provenance, location balances, movements, FEFO/FIFO outbound issue, and accountable reversal; coexistence with Legacy Stock Record during parallel operation

**Bahasa Indonesia companion:** [stok-ledger-domain-id.md](./stok-ledger-domain-id.md)

**Related evidence (non-normative):** legacy stock generation reference `docs/stok-ledger/clbGenStokX1.cls`

## 1. Business Overview

### 1.1 Purpose and value

Stock Ledger owns the accountable inventory consequence of business transactions that change stock quantity or location. It does not own why goods are received, sold, transferred, consumed, destroyed, adjusted, or repacked. Originating business contexts decide and authorize those facts; Stock Ledger records the resulting stock effect with continuous Receipt Source provenance.

The business must ensure that:

- every accountable stock quantity remains traceable to its Receipt Source;
- Remaining Quantity never becomes negative;
- outbound issue within a Stock Location follows FEFO when Expiration Dates are present, FIFO by receipt order when Expiration Dates are absent, and may be overridden by an explicit Expiration Date on the Source Stock Consequence;
- a Stock Balance that reaches zero remains part of the accountable representation;
- completed Stock Movements are not erased; voids are recorded as reverse journals;
- movement history and current balances can be reconciled for one Item and one Receipt Source; and
- during the Coexistence Period, the Legacy Stock Record remains the persisted data authority while Stock Ledger maintains a richer Stock Ledger Representation that must stay reconcilable with it.

The domain must ensure that:

* every stock quantity retains its Item, Receipt Source, Stock Location, Expiration Date when applicable, and Unit Valuation;
* movement between locations preserves the original Receipt Source;
* stock consumption follows FIFO within the Stock Location requested by the source transaction, unless that transaction explicitly identifies an Expiration Date;
* FIFO consumption remains traceable to the Stock Layers consumed;
* depleted Stock Layers remain part of the authoritative stock position;
* the movement ledger and current stock position can be reconciled within a bounded scope;
* one source business transaction does not produce duplicate stock consequences;
* corrections and reversals preserve the original recorded facts;
* stock custody transfer to a Dispensing Temporary Unit is represented as accountable Stock Mutasi, not a separate reservation domain object;
* legacy stock provenance may be reconstructed incrementally without requiring full historical migration; and
* reconstructed facts remain distinguishable from facts recorded natively by the new Stock Ledger;
* negative Remaining Quantity is prohibited without exception; and
* Stock Ledger performs no additional approval beyond the authority already established by the source transaction.

### 1.2 Scope

This context covers:

1. recognition of inbound stock from Goods Receipt;
2. establishment and maintenance of Stock Batch and Location Stock Balance;
3. recording of Stock Movements;
4. Stock Transfer between Stock Locations with preserved Receipt Source;
5. FEFO/FIFO outbound issue for sales and other authorized consumption, including explicit Expiration Date selection;
6. Purchase Return and Sales Return stock consequences;
7. Internal Consumption and Destruction stock consequences;
8. Stock Adjustment increases and decreases;
9. Repack source consumption and result recognition;
10. Stock Reversal through reverse journal;
11. retention of depleted Location Stock Balances;
12. reconciliation by Item and Receipt Source (hospital-wide and per Stock Location); and
13. coexistence with the Legacy Stock Record during parallel operation.

**Out of scope for this version (deferred):**

- Reserved Order stock consequence unrelated to Pharmacy Dispensing Temporary Unit custody.

**In scope for Pharmacy outpatient dispensing (ADR-APT-002):**

- Stock Mutasi between Pharmacy Unit and Dispensing Temporary Unit on Pharmacy authorization;
- Remove Stock from Dispensing Temporary Unit on Medication Handover; and
- Stock Mutasi return from Dispensing Temporary Unit to Pharmacy Unit on No Show resolution.

Stock Ledger does not own `Prepared`, `Handed Over`, No Show, or fulfillment lifecycle states.

### 1.3 Business boundaries

Stock Ledger owns Stock Batch, Location Stock Balance, Stock Movement, Outbound Allocation outcomes (FEFO/FIFO and explicit Expiration Date selection), Unit Valuation continuity within a Receipt Source, reconciliation results for its representation, and Stock Reversal consequences.

It relies on other contexts without taking over their authority:

- Purchasing or Goods Receipt owns commercial receipt of goods;
- Apotek or other fulfillment contexts own sale, dispense, handover, and No Show business facts;
- Stock Transfer owns the operational intent to move stock between locations;
- Internal use / ward supply owns consumption authorization;
- Destruction or write-off authority owns disposal authorization;
- Stock Opname or inventory control owns physical count and adjustment authorization;
- Repack or production-like packing owns the transform intent;
- Product Catalog owns Item identity and unit definition; and
- Facility or organizational authority owns Stock Location identity.

Stock Ledger does not decide whether a receipt, sale, transfer, return, consumption, destruction, adjustment, repack, pharmacy handover, or No Show return should occur. It accepts an authorized Source Stock Consequence and records the inventory effect.

### 1.7 Pharmacy outpatient dispensing boundary (ADR-APT-002)

Stock Ledger remains a pure stock authority. It does not own Pharmacy workflow concepts.

| Owner | Responsibility |
|---|---|
| Stock Ledger | Stock Quantity, Mutasi, Remove Stock, Stock Movement History |
| Pharmacy (Apotek) | Sales Order, Dispensing, dispensing lifecycle, `Prepared`, `Handed Over`, No Show resolution |

**Dispensing Temporary Unit** is a pharmacy Stock Location that holds medication under active dispensing custody after Dispensing Started and before handover or No Show return.

There is no separate inventory reservation operation. Pharmacy Reserve is implemented only as Stock Mutasi from **Pharmacy Unit** to **Dispensing Temporary Unit**.

| Pharmacy event | Inventory action |
|---|---|
| Dispensing Started | Mutasi: Pharmacy Unit → Dispensing Temporary Unit |
| Dispensing Completed / `Prepared` | No inventory action |
| Medication Handed Over | Remove Stock from Dispensing Temporary Unit |
| No Show resolution | Mutasi: Dispensing Temporary Unit → Pharmacy Unit |

Stock Ledger never stores `Prepared`, `Handed Over`, or No Show status. Partial fulfillment semantics belong to Sales Order, not Dispensing.

### 1.4 Information authority during coexistence

| Fact | Authoritative owner |
|---|---|
| Item identity | Product Catalog |
| Stock Location identity | Facility / organizational authority |
| Source business transaction | Originating bounded context |
| Receipt Source identity | Originating goods-receipt authority |
| Persisted stock quantity and journal during Coexistence Period | Legacy Stock Record (`tb_stok` and `tb_buku`) |
| Target Stock Ledger Representation (including depleted balances) | Stock Ledger |
| Outbound allocation (FEFO/FIFO / explicit Expiration Date) performed by Stock Ledger | Stock Ledger |
| Reconciliation of Stock Ledger Representation | Stock Ledger |

```text
Target business model
= Stock Ledger

Persisted data authority during Coexistence Period
= Legacy Stock Record (tb_stok + tb_buku)
```

Successful reconstruction or native recording of a Stock Ledger Representation does not transfer persisted data authority away from the Legacy Stock Record while coexistence continues.

### 1.5 Central business model

```text
Authorized Source Stock Consequence
  -> Stock Movement(s)
       -> Stock Batch established or updated
            -> Location Stock Balance increased or decreased
                 -> Reconciliation available by Item + Receipt Source
```

Provenance relationship:

```text
Item + Receipt Source
  -> one Stock Batch
       -> Location Stock Balances across Stock Locations
            -> Stock Movements that conserve or finally consume batch quantity
```

A Stock Layer may reach zero Remaining Quantity, but it remains an accountable part of the Stock Position history.

### 1.6 Supporting-domain character

Stock Ledger is a supporting bounded context. It may have no direct user-facing workflow for ordinary transactions.

Its business behavior is primarily triggered by facts and requests from other bounded contexts. Lack of direct user interaction does not reduce its authority over inventory movement, provenance, balance, and reconciliation.

---

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Stock Ledger | The bounded context that records accountable inventory consequences with Receipt Source provenance across Stock Locations. |
| Item | A catalogued goods identity whose stock quantities are tracked. |
| Stock Location | A physical or logical place where stock is held (for example warehouse, pharmacy unit, or dispensing temporary unit). |
| Pharmacy Unit | The ordinary pharmacy Stock Location from which outpatient medication enters dispensing custody. |
| Dispensing Temporary Unit | The pharmacy Stock Location that holds medication under active dispensing custody before handover or No Show return. |
| Receipt Source | The durable identity of one goods entry into the hospital stock system, typically the goods-receipt document identity. |
| Stock Batch | The accountable stock originating from one Item and one Receipt Source across all Stock Locations. |
| Location Stock Balance | The remaining quantity of one Stock Batch at one Stock Location for one Expiration Date (including absent Expiration Date). |
| Depleted Balance | A Location Stock Balance whose Remaining Quantity is zero and that remains retained for accountability. |
| Remaining Quantity | The quantity still available within a Stock Batch or Location Stock Balance. |
| Initial Quantity | The quantity recognized when a Location Stock Balance is first established by an inbound movement at that location. |
| Expiration Date | The expiry date associated with a Location Stock Balance. It may be absent when the client hospital does not record expiry. |
| Unit Valuation | The stock value per inventory unit for quantities sharing the same Receipt Source. |
| Stock Movement | An immutable inbound or outbound quantity fact for one Location Stock Balance arising from one Source Stock Consequence. |
| Source Stock Consequence | The authorized inventory effect requested after a source business transaction is completed. |
| Source Transaction Reference | The identity of the originating business transaction responsible for a Stock Movement. |
| Movement Kind | The business classification of a Stock Movement (receipt, transfer out/in, sale issue, return, consumption, destruction, adjustment, repack, or reversal). |
| Outbound Allocation | Selection of Location Stock Balances at one Stock Location to satisfy an outbound quantity under Explicit Expiry Selection, FEFO, or FIFO. |
| Explicit Expiry Selection | An outbound rule in which the Source Stock Consequence names a specific Expiration Date; only balances with that Expiration Date are eligible. |
| FEFO Allocation | First-Expire-First-Out selection: among eligible balances that have an Expiration Date, earlier (nearest) Expiration Dates are consumed first; ties are broken by receipt order. |
| FIFO Allocation | First-In-First-Out selection by receipt order (Receipt Source / entry time), used when eligible balances have no Expiration Date. |
| Stock Transfer | Coordinated outbound and inbound Stock Movements that move quantity between Stock Locations without changing Receipt Source. |
| Goods Receipt Consequence | Inbound recognition of purchased or otherwise received goods into a Stock Location. |
| Purchase Return Consequence | Outbound return of previously received goods to a supplier or equivalent authority. |
| Sale Issue Consequence | Outbound issue of stock for a medication or goods sale. |
| Sales Return Consequence | Inbound restoration of previously issued sale quantity under authorized return. |
| Internal Consumption Consequence | Outbound issue of stock for internal use that is not a patient sale. |
| Destruction Consequence | Outbound removal of stock for destruction or write-off. |
| Stock Adjustment Consequence | Authorized increase or decrease of recorded quantity to align with inventory control decisions. |
| Repack Consequence | Coordinated consumption of source Item quantities and recognition of result Item quantities under one repack authorization. |
| Stock Reversal | An accountable reverse-journal consequence that counteracts a previously recorded Stock Movement without erasing it. |
| Reconciliation Scope | The bounded set of facts compared together, primarily one Item and one Receipt Source. |
| Stock Ledger Representation | Stock Ledger's accountable view of batches, location balances, and movements, including depleted balances. |
| Legacy Stock Record | The currently operating persisted stock journals and balances (`tb_buku` and `tb_stok`) that remain data authority during coexistence. |
| Coexistence Period | The period when Legacy Stock Record and Stock Ledger Representation operate in parallel. |
| Inventory Conservation | Within one Item and Receipt Source, recognized inbound quantity equals remaining quantity plus accountable final outbound and net adjustment outcomes; location transfers do not change hospital-wide batch quantity. |

---

## 3. Business Capabilities

### 3.1 Goods Receipt Recognition

Recognize inbound quantity at a Stock Location, establish or increase the Stock Batch and Location Stock Balance, and record Unit Valuation from the authorized receipt facts.

### 3.2 Location Balance Maintenance

Maintain Remaining Quantity per Stock Batch and Stock Location, including retention of Depleted Balances.

### 3.3 Stock Movement Recording

Record immutable inbound and outbound Stock Movements linked to a Source Transaction Reference and Movement Kind.

### 3.4 Stock Transfer

Move quantity between Stock Locations while preserving Item, Receipt Source, and Unit Valuation, with equal outbound and inbound quantities.

### 3.5 Outbound Allocation

Satisfy an outbound requirement at one requested Stock Location by consuming Location Stock Balances under Explicit Expiry Selection, FEFO, or FIFO, possibly across multiple Stock Batches and Expiration Dates.

### 3.6 Sale and Return Consequences

Apply Sale Issue and Sales Return consequences without owning commercial sale documents.

### 3.7 Purchase Return Consequences

Apply Purchase Return outbound consequences against the applicable Receipt Source quantities.

### 3.8 Consumption and Destruction Consequences

Apply Internal Consumption and Destruction outbound consequences at the authorized Stock Location.

### 3.9 Stock Adjustment Consequences

Apply authorized quantity increases or decreases with accountable Unit Valuation for increases.

### 3.10 Repack Consequences

Consume source quantities and recognize result quantities under one repack authorization while preserving accountability of both sides.

### 3.11 Stock Reversal

Counteract a prior Stock Movement through a reverse journal that preserves the original movement and restores conserved quantities when the reversal is valid.

### 3.12 Stock Reconciliation

Assess consistency of movements and balances within a Reconciliation Scope at hospital-wide and per-location levels.

### 3.13 Legacy Coexistence Alignment

Keep the Stock Ledger Representation reconcilable with the Legacy Stock Record while the Legacy Stock Record remains persisted data authority.

## 4. Actors & Roles

Stock Ledger has no direct end-user workflow. People interact with originating business activities; Stock Ledger receives the resulting Source Stock Consequence.

| Role | Business involvement |
|---|---|
| Goods Receipt Officer | Authorizes receipt facts that produce Goods Receipt Consequences. |
| Purchasing / Returns Officer | Authorizes purchase returns that produce Purchase Return Consequences. |
| Pharmacy Staff | Authorizes sale and sales-return facts that produce Sale Issue or Sales Return Consequences. |
| Stock Transfer Officer | Authorizes transfers between Stock Locations. |
| Clinical / Unit Supply Officer | Authorizes Internal Consumption. |
| Destruction Authorizer | Authorizes Destruction Consequences. |
| Inventory Controller | Authorizes Stock Adjustment and physical-count-driven corrections. |
| Repack Officer | Authorizes Repack Consequences. |
| Stock Ledger Steward | Reviews reconciliation differences and unresolved coexistence inconsistencies; does not invent source business reasons. |

Permissions to create source transactions belong to those originating roles and contexts. Stock Ledger may reject a consequence that violates inventory policy (for example insufficient eligible quantity) without approving the source business decision itself.

## 5. Domain Objects

### 5.1 Stock Batch

Represents all accountable quantity for one Item and one Receipt Source across the hospital.

It preserves Remaining Quantity hospital-wide, Unit Valuation, and the set of Location Stock Balances for that provenance.

### 5.2 Location Stock Balance

Represents Remaining Quantity of one Stock Batch at one Stock Location for one Expiration Date (including absent Expiration Date).

The same Item, Receipt Source, and Stock Location may therefore hold more than one Location Stock Balance when Expiration Dates differ. Each balance may be depleted to zero and must remain retained. Movement history explains how the balance was formed and consumed.

### 5.3 Stock Movement

Represents one immutable inbound or outbound quantity fact against one Location Stock Balance.

It preserves Movement Kind, Source Transaction Reference, quantity direction, Unit Valuation, and effective business time of the stock effect.

### 5.4 Source Stock Consequence

Represents the authorized inventory instruction supplied by an originating context after its business transaction is complete.

It identifies Item, quantity, Stock Location(s), Receipt Source when already known, Expiration Date when explicitly selected or supplied on inbound recognition, Movement Kind, Source Transaction Reference, and effective business time.

### 5.5 Outbound Allocation

Represents how one outbound requirement at one Stock Location is satisfied from one or more Location Stock Balances under Explicit Expiry Selection, FEFO, or FIFO.

### 5.6 Stock Transfer

Represents paired outbound and inbound movements that relocate quantity without changing Receipt Source or hospital-wide Stock Batch quantity.

### 5.7 Stock Reversal

Represents the reverse-journal relationship between an original Stock Movement and the counteracting movement.

### 5.8 Stock Reconciliation

Represents one assessment of conservation and balance consistency for a Reconciliation Scope.

### 5.9 Legacy Stock Record

Represents the operating persisted stock authority during coexistence. It is externally owned by the legacy stock system and is not redesigned by this domain document.

## 6. Aggregates

### 6.1 Stock Batch Aggregate

**Aggregate Root:** `Stock Batch`

Consistency boundary: one Item and one Receipt Source.

The aggregate owns:

- hospital-wide Remaining Quantity for the batch;
- Location Stock Balances for that batch across Stock Locations and Expiration Dates;
- Stock Movements that affect those balances for that batch; and
- depletion state of each Location Stock Balance.

It keeps mutually consistent:

- Inventory Conservation for the batch;
- Unit Valuation for quantities of the batch;
- non-negative Remaining Quantity; and
- retention of Depleted Balances.

A single Source Stock Consequence that requires Outbound Allocation across multiple Receipt Sources coordinates multiple Stock Batch aggregates. Each aggregate remains consistent for its own Receipt Source.

Technical write partitioning of a large batch aggregate is an architecture concern and does not change this business consistency boundary.

### 6.2 Stock Reconciliation Aggregate

**Aggregate Root:** `Stock Reconciliation`

Consistency boundary: one Reconciliation Scope evaluation.

It owns calculated totals, differences, outcome, and effective assessment time. It does not silently rewrite completed Stock Movements or balances.

Legacy Stock Reconstruction establishes previously missing Stock Layers and marks them as reconstructed. Subsequent native Stock Movements continue from the reconstructed Stock Position.

Cross-aggregate coordination must preserve:

* one authoritative consequence per source responsibility;
* Item and Receipt Source consistency;
* quantity conservation;
* Unit Valuation continuity; and
* correction traceability.

---

## 7. Business Rules

### Provenance and conservation

- **BR-STL-001** — Every Stock Movement shall originate from exactly one authorized Source Stock Consequence or Stock Reversal of a prior movement.
- **BR-STL-002** — Stock Ledger shall not create the business reason for receipt, sale, transfer, return, consumption, destruction, adjustment, or repack.
- **BR-STL-003** — Every Stock Movement shall retain a Source Transaction Reference.
- **BR-STL-004** — Repeating the same Source Stock Consequence shall not duplicate inventory quantity.
- **BR-STL-005** — Every accountable quantity shall belong to exactly one Receipt Source.
- **BR-STL-006** — Receipt Source shall remain unchanged through transfer, issue, return, and reversal unless a correction specifically addresses incorrect provenance.
- **BR-STL-007** — A Stock Transfer shall not create a new Receipt Source.
- **BR-STL-008** — Quantities from different Receipt Sources shall remain separately accountable even for the same Item at the same Stock Location.
- **BR-STL-009** — Within one Item and Receipt Source, location transfers shall not change hospital-wide Remaining Quantity.
- **BR-STL-010** — Remaining Quantity shall not become negative.

### Balances and valuation

- **BR-STL-011** — A Location Stock Balance whose Remaining Quantity becomes zero shall remain retained as a Depleted Balance.
- **BR-STL-012** — A Depleted Balance shall not be silently deleted or reused as a different balance identity.
- **BR-STL-013** — Unit Valuation shall represent value per inventory unit, not total line value.
- **BR-STL-014** — Unit Valuation for a Receipt Source shall not be changed independently of an authorized quantity consequence for that provenance.
- **BR-STL-015** — A new Receipt Source with a different Unit Valuation shall establish a separate Stock Batch.

### Movements and reversal

- **BR-STL-016** — A completed Stock Movement shall be immutable.
- **BR-STL-017** — Stock Movements shall record positive quantities and express direction as inbound or outbound.
- **BR-STL-018** — An outbound Stock Movement shall identify the Location Stock Balance and Receipt Source consumed.
- **BR-STL-019** — Void of a prior consequence shall be recorded as Stock Reversal reverse journals; the original movement shall not be erased.
- **BR-STL-020** — A Stock Reversal shall reference the movement or Source Transaction Reference being reversed and shall restore conserved quantities only when the reversal remains business-valid.
- **BR-STL-021** — A Stock Transfer shall record equal outbound and inbound quantities for the transferred provenance.

### Outbound allocation (FEFO / FIFO / explicit expiry)

- **BR-STL-022** — Outbound consumption shall occur only within the Item and Stock Location requested by the source transaction.
- **BR-STL-023** — When the Source Stock Consequence supplies an Explicit Expiry Selection, only Location Stock Balances with that Expiration Date shall be eligible.
- **BR-STL-024** — When there is no Explicit Expiry Selection and eligible balances have Expiration Dates, allocation shall use FEFO: earlier (nearest) Expiration Dates first; equal Expiration Dates shall then follow receipt order (entry time / Receipt Source order).
- **BR-STL-025** — When there is no Explicit Expiry Selection and eligible balances have no Expiration Date, allocation shall use FIFO by receipt order (entry time / Receipt Source order).
- **BR-STL-026** — One outbound requirement may consume multiple Location Stock Balances across Expiration Dates and Stock Batches.
- **BR-STL-027** — Insufficient eligible quantity shall produce rejection or an explicit unfulfilled quantity; it shall not create negative stock.
- **BR-STL-028** — A Location Stock Balance shall be distinct for each combination of Stock Batch, Stock Location, and Expiration Date (including absent Expiration Date).
- **BR-STL-029** — Transfer and inbound recognition shall preserve the Expiration Date of the moved or received quantity on the resulting Location Stock Balance.

### Reconciliation and coexistence

- **BR-STL-030** — Primary Reconciliation Scope shall be one Item and one Receipt Source across all Stock Locations.
- **BR-STL-031** — Reconciliation shall also be possible per Item, Receipt Source, and Stock Location.
- **BR-STL-032** — Reconciliation shall include Depleted Balances and all accountable movements for the scope.
- **BR-STL-033** — During the Coexistence Period, the Legacy Stock Record shall remain the persisted data authority for stock quantity.
- **BR-STL-034** — During coexistence, the Stock Ledger Representation shall remain reconcilable with the applicable Legacy Stock Record for scopes that have been aligned.
- **BR-STL-035** — A reconciliation difference shall be recorded explicitly and shall not be resolved by silently rewriting completed movements.

### Pharmacy dispensing

- **BR-STL-037** — Pharmacy Reserve shall be recorded only as Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit.
- **BR-STL-038** — Medication Handover shall be recorded only as Remove Stock from Dispensing Temporary Unit.
- **BR-STL-039** — Pharmacy No Show return shall be recorded only as Stock Mutasi from Dispensing Temporary Unit back to Pharmacy Unit.
- **BR-STL-040** — Stock Ledger shall not store `Prepared`, `Handed Over`, No Show, or fulfillment lifecycle states.

### Deferred capabilities

- **BR-STL-036** — Reserved Order stock consequences unrelated to Pharmacy Dispensing Temporary Unit custody are outside this version's defined capabilities.

## 8. State Machines & Lifecycles

### 8.1 Location Stock Balance lifecycle

```text
Established (Remaining Quantity > 0)
  -> Active (quantity increases or decreases through movements)
  -> Depleted (Remaining Quantity = 0, retained)
  -> Active (if later inbound or reversal restores quantity)
```

Depleted is a business state of the same balance, not deletion.

### 8.2 Stock Movement lifecycle

```text
Recorded (immutable)
  -> optionally Counteracted by Stock Reversal (original remains visible)
```

There is no editable draft movement inside Stock Ledger. Drafting belongs to the originating business transaction before the Source Stock Consequence is issued.

### 8.3 Stock Batch lifecycle

```text
Established on first inbound recognition for Item + Receipt Source
  -> Active while hospital-wide Remaining Quantity > 0
  -> Fully Depleted while hospital-wide Remaining Quantity = 0 (batch retained)
  -> Active again if accountable inbound or reversal restores quantity
```

### 8.4 Coexistence alignment lifecycle (per Item + Receipt Source)

```text
Not Aligned
  -> Aligned (Stock Ledger Representation established against Legacy Stock Record)
  -> Stale (legacy changed after last alignment)
  -> Aligned (after catch-up)
  -> Inconsistent (unresolved difference; native stock consequences blocked for the scope until resolved)
```

| State | Business meaning |
|---|---|
| Available at Ordinary Location | Quantity may be selected by transactions requesting the ordinary Stock Location. |
| Reserved at Dispensing Temporary Unit | Quantity is held at Dispensing Temporary Unit and unavailable to transactions requesting Pharmacy Unit. |
| Returned to Pharmacy Unit | Quantity was transferred back from Dispensing Temporary Unit to Pharmacy Unit. |
| Removed from Dispensing Temporary Unit | Quantity was removed from Dispensing Temporary Unit on authorized handover. |

Pharmacy custody transfer does not change Receipt Source, Expiration Date, Unit Valuation, or physical ownership of the hospital stock system.

---

## 9. Domain Events

| Event | Meaning |
|---|---|
| Stock Batch Established | A new Item + Receipt Source stock provenance was recognized. |
| Location Stock Balance Established | A batch first held quantity at a Stock Location. |
| Location Stock Balance Increased | Remaining Quantity at a location increased. |
| Location Stock Balance Decreased | Remaining Quantity at a location decreased. |
| Location Stock Balance Depleted | Remaining Quantity at a location reached zero and was retained. |
| Stock Movement Recorded | An immutable inbound or outbound movement was recorded. |
| Stock Transferred | Paired transfer movements completed for a provenance. |
| Stock Issued | An outbound requirement was satisfied through Outbound Allocation (FEFO, FIFO, or Explicit Expiry Selection). |
| Stock Reversed | A Stock Reversal reverse journal counteracted a prior movement. |
| Stock Reconciliation Completed | A reconciliation assessment finished with an outcome. |
| Stock Scope Aligned | A Reconciliation Scope became aligned with the Legacy Stock Record. |
| Stock Scope Marked Inconsistent | An unresolved coexistence or conservation difference was identified. |

## 10. Business Workflows

These workflows describe data and consequence flow, not operator UI procedures.

### 10.1 Inbound Goods Receipt Consequence

### 10.4 Record Stock Return

```text
Originating Goods Receipt completed
  -> Source Stock Consequence accepted
  -> Stock Movement inbound recorded
  -> Stock Batch established or increased
  -> Location Stock Balance established or increased
```

### 10.2 Stock Transfer Consequence

### 10.5 Record Stock Adjustment

```text
Originating transfer authorized
  -> Source Stock Consequence accepted
  -> Outbound Allocation or explicit provenance selection at source location (as applicable)
  -> Stock Movement outbound at source location
  -> Stock Movement inbound at destination location
  -> Location balances updated; hospital-wide batch quantity unchanged
```

### 10.3 Outbound Sale Issue Consequence

### 10.6 Correct or Reverse Stock Movement

```text
Originating sale authorized
  -> Source Stock Consequence accepted for Item + Stock Location + quantity
       (+ optional Explicit Expiry Selection)
  -> Outbound Allocation (Explicit Expiry Selection, else FEFO, else FIFO)
  -> one or more outbound Stock Movements (one per consumed Location Stock Balance)
  -> affected Stock Batches and balances decreased
```

### 10.4 Return, Consumption, Destruction, Adjustment, Repack

### 10.7 Reconcile Item and Receipt Source

```text
Originating transaction authorized
  -> Source Stock Consequence accepted
  -> applicable inbound and/or outbound Stock Movements recorded
  -> Stock Batch and Location Stock Balance updated under conservation and valuation rules
```

### 10.5 Stock Reversal (void)

```text
Originating void authorized for a prior Source Transaction Reference
  -> reverse-journal Source Stock Consequence accepted
  -> counteracting Stock Movement(s) recorded against the original movement(s)
  -> balances restored when reversal is valid
  -> original movement(s) remain visible
```

### 10.6 Coexistence native consequence (parallel period)

```text
Originating transaction will be recorded through Stock Ledger
  -> ensure affected Item + Receipt Source scope is Aligned and not Stale/Inconsistent against Legacy Stock Record
  -> apply Stock Ledger consequence rules
  -> persist effect so Legacy Stock Record and Stock Ledger Representation both reflect the outcome
```

### 10.7 Coexistence legacy-originated change

```text
Legacy Stock Record changes from a legacy-originated transaction
  -> Stock Ledger Representation catch-up for the affected scope
  -> alignment restored or Inconsistent marked
```

### 10.8 Reconciliation

```text
Choose Reconciliation Scope (Item + Receipt Source, optionally + Stock Location)
  -> compare movement conservation with Location Stock Balances (including depleted)
  -> during coexistence, compare with Legacy Stock Record
  -> emit reconciliation outcome
```

**Outcome:** Existing legacy consumers may continue operating during migration. Any detail omitted by the legacy representation does not reduce or replace Stock Ledger authority.

---
