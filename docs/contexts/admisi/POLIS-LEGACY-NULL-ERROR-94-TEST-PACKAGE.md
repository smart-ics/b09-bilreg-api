---
Title: Test Package - Kompatibilitas Persistensi Polis Asuransi (ta_polis & ta_polis_cover) dengan fo-legacy
Code: TP-POLIS-LEGACY-NULL-ERROR-94-001
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-11-27
Status: READY-FOR-EXECUTION
TestExecution: [POLIS-LEGACY-NULL-ERROR-94-TEST-EXECUTION.md](POLIS-LEGACY-NULL-ERROR-94-TEST-EXECUTION.md)
---

# 1. Tujuan pengujian

Paket uji ini memastikan perbaikan pada `b09-bilreg-api` untuk masalah `ISSUE-POLIS-LEGACY-NULL-ERROR-94-001` bekerja menyeluruh, yaitu:

1. Data polis baru yang dibuat lewat sistem baru (`b09-bilreg-api` atau aplikasi web) tersimpan lengkap tanpa nilai `NULL` pada seluruh kolom `ta_polis` dan `ta_polis_cover`.
2. Data polis tersebut dapat dibuka normal di layar `[MSB-35] Polis Asuransi` pada aplikasi `fo-legacy` tanpa galat `Run-time error '94': Invalid use of Null`.
3. Tanggal kedaluwarsa polis (`fd_expired`) ikut tersalin ke setiap peserta pada `ta_polis_cover` (`fd_tgl_expired`) saat pembuatan maupun pembaruan.
4. Data korporat lama dari `fo-legacy` (`fs_kd_bu`, `fs_departemen`, `fs_no_payroll`) serta informasi pembuatan (`crttgl`, `crtjam`) tetap utuh saat polis diperbarui lewat sistem baru.

Dokumen acuan:
- ISSUE: [POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md](POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md)
- BUG-INVESTIGATION: [POLIS-LEGACY-NULL-ERROR-94-BUG-INVESTIGATION.md](POLIS-LEGACY-NULL-ERROR-94-BUG-INVESTIGATION.md)
- ARCHITECTURE: [POLIS-LEGACY-NULL-ERROR-94-ARCHITECTURE.md](POLIS-LEGACY-NULL-ERROR-94-ARCHITECTURE.md)
- IMPLEMENTATION-PLAN: [POLIS-LEGACY-NULL-ERROR-94-IMPLEMENTATION-PLAN.md](POLIS-LEGACY-NULL-ERROR-94-IMPLEMENTATION-PLAN.md) (`Status: COMPLETED`, seluruh slice `IMPLEMENTED` dan `GO`)

---

# 2. Prasyarat lingkungan

| Kode | Prasyarat | Keterangan |
|---|---|---|
| P1 | Layanan `Bilreg.Api` aktif dari worktree | Berjalan dari `worktrees/polis-legacy-null-error-94/b09-bilreg-api` di `http://localhost:5194` (Swagger: `http://localhost:5194/swagger`; port default `5189` sedang dipakai sesi lain). |
| P2 | Basis data `HOSPITAL_PKL` aktif | Terhubung ke server basis data pengembangan (`dev.smart-ics.com` / `HOSPITAL_PKL`) yang sama dengan `fo-legacy`. |
| P3 | Akses otorisasi API atau aplikasi web | Token Bearer yang sah untuk endpoint `/api/Polis`, atau login petugas aktif pada `c012_myhospital_web` (`http://localhost:5173`). |
| P4 | Akses aplikasi `fo-legacy` | Aplikasi `myHospital XI - Front Office` dengan akses ke menu `[MSB-35] Polis Asuransi` (terhubung ke basis data `HOSPITAL_PKL`). |
| P5 | Akses baca basis data (SSMS / Azure Data Studio) | Digunakan untuk memeriksa isi kolom audit dan kolom legacy pada `dbo.ta_polis` serta `dbo.ta_polis_cover`. |

---

# 3. Data uji yang disiapkan (basis data `HOSPITAL_PKL`)

Data berikut diambil langsung dari basis data `HOSPITAL_PKL` dan dapat digunakan untuk pengujian:

