---
Title: Feasibility Assessment - Pengiriman Prefix Antrian Rawat Jalan Berdasarkan Master Ruang ke EMR25 Dashboard
Code: ADMISI-EMR-PREFIX-ANTRIAN
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-10-24
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan perubahan (CHANGE-REQUEST) pengiriman kode prefix antrian (`PrefixAntrian`) dari sistem Bilreg (`b09-bilreg-api`) ke sistem EMR25 (`a054_emr25_api`) melalui endpoint `POST api/Dashboard/addReg` pada alur pendaftaran Rawat Jalan (Walk-In dan By Booking).

Nilai `PrefixAntrian` bersumber dari tabel master ruangan lokal `HiDok_Ruang` melalui Jadwal Praktek dokter yang bertugas. Pelayanan Rawat Darurat (IGD) dikecualikan dari pengiriman prefix antrian (mengirim nilai default/kosong). Mekanisme penyimpanan antrian lokal outbox (`BILRG_EmrAntrianOutboundQueue`) tetap dipertahankan tanpa perubahan skema tabel fisik baru.

Referenced artifacts:

- ISSUE: [ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md) (`ISSUE-BILREG-EMR-PREFIX-ANTRIAN-001`)
- DOMAIN: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)

## Objective

Menilai kelayakan teknis, fungsional, dan arsitektural untuk menyertakan `PrefixAntrian` pada registrasi Rawat Jalan ke EMR25; memvalidasi kompatibilitas kontrak data; menganalisis ketersediaan sumber data ruang; serta menetapkan keputusan gap closure guna memastikan implementasi berjalan tanpa memicu breaking change dan tanpa penambahan tabel database baru.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan investigasi artefak dan kode sumber:

## Existing Behavior

