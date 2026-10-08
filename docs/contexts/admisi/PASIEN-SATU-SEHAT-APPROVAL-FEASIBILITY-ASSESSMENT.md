---
Title: Feasibility Assessment - Integrasi Data Status Satu Sehat Pasien dan Konfirmasi Persetujuan Upload pada Registrasi Admisi
Code: PASIEN-SATU-SEHAT-APPROVAL
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-10-07
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan perubahan (CHANGE-REQUEST) terkait integrasi data status Satu Sehat pasien (`tc_mr_saset`) ke dalam agregat data sosial pasien di sistem Bilreg (`b09-bilreg-api`), penyediaan endpoint persetujuan upload Satu Sehat, serta visualisasi indikator status Satu Sehat dan otomatisasi alur konfirmasi persetujuan upload berbasis grup jaminan pasien (BPJS vs Non-BPJS) pada antarmuka registrasi Admisi (`c012_myhospital_web`).

Referenced artifacts:

- ISSUE: [PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md) (`ISSUE-ADMISI-PASIEN-SASET-001`)
- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)

## Objective

Menilai kelayakan teknis, operasional, arsitektural, dan dampak implementasi dari penambahan kapabilitas integrasi Satu Sehat pada agregat pasien di backend Bilreg dan alur konfirmasi persetujuan upload pada antarmuka registrasi pasien di loket Admisi; mengidentifikasi kesenjangan (gaps), pertanyaan terbuka (open questions), asumsi, risiko, serta mendokumentasikan keputusan resmi penutupan celah (*Gap Closure*) sebelum perancangan arsitektur teknis difinalisasi oleh arsitek.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan investigasi artefak dan kode sumber:

## Existing Behavior

1. **Agregat Data Pasien di Domain Backend (`PasienModel`)**:
   - Model pasien ([PasienModel.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs)) mengelola informasi demografi, kontak, identitas KTP, kartu keluarga, dan status sosial.
   - Belum terdapat entitas, value object, maupun sub-agregat untuk merepresentasikan data integrasi Satu Sehat pasien (`PasienSasetModel`).

2. **Akses Data dan Persistensi Pasien (`PasienRepo` & DAL)**:
   - Data pasien disimpan di tabel `tc_mr`, `tc_mr_ktp`, `tc_mr_telp`, dan `tc_mr_id` yang dikelola secara terpisah melalui DAL masing-masing ([PasienDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienDal.cs), [PasienKtpDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienKtpDal.cs), dll.).
   - Tabel `tc_mr_saset` telah ada di skema database operasional dengan relasi 1-to-1 berbasis `KodeMr`, namun pada kode sumber `b09-bilreg-api` belum terdapat komponen DAL (`PasienSasetDal`) maupun DTO (`PasienSasetDto`) untuk tabel ini.
   - Pada [PasienRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienRepo.cs), proses `LoadEntity` belum membaca data dari `tc_mr_saset` dan `SaveChanges` belum melakukan penyimpanan ke `tc_mr_saset`.

3. **Query Detail Pasien (`PasienGetQuery`)**:
   - Endpoint query `PasienGetQuery` ([PasienGetQuery.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs)) mengembalikan `PasienGetResponse`.
   - Kontrak response saat ini belum memuat data status Satu Sehat (`PasienSasetResponse` atau atribut Satu Sehat terkait).

4. **Usecase Command dan Controller Pasien**:
   - [PasienController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs) hanya menyediakan endpoint `create`, `createByKtp`, `GetData`, `search`, `ktp`, `addContact`, `demografi`, `nonActive`, dan `reActive`.
   - Belum tersedia command usecase `PasienApproveUploadSasetCmd` maupun rute controller untuk menyetujui upload Satu Sehat.

5. **Kontrak Data Frontend Pasien (`pasien.ts`)**:
   - Skema Zod [pasien.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/BillingBase/types/pasien.ts) mendefinisikan `dataSosialPasienSchemaNew`.
   - Skema ini belum menyertakan properti Satu Sehat (`pasienSaset`), sehingga response backend yang memuat data Satu Sehat belum dapat divalidasi dan dikonsumsi oleh komponen frontend.

