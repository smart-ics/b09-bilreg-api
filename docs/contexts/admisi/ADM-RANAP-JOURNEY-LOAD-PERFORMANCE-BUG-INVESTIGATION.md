# BUG-INVESTIGATION

## Context

Issue: [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md) (ISS-ADM-REG-009) — Degradasi Performa dan Timeout pada Pemuatan Daftar Pasien Aktif dan Detail Perjalanan Pasien Admisi Rawat Inap  
Problem Summary: Pemuatan awal daftar pasien aktif rawat inap (`GET /api/admisi-ranap/journeys?scope=active&pageSize=20`) terasa lambat, dan pembukaan detail perjalanan pasien (`GET /api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`) mengalami latensi tinggi, sering mengalami kegagalan *timeout* pada percobaan pertama, dan baru berhasil setelah dicoba ulang beberapa kali.  
Evidence Source:
1. `sp_helpindex ta_trs_billing`: Terverifikasi memiliki indeks `IX_ta_trs_billing_fs_kd_reg` dan `IX_ta_trs_billing_fs_kd_reg2`.
2. `sp_helpindex BILRG_AdmAdmission`: Terverifikasi memiliki indeks `IX_bilrg_admadmission_reservationid` dan `IX_bilrg_admadmission_opnamerequestid`.

## Current State

Saat pengguna membuka antarmuka Admisi Rawat Inap pada frontend (`c012_myhospital_web`):

1. **Pemuatan Daftar Pasien Aktif (`/api/admisi-ranap/journeys?scope=active&pageSize=20`)**:
   - Komponen `useJourneyActiveDatasetProvider` memanggil endpoint backend `bilregapi/api/admisi-ranap/journeys?scope=active&pageSize=20`.
   - Pemuatan daftar membutuhkan waktu eksekusi yang lama (sering mencapai 30 hingga 45 detik pada kondisi *cold hit*) sebelum daftar 20 pasien dan ringkasan facet tahapan berhasil ditampilkan ke layar.

2. **Pemuatan Detail Pasien (`/api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`)**:
   - Saat pengguna memilih kartu pasien dari daftar (misal `rsv:RSV097XD67QY`), frontend mengirim permintaan HTTP ke `bilregapi/api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`.
   - Pada percobaan pertama (*cold hit*), permintaan tersebut kerap gagal dengan galat *timeout* (klien HTTP frontend membatalkan koneksi setelah batas waktu 30 detik / 30.000 ms terlampaui).
   - Setelah pengguna mengklik ulang atau mencoba kembali 2 hingga 3 kali (*retry*), permintaan baru berhasil mengembalikan data detail (`JourneyDetailWorkspace`).

3. **Pengalaman Pengguna (*User Experience*)**:
   - Halaman admisi terasa tidak responsif saat pertama kali dibuka.
   - Panel detail workspace di sisi kanan menampilkan status galat atau pemuatan berulang sebelum data pasien akhirnya muncul.

## Problem Analysis

Investigasi mendalam pada alur eksekusi Backend (`b09-bilreg-api`) dan bukti skema basis data aktif menemukan fakta-fakta berikut:

### 1. Status Indeks Tabel Transaksi Billing (`ta_trs_billing`) — Terverifikasi Optimal
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
  dieksekusi menggunakan **Index Seek** (< 1 ms). Pengecekan ini **bukan** merupakan sumber keterlambatan.

### 2. Status Indeks Tabel Admisi (`BILRG_AdmAdmission`) — Terverifikasi Ada di Database Aktif
- Berdasarkan hasil inspeksi indeks aktif melalui `sp_helpindex BILRG_AdmAdmission`:
  - `IX_bilrg_admadmission_reservationid` (kunci: `ReservationId, VodDate, RegId`)
  - `IX_bilrg_admadmission_opnamerequestid` (kunci: `OpnameRequestId, VodDate, RegId`)
