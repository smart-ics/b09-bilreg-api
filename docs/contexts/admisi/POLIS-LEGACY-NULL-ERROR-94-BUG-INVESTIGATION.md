# BUG-INVESTIGATION

## Context

Issue: [ISSUE-POLIS-LEGACY-NULL-ERROR-94-001](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/admisi/POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md)
Problem Summary: Data polis asuransi yang dibuat atau diperbarui melalui `b09-bilreg-api` gagal dibuka di layar `[MSB-35] Polis Asuransi` pada `fo-legacy` dengan galat `Run-time error '94': Invalid use of Null` karena ada kolom pada `ta_polis` dan `ta_polis_cover` yang tidak diisi oleh `b09-bilreg-api` serta perbedaan perilaku penyimpanan dengan `fo-legacy`.

## Current State

1. Pada `b09-bilreg-api`:
   - `PolisDal` (`Insert` dan `Update`) hanya menulis tujuh kolom ke tabel `ta_polis`: `fs_kd_polis`, `fs_no_polis`, `fs_atas_nama`, `fd_expired`, `fs_kd_tipe_jaminan`, `fb_cover_rj`, dan `fs_kd_kelas_ri`.
   - `PolisCoverDal` (`Insert`) hanya menulis tiga kolom ke tabel `ta_polis_cover`: `fs_kd_polis`, `fs_mr`, dan `fs_kd_status`.
   - Saat pembaruan polis (`PolisRepo.SaveChanges`), `PolisCoverDal` menghapus seluruh baris `ta_polis_cover` untuk `fs_kd_polis` terkait lalu memasukkan ulang daftar cakupan peserta tanpa kolom tanggal kedaluwarsa per peserta.
   - Skema tabel di `Bilreg.SqlDb` (`ta_polis.sql` dan `ta_polis_cover.sql`) hanya mendefinisikan subset kolom di atas, dengan `DEFAULT ('')` pada tiap kolom tersebut.
2. Pada `fo-legacy`:
   - Saat membuka polis di `[MSB-35] Polis Asuransi` (`frmSetupPolis.frm` memanggil `clsPolis.Kode`), `fo-legacy` membaca kolom tambahan pada `ta_polis` (`fs_kd_bu`, `fs_departemen`, `fs_no_payroll`) serta kolom audit (`crttgl`, `crtjam`, `crtusr`, `updtgl`, `updjam`, `updusr`) tanpa `ISNULL(...)`.
   - Saat memuat daftar peserta di `ta_polis_cover` (`clsPolis.Populate`), `fo-legacy` membaca `fd_tgl_expired` tanpa `ISNULL(...)` dan memformatnya dengan fungsi tanggal.
   - Saat menyimpan polis baru maupun perubahan, `fo-legacy` selalu mengisi seluruh kolom tersebut (string kosong `""` jika tidak ada nilai, `01-01-3000` / `3000-01-01` untuk tanggal default, serta metadata audit `crt*`/`upd*`).

## Problem Analysis

1. **Ketidaklengkapan Kolom `ta_polis` pada `b09-bilreg-api` (Terverifikasi di Basis Data)**:
   - Hasil pemeriksaan skema aktual `dbo.ta_polis` menunjukkan kolom `FS_NO_PAYROLL` bertipe `varchar(40) NULL` **tanpa constraint `DEFAULT`**, sedangkan `FS_KD_BU` (`varchar(8) NOT NULL DEFAULT (' ')`) dan `FS_DEPARTEMEN` (`varchar(40) NOT NULL DEFAULT (' ')`) memiliki default spasi, serta kolom audit `CRTTGL`/`UPDTGL` memiliki default `'3000-01-01'`.
   - Pemeriksaan langsung pada baris `fs_kd_polis = 'PS00183968'` di `dbo.ta_polis` membuktikan bahwa `fs_no_payroll` bernilai **`NULL`**, `crttgl = '3000-01-01'`, dan `crtjam = '00:00:00'`.
   - Saat `fo-legacy` (`clsPolis.Kode`, baris 85) mengeksekusi `NoPayroll = oRS("fs_no_payroll").Value` ke variabel bertipe `String` VB6, nilai `NULL` pada `fs_no_payroll` langsung memicu `Run-time error '94': Invalid use of Null`.
2. **Ketidaklengkapan Kolom `ta_polis_cover` pada `b09-bilreg-api`**:
   - `PolisCoverDal.Insert` menggunakan `SqlBulkCopy` dengan pemetaan hanya untuk `fs_kd_polis`, `fs_mr`, dan `fs_kd_status`. Kolom `fd_tgl_expired` tidak dipetakan meskipun properti `ExpiredDate` sudah ada di `PolisCoverModel`.
   - Jika `ta_polis_cover.fd_tgl_expired` pada basis data target bernilai `NULL` (atau menjadi `NULL` setelah `PolisRepo.SaveChanges` menghapus dan menulis ulang `ta_polis_cover`), pemanggilan `My.TglDMY(oRS("fd_tgl_expired").Value)` di `clsPolis.Populate` juga akan memicu `Run-time error '94': Invalid use of Null`.
   - Selain itu, penghapusan dan penulisan ulang seluruh `ta_polis_cover` saat `Update` di `b09-bilreg-api` berisiko menghilangkan nilai `fd_tgl_expired` pada data peserta yang sebelumnya dibuat dari `fo-legacy`.