6. **Antarmuka Registrasi Admisi (`LegacyRegistrationWorkspace.vue`)**:
   - Kotak ringkasan pasien ([LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue) baris 150–280) menampilkan Avatar (No. Antrian / Inisial), Nama Pasien, Icon Gender, Nomor MR, Tanggal Lahir & Umur, Alamat Domisili, serta tombol Ubah Data Pasien.
   - Belum terdapat indikator/label `#SatuSehat` pada kartu ringkasan pasien tersebut.

7. **Alur Simpan Registrasi pada Frontend (`useRegistrasiActions.ts`)**:
   - Tombol **Simpan Registrasi** mengeksekusi handler `handleSubmitRegister` di [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts).
   - Validasi pra-simpan saat ini hanya memeriksa kelengkapan data eligibilitas (`isEligibilityMissing`) dan kelengkapan NIK (`isNikMissing`).
   - Sistem belum melakukan pengecekan status persetujuan Satu Sehat pasien (`IsApprovedUpload`), belum memvalidasi grup jaminan via API eksternal JetliApi (`/JetliAPi/api/GrupJaminan/map`), dan belum menyediakan pop-up dialog konfirmasi persetujuan upload untuk pasien non-BPJS.

## Existing Constraints

1. **Non-blocking Fault Tolerance**:
   - Pemanggilan endpoint persetujuan upload Satu Sehat (`PasienApproveUploadSasetCmd`) maupun pengecekan mapping jaminan ke JetliApi tidak boleh memblokir atau menggagalkan alur simpan registrasi pasien di loket admisi rumah sakit. Apabila koneksi gagal atau timeout, pendaftaran pasien tetap harus tersimpan.
2. **Kepatuhan Regulasi Jaminan BPJS**:
   - Pasien dengan jaminan BPJS Kesehatan memiliki kewajiban regulasi untuk pengiriman rekam medis ke platform Satu Sehat Kemenkes, sehingga persetujuan upload data Satu Sehat dilakukan secara otomatis oleh sistem tanpa memerlukan dialog konfirmasi manual dari petugas/pasien.
3. **Persetujuan Pasien Non-BPJS (Consent)**:
   - Pasien umum dan asuransi non-BPJS berhak atas pemberian persetujuan (*informed consent*) pemrosesan dan pengiriman data ke Satu Sehat. Sistem wajib meminta konfirmasi petugas/pasien terlebih dahulu. Jika pasien/petugas menolak/membatalkan, proses simpan registrasi tetap berjalan normal tanpa persetujuan upload.
4. **Relasi Data 1-to-1 pada `tc_mr_saset`**:
   - Tabel `tc_mr_saset` terikat dengan kunci utama `KodeMr` yang berkorespondensi langsung dengan `PasienId`. Entitas harus mendukung skenario di mana pasien lama belum memiliki baris di `tc_mr_saset` (butuh auto-create/insert saat persetujuan pertama kali dilakukan).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | Ketiadaan model domain `PasienSasetModel` dan komponen DAL/DTO `tc_mr_saset` pada backend `b09-bilreg-api`. |
