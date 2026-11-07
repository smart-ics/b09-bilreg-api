---
Title: Arsitektur Perbaikan Defek Batal Finalisasi, Konfirmasi Reopen, Eliminasi Tombol Settlement, dan Propagasi Identitas PegId Tata Rekening
Code: ARC-TR-BUG-001
Artifact: ARCHITECTURE
Version: 1.1.0
LastUpdated: 2026-11-06
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis komprehensif atas penyelesaian seluruh temuan defek pada modul Tata Rekening:
1. Eliminasi *false-positive downstream blocker* pada aksi Batal Finalisasi (`CancelFinalization`), sehingga tagihan yang difinalisasi dengan penjamin dapat dibatalkan kembali ke status `CLOSED` selama belum ada transaksi pembayaran kasir riil.
2. Penegasan arsitektural terhadap perilaku transisi status Reopen Billing langsung dari `CLOSED` ke `OPEN` sesuai `SOP-TR-09`.
3. Eliminasi elemen tombol dan navigasi langkah berikutnya terkait Inisiasi Settlement pada antarmuka pengguna (`c012_myhospital_web`) untuk menyelaraskan dengan implementasi alur kerja 2-langkah (*Close Bill* $\rightarrow$ *Finalize Billing*).
4. Penyelarasan propagasi identitas autentikasi user login: memastikan `PegId` (ID pegawai verifikator dari Usman) tersimpan sebagai identitas resmi petugas verifikasi, petugas keluar, dan petugas pembatalan (mengeliminasi fallback token subject statis `"BilregApiAccessToken"`).
5. Pencegahan fatal database exception akibat *data truncation* pada kolom `PetugasVerif` tabel `BILRG_TataRekening` dan kolom-kolom audit tabel `TA_REGISTRASI` melalui standardisasi ukuran string dan *defense-in-depth* truncation.

Mengacu pada artefak input:
- Issue: [ISSUE-TR-BUG-001 (TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ISSUE.md)](TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ISSUE.md)
- Investigasi: [TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-INVESTIGATION.md](TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-INVESTIGATION.md)

---

# 2. Architectural Basis

## Business Context & Analysis Input

Berdasarkan `SOP-TR-07 — Finalize Financial Responsibility`, `SOP-TR-08 — Cancel Finalization`, `SOP-TR-09 — Reopen Billing`, dan hasil analisis investigasi:
- **Precondition Batal Finalisasi**: Billing berstatus `FINALIZED` dan **belum terdapat Payment Settlement**.
- **Postcondition Batal Finalisasi**: Billing kembali berstatus `CLOSED`, alokasi penjamin dibatalkan, pencatatan petugas cancel out mencatat `PegId` verifikator, dan tombol aktif berikutnya adalah `Langkah 2: Finalisasi Billing`.
- **Precondition Reopen Billing**: Billing berstatus `CLOSED`.
- **Postcondition Reopen Billing**: Billing kembali berstatus `OPEN`, Billing Lock dilepaskan, alokasi/verifikasi dibersihkan, dan alur berulang dari `Langkah 1: Tutup Billing (Close)`.
- **Alur Kerja 2-Langkah**: Tahap Finalisasi Billing secara atomik telah menyatukan penetapan alokasi penjamin dan penandaan keluar pasien (`reg.SetKeluar(...)` / Reg Out). Oleh karena itu, tahapan mandiri "Inisiasi Settlement (Reg Out)" pada modul Tata Rekening telah usang dan redundan.
- **Standar Identitas Petugas & Audit Trail**: Setiap aksi finansial dan klinis (verifikasi billing, penetapan tanggal keluar pasien, pembatalan finalisasi) harus mencatat identitas personel berupa Nomor Induk / ID Pegawai (`PegId`) yang terdaftar di rumah sakit, bukan identifier teknis sistem/token API. Kolom penampung pada basis data historis memiliki batasan `VARCHAR(10)`.

---

# 3. Scope

