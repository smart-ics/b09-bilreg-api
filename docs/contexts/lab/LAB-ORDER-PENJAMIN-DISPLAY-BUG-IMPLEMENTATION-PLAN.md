---
Title: Perbaikan Tampilan Penjamin / Cara Bayar pada Detail Order Lab
Code: LAB-ORDER-PENJAMIN-DISPLAY-BUG
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-07
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Memperbaiki tampilan **Penjamin / Cara Bayar** pada panel detail order laboratorium (`LabOrderDetailPanel.vue`) agar menampilkan data penjamin riil dari registrasi pasien rumah sakit (Rawat Jalan, Rawat Inap, IGD) alih-alih nilai hardcoded `"BPJS Kesehatan"`.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- ISSUE: [LAB-ORDER-PENJAMIN-DISPLAY-BUG-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PENJAMIN-DISPLAY-BUG-ISSUE.md)
- BUG-INVESTIGATION: [LAB-ORDER-PENJAMIN-DISPLAY-BUG-INVESTIGATION.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PENJAMIN-DISPLAY-BUG-INVESTIGATION.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED
No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved investigation decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

1. **Backend (`b09-bilreg-api`)**:
   - Menambahkan properti `string PenjaminName = ""` pada `LabOrderGetResponse`.
   - Melakukan resolusi `penjaminName` pada `LabOrderGetHandler` dari data `reg.TipeJaminan.TipeJaminanName` jika `order.Patient.RegId` ada, atau `"Umum / Pasien Luar"` untuk `OrderSource == ExternalPatient`.
   - Menambahkan unit test di `LabOrderGetHandlerTest.cs`.

2. **Frontend (`c012_myhospital_web`)**:
   - Memperluas `labOrderDetailSchema` di `lab-order.schema.ts` dengan properti `penjaminName: z.string().optional()`.
   - Mengubah ekspresi tampilan di `LabOrderDetailPanel.vue` dari hardcoded ternary menjadi `order.penjaminName || (order.orderSource === 2 || order.orderSource === 4 ? 'Umum / Tunai' : '—')`.
   - Memperbarui / menambahkan spec unit test di frontend (`lab-order.schema.spec.ts`).

---

# 3. Dependencies

- `P2-S02` Depends On: `P1-S01` (Backend contract enrichment)

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend DTO & Handler | IMPLEMENTED | GO | 1/1 |
| P2 - Frontend Schema & UI | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Backend DTO & Handler

Implementation Status: IMPLEMENTED
Review Status: GO

### Slices

#### P1-S01: Penambahan Properti PenjaminName pada LabOrderGetResponse & Resolusi IRegRepo

- **Repository**: `b09-bilreg-api`
- **Component**: `Bilreg.Application.LabContext.LabOrderFeature`
- **Target Files**:
  - `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs`
  - `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderGetHandlerTest.cs`
- **Depends On**: none
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Description**:
  1. Tambahkan properti opsional `string PenjaminName = ""` pada record `LabOrderGetResponse`.
  2. Di dalam `LabOrderGetHandler.Handle`, ambil nama penjamin dari registrasi pasien:
     - Jika `order.OrderSource == LabOrderSourceEnum.ExternalPatient`, set `PenjaminName = "Umum / Pasien Luar"`.
     - Jika `_regRepo != null` dan `!string.IsNullOrWhiteSpace(order.Patient.RegId)`, muat `_regRepo.LoadEntity(RegModel.Key(order.Patient.RegId))`. Jika ditemukan, gunakan `reg.TipeJaminan.TipeJaminanName`.
  3. Perbarui unit test pada `LabOrderGetHandlerTest.cs` untuk memverifikasi `PenjaminName` terisi dengan benar baik untuk pasien RS maupun pasien luar.
- **Verification Evidence**:
  - `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-build --filter "FullyQualifiedName~LabOrderGetHandlerTest"`: 11 tests passed (0 failed).

---

## P2 - Frontend Schema & UI

Implementation Status: IMPLEMENTED
Review Status: GO

### Slices

#### P2-S02: Pembaruan labOrderDetailSchema & Binding Penjamin di LabOrderDetailPanel

- **Repository**: `c012_myhospital_web`
- **Component**: `Laboratory`
- **Target Files**:
  - `c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts`
  - `c012_myhospital_web/src/modules/Laboratory/components/workspace/LabOrderDetailPanel.vue`
  - `c012_myhospital_web/src/modules/Laboratory/__tests__/lab-order.schema.spec.ts`
- **Depends On**: P1-S01
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Description**:
  1. Tambahkan `penjaminName: z.string().optional()` pada `labOrderDetailSchema`.
  2. Perbarui `LabOrderDetailPanel.vue` pada blok Identitas Pasien:
     Ganti hardcoded string literal `'BPJS Kesehatan'` menjadi `order.penjaminName || (order.orderSource === 2 || order.orderSource === 4 ? 'Umum / Tunai' : '—')`.
  3. Verifikasi parsing schema pada `lab-order.schema.spec.ts` dengan penambahan test case `parses detail order with penjaminName when provided`.
  4. Jalankan vitest dan linting.
- **Verification Evidence**:
  - `pnpm test src/modules/Laboratory/__tests__/lab-order.schema.spec.ts`: 36 tests passed (0 failed).
  - `oxlint`: 0 warnings, 0 errors on modified files.
  - `eslint`: 0 warnings, 0 errors on modified files.
