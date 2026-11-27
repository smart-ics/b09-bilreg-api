---
Title: Architecture - Kompatibilitas Persistensi Polis Asuransi (ta_polis & ta_polis_cover) dengan fo-legacy
Code: ARCH-POLIS-LEGACY-NULL-ERROR-94-001
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-27
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis pada `b09-bilreg-api` (`AdmisiContext.JaminanFeature` dan `Bilreg.SqlDb`) untuk memperbaiki galat `Run-time error '94': Invalid use of Null` di aplikasi legacy `fo-legacy` (`[MSB-35] Polis Asuransi`) serta menyelaraskan data dan perilaku penyimpanan polis asuransi (`ta_polis` dan `ta_polis_cover`) dengan `fo-legacy`.

Referenced originating BUG ISSUE:
- [POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/admisi/POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md) (`ISSUE-POLIS-LEGACY-NULL-ERROR-94-001`)

# 2. Architectural Basis

## Business Context

Originating BUG ISSUE:
- [POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/admisi/POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md)

## Analysis Input

Referenced BUG-INVESTIGATION:
- [POLIS-LEGACY-NULL-ERROR-94-BUG-INVESTIGATION.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/admisi/POLIS-LEGACY-NULL-ERROR-94-BUG-INVESTIGATION.md)

Keputusan investigasi yang direalisasikan:
1. Persistensi `b09-bilreg-api` wajib menulis nilai non-null yang kompatibel dengan `fo-legacy` untuk seluruh kolom `ta_polis` (termasuk `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, dan 10 kolom audit `crt*`/`upd*`) serta `ta_polis_cover` (termasuk `fd_tgl_expired` dan 10 kolom audit `crt*`/`upd*`).
2. Kolom legacy `fs_kd_bu`, `fs_departemen`, dan `fs_no_payroll` pada `ta_polis` bersifat *read-only* bagi `b09-bilreg-api`: diinisialisasi dengan string kosong (`""`) saat `Insert`, dan dipertahankan saat `Update` (dengan perlindungan `ISNULL(..., '')` agar data lama yang sudah telanjur `NULL` ikut pulih menjadi `""` saat diperbarui).
3. Kolom `ta_polis_cover.fd_tgl_expired` diisi mengikuti nilai `ta_polis.fd_expired` milik header polis.
4. Kolom audit mengikuti konvensi `GetSysData()` + `SQLGenerator2` pada `fo-legacy`: saat `Insert` mengisi `crttgl` (`yyyy-MM-dd`), `crtjam` (`HH:mm:ss`), `crtipa`, `crtver`, `crtusr` dan nilai sentinel update (`updtgl = '3000-01-01'`, `updjam = '00:00:00'`, `updipa = ''`, `updver = ''`, `updusr = ''`); saat `Update` memperbarui `updtgl`, `updjam`, `updipa`, `updver`, `updusr` tanpa mengubah `crt*`.

```text
ISSUE (BUG)
        +
        BUG-INVESTIGATION
        ↓
    ARCHITECTURE
```

# 3. Scope

## Included
- Penyelarasan DTO dan DAL header polis (`PolisDto`, `PolisDal`) di `Bilreg.Infrastructure.AdmisiContext.JaminanFeature`.
- Penyelarasan DTO, DAL, dan Repository detail cakupan peserta (`PolisCoverDto`, `PolisCoverDal`, `PolisRepo`) di `Bilreg.Infrastructure.AdmisiContext.JaminanFeature`.
- Penyelarasan definisi DDL tabel `ta_polis.sql` dan `ta_polis_cover.sql` di `Bilreg.SqlDb.AdmisiContext.JaminanFeature`.
- Pembaruan dan penambahan unit/integration test pada `PolisDalTest` dan `PolisCoverDalTest` di `Bilreg.Test`.

## Excluded
- Perubahan kode VB6 pada repositori `fo-legacy`.
- Perubahan UI atau payload kontrak API pada frontend `c012_myhospital_web`.
- Eksposur field Badan Usaha (`fs_kd_bu`), Departemen (`fs_departemen`), dan No. Payroll (`fs_no_payroll`) ke endpoint REST publik `PolisController`.

# 4. Technical Decisions

1. **Isolasi Kolom Kompatibilitas Legacy di Layer Infrastruktur (DTO & DAL)**:
   - `PolisModel` dan `PolisCoverModel` di layer Domain tetap bersih dari atribut teknis legacy (`fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, serta `crt*`/`upd*`).
   - `PolisDto` menangani nilai default kompatibilitas legacy untuk `ta_polis`:
     - `fs_kd_bu = ""`, `fs_departemen = ""`, `fs_no_payroll = ""`
     - `crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`
     - `updtgl`, `updjam`, `updipa`, `updver`, `updusr`
   - Saat `PolisDto.FromModel(PolisModel model)` dipanggil:
     - Untuk keperluan `Insert`, `crttgl` diisi tanggal saat ini (`yyyy-MM-dd`), `crtjam` diisi jam saat ini (`HH:mm:ss`), sedangkan `updtgl = "3000-01-01"` dan `updjam = "00:00:00"`.
     - Untuk keperluan `Update`, `PolisDal.Update` menggunakan tanggal dan jam saat update untuk mengisi `@updtgl` dan `@updjam` (atau properti pada DTO jika sudah disiapkan), serta menggunakan `fs_kd_bu = ISNULL(fs_kd_bu, '')`, `fs_departemen = ISNULL(fs_departemen, '')`, dan `fs_no_payroll = ISNULL(fs_no_payroll, '')` agar nilai existing dari `fo-legacy` tidak tertimpa dan nilai `NULL` otomatis diperbaiki menjadi string kosong.