## Included
- Penyesuaian domain guard `EnsureNoDownstreamSettlement` pada aggregate `TataRekeningModel`.
- Penyesuaian inspeksi downstream pada `DownstreamSettlementInspector` agar baris proyeksi alokasi internal `ta_trs_billing2` (`RO...`) tidak memblokir pembatalan finalisasi.
- Verifikasi perilaku domain `ReOpen()` yang mengubah status dari `Closed` ke `Opened`.
- Penghapusan tombol `Inisiasi Settlement (Reg Out)` dari `TataRekeningActionPanel.vue` dan `TataRekeningActionToolbar.vue`.
- Pemutakhiran komputasi `nextActionInfo` pada `TataRekeningActionPanel.vue` agar status `Finalized` tidak lagi merekomendasikan aksi `settle`.
- Standardisasi pembuatan token JWT di `UserController.cs` agar claim `sub`, `ClaimTypes.NameIdentifier`, dan `"pegId"` memuat `response.PegId`.
- Penyempurnaan resolusi user di `HttpCurrentUserContext.cs` dan `TataRekeningController.cs` (`ResolveUserId`) untuk memprioritaskan `PegId` dan menolak nilai token subject statis.
- Pengayaan kontrak payload frontend pada `TataRekeningService.ts` untuk menyertakan `userId` dari `authStore.currentUser?.pegId`.
- Penerapan *defensive truncation* pada lapisan DTO persistensi database (`BilrgTataRekeningDto` dan `RegDto`) terhadap kolom `VARCHAR(10)`.
- Pemutakhiran unit test backend (`Bilreg.Test`) dan unit test frontend (`vitest`).

## Excluded
- Modifikasi skema tabel database SQL Server (panjang kolom `PetugasVerif` dan audit `TA_REGISTRASI` tetap dipertahankan `VARCHAR(10)` sesuai skema warisan).
- Perubahan pada proses pembayaran di modul Kasir.
- Perubahan pada formula alokasi proporsional penjamin di `AllocateFinalizeCommand`.

---

# 4. Technical Decisions

### TD-01: Isolasi Downstream Blocker hanya pada Transaksi Kasir Riil
- **Keputusan**: Pemeriksaan downstream barrier pada saat `CancelFinalization` hanya memvalidasi apakah telah terdapat transaksi pembayaran kasir riil (`HasCashierSettlement`). Baris penjamin berkode `RO` di `ta_trs_billing2` tidak lagi dievaluasi sebagai blocker downstream, melainkan diakui sebagai entitas proyeksi alokasi yang memang dibentuk saat Finalisasi dan akan di-revert saat pembatalan.
- **Rasional**: Baris `RO` dibuat oleh `TrsBillingRepo.SaveChanges` saat Finalisasi dan dihapus kembali oleh `ClearFinancialResponsibilityAllocation()`. Memblokir pembatalan karena keberadaan baris `RO` internal adalah *false-positive* yang melanggar SOP-TR-08.
- **Dampak Teknis**: Method `EnsureNoDownstreamSettlement` di `TataRekeningModel.cs` hanya melempar pengecualian jika `HasCashierSettlement(settlementInspector)` bernilai `true`.

### TD-02: Konfirmasi Arsitektur Reopen Langsung ke Status Opened
- **Keputusan**: Perilaku `tataRekening.ReOpen()` yang mentransisikan status dari `Closed` ke `Opened` dipertahankan tanpa perubahan kontrak domain.
- **Rasional**: Selaras dengan SOP-TR-09 di mana Reopen bertujuan melepaskan Billing Lock pada Charge Source (tindakan/obat/kamar).
- **Dampak Teknis**: Handler `ReopenBillingCommand` tetap memanggil `tataRekening.ReOpen()` dan mencatat audit trail `TATA_REKENING_REOPEN_BILLING`.

### TD-03: Penghapusan Elemen Settlement pada UI Tata Rekening
- **Keputusan**: Seluruh elemen interaktif dan panduan yang merujuk pada Inisiasi Settlement dihapus dari antarmuka pengguna Tata Rekening.
- **Rasional**: Menghilangkan redundansi dan ambiguitas bagi verifikator. Setelah Finalisasi, tagihan telah selesai diproses di Tata Rekening dan siap untuk dibayar di Kasir.
- **Dampak Teknis**:
  - `TataRekeningActionPanel.vue`: Hapus tombol `Inisiasi Settlement (Reg Out)`.
  - `TataRekeningActionToolbar.vue`: Hapus tombol `Inisiasi Settlement (Reg Out)`.
  - `TataRekeningActionPanel.vue`: Pada `nextActionInfo`, ketika status `Finalized`, kembalikan informasi "Siap Pembayaran Kasir" tanpa aksi yang dapat diklik (`action: 'none'`).

