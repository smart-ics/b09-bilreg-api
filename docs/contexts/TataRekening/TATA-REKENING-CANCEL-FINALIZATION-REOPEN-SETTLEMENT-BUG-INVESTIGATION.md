# BUG-INVESTIGATION

## Context

Issue: [ISSUE-TR-BUG-001 (TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ISSUE.md)](TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-BUG-ISSUE.md)  
Problem Summary: Investigasi teknis terhadap siklus hidup dan antarmuka Tata Rekening, mencakup: (1) Penolakan pembatalan finalisasi akibat false-positive downstream blocker pada alokasi penjamin internal; (2) Konfirmasi perilaku transisi status Reopen dari CLOSED ke OPEN; (3) Redundansi tombol dan panduan Inisiasi Settlement pada antarmuka pengguna; (4) Anomali pencatatan petugas pada Batal Finalisasi yang tersimpan sebagai `BilregApiAccessToken` bukan `PegId` user login; (5) Kegagalan runtime SQL Server truncation pada kolom `PetugasVerif` di tabel `BILRG_TataRekening` saat Finalisasi Billing.

## Current State

Saat ini, siklus hidup penanganan tagihan pasien pada modul Tata Rekening memperlihatkan perilaku berikut:

1. **Batal Finalisasi Terblokir**: Ketika verifikator melakukan finalisasi tagihan dengan penjamin (mis. BPJS/Asuransi), sistem menulis entitas alokasi penjamin ke `ta_trs_billing2` dengan kode transaksi bayar berawalan `RO` (Reg-Out). Saat verifikator mengeksekusi Batal Finalisasi melalui dialog, sistem melempar `InvalidOperationException`:
   > *"Pembatalan status Final ditolak: Alokasi Pembayaran atau Transaksi Kasir telah diproses. Batalkan transaksi kasir/alokasi pembayaran terlebih dahulu sebelum membuka kunci tagihan."*
   Akibatnya, tagihan yang baru difinalisasi di Tata Rekening sama sekali tidak dapat dibatalkan, meskipun belum ada transaksi kasir atau pembayaran hilir.

2. **Perilaku Transisi Reopen**: Pemanggilan perintah Reopen pada tagihan berstatus `CLOSED` langsung memicu `tataRekening.ReOpen()`, yang membersihkan alokasi finansial, mereset verifikasi finansial, dan mengubah status tagihan langsung dari `CLOSED` menjadi `OPEN`. Perilaku ini telah dikonfirmasi selaras dengan `SOP-TR-09`.

3. **Tombol Settlement Masih Tampil**: Pada antarmuka Tata Rekening (`TataRekeningActionPanel.vue` dan `TataRekeningActionToolbar.vue`), tombol aksi "Inisiasi Settlement (Reg Out)" sebelumnya masih dirender dan diaktifkan ketika tagihan berstatus `FINALIZED`. Tombol ini tidak lagi relevan dengan implementasi alur kerja 2-langkah saat ini di mana penetapan Reg Out sudah digabungkan langsung ke dalam aksi Finalisasi Billing.

4. **Anomali Identitas Petugas pada Batal Finalisasi**: Ketika aksi Batal Finalisasi berhasil dipanggil oleh user login (misalnya `octopus@email.com`), field petugas pada audit log, cancellation log, dan `ta_registrasi.fs_kd_petugas_cancel_out` mencatat nilai string `"BilregApiAccessToken"` (atau terpotong menjadi `"BilregApiA"`), bukan `PegId` milik user yang sedang login.

5. **Truncation Error pada Finalisasi Billing (`PetugasVerif`)**: Saat verifikator mengeksekusi aksi Finalisasi Billing (`AllocateFinalize`), proses gagal dengan eksepsi SQL Server:
   > *"String or binary data would be truncated in table 'HOSPITAL_PKL.dbo.BILRG_TataRekening', column 'PetugasVerif'. Truncated value: 'BilregApiA'. The statement has been terminated."*
   Eksepsi ini menggagalkan transaksi finalisasi dan penetapan status keluar pasien. Seharusnya kolom `PetugasVerif` di `BILRG_TataRekening` dan `FS_KD_PETUGAS_KELUAR` di `TA_REGISTRASI` menyimpan `PegId` dari user yang melakukan login.

## Problem Analysis