| GAP-002 | CRITICAL | `PasienRepo` belum mengintegrasikan pemuatan (`LoadEntity`) dan penyimpanan (`SaveChanges`) untuk data Satu Sehat pasien. |
| GAP-003 | CRITICAL | Ketiadaan kontrak data Satu Sehat pada respon query data pasien (`PasienGetQuery` / `PasienGetResponse`). |
| GAP-004 | CRITICAL | Ketiadaan use case command `PasienApproveUploadSasetCmd` dan endpoint API terkait di `PasienController`. |
| GAP-005 | MAJOR | Ketiadaan tipe data Satu Sehat pada skema frontend `dataSosialPasienSchemaNew` di `pasien.ts` dan fungsi pemanggilan approval di `PasienService.ts`. |
| GAP-006 | MAJOR | Antarmuka kartu ringkasan pasien di `LegacyRegistrationWorkspace.vue` belum menampilkan visual indikator status `#SatuSehat` (hijau jika disetujui, abu-abu jika belum). |
| GAP-007 | MAJOR | Alur simpan registrasi di `useRegistrasiActions.ts` belum memiliki mekanisme pra-simpan untuk memeriksa status `IsApprovedUpload` dan mengecek pemetaan grup jaminan BPJS via `/JetliAPi/api/GrupJaminan/map`. |
| GAP-008 | MAJOR | Belum tersedianya komponen dialog konfirmasi persetujuan upload Satu Sehat untuk pasien non-BPJS pada modul Admisi. |
| GAP-009 | MINOR | Belum adanya penanganan fallback non-blocking terstandar saat approval command atau query grup jaminan mengalami network timeout/kegagalan. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| OQ-001 | Jika data pasien di `tc_mr_saset` belum ada saat dipanggil `PasienApproveUploadSasetCmd`, apakah use case langsung melakukan operasi Upsert (membuat record baru dengan `KodeSaset = "-"`, `IsApprovedUpload = true`, dan `PasienId = pasienId` pada registrasi)? | Menentukan logika idempotensi dan mekanisme `InsertOrUpdate` pada DAL dan Handler command. |
| OQ-002 | Bagaimana format respons dari `PasienApproveUploadSasetCmd`? | Menentukan kontrak respon HTTP endpoint di controller dan penerimaan pada layer service frontend. |
| OQ-003 | Apakah pemanggilan endpoint eksternal `/JetliAPi/api/GrupJaminan/map` perlu di-cache di sisi client? | Menentukan optimasi performa latensi dan frekuensi pemanggilan HTTP ke JetliApi saat proses registrasi. |
| OQ-004 | Bagaimana penanganan jika terjadi timeout saat mengakses JetliApi untuk mengambil mapping `GrupJaminan`? | Menentukan perilaku fail-safe agar antrean loket registrasi tidak terhenti. |
| OQ-005 | Apakah endpoint `PasienApproveUploadSasetCmd` membutuhkan parameter `UserId` petugas untuk audit log, atau cukup menerima parameter identitas pasien? | Menentukan keselarasan arsitektur payload command dengan endpoint serupa seperti `PasienAddContactCommand`. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Tabel database `tc_mr_saset` sudah tersedia pada database operasional dengan skema kolom: `KodeMr`, `KodeSaset`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView`. |
| ASM-002 | Base URL untuk `jetliApi` sudah terkonfigurasi pada `c012_myhospital_web` dan dapat diakses dengan metode GET `/api/GrupJaminan/map?tipeJaminanId={id}`. |
| ASM-003 | Respons status HTTP 200 dengan data mapping yang valid dari `/JetliAPi/api/GrupJaminan/map` menandakan bahwa penjamin tersebut adalah grup BPJS Kesehatan. Jika respons kosong, tidak ada data, atau 404, maka pasien dianggap Non-BPJS. |
| ASM-004 | Pasien BPJS memiliki mandat regulasi kewajiban upload data kesehatan ke Satu Sehat Kemenkes sehingga tidak memerlukan konfirmasi persetujuan manual dari pengguna. |
| ASM-005 | Kegagalan pemanggilan `PasienApproveUploadSasetCmd` tidak boleh membatalkan penyimpanan registrasi pasien (alur pendaftaran tetap berjalan sukses). |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Additional Latency pada Simpan Registrasi: Rangkaian pemanggilan network berurutan (Cek Grup Jaminan -> Eksekusi Approval Satu Sehat -> Simpan Registrasi) dapat memperlambat respon tombol Simpan Registrasi. | Menurunkan kecepatan pelayanan loket admisi saat jam sibuk. | Hasil mapping grup jaminan di-cache di client per `tipeJaminanId`; jika terjadi timeout JetliApi, proses konfirmasi di-skip dan registrasi langsung diproses. |
| RISK-002 | Data Inconsistency Record `tc_mr_saset`: Pasien lama yang belum pernah terdata di tabel `tc_mr_saset` dapat memicu `NullReferenceException` atau *record not found* saat proses load atau update. | Kegagalan query detail data pasien atau kegagalan command persetujuan. | DAL dan Repo wajib mengembalikan model default yang aman jika record di `tc_mr_saset` belum ada, serta melakukan `Upsert` (insert jika belum ada, update jika sudah ada) saat persetujuan dilakukan. |
| RISK-003 | Flaky External Dependency (`JetliApi` Down): Jika service JetliApi mengalami kendala, pengecekan grup jaminan berpotensi melempar unhandled error. | Menggagalkan proses registrasi pasien di loket. | Bungkus pemanggilan JetliApi dalam blok `try-catch` dengan timeout pendek; jika timeout/gagal, lewati (skip) konfirmasi dan langsung lanjutkan simpan registrasi. |

---

# 7. Recommendations

## Option A: Client-Side Orchestration (Frontend Pre-Submit Interceptor) (SELECTED)

Frontend (`useRegistrasiActions.ts`) mengorkestrasi evaluasi pra-simpan:
1. Sebelum mutasi registrasi dipanggil, periksa apakah `pasienDetail.pasienSaset?.isApprovedUpload` bernilai false / belum ada.
2. Jika sudah disetujui (`true`), lanjutkan simpan registrasi seperti biasa.
3. Jika belum disetujui (`false`):
   - Ambil pemetaan grup jaminan (menggunakan cache di client berdasarkan `tipeJaminanId` jika sudah pernah diambil).
   - Jika JetliApi timeout/error: lewati konfirmasi dan langsung simpan registrasi (evaluasi akan berulang pada kunjungan berikutnya).
   - Jika terdeteksi BPJS: otomatis panggil `PasienApproveUploadSasetCmd` secara non-blocking tanpa memunculkan dialog, lalu lanjutkan simpan registrasi.
   - Jika terdeteksi Non-BPJS: tampilkan dialog konfirmasi persetujuan upload Satu Sehat. Jika disetujui (OK), panggil `PasienApproveUploadSasetCmd` lalu simpan registrasi. Jika dibatalkan/ditolak, lewati pemanggilan command dan langsung simpan registrasi.
4. Seluruh pemanggilan approval dibungkus blok non-blocking agar simpan registrasi tetap berhasil meskipun approval gagal.

### Advantages

- Memisahkan secara bersih interaksi antarmuka pengguna (dialog konfirmasi) dari transaksi database backend.
- Backend usecase registrasi (`RegJalanWalkInCommand`, dll.) tetap bersih dan tidak terikat dengan logika dialog konfirmasi loket.
- Memberikan kontrol fleksibel atas toleransi kegagalan dan visual feedback kepada petugas loket secara langsung.

### Disadvantages

- Memerlukan koordinasi pemanggilan API dari sisi frontend yang dilindungi error handling defensif.

---

## Option B: Backend-Side Orchestrated Auto-Approval inside Registration Handler

Backend use case registrasi mengecek jenis jaminan dan langsung melakukan update ke `tc_mr_saset` saat registrasi disimpan.

---

# 8. Gap Closure

Resolusi definitif seluruh GAPs dan Open Questions berdasarkan investigasi sistem dan keputusan resmi:

## GAP-001

### Status: CLOSED
### Decision
Buat entitas domain `PasienSasetModel` di `Bilreg.Domain.PasienContext.PasienFeature` dengan properti: `PasienId`, `SasetId`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView`. Buat interface `IPasienSasetDal`, implementasi `PasienSasetDal`, dan `PasienSasetDto` di `Bilreg.Infrastructure.PasienContext.PasienFeature` untuk mengelola tabel `tc_mr_saset`.

