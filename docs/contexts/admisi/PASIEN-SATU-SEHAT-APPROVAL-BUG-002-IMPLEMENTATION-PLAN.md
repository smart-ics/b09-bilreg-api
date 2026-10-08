---
Title: Implementation Plan - Koreksi Siklus Hidup dan Integrasi Event Dialog Konfirmasi Persetujuan Satu Sehat
Code: PASIEN-SATU-SEHAT-APPROVAL-BUG-002
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-08
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan arsitektur target koreksi kecacatan (*bug correction*) yang didefinisikan pada [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md) (V1.0) guna menuntaskan issue [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ISSUE.md) (`ISSUE-ADMISI-PASIEN-SASET-BUG-002`):
1. Merekayasa ulang tombol konfirmasi pada `DialogPersetujuanSatuSehat.vue` dengan melepaskan komponen primitif auto-close `AlertDialogAction` (Reka-UI `DialogClose`) menjadi komponen `<Button>` aksi standar, sehingga penekanan tombol "Setuju" tidak memicu penutupan dialog prematur dan tidak memancarkan event `cancel`.
2. Menerapkan proteksi penutupan dialog (*guarded dismissal*) pada `DialogPersetujuanSatuSehat.vue` dan `LegacyRegistrationWorkspace.vue` saat kondisi asinkron `isPending` aktif.
3. Menata ulang orkestrasi asinkron dua fase (*two-phase asynchronous lifecycle*) pada `useRegistrasiActions.ts`, di mana modal tetap terbuka menampilkan status visual loading (`isPending = true`) hingga mutasi `performApprovalNonBlocking(targetPasienId)` selesai dieksekusi dan modal ditutup secara teratur di blok `finally`.
4. Memperbarui dan melengkapi automated testing suite (`DialogPersetujuanSatuSehat.spec.ts` dan `useRegistrasiActions.satuSehat.spec.ts`) guna menjamin regresi teratasi secara tuntas.

Referenced artifacts:
- ARCHITECTURE: [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md) (V1.0)
- BUG-INVESTIGATION: [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-BUG-INVESTIGATION.md)
- ISSUE: [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ISSUE.md) (`ISSUE-ADMISI-PASIEN-SASET-BUG-002`)
- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repository:
- `c012_myhospital_web` (Frontend Vue 3 + TypeScript: Admisi Components/Composables dan Automated Unit/Integration Tests)

In scope:
- Refaktor [DialogPersetujuanSatuSehat.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue): penggantian `AlertDialogAction` dengan `Button`, penjagaan handler `onOpenChange` terhadap state `isPending`.
- Penyesuaian event handler dan lifecycle modal pada [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue).
- Penataan alur resolusi Promise dan blok penutupan dialog `finally` di [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts).
- Pembaruan unit test stubs & skenario di [DialogPersetujuanSatuSehat.spec.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/__tests__/DialogPersetujuanSatuSehat.spec.ts).
- Pembaruan & penambahan integrasi test di [useRegistrasiActions.satuSehat.spec.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts).

Out of scope:
- Modifikasi backend .NET `b09-bilreg-api` (endpoint, command, DAL, dan repositori telah stabil dan fungsional).
- Modifikasi skema tabel database `tc_mr_saset`.
- Modifikasi alur auto-approval pasien BPJS (sudah berfungsi dengan benar tanpa dialog).

---

# 3. Dependencies

External dependencies:
- Backend endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset` tersedia dan dapat merespons `JSendOk("Done")`.
- Pustaka UI `@/shared/components/ui/button` tersedia di frontend `c012_myhospital_web`.

Slice dependencies:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED` dan artefak keluaran terverifikasi di repositori target.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.
- Slices yang tidak memiliki dependensi satu sama lain dapat dikerjakan secara paralel (Dependency-Driven Parallelism).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - UI Component & Modal Primitive Decoupling | IMPLEMENTED | GO | 1/1 |
| P2 - Workflow Orchestration & Integration Testing | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - UI Component & Modal Primitive Decoupling

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini berfokus pada refaktor internal komponen modal [DialogPersetujuanSatuSehat.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue) untuk melepaskan tombol "Setuju" dari primitif auto-close bawaan `reka-ui`, mengamankan siklus `onOpenChange`, serta memperbarui unit test stubs.

