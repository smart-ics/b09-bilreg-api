# BUG-INVESTIGATION

## Context

Issue: ISSUE-IGD-EMR-TIMELINE-001
Problem Summary: Data Assesment IGD Triage yang berhasil disimpan dan dihubungkan ke pendaftaran (RegId) tampil pada menu Catalog EMR, namun tidak muncul pada Timeline EMR karena entitas label (EMREC_label) belum digenerate.

## Current State

Saat ini, alur penanganan asesmen IGD Triage hingga registrasi pasien berjalan sebagai berikut:

1. **Pembuatan Triage di IGD**:
   - Pengguna melakukan asesmen triage pasien di IGD (`IgdVisitTriage`).
   - Sistem BILREG memanggil endpoint SMASS (`POST /api/Assesment/generateIgdTriage`) untuk membentuk asesmen di SMASS.
   - Karena pasien gawat darurat ditangani sebelum proses administrasi pendaftaran selesai, asesmen terbentuk tanpa `RegId`, `PasienId`, dan `PasienName`, dengan status `RegistrationLinkStatus = Pending Registration` (Drafting) pada tabel `SMASS_Assesment`.
2. **Proses Registrasi / Linking**:
   - Setelah administrasi pendaftaran selesai, sistem memanggil `{BilregApi} PATCH api/IgdVisit/{id}/register` (`IgdVisitAssignRegisterCmd`) untuk menghubungkan kunjungan IGD dengan `RegId` pendaftaran.
   - Pasca-commit transaksi registrasi di BILREG, `IgdVisitSmassLinkHook` mengeksekusi panggilan ke endpoint SMASS `{SmassApi} PATCH /api/Assesment/linkIgdVisit` dengan payload informasi registrasi (`RegId`, `PasienId`, `PasienName`, `LayananId`, `LayananName`).
   - SMASS memperbarui data asesmen terkait menjadi `RegistrationLinkStatus = Registered`.
3. **Perilaku di Aplikasi EMR**:
   - Pada menu **Catalog** EMR, data asesmen hasil generate dari triage IGD sudah berhasil tampil karena Catalog mengambil data langsung dari service/tabel asesmen SMASS berdasarkan `RegId`/`PasienId`.
   - Namun, pada menu **Timeline** EMR (`GET /api/Label/TimeLine`), data asesmen tersebut **tidak muncul**.
   - Hal ini disebabkan Timeline EMR mengandalkan entitas label pada tabel `EMREC_label` (tipe label `12` untuk asesmen SMASS). Pembuatan record `EMREC_label` memerlukan pemanggilan endpoint `{Emr20Api} POST /api/LabelV2/AddSmass`, yang saat ini belum pernah dipanggil pada alur pembuatan maupun linking registrasi IGD.

## Problem Analysis

1. **Pemisahan Sumber Data Antara Catalog dan Timeline EMR**:
   - Menu **Catalog** pada EMR membaca data asesmen klinis dari SMASS. Begitu `RegId` terhubung di `SMASS_Assesment`, asesmen langsung dapat ditemukan pada pencarian/katalog pasien.
   - Menu **Timeline** pada EMR 2.0 (`Emr2.Api` / `LabelController.TimeLine`) mengagregasi aktivitas medis pasien dari tabel label `EMREC_label` melalui `LabelBL.ListTimeLine`. Asesmen tidak akan muncul dalam kronologi timeline jika belum terdaftar pada `EMREC_label`.

2. **Mekanisme Pendaftaran Label SMASS ke EMR 2.0**:
   - EMR 2.0 menyediakan endpoint `{Emr20Api} POST /api/LabelV2/AddSmass` (ditangani oleh `AddSmassHandler` di `Emr2.Application.LabelContext`).
   - Endpoint tersebut bertugas membuat record baru pada `EMREC_label` dengan `LabelTypeID = "12"` dan `ReffID = AssesmentId`, dengan parameter:
     - `AssesmentId`: ID asesmen dari SMASS.
     - `LayananId`: ID unit/layanan.
     - `PaperId`: ID formulir dokumen klinis (misal `PP-ICS-TRGE`).
     - `PaperName`: Nama formulir dokumen klinis (misal `"FORMULIR TRIASE IGD"`).
     - `RegId`: Nomor registrasi pendaftaran pasien.
     - `UserrId`: ID pengguna/petugas yang bertanggung jawab.

3. **Temuan Rekam Jejak Desain Awal (Dokumen Arsitektur Sebelumnya)**:
   - Pada perancangan awal integrasi SMASS IGD Triage (`docs/contexts/igd/igd-triage-abc-smass-architecture.md` §9.7 Risk R-03 dan AR-14), terdapat catatan arsitektural:
     > *"Reflection, diagnosa-terpusat, time-table and EMR 'assesment created' hooks do not run for IGD-generated assessments, even after linking."*
     > *"Downstream SMASS features that rely on those hooks will not see IGD assessments. Requires a separate decision if/when those integrations must include IGD triage."*
   - Pada saat pembuatan triage awal, penonaktifan event/hook ke EMR dilakukan secara sengaja karena data `RegId` belum tersedia (mencegah data tanpa registrasi masuk ke alur downstream yang mewajibkan `RegId`).
   - Namun, saat proses `LinkIgdVisit` pasca-registrasi diimplementasikan, hook pembuatan label ke EMR belum disertakan, sehingga risiko residual R-03 kini terealisasi sebagai laporan bug/isu operasional.

