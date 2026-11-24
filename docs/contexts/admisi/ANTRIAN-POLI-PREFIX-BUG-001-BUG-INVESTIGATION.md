# BUG-INVESTIGATION

## Context

Issue: [ANTRIAN-POLI-PREFIX-BUG-001-ISSUE.md](file:///home/hlysx/projects/ics/billingweb/b21-myhosweb-system/Issues/ANTRIAN-POLI-PREFIX-BUG-001-ISSUE.md)
Problem Summary: Nomor antrian poli pada bukti cetak registrasi rawat jalan dan respons detail registrasi `GET /api/reg/{id}` tercetak dan terkirim sebagai angka polos tanpa kode prefix ruangan (tercetak `7`, bukan `C7`), karena kontrak respons `RegGetResponse` dan pemetaan data cetak di frontend belum menyertakan label antrian berprefix (`queueLabel`).

## Current State

Saat pengujian kasus uji TC-01 (Pendaftaran Walk-In dengan Ruangan Berprefix) pada fitur antrian poli prefix:
1. Pasien didaftarkan ke Poli Penyakit Dalam pada ruangan dengan prefix `C` (bukti registrasi `RG01378282`).
2. Backend `b09-bilreg-api` berhasil menyimpan sesi antrian dokter dengan snapshot prefix ruangan `C` pada tabel `BILRG_Antrian`.
3. Endpoint mutasi pendaftaran `POST /api/reg/rajalWalkIn` mengembalikan respons `RegJalanCreateResponse` berlabel lengkap (`noAntrian: 7`, `queueLabel: "C7"`, `prefixAntrian: "C"`).
4. Namun, saat antarmuka pendaftaran admisi memuat detail pendaftaran untuk proses cetak struk melalui `registrasiService.useDetailRegistrasi("RG01378282")` (`GET /api/reg/RG01378282`):
   - Handler `RegJalanGethandler` di backend hanya memetakan kolom `NoAntrian: int` ke dalam `RegGetResponse`. Respons JSON hanya memuat `"noAntrian": 7` tanpa properti `queueLabel` atau `prefixAntrian`.
   - Fungsi akses data `AntrianDal.ListData` yang dipanggil handler tidak menyertakan kolom `bb.QueuePrefixSnapshot` dari tabel `BILRG_Antrian`.
   - Model perantara `AntrianViewDto` dan domain view `AntrianView` tidak memiliki properti snapshot prefix atau label antrian.
   - Di sisi frontend `c012_myhospital_web`, skema `registerPasienSchema`, `registerPasienOutputSchema`, dan adapter `registerPasienAdapter` belum mendeklarasikan properti aditif `queueLabel` dan `prefixAntrian`.
   - Fungsi `preparePrintItems()` pada `LegacyRegistrationWorkspace.vue` memetakan langsung `noAntrian: reg.noAntrian` (nilai integer `7`) ke variabel template cetak `antrian_registrasi.html`.
5. Akibatnya, bukti fisik cetak struk registrasi maupun pratinjau cetak di loket admisi hanya menampilkan angka numerik polos tanpa kode prefix ruangan (`7`).

## Problem Analysis

### 1. Kesenjangan Kontrak API Query Registrasi (GET /api/reg/{id})
Pada perancangan awal di `ANTRIAN-POLI-PREFIX-ARCHITECTURE.md`, keputusan TD-06 memfokuskan pembaruan kontrak aditif pada DTO mutasi pendaftaran walk-in (`RegJalanCreateResponse`), booking (`BookingCreateResponse`), dan pembacaan antrian per sesi (`QueGetAntrianResponse`). Endpoint pembacaan detail registrasi `GET /api/reg/{id}` (`RegGetQuery` dan `RegGetResponse`) terlewat dari cakupan pembaruan DTO berlabel, sehingga masih beroperasi dengan kontrak lama yang hanya menyediakan `int NoAntrian`.

### 2. Ketiadaan Proyeksi Snapshot Prefix pada Lapisan Akses Data Antrian
Pada `RegJalanGethandler`, nomor antrian diperoleh melalui `_queRepo.ListData(dateTime)`. Implementasi query SQL pada `AntrianDal.ListData`:
```sql
SELECT
    aa.AntrianId, aa.AntrianStatus, aa.NoUrut, 
    aa.PersonName, aa.ReffId, aa.ReffDesc,
    ISNULL(bb.AntrianDate,'') AS AntrianDate, 
    ISNULL(bb.SequenceTag,'') AS SequenceTag, 
    ISNULL(bb.AntrianDescription,'') AS AntrianDescription, 
    ISNULL(bb.StartTime,'') AS StartTime, 
    ISNULL(bb.EndTime,'') AS EndTime
FROM
    BILRG_AntrianEntry aa
    LEFT JOIN BILRG_Antrian bb ON aa.AntrianId = bb.AntrianId
WHERE 
    bb.AntrianDate = @Tgl
```
Klausa SELECT tidak memproyeksikan kolom `bb.QueuePrefixSnapshot` dari tabel `BILRG_Antrian`. DTO `AntrianViewDto` dan objek domain `AntrianView` juga belum memiliki properti prefix atau label, sehingga `RegJalanGethandler` kehilangan konteks prefix ruangan jadwal dokter saat membentuk respons registrasi.

### 3. Ketergantungan Alur Cetak Frontend pada Query Detail Registrasi
Fungsi cetak bukti pendaftaran di modul Admisi (`LegacyRegistrationWorkspace.vue`) tidak mengambil nomor antrian langsung dari hasil mutasi registrasi walk-in. Sistem menunggu pembaruan state reaktif `pasien.registration.value` yang diperoleh dari `useDetailRegistrasi` (`GET /api/reg/{id}`). Karena model registrasi frontend hanya menerima `noAntrian: number`, fungsi `preparePrintItems()` hanya dapat menyuplai angka murni ke template `antrian_registrasi.html`.

## Affected Components

1. **Backend Core Persistence & Application (`b09-bilreg-api`)**:
   - `Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianDal.cs`: Tambahkan proyeksi `ISNULL(bb.QueuePrefixSnapshot, '') AS QueuePrefixSnapshot` pada query `ListData`.
   - `Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianViewDto.cs`: Tambahkan properti `QueuePrefixSnapshot` dan petakan ke `AntrianView`.
   - `Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs`: Perluas record `AntrianView` untuk menyertakan `QueuePrefixSnapshot` dan method atau properti kalkulasi `QueueLabel`.
   - `Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegGetQuery.cs`: Tambahkan properti string aditif `QueueLabel` dan `PrefixAntrian` pada record `RegGetResponse`, serta petakan nilai format ringkas poli (`physicianQue.QueueLabel`) pada `RegJalanGethandler`.
   - Unit test terkait di `Bilreg.Test` (pengujian query registrasi `RegGetQuery`).

2. **Frontend UI Module (`c012_myhospital_web`)**:
   - `src/modules/Admisi/types/registrasi.ts`: Tambahkan properti opsional aditif `queueLabel` dan `prefixAntrian` pada `registerPasienSchema`, `registerPasienOutputSchema`, dan `registerPasienAdapter`.
   - `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue`: Perbarui fungsi `preparePrintItems()` agar memetakan label antrian berprefix (`reg.queueLabel || (reg.prefixAntrian ? `${reg.prefixAntrian}${reg.noAntrian}` : reg.noAntrian)`) ke token cetak `noAntrian`.
   - Unit test terkait di modul Admisi frontend.

3. **Template Dokumen Cetak**:
   - `public/template/antrian_registrasi.html`: Menggunakan token placeholder `{{ noAntrian }}` yang menerima nilai label berprefix secara transparan tanpa perlu perubahan markup HTML.

## Impact Assessment

- **Business Impact**: Pasien rawat jalan menerima bukti pendaftaran fisik yang tidak mencantumkan kode prefix ruangan poliklinik, memicu kebingungan pasien saat mencari ruangan dokter dan mencocokkan nomor tiket dengan tampilan monitor poli.
- **Operational Impact**: Petugas admisi dan perawat poliklinik terpaksa mengarahkan pasien secara manual karena tiket pendaftaran tidak mencerminkan identitas prefix poliklinik tujuan.
- **Technical Impact**: Terjadi inkonsistensi representasi data antrian antara endpoint mutasi pendaftaran (`queueLabel` terisi) dengan endpoint query detail registrasi (`queueLabel` kosong), yang menyebabkan komponen konsumen detail registrasi kehilangan informasi prefix.

## Assumptions

- ASM-INV-001: Template cetak `antrian_registrasi.html` merender variabel `{{ noAntrian }}` sebagai string murni, sehingga pemberian nilai string seperti `"C7"` tidak merusak tata letak cetak struk thermal.
- ASM-INV-002: Sesi antrian dokter di `BILRG_Antrian` untuk pendaftaran rawat jalan telah memiliki snapshot prefix ruangan yang valid (`QueuePrefixSnapshot`), sesuai verifikasi fitur utama.
- ASM-INV-003: Penambahan properti `QueueLabel` dan `PrefixAntrian` pada `RegGetResponse` bersifat aditif murni dan tidak memengaruhi pemanggil lain yang hanya membaca properti numerik `NoAntrian`.

## Open Questions

Tidak ada pertanyaan terbuka yang menghambat. Seluruh jalur eksekusi dari skema basis data, query handler, respons API, adapter frontend, hingga penyiapan data cetak telah teridentifikasi secara jelas.

## Recommended Decision

Selaraskan kontrak data query detail registrasi dengan kontrak mutasi pendaftaran secara aditif, sertakan proyeksi snapshot prefix pada lapisan akses data antrian, dan sesuaikan pemetaan data cetak pada antarmuka registrasi untuk mengutamakan label berprefix.

## Decision

Menetapkan arahan perbaikan terpadu pada backend dan frontend:
1. **Backend (`b09-bilreg-api`)**: Sertakan kolom `QueuePrefixSnapshot` pada query `AntrianDal.ListData`, teruskan ke `AntrianViewDto` dan `AntrianView`, lalu tambahkan properti aditif `QueueLabel` dan `PrefixAntrian` pada `RegGetResponse` di `RegGetQuery.cs` dengan memanfaatkan format ringkas poli (`FormatClinicQueueLabel`).
2. **Frontend (`c012_myhospital_web`)**: Perbarui skema DTO dan adapter registrasi pasien (`registrasi.ts`) agar memuat `queueLabel` dan `prefixAntrian`, serta perbarui fungsi `preparePrintItems()` pada antarmuka pendaftaran untuk memetakan label antrian berprefix ke token cetak struk registrasi.

## Decision Rationale

1. **Konsistensi Kontrak Data**: Menghilangkan ketidakselarasan antara respons endpoint pendaftaran walk-in (`RegJalanCreateResponse`) dan query detail pendaftaran (`RegGetResponse`).
2. **Nol Regresi (Zero Breaking Change)**: Menjaga kompatibilitas penuh dengan tetap mempertahankan properti `noAntrian: int` sebagai nilai integer murni bagi integrasi atau modul eksisting.
3. **Integritas Tiket Pasien**: Memastikan nomor yang dipegang pasien di atas kertas struk registrasi identik dengan nomor yang terpampang di layar monitor ruang tunggu dan suara panggilan perawat.
4. **Efisiensi Implementasi**: Memanfaatkan template cetak `antrian_registrasi.html` yang sudah ada tanpa perlu merombak struktur berkas template.

## Architecture Applicability

### Decision

ARCHITECTURE-NOT-REQUIRED

### Rationale

Perbaikan cacat ini memenuhi kriteria ARCHITECTURE-NOT-REQUIRED karena:
- Tidak ada penambahan tabel, kolom basis data, atau skrip migrasi DDL baru (kolom `QueuePrefixSnapshot VARCHAR(5)` pada tabel `BILRG_Antrian` sudah tersedia dari fitur utama).
- Tidak ada batasan bounded context, dependensi arsitektural, atau protokol komunikasi baru.
- Kebijakan arsitektur mengenai format ringkas tanpa leading zero (TD-05) dan evolusi kontrak aditif tanpa breaking change (TD-06) dari artefak `ANTRIAN-POLI-PREFIX-ARCHITECTURE.md` sudah jelas dan langsung menjadi acuan perbaikan.
- Perubahan hanya berupa perbaikan proyeksi SQL pada DAL, penambahan properti aditif pada DTO query, dan pemetaan data cetak di frontend.

Tahapan SDLC dapat langsung melangkah ke perancangan rencana implementasi (IMPLEMENTATION-PLAN) oleh peran arsitek atau tim pelaksana.