1. **Registrasi Rawat Jalan Walk-In (`RegJalanWalkInCommand`)**:
   - Jadwal praktek di-resolve menggunakan `BookingScheduleResolver.ResolveWalkIn(...)`.
   - Objek jadwal yang dihasilkan (`schedule.LegacyJadwal` dan `schedule.Effective`) memuat entitas `RuangType` yang telah menampung kolom `PrefixAntrian` dari hasil join ke tabel `HiDok_Ruang`.
   - Namun, saat menginisiasi DTO antrian EMR ([RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs#L349-L355)), payload hanya memasukkan 9 parameter tanpa menyertakan `PrefixAntrian`.
   - Payload di-enqueue ke tabel lokal `BILRG_EmrAntrianOutboundQueue` via `_emrOutboundEnqueue.TryEnqueueAddReg(emrPayload, occurredAt)` di dalam cakupan transaksi database (`TransHelper.NewScope()`).

2. **Registrasi Rawat Jalan By Booking (`RegJalanByBookingCmd`)**:
   - Booking di-load melalui `_bookingRepo.LoadEntity(BookingModel.Key(id))`.
   - DTO antrian EMR ([RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs#L338-L344)) dibentuk dengan 9 parameter tanpa `PrefixAntrian`.
   - Payload di-enqueue ke tabel lokal `BILRG_EmrAntrianOutboundQueue` via `_emrOutboundEnqueue.TryEnqueueAddReg(...)` di dalam transaksi database.

3. **Registrasi Rawat Darurat IGD (`RegDaruratCreateCmd`)**:
   - Registrasi IGD ditangani oleh [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs#L301-L312).
   - Membentuk `AddAntrianEmrByRegCommand` dengan nilai statis `JamJadwal = "00:00"` dan `NoAntrian = 0`.
   - Pengiriman dilakukan secara langsung (direct sync call) via `_addAntrianEmrByRegService.Execute(payload)` pasca-commit transaksi registrasi.
   - Tidak ada jadwal praktek maupun pemilihan ruang pada alur IGD.

4. **Tabel Master Ruang (`HiDok_Ruang`)**:
   - Tabel fisik `HiDok_Ruang` memiliki kolom `RuangId`, `RuangName`, dan `PrefixAntrian`.
   - Dikelola melalui [RuangDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/RuangDal.cs) dan [RuangRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/RuangRepo.cs).
   - Di-JOIN secara konsisten pada pembacaan jadwal dokter:
     - `BILRG_JadwalPraktek` via `JadwalPraktekDal`
     - `BILRG_JadwalPraktekHarian` via `JadwalPraktekHarianDal`
     - `ta_jadwal_dokter` via `JadwalFoDal`

5. **Penyimpanan Lokal Outbox (`BILRG_EmrAntrianOutboundQueue`)**:
   - Tabel fisik `BILRG_EmrAntrianOutboundQueue` menyimpan data pada kolom `PayloadJson NVARCHAR(MAX)`.
   - Serialisasi dan deserialisasi ditangani oleh [EmrAntrianOutboundPayloadBuilder.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundPayloadBuilder.cs) menggunakan `System.Text.Json` dengan naming policy camelCase.
   - Pengiriman aktual ke EMR25 dieksekusi secara asinkron oleh background processor [EmrAntrianOutboundProcessor.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundProcessor.cs) memanggil endpoint EMR.

6. **Penerima EMR25 (`a054_emr25_api`)**:
   - Di servis EMR25, record [AddRegCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/a054_emr25_api/Emr25.Application/DashboardContext/DashboardAgg/UseCases/AddRegCommand.cs#L17-L20) sudah mendefinisikan parameter opsional:
     `string PrefixAntrian = ""` (parameter ke-10).
   - Handler EMR25 (`AddRegHandler`) telah mengimplementasikan sanitasi (trim dan pemotongan maksimal 2 karakter) serta memetakan nilai tersebut ke `RegBilregDto.PrefixAntrian` pada antrian dokter poli dan antrian periksa awal (`AddAntrianAwalWorker`).

## Existing Constraints

1. **Zero Breaking Change**: Modifikasi kontrak DTO `AddAntrianEmrByRegCommand` tidak boleh merusak komponen pemanggil lain maupun unit test yang sudah ada.
2. **Tanpa Perubahan Skema Tabel Fisik Lokal**: Tidak diperkenankan menambah kolom fisik baru pada tabel lokal `BILRG_EmrAntrianOutboundQueue` maupun tabel registrasi/antrian lokal lainnya.
3. **Pengecualian IGD**: Alur pelayanan IGD tidak memiliki prefix antrian dan tidak boleh dipaksa melakukan query ruang atau mengirimkan nilai prefix fiktif.
4. **Reliability & Idempotensi**: Penyimpanan antrian EMR Rawat Jalan harus tetap berada dalam transaksi atomik registrasi lokal (`TransHelper.NewScope()`) sebelum dikirimkan oleh outbox processor.

---

# 3. Gap Analysis

| ID | Severity | Status | Gap |
|---|---|---|---|
| GAP-001 | CRITICAL | CLOSED | Kontrak record DTO `AddAntrianEmrByRegCommand` di layer Application Bilreg hanya menerima 9 parameter dan belum memiliki properti `PrefixAntrian`. |
| GAP-002 | MAJOR | CLOSED | Handler `RegJalanWalkInCommand` belum memetakan nilai `PrefixAntrian` dari objek `schedule.LegacyJadwal.Ruang` / `schedule.Effective.Ruang` ke dalam DTO `AddAntrianEmrByRegCommand`. |
| GAP-003 | MAJOR | CLOSED | Handler `RegJalanByBookingCmd` belum memetakan nilai `PrefixAntrian` dari jadwal booking dokter terkait ke dalam DTO `AddAntrianEmrByRegCommand`. |
| GAP-004 | MINOR | CLOSED | Pengujian unit test dan serialisasi JSON outbox belum mencakup verifikasi pengiriman properti `PrefixAntrian`. |

---

# 4. Open Questions

| ID | Status | Question | Impact |
|---|---|---|---|
| OQ-001 | CLOSED | Bagaimana mekanisme terbaik untuk mendapatkan `PrefixAntrian` pada alur By Booking (`RegJalanByBookingCmd`) mengingat `BookingModel` memuat referensi jadwal dokter? | Menentukan apakah use case cukup me-resolve jadwal booking via `BookingScheduleResolver` atau me-load entitas jadwal secara langsung. |
| OQ-002 | CLOSED | Bagaimana penanganan nilai jika data ruang di `HiDok_Ruang` memiliki kolom `PrefixAntrian` yang bernilai NULL atau spasi kosong? | Memastikan sistem defensif dan tidak mengirimkan nilai null ke EMR25. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Endpoint EMR25 `POST api/Dashboard/addReg` kompatibel penuh dan siap menerima JSON property `prefixAntrian` tanpa memerlukan modifikasi kode di sisi `a054_emr25_api`. |
| ASM-002 | Kolom `BILRG_EmrAntrianOutboundQueue.PayloadJson` (`NVARCHAR(MAX)`) secara fleksibel dapat menampung penambahan properti baru hasil serialisasi JSON tanpa perlu penambahan kolom fisik di database. |
| ASM-003 | Alur pendaftaran IGD (`RegDaruratCreateCmd`) secara operasional memang tidak menggunakan prefix antrian, sehingga pengiriman nilai default string kosong `""` adalah valid dan benar. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Master ruang atau jadwal pada pendaftaran tertentu belum diset nilai `PrefixAntrian`-nya di `HiDok_Ruang`. | LOW | Gunakan fallback nilai string kosong (`""`) agar payload EMR tetap valid dan proses pendaftaran tidak gagal. |
| RISK-002 | Panjang nilai `PrefixAntrian` dari master ruang melebihi standar panjang kolom di downstream EMR (maksimal 2 karakter). | LOW | Terapkan sanitasi pemotongan substring maksimal 2 karakter di sisi Bilreg sebelum payload dibentuk, sejalan dengan proteksi di EMR25. |

---

# 7. Recommendations

## Option A: Penambahan Properti Opsional pada DTO `AddAntrianEmrByRegCommand` dengan Default Value

Menambahkan parameter ke-10 `string PrefixAntrian = ""` pada positional record `AddAntrianEmrByRegCommand`. Pada `RegJalanWalkInCommand`, nilai diambil dari `schedule.LegacyJadwal.Ruang.PrefixAntrian`. Pada `RegJalanByBookingCmd`, jadwal booking di-resolve untuk mengambil `Ruang.PrefixAntrian`. Pada `RegDaruratCreateCmd`, parameter ke-10 tidak perlu diisi sehingga otomatis bernilai `""`.

### Advantages

- Menjamin *zero breaking change* (kompatibel penuh dengan pemanggil lama).
- Memanfaatkan pipeline outbox JSON yang sudah ada tanpa perubahan DDL/tabel database lokal.
- Sederhana, bersih, dan langsung menjawab kebutuhan pengguna.

### Disadvantages

- Positional record bertambah 1 parameter (tetap aman karena diletakkan di urutan terakhir dengan default value).

## Option B: Pemisahan DTO Registrasi Antrian Antara Rawat Jalan dan IGD

Membuat DTO baru khusus untuk rawat jalan (misal `AddAntrianEmrByRegJalanCommand`) dan membiarkan DTO lama untuk IGD.

### Advantages

- Memisahkan secara ketat kontrak data Rawat Jalan dan Rawat Darurat.

### Disadvantages

- Menyebabkan duplikasi kode dan memecah service `IAddAntrianEmrByRegService` serta logika `EmrAntrianOutboundPayloadBuilder`.
- Menambah kompleksitas yang tidak diperlukan untuk perubahan satu properti opsional.

---

# 8. Gap Closure

## GAP-001

### Status: CLOSED

### Decision
Memperbarui record DTO `AddAntrianEmrByRegCommand` pada [IDashboardEmrAddRegService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IDashboardEmrAddRegService.cs) dengan menambahkan parameter opsional ke-10:
`string PrefixAntrian = ""`.

### Rationale
Penambahan parameter opsional di akhir positional record menjamin kompatibilitas mundur penuh (*backward compatibility*). Komponen serialisasi JSON (`System.Text.Json`) akan secara transparan menyertakan properti `"prefixAntrian"` ke dalam kolom `PayloadJson` pada tabel outbox lokal.

### Impact
Kontrak DTO kini memfasilitasi pembawaan kode prefix antrian ke EMR25 tanpa mengganggu pemanggil yang ada.

### Architecture Impact
Perubahan minor pada Application layer interface/contract.

### Resolved By
ica-analyst

### Resolved Date
2026-10-24

---

## GAP-002

### Status: CLOSED

### Decision
Pada [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs), ekstraksi nilai prefix dari objek `schedule.LegacyJadwal.Ruang?.PrefixAntrian` (dengan fallback ke `schedule.Effective.Ruang?.PrefixAntrian` atau string kosong `""`), kemudian teruskan ke instansiasi `AddAntrianEmrByRegCommand`.

### Rationale
Objek `schedule` pada walk-in telah memuat entitas `RuangType` yang datanya berasal dari tabel `HiDok_Ruang`, sehingga nilai prefix dapat langsung digunakan tanpa memerlukan query database tambahan.

### Impact
Antrian pasien rawat jalan walk-in yang dikirim ke EMR25 membawa prefix antrian master ruang.

### Architecture Impact
Penyesuaian use case handler `RegJalanWalkInCommand`.

### Resolved By
ica-analyst

### Resolved Date
2026-10-24

---

## GAP-003

### Status: CLOSED

### Decision
Pada [RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs), resolusikan jadwal praktek dokter terkait booking (melalui `BookingScheduleResolver.Resolve` menggunakan `booking.Dokter`, `booking.TglBerobat`, dan `booking.JamPraktek`) untuk mendapatkan `Ruang.PrefixAntrian`, kemudian teruskan ke instansiasi `AddAntrianEmrByRegCommand`.

### Rationale
Pendaftaran by booking memerlukan lookup jadwal praktek dokter yang bersangkutan guna memperoleh data ruangan tempat praktek berlangsung beserta kode prefix antriannya.

### Impact
Antrian pasien rawat jalan dari perjanjian/booking yang dikirim ke EMR25 membawa prefix antrian master ruang.

### Architecture Impact
Penyesuaian use case handler `RegJalanByBookingCmd`.

### Resolved By
ica-analyst

### Resolved Date
2026-10-24

---

## GAP-004

### Status: CLOSED

### Decision
Menambahkan unit test pada `Bilreg.Test` untuk memverifikasi bahwa:
1. Serialisasi dan deserialisasi `AddAntrianEmrByRegCommand` dengan `PrefixAntrian` berjalan presisi di `EmrAntrianOutboundPayloadBuilder`.
2. Handler `RegJalanWalkInCommand` dan `RegJalanByBookingCmd` memasukkan nilai `PrefixAntrian` yang tepat ke outbox queue.
3. Handler `RegDaruratCreateCmd` tetap berjalan normal dengan `PrefixAntrian` string kosong.

### Rationale
Mencegah regresi pengujian dan membuktikan bahwa integrasi outbox lokal hingga payload EMR berjalan sesuai ekspektasi.

### Impact
Cakupan pengujian otomatis terjamin.

### Architecture Impact
Penambahan skenario pengujian di test suite.

### Resolved By
ica-analyst

### Resolved Date
2026-10-24

---

## OQ-001

### Status: CLOSED

### Decision
Ekstraksi `PrefixAntrian` pada alur By Booking dilakukan dengan me-resolve jadwal dokter memanfaatkan `BookingScheduleResolver.Resolve(...)` menggunakan parameter tanggal berobat, jam praktek, dan dokter dari entitas `BookingModel`.

### Rationale
Pendekatan ini konsisten dengan arsitektur resolusi jadwal di Bilreg dan mampu menangani skenario jadwal reguler maupun jadwal harian (*daily override*) secara elegan.

### Impact
Data ruangan dan prefix antrian yang diperoleh selalu akurat sesuai jadwal praktek pada tanggal berobat pasien.

### Architecture Impact
Konsistensi pemanfaatan `BookingScheduleResolver` di alur registrasi booking.

### Resolved By
ica-analyst

### Resolved Date
2026-10-24

---

## OQ-002

### Status: CLOSED

### Decision
Terapkan sanitasi defensif saat membaca `PrefixAntrian`:
`(prefix ?? string.Empty).Trim()` dan jika panjangnya melebihi 2 karakter, potong maksimal 2 karakter (`Substring(0, 2)`).

### Rationale
Downstream EMR25 mengalokasikan kode prefix antrian maksimal 2 karakter (misal: "A", "B", "P1"). Sanitasi ini mencegah error integrasi akibat ketidaksesuaian input data master.

### Impact
Sistem stabil dan tahan terhadap anomali data di tabel `HiDok_Ruang`.

### Architecture Impact
Pola sanitasi seragam antara sender (Bilreg) dan receiver (EMR25).

### Resolved By
ica-analyst

### Resolved Date
2026-10-24

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Perubahan melibatkan penyesuaian kontrak DTO integrasi lintas batas (*cross-boundary contract* antara Bilreg dan EMR25), kontrak pesan outbox lokal (`BILRG_EmrAntrianOutboundQueue`), pemetaan data pada use case registrasi rawat jalan, serta verifikasi tata kelola transaksi lokal. Diperlukan pembaruan artefak arsitektur formal untuk mendokumentasikan spesifikasi teknis target.

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

Seluruh kesenjangan (GAPs) dan pertanyaan terbuka (OQs) telah dianalisis dan ditutup dengan keputusan afirmatif. Gerbang kelayakan READY-FOR-PLANNING secara resmi diberikan oleh Architect untuk melanjutkan ke tahap Architecture Update dan Planning.

---

# 11. References

Referenced artifacts:

- ISSUE: [ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md) (`ISSUE-BILREG-EMR-PREFIX-ANTRIAN-001`)
- DOMAIN: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)

Referenced codebase locations:

- [IDashboardEmrAddRegService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IDashboardEmrAddRegService.cs)
- [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs)
- [RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs)
- [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs)
- [EmrAntrianOutboundEnqueueService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundEnqueueService.cs)
- [EmrAntrianOutboundPayloadBuilder.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundPayloadBuilder.cs)
- [BILRG_EmrAntrianOutboundQueue.sql](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/AdmisiContext/EmrAntrianOutboundFeature/BILRG_EmrAntrianOutboundQueue.sql)
- [RuangType.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/JadwalPraktekFeature/RuangType.cs)
- [RuangDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/RuangDal.cs)
- [AddRegCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/a054_emr25_api/Emr25.Application/DashboardContext/DashboardAgg/UseCases/AddRegCommand.cs)
- [DashboardController.cs](file:///d:/project_aktif/project_MyHospitalWeb/a054_emr25_api/Emr25.Api/Controllers/DashboardController.cs)