2. **Propagasi `ta_polis.fd_expired` ke `ta_polis_cover.fd_tgl_expired`**:
   - Pada `PolisRepo.SaveChanges(PolisModel model)`, pembuatan `PolisCoverDto` meneruskan `model.ExpiredDate` dari header `PolisModel` ke `PolisCoverDto.FromModel(cover, model.ExpiredDate)`.
   - `PolisCoverDto` memiliki properti `fd_tgl_expired` (default `"3000-01-01"`, diisi `expiredDate.ToString("yyyy-MM-dd")` dari header) serta 10 properti kolom audit (`crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr`).
   - `PolisCoverDal.Insert` memetakan seluruh 14 kolom (`fs_kd_polis`, `fs_mr`, `fs_kd_status`, `fd_tgl_expired`, dan 10 kolom audit) pada `SqlBulkCopy`.
   - `PolisCoverDal.ListData` membaca `ISNULL(aa.fd_tgl_expired, '3000-01-01') AS fd_tgl_expired`.
3. **Pembacaan Defensif (`ISNULL`) pada `PolisDal`**:
   - Seluruh query `SELECT` di `PolisDal` (`GetData`, `ListData`, `GetDataByNoPeserta`) menggunakan `ISNULL` pada kolom-kolom tabel `ta_polis` agar `b09-bilreg-api` juga tahan terhadap baris data legacy yang memiliki nilai `NULL`.

# 5. Component Responsibilities

| Component | Responsibility |
|---|---|
| `PolisDto` | Memetakan `PolisModel` ke struktur baris `ta_polis` lengkap dengan nilai default non-null untuk `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, dan metadata audit `crt*`/`upd*`. |
| `PolisDal` | Mengeksekusi `INSERT` 20 kolom ke `ta_polis`, `UPDATE` kolom bisnis beserta `upd*` dan self-healing `ISNULL` pada kolom legacy *read-only*, serta `SELECT` data polis. |
| `PolisCoverDto` | Memetakan `PolisCoverModel` beserta `ExpiredDate` header polis ke struktur baris `ta_polis_cover` (`fd_tgl_expired` dan metadata audit `crt*`/`upd*`). |
| `PolisCoverDal` | Mengeksekusi bulk insert 14 kolom ke `ta_polis_cover` via `SqlBulkCopy`, menghapus detail per `fs_kd_polis`, dan membaca daftar cakupan peserta beserta `fd_tgl_expired`. |
| `PolisRepo` | Mengorkestrasi penyimpanan agregat `PolisModel` ke `PolisDal` dan `PolisCoverDal` dengan meneruskan `model.ExpiredDate` ke setiap item `PolisCoverDto`. |
| `Bilreg.SqlDb` (`ta_polis.sql`, `ta_polis_cover.sql`) | Mendefinisikan skema referensi tabel `ta_polis` (20 kolom) dan `ta_polis_cover` (14 kolom) yang selaras dengan basis data operasional dan memiliki constraint `NOT NULL DEFAULT`. |

# 6. Integration Design

| Source | Target | Purpose |
|---|---|---|
| `PolisRepo.SaveChanges` | `PolisDto.FromModel` & `PolisDal.Insert` / `Update` | Menyimpan header polis ke `dbo.ta_polis` dengan seluruh kolom legacy dan audit terisi non-null. |
| `PolisRepo.SaveChanges` | `PolisCoverDto.FromModel` & `PolisCoverDal.Insert` | Menyimpan detail cakupan peserta ke `dbo.ta_polis_cover` dengan `fd_tgl_expired = model.ExpiredDate` dan kolom audit terisi non-null. |
| `fo-legacy` (`clsPolis`) | `dbo.ta_polis` & `dbo.ta_polis_cover` | Membaca baris polis yang ditulis oleh `b09-bilreg-api` tanpa mengalami `Invalid use of Null` (Error 94). |

# 7. Data Ownership

| Data | Owner |
|---|---|
| `dbo.ta_polis` (kolom bisnis utama: `fs_kd_polis`, `fs_kd_tipe_jaminan`, `fs_no_polis`, `fs_atas_nama`, `fd_expired`, `fs_kd_kelas_ri`, `fb_cover_rj`) | `AdmisiContext.JaminanFeature` (`b09-bilreg-api` & `fo-legacy` selama masa koeksistensi) |
| `dbo.ta_polis` (kolom korporat legacy: `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`) | `fo-legacy` (*read-only* di `b09-bilreg-api`: diinisialisasi `''` saat insert, dipertahankan saat update) |
| `dbo.ta_polis_cover` (`fs_kd_polis`, `fs_mr`, `fs_kd_status`, `fd_tgl_expired`) | `AdmisiContext.JaminanFeature` (`b09-bilreg-api` mengisi `fd_tgl_expired` mengikuti `ta_polis.fd_expired`) |

# 8. Database Design

## New Tables

Tidak ada tabel baru.

## Modified Tables

| Table | Change |
|---|---|
| `ta_polis` (`Bilreg.SqlDb`) | Menyelaraskan definisi DDL `ta_polis.sql` agar mencakup `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, serta `crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr` dengan constraint `NOT NULL DEFAULT`. |
| `ta_polis_cover` (`Bilreg.SqlDb`) | Menyelaraskan definisi DDL `ta_polis_cover.sql` agar mencakup `fd_tgl_expired` (`DEFAULT('3000-01-01')`), ukuran `fs_mr VARCHAR(15)`, serta `crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr` dengan constraint `NOT NULL DEFAULT`. |

