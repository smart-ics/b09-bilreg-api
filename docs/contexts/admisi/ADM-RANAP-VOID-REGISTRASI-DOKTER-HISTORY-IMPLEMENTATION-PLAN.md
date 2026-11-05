---
Title: Implementation Plan - Perbaikan Void Registrasi Admisi Ranap Terkait Riwayat Dokter
Code: PLAN-ADM-RANAP-VOID-REG-DOKTER-001
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-03T10:30:00+07:00
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Memperbaiki kegagalan pembatalan admisi / void registrasi rawat inap (*coordinated cancellation*) yang terjadi akibat ketiadaan riwayat penugasan dokter di tabel `ta_reg_history_dokter`, mencegah terhapusnya data riwayat dokter rawat inap saat pembaruan data registrasi / eligibilitas jaminan, serta menyajikan pesan kesalahan server secara transparan pada antarmuka pengguna jika terjadi penolakan.

Referenced artifacts:

- ISSUE: [ADM-RANAP-VOID-REGISTRASI-DOKTER-HISTORY-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-VOID-REGISTRASI-DOKTER-HISTORY-BUG-ISSUE.md) (`ISSUE-ADM-RANAP-VOID-REG-DOKTER-001`)
- BUG-INVESTIGATION: [ADM-RANAP-VOID-REGISTRASI-DOKTER-HISTORY-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-VOID-REGISTRASI-DOKTER-HISTORY-BUG-INVESTIGATION.md)
- FEASIBILITY-ASSESSMENT: Terintegrasi dalam BUG-INVESTIGATION (Status: CLOSED / APPROVED)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED

No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

Perbaikan mencakup dua repositori dalam *workspace*:

1. **Backend Core Persistence & Coordination (`b09-bilreg-api`)**:
   - `RegRepo.cs`: Mengisolasi pemanggilan penghapusan riwayat dokter (`_regHistoryDokterDal.Delete`) agar **tidak dijalankan** pada registrasi rawat inap (`model.JenisReg == JenisRegEnum.RegInap`). Siklus hidup dokter rawat inap secara eksklusif dikelola oleh `RegInapRepo`.
   - `AdmCoordinatedCancelCmd.cs`:
     - Menghapus syarat wajib `state.ActiveDoctorAssignments < 1` pada metode `ValidateState`. Ketiadaan rekaman dokter aktif tidak boleh menggagalkan pembatalan jika registrasi dan admisi valid dan belum memiliki transaksi tagihan.
     - Memperbarui langkah mutasi penutupan dokter agar tidak memicu `CoordinatedCancellationErrorCode.ConcurrencyConflict` saat `state.ActiveDoctorAssignments == 0` (hanya mengecek `_repo.EndDoctorAssignments(...) < 1` jika `state.ActiveDoctorAssignments > 0`).
   - Validasi Otomatis:
     - Unit test di `RegRepoTest.cs` memastikan `SaveChanges` rawat inap tidak memanggil `Delete` pada `IRegHistoryDokterDal`.
     - Unit test di `AdmCoordinatedCancelHandlerTest.cs` memverifikasi skenario pembatalan admisi dengan `ActiveDoctorAssignments == 0` dan billing kosong berhasil dijalankan.
     - Integration test di `AdmissionCancellationApiIntegrationTest.cs` memverifikasi alur API pembatalan tetap sukses.

2. **Frontend UI Module (`c012_myhospital_web`)**:
   - `useJourneyActionHost.ts`: Memperbarui fungsi `handleJourneyMutationError` agar memprioritaskan pesan error terperinci dari payload respon server (`err.data?.message` atau `err.message`) saat menerima status HTTP 409 Conflict, dan hanya menampilkan toast default bentrokan versi jika pesan server tidak tersedia.
   - Validasi Otomatis:
     - Unit test di `useJourneyActionHost.spec.ts` memverifikasi pemunculan pesan error server saat mutasi pembatalan mengembalikan HTTP 409 dengan payload pesan.

---

# 3. Dependencies

- External Dependencies:
  - Lingkungan database SQL Server `HOSPITAL_PKL` dengan kredensial pengujian yang valid.
  - Runtime .NET 8 SDK untuk `b09-bilreg-api` dan Node.js / pnpm untuk `c012_myhospital_web`.

- Slice Dependencies:
  - `P1-S01`: Tidak memiliki ketergantungan (titik awal perbaikan persistensi).
  - `P1-S02`: Bergantung pada `P1-S01` (memastikan persistensi aman sebelum koordinasi pembatalan direlaksasi).
  - `P2-S03`: Bergantung pada `P1-S02` (observabilitas frontend diuji setelah perilaku backend terdefinisi).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---------|---------|---------|---------|