### TD-04: Standardisasi Claim JWT Berbasis `PegId` User Login
- **Keputusan**: Pada endpoint login (`UserController.Login`), claim `sub` (`JwtRegisteredClaimNames.Sub`) dan `ClaimTypes.NameIdentifier` wajib memuat `response.PegId` (dengan fallback ke `UserLogin` / `UserName` hanya jika `PegId` tidak tersedia, bukan ke konfigurasi statis `"Jwt:Subject"`). Ditambahkan pula custom claim `"pegId"`.
- **Rasional**: Token Subject menurut standar RFC 7519 adalah identitas prinsipal yang diotentikasi. Mengisi `sub` dengan string statis `"BilregApiAccessToken"` menyebabkan seluruh downstream controller salah mengenali subjek token sebagai identitas pengguna.
- **Dampak Teknis**: Token JWT yang diterbitkan kepada pengguna memuat ID Pegawai verifikator yang sah.

### TD-05: Resolusi Identitas Aktor di Web API dengan Prioritas `PegId`
- **Keputusan**: `HttpCurrentUserContext.TryGetUserId()` dan `TataRekeningController.ResolveUserId()` memprioritaskan pengambilan claim `"pegId"`, `ClaimTypes.NameIdentifier`, dan `ClaimTypes.Name`, serta secara eksplisit menolak/mengabaikan string bernilai `"BilregApiAccessToken"`.
- **Rasional**: Mencegah controller menggunakan string default sistem sebagai identitas petugas apabila request tidak menyertakan explicit `userId`.
- **Dampak Teknis**: Nilai petugas yang diteruskan ke `AllocateFinalizeCommand`, `CancelFinalizationCommand`, dan `ReopenBillingCommand` adalah `PegId` staf.

### TD-06: Pengayaan Kontrak Mutasi Frontend Tata Rekening
- **Keputusan**: Pada `TataRekeningService.ts`, mutasi `useFinalizeMutation` dan `useCancelFinalizationMutation` secara proaktif menyertakan `userId: authStore.currentUser?.pegId` (dan `supervisorUserId: authStore.currentUser?.pegId` pada pembatalan finalisasi).
- **Rasional**: *Defense-in-depth*. Menyertakan `userId` secara eksplisit dari store frontend menjamin payload tetap membawa `PegId` bahkan jika terdapat perbedaan interpretasi claim pada middleware API.
- **Dampak Teknis**: Payload request HTTP POST `/api/tatarekening/{regId}/allocate-finalize` dan `/api/tatarekening/{regId}/cancel-finalize` membawa atribut `userId`.

### TD-07: Defensive Data Truncation pada Lapisan DTO Basis Data
- **Keputusan**: Lapisan DTO persistensi database (`BilrgTataRekeningDto.FromModel` dan `RegDto.FromModel`) menerapkan sanitasi pemotongan string aman maksimal 10 karakter (`Truncate(value, 10)`) sebelum objek DTO dikirim ke Dapper DAL.
- **Rasional**: Skema tabel historis `BILRG_TataRekening` (`PetugasVerif`) dan `TA_REGISTRASI` (`FS_KD_PETUGAS_KELUAR`, `FS_KD_PETUGAS_CANCEL_OUT`, `FS_KD_PETUGAS`) dibatasi `VARCHAR(10)`. Pemotongan aman di lapisan DTO menjamin aplikasi tidak akan pernah melempar fatal exception `String or binary data would be truncated` ke pengguna.
- **Dampak Teknis**: Penjaminan ketahanan runtime database terhadap input identitas yang melebihi 10 karakter.

---

# 5. Component Responsibilities