| Kegunaan | Kode / ID | Detail data |
|---|---|---|
| Pasien utama untuk polis baru (TC-001) | `3375022X0144080` | Nama: `TIKA RESTU FARMASI` |
| Pasien tambahan untuk uji tambah/hapus peserta (TC-004) | `3375022X0144079` | Nama: `AIRUL BERKAH (ST.RITA)` |
| Tipe jaminan uji | `AAD00` | `ASURANSI ADIRA DINAMIKA PT` (alternatif: `JKN00` = `JKN NON PBI`, `ADM00` = `ADMEDIKA`) |
| Kelas rawat inap uji | `201` | `I A` (alternatif: `204` = `I B`, `101` = `VVIP`) |
| Polis legacy dengan Departemen terisi (TC-003) | `PS00060388` | No. Polis: `8000198700870145`, Atas Nama: `HENIK SULISTYORINI`, Tipe: `ADM00`, Kelas: `202`, MR: `337502200178696`, `fs_departemen = 'PLN UID LAMPUNG'`, `crttgl = '2018-11-30'` |
| Polis legacy dengan No. Payroll terisi (TC-003) | `PS00033477` | No. Polis: `1111`, Atas Nama: `JIHAN`, Tipe: `KPM01`, Kelas: `607`, MR: `337502200146995`, `fs_no_payroll = '1111'`, `crttgl = '2016-07-15'` |
| Polis kasus awal dari laporan bug (TC-005) | `PS00183968` | No. Polis: `11198909090901`, Atas Nama: `test`, Tipe: `JKN00`, Kelas: `302`, Expired: `2050-01-01`, MR: `337502200307118` |

---

# 4. Urutan pelaksanaan

Jalankan kasus uji secara berurutan dari `TC-001` sampai `TC-006`. Kasus uji `TC-002` dan `TC-004` menggunakan `PolisId` yang dihasilkan dari `TC-001`.

---

# 5. Daftar kasus uji

## TC-001 — Pembuatan polis baru dan buka data di fo-legacy (Happy Path)

**Tujuan**: Memastikan polis baru yang dibuat lewat sistem baru menulis seluruh 20 kolom `ta_polis` dan 14 kolom `ta_polis_cover` tanpa nilai `NULL`, serta dapat dibuka normal di `fo-legacy`.

**Prasyarat**: P1, P2, P3, P4, P5.

**Data uji**:
- `pasienId`: `3375022X0144080`
- `tipeJaminanId`: `AAD00`
- `noPolis`: `UJI-POLIS-20261127-01`
- `atasName`: `TIKA RESTU FARMASI`
- `expiredDate`: `2030-12-31`
- `kelasRanapId`: `201`
- `isCoverRajal`: `true`

**Langkah**:
1. Kirim permintaan pembuatan polis baru melalui `POST http://localhost:5194/api/Polis` (lewat Swagger atau HTTP client dengan token valid), atau buat polis baru dengan data uji di atas melalui form pembuatan polis di `c012_myhospital_web`.
2. Catat nilai `polisId` dari respons API (misalnya `PSxxxxxxxx`).
3. Jalankan kueri pemeriksaan di basis data `HOSPITAL_PKL` untuk `polisId` tersebut pada tabel `dbo.ta_polis` dan `dbo.ta_polis_cover`.
4. Buka aplikasi `fo-legacy`, masuk ke layar `[MSB-35] Polis Asuransi`.
5. Masukkan `polisId` pada kolom **Kode Polis**, lalu tekan Enter.

**Hasil yang diharapkan**:
- Respons API berstatus `200 OK` dan mengembalikan `data.polisId` yang tidak kosong.
- Pada tabel `dbo.ta_polis`, baris `polisId` memiliki nilai non-null pada seluruh 20 kolom:
  - `fs_kd_bu = ''`, `fs_departemen = ''`, `fs_no_payroll = ''` (bukan `NULL`).
  - `crttgl` berisi tanggal hari ini (`yyyy-MM-dd`) dan `crtjam` berisi jam pembuatan (`HH:mm:ss`).
  - `updtgl = '3000-01-01'` dan `updjam = '00:00:00'`.
  - `crtipa`, `crtver`, `crtusr`, `updipa`, `updver`, `updusr` tidak bernilai `NULL`.
- Pada tabel `dbo.ta_polis_cover`, baris untuk `fs_mr = '3375022X0144080'` memiliki `fd_tgl_expired = '2030-12-31'` (sama dengan `ta_polis.fd_expired`) serta seluruh kolom audit terisi non-null.
- Di layar `[MSB-35] Polis Asuransi` (`fo-legacy`), seluruh informasi polis (`Tipe Jaminan`, `No.Polis`, `Atas Nama`, `Expired`, `Hak Kelas Ranap`) dan baris peserta pada tabel bawah tampil lengkap tanpa galat `Run-time error '94': Invalid use of Null`.

---