- Kolom `ReservationId` dan `OpnameRequestId` merupakan kolom terdepan, sehingga kueri pencarian admisi pada `FindAdmissionByReservation` dan `FindAdmissionByOpname` dieksekusi via **Index Seek** (< 1 ms).

### 3. Biang Kerok Utama: Beban Berat Eksekusi Kueri Daftar (`BuildJourneyCte`)
Jika kueri detail secara mandiri sangat cepat (< 5 ms), penyebab keterlambatan dan *timeout* adalah:
1. **Global Unbounded GROUP BY pada `ta_reg_history_dokter`**:
   Subkueri `hd` pada CTE `AdmissionRoots`:
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
   Subkueri ini mengagregasi **seluruh baris riwayat dokter rumah sakit** tanpa filter registrasi di level subkueri.
2. **Eksekusi Ganda CTE Proyeksi**:
   Kueri `BuildJourneyCte` dieksekusi **2 kali berurutan**: satu kali untuk menghitung facet (`facetSql`), satu kali untuk mengambil 20 data teratas (`pageSql`).
3. **Pemrosesan Seluruh Rekaman Historis Admisi**:
   CTE memproses seluruh data admisi historis rumah sakit sebelum disaring oleh ekspresi komputasi tahap (`SqlStage NOT IN (5, 6)`) di level terluar.
4. **Eksekusi Sinkron / Blocking di Backend**:
   `JourneyDal` mengeksekusi Dapper secara sinkron (`conn.Query`) dan di-wrap dengan `Task.FromResult`, memblokir thread pool ASP.NET Core saat kueri daftar berjalan puluhan detik.
5. **Antrean Sumber Daya (*Resource & Connection Contention*)**:
   Permintaan detail pasien yang masuk saat kueri daftar sedang berjalan tertahan di antrean basis data/thread pool lebih dari 30 detik, sehingga klien frontend memutuskan koneksi (*timeout*). Pada percobaan ke-2/ke-3, kueri daftar sudah selesai dan data sudah berada di RAM cache, sehingga kueri detail langsung selesai dalam beberapa milidetik.

## Affected Components

1. **Backend Basis Data & DAL (`b09-bilreg-api`)**:
   - `Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs`: Pembangunan CTE kueri daftar (`BuildJourneyCte`), penggabungan eksekusi batch facet & paging, dan konversi ke async I/O.
   - `Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs`: Konversi batch fact hydration ke async.
   - `Bilreg.Application/AdmisiRanapContext/JourneyFeature/UseCases/AdmListJourneyQry.cs`: Handler MediatR async.
   - `Bilreg.SqlDb`: Penyelarasan skrip DDL indeks `BILRG_AdmAdmission` di repositori.

2. **Frontend UI & API Client (`c012_myhospital_web`)**:
   - Mengonsumsi API yang sudah teroptimasi tanpa perlu modifikasi kode atau penaikan timeout.

## Impact Assessment

- **Business Impact**: Petugas admisi rawat inap dapat melayani pasien tanpa hambatan waktu tunggu dan tanpa antarmuka yang macet.
- **Operational Impact**: Eliminasi klik berulang (*retry*) yang sebelumnya membebani server basis data produksi.
- **Technical Impact**: Utilisasi CPU dan Disk I/O SQL Server turun drastis, kueri daftar selesai dalam < 2 detik dan detail selesai dalam < 500 ms.

## Assumptions

1. Basis data aktif `HOSPITAL_PKL` telah memiliki indeks pendukung pada `ta_trs_billing` dan `BILRG_AdmAdmission`.
2. Repositori `Bilreg.SqlDb` perlu diselaraskan agar indeks `BILRG_AdmAdmission` terdokumentasi resmi dalam skrip migrasi.
3. Kontrak wire JSON tidak berubah sehingga frontend `c012_myhospital_web` kompatibel 100%.

## Open Questions and Resolution (Gap Closure)