### 1. Investigasi Defek Batal Finalisasi (False-Positive Downstream Blocker)

- **Mekanisme Terjadinya Bug**:
  Pada proses Finalisasi (`AllocateFinalizeCommand`), sistem mengeksekusi alokasi pertanggungan dan memanggil `TrsBillingRepo.SaveChanges(bill)`. Di dalam `TaTrsBilling2Dto.FromModelFinalization`, baris alokasi penjamin disimpan ke tabel `ta_trs_billing2` dengan nilai `fs_kd_trs_bayar = $"RO{regId[^8..]}"` dan jenis bayar selain `KAS` dan `HUT`.
  
  Ketika verifikator mengeksekusi Batal Finalisasi (`CancelFinalizationCommand`), handler memanggil `tataRekening.CancelFinalization(_settlementInspector, adminTrsIds)`. Di dalam domain aggregate `TataRekeningModel`:
  ```csharp
  private void EnsureNoDownstreamSettlement(IDownstreamSettlementInspector settlementInspector)
  {
      if (HasApprovedAllocation(settlementInspector) || HasCashierSettlement(settlementInspector))
          throw new InvalidOperationException(
              "Pembatalan status Final ditolak: Alokasi Pembayaran atau Transaksi Kasir telah diproses. " +
              "Batalkan transaksi kasir/alokasi pembayaran terlebih dahulu sebelum membuka kunci tagihan.");
  }
  ```
  Di lapisan infrastruktur, `DownstreamSettlementInspector.HasApprovedAllocation(regId)` mengecek keberadaan baris `RO` pada `ta_trs_billing2`.
  
  **Temuan Analisis**: Kueri tersebut salah mengasumsikan bahwa keberadaan baris `RO` di `ta_trs_billing2` adalah bukti alokasi telah diproses oleh modul hilir (downstream), padahal baris tersebut adalah representasi alokasi internal verifikator yang sengaja dibentuk saat Finalisasi. Sesuai `SOP-TR-08 — Cancel Finalization`, pembatalan status final hanya boleh ditolak jika **Payment Settlement telah dilakukan** (ada transaksi kasir riil `HasCashierSettlement`).

### 2. Investigasi Perilaku Reopen Billing (Closed ke Opened)

- **Kesesuaian dengan Dokumen Spesifikasi**:
  Berdasarkan analisis terhadap `SOP-TR-09 — Reopen Billing`:
  - **Tujuan SOP**: Membuka kembali Billing agar subsistem sumber (*Charge Source*) dapat melakukan perubahan, penambahan, atau pembatalan terhadap item biaya operasional (*Financial Charge*).
  - **Precondition**: Billing berstatus `CLOSED`.
  - **Postcondition**: Billing berstatus `OPEN`. Seluruh kunci operasional (*Billing Lock*) dilepaskan.
  
  **Temuan Analisis**: Perilaku sistem yang langsung mengubah status dari `CLOSED` ke `OPEN` pada aksi Reopen adalah **benar secara bisnis dan sepenuhnya selaras dengan SOP-TR-09**.

### 3. Investigasi Redundansi Tombol Inisiasi Settlement pada Antarmuka

- **Evaluasi Alur Kerja Tata Rekening**:
  Pada alur kerja 2-langkah terkini, tahap alokasi, finalisasi, dan penetapan nomor Reg Out (`reg.SetKeluar(...)`) telah disatukan di dalam perintah atomik `AllocateFinalizeCommand`.
  
  **Temuan Analisis**: Tombol `Inisiasi Settlement (Reg Out)` pada antarmuka Tata Rekening bersifat redundan dan harus dihilangkan. Ketika tagihan mencapai status `FINALIZED`, panel aksi mengindikasikan bahwa tagihan siap untuk diselesaikan di kasir, dan opsi tindakan verifikator hanyalah **Batalkan Finalisasi**.

### 4. Investigasi Pencatatan Petugas `BilregApiAccessToken` pada Batal Finalisasi