## TC-002 — Pembaruan polis dan penyelarasan tanggal kedaluwarsa peserta (Business Rule)

**Tujuan**: Memastikan pembaruan polis memperbarui kolom audit `upd*`, menjaga kolom `crt*` tetap utuh, dan menyalin tanggal kedaluwarsa terbaru ke `ta_polis_cover.fd_tgl_expired`.

**Prasyarat**: P1, P2, P3, P4, P5, dan `TC-001` sudah menghasilkan `polisId`.

**Data uji**:
- `polisId`: `<polisId dari TC-001>`
- `pasienId`: `3375022X0144080`
- `noPolis`: `UJI-POLIS-20261127-01`
- `atasName`: `TIKA RESTU FARMASI REVISI`
- `expiredDate`: `2032-06-30`
- `kelasRanapId`: `204`
- `statusPesertaId`: `P`
- `isCoverRajal`: `false`

**Langkah**:
1. Catat nilai `crttgl` dan `crtjam` pada `dbo.ta_polis` dari hasil `TC-001`.
2. Kirim permintaan pembaruan melalui `PUT http://localhost:5194/api/Polis` dengan data uji di atas.
3. Periksa kembali baris `polisId` pada `dbo.ta_polis` dan `dbo.ta_polis_cover`.
4. Buka ulang `polisId` di layar `[MSB-35] Polis Asuransi` (`fo-legacy`).

**Hasil yang diharapkan**:
- Respons API berstatus `200 OK`.
- Pada `dbo.ta_polis`:
  - `fs_atas_nama` berubah menjadi `TIKA RESTU FARMASI REVISI`, `fd_expired = '2032-06-30'`, `fs_kd_kelas_ri = '204'`, `fb_cover_rj = 0`.
  - `crttgl` dan `crtjam` tetap sama seperti sebelum pembaruan.
  - `updtgl` berubah menjadi tanggal hari ini (`yyyy-MM-dd`) dan `updjam` berisi jam pembaruan (`HH:mm:ss`), bukan lagi `3000-01-01`.
- Pada `dbo.ta_polis_cover`:
  - `fd_tgl_expired` ikut berubah menjadi `2032-06-30` (bukan `NULL` dan bukan `3000-01-01`).
- Di layar `[MSB-35] Polis Asuransi` (`fo-legacy`), data terbaru dan tanggal kedaluwarsa tampil normal tanpa galat.

---

## TC-003 — Pelestarian data korporat legacy saat memperbarui polis lama (Business Rule & Regression Risk)

**Tujuan**: Memastikan pembaruan polis lama yang berasal dari `fo-legacy` tidak menghapus atau menimpa nilai `fs_kd_bu`, `fs_departemen`, maupun `fs_no_payroll`.

**Prasyarat**: P1, P2, P3, P4, P5.

**Data uji** (pilih salah satu; disarankan `PS00060388`):
- `polisId`: `PS00060388`
- `pasienId`: `337502200178696`
- `noPolis`: `8000198700870145`
- `atasName`: `HENIK SULISTYORINI`
- `expiredDate`: `3000-01-01`
- `kelasRanapId`: `202`
- `statusPesertaId`: `P`
- `isCoverRajal`: `false`

**Langkah**:
1. Catat nilai awal `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, `crttgl`, dan `crtjam` milik `PS00060388` di `dbo.ta_polis`.
2. Kirim permintaan pembaruan melalui `PUT http://localhost:5194/api/Polis` dengan data uji di atas (menyimpan ulang data tanpa mengubah informasi bisnis pasien).
3. Periksa kembali baris `PS00060388` pada `dbo.ta_polis` dan `dbo.ta_polis_cover`.
4. Buka `PS00060388` di layar `[MSB-35] Polis Asuransi` (`fo-legacy`).

**Hasil yang diharapkan**:
- Respons API berstatus `200 OK`.
- Nilai `fs_departemen` tetap `PLN UID LAMPUNG` (tidak menjadi kosong atau `NULL`).
- Nilai `fs_kd_bu` dan `fs_no_payroll` tetap non-null.
- Nilai `crttgl` tetap `2018-11-30` dan `crtjam` tetap `01:17:35`.
- Nilai `updtgl` dan `updjam` diperbarui ke waktu pembaruan saat ini.
- Pada `dbo.ta_polis_cover`, baris `337502200178696` memiliki `fd_tgl_expired = '3000-01-01'` (non-null).
- Di layar `[MSB-35] Polis Asuransi` (`fo-legacy`), polis `PS00060388` terbuka normal beserta nilai departemen `PLN UID LAMPUNG`.

