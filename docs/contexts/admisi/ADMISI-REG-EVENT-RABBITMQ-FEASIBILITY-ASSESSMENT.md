---
Title: Feasibility Assessment - Publikasi Event Registrasi Pasien ke RabbitMQ
Code: ADMISI-REG-EVENT-RABBITMQ
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-10-05
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan perubahan (CHANGE-REQUEST) publikasi event registrasi pasien (Rawat Jalan, Rawat Inap, Rawat Darurat) ke message broker RabbitMQ langsung dari sistem Bilreg (`b09-bilreg-api`), sehubungan dengan rencana penghentian (decommissioning) komponen `BipubApi`.

Referenced artifacts:

- ISSUE: [ADMISI-REG-EVENT-RABBITMQ-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ISSUE.md) (`ISSUE-BILREG-ADMISI-EVENT-001`)
- DOMAIN: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)
- DOMAIN: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

## Objective

Menilai kelayakan teknis, operasional, dan arsitektural untuk mengimplementasikan kapabilitas penerbitan (publishing) event notifikasi registrasi dari `b09-bilreg-api` ke RabbitMQ; mengidentifikasi kesenjangan (gaps), pertanyaan terbuka (open questions), asumsi, risiko, serta opsi pendekatan arsitektur sebelum perancangan teknis difinalisasi.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan investigasi artefak dan kode sumber:

## Existing Behavior

1. **Registrasi Rawat Jalan (Rajal)**:
   - Use case walk-in ditangani oleh `RegJalanCreateHandler` via [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs).
   - Use case booking ditangani oleh `RegJalanByBookingHandler` via [RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs).
   - Handler menyimpan `RegModel`, `RegAktifModel`, antrian, billing, jurnal, dan outbox EMR antrian (`EmrAntrianOutboundEnqueueService`) di dalam satu transaksi database (`TransHelper.NewScope()`).
   - Tidak ada proses raise/publish event notifikasi ke message broker (RabbitMQ).

2. **Registrasi Rawat Inap (Ranap)**:
   - Use case penerimaan permintaan opname ditangani oleh `AdmProcessOpnameRequestHandler` via [AdmProcessOpnameRequestCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessOpnameRequestCmd.cs).
   - Use case reservasi kamar ditangani oleh `AdmProcessReservationHandler` via [AdmProcessReservationCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessReservationCmd.cs).
   - Kedua handler mendelegasikan alur registrasi ke [AdmissionRegistrationOrchestrator.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs).
   - Orkestrator menyimpan `AdmissionModel`, `RegModel`, `RegInapModel`, `RegAktifModel`, dan audit log di dalam `TransHelper.NewScope()`.
   - Tidak ada proses raise/publish event notifikasi registrasi ranap ke RabbitMQ.

3. **Registrasi Rawat Darurat (IGD)**:
   - Ditangani oleh `RegDaruratCreateHandler` via [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs).
   - Handler menyimpan `RegModel`, `RegAktifModel`, tindakan awal, billing, jurnal, dan pendaftaran antrian EMR di dalam `TransHelper.NewScope()`.
   - Tidak ada proses raise/publish event notifikasi ke RabbitMQ.

4. **Karakteristik Implementasi Referensi di `BipubApi`**:
   - Berbasis .NET 6.0 dengan pustaka `MassTransit` dan transport RabbitMQ.
   - Menggunakan package `MyHospital.MsgContract` (`MyHospital.MsgContract.Billing.AdmisiEvents`):
     - `RegRajalCreatedNotifEvent(string RegId)`
     - `RegRanapCreatedNotifEvent(string RegId)`
   - Publikasi dilakukan langsung via `IBus.Publish` melalui `EventPublisher`.
   - `BipubApi` tidak memiliki event maupun handler untuk Registrasi Rawat Darurat (IGD).