3. **Kesesuaian Kondisi Gejala pada Screenshot**:
   - Pada bukti screenshot (`Kode Polis: PS00183968`), seluruh field tampilan (`Tipe Jaminan`, `No.Polis`, `Atas Nama`, `Expired`, `Hak Kelas Ranap`) belum terisi saat dialog error muncul. Hal ini sejalan dengan alur `frmSetupPolis.frm` yang memanggil `clsPolis.Kode` terlebih dahulu sebelum mengisi kontrol UI, sehingga galat terjadi tepat pada pembacaan recordset di `clsPolis`.

## Affected Components

- **Modules**: `AdmisiContext` — `JaminanFeature` pada `b09-bilreg-api`.
- **Services / Application & Domain**:
  - Domain model polis dan cakupan peserta (`PolisModel`, `PolisCoverModel`).
  - Command handler pembuatan dan pembaruan polis (`PolisCreateCommand`, `PolisUpdateCommand`, `PolisAddCoverageCommand`, `PolisRemoveCoverageCommand`).
- **Persistence / Infrastructure**:
  - Repository dan Data Access Layer untuk header polis dan detail cakupan peserta (`PolisRepo`, `PolisDal`, `PolisDto`, `PolisCoverDal`, `PolisCoverDto`).
- **Database Objects**:
  - Tabel `ta_polis` dan `ta_polis_cover` (termasuk definisi skema di `Bilreg.SqlDb` dan kompatibilitasnya terhadap tabel operasional bersama `fo-legacy`).
- **Integrations / Co-existence**:
  - Aplikasi legacy `fo-legacy` (`[MSB-35] Polis Asuransi` / `frmSetupPolis.frm`, `frmSetupPolis2.frm`, dan `clsPolis.cls`) yang berbagi tabel `ta_polis` dan `ta_polis_cover` pada basis data yang sama.

## Impact Assessment

- **Business Impact**: Petugas administrasi / front office tidak dapat membuka atau meninjau data polis asuransi tertentu di `fo-legacy` selama masa transisi koeksistensi sistem baru dan lama.
- **Operational Impact**: Polis yang dibuat atau diperbarui melalui sistem baru berpotensi macet saat dibuka di `fo-legacy` (sehingga verifikasi manual atau penyesuaian di aplikasi legacy terhambat), dan pembaruan polis dari sistem baru dapat menghapus tanggal kedaluwarsa peserta (`fd_tgl_expired`) yang sudah ada.
- **Technical Impact**: Terdapat kesenjangan kontrak data antara persistensi `b09-bilreg-api` dengan struktur serta ekspektasi pembacaan non-null pada `fo-legacy`. Hal ini memengaruhi konsistensi data lintas sistem yang berbagi basis data yang sama.

## Assumptions