### P1-S01

Title: Refaktor DialogPersetujuanSatuSehat & Pembaruan Unit Test Komponen

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
1. Mengganti implementasi tombol "Setuju" dari `<AlertDialogAction>` ke `<Button>` standar bertipe `button` dengan Tailwind styling emerald (`bg-emerald-600 text-white hover:bg-emerald-700`) dan event click `@click="onConfirm"`.
2. Melindungi fungsi `onOpenChange(value: boolean)` agar mengabaikan event penutupan (`return`) jika `!value && props.isPending`.
3. Memastikan klik pada tombol "Setuju" hanya memancarkan event `confirm` tanpa memicu event `cancel` atau memanipulasi event `update:open`.
4. Memperbarui suite pengujian pada `DialogPersetujuanSatuSehat.spec.ts` untuk menguji penggunaan komponen `<Button>`, memastikan event `confirm` terisolasi dari penutupan prematur, dan memverifikasi proteksi penutupan saat `isPending === true`.

Depends On: None  

Repository: `c012_myhospital_web`  

Completion Criteria:  
- File `src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue` diperbarui:
  - Mengimpor `Button` dari `@/shared/components/ui/button`.
  - Mengganti `<AlertDialogAction>` dengan `<Button>`.
  - Fungsi `onOpenChange` memiliki pengecekan `if (!value && props.isPending) return`.
  - Klik tombol "Setuju" hanya memancarkan event `confirm`.
- File `src/modules/Admisi/components/registrationAssistance/__tests__/DialogPersetujuanSatuSehat.spec.ts` diperbarui:
  - Stubs disesuaikan dengan komponen `Button`.
  - Test case memverifikasi tombol Setuju memancarkan `confirm` tanpa emisi `cancel` atau `update:open: false`.
  - Test case memverifikasi penutupan dialog diabaikan saat `isPending === true`.
- Seluruh pengujian pada `DialogPersetujuanSatuSehat.spec.ts` berstatus hijau / PASS (`pnpm test DialogPersetujuanSatuSehat.spec.ts`).

Notes:  
- Mengacu pada keputusan teknis TD-01 dan TD-02 pada [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md).
- Implementation Notes:
  - Komponen `DialogPersetujuanSatuSehat.vue` telah direfaktor untuk menggunakan `<Button>` standar menggantikan `<AlertDialogAction>` (TD-01).
  - Handler `onOpenChange` telah diproteksi dengan guard `if (!value && props.isPending) return` untuk mencegah modal dismiss prematur saat proses asinkron aktif (TD-02).
  - Test suite pada `DialogPersetujuanSatuSehat.spec.ts` diperbarui dengan stub `Button`, verifikasi isolasi event `confirm`, dan pengujian proteksi dismissal saat `isPending === true`. Seluruh 10 test case lulus (PASS).
  - Changed files:
    - `src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue`
    - `src/modules/Admisi/components/registrationAssistance/__tests__/DialogPersetujuanSatuSehat.spec.ts`
    - `package.json` (menambahkan script `test` untuk mengeksekusi vitest)

---

## P2 - Workflow Orchestration & Integration Testing

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini mengintegrasikan komponen dialog dengan workspace pendaftaran admisi [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue), menata siklus hidup asinkron dua fase di [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts), dan memvalidasi alur akhir melalui integration testing.

### P2-S02