5. **Kondisi Komponen Message Broker di `b09-bilreg-api`**:
   - `b09-bilreg-api` saat ini berbasis .NET 8.0.
   - Belum memiliki dependensi pustaka message broker (MassTransit atau RabbitMQ.Client).
   - Belum memiliki dependensi package `MyHospital.MsgContract`.
   - Konfigurasi `appsettings.json` belum memiliki entri koneksi RabbitMQ (`RabbitMqOption`).
   - Namun, Bilreg telah memiliki preseden pola transactional outbox pada [EmrAntrianOutboundFeature](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundEnqueueService.cs) untuk pengiriman data antrian ke subsistem EMR.

## Existing Constraints

1. **Target Runtime & Dependensi**: `b09-bilreg-api` berjalan di .NET 8.0, sedangkan package `MyHospital.MsgContract` pada feed lokal terkompilasi untuk .NET 6.0 (`net6.0`). Kompatibilitas assembly dan versi MassTransit harus dipastikan.
2. **Integritas Transaksional (ACID Scope)**: Semua flow registrasi di Bilreg dibungkus oleh `TransactionScope` (`TransHelper.NewScope()`). Publikasi direct publish harus dilakukan setelah transaksi database berhasil di-commit (`trans.Complete()`) agar tidak terjadi pengiriman event hantu saat database rollback.
3. **Ketersediaan Broker**: Kegagalan jaringan atau terhentinya broker RabbitMQ tidak boleh menggagalkan proses pendaftaran pasien di rumah sakit (resilience requirement: logging error tanpa throw exception ke caller).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | Belum adanya pustaka perantara (message bus client), dependensi message contract, dan konfigurasi koneksi RabbitMQ pada `Bilreg.Api` dan `Bilreg.Infrastructure`. |
| GAP-002 | CRITICAL | Ketiadaan definisi kontrak event registrasi Rawat Darurat (IGD). Package `MyHospital.MsgContract.Billing.AdmisiEvents` hanya memiliki `RegRajalCreatedNotifEvent` dan `RegRanapCreatedNotifEvent`. |
| GAP-003 | MAJOR | Titik pemicu (dispatching trigger) belum terpasang pada use case registrasi di Bilreg: `RegJalanWalkInCommand`, `RegJalanByBookingCmd`, `AdmissionRegistrationOrchestrator` (Ranap), dan `RegDaruratCreateCmd`. |
| GAP-004 | MAJOR | Belum ditetapkannya pola jaminan keandalan pengiriman event (Direct Publish post-commit vs. Transactional Outbox Pattern) untuk menjamin at-least-once delivery tanpa ghost messages. |
| GAP-005 | MINOR | Belum adanya standardisasi penanganan error/logging dan graceful fallback jika koneksi RabbitMQ mengalami gangguan saat event dipublikasikan. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| OQ-001 | Apakah untuk Rawat Darurat (IGD) perlu diterbitkan versi baru dari package `MyHospital.MsgContract` dengan kontrak `RegDaruratCreatedNotifEvent`, atau konsumer downstream dapat menerima format kontrak internal/kontrak alternatif? | Menentukan dependensi eksternal terhadap rilis NuGet package sebelum implementasi registrasi IGD dapat difinalisasi. |
| OQ-002 | Apakah mekanisme penerbitan event harus mengadopsi Transactional Outbox Pattern (seperti pola `EmrAntrianOutboundFeature`) atau cukup Direct Publish setelah `trans.Complete()`? | Mempengaruhi kebutuhan pembuatan tabel outbox, background worker polling/dispatching, dan kompleksitas arsitektur. |
| OQ-003 | Apakah scope publikasi ini hanya mencakup event registrasi baru (`CreatedNotifEvent`), atau apakah event pembatalan registrasi (`RegJalanBatalCmd`, batal admisi ranap) juga harus dipublikasikan ke broker RabbitMQ? | Mempengaruhi batasan use case yang harus dimodifikasi dalam bounded context Admisi dan Admisi Ranap. |
| OQ-004 | Apakah pustaka message broker yang akan diadopsi adalah `MassTransit` (konsisten dengan arsitektur `BipubApi` sebelumnya) atau native `RabbitMQ.Client`? | Menentukan framework abstraction, footprint dependensi NuGet di .NET 8.0, dan konfigurasi DI di `Bilreg.Api`. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Konsumer downstream (misal: antrian, farmasi, kasir, atau EMR) yang saat ini mengonsumsi pesan dari `BipubApi` mendengarkan routing dan schema yang kompatibel dengan tipe `RegRajalCreatedNotifEvent` dan `RegRanapCreatedNotifEvent` dari `MyHospital.MsgContract`. |
| ASM-002 | Konfigurasi server RabbitMQ (host, vhost, username, password) sama dengan cluster broker yang saat ini digunakan di server target (`dev.smart-ics.com` / production). |
| ASM-003 | Muatan payload event hanya membutuhkan atribut identitas registrasi (`string RegId`) sesuai definisi kontrak event eksisting di `MyHospital.MsgContract.Billing.AdmisiEvents`. |
| ASM-004 | Kegagalan teknis saat menghubungi RabbitMQ tidak boleh menggagalkan transaksi penyimpanan registrasi pasien di database Bilreg. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Dual-Write Inconsistency: Jika event dipublikasikan sebelum transaksi commit dan transaksi database rollback, pesan hantu (ghost event) terkirim ke downstream. Sebaliknya jika commit sukses namun direct publish gagal karena network timeout, event hilang. | Ketidaksinkronan data downstream dengan data authoritative Bilreg. | Isolasi direct publish secara ketat di luar `using (trans)` setelah `trans.Complete()`, dibungkus `try-catch` dengan error logging. |
| RISK-002 | Blocking Request Latency: Pemanggilan broker message secara sinkronus di thread HTTP handler saat publish dapat memperlambat waktu respon registrasi pasien. | Menurunkan performa API loket pendaftaran di rumah sakit. | Gunakan asynchronous publish (`PublishAsync` / `Task`) melalui interface publisher abstraction. |
| RISK-003 | Broken Decommissioning of BipubApi: Jika Bilreg mulai mempublikasikan event sementara BipubApi masih aktif dan mempublikasikan event yang sama, terjadi duplikasi event di downstream. | Downstream memproses registrasi ganda (duplicate processing). | Buat rollout checklist yang memastikan koordinasi penghentian publisher di BipubApi bersamaan dengan aktivasi di Bilreg. |

