# Tata Rekening Backend Implementation Plan

**Version:** 1.0  
**Status:** Planning  
**Source of Truth:** Tata Rekening Artifacts (`01-context.md`, `02-domain.md`, `03-design.md`, `04-sop.md`, `SOP-TR-01` … `SOP-TR-10`)  
**Gap Analysis:** `tata-rekening-domain-gap-analysis-report.md`

---

# Objective

Tujuan implementasi adalah menyelaraskan codebase dengan seluruh artifact Tata Rekening sehingga backend siap digunakan sebagai fondasi implementasi Frontend.

Implementasi dilakukan secara **bertahap (incremental)** agar setiap fase menghasilkan codebase yang stabil, dapat diuji, dan menjadi fondasi bagi fase berikutnya.

Prinsip utama:

- Artifact adalah source of truth.
- Setiap fase harus menghasilkan code yang dapat di-build.
- Hindari implementasi sementara yang akan dibuang pada fase berikutnya.
- Refactoring diperbolehkan apabila diperlukan untuk menjaga kesesuaian terhadap artifact.

---

# Phase Overview

| Phase | Objective | Result |
|--------|-----------|--------|
| Phase 1 | Domain Alignment | Domain Model sesuai artifact |
| Phase 2 | Domain Feature Completion | Seluruh Business Capability tersedia |
| Phase 3 | Application Layer | Seluruh SOP memiliki Use Case |
| Phase 4 | Infrastructure & Integration | Persistence dan Integrasi lengkap |
| Phase 5 | API Layer | Backend siap digunakan Frontend |
| Phase 6 | Frontend Implementation | Implementasi UI berdasarkan SOP |

---

# Phase 1 — Domain Alignment

## Objective

Menyelaraskan Domain Model terhadap artifact tanpa memperhatikan API maupun database.

Fokus utama fase ini adalah memperbaiki struktur Domain sehingga seluruh lifecycle dan business capability sesuai dengan SOP terbaru.

---

## Scope

### Aggregate

Refactor `TataRekeningModel` agar sesuai dengan Domain Model.

Pastikan lifecycle menjadi:

```text
OPEN
    │
    ▼
CLOSED
    │
    ▼
FINALIZED
    │
    ▼
LUNAS
```

---

### Allocation

Pisahkan Allocation dari Finalization.

Ubah:

```text
Finalize()
    ├── Allocate()
    └── Lock()
```

menjadi

```text
Allocate()

Finalize()
```

sesuai SOP.

---

### Verification

Tambahkan konsep Financial Verification pada Domain.

Minimal berupa state atau mekanisme validasi sehingga Finalization tidak dapat dilakukan sebelum Verification selesai.

---

### Settlement

Tambahkan operation:

```text
InitiateSettlement()
```

Settlement bukan Payment.

Payment tetap menjadi tanggung jawab Cashier.

---

### Pay()

Pertahankan implementasi `Pay()` hanya sebagai compatibility bridge apabila diperlukan.

Namun tandai sebagai legacy.

Jangan lagi digunakan sebagai workflow utama.

---

### Lifecycle

Pastikan seluruh operation memiliki guard yang benar.

Contoh:

- Close hanya boleh dari OPEN.
- Finalize hanya boleh dari CLOSED.
- Cancel Finalization hanya boleh dari FINALIZED.
- Reopen hanya boleh dari CLOSED.
- Settlement Initiation hanya boleh dari FINALIZED.

---

### Domain Events

Buat placeholder Domain Event.

Belum perlu dispatcher.

Contoh:

- BillClosed
- AllocationCompleted
- FinalizationCompleted
- FinalizationCancelled
- SettlementInitiated

---

## Deliverables

Setelah Phase-1 selesai:

- Domain Model sesuai artifact.
- Lifecycle sesuai SOP.
- Allocation dipisahkan dari Finalization.
- Settlement tidak lagi identik dengan Payment.
- Unit Test seluruh Domain tetap hijau.

---

## Out of Scope