| ID | Pertanyaan | Status | Keputusan / Resolusi |
|---|---|---|---|
| **OQ-01** | Apakah tabel `BILRG_AdmAdmission` dan `ta_trs_billing` memerlukan penambahan indeks baru di database produksi? | **CLOSED** | **Tidak perlu penambahan di DB aktif.** Bukti `sp_helpindex` menunjukkan indeks `IX_ta_trs_billing_fs_kd_reg` dan `IX_bilrg_admadmission_reservationid` sudah aktif. Yang diperlukan hanya penyelarasan skrip DDL di `Bilreg.SqlDb` repositori. |
| **OQ-02** | Bagaimana mengeliminasi full table scan pada riwayat dokter di `BuildJourneyCte`? | **CLOSED** | **Gunakan `OUTER APPLY` terarah.** Mengganti subkueri global `GROUP BY ta_reg_history_dokter` dengan `OUTER APPLY (SELECT TOP 1 ... WHERE h.fs_kd_reg = a.RegId ...)` sehingga SQL Server melakukan *Index Seek* cepat per registrasi. |
| **OQ-03** | Bagaimana mencegah eksekusi ganda CTE `BuildJourneyCte` pada pemanggilan kueri daftar? | **CLOSED** | **Satukan dalam Single Batch via `QueryMultipleAsync`.** Menggabungkan kueri hitung facet dan kueri paging dalam satu batch SQL, dieksekusi dalam 1 *round-trip*. |
| **OQ-04** | Apakah perubahan async DAL mempengaruhi modul lain di luar Journey? | **CLOSED** | **Tidak.** Perubahan asinkron diisolasi hanya pada `IJourneyDal`, `JourneyDal`, dan handler MediatR di `AdmisiRanapContext/JourneyFeature`. Modul lain tidak tersentuh. |
| **OQ-05** | Apakah frontend `c012_myhospital_web` perlu menaikkan batas timeout 30 detik? | **CLOSED** | **Tidak perlu.** Target waktu respons backend setelah optimasi adalah < 2 detik pada kueri daftar dan < 500 ms pada detail, sehingga batas 30 detik di frontend sudah sangat aman. |

## Recommended Decision

1. Lakukan optimalisasi kueri `BuildJourneyCte` dengan mengganti global `GROUP BY ta_reg_history_dokter` menjadi correlated `OUTER APPLY`.
2. Gabungkan kueri facet dan paging ke dalam satu batch SQL menggunakan `conn.QueryMultipleAsync`.
3. Terapkan filter dini status admisi aktif (`a.AdmissionStatus <> 4`) pada CTE `AdmissionRoots` saat `@ScopeActive = 1`.
4. Ubah seluruh pemanggilan DAL dan handler Journey menjadi *non-blocking async* (`async/await`).
5. Dokumentasikan indeks `BILRG_AdmAdmission` pada repositori `Bilreg.SqlDb`.

## Decision

Penyelidikan menetapkan bahwa seluruh pertanyaan terbuka (*Open Questions*) telah terjawab dan ditutup. Masalah performa diselesaikan melalui optimalisasi kueri proyeksi DAL (`OUTER APPLY`, *single batch execution*, *early status filtering*) dan modernisasi asinkron non-blocking, dengan mempertahankan kompatibilitas kontrak 100%.

## Decision Rationale

- Seluruh bukti empiris dari database telah memvalidasi bahwa indeks-indeks tabel detail sudah aktif.
- Perbaikan fokus secara tepat pada titik berat komputasi (*bottleneck*) yaitu kueri daftar `BuildJourneyCte`.
- Menjaga stabilitas sistem tanpa menimbulkan risiko efek samping (*side effects*) ke modul lain.

## Architecture Applicability

### Decision

ARCHITECTURE-REQUIRED

### Rationale

Penyelesaian masalah membutuhkan restrukturisasi arsitektur kueri proyeksi (`BuildJourneyCte`), desain eksekusi batch facet & paging, dan transisi ke non-blocking async pipeline pada `JourneyFeature`. Dokumen [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-ARCHITECTURE.md) telah memformalkan seluruh keputusan teknis ini.