- **Mekanisme Terjadinya Anomali**:
  1. Pada saat user login melalui endpoint `/api/User/login` (`UserController.cs`), sistem memanggil `UsmanGetUserService` yang menghasilkan `UserGetUserUsmanResponse` berisi field `PegId` (misalnya `"PEG-001"` atau ID pegawai verifikator).
  2. Saat membentuk JWT Token (`UserController.GetToken`), kode menetapkan claim Subject sebagai berikut:
     ```csharp
     new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"] ?? string.Empty)
     ```
     Di mana konfigurasi `appsettings.json` mendefinisikan `"Jwt:Subject": "BilregApiAccessToken"`.
  3. Dalam arsitektur ASP.NET Core JWT Bearer authentication, middleware secara otomatis memetakan claim `JwtRegisteredClaimNames.Sub` (`sub`) ke `ClaimTypes.NameIdentifier`.
  4. Pada sisi frontend (`c012_myhospital_web`), mutation `useCancelFinalizationMutation` memanggil endpoint `/api/tatarekening/{regId}/cancel-finalize` dengan payload `{ reason }` tanpa menyertakan `userId` atau `supervisorUserId`.
  5. Pada `TataRekeningController.cs`, metode `ResolveUserId(request.UserId)` mengevaluasi:
     ```csharp
     User.FindFirst(ClaimTypes.NameIdentifier)?.Value // Bernilai "BilregApiAccessToken"
     ```
  6. Nilai string `"BilregApiAccessToken"` tersebut kemudian diteruskan ke:
     - `CancelFinalizationCommand.UserId`
     - `reg.CancelKeluar(request.UserId, ...)` $\rightarrow$ `TA_REGISTRASI.FS_KD_PETUGAS_CANCEL_OUT`
     - `TataRekeningCancellationLog.UserId` dan `SupervisorUserId`
     - `AuditLog.UserId`
  
  **Temuan Analisis**:
  Claim Subject token JWT seharusnya memuat `PegId` user login, bukan string statis konfigurasi API. Selain itu, `ResolveUserId` dan `HttpCurrentUserContext` harus memprioritaskan claim identitas pegawai (`PegId`) dan menolak nilai fallback statis token akses.

### 5. Investigasi Truncation Error pada Finalisasi Billing (`BILRG_TataRekening.PetugasVerif`)

- **Mekanisme Terjadinya Error**:
  1. Saat pengguna menekan tombol **Finalisasi Billing** pada dialog `FinalizeDialog.vue`, mutasi memanggil endpoint `/api/tatarekening/{regId}/allocate-finalize`.
  2. Frontend hanya mengirimkan payload `{ payments, finalizationDate }` tanpa parameter `userId`.
  3. Controller memanggil `ResolveUserId(request.UserId)` yang menghasilkan string 20 karakter: `"BilregApiAccessToken"`.
  4. Handler `AllocateFinalizeCommand` mengeksekusi:
     ```csharp
     tataRekening.FinalizeFinancialResponsibility(petugasVerif, occurredAt, adminTrsIds);
     _tataRekeningRepo.SaveChanges(tataRekening);
     ```
  5. Pada lapisan infrastruktur `BilrgTataRekeningDto.FromModel(model)`:
     ```csharp
     model.FinalizationInfo.PetugasVerif // Bernilai "BilregApiAccessToken"
     ```
  6. Ketika Dapper mengeksekusi `BilrgTataRekeningDal.Update` atau `Insert`:
     ```sql
     UPDATE BILRG_TataRekening SET PetugasVerif = @PetugasVerif ...
     ```
     Kolom `BILRG_TataRekening.PetugasVerif` di SQL Server bertipe `VARCHAR(10)`. Karena string `"BilregApiAccessToken"` berukuran 20 karakter, SQL Server menghentikan transaksi dengan error:
     `"String or binary data would be truncated in table 'HOSPITAL_PKL.dbo.BILRG_TataRekening', column 'PetugasVerif'. Truncated value: 'BilregApiA'. The statement has been terminated."`
  7. Selain tabel `BILRG_TataRekening`, perintah `reg.SetKeluar(petugasVerif, occurredAt)` juga menuliskan petugas ke kolom `TA_REGISTRASI.FS_KD_PETUGAS_KELUAR` yang juga bertipe `VARCHAR(10)`.
  
  **Temuan Analisis**:
  Sesuai kebutuhan bisnis dan integritas data rumah sakit, petugas yang melakukan finalisasi billing maupun petugas keluar pasien harus merupakan **`PegId` dari user yang sedang login** (yang berukuran ringkas $\le 10$ karakter). Kegagalan ini disebabkan oleh kombinasi tidak dikirimkannya `PegId` dari sesi login, claim JWT yang salah memuat string token subject, ketiadaan sanitasi defensive truncation pada `BilrgTataRekeningDto`, dan ketergantungan controller pada fallback subject token.