| P1 | IMPLEMENTED | GO | 2/2 |
| P2 | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Backend Persistence & Coordinated Cancellation Fixes (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01

Title: Cegah Penghapusan ta_reg_history_dokter pada RegRepo untuk Registrasi Rawat Inap

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mencegah `RegRepo.SaveChanges` menghapus data riwayat dokter rawat inap (`ta_reg_history_dokter`) saat dipanggil pada alur pembaruan registrasi (misalnya saat pengisian jaminan atau eligibilitas SEP JKN).

Depends On: None  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Berkas [RegRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegRepo.cs) diperbarui: pemanggilan `_regHistoryDokterDal.Delete(model)` hanya dilakukan jika `model.JenisReg != JenisRegEnum.RegInap`.
2. Pengujian unit di [RegRepoTest.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegRepoTest.cs) ditambahkan untuk memverifikasi bahwa ketika `SaveChanges` dipanggil dengan `model.JenisReg == JenisRegEnum.RegInap`, `_regHistoryDokterDal.Delete` **tidak pernah dipanggil** (`Times.Never()`).
3. Seluruh rangkaian pengujian di `Bilreg.Test` untuk `AdmisiContext/RegFeature` lulus tanpa regresi via perintah:  
   `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RegRepoTest"`

Implementation Notes:  
- Menambahkan pengecekan `if (model.JenisReg != JenisRegEnum.RegInap)` sebelum `_regHistoryDokterDal.Delete(model)` pada `RegRepo.SaveChanges`. Dengan demikian riwayat penugasan dokter rawat inap tidak terhapus saat pembaruan data registrasi / eligibilitas.
- Menambahkan pengujian unit di `RegRepoTest.cs`:
  - `GivenRawatInapRegistration_WhenSaveChanges_ThenDoesNotDeleteHistoryDokter`: memverifikasi `_regHistoryDokterDal.Delete` tidak pernah dipanggil (`Times.Never`).
  - `GivenNonRawatInapRegistration_WhenSaveChanges_ThenDeletesHistoryDokter`: memverifikasi `_regHistoryDokterDal.Delete` dipanggil 1 kali (`Times.Once`) untuk non-rawat-inap (`RegJalan`).
- Seluruh 8 pengujian di `RegRepoTest` lulus sukses.

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegRepo.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegRepoTest.cs`

Notes:  
Siklus hidup dan data dokter rawat inap dikelola seutuhnya oleh `RegInapRepo` yang merepresentasikan `RegInapModel.ListDokter`. `RegModel` tidak memiliki properti koleksi dokter, sehingga pemanggilan `_regHistoryDokterDal.Delete(model)` pada rawat inap bersifat destruktif tanpa memasukkan kembali datanya.

---

### P1-S02

Title: Relaksasi Validasi Pembatalan Admisi Terkoordinasi saat Riwayat Dokter Kosong

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menghapus aturan restriktif pada pembatalan admisi terkoordinasi yang menolak eksekusi jika `ActiveDoctorAssignments < 1`, serta memastikan mutasi penutupan dokter tidak menggagalkan proses jika penugasan dokter memang tidak ada di basis data.

Depends On: P1-S01  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Berkas [AdmCoordinatedCancelCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs) diperbarui:
   - Pada `ValidateState`: Pemeriksaan `state.ActiveDoctorAssignments < 1` dihapus sehingga hanya memeriksa `state.RegAktifCount != 1`.
   - Pada eksekusi mutasi: Pemanggilan `_repo.EndDoctorAssignments(...)` dan verifikasi baris terpengaruh hanya dijalankan jika `state.ActiveDoctorAssignments > 0`.
2. Pengujian unit di [AdmCoordinatedCancelHandlerTest.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmCoordinatedCancelHandlerTest.cs) ditambahkan untuk menguji skenario pembatalan admisi dengan `ActiveDoctorAssignments == 0` (tanpa riwayat dokter) dan belum memiliki billing: pembatalan berhasil dengan status `Cancelled` dan `RegistrationVoided == true`.
3. Pengujian integrasi di [AdmissionCancellationApiIntegrationTest.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/Api/AdmissionCancellationApiIntegrationTest.cs) dipastikan tetap lulus tanpa regresi via perintah:  
   `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmCoordinatedCancelHandlerTest|FullyQualifiedName~AdmissionCancellationApiIntegrationTest"`

Implementation Notes:  
- Menghapus validasi restriktif `state.ActiveDoctorAssignments < 1` pada `ValidateState` di `AdmCoordinatedCancelCmd.cs` sehingga pembatalan admisi terkoordinasi hanya memvalidasi `state.RegAktifCount != 1`.
- Memperbarui langkah mutasi penutupan dokter pada `AdmCoordinatedCancelCmd.cs` menjadi bersyarat: `if (state.ActiveDoctorAssignments > 0 && _repo.EndDoctorAssignments(...) < 1)` agar penutupan penugasan dokter dan pemeriksaan baris terpengaruh hanya dieksekusi jika terdapat penugasan dokter aktif, tanpa memicu `ConcurrencyConflict` saat riwayat dokter kosong.
- Menambahkan pengujian unit `WhenActiveDoctorAssignmentsZeroAndNoBilling_CancellationSucceedsWithoutEndingDoctors` pada `AdmCoordinatedCancelHandlerTest.cs` untuk memverifikasi bahwa ketika `ActiveDoctorAssignments == 0` dan tidak ada tagihan billing, pembatalan sukses dengan status `Cancelled`, `RegistrationVoided == true`, serta `EndDoctorAssignments` tidak dipanggil.
- Memverifikasi pengujian integrasi `AdmissionCancellationApiIntegrationTest` (9 pengujian) dan pengujian unit `AdmCoordinatedCancelHandlerTest` (8 pengujian, total 17 pengujian) lulus tanpa regresi.

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmCoordinatedCancelHandlerTest.cs`

Notes:  
Aturan bisnis menetapkan bahwa penentu tunggal kelayakan pembatalan registrasi adalah keberadaan tagihan di Tata Rekening (`ta_trs_billing == 0`). Ketiadaan baris di `ta_reg_history_dokter` tidak boleh menjadi penghalang operasional.

---

## P2 - Frontend Error Transparency (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S03

Title: Transparansi Pesan Kesalahan HTTP 409 pada useJourneyActionHost

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Meningkatkan penanganan galat `handleJourneyMutationError` pada `useJourneyActionHost.ts` agar menyajikan pesan spesifik dari respon server jika tersedia saat terjadi HTTP 409 Conflict, alih-alih selalu menutupi dengan pesan generik konflik concurrency.

Depends On: P1-S02  

Repository: c012_myhospital_web  

Completion Criteria:  
1. Berkas [useJourneyActionHost.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts) diperbarui:
   - `handleJourneyMutationError` mengekstrak pesan server dari `(err.data as { message?: string })?.message` atau `err.message` (selain string mentah `"Conflict"`).
   - Jika pesan server ditemukan, tampilkan pesan tersebut melalui `toast.error(serverMessage)`.
   - Jika tidak ada pesan khusus dari server, tampilkan pesan fallback standar (*"Perjalanan berubah sejak terakhir dimuat..."*).
   - `refetchSelectedJourney()` tetap dipanggil untuk sinkronisasi state.
2. Pengujian unit di [useJourneyActionHost.spec.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/__tests__/useJourneyActionHost.spec.ts) ditambahkan / diperbarui:
   - Memverifikasi bahwa ketika API menghasilkan 409 dengan payload pesan tertentu (misal `"State pembatalan terkoordinasi tidak konsisten."`), toast menampilkan pesan tersebut.
   - Memverifikasi bahwa ketika 409 tanpa payload pesan diterima, toast menampilkan pesan default bentrokan versi.
3. Seluruh pengujian unit berjalan sukses via perintah:  
   `pnpm --dir d:\project_aktif\project_MyHospitalWeb\c012_myhospital_web test src/modules/Admisi/composables/__tests__/useJourneyActionHost.spec.ts`

Implementation Notes:  
- Memperbarui `handleJourneyMutationError` pada `useJourneyActionHost.ts` untuk mengekstrak pesan spesifik dari server melalui `(err.data as { message?: unknown })?.message` maupun `err.message` yang bukan string mentah `"Conflict"`.
- Jika pesan spesifik server ditemukan, pesan tersebut langsung ditampilkan via `toast.error(serverMessage)`. Jika tidak ditemukan pesan spesifik, tetap menampilkan pesan fallback bentrokan konkurensi default (*"Perjalanan berubah sejak terakhir dimuat. Data dimuat ulang — tinjau ulang sebelum melanjutkan."*).
- Menjamin pemanggilan `refetchSelectedJourney()` tetap dilakukan pada seluruh penanganan status 409 untuk sinkronisasi state query.
- Menambahkan 2 unit test baru di `useJourneyActionHost.spec.ts`:
  - `on 409 with specific server error payload surfaces specific message`: memverifikasi ketika API menghasilkan 409 dengan payload `message` di `err.data`, pesan tersebut ditampilkan di toast.
  - `on 409 with non-conflict error message string surfaces that message`: memverifikasi ketika API menghasilkan 409 dengan string `message` non-konflik, pesan tersebut ditampilkan di toast.
  - Memverifikasi pengujian eksisting tetap memvalidasi kemunculan pesan default saat `err.message` bernilai `"Conflict"`.
- Seluruh 22 test di `useJourneyActionHost.spec.ts` serta seluruh 1052 unit test pada `src/modules/Admisi` lulus tanpa regresi.

Changed Files:  
- `c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts`
- `c012_myhospital_web/src/modules/Admisi/composables/__tests__/useJourneyActionHost.spec.ts`

Notes:  
Perubahan ini menjaga konsistensi UX di mana pengguna mendapatkan umpan balik yang sebenarnya jika operasi mereka ditolak oleh aturan validasi backend.

---

# 6. Change Log

- 2026-11-03: Pembuatan awal dokumen rencana implementasi (`PLAN-ADM-RANAP-VOID-REG-DOKTER-001`) berdasarkan investigasi bug `ADM-RANAP-VOID-REGISTRASI-DOKTER-HISTORY-BUG-INVESTIGATION.md`. Execution Approval diberikan (`APPROVED`).
- 2026-11-03: Review slice P2-S03 selesai dengan keputusan GO. Seluruh slice (P1-S01, P1-S02, P2-S03) telah berstatus IMPLEMENTED & GO; status rencana diset menjadi COMPLETED.
