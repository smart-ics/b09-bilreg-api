# BUG-INVESTIGATION

## Context

Issue: [LAB-ORDER-PATIENT-BIRTHDATE-BUG-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PATIENT-BIRTHDATE-BUG-ISSUE.md)
Problem Summary: Isian tanggal lahir pasien pada kartu Identitas Pasien di layar detail order laboratorium (`LabOrderDetailPanel.vue`) keliru menampilkan tanggal registrasi kunjungan (`regDate`) atau nilai fallback default, alih-alih menampilkan tanggal lahir pasien sebenarnya dari data sosial pasien (`PAS-DATSOS` / `tc_mr.fd_tgl_lahir`).

## Current State

1. Pengguna membuka modul Laboratorium dan masuk ke antarmuka pembuatan order laboratorium baru internal (`LabOrderCreateCanvas.vue`).
2. Pengguna mencari dan memilih pasien kunjungan aktif melalui dropdown pencarian registrasi aktif (yang mengambil data dari endpoint `GET /api/reg/listAktif`).
3. Pengguna memilih satu atau beberapa tindakan pemeriksaan laboratorium, melengkapi metadata order, dan menekan tombol **Simpan & Terbitkan Order Lab**.
4. Frontend mengirimkan request `POST /api/LabContext/LabOrderFeature/fromEmr` dengan payload di mana field `birthDate` dan `birthDateYmd` terisi nilai fallback tanggal registrasi (`selectedPatient.regDate`), misalnya `2026-11-19`, karena objek registrasi dari endpoint lookup tidak memuat atribut tanggal lahir.
5. Backend `LabOrderCreateFromEmrHandler` memvalidasi registrasi kunjungan aktif ke repository registrasi (`_regRepo`), namun langsung memetakan `request.BirthDateYmd` klien ke dalam snapshot pasien tanpa rekonsiliasi ke data lahir master registrasi (`reg.Pasien.TglLahir`).
6. Nilai tanggal lahir yang keliru disimpan ke kolom `BirthDate` di tabel basis data `BILRG_LabOrder` dan usia saat order (`AgeAtOrder`) dihitung menjadi `0` tahun.
7. Saat pengguna membuka rincian pesanan lab yang terbentuk pada panel kerja laboratorium (`LabOrderDetailPanel.vue`), panel Identitas Pasien menampilkan tanggal registrasi sebagai tanggal lahir dan usia pasien tercatat 0 tahun.

## Problem Analysis