---

## TC-004 — Penambahan dan penghapusan peserta cakupan polis (Business Rule)

**Tujuan**: Memastikan penambahan dan penghapusan peserta cakupan (`addCoverage` dan `removeCoverage`) tetap mengisi `fd_tgl_expired` dan kolom audit non-null pada seluruh baris peserta.

**Prasyarat**: P1, P2, P3, P4, P5, dan `TC-002` sudah selesai dijalankan.

**Data uji**:
- `polisId`: `<polisId dari TC-001>`
- Peserta tambahan: `pasienId = 3375022X0144079`, `statusPesertaId = T` (atau `I`/`A`/`P` sesuai status valid di sistem)

**Langkah**:
1. Kirim `PUT http://localhost:5194/api/Polis/addCoverage` dengan `polisId`, `pasienId = 3375022X0144079`, dan `statusPesertaId`.
2. Periksa tabel `dbo.ta_polis_cover` untuk `polisId` tersebut.
3. Buka `polisId` di layar `[MSB-35] Polis Asuransi` (`fo-legacy`) dan periksa daftar peserta di tabel bawah.
4. Kirim `PUT http://localhost:5194/api/Polis/removeCoverage` dengan `polisId` dan `pasienId = 3375022X0144079`.
5. Periksa kembali tabel `dbo.ta_polis_cover`.

**Hasil yang diharapkan**:
- Setelah `addCoverage`, terdapat 2 baris peserta (`3375022X0144080` dan `3375022X0144079`) pada `dbo.ta_polis_cover`.
- Kedua baris memiliki `fd_tgl_expired = '2032-06-30'` (mengikuti `ta_polis.fd_expired`) dan seluruh kolom audit terisi non-null.
- Di `fo-legacy` `[MSB-35]`, kedua peserta tampil di grid tanpa galat.
- Setelah `removeCoverage`, hanya peserta utama `3375022X0144080` yang tersisa dengan `fd_tgl_expired = '2032-06-30'` dan kolom audit non-null.

---

## TC-005 — Pembacaan polis kasus awal (`PS00183968`) di API dan fo-legacy (Error Handling & Regression)

**Tujuan**: Memastikan data polis yang sebelumnya dilaporkan bermasalah (`PS00183968`) dapat dibaca dengan aman melalui endpoint `b09-bilreg-api` maupun `fo-legacy`.

**Prasyarat**: P1, P2, P3, P4.

**Data uji**:
- `polisId`: `PS00183968`
- `noPeserta`: `11198909090901`

**Langkah**:
1. Panggil `GET http://localhost:5194/api/Polis/PS00183968`.
2. Panggil `GET http://localhost:5194/api/Polis/byNoPeserta?noPeserta=11198909090901`.
3. Buka `PS00183968` di layar `[MSB-35] Polis Asuransi` (`fo-legacy`).

**Hasil yang diharapkan**:
- Kedua panggilan API mengembalikan `200 OK` dengan detail polis `PS00183968` (`atasName = "test"`, `expiredDate = "2050-01-01"`) beserta peserta `337502200307118`.
- Di `fo-legacy` `[MSB-35]`, data polis `PS00183968` tampil tanpa galat `Run-time error '94': Invalid use of Null`.

---

## TC-006 — Penolakan input tidak lengkap saat pembuatan polis (Validation Rule)

**Tujuan**: Memastikan sistem menolak permintaan pembuatan polis dengan data wajib kosong atau format tanggal salah, serta tidak menulis data rusak ke basis data.

**Prasyarat**: P1, P2, P3.

**Data uji**:
- `pasienId`: `3375022X0144080`
- `tipeJaminanId`: `AAD00`
- `noPolis`: `""` (kosong)
- `atasName`: `UJI VALIDASI`
- `expiredDate`: `2030-12-31`
- `kelasRanapId`: `201`
- `isCoverRajal`: `true`

**Langkah**:
1. Kirim `POST http://localhost:5194/api/Polis` dengan data uji di atas (`noPolis` kosong).
2. Ulangi dengan `noPolis = "UJI-INVALID-DATE"` dan `expiredDate = "31-12-2030"` (format tanggal bukan `yyyy-MM-dd`).

**Hasil yang diharapkan**:
- Kedua permintaan ditolak oleh API (status bukan `200 OK`, mengembalikan pesan error validasi).
- Tidak ada baris baru yang tersimpan di `dbo.ta_polis` untuk `UJI VALIDASI` atau `UJI-INVALID-DATE`.