4. **Kondisi Konfigurasi dan Komponen Integrasi di BILREG**:
   - Di servis BILREG, konfigurasi `appsettings.json` section `Emr` saat ini hanya mendefinisikan `BaseApiUrl` untuk `Emr25Api` (`http://dev.smart-ics.com:8089/emr25api`) yang dipakai untuk antrian admisi/dashboard (`api/Dashboard/addReg`).
   - Belum terdapat konfigurasi URL untuk `Emr20Api` (`http://dev.smart-ics.com:8083/emr20-api`), yang merupakan servis terpisah pemilik endpoint `LabelV2/AddSmass`.
   - Di `Bilreg.Application.IgdContext`, hook pasca-registrasi (`IgdVisitSmassLinkHook`) saat ini hanya berfokus mengirimkan event link ke SMASS (`ISmassAssessmentGateway.LinkIgdVisit`) dan belum memiliki gateway maupun abstraksi untuk pembuatan label ke EMR 2.0.

## Affected Components

- **Modules / Subsystems**:
  - `Bilreg.Application` (`IgdContext\IgdVisitFeature`): Alur pasca-registrasi kunjungan IGD (`IgdVisitAssignRegisterCmd` / hook penyelesaian integrasi).
  - `Bilreg.Infrastructure` (`IgdContext\Integration`): Integrasi gateway outbound menuju EMR 2.0 API (`Emr20Api`) dan adaptasi pembacaan respons link SMASS.
  - `Bilreg.Api`: Konfigurasi `appsettings.json` (kebutuhan endpoint `Emr20Api` dan opsi `SmassTriagePaperName`).
  - `EMR 2.0 API` (`LabelContext`): Endpoint `POST /api/LabelV2/AddSmass` dan pembacaan `GET /api/Label/TimeLine`.
- **Database Objects**:
  - `EMREC_label`: Penyimpanan record label timeline di database EMR 2.0.
  - `SMASS_Assesment`: Penyimpanan data asesmen triage di database SMASS.
  - `BILRG_IgdVisitSmassTask`: Tabel pelacakan status task operasional integrasi IGD di database BILREG (tetap hanya untuk SMASS task).
- **Integrations / Endpoints**:
  - `{BilregApi} PATCH /api/IgdVisit/{id}/register`
  - `{SmassApi} PATCH /api/Assesment/linkIgdVisit`
  - `{Emr20Api} POST /api/LabelV2/AddSmass`
  - `{Emr20Api} GET /api/Label/TimeLine`
- **Workflows & UI**:
  - Alur Registrasi Pasien IGD Triage ke Sistem Billing/Pendaftaran.
  - Tampilan Kronologi Timeline Pasien pada aplikasi Rekam Medis (EMR).

## Impact Assessment

- **Business Impact**:
  - Kronologi riwayat klinis pasien pada rekam medis elektronik menjadi tidak lengkap karena asesmen awal triage di IGD tidak muncul pada Timeline utama pasien.
  - Dokter pemeriksa atau perawat di unit rawat lanjutan dapat terlewat melihat hasil asesmen triage awal kecuali jika secara khusus membuka menu Catalog EMR.
- **Operational Impact**:
  - Tenaga medis memerlukan langkah ekstra untuk memeriksa dokumen di menu Catalog daripada memanfaatkan Timeline terpusat.
- **Technical Impact**:
  - Diperlukan penambahan mekanisme integrasi pembentukan label ke `Emr20Api`.
  - Diperlukan mekanisme ketahanan (fault-tolerance/resilience) agar kegagalan pembuatan label tidak membatalkan atau mengganggu komitmen transaksi registrasi pasien di BILREG.

## Assumptions

1. Tampilan Timeline EMR 2.0 secara konsisten mengandalkan data pada tabel `EMREC_label` dengan `LabelTypeID = "12"` yang dibentuk melalui `{Emr20Api} POST /api/LabelV2/AddSmass`.
2. Seluruh atribut yang dibutuhkan untuk membentuk label (`AssesmentId`, `PaperId`, `RegId`, `LayananId`, `UserrId`) tersedia atau dapat diidentifikasi saat proses linking registrasi selesai.
3. Asesmen triage IGD yang telah dibuat tetap valid dan utuh di SMASS, sehingga hanya proses pendaftaran labelnya ke EMR yang perlu diselesaikan.
4. Karakteristik pemanggilan ke EMR harus bersifat *non-blocking* dan *fail-safe* terhadap proses registrasi pasien utama.

## Open Questions & Resolutions