## Affected Components

- **Backend Authentication & Context**:
  - `Bilreg.Api/Controllers/UserController.cs`: Pembuatan claim token JWT (`sub`, `ClaimTypes.NameIdentifier`, dan custom claim `pegId` harus menggunakan `response.PegId`).
  - `Bilreg.Api/Authorization/HttpCurrentUserContext.cs`: Ekstraksi `ActorUserId` harus memprioritaskan claim `pegId` dan `ClaimTypes.NameIdentifier` yang bukan token subject.
  - `Bilreg.Api/Controllers/PaymentContext/TataRekeningFeature/TataRekeningController.cs`: Logika `ResolveUserId` harus mengekstrak `PegId` user yang terautentikasi dan mengabaikan nilai placeholder.
- **Backend Domain, Application, & Infrastructure**:
  - `Bilreg.Application/PaymentContext/TataRekeningFeature/UseCases/AllocateFinalizeCommand.cs`: Penerimaan dan propagasi `petugasVerif` (`PegId`) ke domain `tataRekening` dan `reg.SetKeluar`.
  - `Bilreg.Application/PaymentContext/AlokasiPembayaranFeature/CancelFinalizationCommand.cs`: Penerimaan dan propagasi `userId` dan `supervisorUserId` (`PegId`) ke domain `reg.CancelKeluar`, cancellation log, dan audit log.
  - `Bilreg.Infrastructure/PaymentContext/TataRekeningFeature/BilrgTataRekeningDto.cs`: Penambahan defensive sanitasi `Truncate(..., 10)` pada `PetugasVerif` dan `FinVerifPetugas` untuk mencegah fatal database exception.
  - `Bilreg.Application/AdmisiContext/RegFeature/RegDto.cs`: Pemastian `Truncate(..., 10)` pada `fs_kd_petugas_keluar` dan `fs_kd_petugas_cancel_out`.
- **Frontend Tata Rekening**:
  - `src/modules/TataRekening/services/TataRekeningService.ts`: Mutasi `useFinalizeMutation` dan `useCancelFinalizationMutation` menyertakan `userId` secara eksplisit dari store user login (`authStore.currentUser?.pegId`).
  - `src/modules/TataRekening/components/actions/FinalizeDialog.vue` & `CancelFinalizationDialog.vue`: Pengikatan data `pegId` user login ke payload aksi.
- **Database Tables**:
  - `HOSPITAL_PKL.dbo.BILRG_TataRekening`: Kolom `PetugasVerif` (`VARCHAR(10)`).
  - `HOSPITAL_PKL.dbo.TA_REGISTRASI`: Kolom `FS_KD_PETUGAS_KELUAR` (`VARCHAR(10)`) dan `FS_KD_PETUGAS_CANCEL_OUT` (`VARCHAR(10)`).
  - `HOSPITAL_PKL.dbo.BILRG_TataRekeningCancellationLog`: Kolom `UserId` dan `SupervisorUserId`.

## Impact Assessment

- **Business Impact**: Memastikan seluruh riwayat verifikasi finansial, penetapan status keluar pasien, dan pembatalan finalisasi tercatat atas nama ID Pegawai (`PegId`) verifikator yang sah dan dapat diaudit secara hukum di rumah sakit.
- **Operational Impact**: Menghilangkan kegagalan operasional saat staf verifikator menekan tombol Finalisasi Billing, sehingga tagihan dapat difinalisasi dengan lancar tanpa error database.
- **Technical Impact**: Mengeliminasi SQL Server data truncation error pada tabel `BILRG_TataRekening` dan `TA_REGISTRASI`. Menyelaraskan token authentication claims antara backend Usman, Bilreg API, dan antarmuka web klien.

## Assumptions

1. Layanan Usman API (`IUsmanGetUserService`) selalu mengembalikan field `pegId` yang valid (ID pegawai dengan panjang $\le 10$ karakter) saat user berhasil login.
2. Store autentikasi pada frontend (`useAuthStore`) menyimpan objek `currentUser` yang memiliki properti `pegId`.
3. Kolom database `PetugasVerif` pada `BILRG_TataRekening` dan `FS_KD_PETUGAS_KELUAR` / `FS_KD_PETUGAS_CANCEL_OUT` pada `TA_REGISTRASI` memiliki panjang tepat `VARCHAR(10)`.

