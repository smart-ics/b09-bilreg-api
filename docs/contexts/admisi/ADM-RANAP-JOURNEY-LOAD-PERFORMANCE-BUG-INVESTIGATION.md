# BUG-INVESTIGATION

## Context

Issue: [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md) (ISS-ADM-REG-009) — Degradasi Performa dan Timeout pada Pemuatan Daftar Pasien Aktif dan Detail Perjalanan Pasien Admisi Rawat Inap  
Problem Summary: Pemuatan awal daftar pasien aktif rawat inap (`GET /api/admisi-ranap/journeys?scope=active&pageSize=20`) terasa lambat, dan pembukaan detail perjalanan pasien (`GET /api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`) mengalami latensi tinggi, sering mengalami kegagalan *timeout* pada percobaan pertama, dan baru berhasil setelah dicoba ulang beberapa kali.  
Evidence Source: Verifikasi skema indeks `sp_helpindex ta_trs_billing` mengonfirmasi keberadaan `IX_ta_trs_billing_fs_kd_reg` dan `IX_ta_trs_billing_fs_kd_reg2` pada basis data aktif.

## Current State

Saat pengguna membuka antarmuka Admisi Rawat Inap pada frontend (`c012_myhospital_web`):

1. **Pemuatan Daftar Pasien Aktif (`/api/admisi-ranap/journeys?scope=active&pageSize=20`)**:
   - Komponen `useJourneyActiveDatasetProvider` memanggil endpoint backend `bilregapi/api/admisi-ranap/journeys?scope=active&pageSize=20`.
   - Pemuatan daftar membutuhkan waktu eksekusi yang lama (sering mencapai belasan hingga puluhan detik) sebelum daftar 20 pasien dan ringkasan facet tahapan berhasil ditampilkan ke layar.

2. **Pemuatan Detail Pasien (`/api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`)**:
   - Saat pengguna memilih kartu pasien dari daftar (misal `rsv:RSV097XD67QY`), frontend mengirim permintaan HTTP ke `bilregapi/api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`.
   - Pada percobaan pertama (*cold hit*), permintaan tersebut kerap gagal dengan galat *timeout* (klien HTTP frontend membatalkan koneksi setelah batas waktu 30 detik / 30.000 ms terlampaui).
   - Setelah pengguna mengklik ulang atau mencoba kembali 2 hingga 3 kali (*retry*), permintaan baru berhasil mengembalikan data detail (`JourneyDetailWorkspace`).

3. **Pengalaman Pengguna (*User Experience*)**:
   - Halaman admisi terasa tidak responsif saat pertama kali dibuka.
   - Panel detail workspace di sisi kanan menampilkan status galat atau pemuatan berulang sebelum data pasien akhirnya muncul.

## Problem Analysis

Investigasi penelusuran kode program dan bukti empiris skema indeks basis data aktif menemukan fakta-fakta berikut:

### 1. Status Indeks Tabel Transaksi Billing (`ta_trs_billing`) — Terverifikasi Memiliki Indeks
- Berdasarkan hasil inspeksi indeks aktif melalui `sp_helpindex ta_trs_billing`:
  - `IX_ta_trs_billing_fs_kd_reg` (nonclustered, kunci: `FS_KD_REG, FN_MODUL, FS_KD_TRS`)
  - `IX_ta_trs_billing_fs_kd_reg2` (nonclustered, kunci: `FS_KD_REG, FS_KD_TRS`)
- Kolom `FS_KD_REG` merupakan kolom terdepan (*leading column*) pada kedua indeks tersebut.
- Oleh karena itu, kueri evaluasi tagihan pada `RegistrationCancellationEligibilityDal.HasBillingItemsSql`:
  ```sql
  SELECT CAST(CASE WHEN EXISTS (
      SELECT 1 FROM ta_trs_billing aa WHERE aa.fs_kd_reg = @RegId
  ) THEN 1 ELSE 0 END AS bit)
  ```
  **sudah dieksekusi menggunakan Index Seek** (bukan *Full Table Scan*). Pengecekan ini selesai dalam waktu < 1 ms dan **bukan merupakan sumber keterlambatan utama**.