- **Q-01 (Boundary Ownership)**: Di mana orkestrasi pemanggilan ke `POST /api/LabelV2/AddSmass` seharusnya ditempatkan?
  - **Resolution [Closed by User Decision]**: Orkestrasi pemanggilan ke `{Emr20Api} POST /api/LabelV2/AddSmass` ditempatkan di **BILREG** setelah menerima konfirmasi sukses dari pemanggilan SMASS `linkIgdVisit`. Proses berjalan berurutan: jika link ke SMASS berhasil, BILREG melanjutkan dengan meng-generate label ke EMR 2.0 untuk setiap asesmen yang berhasil di-link dengan `RegId`.
- **Q-02 (Parameter Metadata PaperName)**: Nilai `PaperName` diwajibkan pada payload `AddSmass`. Dari mana nilai ini diperoleh?
  - **Resolution [Closed by User Decision]**: Karena metadata balikan dari SMASS `linkIgdVisit` (`LinkResponse`) hanya mengembalikan `ListAssesmentId` tanpa `PaperName`, maka nilai `PaperName` dikonfigurasikan pada `IgdVisitOptions:SmassTriagePaperName` di servis BILREG.
- **Q-03 (Handling Multi-Assessment)**: Jika satu kunjungan IGD memiliki lebih dari satu asesmen, bagaimana penanganannya?
  - **Resolution [Closed by User Decision]**: Setiap `AssesmentId` yang dihasilkan dari integrasi IGD triage ke SMASS selalu di-generate ke Label EMR, dengan batasan hanya untuk asesmen-asesmen yang berstatus sukses dari proses link register (`ListAssesmentId` yang terkonfirmasi oleh respons SMASS `linkIgdVisit`).
- **Q-04 (Resilience & Task Logging)**: Apakah BILREG perlu mencatat task status terpisah untuk pembuatan label EMR?
  - **Resolution [Closed by User Decision]**: Untuk saat ini, **tidak perlu** pencatatan task terpisah untuk Label EMR. Eksekusi dilakukan secara langsung (*best-effort/inline post-link*) tanpa perlu tabel antrian/task tersendiri untuk EMR label.

## Recommended Decision

- Mengimplementasikan pemanggilan ke `{Emr20Api} POST /api/LabelV2/AddSmass` di dalam alur pasca-link registrasi IGD pada servis **BILREG**.
- Mengambil daftar `ListAssesmentId` dari respons sukses SMASS `linkIgdVisit`, lalu mengeksekusi pembuatan label ke `Emr20Api` secara berurutan untuk setiap `AssesmentId` yang sukses di-link.
- Membaca konfigurasi `PaperId` dari `IgdVisitOptions.SmassTriagePaperId` dan `PaperName` dari konfigurasi baru `IgdVisitOptions.SmassTriagePaperName`.
- Menambahkan konfigurasi URL base endpoint `Emr20Api` pada konfigurasi BILREG.
- Menjaga prinsip *fault-isolation*: kegagalan pemanggilan label ke EMR tidak boleh membatalkan atau memengaruhi transaksi registrasi pasien yang telah di-commit.

## Decision

Menyetujui secara definitif:
1. **Orkestrator**: BILREG bertindak sebagai orkestrator yang memanggil `{Emr20Api} POST /api/LabelV2/AddSmass` segera setelah pemanggilan `linkIgdVisit` ke SMASS berhasil.
2. **Target Asesmen**: Label dibuat untuk seluruh asesmen yang sukses di-link, berdasarkan daftar `ListAssesmentId` yang dikembalikan oleh SMASS `linkIgdVisit`.
3. **Konfigurasi**: Menambahkan konfigurasi endpoint `Emr20Api` dan nilai `SmassTriagePaperName` di BILREG.
4. **Scope Resilience**: Tanpa pencatatan task tabel terpisah untuk label pada fase ini.

## Decision Rationale

1. **Konsistensi Alur Operasional**: BILREG sudah memegang seluruh konteks registrasi (`RegId`, `LayananId`, `UserId`) dan status komitmen visit, sehingga orkestrasi berurutan setelah link SMASS menjamin keselarasan data tanpa perlu merombak domain internal SMASS.
2. **Kesesuaian dengan Bukti Teknis**: SMASS `LinkResponse` secara faktual menyediakan daftar `ListAssesmentId`, sementara ketiadaan `PaperName` pada respons diselesaikan secara deterministik melalui konfigurasi opsi BILREG.
3. **Efisiensi Implementasi**: Menghindari kompleksitas penambahan tabel/task queue baru sesuai kebutuhan terkini tim, sembari tetap menyelesaikan isu hilangnya asesmen pada Timeline EMR.

## Architecture Applicability

### Decision

ARCHITECTURE-REQUIRED

### Rationale

Perbaikan ini memenuhi kriteria **ARCHITECTURE-REQUIRED** karena:
1. Menetapkan integrasi lintas batas servis baru (BILREG ke EMR 2.0 API).
2. Memerlukan pembaruan kontrak gateway integrasi SMASS di BILREG untuk mengekspos `ListAssesmentId`.
3. Memperkenalkan komponen outbound gateway/adapter baru untuk EMR 2.0 Label di lapisan Infrastructure BILREG.
4. Memerlukan penambahan struktur konfigurasi pada `IgdVisitOptions` dan konfigurasi environment EMR 2.0.