---

# 7. Recommendations

## Option A: Direct Publish Post-Commit via MassTransit Abstraction (SELECTED)

Mengadopsi `MassTransit` pada `Bilreg.Api` dan `Bilreg.Infrastructure`, membuat interface publisher di `Bilreg.Application`, dan memanggil publikasi langsung tepat setelah `trans.Complete()` di use case handler.

### Advantages

- Implementasi lebih cepat dan ringkas, tidak membutuhkan skema tabel database baru maupun worker background khusus.
- Mengikuti pola yang sudah familiar dari implementasi referensi `BipubApi`.

### Disadvantages

- Tidak menjamin at-least-once delivery jika proses crash tepat di antara commit database dan koneksi broker.
- Memerlukan penanganan error `try-catch` agar error koneksi RabbitMQ tidak melempar HTTP 500 ke client yang registrasinya sudah tersimpan.

## Option B: Transactional Outbox Pattern (Dedicated Outbox Table + Worker)

Mengadopsi pola outbox lokal (konsisten dengan preseden `EmrAntrianOutboundFeature`), di mana record event ditulis ke tabel antrian outbox di dalam transaksi database yang sama dengan registrasi, lalu background worker membaca dan mempublikasikannya ke RabbitMQ.

---

# 8. Gap Closure

Resolusi resmi untuk seluruh GAPs dan Open Questions:

## GAP-001

### Status: CLOSED
### Decision
Tambahkan paket `MassTransit` (transport RabbitMQ) dan `MyHospital.MsgContract` ke `Bilreg`, daftarkan konfigurasi `RabbitMqOption` di `appsettings.json`, serta registrasikan layanan MassTransit di `Bilreg.Api`.

