---
Title: Implementation Plan - Penanganan Respons Non-BPJS JetliApi untuk Konfirmasi Persetujuan Satu Sehat Admisi
Code: PASIEN-SATU-SEHAT-APPROVAL-BUG-001
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-08
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Memperbaiki penanganan respons pemetaan grup jaminan eksternal JetliApi (`/GrupJaminan/map`) pada alur pra-simpan registrasi admisi rawat jalan (`useRegistrasiActions.ts`), sehingga kode status HTTP 400 / 404 dengan payload `"Data Not Found"` dari JetliApi diinterpretasikan secara tepat sebagai kelompok jaminan Non-BPJS (`false`). Hal ini memastikan pop-up modal dialog "Konfirmasi Persetujuan Satu Sehat" ditampilkan kepada petugas/pasien sebelum registrasi disimpan, sembari tetap menjaga mekanisme fail-safe non-blocking (`null`) untuk gangguan jaringan murni atau error internal server.

Referenced artifacts:
- BUG ISSUE: [PASIEN-SATU-SEHAT-APPROVAL-BUG-001-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-001-ISSUE.md)
- BUG INVESTIGATION: [PASIEN-SATU-SEHAT-APPROVAL-BUG-001-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-001-BUG-INVESTIGATION.md)
- Reference Architecture: [PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED
No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

### Included:
1. **Frontend Logic (`c012_myhospital_web`)**:
   - Modifikasi fungsi `checkIsBpjsJaminan` pada [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts):
     - Membedakan penanganan error penolakan bisnis JetliApi (HTTP 400 / 404 dengan pesan "Data Not Found" atau "Mapping ... tidak ditemukan") dari error teknis jaringan.
     - Mengembalikan nilai boolean `false` (Non-BPJS) dan menyimpan mapping `null` ke `grupJaminanCache` ketika respons 400/404 "Data Not Found" diterima.
     - Mempertahankan pengembalian nilai `null` (*fail-safe*) hanya jika terjadi error jaringan riil, timeout, atau kesalahan teknis server (5xx).
2. **Automated Unit Testing (`c012_myhospital_web`)**:
   - Pembaruan dan penambahan skenario pengujian unit pada [useRegistrasiActions.satuSehat.spec.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts):
     - Memverifikasi `checkIsBpjsJaminan('00000')` mengembalikan `false` saat JetliApi melempar `ApiError` dengan status 400 / 404 dan pesan "Data Not Found".
     - Memverifikasi dialog `requestSatuSehatApproval` muncul saat submit registrasi untuk pasien Non-BPJS umum (`00000`) ketika JetliApi mengembalikan 400 "Data Not Found".
     - Memverifikasi skenario fail-safe tetap bekerja (mengembalikan `null` dan melewati dialog) bila terjadi error jaringan murni (misal: timeout atau koneksi putus).

### Excluded:
1. Modifikasi service backend `b09-bilreg-api` (tidak terdampak).
2. Modifikasi endpoint eksternal `JetliApi` (di luar repositori Bilreg).
3. Modifikasi komponen visual dialog [DialogPersetujuanSatuSehat.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/DialogPersetujuanSatuSehat.vue).

---

# 3. Dependencies

### External Dependencies:
- Klien HTTP `ApiService` dan kelas `ApiError` (`@/core/services/ApiService.ts`).
- Modul Admisi composable `useRegistrasiActions.ts`.
- Framework pengujian `vitest` pada `c012_myhospital_web`.

### Slice Dependencies:
- `Depends On` mendeklarasikan prasyarat implementasi antar slice.
- P1-S01 tidak memiliki dependensi (`Depends On: None`).
- P1-S02 bergantung pada selesainya perbaikan logika di P1-S01 (`Depends On: P1-S01`).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Client Response Classification Correction | COMPLETED | GO | 2/2 |

---

# 5. Phases

## P1 - Client Response Classification Correction

Implementation Status: COMPLETED  
Review Status: GO  

### P1-S01

Title: Klasifikasi Respons HTTP 400/404 "Data Not Found" JetliApi sebagai Non-BPJS

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memperbarui fungsi `checkIsBpjsJaminan` di `useRegistrasiActions.ts` agar dapat menginspeksi error yang ditangkap dari `ApiService.get`. Jika error berupa `ApiError` (atau Axios error) dengan status HTTP 400 atau 404 yang mengindikasikan "Data Not Found" / mapping tidak ditemukan, kembalikan nilai `false` (Non-BPJS) dan simpan ke `grupJaminanCache.set(tipeJaminanId, null)`. Jika error merupakan kesalahan teknis/jaringan riil, pertahankan *fail-safe* dengan mengembalikan `null`.

Depends On: None  

Repository: c012_myhospital_web  

Completion Criteria:  
- Fungsi `checkIsBpjsJaminan` mengidentifikasi error respons 400/404 dengan payload / message "Data Not Found".
- Memanggil `checkIsBpjsJaminan("00000")` saat JetliApi merespons HTTP 400 "Data Not Found" menghasilkan kembalian `false`.
- Nilai di-cache ke `grupJaminanCache` sehingga pemanggilan berikutnya tidak mengulang request ke JetliApi.
- Error koneksi murni (Network Error / Timeout / 5xx) tetap mengembalikan `null` dengan peringatan di console log.

Notes:  
Periksa struktur `ApiError` pada `ApiService.ts`: properti `status`, `message`, dan `data` dapat digunakan untuk mendeteksi penolakan bisnis 400/404 secara presisi.

Implementation Notes:  
- Menambahkan fungsi helper `isJetliNotFound(err: unknown): boolean` di `useRegistrasiActions.ts` untuk memeriksa error respons HTTP 400 atau 404 yang mengindikasikan bahwa data mapping grup jaminan tidak ditemukan pada JetliApi (memeriksa status, pesan error, serta payload `data` terhadap "not found" / "tidak ditemukan" / "data not found").
- Memperbarui blok penanganan error pada `checkIsBpjsJaminan`: saat error dikenali sebagai `isJetliNotFound`, fungsi menyimpan mapping `null` ke `grupJaminanCache` dan mengembalikan nilai boolean `false` (Non-BPJS).
- Untuk gangguan teknis riil (Network Error, Timeout, 5xx), fail-safe tetap dipertahankan dengan mencatat warning ke console dan mengembalikan nilai `null`.
- Verifikasi berhasil: type-check `vue-tsc` lolos tanpa error dan 814 unit test Admisi lulus.

Changed Files:  
- `c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts`

---

### P1-S02

Title: Pengujian Unit Interseptor dan Klasifikasi Error JetliApi Satu Sehat

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan dan memperbarui cakupan unit test pada `useRegistrasiActions.satuSehat.spec.ts` untuk memvalidasi perilaku penanganan error 400/404 JetliApi dan memastikan dialog konfirmasi persetujuan Satu Sehat ditampilkan pada alur submit registrasi pasien jaminan Non-BPJS.

Depends On: P1-S01  

Repository: c012_myhospital_web  

Completion Criteria:  
- Test case `checkIsBpjsJaminan`: memverifikasi pengembalian `false` ketika `ApiService.get` melempar `ApiError` status 400 dengan data `{ status: "Data Not Found", data: "Data Mapping Group Jaminan tidak ditemukan" }`.
- Test case `handleSubmitRegister`: memverifikasi bahwa dialog konfirmasi persetujuan Satu Sehat (`openDialogStates.satuSehatConfirmation`) terbuka ketika jaminan Non-BPJS umum (`00000`) didaftarkan dan JetliApi merespons HTTP 400 "Data Not Found".
- Test case fail-safe murni tetap lulus (*PASS*) untuk error timeout / network error tanpa memunculkan dialog.
- Seluruh unit test dalam `useRegistrasiActions.satuSehat.spec.ts` lulus tanpa kegagalan (`vitest run`).

Notes:  
Gunakan helper mock `ApiError` dari `@/core/services` pada pengujian Vitest.

Implementation Notes:  
- Mengimpor `ApiError` dari `@/core/services` ke dalam `useRegistrasiActions.satuSehat.spec.ts`.
- Menambahkan pengujian unit pada suite `checkIsBpjsJaminan & grupJaminanCache` yang memverifikasi bahwa saat `ApiService.get` melempar `ApiError` status 400 dengan payload `"Data Not Found"` / `"Data Mapping Group Jaminan tidak ditemukan"`, fungsi mengembalikan `false` dan menyimpan mapping `null` ke `grupJaminanCache`. Panggilan berikutnya terbukti membaca dari cache tanpa mengulang pemanggilan API.
- Menambahkan pengujian unit pada alur `handleSubmitRegister` yang memvalidasi bahwa dialog konfirmasi persetujuan Satu Sehat (`openDialogStates.satuSehatConfirmation`) terbuka saat pasien jaminan Non-BPJS umum (`00000`) didaftarkan dan JetliApi merespons HTTP 400 "Data Not Found", serta memastikan pendaftaran tetap berlanjut dan persetujuan dicatat saat user menyetujui dialog.
- Memastikan pengujian alur fail-safe tetap lulus (*PASS*) untuk gangguan jaringan riil tanpa memunculkan dialog.
- Verifikasi pengujian: seluruh 12 unit test pada `useRegistrasiActions.satuSehat.spec.ts` lulus (100%), seluruh 816 unit test modul Admisi lulus, dan `npm run type-check` (`vue-tsc`) lolos tanpa error.

Changed Files:  
- `c012_myhospital_web/src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts`

---

# 6. Change Log

- 2026-10-08: Inisialisasi dokumen IMPLEMENTATION-PLAN untuk resolusi defect DEF-001 / BUG-001 (ISSUE-ADMISI-PASIEN-SASET-BUG-001). Execution Approval di-set ke APPROVED.
