# BUG-INVESTIGATION

## Context

Issue: [ADM-RANAP-VOID-REGISTRASI-DOKTER-HISTORY-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-VOID-REGISTRASI-DOKTER-HISTORY-BUG-ISSUE.md) (`ISSUE-ADM-RANAP-VOID-REG-DOKTER-001`)  
Problem Summary: Saat pengguna melakukan pembatalan admisi / void registrasi rawat inap untuk pasien yang belum memiliki transaksi tagihan billing (spesimen `RGA79QTXGI`), sistem memunculkan toast kegagalan generik HTTP 409 (*"Perjalanan berubah sejak terakhir dimuat..."*). Kegagalan ini bersumber dari dua anomali sistem yang saling terikat:
1. **Terhapusnya data dokter di basis data**: Tabel `ta_reg_history_dokter` terhapus tanpa sengaja oleh `RegRepo.SaveChanges` saat alur kelola jaminan/SEP dijalankan.
2. **Invarian pembatalan terkoordinasi yang *over-restrictive***: Logika pembatalan admisi terkoordinasi menolak eksekusi jika tidak ada riwayat dokter aktif (`ActiveDoctorAssignments < 1` dan `EndDoctorAssignments < 1`), padahal dari sudut pandang bisnis rumah sakit, pembatalan registrasi murni hanya dibatasi oleh keberadaan transaksi tagihan di Tata Rekening (`ta_trs_billing`).

## Current State

1. **Kasus Spesimen Riil di Lingkungan Pengujian (`HOSPITAL_PKL`)**:
   - Pasien: HERI KUSPRATIWI, NY (No. RM: `00-17-31-21`, No. Reg: `RGA79QTXGI`).
   - Penjamin: JKN NON PBI (memiliki data eligibilitas SEP).
   - Sumber Awal: Reservasi `RSV0A1I35R3W` yang direalisasikan pada 06 Juni 2024 09:29:12.
   - Status Perjalanan Saat Ini: "Menunggu penerimaan bangsal" (*Stage 2: `WardAcceptanceRequired`*), dengan tugas bangsal ST.ANNA: "Terima handover akomodasi".
2. **Kondisi Basis Data**:
   - `ta_registrasi`: Aktif, `fs_kd_jenis_reg = '1'`, `fs_kd_medis = '10127'`, `fd_tgl_void = '3000-01-01'`, `fd_tgl_keluar = '3000-01-01'`.
   - `BILRG_AdmAdmission`: Status `0` (*Admitted*), sumber Reservasi `RSV0A1I35R3W`.
   - `ta_reg_inap`: Terdaftar dengan cara masuk `101`.
   - `BILRG_RegAktif`: Terdaftar `1` baris aktif.
   - `BILRG_BedWaitingList`: Terdaftar antrean `WTL0A1I3RYQ9` berstatus `0` (*Waiting*).
   - `BILRG_AdmReservation`: Berstatus `2` (*Realized*) dengan `RealizedRegId = 'RGA79QTXGI'`.
   - `ta_trs_billing`: **`0` item tagihan** (pasien belum pernah menerima tindakan/transaksi billing Tata Rekening).
   - ❌ `ta_reg_history_dokter`: **`0` baris (KOSONG)** untuk nomor registrasi `RGA79QTXGI`.
3. **Perilaku Eksekusi Saat Pembatalan Dijalankan**:
   - Pengguna menekan tombol "Batalkan Registrasi" pada antarmuka admisi dan memasukkan alasan pembatalan *"batal berobat"*.
   - Frontend mengirimkan mutasi `POST /api/admisi-ranap/admission/RGA79QTXGI/cancel`.
   - Backend memproses via `AdmCoordinatedCancelHandler`. Saat melakukan penguncian dan pembacaan state via `CoordinatedCancellationRepo.LockState("RGA79QTXGI")`, kueri `SELECT 1 FROM ta_reg_history_dokter ... WHERE ISNULL(LTRIM(RTRIM(fd_tgl_selesai)),'')=''` menghasilkan hitungan `0` (`ActiveDoctorAssignments = 0`).
   - Pada metode `ValidateState`, pemeriksaan `if (state.RegAktifCount != 1 || state.ActiveDoctorAssignments < 1)` mengevaluasi `true` dan melempar `CoordinatedCancellationException(CC_STATE_INCONSISTENT, "State pembatalan terkoordinasi tidak konsisten.")`.
   - `AdmissionController` menangkap exception tersebut dan mengembalikannya dengan HTTP status **`409 Conflict`**.
   - Frontend `useJourneyActionHost.ts` menangani status 409 secara statis dan memunculkan toast: *"Perjalanan berubah sejak terakhir dimuat. Data dimuat ulang — tinjau ulang sebelum melanjutkan."*, menutupi alasan sebenarnya kepada pengguna.

