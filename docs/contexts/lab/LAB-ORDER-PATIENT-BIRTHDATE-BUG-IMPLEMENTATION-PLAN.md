---
Title: Perbaikan Ketidaksesuaian Tanggal Lahir Pasien pada Detail dan Transaksi Order Lab
Code: LAB-ORDER-PATIENT-BIRTHDATE-BUG
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-19
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Memperbaiki aliran data dan rekonsiliasi data tanggal lahir serta jenis kelamin pasien pada transaksi pembuatan dan rincian pesanan laboratorium internal agar isian **Tanggal Lahir** dan **Usia** pada panel **Identitas Pasien** (`LabOrderDetailPanel.vue`) konsisten dan akurat sesuai data master sosial pasien (`PAS-DATSOS` / `tc_mr.fd_tgl_lahir`).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- ISSUE: [LAB-ORDER-PATIENT-BIRTHDATE-BUG-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PATIENT-BIRTHDATE-BUG-ISSUE.md)
- BUG-INVESTIGATION: [LAB-ORDER-PATIENT-BIRTHDATE-BUG-INVESTIGATION.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PATIENT-BIRTHDATE-BUG-INVESTIGATION.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED
No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved investigation decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

1. **Backend (`b09-bilreg-api`)**:
   - **AdmisiContext**: Memperluas respons DTO `RegRegAktifListResponse` dengan field `string TglLahir = ""` dan `string Gender = ""` pada `RegRegAktifListQuery.cs`, serta memetakan nilainya dari `x.Pasien.TglLahir.ToString("yyyy-MM-dd")` dan `x.Pasien.Gender` di `RegRegAktifHandler`.
   - **LabContext**: Memperkuat `LabOrderCreateFromEmrHandler` pada saat menyusun `PatientSnapshotType` melalui `LabOrderCreateHelper.BuildSnapshot` agar merekonsiliasi tanggal lahir dan jenis kelamin snapshot dengan entitas kunjungan aktif yang sah (`reg.Pasien.TglLahir` dan `reg.Pasien.Gender`).
   - Menambahkan unit test di `Bilreg.Test/AdmisiContext/RegFeature/RegRegAktifListQueryTest.cs` dan memutakhirkan unit test di `Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateFromEmrHandlerTest.cs`.

2. **Frontend (`c012_myhospital_web`)**:
   - **Lookup Schema**: Memperluas skema `labMasterRegItemSchema` di `useLabMasterLookup.ts` agar menerima atribut `tglLahir`, `birthDate`, dan `gender`.
   - **Form Canvas**: Memperbarui kanvas pembuatan order `LabOrderCreateCanvas.vue` untuk memetakan tanggal lahir dari `selectedPatient.tglLahir || selectedPatient.birthDate` tanpa beralih secara keliru ke `regDate`, serta menyelaraskan tampilan informasi tanggal lahir di kartu ringkasan pasien.
   - Memperbarui / menambahkan spec unit test di frontend (`useLabMasterLookup.spec.ts` dan `LabOrderCreateCanvas.spec.ts`).

---

# 3. Dependencies

- `P1-S01`: None (mandiri pada backend AdmisiContext).
- `P1-S02`: None (mandiri pada backend LabContext).
- `P2-S03`: Depends On: `P1-S01` (frontend lookup mengonsumsi DTO respons `api/reg/listAktif` yang diperkaya).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend DTO Enrichment & Domain Snapshot Guard | IMPLEMENTED | GO | 2/2 |
| P2 - Frontend Schema & Form Canvas Alignment | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Backend DTO Enrichment & Domain Snapshot Guard

Implementation Status: IMPLEMENTED
Review Status: GO

### Slices

#### P1-S01: Penambahan Field TglLahir & Gender pada RegRegAktifListResponse di AdmisiContext

- **Repository**: `b09-bilreg-api`
- **Component**: `Bilreg.Application.AdmisiContext.RegFeature`
- **Target Files**:
  - `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegRegAktifListQuery.cs`
  - `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegRegAktifListQueryTest.cs`
- **Depends On**: None
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Memaparkan data tanggal lahir (`TglLahir`) dan jenis kelamin (`Gender`) dari `RegAktifModel` ke consumer endpoint `GET /api/reg/listAktif`.
- **Completion Criteria**:
  1. Record `RegRegAktifListResponse` memiliki field `string TglLahir = ""` dan `string Gender = ""`.
  2. `RegRegAktifHandler` memetakan `x.Pasien.TglLahir.ToString("yyyy-MM-dd")` dan `x.Pasien.Gender` dari `RegAktifModel` ke objek DTO respons.
  3. Dibuat unit test pada `RegRegAktifListQueryTest.cs` yang memverifikasi bahwa kueri `RegRegAktifListQuery` mengembalikan `TglLahir` dan `Gender` yang sesuai.
- **Verification Commands**:
  - `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RegRegAktifListQueryTest"`
- **Changed Files**:
  - `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegRegAktifListQuery.cs`
  - `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegRegAktifListQueryTest.cs`
- **Implementation Notes**:
  - Menambahkan parameter opsional `string TglLahir = ""` dan `string Gender = ""` pada record `RegRegAktifListResponse` untuk menjaga kompatibilitas ke belakang bagi consumer lama.
  - Memetakan `x.Pasien.TglLahir.ToString("yyyy-MM-dd")` dan `x.Pasien.Gender` pada `RegRegAktifHandler.Handle`.
  - Menambahkan unit test komprehensif pada `RegRegAktifListQueryTest.cs` (pemetaan seluruh properti, penanganan null repository, penanganan empty collection, serta verifikasi nilai default untuk backward compatibility).
  - Verifikasi: `dotnet test` berhasil (4 passed, 0 failed).

---

#### P1-S02: Rekonsiliasi Tanggal Lahir dan Gender Pasien Snapshot pada LabOrderCreateFromEmrHandler

- **Repository**: `b09-bilreg-api`
- **Component**: `Bilreg.Application.LabContext.LabOrderFeature`
- **Target Files**:
  - `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs`
  - `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateFromEmrHandlerTest.cs`
- **Depends On**: None
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Menjamin integritas data snapshot pasien (`PatientSnapshotType`) yang dipersistensikan pada `BILRG_LabOrder` dengan merekonsiliasi tanggal lahir dan gender resmi dari entitas registrasi aktif (`reg.Pasien`).
- **Completion Criteria**:
  1. Pada `LabOrderCreateFromEmrHandler`, evaluasi nilai `birthDateYmd`:
     Jika `reg?.Pasien?.TglLahir` bernilai valid (bukan default/sentinel `3000-01-01`), gunakan `reg.Pasien.TglLahir.ToString("yyyy-MM-dd")`. Jika tidak, fallback ke `request.BirthDateYmd`.
  2. Evaluasi nilai `gender`:
     Jika `!string.IsNullOrWhiteSpace(reg?.Pasien?.Gender)` dan bukan `"-"`, gunakan `reg.Pasien.Gender`. Jika tidak, fallback ke `request.Gender`.
  3. Evaluasi nilai `patientName`:
     Jika `!string.IsNullOrWhiteSpace(reg?.Pasien?.PasienName)` dan bukan `"-"`, gunakan `reg.Pasien.PasienName`. Jika tidak, fallback ke `request.PatientName`.
  4. Snapshot order (`PatientSnapshotType`) memuat tanggal lahir dan usia saat order (`AgeAtOrder`) yang benar-benar berasal dari tanggal lahir master pasien.
  5. Unit test pada `LabOrderCreateFromEmrHandlerTest.cs` diperbarui/ditambahkan untuk memverifikasi bahwa snapshot order menggunakan data lahir dari entitas registrasi.
- **Verification Commands**:
  - `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~LabOrderCreateFromEmrHandlerTest"`
- **Changed Files**:
  - `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs`
  - `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateFromEmrHandlerTest.cs`
- **Implementation Notes**:
  - Merevisi pembentukan `PatientSnapshotType` pada `LabOrderCreateFromEmrHandler` untuk merekonsiliasi data `patientId`, `patientName`, `birthDateYmd`, dan `gender` terhadap `reg.Pasien`.
  - Jika `reg.Pasien.TglLahir` memiliki tahun bukan 3000 (bukan sentinel default), diformat ke `yyyy-MM-dd`; jika tidak, fallback ke `request.BirthDateYmd`.
  - Jika `reg.Pasien.Gender` atau `reg.Pasien.PasienName` tidak kosong dan bukan `"-"`, digunakan sebagai data snapshot; jika tidak, fallback ke nilai dari command `request`.
  - Memperbarui `LabOrderCreateFromEmrHandlerTest.cs` dengan menambahkan parameter opsional `PasienReff? pasien` pada `CreateTestReg`, memvalidasi snapshot data fallback pada tes eksisting, serta menambahkan unit test baru `Handle_RegHasOfficialPatientData_ReconcilesSnapshotWithRegPasienData` yang memverifikasi rekonsiliasi data master registrasi dan kalkulasi `AgeAtOrder`.
  - Verifikasi: `dotnet test` berhasil (17 passed, 0 failed).

---

## P2 - Frontend Schema & Form Canvas Alignment

Implementation Status: IMPLEMENTED
Review Status: GO

### Slices

#### P2-S03: Pembaruan labMasterRegItemSchema & Form Binding pada LabOrderCreateCanvas

- **Repository**: `c012_myhospital_web`
- **Component**: `Laboratory`
- **Target Files**:
  - `c012_myhospital_web/src/modules/Laboratory/composables/useLabMasterLookup.ts`
  - `c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue`
  - `c012_myhospital_web/src/modules/Laboratory/__tests__/useLabMasterLookup.spec.ts`
  - `c012_myhospital_web/src/modules/Laboratory/__tests__/LabOrderCreateCanvas.spec.ts`
- **Depends On**: P1-S01
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengonsumsi field tanggal lahir dan gender pada lookup registrasi aktif frontend dan mengeliminasi fallback keliru ke `regDate` pada saat pembuatan payload order laboratorium internal.
- **Completion Criteria**:
  1. `labMasterRegItemSchema` di `useLabMasterLookup.ts` ditambahkan field:
     - `tglLahir: z.string().optional().default('')`
     - `birthDate: z.string().optional().default('')`
     - `gender: z.string().optional().default('')`
  2. Di `LabOrderCreateCanvas.vue`:
     - Pada pembentukan payload `cmd`, ekstrak `birthDate` dan `birthDateYmd` dari `selectedPatient.value.tglLahir || selectedPatient.value.birthDate || '1990-01-01'` (menghilangkan fallback `selectedPatient.value.regDate`).
     - Ekstrak `gender` dari `selectedPatient.value.gender || 'L'`.
     - Pada kartu ringkasan pasien (baris info demografi), ganti label `Tgl Registrasi: ` menjadi `Tanggal Lahir: ` dengan menampilkan tanggal lahir yang diformat lokal (atau fallback `—` bila kosong).
  3. Unit test `useLabMasterLookup.spec.ts` memverifikasi schema parsing untuk field `tglLahir` dan `gender`.
  4. Unit test `LabOrderCreateCanvas.spec.ts` memverifikasi payload `birthDateYmd` terbentuk dari `tglLahir` pasien yang dipilih dan tidak menggunakan tanggal registrasi.
- **Verification Commands** (dijalankan di direktori `c012_myhospital_web`):
  - `npx prettier --write src/modules/Laboratory/composables/useLabMasterLookup.ts src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue src/modules/Laboratory/__tests__/useLabMasterLookup.spec.ts src/modules/Laboratory/__tests__/LabOrderCreateCanvas.spec.ts`
  - `pnpm tc:app`
  - `pnpm test src/modules/Laboratory/__tests__/useLabMasterLookup.spec.ts`
  - `pnpm test src/modules/Laboratory/__tests__/LabOrderCreateCanvas.spec.ts`
  - `pnpm lint`
- **Changed Files**:
  - `c012_myhospital_web/src/modules/Laboratory/composables/useLabMasterLookup.ts`
  - `c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue`
  - `c012_myhospital_web/src/modules/Laboratory/__tests__/useLabMasterLookup.spec.ts`
  - `c012_myhospital_web/src/modules/Laboratory/__tests__/LabOrderCreateCanvas.spec.ts`
- **Implementation Notes**:
  - Menambahkan field `tglLahir`, `birthDate`, dan `gender` dengan tipe `z.string().optional().default('')` pada `labMasterRegItemSchema` di `useLabMasterLookup.ts`.
  - Memperbarui `LabOrderCreateCanvas.vue` dalam pembentukan payload `cmd`: mengekstrak `patientBirthDate` dari `selectedPatient.value.tglLahir || selectedPatient.value.birthDate || '1990-01-01'` dan `patientGender` dari `selectedPatient.value.gender || 'L'`, secara tuntas mengeliminasi fallback keliru ke `selectedPatient.value.regDate`.
  - Memperbarui kartu ringkasan demografi pasien pada `LabOrderCreateCanvas.vue`: mengganti label `Tgl Registrasi: ` menjadi `Tanggal Lahir: `, dan memformat tanggal lahir secara lokal dengan `formatDateLocale(rawDate, 'short')` serta fallback `—` bila kosong, dengan mempertahankan atribut `data-testid="patient-birth-date"`.
  - Memperbarui unit test `useLabMasterLookup.spec.ts` untuk memvalidasi parsing `tglLahir`, `birthDate`, dan `gender` serta nilai default string kosong.
  - Memperkaya dan menambahkan unit test pada `LabOrderCreateCanvas.spec.ts` untuk memverifikasi tampilan kartu ringkasan demografi pasien (label "Tanggal Lahir: " dan tanggal lokal), pembentukan payload `birthDateYmd` dari `tglLahir` (bukan dari `regDate`), serta fallback ke `'1990-01-01'` saat field tanggal lahir kosong.
  - Verifikasi:
    - Prettier: 4 files formatted.
    - Type check (`pnpm tc:app`): 0 error.
    - Targeted tests:
      - `useLabMasterLookup.spec.ts`: 38 passed (38).
      - `LabOrderCreateCanvas.spec.ts`: 34 passed (34).
    - Lint (oxlint + eslint): 0 error, 0 warning pada seluruh file terdampak.

---

# 6. Change Log

- **2026-11-19**: Perumusan awal IMPLEMENTATION-PLAN (v1.0) untuk penyelesaian defek ketidaksesuaian tanggal lahir pasien pada order laboratorium (`LAB-ORDER-PATIENT-BIRTHDATE-BUG`).