| Komponen | Layer | Tanggung Jawab Teknis |
|---|---|---|
| `UserController` | API / Auth | Menerbitkan token JWT dengan claim `sub`, `NameIdentifier`, dan `pegId` yang bersumber dari `response.PegId`. |
| `HttpCurrentUserContext` | API / Auth | Mengekstrak identitas aktor dari `pegId` / `NameIdentifier` yang bukan token subject statis. |
| `TataRekeningController` | API / Payment | Menyediakan endpoint REST Tata Rekening dan me-resolve `userId` berbasis `PegId` untuk setiap mutasi. |
| `TataRekeningModel` | Domain | Mengeksekusi siklus hidup (`CancelFinalization`, `FinalizeFinancialResponsibility`, `ReOpen`) dengan barrier guard yang terisolasi. |
| `DownstreamSettlementInspector` | Infrastructure | Memeriksa ketiadaan transaksi kasir riil pada `ta_trs_billing2` (`HasCashierSettlement`). |
| `BilrgTataRekeningDto` | Infrastructure | Memetakan model domain ke DTO tabel `BILRG_TataRekening` dengan defensive truncation 10 karakter pada `PetugasVerif`. |
| `RegDto` | Infrastructure | Memetakan model registrasi ke tabel `TA_REGISTRASI` dengan defensive truncation 10 karakter pada kolom petugas keluar & cancel out. |
| `TataRekeningService.ts` | Frontend Service | Mengirimkan payload mutasi finalisasi dan pembatalan dengan menyertakan `userId: authStore.currentUser?.pegId`. |
| `TataRekeningActionPanel.vue` | UI Component | Menampilkan alur kerja 2 langkah, eliminasi tombol settlement, dan pengikatan aksi pengguna. |

---

# 6. Integration Design

```text
[User Login: octopus@email.com]
       │
       │ 1. POST /api/User/login
       ▼
[UserController.Login] ─── calls ───► [UsmanGetUserService]
       │                                     │
       │                                     ▼ returns UserGetUserUsmanResponse
       │                                       (PegId = "PEG-001", UserName = "Octopus")
       │ 2. Generate JWT Token
       │    sub = "PEG-001"
       │    NameIdentifier = "PEG-001"
       │    pegId = "PEG-001"
       ▼
[Frontend: authStore.saveUserData(user)] (user.pegId = "PEG-001")
       │
       │ 3. POST /api/tatarekening/{regId}/allocate-finalize
       │    Payload: { payments, finalizationDate, userId: "PEG-001" }
       ▼
[TataRekeningController.AllocateFinalize]
       │
       │ 4. ResolveUserId(request.UserId) -> "PEG-001"
       ▼
[AllocateFinalizeHandler]
       ├─► TataRekeningModel.FinalizeFinancialResponsibility("PEG-001", ...)
       ├─► BilrgTataRekeningDal.Update (BILRG_TataRekening.PetugasVerif = "PEG-001")
       └─► RegModel.SetKeluar("PEG-001", ...) ──► RegDal.Update (TA_REGISTRASI.FS_KD_PETUGAS_KELUAR = "PEG-001")
```

---

# 7. Verification Strategy

1. **Unit Testing Backend (`Bilreg.Test`)**:
   - `UserControllerTest`: Verifikasi token claim `sub`, `NameIdentifier`, dan `pegId` memuat `PegId` user.
   - `TataRekeningControllerTest`: Verifikasi `ResolveUserId` mengembalikan `PegId` dan mengabaikan subject token placeholder.
   - `BilrgTataRekeningDtoTest`: Verifikasi `FromModel` memotong `PetugasVerif` menjadi maksimal 10 karakter secara aman.
   - `CancelFinalizationSupervisorGovernedTest`: Verifikasi pembatalan finalisasi sukses dan mencatat `PegId` pada log dan registrasi.
2. **Unit Testing Frontend (`c012_myhospital_web`)**:
   - `TataRekeningService.spec.ts`: Verifikasi mutasi `allocateFinalize` dan `cancelFinalization` menyertakan `userId` dari auth store.
   - `TataRekeningActionPanel.spec.ts`: Verifikasi ketiadaan tombol settlement dan alur tombol aktif.
3. **End-to-End Test Execution (`EXEC-TR-BUG-001`)**:
   - Eksekusi `TC-TR-01` (Batal Finalisasi) dan `TC-TR-05` (Siklus Penuh) menggunakan user login `octopus@email.com` tanpa truncation error, dengan bukti audit petugas tersimpan sebagai `PegId`.