## Open Questions

- *Tidak ada open questions yang memblokir.* Definisi format dan sumber `PegId` telah terkonfirmasi jelas dari Usman dan skema tabel database.

## Recommended Decision

1. **Standardisasi Pembuatan Token JWT Backend (`UserController`)**:
   - Claim `JwtRegisteredClaimNames.Sub` dan `ClaimTypes.NameIdentifier` wajib diisi dengan `response.PegId` (fallback ke `UserLogin` / `UserName` jika PegId kosong, bukan ke konfigurasi `"Jwt:Subject"`).
   - Tambahkan custom claim `"pegId"` dengan nilai `response.PegId`.
2. **Koreksi Resolusi Identitas di Controller & CurrentUserContext**:
   - `TataRekeningController.ResolveUserId` dan `HttpCurrentUserContext.TryGetUserId` harus memprioritaskan claim `"pegId"`, `ClaimTypes.NameIdentifier`, dan `ClaimTypes.Name`, serta secara eksplisit menolak/mengabaikan string bernilai `"BilregApiAccessToken"`.
3. **Pengiriman Eksplisit `userId` dari Frontend Tata Rekening**:
   - Pada `TataRekeningService.ts`, mutasi `allocateFinalize` dan `cancelFinalization` secara proaktif menyertakan `userId: authStore.currentUser?.pegId` (dan `supervisorUserId: authStore.currentUser?.pegId`) pada payload request ke backend.
4. **Defensive Truncation pada Lapisan DTO Database**:
   - Terapkan sanitasi `Truncate(..., 10)` pada `BilrgTataRekeningDto.FromModel` untuk kolom `PetugasVerif` dan `FinVerifPetugas`, serta pastikan `RegDto.FromModel` menerapkan hal yang sama pada seluruh audit user ID.

## Decision

1. Menetapkan `PegId` dari user yang melakukan login sebagai identitas tunggal yang sah dan wajib disimpan ke seluruh field audit verifikator dan petugas keluar (`BILRG_TataRekening.PetugasVerif`, `TA_REGISTRASI.FS_KD_PETUGAS_KELUAR`, `TA_REGISTRASI.FS_KD_PETUGAS_CANCEL_OUT`, serta cancellation log).
2. Memperbaiki pembuatan claim JWT pada `UserController` agar claim subject (`sub`) dan `NameIdentifier` memuat `PegId` user login.
3. Memperbaiki resolusi `ResolveUserId` pada `TataRekeningController` dan `HttpCurrentUserContext` untuk mengekstrak `PegId` dan mengeliminasi fallback token subject statis.
4. Memperbarui kontrak mutasi frontend pada `TataRekeningService.ts` untuk menyertakan `userId` dari `currentUser.pegId`.
5. Menerapkan defensive truncation maksimal 10 karakter pada pemetaan `BilrgTataRekeningDto` guna menjamin keamanan integritas SQL Server.

## Decision Rationale

- Standar audit medis dan finansial rumah sakit mewajibkan pencatatan identitas petugas berupa ID Pegawai (`PegId`), bukan token subject teknis sistem.
- Perbaikan di kedua sisi (token claims di backend dan payload eksplisit dari frontend) memberikan mekanisme pertahanan ganda (*defense-in-depth*) sehingga transaksi tetap memiliki identitas petugas yang valid baik saat token di-decode maupun saat payload dibaca langsung.
- Penerapan defensive truncation pada DTO database mencegah crash sistem apabila terjadi variasi data dari sistem autentikasi eksternal.

## Architecture Applicability

### Decision

**ARCHITECTURE-REQUIRED**

### Rationale

Perbaikan ini melibatkan penataan ulang propagasi identitas autentikasi lintas batas konteks (integrasi login Usman, claim token JWT, konteks `HttpCurrentUserContext`, kontrak payload API Tata Rekening, dan pemetaan DTO persistensi database `BILRG_TataRekening` serta `TA_REGISTRASI`). Oleh karena itu, diperlukan tinjauan dan penetapan keputusan arsitektur formal sebelum implementasi dieksekusi.