### Rationale
Diperlukan komponen representasi data dan persistensi data Satu Sehat yang terisolasi dan konsisten dengan arsitektur DAL berbasis Dapper di Bilreg.

### Impact
Domain Pasien memiliki kapabilitas data Satu Sehat dan layer infrastruktur dapat berinteraksi langsung dengan tabel `tc_mr_saset`.

### Architecture Impact
Penambahan file `PasienSasetModel.cs`, `PasienSasetDal.cs`, dan `PasienSasetDto.cs` serta registrasi DI untuk `IPasienSasetDal`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## GAP-002

### Status: CLOSED
### Decision
Perluas [PasienRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienRepo.cs) untuk menginjeksi `IPasienSasetDal`. Pada `LoadEntity`, repo memuat data dari `_pasienSasetDal.GetData(key)` (mengembalikan entitas default yang aman jika belum ada). Pada `SaveChanges`, repo melakukan penyimpanan data Satu Sehat ke `tc_mr_saset` di dalam lingkup `TransHelper.NewScope()`.

### Rationale
Menjaga konsistensi agregat pasien di mana semua data turunan rekam medis dimuat dan disimpan melalui repositori tunggal `IPasienRepo`.

### Impact
Semua siklus hidup agregat `PasienModel` secara transparan memuat dan menyimpan data status Satu Sehat.

