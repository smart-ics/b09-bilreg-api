---
Title: Implementation Plan - Perbaikan Prefix Ruangan pada Cetakan Struk Registrasi Rawat Jalan dan Query Detail Registrasi (GET /api/reg/{id})
Code: ANTRIAN-POLI-PREFIX-BUG-001
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-13
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Memperbaiki cacat tidak tampilnya kode prefix ruangan poliklinik pada bukti cetak struk registrasi rawat jalan dan respons endpoint detail registrasi `GET /api/reg/{id}` (`ISSUE-BILREG-ANTRIAN-POLI-PREFIX-BUG-001`). Rencana ini menyelaraskan proyeksi `QueuePrefixSnapshot` pada lapisan akses data antrian (`AntrianDal.ListData`), menambahkan properti aditif `QueueLabel` dan `PrefixAntrian` pada `RegGetResponse` (serta `RegJalanByBookingResponse`), dan memperbarui pemetaan data cetak `preparePrintItems()` pada antarmuka pendaftaran petugas (`LegacyRegistrationWorkspace.vue`).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- ISSUE: [ANTRIAN-POLI-PREFIX-BUG-001-ISSUE.md](file:///home/hlysx/projects/ics/billingweb/b21-myhosweb-system/Issues/ANTRIAN-POLI-PREFIX-BUG-001-ISSUE.md) (`ISSUE-BILREG-ANTRIAN-POLI-PREFIX-BUG-001`)
- BUG-INVESTIGATION: [ANTRIAN-POLI-PREFIX-BUG-001-BUG-INVESTIGATION.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-BUG-001-BUG-INVESTIGATION.md)
- ARCHITECTURE (Reference): [ANTRIAN-POLI-PREFIX-ARCHITECTURE.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED

No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

### Included:
1. **Backend (`b09-bilreg-api` / worktree `b09-bilreg-api-worktree-antrian-poli-prefix`)**:
   - Proyeksi kolom `ISNULL(bb.QueuePrefixSnapshot, '') AS QueuePrefixSnapshot` pada query `AntrianDal.ListData(DateTime date)`.
   - Penambahan properti opsional/default `QueuePrefixSnapshot` pada `AntrianViewDto` dan `AntrianView`, beserta kalkulasi label ringkas poli (`QueueLabel`).
   - Penambahan properti aditif `QueueLabel` dan `PrefixAntrian` pada `RegGetResponse` di `RegGetQuery.cs` serta pemetaan nilai dari `physicianQue` pada `RegJalanGethandler`.
   - Penyelarasan `RegJalanByBookingResponse` di `RegJalanByBookingCmd.cs` agar turut mengembalikan `QueueLabel` dan `PrefixAntrian` secara konsisten.
   - Pengujian unit terarah pada `Bilreg.Test` untuk memvalidasi `RegGetQuery` mengembalikan `QueueLabel` dan `PrefixAntrian` dengan benar.
2. **Frontend (`c012_myhospital_web` / worktree `c012_myhospital_web-worktree-antrian-poli-prefix`)**:
   - Memastikan skema `registerPasienSchema`, `registerPasienOutputSchema`, dan adapter `registerPasienAdapter` di `src/modules/Admisi/types/registrasi.ts` meneruskan `queueLabel` dan `prefixAntrian`.
   - Pembaruan fungsi `preparePrintItems()` di `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue` agar memetakan `noAntrian: reg.queueLabel || (reg.prefixAntrian ? `${reg.prefixAntrian}${reg.noAntrian}` : reg.noAntrian)` ke data template cetak `antrian_registrasi.html`.
   - Pengujian unit terarah pada `registrasi.spec.ts` untuk memverifikasi adaptasi `queueLabel` dan `prefixAntrian` pada `registerPasienAdapter`.

### Excluded:
1. Perubahan struktur DDL database (kolom `BILRG_Antrian.QueuePrefixSnapshot VARCHAR(5)` sudah tersedia).
2. Perubahan markup HTML pada `public/template/antrian_registrasi.html` (sudah menggunakan placeholder `{{ noAntrian }}`).
3. Perubahan pada repositori `c013-kiosk-queue-display-web` (tidak terdampak oleh cacat ini).

---

# 3. Dependencies

### External Dependencies:
- Struktur kolom `BILRG_Antrian.QueuePrefixSnapshot` yang telah diimplementasikan pada rencana utama `ANTRIAN-POLI-PREFIX`.

### Slice Dependencies:
- `Depends On` mendeklarasikan prasyarat implementasi antar irisan.
- Dependensi hanya merujuk pada ID irisan (Slice ID).
- Pemenuhan dependensi mensyaratkan irisan yang dirujuk berstatus implementasi `IMPLEMENTED`.
- Pemenuhan dependensi tidak mensyaratkan status review `GO`.
- Dependensi merepresentasikan prasyarat teknis implementasi yang nyata.

---

# 4. Progress Summary

Plan status values are:
- NOT-STARTED
- IN-PROGRESS
- BLOCKED
- COMPLETED

Execution Approval values are:
- PENDING
- APPROVED

Execution Approval is owned by the Architect. It is PENDING during Planning and set to APPROVED when the plan is released for execution. Execution must not begin while Execution Approval is PENDING.

COMPLETED is a plan-level status only. Set it only when every slice has implementation status IMPLEMENTED and review status GO.

Testing and test-package creation must not begin until the plan is COMPLETED. An individual slice with review status GO is not a testing entry condition.

Slice implementation status values are:
- NOT-STARTED
- IN-PROGRESS
- IMPLEMENTED
- BLOCKED

Slice review status values are:
- NOT-REVIEWED
- GO
- NO-GO

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend Query & Contract Alignment (`b09-bilreg-api`) | IMPLEMENTED | GO | 1/1 |
| P2 - Frontend Print Receipt Alignment (`c012_myhospital_web`) | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Backend Query & Contract Alignment (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01

Title: Proyeksi QueuePrefixSnapshot pada AntrianDal.ListData dan Kontrak Aditif RegGetResponse

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperluas query `AntrianDal.ListData(DateTime date)` beserta `AntrianViewDto` dan `AntrianView` agar memuat `QueuePrefixSnapshot` dan label antrian ringkas poli, serta memperbarui `RegGetResponse` (`RegGetQuery.cs`) dan `RegJalanByBookingResponse` (`RegJalanByBookingCmd.cs`) dengan properti aditif `QueueLabel` dan `PrefixAntrian`.

Depends On: None

Repository: b09-bilreg-api

Completion Criteria:
- Query SQL pada `AntrianDal.ListData(DateTime date)` di [AntrianDal.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianDal.cs) menyertakan `ISNULL(bb.QueuePrefixSnapshot, '') AS QueuePrefixSnapshot`.
- Record `AntrianViewDto` di [AntrianViewDto.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianViewDto.cs) menerima `string QueuePrefixSnapshot = ""` dan meneruskannya ke `AntrianView` melalui `ToView()`.
- Record `AntrianView` di [AntrianModel.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs) memiliki parameter opsional `string QueuePrefixSnapshot = ""` dan menyediakan properti/method pemformat label ringkas poli (`QueueLabel`: jika `NoUrut <= 0` maka `""`, jika `QueuePrefixSnapshot` kosong maka `NoUrut.ToString()`, selain itu `$"{QueuePrefixSnapshot}{NoUrut}"`).
- Record `RegGetResponse` di [RegGetQuery.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegGetQuery.cs) memiliki parameter aditif `string QueueLabel = "", string PrefixAntrian = ""` dengan tetap menjaga `int NoAntrian`.
- `RegJalanGethandler.Handle` mengisi `QueueLabel` dan `PrefixAntrian` pada `RegGetResponse` dari objek `physicianQue`.
- Record `RegJalanByBookingResponse` di [RegJalanByBookingCmd.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs) memiliki parameter aditif `string QueueLabel = "", string PrefixAntrian = ""` dan diisi menggunakan `antrian.FormatClinicQueueLabel(booking.NoAntrian)` serta `antrian.QueuePrefixSnapshot`.
- Unit test untuk `RegJalanGethandler` (`RegGetQuery`) ditambahkan/diperbarui di `Bilreg.Test` untuk memverifikasi pengembalian `NoAntrian: 7`, `QueueLabel: "C7"`, dan `PrefixAntrian: "C"` saat sesi dokter memiliki prefix `"C"`, serta perilaku fallback saat prefix kosong.
- Kompilasi dan pengujian unit terarah di `b09-bilreg-api-worktree-antrian-poli-prefix` lulus tanpa error (`dotnet test ... -p:UseSharedCompilation=false /nodeReuse:false`).

Notes:
- Bekerja di direktori worktree `/home/hlysx/projects/ics/billingweb/b09-bilreg-api-worktree-antrian-poli-prefix`.
- Gunakan parameter default pada konstruktor record `AntrianViewDto`, `AntrianView`, `RegGetResponse`, dan `RegJalanByBookingResponse` agar kode pemanggil atau test yang sudah ada tidak rusak (*zero breaking change*).
- Jalankan `dotnet build-server shutdown` setelah siklus pengujian selesai.

Implementation Notes:
- Menambahkan proyeksi kolom `ISNULL(bb.QueuePrefixSnapshot, '') AS QueuePrefixSnapshot` pada query SQL `AntrianDal.ListData(DateTime date)`.
- Menambahkan parameter default `string QueuePrefixSnapshot = ""` pada `AntrianViewDto` dan meneruskannya ke konstruktor `AntrianView` pada metode `ToView()`.
- Menambahkan parameter default `string QueuePrefixSnapshot = ""` dan properti terhitung `QueueLabel` pada record `AntrianView` dengan aturan: jika `NoUrut <= 0` maka `""`, jika `QueuePrefixSnapshot` kosong/null maka `NoUrut.ToString()`, selain itu `$"{QueuePrefixSnapshot}{NoUrut}"`.
- Memperluas record `RegGetResponse` (`RegGetQuery.cs`) dengan properti aditif opsional `string QueueLabel = "", string PrefixAntrian = ""` dan memetakan nilainya dari `physicianQue` pada `RegJalanGethandler.Handle`.
- Memperluas record `RegJalanByBookingResponse` (`RegJalanByBookingCmd.cs`) dengan properti aditif opsional `string QueueLabel = "", string PrefixAntrian = ""` dan memetakan nilainya dari `antrian.FormatClinicQueueLabel(booking.NoAntrian)` dan `antrian.QueuePrefixSnapshot` pada `RegJalanByBookingHandler`.
- Menambahkan file test `RegGetQueryTest.cs` di `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/` yang memverifikasi pengembalian `NoAntrian: 7, QueueLabel: "C7", PrefixAntrian: "C"` saat prefix `"C"`, fallback prefix kosong `NoAntrian: 7, QueueLabel: "7", PrefixAntrian: ""`, dan fallback saat antrian tidak ditemukan `NoAntrian: -1, QueueLabel: "", PrefixAntrian: ""`.
- Menambahkan pengujian `AntrianView.QueueLabel` dan pemetaan `AntrianViewDto.ToView` pada `AntrianModelTest.cs`, serta assertions `QueueLabel` dan `PrefixAntrian` pada `RegJalanByBookingHandlerTest.cs`.
- Menjalankan pengujian unit terarah:
  - `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RegGet" -p:UseSharedCompilation=false /nodeReuse:false` -> 3 passed, 0 failed.
  - `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AntrianModelTest|FullyQualifiedName~RegJalanByBookingHandlerTest|FullyQualifiedName~RegGet" -p:UseSharedCompilation=false /nodeReuse:false` -> 58 passed, 0 failed.
- Memanggil `dotnet build-server shutdown` setelah pengujian selesai.

Changed Files:
- `src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianViewDto.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianDal.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegGetQuery.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs`
- `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegGetQueryTest.cs`
- `src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/AntrianModelTest.cs`
- `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegJalanByBookingHandlerTest.cs`

---

## P2 - Frontend Print Receipt Alignment (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S02

Title: Pemetaan Label Antrian Berprefix pada Fungsi Cetak Bukti Registrasi (preparePrintItems) dan Unit Test Adapter

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui fungsi `preparePrintItems()` pada `LegacyRegistrationWorkspace.vue` agar memetakan label antrian berprefix (`queueLabel`) ke data cetak `antrian_registrasi.html`, serta menambahkan unit test pada `registrasi.spec.ts` untuk memverifikasi pemetaan `queueLabel` dan `prefixAntrian` pada `registerPasienAdapter`.

Depends On: P1-S01

Repository: c012_myhospital_web

Completion Criteria:
- Fungsi `preparePrintItems()` di [LegacyRegistrationWorkspace.vue](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web-worktree-antrian-poli-prefix/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue) memetakan properti `noAntrian` pada `baseData` dengan prioritas: `reg.queueLabel || (reg.prefixAntrian ? `${reg.prefixAntrian}${reg.noAntrian}` : reg.noAntrian)`.
- Skema `registerPasienSchema`, `registerPasienOutputSchema`, dan `registerPasienAdapter` di [registrasi.ts](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web-worktree-antrian-poli-prefix/src/modules/Admisi/types/registrasi.ts) terverifikasi meneruskan `queueLabel` dan `prefixAntrian`.
- File pengujian [registrasi.spec.ts](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web-worktree-antrian-poli-prefix/src/modules/Admisi/types/__tests__/registrasi.spec.ts) memiliki test case yang memvalidasi `registerPasienAdapter` memetakan `queueLabel` (contoh: `"C7"`) dan `prefixAntrian` (contoh: `"C"`) dari respons `GET /api/reg/{id}`, serta tetap kompatibel saat properti tersebut tidak dikirim.
- Verifikasi berurutan di direktori `/home/hlysx/projects/ics/billingweb/c012_myhospital_web-worktree-antrian-poli-prefix` lulus:
  1. `npx prettier --write` pada file yang dimodifikasi
  2. `pnpm tc:app`
  3. `pnpm test src/modules/Admisi/types/__tests__/registrasi.spec.ts`
  4. `pnpm lint`

Notes:
- Bekerja strictly di dalam direktori worktree `/home/hlysx/projects/ics/billingweb/c012_myhospital_web-worktree-antrian-poli-prefix`.
- Jangan menjalankan `pnpm test` penuh tanpa argumen path spec.

Implementation Notes:
- Memperbarui fungsi `preparePrintItems()` pada `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue`: memetakan `noAntrian: reg.queueLabel || (reg.prefixAntrian ? `${reg.prefixAntrian}${reg.noAntrian}` : reg.noAntrian)`.
- Memverifikasi `src/modules/Admisi/types/registrasi.ts` telah memuat `queueLabel` dan `prefixAntrian` pada `registerPasienSchema`, `registerPasienOutputSchema`, dan transformasi `registerPasienAdapter`.
- Menambahkan test case pada `src/modules/Admisi/types/__tests__/registrasi.spec.ts` di bawah `describe('registerPasienAdapter')` untuk memvalidasi pemetaan `queueLabel: 'C7'` dan `prefixAntrian: 'C'`, serta gracefully handling fallback sebagai undefined saat data tidak ada.
- Verifikasi berhasil:
  1. `npx prettier --write src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue src/modules/Admisi/types/registrasi.ts src/modules/Admisi/types/__tests__/registrasi.spec.ts` -> Lulus (unchanged / formatted, exit code 0).
  2. `pnpm tc:app` (`vue-tsc --noEmit --project tsconfig.app.json`) -> Lulus tanpa error tipe (exit code 0).
  3. `vitest run src/modules/Admisi/types/__tests__/registrasi.spec.ts` -> Lulus 42 passed (100% lulus, exit code 0).
  4. Linter: `npx oxlint` dan `npx eslint` pada file yang dimodifikasi lulus (0 warnings, 0 errors).

Changed Files:
- `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue`
- `src/modules/Admisi/types/__tests__/registrasi.spec.ts`

---

# 6. Change Log

- **2026-11-13**: Pembuatan `ANTRIAN-POLI-PREFIX-BUG-001-IMPLEMENTATION-PLAN.md` (Version 1.0) oleh `ica-architect` berdasarkan keputusan investigasi pada `ANTRIAN-POLI-PREFIX-BUG-001-BUG-INVESTIGATION.md`. Status `Execution Approval` ditetapkan menjadi `APPROVED`.
