# BUG-INVESTIGATION

## Context

Issue: [PASIEN-SATU-SEHAT-APPROVAL-BUG-001-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-001-ISSUE.md)
Problem Summary: Pop-up dialog "Konfirmasi Persetujuan Satu Sehat" tidak muncul ke layar petugas admisi saat tombol "Simpan Registrasi" ditekan untuk pasien dengan jaminan Non-BPJS (Umum / Tunai `tipeJaminanId: "00000"`) yang status Satu Sehat-nya belum disetujui (`isApprovedUpload = false`), sehingga sistem langsung menyimpan pendaftaran tanpa proses informed consent.

## Current State

Pada antarmuka Pendaftaran Rawat Jalan (Admisi):
1. Petugas memilih pasien yang belum disetujui Satu Sehat (indikator visual `#SatuSehat` berwarna abu-abu).
2. Petugas memilih Jenis Penjamin "Umum / Tunai" (`tipeJaminanId: "00000"`).
3. Saat tombol "Simpan Registrasi" diklik, sistem mengeksekusi fungsi `checkIsBpjsJaminan("00000")` untuk memverifikasi apakah jaminan tersebut BPJS atau Non-BPJS melalui endpoint JetliApi:
   `GET http://dev.smart-ics.com:8888/JknTrustedLink/api/GrupJaminan/map?tipeJaminanId=00000`
4. Layanan eksternal JetliApi mengembalikan respons HTTP `400 Bad Request` dengan body:
   ```json
   {
     "status": "Data Not Found",
     "code": "400",
     "data": "Data Mapping Group Jaminan tidak ditemukan"
   }
   ```
5. Klien HTTP Axios memperlakukan status HTTP 400 sebagai error/rejection, memicu blok `catch` pada fungsi `checkIsBpjsJaminan`.
6. Blok `catch` saat ini mengasumsikan kegagalan sebagai technical failure / network timeout dan mengembalikan nilai `null` (*fail-safe*).
7. Pada alur evaluasi di `handleSubmitRegister`:
   - Kondisi `if (isBpjs === true)` bernilai false.
   - Kondisi `else if (isBpjs === false)` bernilai false (karena `isBpjs` bernilai `null`).
   - Sistem menganggap kondisi ini sebagai kegagalan teknis jaminan (*fail-safe*) dan langsung melompati (*bypasses*) dialog konfirmasi, lalu mengeksekusi simpan registrasi pasien.

## Problem Analysis

### Temuan Analisis Alur & Bukti Eksekusi

1. **Karakteristik Kontrak API Eksternal JetliApi (`/GrupJaminan/map`)**:
   - Berdasarkan observasi network dan pengujian integrasi riil, API JetliApi tidak mengembalikan respons HTTP 200 dengan nilai `null`, melainkan mengembalikan kode status HTTP non-200 (spesifiknya HTTP 400 Bad Request atau 404 Not Found) dengan payload JSON:
     `{"status": "Data Not Found", "code": "400", "data": "Data Mapping Group Jaminan tidak ditemukan"}` ketika suatu `tipeJaminanId` tidak dipetakan ke BPJS.
   - Respons ini adalah representasi bisnis yang valid dari JetliApi yang menyatakan: **"Jaminan ini bukan jaminan BPJS"** (Non-BPJS).

2. **Celah pada Logika Penanganan Error (`useRegistrasiActions.ts`)**:
   - Pemanggilan `ApiService.get` berada dalam blok `try { ... } catch (err) { return null }`.
   - `ApiService.handleAxiosError` membungkus error Axios menjadi `ApiError` yang mempertahankan `status: 400` atau `404` beserta payload respons aslinya (`data`).
   - Karena `checkIsBpjsJaminan` menangkap seluruh error tanpa membedakan antara:
     - **Respons Bisnis "Data Not Found"** (HTTP 400/404 dengan pesan "Data Not Found" / status bukan BPJS), versus
     - **Gangguan Teknis / Jaringan Riil** (Network error, connection refused, gateway timeout 502/503/504, request timeout),
     maka penolakan sah (Non-BPJS) diperlakukan sama persis dengan kegagalan teknis server (`null`).

3. **Efek Rantai Evaluasi Pra-Simpan**:
   - Sesuai keputusan arsitektur awal (TD-05 & TD-06), nilai `null` difungsikan sebagai *fail-safe* agar registrasi tidak mandek saat JetliApi down.
   - Namun, karena jaminan Non-BPJS Umum (`00000`) selalu menghasilkan HTTP 400 "Data Not Found", sistem selalu mengembalikan `null`, sehingga dialog persetujuan `requestSatuSehatApproval` tidak pernah dipanggil untuk seluruh pasien jaminan Umum / Non-BPJS yang tidak terdaftar di mapping BPJS.

## Affected Components

