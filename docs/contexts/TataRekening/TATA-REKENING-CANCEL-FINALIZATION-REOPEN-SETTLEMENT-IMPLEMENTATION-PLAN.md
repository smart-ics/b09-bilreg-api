---
Title: Rencana Implementasi Perbaikan Defek Batal Finalisasi, Verifikasi Reopen, dan Eliminasi Tombol Settlement Tata Rekening
Code: IPL-TR-BUG-001
Artifact: IMPLEMENTATION-PLAN
Version: 1.0.0
LastUpdated: 2026-11-06
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Rencana implementasi untuk menyelesaikan tiga masalah siklus hidup dan antarmuka Tata Rekening yang teridentifikasi pada [ISSUE-TR-BUG-001](TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ISSUE.md):
1. Mengoreksi pengecekan downstream barrier pada Batal Finalisasi sehingga tidak terhambat oleh keberadaan record alokasi penjamin internal.
2. Memverifikasi dan menjaga kepatuhan transisi status Reopen dari `CLOSED` ke `OPEN`.
3. Menghilangkan tombol dan panduan Inisiasi Settlement pada antarmuka Tata Rekening agar konsisten dengan alur kerja 2 langkah.

Planning Mode: FEATURE-PLANNING  
Referenced artifacts:
- ISSUE: [TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ISSUE.md](TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ISSUE.md)
- BUG-INVESTIGATION: [TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-INVESTIGATION.md](TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-INVESTIGATION.md)
- ARCHITECTURE: [TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ARCHITECTURE.md](TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

- Backend Repository: `b09-bilreg-api` (Domain, Infrastructure, Application, Unit Test)
- Frontend Repository: `c012_myhospital_web` (Components, Composables, Unit Test)
- Slices: 5 slice implementasi dalam 3 phase terstruktur.

---

# 3. Dependencies

- `P1-S02` bergantung pada `P1-S01`
- `P2-S04` bergantung pada `P2-S03`
- `P3-S05` bergantung pada `P1-S02` dan `P2-S04`

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend Downstream Barrier Correction | IMPLEMENTED | GO | 2/2 |
| P2 - Frontend UI Settlement Elimination | IMPLEMENTED | GO | 2/2 |
| P3 - End-to-End Regression Verification | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Backend Downstream Barrier Correction

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01

Title: Koreksi Logika Downstream Barrier pada Aggregate Tata Rekening & Inspector  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menyesuaikan validasi `EnsureNoDownstreamSettlement` pada `TataRekeningModel.cs` dan kueri pada `DownstreamSettlementInspector.cs` agar baris alokasi penjamin internal yang dibuat saat finalisasi (`ta_trs_billing2` dengan prefix `RO`) tidak lagi dianggap sebagai blocker downstream. Pembatalan status final hanya boleh ditolak jika terdapat transaksi pembayaran kasir riil (`HasCashierSettlement`).

Depends On: None  
Repository: `b09-bilreg-api`  

Completion Criteria:  
- `TataRekeningModel.CancelFinalization` berhasil dieksekusi pada tagihan finalized yang memiliki alokasi penjamin internal selama belum ada pembayaran kasir.
- Status tagihan berhasil kembali ke `CLOSED` dan seluruh alokasi penjamin dibatalkan.
- Pembatalan status final tetap ditolak dengan pesan blocker jika `HasCashierSettlement` bernilai `true`.

Implementation Notes:  
- Memodifikasi `EnsureNoDownstreamSettlement` pada `TataRekeningModel.cs` sehingga hanya memeriksa transaksi kasir hilir (`HasCashierSettlement(settlementInspector)`). Keberadaan alokasi penjamin internal tidak lagi memblokir pembatalan finalisasi.
- Memutakhirkan `DownstreamSettlementInspector.cs`: menetralkan method `HasApprovedAllocation(regId)` dengan mengembalikan `false` dan mendokumentasikan bahwa baris penjamin berawalan 'RO' merupakan proyeksi alokasi internal yang dibentuk saat finalisasi dan dibatalkan saat pembatalan finalisasi.
- Seluruh kompilasi pada `Bilreg.Domain` dan `Bilreg.Infrastructure` berhasil tanpa galat (`0 Error(s)`). Pengujian lifecycle pada `TataRekeningLifecycleDomainTest` lulus 100% (16/16).

Changed Files:  
- `src/bilreg/Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs`
- `src/bilreg/Bilreg.Infrastructure/PaymentContext/TataRekeningFeature/DownstreamSettlementInspector.cs`

Notes:  
Periksa `TataRekeningModel.cs` baris 489–495 dan `DownstreamSettlementInspector.cs` baris 25–41.

---

### P1-S02

Title: Pemutakhiran Unit Test Backend Batal Finalisasi & Reopen  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memutakhirkan test suite backend pada `CancelFinalizationSupervisorGovernedTest.cs` dan `TataRekeningLifecycleDomainTest.cs` agar mencerminkan aturan baru bahwa alokasi penjamin internal tidak memblokir pembatalan, serta mengonfirmasi bahwa eksekusi Reopen mentransisikan status dari `Closed` ke `Opened`.

Depends On: P1-S01  
Repository: `b09-bilreg-api`  

Completion Criteria:  
- Pengujian unit `CancelFinalizationSupervisorGovernedTest` dan `TataRekeningLifecycleDomainTest` lulus 100%.
- Skenario pembatalan finalisasi dengan alokasi penjamin internal berjalan sukses mengembalikan status ke `Closed`.
- Skenario Reopen mengembalikan status ke `Opened` dengan alokasi/verifikasi yang bersih.

Implementation Notes:  
- Memutakhirkan `CancelFinalizationSupervisorGovernedTest.cs`:
  - Mengubah test `S04_02` dari yang sebelumnya mengharapkan exception downstream barrier pada alokasi penjamin menjadi memverifikasi bahwa `HasApprovedAllocation` tidak memblokir pembatalan, pembatalan finalisasi sukses, status beralih ke `Closed`, alokasi penjamin dibatalkan, dan cancellation log serta audit log tersimpan dengan benar.
  - Memperbarui `S04_07` untuk menguji bahwa `HasCashierSettlement` pada aggregate melempar `InvalidOperationException` dan mempertahankan status `Finalized`.
  - Menambahkan `S04_08` pada aggregate untuk memvalidasi bahwa alokasi penjamin internal saja tidak memblokir dan berhasil mengembalikan status ke `Closed`.
- Memutakhirkan `TataRekeningLifecycleDomainTest.cs`:
  - Menambahkan test `UT16` untuk memverifikasi bahwa `ReOpen()` mentransisikan status dari `Closed` ke `Opened`, mereset `FinancialVerificationStatus` menjadi `NotVerified`, mengosongkan `FinancialVerificationInfo`, mengosongkan `IsFinancialResponsibilityAllocated`, dan memastikan `SettlementInitiated` bernilai `false`.
  - Menambahkan test `UT17` untuk memverifikasi siklus hidup lengkap: `Finalized` -> `CancelFinalization` (dengan alokasi penjamin internal) -> `Closed` -> `ReOpen` -> `Opened`.
- Hasil eksekusi test:
  - `dotnet test --filter "FullyQualifiedName~CancelFinalization|FullyQualifiedName~TataRekeningLifecycleDomainTest"` lulus 100% (32/32 tests passed).
  - Keseluruhan test suite `FullyQualifiedName~TataRekening` lulus 100% (298/298 tests passed).

Changed Files:  
- `src/bilreg/Bilreg.Test/PaymentContext/TataRekeningFeature/CancelFinalizationSupervisorGovernedTest.cs`
- `src/bilreg/Bilreg.Test/PaymentContext/TataRekeningFeature/TataRekeningLifecycleDomainTest.cs`

Notes:  
Gunakan `dotnet test` dengan filter `FullyQualifiedName~CancelFinalization` dan `FullyQualifiedName~ReOpen`.

---

## P2 - Frontend UI Settlement Elimination & Action Flow Alignment

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S03

Title: Penghapusan Tombol Inisiasi Settlement pada Action Panel & Toolbar  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menghapus tombol "Inisiasi Settlement (Reg Out)" dan dialog terkait dari komponen antarmuka pengguna Tata Rekening.

Depends On: None  
Repository: `c012_myhospital_web`  

Completion Criteria:  
- Tombol `Inisiasi Settlement (Reg Out)` dihapus dari `TataRekeningActionPanel.vue` (baris 790–822).
- Tombol `Inisiasi Settlement (Reg Out)` dihapus dari `TataRekeningActionToolbar.vue`.
- Tagihan berstatus `FINALIZED` tidak lagi menampilkan tombol inisiasi settlement.

Implementation Notes:  
- Menghapus tombol `Inisiasi Settlement (Reg Out)` dari `TataRekeningActionPanel.vue`, menonaktifkan trigger pembukaan dialog settlement (`onOpenSettlementDialog`), serta menghapus render `<SettlementInitiationDialog>` dan import komponennya.
- Menghapus tombol `Inisiasi Settlement` dari `TataRekeningActionToolbar.vue`, menyelaraskan teks banner informasi agar merujuk ke kesiapan pembayaran Kasir (menggantikan istilah Settlement), serta menghapus import ikon `Send` yang tidak lagi digunakan.
- Menambahkan unit test suite `Penghapusan Tombol Inisiasi Settlement (TD-03)` pada `TataRekeningActionToolbar.spec.ts` untuk memverifikasi tombol Inisiasi Settlement tidak dirender pada toolbar meskipun `canSettlementInitiate` bernilai true dan status tagihan adalah `Finalized`.
- Hasil pengujian:
  - `npx vitest run src/modules/TataRekening/components/actions/__tests__/TataRekeningActionToolbar.spec.ts` lulus 100% (10/10 tests passed).
  - `npx vitest run src/modules/TataRekening/components/__tests__/TataRekeningWorkspace.spec.ts` lulus 100% (20/20 tests passed).

Changed Files:  
- `c012_myhospital_web/src/modules/TataRekening/components/actions/TataRekeningActionPanel.vue`
- `c012_myhospital_web/src/modules/TataRekening/components/actions/TataRekeningActionToolbar.vue`
- `c012_myhospital_web/src/modules/TataRekening/components/actions/__tests__/TataRekeningActionToolbar.spec.ts`

Notes:  
Dialog settlement dapat tetap disimpan di codebase atau dinonaktifkan dari rendering jika tidak lagi diakses.

---

### P2-S04

Title: Penyesuaian Next Action Guidance & Reaktivitas Aksi Pasca-Batal  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menyesuaikan komputasi `nextActionInfo` dan guard aksi pada `TataRekeningActionPanel.vue` serta `useTataRekeningActionGuards.ts`:
1. Pada status `FINALIZED`, hilangkan ajakan `action: 'settle'` ("Langkah Berikutnya: Inisiasi Settlement (Reg Out)") dan ganti dengan kartu status informatif "Siap Pembayaran Kasir" tanpa tombol aksi aktif (`action: 'none'`).
2. Memastikan bahwa ketika status kembali ke `CLOSED` (termasuk segera setelah mutasi Batal Finalisasi berhasil), tombol aktif yang disorot otomatis adalah **Langkah 2: Finalisasi Billing**.

Depends On: P2-S03  
Repository: `c012_myhospital_web`  

Completion Criteria:  
- Pada status `FINALIZED`, tidak ada panduan langkah settlement.
- Pada status `CLOSED`, langkah berikutnya mengarah ke `Langkah 2: Finalisasi Billing`.
- Unit test frontend pada `TataRekeningActionPanel.spec.ts` diperbarui dan lulus 100%.

Implementation Notes:  
- Menyesuaikan `nextActionInfo` pada `TataRekeningActionPanel.vue` saat status `FINALIZED`:
  - Menghilangkan `action: 'settle'` ("Langkah Berikutnya: Inisiasi Settlement (Reg Out)").
  - Menggantinya dengan kartu informatif "Siap Pembayaran Kasir" dengan `action: 'none'` dan badge/icon `CheckCircle2`, serta mempertahankan "Menunggu Pembayaran Kasir" (`action: 'none'`) saat `settlementInitiated` telah bernilai true.
  - Menghapus import `Send` dari `@lucide/vue` yang tidak lagi digunakan.
  - Memperbarui teks banner pasca-finalisasi dan pasca-batal-finalisasi agar merujuk ke kesiapan pembayaran Kasir alih-alih Settlement.
- Memperbarui guard `canCancelFinalization` pada `useTataRekeningActionGuards.ts` menggunakan `!toValue(summary)?.settlementInitiated` untuk penanganan status falsy yang lebih tangguh.
- Memperbarui unit test pada `TataRekeningActionPanel.spec.ts`:
  - Mengubah test klik tombol settlement menjadi pengujian yang memastikan tombol Inisiasi Settlement tidak dirender pada DOM panel aksi saat Finalized.
  - Menyesuaikan pengujian workflow `next-action-guidance` pada status `FINALIZED` agar memverifikasi kartu "Siap Pembayaran Kasir" dengan `action: 'none'` dan ketiadaan tombol settlement.
  - Memperbarui pengujian urutan kanonikal tombol 2-step workflow (Langkah 1 Tutup Billing -> Langkah 2 Finalisasi Billing -> Aksi Sekunder) tanpa adanya tombol settlement.
  - Menambahkan test verifikasi bahwa saat status `CLOSED` (termasuk pasca-batal finalisasi), next action guidance otomatis menyorot `Langkah 2: Finalisasi Billing` dengan style tombol emerald teraktivasi.
- Hasil pengujian:
  - `npx vitest run src/modules/TataRekening/components/__tests__/TataRekeningActionPanel.spec.ts` lulus 100% (24/24 tests passed).
  - Keseluruhan suite pengujian TataRekening (`npx vitest run src/modules/TataRekening/`) lulus 100% (8 test files, 98 tests passed).

Changed Files:  
- `c012_myhospital_web/src/modules/TataRekening/components/actions/TataRekeningActionPanel.vue`
- `c012_myhospital_web/src/modules/TataRekening/composables/useTataRekeningActionGuards.ts`
- `c012_myhospital_web/src/modules/TataRekening/components/__tests__/TataRekeningActionPanel.spec.ts`

Notes:  
Gunakan `npx vitest run src/modules/TataRekening/components/__tests__/TataRekeningActionPanel.spec.ts`.

---

## P3 - End-to-End Regression Verification

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P3-S05

Title: Verifikasi Regresi Lengkap Backend & Frontend  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menjalankan seluruh suite pengujian otomatis pada kedua repositori untuk memastikan tidak ada efek samping maupun regresi pada fitur Tata Rekening dan konteks terkait.

Depends On: P1-S02, P2-S04  
Repository: `b09-bilreg-api`, `c012_myhospital_web`  

Completion Criteria:  
- `dotnet test` pada `b09-bilreg-api` terkait PaymentContext dan TataRekening lulus 100%.
- `vitest run` pada `c012_myhospital_web` untuk modul Tata Rekening lulus 100%.
- Kedua repositori dalam kondisi clean dan siap untuk sinkronisasi git.

Implementation Notes:  
- Menjalankan suite pengujian backend: `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~TataRekening|FullyQualifiedName~AlokasiPembayaran"`.
  - Hasil: 330 passed, 0 failed, 0 skipped (100% lulus, durasi 10 detik).
- Menjalankan suite pengujian frontend: `cd c012_myhospital_web && npx vitest run src/modules/TataRekening/`.
  - Hasil: 25 test files passed, 350 tests passed, 0 failed (100% lulus).
- Memverifikasi status repositori git:
  - `b09-bilreg-api`: 4 modified files (aggregate model, downstream inspector, cancel finalization test, lifecycle domain test), 4 untracked markdown docs.
  - `c012_myhospital_web`: 5 modified files (action panel, action toolbar, useTataRekeningActionGuards, dan unit test masing-masing).
  - Tidak ada compiler warning atau assertion error yang tertinggal.
- Seluruh kriteria penyelesaian terpenuhi, tidak ditemukan regresi atau efek samping.

Changed Files:  
- None (verification slice across both repositories)

Notes:  
Pastikan tidak ada compiler warning atau assertion error yang tertinggal.

---

# 6. Change Log

- 2026-11-06: Dokumen rencana implementasi awal disetujui (Execution Approval: APPROVED) untuk perbaikan defek Batal Finalisasi, verifikasi Reopen, dan eliminasi tombol settlement.