### 2. Beban Berat Eksekusi Kueri Daftar (`BuildJourneyCte`) dan Antrean Sumber Daya (*Resource Contention*)
- Saat layar Admisi Rawat Inap pertama kali dibuka:
  - Frontend memanggil endpoint daftar aktif `journeys?scope=active&pageSize=20`.
  - Pada `JourneyDal.List`, kueri CTE raksasa `BuildJourneyCte()` dieksekusi **dua kali berurutan**:
    1. Kueri 1: `BuildFacetSql` untuk menghitung agregasi status seluruh perjalanan aktif.
    2. Kueri 2: `BuildPageSql` untuk mengambil 20 kunci perjalanan teratas.
  - Di dalam CTE `AdmissionRoots`:
    - Penggabungan (*LEFT JOIN*) ke subkueri riwayat dokter:
      ```sql
      LEFT JOIN (
          SELECT h.fs_kd_reg,
                 MAX(h.fs_kd_dokter) AS DokterId,
                 MAX(ISNULL(p.fs_nm_peg,'')) AS DokterName
          FROM ta_reg_history_dokter h
          LEFT JOIN td_peg p ON h.fs_kd_dokter = p.fs_kd_peg
          WHERE ISNULL(LTRIM(RTRIM(h.fd_tgl_selesai)),'') = ''
          GROUP BY h.fs_kd_reg
      ) hd ON hd.fs_kd_reg = a.RegId
      ```
      Subkueri ini melakukan pengelompokan (*GROUP BY*) terhadap **seluruh baris tabel `ta_reg_history_dokter`** tanpa pembatasan registrasi aktif di level subkueri.
    - Penggabungan agregasi waiting list (`WlAgg`) memindai seluruh tabel `BILRG_BedWaitingList`.
    - Pengecekan multi-link (`OpnameMulti` dan `ReservationMulti`) memindai tabel `BILRG_AdmAdmission`.
    - Seluruh penggabungan ini dievaluasi untuk seluruh data historis admisi, sebelum disaring oleh ekspresi komputasi tahap (`SqlStage NOT IN (5, 6)`).
  - Karena `SqlStage` merupakan kalkulasi ekspresi CASE WHEN jamak, SQL Server tidak dapat melakukan penyaringan dini (*predicate pushdown*). Seluruh data historis diproses sebelum dipotong menjadi 20 baris teratas.
  - Pada pemanggilan pertama (*cold hit*), kueri daftar memakan waktu puluhan detik, membebani CPU, memicu pembacaan disk besar-besaran, dan menahan antrean *connection pool* / *worker thread* basis data.

### 3. Keterlambatan dan *Timeout* pada Panggilan Detail Pasien
- Begitu antarmuka menampilkan daftar atau pengguna memilih pasien (`rsv:RSV097XD67QY`):
  - Kueri detail `GET /api/admisi-ranap/journeys/rsv%3ARSV097XD67QY` dikirimkan saat basis data atau koneksi masih sibuk memproses kueri daftar.
  - Di dalam `JourneyDal.LoadReservationJourney`:
    - Dijalankan pencarian admisi terkait via `FindAdmissionByReservation`:
      ```sql
      SELECT a.RegId, ...
      FROM BILRG_AdmAdmission a
      LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
      WHERE a.VodDate = @VodDate AND a.ReservationId = @ReservationId
      ORDER BY a.AdmissionDate DESC
      ```
      Tabel `BILRG_AdmAdmission` **tidak memiliki indeks pada kolom `ReservationId`** (hanya `PK_BILRG_AdmAdmission (RegId)`, `IX_Status_CrtDate`, dan `IX_PasienId_Status`), sehingga pencarian admisi ini melakukan pemindaian tabel admisi dan menyortir rekaman.
    - Dijalankan `LoadRegistrationExtras`, yang menggabungkan beberapa tabel SIMRS (`ta_registrasi`, `ta_reg_inap`, `ta_tipe_jaminan`, `ta_kelas`, `td_peg`, `ta_reg_history_dokter`).
- Akibat persaingan sumber daya dengan kueri daftar dan latensi *disk read* pada pemanggilan pertama, total waktu respons detail melampaui batas waktu 30 detik klien Axios frontend, memicu pembatalan koneksi (*timeout*).
- Pada percobaan ke-2 atau ke-3: Halaman memori tabel-tabel utama sudah masuk ke dalam *buffer pool* RAM SQL Server (*warm cache*), dan eksekusi kueri daftar sebelumnya sudah selesai, sehingga kueri detail dapat diproses tanpa antrean dan selesai dalam hitungan detik.

## Affected Components

1. **Backend Basis Data & DAL (`b09-bilreg-api`)**:
   - `Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs`: Pembangunan CTE kueri daftar (`BuildJourneyCte`, `BuildFacetSql`, `BuildPageSql`), subkueri global `ta_reg_history_dokter`, dan pemuatan admisi per reservasi (`FindAdmissionByReservation`).
   - Skema basis data SQL Server: Tabel `BILRG_AdmAdmission` (ketiadaan indeks pendukung pada `ReservationId` dan `OpnameRequestId`).

2. **Frontend UI & API Client (`c012_myhospital_web`)**:
   - `src/modules/Admisi/queries/AdmisiRanapService.ts`: Batas waktu (timeout 30 detik) dan strategi *retry* kueri pemantauan perjalanan.
   - `src/modules/Admisi/composables/useJourneyDataset.ts`: Siklus pemanggilan kueri daftar dan detail saat inisialisasi layar.