- **Workflows & UI Logic**:
  - Alur pra-simpan registrasi admisi rawat jalan (`handleSubmitRegister`).
  - Mekanisme lookup dan verifikasi grup jaminan (`checkIsBpjsJaminan`).
  - Dialog informed consent Satu Sehat (`requestSatuSehatApproval` / `DialogPersetujuanSatuSehat.vue`).
- **Files**:
  - `c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts` (fungsi `checkIsBpjsJaminan`).
  - `c012_myhospital_web/src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts` (cakupan unit test verifikasi respons HTTP 400/404).

## Impact Assessment

- **Business Impact**:
  - Pelanggaran prosedur *informed consent*: Pasien jaminan Umum/Non-BPJS didaftarkan tanpa adanya konfirmasi persetujuan upload data rekam medis ke platform Satu Sehat Kemenkes, bertentangan dengan kebijakan perlindungan data pribadi pasien.
- **Operational Impact**:
  - Petugas loket admisi tidak diberikan kesempatan menanyakan persetujuan upload Satu Sehat kepada pasien umum di loket.
- **Technical Impact**:
  - Data status persetujuan Satu Sehat pada database (`tc_mr_saset`) untuk pasien Non-BPJS tetap bernilai false / tidak pernah tercatat persetujuannya, meskipun pasien mungkin bersedia menyetujui.

## Assumptions

- ASM-INV-001: Respons dari JetliApi `/GrupJaminan/map` yang mengembalikan kode status 400 atau 404 dengan body `"status": "Data Not Found"` (atau pesan mapping tidak ditemukan) secara semantik menyatakan bahwa penjamin tersebut adalah Non-BPJS.
- ASM-INV-002: Gangguan teknis riil (koneksi terputus, timeout, internal server error 5xx) tetap harus memicu *fail-safe* (`null`), sehingga registrasi pasien di loket tidak terblokir.
- ASM-INV-003: Nilai hasil evaluasi Non-BPJS yang valid harus di-cache di client (`grupJaminanCache`) agar penjamin umum tidak terus-menerus memicu request HTTP saat registrasi berikutnya.

## Open Questions

Tidak ada pertanyaan terbuka yang menghambat. Perilaku respons API eksternal dan kebutuhan alur bisnis telah teridentifikasi secara jelas dan dapat direproduksi.

## Recommended Decision

Bedakan penanganan kesalahan pada pengecekan grup jaminan:
1. Jika respons dari JetliApi mengindikasikan status "Data Not Found" (HTTP 400 atau 404 yang membawa indikator data mapping tidak ditemukan), interpretasikan respons tersebut sebagai jaminan **Non-BPJS** yang sah (`false`), simpan ke dalam cache client sebagai Non-BPJS, dan lanjutkan alur pemunculan dialog konfirmasi.
2. Jika respons berasal dari gangguan jaringan, timeout, atau kesalahan teknis server (5xx), pertahankan perilaku *fail-safe* (`null`) yang melewati konfirmasi demi kelancaran antrean loket.

## Decision

Memperbarui logika penanganan pengecekan grup jaminan pada modul pendaftaran agar dapat mengenali respons HTTP 400 / 404 "Data Not Found" dari JetliApi sebagai representasi kelompok jaminan **Non-BPJS** (`isBpjs = false`), sehingga dialog konfirmasi persetujuan Satu Sehat ditampilkan kepada petugas/pasien sebelum registrasi disimpan, sembari tetap menjaga *fail-safe* (`null`) untuk error teknis/koneksi murni.

## Decision Rationale

1. **Pemenuhan Regulasi Informed Consent**: Memastikan pasien Non-BPJS (Umum/Asuransi Swasta) mendapatkan hak konfirmasi persetujuan upload data sebelum pendaftaran diselesaikan.
2. **Kesesuaian dengan Karakteristik API Riil**: Mengakomodasi format penolakan/not found dari service JetliApi yang mengembalikan kode 400 dengan pesan "Data Not Found" tanpa harus mengubah backend JetliApi yang berada di luar kendali Bilreg.
3. **Preservasi Prinsip Zero-Blockage**: Tidak mengorbankan ketahanan sistem; bila server JetliApi mati total atau timeout, alur registrasi tetap berjalan mulus melalui fail-safe.
4. **Efisiensi Caching**: Menyimpan status Non-BPJS ke dalam cache client untuk mencegah redundant request berulang untuk tipe jaminan yang sama.

## Architecture Applicability

### Decision

ARCHITECTURE-NOT-REQUIRED

### Rationale

Perbaikan ini merupakan koreksi logika klasifikasi respons error pada komponen client-side composable (`checkIsBpjsJaminan`) yang sudah ada. Struktur komponen, kontrak data, batasan integrasi, dan diagram alur arsitektur target yang didefinisikan pada [PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md) tetap valid dan tidak mengalami perubahan struktural atau konseptual (TD-05 secara prinsip telah mengamanatkan: jika respons tidak ada data / non-BPJS, tampilkan dialog konfirmasi).