### Rationale
Konsisten dengan arsitektur `BipubApi` dan keputusan OQ-004 untuk mengadopsi MassTransit.

### Impact
`Bilreg.Api` dan `Bilreg.Infrastructure` memiliki dependensi MassTransit dan konfigurasi broker RabbitMQ.

### Architecture Impact
Komponen `EventPublisher` dan DI registration `AddMassTransit` ditambahkan ke layer Infrastructure dan Presentation API.

### Resolved By
User & ica-architect

### Resolved Date
2026-10-05

---

## GAP-002

### Status: CLOSED
### Decision
Registrasi Rawat Darurat (IGD) mempublikasikan pesan bertipe `MyHospital.MsgContract.Billing.AdmisiEvents.RegRajalCreatedNotifEvent` dengan payload `RegId`.

### Rationale
Keputusan user pada OQ-001 menyatakan bahwa layanan IGD menggunakan `RegRajalCreatedNotifEvent` sehingga tidak diperlukan penerbitan rilis NuGet package baru untuk tipe event IGD.

### Impact
Handler registrasi IGD langsung dapat menggunakan kontrak pesan yang sudah tersedia di `MyHospital.MsgContract`.

### Architecture Impact
Use case `RegDaruratCreateHandler` memanggil publisher untuk `RegRajalCreatedNotifEvent`.

### Resolved By
User & ica-architect

### Resolved Date
2026-10-05

---

## GAP-003

### Status: CLOSED
### Decision
Pasang pemanggilan publikasi event notifikasi pada handler registrasi baru di luar skop transaksi database (`trans.Complete()`):
- Rawat Jalan: `RegJalanCreateHandler` & `RegJalanByBookingHandler` mempublikasikan `RegRajalCreatedNotifEvent(reg.RegId)`.
- Rawat Darurat: `RegDaruratCreateHandler` mempublikasikan `RegRajalCreatedNotifEvent(reg.RegId)`.
- Rawat Inap: `AdmissionRegistrationOrchestrator` mempublikasikan `RegRanapCreatedNotifEvent(admission.RegId)`.

### Rationale
Memenuhi seluruh kebutuhan fungsional intake perubahan pada [ADMISI-REG-EVENT-RABBITMQ-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ISSUE.md).

### Impact
Seluruh use case pendaftaran pasien baru memicu penerbitan event ke broker secara otomatis.

### Architecture Impact
Abstraksi publisher diinjeksi ke masing-masing use case / orkestrator terkait.

### Resolved By
User & ica-architect

### Resolved Date
2026-10-05

---

## GAP-004

### Status: CLOSED
### Decision
Menerapkan Direct Publish post-commit (`trans.Complete()`) menggunakan abstraksi publisher asynchronous (`Task PublishAsync`), tanpa transactional outbox table untuk fase saat ini.

### Rationale
Sesuai keputusan user pada OQ-002 untuk memprioritaskan kecepatan implementasi dan kemudahan operasional langsung setelah `trans.Complete()`.

### Impact
Tidak memerlukan penambahan tabel outbox database atau background worker scheduler baru.

### Architecture Impact
Event dipublikasikan langsung melalui instance `IBus` / `IEventPublisher` setelah transaksi database selesai di-commit.

### Resolved By
User & ica-architect

### Resolved Date
2026-10-05

---

## GAP-005

### Status: CLOSED
### Decision
Penerbitan event dibungkus dalam blok proteksi `try-catch` dengan pencatatan log peringatan (`_logger.LogWarning / LogError`) sehingga jika RabbitMQ offline atau timeout, pengecualian tidak menggagalkan response registrasi pasien.

### Rationale
Registrasi medis dan transaksi keuangan rumah sakit adalah domain inti yang tidak boleh dibatalkan hanya karena kegagalan notifikasi broker eksternal (resilience).

### Impact
Sistem tetap memberikan respon sukses kepada petugas loket/kiosk meskipun broker bermasalah.