1. Selama masa migrasi/koeksistensi, `b09-bilreg-api` dan `fo-legacy` masih membaca dan menulis ke tabel `ta_polis` dan `ta_polis_cover` pada basis data SQL Server yang sama.
2. Perbaikan untuk tahap ini difokuskan pada `b09-bilreg-api` agar kompatibel dengan data dan perilaku `fo-legacy`, tanpa mengubah kode VB6 `fo-legacy`.
3. Kolom `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, kolom audit `crt*`/`upd*`, serta `fd_tgl_expired` pada `ta_polis_cover` memang ada pada basis data operasional klien yang menjalankan `fo-legacy`.

## Open Questions

Seluruh pertanyaan terbuka telah terjawab dan diputuskan:

1. **Nilai Default Kolom Audit pada `ta_polis`** *(Resolved)*:
   - Mengikuti pola `GetSysData()` + `My.SQLGenerator2` pada `fo-legacy` (`clsPolis.cls`):
     - Saat `Insert`: `crttgl` (`yyyy-MM-dd`), `crtjam` (`HH:mm:ss`), `crtusr` (kode petugas / user aktif), `crtipa` (IP / `""`), `crtver` (versi / `""`), sedangkan kolom update diisi nilai sentinel legacy (`updtgl = '3000-01-01'`, `updjam = '00:00:00'`, `updusr = ''`, `updipa = ''`, `updver = ''`).
     - Saat `Update`: kolom `crt*` tetap dipertahankan, sedangkan `updtgl` (`yyyy-MM-dd`), `updjam` (`HH:mm:ss`), `updusr` (kode petugas / user aktif), `updipa`, dan `updver` diperbarui.
2. **Nilai `fd_tgl_expired` pada `ta_polis_cover`** *(Resolved)*:
   - Mengikuti nilai `ta_polis.fd_expired` milik header polis (bukan hardcoded `3000-01-01`).
   - Catatan penamaan kolom: tabel `ta_polis` menggunakan kolom `fd_expired`, sedangkan `ta_polis_cover` menggunakan kolom `fd_tgl_expired`.
3. **Pelestarian Nilai Legacy saat `Update`** *(Resolved)*:
   - Kolom legacy `fs_kd_bu`, `fs_departemen`, `fs_no_payroll` pada `ta_polis` bersifat **read-only / dipertahankan apa adanya** oleh `b09-bilreg-api` (diisi `''` saat `Insert` baru, dan tidak ditimpa/diubah saat `Update` polis lama).
   - Untuk `ta_polis_cover`, nilai `fd_tgl_expired` diisi mengikuti `ta_polis.fd_expired` saat penyimpanan/pembaruan agar tidak pernah `NULL` maupun hilang saat `PolisCoverDal` menulis ulang detail cakupan.

## Alternative Evaluation

1. **Alternatif A — Menyelaraskan Persistensi `b09-bilreg-api` dengan Ekspektasi Non-Null dan Perilaku `fo-legacy`**:
   - `b09-bilreg-api` mengisi seluruh kolom wajib-baca `fo-legacy` pada `ta_polis` dan `ta_polis_cover` dengan nilai non-null yang valid saat `Insert`, serta mempertahankan data legacy yang sudah ada saat `Update`.
   - *Kelebihan*: Tidak perlu mengubah atau mengkompilasi ulang aplikasi legacy `fo-legacy`, dan langsung mencegah terjadinya nilai `NULL` maupun hilangnya data legacy dari penulisan sistem baru.
   - *Kekurangan*: Membutuhkan penyesuaian pada layer persistensi (dan kemungkinan definisi skema/model) di `b09-bilreg-api`.
2. **Alternatif B — Menambahkan Constraint `DEFAULT` di Basis Data Saja Tanpa Mengubah `b09-bilreg-api`**:
   - Hanya mengandalkan `DEFAULT ('')` di SQL Server untuk kolom yang tidak dikirim `b09-bilreg-api`.
   - *Kelebihan*: Tidak ada perubahan kode aplikasi.
   - *Kekurangan*: Tidak menyelesaikan masalah hilangnya `fd_tgl_expired` saat `PolisCoverDal` menghapus dan menulis ulang `ta_polis_cover`, tidak mengisi kolom audit `crt*`/`upd*` dengan format yang diharapkan `fo-legacy` (misalnya `updtgl = '3000-01-01'`), serta bergantung pada perubahan manual DDL di setiap server klien.
3. **Alternatif C — Mengubah `fo-legacy` untuk Menambahkan `ISNULL` pada Semua Query `clsPolis`**:
   - *Kelebihan*: Tahan terhadap nilai `NULL` dari sumber mana pun.
   - *Kekurangan*: Di luar ruang lingkup permintaan (permintaan secara eksplisit meminta kompatibilitas dari sisi `b09-bilreg-api`) dan membutuhkan rilis ulang biner VB6 ke seluruh klien.

## Recommended Decision

Menyelaraskan persistensi `b09-bilreg-api` agar kompatibel dengan struktur kolom, nilai non-null, dan perilaku penyimpanan `fo-legacy` pada tabel `ta_polis` dan `ta_polis_cover` (Alternatif A), termasuk menjaga agar pembaruan data dari `b09-bilreg-api` tidak menghilangkan atau mengosongkan atribut legacy yang sudah ada.

## Decision

Dipilih **Alternatif A**: memperbarui perilaku penyimpanan dan pembacaan data polis pada `b09-bilreg-api` agar kompatibel penuh dengan data dan perilaku `fo-legacy` pada tabel `ta_polis` dan `ta_polis_cover`.

## Decision Rationale

1. Fokus perbaikan diminta khusus pada `b09-bilreg-api` tanpa bergantung pada perubahan biner `fo-legacy`.
2. Menyelaraskan penulisan kolom `ta_polis` (termasuk kolom non-UI seperti Badan Usaha, Departemen, No. Payroll, dan metadata audit) serta `ta_polis_cover` (termasuk `fd_tgl_expired`) langsung dari `b09-bilreg-api` menjamin setiap data baru maupun hasil pembaruan aman dibuka di `fo-legacy`.
3. Pendekatan ini juga melindungi data polis lama yang sebelumnya dibuat di `fo-legacy` agar tidak kehilangan nilai `fd_tgl_expired` atau atribut lainnya saat diperbarui melalui `b09-bilreg-api`.

## Architecture Applicability

### Decision

ARCHITECTURE-REQUIRED

### Rationale

Perbaikan ini menyentuh batas persistensi dan kepemilikan data pada `b09-bilreg-api` (`ta_polis`, `ta_polis_cover`, serta penyelarasan skema di `Bilreg.SqlDb` dengan tabel legacy). Diperlukan keputusan arsitektur untuk menentukan:
- Apakah atribut legacy (`fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, audit `crt*`/`upd*`, dan `fd_tgl_expired` pada `ta_polis_cover`) dikelola sebagai bagian dari domain/DTO atau ditangani murni di layer persistensi sebagai kompatibilitas legacy.
- Bagaimana strategi pembaruan (`Update`) mempertahankan nilai kolom legacy yang sudah ada agar tidak tertimpa saat `b09-bilreg-api` menyimpan perubahan polis.