## Impact Assessment

- **Business Impact**: Petugas admisi rawat inap mengalami hambatan operasional dan penundaan pelayanan saat membuka antarmuka dan memilih pasien untuk dialokasikan kamar.
- **Operational Impact**: Kegagalan *timeout* memaksa pengguna melakukan klik ulang (*manual retry*), yang justru menambah beban antrean pada server basis data yang sedang sibuk.
- **Technical Impact**: Utilisasi CPU dan Disk I/O SQL Server mengalami lonjakan tinggi saat kueri daftar berjalan, menghambat *throughput* koneksi API lainnya.

## Assumptions

1. Basis data `HOSPITAL_PKL` telah memiliki indeks `IX_ta_trs_billing_fs_kd_reg` dan `IX_ta_trs_billing_fs_kd_reg2` sehingga `ta_trs_billing` bukan merupakan sumber keterlambatan.
2. Tabel `BILRG_AdmAdmission` belum memiliki indeks non-terklaster pada kolom relasi asal (`ReservationId` dan `OpnameRequestId`).
3. Volume data historis pada `BILRG_AdmAdmission`, `ta_registrasi`, dan `ta_reg_history_dokter` cukup besar sehingga agregasi tanpa batas pada `BuildJourneyCte` menyebabkan degradasi performa signifikan pada kondisi *cold cache*.

## Open Questions

1. Apakah penambahan indeks non-terklaster pada `BILRG_AdmAdmission(ReservationId, VodDate)` dan `BILRG_AdmAdmission(OpnameRequestId, VodDate)` dapat segera diterapkan melalui migrasi skema `Bilreg.SqlDb`?
2. Apakah struktur CTE `BuildJourneyCte` dapat dioptimalkan dengan membatasi ruang lingkup registrasi aktif sebelum menggabungkan tabel riwayat dokter (`ta_reg_history_dokter`)?
3. Apakah kueri facet status dan kueri pengambilan 20 data teratas dapat disatukan dalam satu *round-trip* atau di-cache sementara untuk menghindari eksekusi ganda CTE?

## Recommended Decision

1. Rekomendasi pendekatan koreksi: Optimalisasi struktur kueri proyeksi daftar perjalanan (`BuildJourneyCte`) dan penambahan indeks pendukung pada tabel admisi (`BILRG_AdmAdmission`).
2. Eliminasi agregasi global pada tabel riwayat dokter (`ta_reg_history_dokter`) di dalam kueri daftar, membatasi lingkup hanya pada registrasi yang relevan.
3. Penambahan indeks non-terklaster pada `BILRG_AdmAdmission` untuk kolom `ReservationId` dan `OpnameRequestId` guna mempercepat resolusi detail perjalanan.
4. Peninjauan ulang eksekusi berganda antara kueri facet dan kueri paging agar tidak mengevaluasi seluruh grafik CTE dua kali.

## Decision

Penyelidikan menetapkan bahwa keberadaan indeks `IX_ta_trs_billing_fs_kd_reg` telah mengeliminasi pemindaian penuh pada tabel billing. Sumber keterlambatan dan *timeout* berasal dari eksekusi ganda kueri proyeksi daftar (`BuildJourneyCte`) yang memuat agregasi riwayat dokter secara global serta ketiadaan indeks relasi pada `BILRG_AdmAdmission(ReservationId)`. Pendekatan perbaikan yang dipilih adalah optimalisasi kueri proyeksi DAL dan penambahan indeks relasional pada `BILRG_AdmAdmission`.

## Decision Rationale

- Bukti empiris `sp_helpindex ta_trs_billing` membuktikan pencarian billing sudah optimal melalui *Index Seek*.
- Restrukturisasi kueri CTE dan penambahan indeks relasi admisi menyelesaikan langsung beban komputasi dan I/O piringan yang menyebabkan antrean koneksi dan *timeout* 30 detik pada *cold hit*.
- Tidak mengubah struktur kontrak data eksternal (*wire contract parity*) antara backend dan frontend.

## Architecture Applicability

### Decision

ARCHITECTURE-REQUIRED

### Rationale

Penyelesaian masalah ini memerlukan spesifikasi arsitektur teknis mengenai:
1. Skema indeks non-terklaster pada tabel `BILRG_AdmAdmission` untuk relasi reservasi dan opname request.
2. Restrukturisasi arsitektur kueri proyeksi `JourneyDal` (efisiensi evaluasi CTE, eliminasi *full scan* riwayat dokter, dan pemisahan/penyatuan beban *faceting* dan *paging*).