### Architecture Impact
`PasienRepo` mengorkestrasikan persistensi `tc_mr_saset` bersama dengan `tc_mr`, `tc_mr_ktp`, `tc_mr_telp`, dan `tc_mr_id`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## GAP-003

### Status: CLOSED
### Decision
Perluas kontrak respons `PasienGetResponse` pada [PasienGetQuery.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs) untuk menyertakan objek data `PasienSaset` (`SasetId`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView`).

### Rationale
Memungkinkan seluruh klien (frontend web Admisi) menerima status lengkap Satu Sehat pasien dalam satu kali pemanggilan query detail pasien.

### Impact
Respon JSON dari `{GET} /api/pasien/{id}` memuat field `pasienSaset`.

### Architecture Impact
Modifikasi record `PasienGetResponse` dan mapping di `PasienGetHandler`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## GAP-004

### Status: CLOSED
### Decision
Buat use case command `PasienApproveUploadSasetCmd(string PasienId) : IRequest, IPasienKey` di `Bilreg.Application.PasienContext.PasienFeature`. Tambahkan rute endpoint pada [PasienController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs):
```csharp
[HttpPatch]
[Route("approveUploadSaset")]
public async Task<IActionResult> ApproveUploadSaset(PasienApproveUploadSasetCmd cmd)
{
    await _mediator.Send(cmd);
    return Ok(new JSendOk("Done"));
}
```

### Rationale
Sesuai konfirmasi user pada OQ-002, pola respons mengikuti endpoint existing seperti `PasienAddContactCommand` yang mengembalikan `"Done"`.

### Impact
Tersedia endpoint khusus yang ringan dan terstandar untuk memperbarui status persetujuan upload Satu Sehat.

### Architecture Impact
Penambahan file usecase `PasienApproveUploadSasetCmd.cs` dan method handler di `PasienController`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## GAP-005

### Status: CLOSED
### Decision
Perluas skema `dataSosialPasienSchemaNew` pada [pasien.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/BillingBase/types/pasien.ts) dengan menambahkan field `pasienSaset`:
```typescript
pasienSaset: z.object({
  sasetId: z.string(),
  isApprovedUpload: z.boolean(),
  tglJamApprovedUpload: z.string(),
  fileGeneralConcentUpload: z.string(),
  isApprovedView: z.boolean(),
  tglJamApprovedView: z.string(),
  fileGeneralConcentView: z.string(),
}).nullable().optional(),
```
Tambahkan mutation hook `useApproveUploadSaset` pada [PasienService.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/BillingBase/services/PasienService.ts).

### Rationale
Memastikan parsing respon API aman via Zod dan menyediakan abstraction service untuk pemanggilan endpoint approval dari modul UI.

### Impact
Frontend dapat mengenali tipe data Satu Sehat pasien dan mengeksekusi mutasi persetujuan.

### Architecture Impact
Pembaruan schema tipe pasien dan ekspansi servis `PasienService`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## GAP-006

### Status: CLOSED
### Decision
Pada komponen [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue), tampilkan indikator teks `#SatuSehat` pada kartu ringkasan pasien:
- Teks berwarna **hijau** (`text-emerald-600 font-semibold`) jika `pasienDetail.pasienSaset?.isApprovedUpload === true`.
- Teks berwarna **abu-abu** (`text-slate-400 font-normal`) jika `isApprovedUpload === false` atau objek `pasienSaset` bernilai null/undefined.

### Rationale
Memberikan visibilitas langsung kepada petugas pendaftaran loket admisi mengenai status persetujuan Satu Sehat pasien sesuai mockup bukti kebutuhan.

### Impact
Petugas loket dapat langsung melihat status kesiapan upload Satu Sehat pasien di antarmuka registrasi.

### Architecture Impact
Pembaruan template kartu data pasien di `LegacyRegistrationWorkspace.vue`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## GAP-007 & GAP-008

### Status: CLOSED
### Decision
Di dalam [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts), sebelum mutasi registrasi dieksekusi:
1. Periksa apakah `pasienDetail?.pasienSaset?.isApprovedUpload` bernilai false / belum ada.
2. Jika bernilai false:
   - Ambil mapping grup jaminan dari JetliApi (dengan pengecekan cache client).
   - Jika BPJS: otomatis panggil `PasienApproveUploadSasetCmd` tanpa memunculkan dialog konfirmasi.
   - Jika Non-BPJS: tampilkan komponen pop-up dialog konfirmasi persetujuan upload. Jika petugas memilih OK, panggil `PasienApproveUploadSasetCmd`; jika membatalkan/menolak, proses simpan registrasi tetap dilanjutkan tanpa approval command.

### Rationale
Memenuhi seluruh aturan bisnis pembedaan alur antara pasien wajib upload (BPJS) dan pasien yang memerlukan informed consent (Non-BPJS).

### Impact
Alur simpan registrasi menjadi cerdas dan otomatis memandu petugas sesuai jenis jaminan pasien.

### Architecture Impact
Penambahan composable dialog konfirmasi di modul Admisi dan interseptor pra-simpan di `useRegistrasiActions.ts`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## GAP-009

### Status: CLOSED
### Decision
Seluruh pemanggilan ke `PasienApproveUploadSasetCmd` maupun pemanggilan ke `/JetliAPi/api/GrupJaminan/map` diisolasi dalam blok proteksi `try-catch` dengan timeout pendek. Jika pemanggilan gagal, sistem menampilkan notifikasi peringatan (toast warning) dan langsung melanjutkan proses penyimpanan registrasi pasien hingga tuntas.

### Rationale
Proses pelayanan administrasi registrasi pasien di rumah sakit bersifat misi-kritis dan tidak boleh macet hanya karena gangguan koneksi ke layanan penunjang Satu Sehat atau JetliApi.

### Impact
Keandalan sistem tetap terjamin tinggi (zero-blockage).

### Architecture Impact
Penerapan pola *fail-safe / resilient execution* pada composable action registrasi.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## OQ-001

### Status: CLOSED
### Decision
Untuk pasien lama yang belum pernah memiliki baris data di tabel `tc_mr_saset`, pemanggilan `PasienApproveUploadSasetCmd` langsung melakukan operasi **Upsert** dengan menginisialisasi record baru: `PasienId = request.PasienId`, `KodeSaset = "-"`, `IsApprovedUpload = true`, `TglJamApprovedUpload = Now`, dan field lainnya default.

### Rationale
Sesuai arahan eksplisit user: pasien lama yang belum pernah memiliki record `tc_mr_saset` harus dibuatkan record baru saat pertama kali disetujui tanpa perlu sinkronisasi awal terpisah.

### Impact
DAL dan Repo menangani pembuatan record baru jika record lama belum ditemukan (`Upsert`), mencegah error *record not found*.

### Architecture Impact
Metode penyimpanan Satu Sehat pada DAL/Repo mengecek keberadaan data eksisting sebelum memutuskan operasi `Insert` atau `Update`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## OQ-002

### Status: CLOSED
### Decision
Format respons dari endpoint `PasienApproveUploadSasetCmd` adalah string standar `"DONE"` yang dibungkus oleh `JSendOk("Done")`, serupa dengan endpoint mutasi sederhana lain seperti `PasienAddContactCommand`.

### Rationale
Sesuai arahan eksplisit user: karena command dijalankan di akhir pengisian form registrasi dan tepat sebelum simpan registrasi, respons sederhana `"DONE"` sudah mencukupi dan konsisten dengan konvensi API eksisting di Bilreg.

### Impact
Kontrak return endpoint berukuran minimal dan tidak membebani payload jaringan.

### Architecture Impact
Usecase command mengimplementasikan `IRequest` (tanpa type return khusus) dan controller mengembalikan `JSendOk("Done")`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## OQ-003

### Status: CLOSED
### Decision
Respons pemetaan grup jaminan dari `/JetliAPi/api/GrupJaminan/map` wajib di-cache di sisi client (frontend) berdasarkan `tipeJaminanId`.

### Rationale
Sesuai arahan eksplisit user: mencegah redundansi pemanggilan HTTP berulang ke JetliApi saat pengguna meregistrasikan pasien atau saat memilih jenis jaminan yang sama, sehingga menekan latensi klik simpan registrasi.

### Impact
Pengurangan signifikan pada traffic network ke JetliApi dan eksekusi pra-simpan menjadi instan jika grup jaminan sudah pernah ter-cache.

### Architecture Impact
Penerapan client-side caching (in-memory map atau TanStack Query cache) pada composable / service jaminan.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## OQ-004

### Status: CLOSED
### Decision
Jika terjadi timeout atau kegagalan koneksi saat mengakses JetliApi untuk mengambil mapping grup jaminan, sistem **melewati (skip) proses konfirmasi** dan langsung melanjutkan proses simpan registrasi.

### Rationale
Sesuai arahan eksplisit user: setiap kunjungan pasien dengan kondisi pasien non-BPJS dan flag `IsApprovedUpload = false` akan otomatis dievaluasi dan dikonfirmasi ulang pada kesempatan berikutnya, sehingga kegagalan sementara pada JetliApi tidak boleh menghambat pelayanan loket.

### Impact
Alur loket registrasi tetap lancar tanpa jeda waktu tunggu (freeze) saat JetliApi mengalami kendala.

### Architecture Impact
Handler pra-simpan menangkap error/timeout JetliApi dan langsung memintas (*bypass*) alur konfirmasi menuju simpan registrasi.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

## OQ-005

### Status: CLOSED
### Decision
Command `PasienApproveUploadSasetCmd` hanya menerima parameter `string PasienId` tanpa memerlukan parameter `UserId`.

### Rationale
Konsisten dengan keputusan OQ-001 dan OQ-002 serta pola endpoint mutasi agregat pasien eksisting (`PasienModel.Key`).

### Impact
Kontrak request command ringkas dan mudah dikonsumsi dari frontend.

### Architecture Impact
`PasienApproveUploadSasetCmd` didefinisikan sebagai `record PasienApproveUploadSasetCmd(string PasienId) : IRequest, IPasienKey`.

### Resolved By
User & ica-analyst

### Resolved Date
2026-10-07

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Perubahan ini mencakup:
1. Pembuatan entitas/komponen baru di Domain, Infrastructure, Application, dan Presentation layer backend (`PasienSasetModel`, `PasienSasetDal`, `PasienApproveUploadSasetCmd`).
2. Modifikasi persistensi dan skema agregat `PasienModel` di `PasienRepo` yang melibatkan tabel `tc_mr_saset` dengan pola *Upsert*.
3. Perluasan kontrak DTO respon query publik `PasienGetResponse`.
4. Integrasi multi-service lintas sistem di frontend antara modul Admisi Bilreg dan JetliApi (`/JetliAPi/api/GrupJaminan/map`) dengan mekanisme client-side caching.
5. Perubahan alur operasional loket admisi dengan penambahan dialog konfirmasi, indikator visual `#SatuSehat`, dan interseptor pra-simpan registrasi yang tangguh terhadap kegagalan jaringan.

Oleh karena itu, diperlukan perancangan arsitektur formal (**ARCHITECTURE-REQUIRED**) oleh peran `ica-architect`.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status

**READY-FOR-PLANNING**

## Notes

Seluruh kesenjangan kritis (GAP-001 s/d GAP-009) dan seluruh pertanyaan terbuka (OQ-001 s/d OQ-005) telah ditutup secara definitif dan dicatat secara formal dalam dokumen ini oleh peran `ica-analyst`.

Sesuai manifesto SDLC:
- Ceklis kesiapan telah diperbarui untuk merefleksikan bahwa seluruh celah dan keputusan telah tuntas.
- Status kelayakan tetap dipertahankan sebagai **NOT-READY** hingga peran `ica-architect` memverifikasi arsitektur teknis dan memberikan gerbang kelayakan resmi (**READY-FOR-PLANNING**).

---

# 11. References

Referenced artifacts:

- ISSUE: [PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb\b09-bilreg-api\docs\contexts\admisi\PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md)
- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb\b09-bilreg-api\docs\contexts\admisi-rajal\admisi-rajal-domain.md)
- Evidence UI Mockup: [evidence/pasien-satusehat-ui-box.png](file:///d:/project_aktif/project_MyHospitalWeb\b09-bilreg-api\docs\contexts\admisi\evidence\pasien-satusehat-ui-box.png)

Referenced codebase locations:

- [PasienModel.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs)
- [PasienRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienRepo.cs)
- [PasienDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienDal.cs)
- [PasienGetQuery.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs)
- [PasienController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs)
- [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue)
- [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts)
- [useRegistrasiPasien.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiPasien.ts)
- [PasienService.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/BillingBase/services/PasienService.ts)
- [pasien.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/BillingBase/types/pasien.ts)