Title: Orkestrasi Asinkron Pre-Submit Admisi & Pengujian Integrasi Persetujuan Satu Sehat

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
1. Memperbarui binding event `<DialogPersetujuanSatuSehat>` pada `LegacyRegistrationWorkspace.vue` agar listener `@update:open` tidak memicu pembatalan (`handleCancelSatuSehat`) jika state `satuSehatDialog.isPending` sedang aktif.
2. Memperbaiki fungsi penanganan pada `useRegistrasiActions.ts`:
   - `handleConfirmSatuSehat`: Menyelesaikan Promise dengan `true` (`satuSehatDialog.resolve(true)`) dan mengosongkan resolver pointer (`satuSehatDialog.resolve = null`), tanpa menutup dialog (`openDialogStates.satuSehatConfirmation` tetap `true`).
   - `handleCancelSatuSehat`: Menutup dialog (`openDialogStates.satuSehatConfirmation = false`) dan menyelesaikan Promise dengan `false` hanya jika `!satuSehatDialog.isPending`.
   - `handleSubmitRegister`: Menjalankan alur persetujuan Non-BPJS dengan proteksi `try...finally`:
     - Set `satuSehatDialog.isPending = true`.
     - `await performApprovalNonBlocking(targetPasienId)`.
     - Pada blok `finally`: set `satuSehatDialog.isPending = false` dan `openDialogStates.satuSehatConfirmation = false`.
3. Memperbarui unit & integration test pada `useRegistrasiActions.satuSehat.spec.ts`:
   - Memvalidasi bahwa ketika tombol "Setuju" ditekan (memicu `handleConfirmSatuSehat`), dialog tetap terbuka (`satuSehatConfirmation === true`) dengan status `isPending === true` selama pemanggilan approval berlangsung.
   - Memvalidasi bahwa mutasi `useApproveUploadSaset.mutateAsync({ pasienId })` terpanggil dengan parameter yang tepat.
   - Memvalidasi bahwa dialog tertutup rapi setelah mutasi selesai dan registrasi kunjungan berhasil disimpan.

Depends On: P1-S01  

Repository: `c012_myhospital_web`  

Completion Criteria:  
- File `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue` diperbarui pada binding `@update:open`:
  `@update:open="(val) => { if (!val && !satuSehatDialog.isPending) handleCancelSatuSehat() }"`
- File `src/modules/Admisi/composables/useRegistrasiActions.ts` diperbarui:
  - `handleConfirmSatuSehat` me-resolve `true` dan tidak menutup dialog secara prematur.
  - `handleCancelSatuSehat` memproteksi eksekusi pembatalan saat `isPending === true`.
  - `handleSubmitRegister` menutup dialog pada blok `finally` setelah eksekusi `performApprovalNonBlocking`.
- File `src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts` mencakup pengujian alur asinkron dua fase dan seluruh test suite berstatus hijau / PASS (`pnpm test useRegistrasiActions.satuSehat.spec.ts`).

Notes:  
- Mengacu pada keputusan teknis TD-02 dan TD-03 pada [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md).
- Implementation Notes:
  - Event listener `@update:open` pada `LegacyRegistrationWorkspace.vue` telah diperbarui dengan guard `if (!val && !satuSehatDialog.isPending) handleCancelSatuSehat()`.
  - `handleCancelSatuSehat` pada `useRegistrasiActions.ts` telah dilengkapi guard `if (satuSehatDialog.isPending) return`.
  - Alur persetujuan Non-BPJS pada `handleSubmitRegister` mempertahankan modal terbuka selama proses approval berlangsung dan menutup modal rapi pada blok `finally`.
  - Integration test suite pada `useRegistrasiActions.satuSehat.spec.ts` diperbarui untuk menguji dua fase asinkron, proteksi pembatalan saat loading, dan penutupan tertib saat mutasi selesai (12/12 tests PASS).
  - Verifikasi TypeScript `vue-tsc --noEmit` sukses tanpa kesalahan (0 errors).
  - Changed files:
    - `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue`
    - `src/modules/Admisi/composables/useRegistrasiActions.ts`
    - `src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts`

---

# 6. Change Log

- 2026-10-08: Rencana implementasi awal dibuat berdasarkan arsitektur target PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md V1.0. Rencana disetujui untuk eksekusi (Execution Approval: APPROVED).