## Relationships

- `ta_polis_cover.fs_kd_polis` berelasi ke `ta_polis.fs_kd_polis`.
- `ta_polis_cover.fd_tgl_expired` disinkronkan secara aplikatif dari `ta_polis.fd_expired` saat penyimpanan agregat `PolisModel`.

## Migration Considerations

- Di basis data operasional yang sudah berjalan, tabel `dbo.ta_polis` dan `dbo.ta_polis_cover` sudah memiliki ke-20 dan ke-14 kolom tersebut, tetapi `dbo.ta_polis.FS_NO_PAYROLL` bertipe `varchar(40) NULL` tanpa `DEFAULT`.
- Dengan selalu mengirimkan parameter eksplisit `@fs_no_payroll = ''` pada `PolisDal.Insert` dan `fs_no_payroll = ISNULL(fs_no_payroll, '')` pada `PolisDal.Update`, aplikasi tidak lagi menghasilkan nilai `NULL` meskipun skema fisik di server klien belum memiliki constraint `DEFAULT` pada `FS_NO_PAYROLL`.
- Untuk baris data yang sudah telanjur memiliki `fs_no_payroll IS NULL` (seperti `PS00183968`), pembaruan polis melalui `b09-bilreg-api` atau perbaikan data satu kali (`UPDATE ta_polis SET fs_no_payroll = '' WHERE fs_no_payroll IS NULL`) akan langsung memulihkan kompatibilitas dengan `fo-legacy`.

# 9. Cross-Cutting Concerns

- **Audit Trail Kompatibilitas Legacy**:
  - Format tanggal audit wajib `yyyy-MM-dd` (`VARCHAR(10)`) dan format jam wajib `HH:mm:ss` (`VARCHAR(8)`).
  - Nilai sentinel *not-updated* pada `updtgl` wajib `'3000-01-01'` dan `updjam` wajib `'00:00:00'` agar ekspresi `CASE WHEN updtgl = '3000-01-01' THEN crttgl ELSE updtgl END` pada `clsPolis.GetLastPetugasTglJam()` di `fo-legacy` berfungsi dengan benar.

# 10. Implementation Constraints

- Wajib menggunakan Dapper dan `SqlBulkCopy` sesuai pola eksisting di `Bilreg.Infrastructure`.
- Tidak boleh mengubah tanda tangan konstruktor publik `PolisModel` atau `PolisCoverModel` yang sudah digunakan oleh modul/test lain, atau jika menambahkan parameter pada `PolisCoverDto.FromModel`, wajib menggunakan default parameter agar backward-compatible.
- Seluruh unit/integration test di `Bilreg.Test` (`PolisDalTest`, `PolisCoverDalTest`, `PolisModelTest`, `PolisUpdateHandlerTest`, `PolisControllerTest`) wajib lulus dengan transaksi terisolasi (`TransHelper.NewScope()`).

# 11. Acceptance Conditions

1. `PolisDal.Insert` menulis seluruh 20 kolom `ta_polis` dengan nilai non-null (`fs_kd_bu = ''`, `fs_departemen = ''`, `fs_no_payroll = ''`, `crttgl`/`crtjam` terisi waktu pembuatan, `updtgl = '3000-01-01'`, `updjam = '00:00:00'`).
2. `PolisDal.Update` memperbarui kolom bisnis polis dan kolom audit `upd*`, mempertahankan nilai `fs_kd_bu`, `fs_departemen`, `fs_no_payroll` yang sudah ada (serta mengubahnya menjadi `''` jika sebelumnya `NULL`), dan tidak menimpa `crt*`.
3. `PolisCoverDal.Insert` menulis `fd_tgl_expired` (mengikuti `ta_polis.fd_expired` dari `PolisModel.ExpiredDate`) beserta kolom audit `crt*`/`upd*` ke `ta_polis_cover`.
4. Seluruh pengujian otomatis pada `JaminanFeature` di `Bilreg.Test` berjalan hijau (`PASS`).