### Architecture Impact
Logika proteksi dimasukkan ke dalam service publisher abstraction atau handler wrapper.

### Resolved By
User & ica-architect

### Resolved Date
2026-10-05

---

## OQ-001

### Status: CLOSED
### Decision
Untuk Rawat Darurat (IGD), gunakan tipe event `MyHospital.MsgContract.Billing.AdmisiEvents.RegRajalCreatedNotifEvent`.

### Rationale
Keputusan eksplisit dari user/stakeholder; downstream consumer memperlakukan event kunjungan darurat selaras dengan event kunjungan rawat jalan.

### Impact
Tidak ada blocker dependensi paket eksternal baru.

### Architecture Impact
Kontrak `RegRajalCreatedNotifEvent` dipetakan pada domain IGD.

### Resolved By
User

### Resolved Date
2026-10-05

---

## OQ-002

### Status: CLOSED
### Decision
Mekanisme penerbitan event cukup Direct Publish setelah `trans.Complete()`.

### Rationale
Keputusan eksplisit dari user/stakeholder untuk menyederhanakan arsitektur fase saat ini.

### Impact
Implementasi langsung di handler setelah commit database.

### Architecture Impact
Desain Direct Publish via MassTransit `IBus`.

### Resolved By
User

### Resolved Date
2026-10-05

---

## OQ-003

### Status: CLOSED
### Decision
Scope publikasi event saat ini terbatas hanya untuk Registrasi Baru (Rawat Jalan, Rawat Darurat, Rawat Inap). Batal admisi/batal registrasi belum perlu mempublikasikan event ke RabbitMQ.

### Rationale
Keputusan eksplisit dari user/stakeholder untuk membatasi ruang lingkup hanya pada use case pendaftaran aktif.

### Impact
Use case pembatalan (`RegJalanBatalCmd`, batal admisi ranap) dikecualikan dari perubahan ini.

### Architecture Impact
Fokus integrasi hanya pada use case create/registrasi.

### Resolved By
User

### Resolved Date
2026-10-05

---

## OQ-004

### Status: CLOSED
### Decision
Pustaka yang diadopsi adalah `MassTransit` dengan transport RabbitMQ, konsisten dengan implementasi `BipubApi`.

### Rationale
Keputusan eksplisit dari user/stakeholder demi standardisasi stack messaging pada ekosistem `project_MyHospitalWeb`.

### Impact
Penambahan package NuGet `MassTransit.RabbitMQ` (atau `MassTransit`) pada solusi Bilreg.

### Architecture Impact
Konfigurasi standard MassTransit pada DI container `Bilreg.Api`.

### Resolved By
User

### Resolved Date
2026-10-05

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Perubahan ini memperkenalkan boundary integrasi eksternal baru ke message broker (RabbitMQ), memerlukan integrasi library MassTransit pada runtime .NET 8.0, konfigurasi DI, interface publisher di Application layer, serta modifikasi implementasi pada 4 use case handler registrasi di 2 bounded context (Admisi dan Admisi Ranap).

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

Seluruh gap kritikal dan pertanyaan terbuka telah ditutup dengan keputusan definitif dari stakeholder dan arsitek. Tahap Feasibility Assessment selesai dan gerbang **READY-FOR-PLANNING** telah diberikan oleh `ica-architect`.

---

# 11. References

Referenced artifacts:

- ISSUE: [ADMISI-REG-EVENT-RABBITMQ-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ISSUE.md)
- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)
- DOMAIN Ranap: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

Referenced codebase locations:

- [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs)
- [RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs)
- [AdmProcessOpnameRequestCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessOpnameRequestCmd.cs)
- [AdmProcessReservationCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessReservationCmd.cs)
- [AdmissionRegistrationOrchestrator.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs)
- [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs)
- [Program.cs (Bipub.Api)](file:///d:/project_aktif/project_MyHospitalWeb/a045_bipub_billingpublicapi/Bipub.Api/Program.cs)