Jangan mengimplementasikan:

- Merge Request
- Merge Billing
- Repository
- Database
- API
- Authorization
- Accounting
- Controller
- UI

---

# Phase 2 — Domain Feature Completion

## Objective

Melengkapi seluruh business capability yang belum tersedia pada Domain.

## Scope

Implementasi:

- MergeRequest
- MergeRequest Status
- MergeBilling Domain Service
- Financial Verification
- Financial Adjustment
- Projection Regeneration

Deliverable:

Seluruh business process telah tersedia pada Domain.

---

# Phase 3 — Application Layer

## Objective

Mengimplementasikan seluruh SOP sebagai Application Use Case.

## Scope

Implementasi:

- Open Tata Rekening Query
- Close Bill Command
- Merge Billing Command
- Financial Verification Command
- Financial Adjustment Command
- Allocation Command
- Finalize Command
- Cancel Finalization Command
- Reopen Command
- Settlement Initiation Command

Tambahkan:

- Unit of Work
- Transaction Boundary
- Projection Regeneration Orchestration

Deliverable:

Seluruh SOP dapat dijalankan melalui Application Layer.

---

# Phase 4 — Infrastructure & Integration

## Objective

Melengkapi persistence dan integrasi antar bounded context.

## Scope

Implementasi:

- MergeRequest Table
- Repository
- Audit Trail
- Projection Persistence
- Accounting Integration
- Transfer Receivable
- Optimistic Concurrency

Hilangkan:

- Dual writer `ta_registrasi3`

Deliverable:

Persistence dan integrasi sesuai artifact.

---

# Phase 5 — API Layer

## Objective

Menyediakan backend yang siap digunakan Frontend.

## Scope

Implementasi:

- REST API
- DTO
- Validation
- Authorization
- Error Handling

Endpoint mengikuti seluruh SOP.

Deliverable:

Backend siap digunakan oleh Frontend.

---

# Phase 6 — Frontend

## Objective

Mengimplementasikan seluruh workflow Tata Rekening.

## Implementasi dilakukan sesuai urutan SOP:

1. Open Tata Rekening
2. Close Bill
3. Merge Billing
4. Financial Verification
5. Financial Adjustment
6. Financial Responsibility Allocation
7. Finalize Financial Responsibility
8. Cancel Finalization
9. Reopen Billing
10. Settlement Initiation

---

# Cross-Cutting Tasks

Task berikut dapat dikerjakan bertahap sepanjang implementasi.

- Authorization
- Audit Trail
- Domain Events Dispatcher
- Logging
- Integration Test
- Workflow Test
- Performance Improvement
- Documentation Update

---

# Completion Criteria

Backend dianggap siap untuk implementasi Frontend apabila:

- Seluruh artifact telah terimplementasi.
- Seluruh SOP memiliki Application Use Case.
- Domain Model sesuai dengan `02-domain.md`.
- Workflow sesuai `04-sop.md`.
- REST API tersedia.
- Unit Test dan Integration Test lulus.
- Tidak terdapat legacy workflow yang menjadi jalur utama Financial Control.

---

# Agent Working Rules

Seluruh implementasi agent harus mengikuti aturan berikut.

1. Artifact adalah otoritas tertinggi apabila terjadi konflik dengan codebase.
2. Jangan mengubah artifact.
3. Implementasi dilakukan hanya pada phase yang sedang dikerjakan.
4. Jangan mengimplementasikan phase berikutnya.
5. Hindari perubahan yang tidak berhubungan dengan objective phase.
6. Pertahankan backward compatibility selama memungkinkan.
7. Semua perubahan harus disertai unit test apabila menyentuh Domain.
8. Refactoring diperbolehkan apabila meningkatkan kesesuaian terhadap artifact tanpa mengubah perilaku bisnis yang telah ditetapkan.
9. Setelah phase selesai, hasil implementasi harus dapat di-build dan seluruh test harus lulus sebelum melanjutkan ke phase berikutnya.