## Problem Analysis

Investigasi mendalam terhadap kode sumber dan basis data menemukan tiga akar penyebab teknis:

### 1. Titik Kebocoran Penghapusan Data Dokter: `RegRepo.SaveChanges` Menghapus Riwayat Dokter Rawat Inap
- Pada berkas `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegRepo.cs` (baris 25):
  ```csharp
  public void SaveChanges(RegModel model)
  {
      LoadEntity(model)
          .Match(
              onSome: _ => _regDal.Update(RegDto.FromModel(model)),
              onNone: () => _regDal.Insert(RegDto.FromModel(model))
          );

      _regJaminanDal.Delete(model);
      _regJaminanDal.Insert(RegJaminanDto.FromModel(model));
      ...
      _regHistoryDokterDal.Delete(model); // <-- BUG: Selalu menghapus ta_reg_history_dokter!
  }
  ```
- **Mekanisme Terjadinya Bug**:
  - Pada arsitektur domain, `RegModel` **tidak memiliki** koleksi `ListDokter`. Koleksi dokter rawat inap dikelola oleh `RegInapModel` dan disimpan melalui `RegInapRepo`.
  - Di `RegRepo.cs`, pemanggilan `_regHistoryDokterDal.Delete(model)` sengaja ditempatkan di baris akhir tanpa pernah memasukkan kembali data dokter.
  - Setiap kali ada alur kerja yang memanggil `_regRepo.SaveChanges(reg)`—termasuk alur penyimpanan eligibilitas SEP pada [RegSetDataEligibilityCmd.cs#L22](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegSetDataEligibilityCmd.cs#L22) (`_regRepo.SaveChanges(reg)`):
    Tabel `ta_reg_history_dokter` untuk registrasi rawat inap tersebut **terhapus bersih secara diam-diam**.
  - Inilah jawaban pasti mengapa pasien `RGA79QTXGI` (dan pasien lain yang menjalani pengisian eligibilitas SEP JKN) kehilangan baris `ta_reg_history_dokter`.

### 2. Validasi Pembatalan Terkoordinasi yang Menolak Eksekusi Tanpa Dokter
- Pada berkas `src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs`:
  - **Titik Hambat 1** ([AdmCoordinatedCancelCmd.cs#L121](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs#L121)):
    ```csharp
    if (state.RegAktifCount != 1 || state.ActiveDoctorAssignments < 1)
        throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.StateInconsistent, "State pembatalan terkoordinasi tidak konsisten.");
    ```
    Memeriksa bahwa jumlah dokter aktif harus `>= 1`. Jika `0`, langsung melempar `CC_STATE_INCONSISTENT`.
  - **Titik Hambat 2** ([AdmCoordinatedCancelCmd.cs#L88](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs#L88)):
    ```csharp
    if (_repo.EndDoctorAssignments(normalized.RegId, DateOnly.FromDateTime(occurredAt)) < 1)
        throw new CoordinatedCancellationException(CoordinatedCancellationErrorCode.ConcurrencyConflict, "Penugasan dokter telah berubah.");
    ```
    Memeriksa bahwa baris yang ter-update saat mengakhiri penugasan dokter harus `>= 1`. Jika awalnya memang tidak ada penugasan dokter, update menghasilkan 0 rows dan melempar `CC_CONCURRENCY_CONFLICT`.
- **Kesenjangan Terhadap Aturan Bisnis**:
  - Sesuai aturan bisnis rumah sakit yang ditegaskan, pembatalan admisi / void registrasi murni hanya dibatasi oleh **apakah pasien sudah memiliki tagihan di Tata Rekening (`ta_trs_billing`) atau belum**.
  - Selama `ta_trs_billing` belum ada transaksi, ketiadaan rekaman di tabel penunjang `ta_reg_history_dokter` tidak boleh memblokir pembatalan registrasi. Jika baris penugasan dokter tidak ada, pengakhiran penugasan dokter seharusnya bersifat *no-op* yang aman.

### 3. Masking Pesan Error pada Penanganan Mutasi Frontend (*Frontend Error Masking*)
- Pada berkas `src/modules/Admisi/composables/useJourneyActionHost.ts` (baris 90–96):
  ```typescript
  if (err instanceof ApiError && err.status === 409) {
    toast.error(
      'Perjalanan berubah sejak terakhir dimuat. Data dimuat ulang — tinjau ulang sebelum melanjutkan.',
    )
    void refetchSelectedJourney()
    return
  }
  ```
- Frontend mengasumsikan bahwa semua HTTP 409 adalah *stale concurrency conflict*.
- Akibatnya, pesan penolakan bisnis/invarian riil dari backend (`err.data?.message` atau `err.message`) tertutup dan mengarahkan pengguna pada asumsi keliru bahwa data hanya perlu dimuat ulang (*refresh*), padahal eksekusi ulang tetap akan gagal.

## Affected Components

1. **Backend Core Persistence & Coordination (`b09-bilreg-api`)**:
   - [`RegRepo.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegRepo.cs):
     Hapus `_regHistoryDokterDal.Delete(model);` untuk registrasi rawat inap (`JenisReg == JenisRegEnum.RegInap`). Riwayat dokter rawat inap sepenuhnya dimiliki dan dikelola oleh `RegInapRepo`.
   - [`AdmCoordinatedCancelCmd.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs):
     - Pada `ValidateState`: Hapus syarat wajib `state.ActiveDoctorAssignments < 1`. Ketiadaan dokter aktif tidak boleh menggagalkan pembatalan jika registrasi dan admisi valid.
     - Pada eksekusi mutasi: Ubah pemeriksaan `EndDoctorAssignments` agar toleran jika baris yang ter-update bernilai 0 (`if (state.ActiveDoctorAssignments > 0 && affected < 1) ...`).
   - [`CoordinatedCancellationRepo.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationRepo.cs):
     Pastikan `IsCoherentlyCancelled` mengevaluasi konsistensi dengan tepat baik saat dokter pernah ada maupun tidak ada.
   - Test Suite:
     - `RegRepoTest.cs`: Verifikasi bahwa `RegRepo.SaveChanges` untuk rawat inap tidak memicu delete pada `IRegHistoryDokterDal`.
     - `AdmCoordinatedCancelHandlerTest.cs`: Uji coba skenario pembatalan admisi dengan `ActiveDoctorAssignments == 0` dan billing kosong berhasil commit dengan sukses.
2. **Frontend UI Module (`c012_myhospital_web`)**:
   - [`useJourneyActionHost.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts):
     Perbaiki `handleJourneyMutationError` agar memprioritaskan penampilan pesan teks spesifik dari payload respons server (`err.data?.message` / `err.message`).

## Impact Assessment

- **Business Impact**:
  - Penegasan aturan bisnis terpenuhi: Ketiadaan transaksi billing Tata Rekening (`ta_trs_billing == 0`) menjadi satu-satunya gerbang pembatas pembatalan registrasi.
  - Alur kelola jaminan / SEP tidak lagi merusak data riwayat dokter yang sudah ada di rawat inap.
  - Operasional admisi dapat melakukan void registrasi secara mandiri tanpa memerlukan campur tangan teknis perbaikan database.
- **Operational Impact**:
  - Petugas loket pendaftaran rawat inap mendapatkan pesan error yang jelas dan akurat jika pembatalan memang ditolak (misalnya jika tagihan billing sudah terbentuk).
- **Technical Impact**:
  - Tidak ada perubahan skema DDL basis data.
  - Menghilangkan *side effect* destruktif antar repositori domain (`RegRepo` vs `RegInapRepo`).

## Assumptions

- **ASM-01**: Pengecekan `ta_trs_billing` (`IRegistrationCancellationEligibilityRepo.HasBillingItems`) adalah satu-satunya gerbang ketergantungan hilir (*downstream business eligibility blocker*) yang sah untuk membatalkan registrasi.
- **ASM-02**: Untuk registrasi Rawat Inap (`JenisRegEnum.RegInap`), entitas `RegModel` tidak mengelola siklus hidup riwayat dokter; kepemilikan mutlak penugasan dokter rawat inap berada pada `RegInapModel` via `RegInapRepo`.
- **ASM-03**: Ketiadaan baris di `ta_reg_history_dokter` tidak boleh menghalangi proses void pada `ta_registrasi`, `BILRG_AdmAdmission`, `BILRG_RegAktif`, maupun pemulihan sumber (`BILRG_AdmReservation`/`OpnameRequest`).
- **ASM-04**: Jika terdapat penugasan dokter aktif di `ta_reg_history_dokter`, sistem tetap wajib menutupnya via `EndDoctorAssignments`. Jika tidak ada, proses dilewati tanpa error.

## Open Questions & Gap Closures

- **OQ-01 (Kapan dan mengapa `ta_reg_history_dokter` terhapus?)**:
  - *Keputusan*: **CLOSED**. Terbukti secara faktual di kode [RegRepo.cs#L25](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegRepo.cs#L25) bahwa `_regHistoryDokterDal.Delete(model)` dipanggil tanpa syarat setiap kali `RegRepo.SaveChanges` dipanggil. Pada registrasi yang melalui alur kelola jaminan/SEP (`RegSetDataEligibilityCmd`), pemanggilan ini menghapus data dokter rawat inap yang sebelumnya sudah disimpan oleh `RegInapRepo`.
- **OQ-02 (Apakah ketiadaan dokter aktif boleh menggagalkan pembatalan registrasi?)**:
  - *Keputusan*: **CLOSED (TIDAK BOLEH)**. Aturan bisnis menetapkan bahwa selama pasien belum memiliki billing di `ta_trs_billing`, registrasi dapat dibatalkan. Ketiadaan baris di `ta_reg_history_dokter` tidak boleh menjadi penghalang.
- **OQ-03 (Bagaimana penanganan update dokter di `EndDoctorAssignments` jika baris dokter = 0?)**:
  - *Keputusan*: **CLOSED**. Pemeriksaan `if (_repo.EndDoctorAssignments(...) < 1)` diubah agar hanya dijalankan atau diverifikasi jika `state.ActiveDoctorAssignments > 0`. Jika `state.ActiveDoctorAssignments == 0`, langkah ini dilewati dengan aman (*safe bypass*).
- **OQ-04 (Penyajian Error Toast di Frontend)**:
  - *Keputusan*: **CLOSED**. Frontend `useJourneyActionHost.ts` harus mengecek apakah error 409 memiliki payload pesan spesifik dari backend (`err.data?.message` / `err.message`) sebelum menampilkan pesan default bentrokan versi.

## Alternative Evaluation

### Alternatif 1: Mempertahankan Invarian Ketat dan Mengharuskan Insert Dokter Manual
- **Deskripsi**: Menolak perubahan logika pembatalan; mewajibkan setiap registrasi yang kehilangan data dokter diperbaiki manual lewat database sebelum bisa dibatalkan.
- **Kelemahan**: Mengabaikan *root cause* utama bahwa data terhapus oleh `RegRepo.SaveChanges`, membebani tim IT dengan intervensi manual, dan bertentangan dengan kebijakan bisnis bahwa pembatalan murni bergantung pada ada/tidaknya billing.
- **Keputusan**: **DITOLAK**.

### Alternatif 2: Perbaikan Penuh pada Titik Hapus dan Relaksasi Validasi Pembatalan (Pilihan Terpilih)
- **Deskripsi**:
  1. Hentikan penghapusan `_regHistoryDokterDal.Delete` di `RegRepo.cs` khusus untuk `JenisReg == JenisRegEnum.RegInap`.
  2. Hapus batasan `ActiveDoctorAssignments < 1` pada `ValidateState` di `AdmCoordinatedCancelCmd.cs`.
  3. Buat penutupan dokter di `EndDoctorAssignments` aman ketika penugasan dokter aktif berjumlah 0.
  4. Perbaiki penyajian error pada frontend agar transparan.
- **Keunggulan**: Menyelesaikan masalah di hulu (mencegah data dokter terhapus saat update jaminan/SEP) sekaligus di hilir (memastikan void registrasi tanpa billing tidak pernah terblokir oleh anomali dokter), tanpa merusak integritas data lainnya.
- **Keputusan**: **DISETUJUI**.

## Recommended Decision

Menerapkan perbaikan komprehensif sesuai **Alternatif 2**:
1. Mengamankan `RegRepo.cs` agar tidak menghapus riwayat dokter rawat inap.
2. Menghapus syarat `ActiveDoctorAssignments < 1` dari `ValidateState` di `AdmCoordinatedCancelCmd.cs`.
3. Menyesuaikan pemanggilan `EndDoctorAssignments` agar tidak melempar exception bila baris dokter aktif memang tidak ada.
4. Memperbaiki `handleJourneyMutationError` di `useJourneyActionHost.ts` untuk menampilkan pesan server yang informatif.

## Decision

Menyetujui secara definitif hasil analisis investigasi cacat pembatalan admisi dan penghapusan riwayat dokter:
1. **Pencegahan Kehilangan Data Dokter**: `RegRepo.cs` dilarang menghapus `ta_reg_history_dokter` untuk transaksi rawat inap.
2. **Kepatuhan Aturan Bisnis Void**: Pembatalan registrasi rawat inap hanya dibatasi oleh keberadaan transaksi tagihan di Tata Rekening (`ta_trs_billing == 0`). Ketiadaan baris dokter aktif tidak boleh menggagalkan pembatalan.
3. **Eksekusi Penutupan Dokter Non-Blocking**: Jika `ActiveDoctorAssignments == 0`, pengakhiran dokter dilewati secara aman tanpa melempar `CC_CONCURRENCY_CONFLICT`.
4. **Transparansi UI Frontend**: Frontend menampilkan pesan kesalahan yang dikirimkan oleh backend saat pembatalan gagal.

## Decision Rationale

1. **Akurasi Akar Masalah**: Mengidentifikasi titik pasti hilangnya data di `RegRepo.cs` saat penyimpanan eligibilitas SEP, sehingga menghentikan terulangnya insiden pada registrasi pasien baru lainnya.
2. **Kepatuhan Kebijakan Rumah Sakit**: Memastikan aturan bisnis "tanpa billing maka registrasi dapat dibatalkan" terwujud secara konsisten di tingkat implementasi teknis.
3. **Pemberantasan Titik Buntu Operasional (*Operational Deadlock*)**: Menjamin bahwa registrasi yang memiliki inkonsistensi historis dokter tetap dapat dibatalkan secara bersih dan terkoordinasi (termasuk memulihkan reservasi/opname request terkait).

## Architecture Applicability

### Decision

**ARCHITECTURE-NOT-REQUIRED**

### Rationale

Perbaikan cacat ini memenuhi kriteria `ARCHITECTURE-NOT-REQUIRED` karena:
- Tidak ada introduksi komponen, dependensi arsitektur, atau protokol komunikasi baru.
- Tidak ada perubahan batas domain maupun kontrak wire format API (endpoint `POST /api/admisi-ranap/admission/{regId}/cancel` dan skema DTO tetap sama).
- Tidak ada perubahan skema persistensi basis data (DDL) maupun redistribusi kepemilikan agregat.
- Pola arsitektur Coordinated Cancellation dan Repositori yang telah ada tetap dipertahankan; perbaikan hanya mencakup pengamanan pemanggilan DAL di `RegRepo` dan penyesuaian guard internal di `AdmCoordinatedCancelCmd` serta penanganan error di frontend.

Tahapan SDLC dapat langsung melangkah ke penyusunan **IMPLEMENTATION-PLAN** oleh peran yang berwenang.