### 1. Omission pada DTO Query Registrasi Aktif (`RegRegAktifListResponse`)
- Pada layer data access [`RegAktifDal.cs:194-218`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegAktifDal.cs#L194-L218), metode `ListData()` sebenarnya sudah melakukan kueri gabungan `LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr` dan mengekstrak `ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir` serta `ISNULL(bb.fs_jns_kelamin, '-') AS Gender`.
- DTO perantara [`RegAktifDto.cs:12, 38`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegAktifDto.cs#L12) dan model domain `RegAktifModel` (`x.Pasien.TglLahir` bertipe `DateOnly` dan `x.Pasien.Gender`) sudah menyimpan data tersebut secara utuh.
- Namun, record DTO respons [`RegRegAktifListResponse`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegRegAktifListQuery.cs#L7-L9) pada `RegRegAktifListQuery.cs` hanya mengekspos:
  ```csharp
  public record RegRegAktifListResponse(string RegId, string RegDate, string PasienId,
      string PasienName, string JenisReg, string JenisRegString, string LayananId,
      string LayananName, string DokterId, string DokterName, int StatusTataRekening = 0);
  ```
  Field `TglLahir` dan `Gender` dihilangkan (*omitted*) dari konstruktor respons, sehingga klien consumer `api/reg/listAktif` tidak pernah menerima tanggal lahir pasien.
- Pola ini kontras dengan [`RegAktifRajalIgdListQuery.cs:7-22`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegAktifRajalIgdListQuery.cs#L7-L22) yang sudah menyertakan `string TglLahir` dan `string Gender`.

### 2. Fallback Berbahaya pada Pembuatan Payload Frontend (`LabOrderCreateCanvas.vue`)
- Pada [`LabOrderCreateCanvas.vue:372-379`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue#L372-L379):
  ```typescript
  birthDate:
    ((selectedPatient.value as unknown as Record<string, unknown>).birthDate as string) ||
    selectedPatient.value.regDate ||
    '1990-01-01',
  birthDateYmd:
    ((selectedPatient.value as unknown as Record<string, unknown>).birthDate as string) ||
    selectedPatient.value.regDate ||
    '1990-01-01',
  ```
- Skema lookup `labMasterRegItemSchema` di [`useLabMasterLookup.ts:36-55`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/composables/useLabMasterLookup.ts#L36-L55) tidak mendefinisikan atribut tanggal lahir.
- Karena `birthDate` bernilai `undefined`, ekspresi fallback secara diam-diam beralih ke `selectedPatient.value.regDate` (tanggal registrasi kunjungan) dan mengirimkannya ke API backend sebagai tanggal lahir pasien.
- Di samping itu, kartu ringkasan pasien di canvas pembuatan order ([`LabOrderCreateCanvas.vue:872-876`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue#L872-L876)) menampilkan label `"Tgl Registrasi: "` padahal menggunakan penanda tes `data-testid="patient-birth-date"`, menandakan kekeliruan pemahaman atribut pasien pada formulir tersebut.

### 3. Ketiadaan Rekonsiliasi Tanggal Lahir pada Backend Command Handler (`LabOrderCreateFromEmrCmd.cs`)
- Pada [`LabOrderCreateFromEmrCmd.cs:121-160`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs#L121-L160), handler memuat entitas registrasi aktif:
  ```csharp
  var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId)).Match(onSome: x => x, onNone: () => null as RegModel);
  ```
- Entitas `reg.Pasien` ([`PasienReff`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienReff.cs#L4-L7)) memuat data sah: `PasienId`, `PasienName`, `DateOnly TglLahir`, dan `Gender`.
- Namun, saat membangun snapshot order melalui `LabOrderCreateHelper.BuildSnapshot(...)`:
  ```csharp
  var patientId = !string.IsNullOrWhiteSpace(reg?.Pasien?.PasienId) && reg.Pasien.PasienId != "-"
      ? reg.Pasien.PasienId
      : request.PatientId;
  var snapshot = LabOrderCreateHelper.BuildSnapshot(
      request.RegId,
      patientId,
      request.PatientName,
      request.BirthDateYmd,
      request.Gender,
      audit.Timestamp);
  ```
  Handler hanya merekonsiliasi `patientId`, sedangkan `BirthDateYmd`, `PatientName`, dan `Gender` diambil mentah-mentah dari payload permintaan klien tanpa verifikasi silang terhadap entitas registrasi aktif (`reg.Pasien`).
- Akibatnya, nilai tanggal registrasi dari klien diterima tanpa koreksi dan dipersistensikan sebagai tanggal lahir pasien.

### 4. Dampak Distorsi Metadata Klinis (`AgeAtOrder`)
- [`LabOrderCreateHelper.ComputeAgeAtOrder`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateHelper.cs#L43-L49) menghitung usia pasien saat order dibuat dari selisih `orderTimestamp` dengan `birthDate`.
- Karena `birthDate` terisi tanggal hari ini (`regDate`), usia pasien selalu dihitung `0` tahun, mendistorsi demografi pasien pada seluruh dokumen hasil dan histori pemeriksaan laboratorium.

## Affected Components

- **Backend (`b09-bilreg-api`)**:
  - `Bilreg.Application.AdmisiContext.RegFeature.UseCases.RegRegAktifListResponse`: penambahan field `string TglLahir = ""` dan `string Gender = ""` pada respons DTO.
  - `Bilreg.Application.AdmisiContext.RegFeature.UseCases.RegRegAktifHandler`: pemetaan `x.Pasien.TglLahir.ToString("yyyy-MM-dd")` dan `x.Pasien.Gender` dari `RegAktifModel`.
  - `Bilreg.Application.LabContext.LabOrderFeature.UseCases.LabOrderCreateFromEmrHandler`: rekonsiliasi data snapshot pasien terhadap entitas `reg.Pasien` (mengutamakan tanggal lahir dan gender yang sah dari master registrasi pasien bila valid).
  - `Bilreg.Test.AdmisiContext.RegFeature.RegRegAktifListQueryTest`: penambahan/pembaruan unit test verifikasi pemetaan `TglLahir` dan `Gender`.
- **Frontend (`c012_myhospital_web`)**:
  - `c012_myhospital_web/src/modules/Laboratory/composables/useLabMasterLookup.ts`: perluasan `labMasterRegItemSchema` untuk memuat `tglLahir: z.string().optional().default('')`, `birthDate: z.string().optional().default('')`, dan `gender: z.string().optional().default('')`.
  - `c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue`: pemetaan `birthDate` / `birthDateYmd` dan `gender` yang benar dari `selectedPatient` tanpa fallback keliru ke `regDate`, serta perbaikan tampilan informasi tanggal lahir di kartu ringkasan pasien.

## Impact Assessment

- **Business Impact**: Menjamin keakuratan data identitas pasien pada seluruh siklus order laboratorium. Mencegah kesalahan interpretasi rentang nilai rujukan (*reference range*) klinis yang bergantung pada usia dan jenis kelamin pasien.
- **Operational Impact**: Petugas laboratorium dan analis patologi menerima data demografi pasien yang valid tanpa kebingungan saat mencocokkan identitas pasien dengan berkas rekam medis.
- **Technical Impact**: Perubahan bersifat backward-compatible pada query DTO `api/reg/listAktif` dan memperkuat validasi domain handler pembuatan order lab tanpa membutuhkan modifikasi skema tabel basis data.

## Assumptions

- **ASM-BUG-001**: Sumber kebenaran tanggal lahir dan jenis kelamin pasien internal rumah sakit tersimpan di tabel `tc_mr` (`fd_tgl_lahir`, `fs_jns_kelamin`) dan diakses melalui agregat `RegModel` (`reg.Pasien.TglLahir`, `reg.Pasien.Gender`) atau `RegAktifModel`.
- **ASM-BUG-002**: Untuk pasien luar walk-in (`OrderSource == 4` / `LabExternalRegistrationCanvas`), tanggal lahir diisi manual oleh petugas melalui formulir pendaftaran luar dan tidak terikat pada kunjungan `ta_registrasi`.
- **ASM-BUG-003**: Penambahan field `TglLahir` dan `Gender` pada DTO `RegRegAktifListResponse` bersifat backward-compatible dan tidak mengganggu consumer lain dari endpoint `api/reg/listAktif`.

## Open Questions

Tidak ada pertanyaan terbuka (*Zero open questions*). Alur transmisi data dari persistence DAL hingga binding UI telah ditelusuri dan diidentifikasi secara tuntas.

## Recommended Decision

Lakukan perbaikan terintegrasi dua lapis (*two-tier alignment*):
1. **Lapisan Penyaluran Data (Data Exposure)**: Paparkan data tanggal lahir (`TglLahir`) dan jenis kelamin (`Gender`) dari `RegAktifModel` pada DTO `RegRegAktifListResponse` di `b09-bilreg-api`, serta petakan ke dalam skema lookup `useLabMasterLookup.ts` dan form binding `LabOrderCreateCanvas.vue` di `c012_myhospital_web`.
2. **Lapisan Integritas Domain (Domain Snapshot Guard)**: Perkuat `LabOrderCreateFromEmrHandler` di `b09-bilreg-api` untuk merekonsiliasi tanggal lahir dan jenis kelamin snapshot dari entitas `reg.Pasien` yang sudah dimuat dari basis data ketika pesanan dibuat dari kunjungan registrasi internal.

## Decision

Setujui perbaikan terintegrasi: paparkan tanggal lahir pasien pada lookup registrasi aktif dan perbarui penanganan payload di frontend canvas, sekaligus perkuat handler backend pembuatan order lab internal agar memvalidasi dan memprioritaskan tanggal lahir serta gender kanonikal dari registrasi kunjungan aktif pasien.

## Decision Rationale

1. **Ketahanan Ganda (Defense in Depth)**: Memperbaiki sisi frontend lookup memastikan form UI dapat menampilkan tanggal lahir pasien secara transparan sebelum order dikirim, sementara rekonsiliasi di backend handler menjamin integritas data snapshot order tidak dapat dirusak oleh payload klien mana pun.
2. **Nol Perubahan Skema Basis Data**: Kolom basis data `BILRG_LabOrder.BirthDate` dan kueri DAL `RegAktifDal` telah memiliki data `fd_tgl_lahir`; perbaikan hanya menghubungkan data yang sudah ada di persistence ke layer use case dan UI.
3. **Penyelarasan Standar Kontrak**: Menyelaraskan endpoint `reg/listAktif` dengan endpoint `reg/aktif/rajal-igd` yang sudah lebih dahulu memuat `TglLahir` dan `Gender`.

## Architecture Applicability

### Decision

**`ARCHITECTURE-NOT-REQUIRED`**

### Rationale

Perbaikan ini merupakan koreksi defek aliran data dan rekonsiliasi use case DTO/handler terisolasi:
- Tidak memperkenalkan bounded context baru atau mengubah batas context yang ada.
- Tidak memodifikasi skema tabel database (`BILRG_LabOrder`, `BILRG_RegAktif`, `ta_registrasi`, `tc_mr`).
- Tidak mengubah state machine atau aturan transisi status pesanan laboratorium.
- Detail teknis sudah sangat jelas dan dapat langsung dieksekusi melalui perumusan Implementation Plan.
