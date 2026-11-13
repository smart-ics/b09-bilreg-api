---
Title: Feasibility Assessment - Penerapan Prefix Ruangan pada Sesi Antrian Poli/Dokter di Bilreg, Display Antrian, dan Worklist Rawat Jalan
Code: ANTRIAN-POLI-PREFIX
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-11-12
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan atas permintaan perubahan (`ISSUE-BILREG-ANTRIAN-POLI-PREFIX-001`) untuk menerapkan kode prefix ruangan poliklinik (`HiDok_Ruang.prefixAntrian`) secara menyeluruh pada antrian pemeriksaan dokter di tiga repositori:

1. **`b09-bilreg-api`**: Pencatatan snapshot prefix pada sesi antrian dokter (`BILRG_Antrian`) dan penyajian label nomor antrian pada kontrak API.
2. **`c013-kiosk-queue-display-web`**: Pencetakan tiket registrasi di anjungan mandiri (`kiosk-web`) serta tampilan dan pemanggilan suara di layar monitor klinik (`display-web`).
3. **`c012_myhospital_web`**: Tampilan nomor antrian pada alur pendaftaran pasien (`Admisi`) dan daftar tunggu periksa dokter/perawat (`Outpatient`).

Referenced artifacts:

- ISSUE: [ANTRIAN-POLI-PREFIX-ISSUE.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-ISSUE.md) (`ISSUE-BILREG-ANTRIAN-POLI-PREFIX-001`)
- DOMAIN (Admisi Rajal): [admisi-rajal-domain.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)
- DOMAIN (Patient Tracker / Queue): [TRACKER-DOMAIN.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/pasien-tracker/TRACKER-DOMAIN.md)
- Related Architecture (EMR Prefix): [ADMISI-EMR-PREFIX-ANTRIAN-ARCHITECTURE.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-ARCHITECTURE.md)

## Objective

Menilai apakah permintaan perubahan ini layak diterapkan di ketiga repositori dengan aman; mengidentifikasi kesenjangan teknis dan kontrak data di setiap repo; serta merumuskan pertanyaan terbuka dan risiko bisnis yang harus diputuskan sebelum masuk ke tahap arsitektur.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan penelusuran artefak dan kode sumber pada ketiga repositori:

## Existing Behavior

1. **Master Ruang dan Sesi Antrian di `b09-bilreg-api`**:
   - Tabel master ruangan `HiDok_Ruang` sudah memiliki kolom `prefixAntrian` (contoh: `"A"`, `"B"`, `"OB"`), dikelola oleh `RuangDal` dan `RuangRepo`, serta diekspos oleh `GET /api/Ruang/list`, `GET /api/Ruang/{id}`, dan `GET /api/Antrian/header/{tglYmd}/list`.
   - Saat registrasi Rawat Jalan (Walk-In dan By Booking), nilai `PrefixAntrian` sudah diambil dari ruang jadwal dokter, lalu dikirimkan ke EMR25 melalui Outbox Queue (`BILRG_EmrAntrianOutboundQueue`).
   - Tabel `BILRG_Antrian` dipakai bersama oleh antrian Loket Admisi dan antrian Poli/Dokter. Kolom `QueuePrefixSnapshot` (`CHAR(1)`) pada tabel ini sudah dipakai secara tulis-sekali (*write-once*) oleh Loket Admisi melalui `AntrianFactory.Create(AdmissionServicePointModel, DateOnly)`.
   - Keempat alur yang membuka sesi antrian dokter (`RegJalanWalkInCommand`, `RegJalanUbahKunjunganCmd`, `BookingCreateCmd`, `BookingCreateFromHidokCommand`) memanggil `AntrianFactory.Create(DateOnly, JadwalPraktekType/Effective)` tanpa meneruskan nilai prefix ruangan. Akibatnya, `QueuePrefixSnapshot` pada sesi dokter selalu tersimpan kosong (`""`).
   - Query tampilan layar klinik (`ClinicQueueGetDisplaySnapshotHandler`) sudah memanggil `queue.FormatQueueLabel(call.NoUrut)`, dan unit test (`ClinicQueueGetDisplaySnapshotHandlerTest`) bahkan menguji skenario `QueuePrefixSnapshot: "A"` dengan ekspektasi output `"A0001"` dan `"A0003"`. Namun di lingkungan nyata, fungsi tersebut selalu kembali ke angka murni karena `QueuePrefixSnapshot` kosong.
   - Endpoint pendaftaran (`RegJalanCreateResponse`) dan detail antrian dokter (`QueGetAntrianResponse`) hanya mengembalikan `NoAntrian` bertipe `int`.

2. **Anjungan Mandiri dan Layar Monitor di `c013-kiosk-queue-display-web`**:
   - **Layar Monitor (`display-web`)**: Skema `clinicQueueCallItemSchema` dan `clinicQueueWaitingItemSchema` di `packages/shared-types/src/clinic.ts` sudah menyiapkan properti string `nomorAntrean` dan `queueLabel`. Komponen `ClinicDisplayLayout.vue` dan mesin suara `clinicAnnouncement.ts` sudah mendukung penuh pemecahan huruf dan angka (`parseQueueLabel`) untuk memutar audio `letters/{huruf}.wav` + `numbers/{angka}.wav`.
   - **Anjungan Kiosk (`kiosk-web`)**: Pendaftaran mandiri memakai skema `returnCreateWalkInSchema` (`{ regId: string, noAntrian: number }`). Cetakan struk pendaftaran di `useKioskSelfPrint.ts` dan `registrationReceipt.ts` hanya menerima `noAntrian` bertipe angka.

3. **Aplikasi Web Utama di `c012_myhospital_web`**:
   - **Modul `Admisi`**: Skema validasi (`registrasi.ts`, `booking.ts`) mengunci `noAntrian` harus bertipe angka bulat (`z.number().int().nonnegative()`). Komponen UI (`LegacyRegistrationWorkspace.vue`, `AppointmentReg.vue`, `TrackerEventDetail.vue`) menampilkan angka murni tersebut.
   - **Modul `Outpatient`**: Header jadwal dokter sudah menerima properti `ruang.prefixAntrian` dari `QueListAntrianHeaderQuery`, tetapi baru memakainya untuk badge nama ruangan. Daftar antrian (`antrianDetailSchema`) dan komponen UI (`TindakanWorkbenchColumn.vue`, `PatientInfoBanner.vue`) hanya menampilkan `patient.noAntrian` bertipe angka.

## Existing Constraints

1. **Batas Kepemilikan Konteks (`admisi-rajal-domain.md`)**: Konteks Admisi Rajal hanya memiliki data jadwal dan ruangan tujuan (`HiDok_Ruang`), sedangkan kepemilikan sesi antrian, entri antrian, dan nomor urut berada pada konteks Patient Tracker (`AntrianModel`).
2. **Integritas Pengurutan Numerik**: Kolom dan properti `NoUrut` / `NoAntrian` harus tetap bertipe numerik (`int`) agar pengurutan daftar tunggu dan mekanisme kunci sekuens (*sequence locking*) tetap akurat.
3. **Kompatibilitas Skema Klien (Zero Breaking Change)**: Puluhan skema validasi Zod di `c012_myhospital_web` dan `c013-kiosk-queue-display-web` memvalidasi `noAntrian` sebagai `number`. Mengubah tipe `noAntrian` menjadi string akan mematahkan klien yang ada.
4. **Ketidakberubahan Snapshot (*Snapshot Immutability*)**: `QueuePrefixSnapshot` pada `BILRG_Antrian` sengaja tidak diubah lewat perintah `UPDATE` di `AntrianDal` demi menjaga jejak audit tiket yang sudah tercetak.

---

# 3. Gap Analysis

| ID | Severity | Status | Gap |
|---|---|---|---|
| GAP-001 | CRITICAL | CLOSED | **(`b09-bilreg-api`)** Kolom fisik `BILRG_Antrian.QueuePrefixSnapshot` bertipe `CHAR(1)` dan domain `AntrianModel` memakai validator `AdmissionServicePointModel.NormalizePrefix` yang membatasi panjang tepat 1 huruf `[A-Z]`, sedangkan prefix ruang poli di `HiDok_Ruang` dapat terdiri dari 1 hingga 2 karakter (misalnya `"OB"`). |
| GAP-002 | CRITICAL | CLOSED | **(`b09-bilreg-api`)** Pembuatan sesi antrian dokter di `AntrianFactory` pada 4 alur registrasi/booking tidak meneruskan `PrefixAntrian` dari ruangan jadwal ke `AntrianModel`. |
| GAP-003 | MAJOR | CLOSED | **(`b09-bilreg-api`)** `AntrianModel.FormatQueueLabel` memformat nomor dengan padding 4 digit (`"A0003"`), yang belum tentu sesuai dengan format ringkas yang diharapkan untuk poli (`"A3"`). |
| GAP-004 | MAJOR | CLOSED | **(`b09-bilreg-api`)** Response pendaftaran (`RegJalanCreateResponse`), booking (`BookingCreateResponse`), dan detail antrian (`QueGetAntrianResponse`) belum menyertakan atribut string berprefix atau informasi prefix antrian secara aditif. |
| GAP-005 | MAJOR | CLOSED | **(`c013-kiosk-queue-display-web`)** Skema `returnCreateWalkInSchema` serta pencetakan struk registrasi di `kiosk-web` (`useKioskSelfPrint.ts`, `registrationReceipt.ts`) belum menerima dan mencetak nomor antrian berprefix. |
| GAP-006 | MAJOR | CLOSED | **(`c012_myhospital_web`)** Modul `Admisi` dan `Outpatient` belum menampilkan label nomor antrian berprefix pada hasil registrasi, banner pasien, maupun daftar tunggu poliklinik. |
| GAP-007 | MINOR | CLOSED | **(`b09-bilreg-api`)** Proyeksi daftar kerja loket admisi (`AdmissionQueueOperationalProjection`) dan `ClinicQueueDisplayQueries` berbagi tabel `BILRG_Antrian` dan masih memakai aturan format atau penyaringan berbasis asumsi awalan `ServicePointCode` / SQL hardcode `RIGHT('0000' + ...)`. |

---

# 4. Open Questions

| ID | Status | Question | Impact |
|---|---|---|---|
| OQ-001 | CLOSED | **Format tampilan standar antrian poli**: Apakah format nomor antrian poli/dokter yang disepakati adalah tanpa angka nol di depan (contoh: `A3`), 3 digit (`A003` / `A-003`), atau 4 digit (`A0003` seperti pada `ClinicQueueGetDisplaySnapshotHandlerTest` dan loket admisi)? | Menentukan aturan pemformatan pada `AntrianModel`, teks layar `display-web`, audio `clinicAnnouncement.ts`, dan struk `kiosk-web`. |
| OQ-002 | CLOSED | **Ruang dokter kosong atau pindah ruangan**: Bagaimana perlakuan terhadap `QueuePrefixSnapshot` jika saat pasien pertama mendaftar/booking jadwal dokter belum memiliki ruangan (`RuangId = "-"` / `prefixAntrian = ""`), atau jika petugas poli memindahkan ruangan dokter di tengah hari praktek melalui `c012_myhospital_web`? | Menentukan apakah prefix dikunci permanen saat sesi dibuat (immutable snapshot), boleh diperbarui saat ruang ditentukan, atau apakah pemanggilan klinik memakai prefix ruang tempat memanggil. |
| OQ-003 | CLOSED | **Aturan karakter `HiDok_Ruang.prefixAntrian`**: Apakah prefix ruangan poli hanya boleh huruf `[A-Z]` (1–2 huruf seperti `"A"` atau `"OB"`), atau boleh memuat angka/tanda hubung? Bagaimana perlakuan jika data master diisi nilai sentinel `"-"`? | Menentukan aturan normalisasi domain di `AntrianModel` serta kompatibilitas parser suara `parseQueueLabel` di `c013`. |
| OQ-004 | CLOSED | **Strategi transisi kontrak API**: Apakah seluruh endpoint registrasi dan detail antrian di `b09-bilreg-api` wajib tetap mempertahankan `noAntrian` bertipe integer dan menambahkan properti baru (misalnya `queueLabel` / `nomorAntrean` / `prefixAntrian`) agar klien lama tidak rusak selama masa transisi? | Menentukan strategi perubahan kontrak DTO di `b09-bilreg-api` dan pembaruan skema Zod di `c012` serta `c013`. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Setiap ruangan poliklinik di `HiDok_Ruang` yang menggunakan fitur antrian berprefix telah diisi nilai `prefixAntrian` yang valid sebelum sesi antrian hari tersebut terbentuk. |
| ASM-002 | Mesin audio pada `c013-kiosk-queue-display-web` (`display-web`) hanya perlu membaca huruf dan angka secara terpisah, sehingga sudah siap menerima input baik format `"A3"` maupun `"A0003"` selama berawalan huruf diikuti angka. |
| ASM-003 | Pendaftaran Rawat Darurat (IGD) tetap dikecualikan dan tidak menggunakan prefix antrian. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | **Breaking change pada skema klien**: Mengubah tipe field `noAntrian` yang ada dari `int` menjadi `string` akan menyebabkan kegagalan parsing Zod di `c012_myhospital_web` dan `c013-kiosk-queue-display-web`. | HIGH | Pertahankan `noAntrian` tetap bertipe `int` pada semua endpoint yang sudah ada, dan tambahkan atribut string baru secara aditif. |
| RISK-002 | **Kegagalan simpan / SQL truncation**: Mengisi `QueuePrefixSnapshot` dengan kode ruang poli 2 karakter (misal `"OB"`) sebelum kolom `CHAR(1)` dan validator domain diperlebar akan memicu kegagalan pendaftaran pasien. | HIGH | Pastikan perubahan skema kolom database dan pelonggaran aturan normalisasi pada domain dijalankan terlebih dahulu sebelum mengaktifkan pengisian prefix di factory. |
| RISK-003 | **Pencampuran antrian loket dan poli**: Pengisian `QueuePrefixSnapshot` pada baris poli di `BILRG_Antrian` dapat memunculkan nomor antrian poli pada daftar kerja loket admisi jika filter `ServicePointIds` pada `AdmissionQueueOfficerWorklistQry` dipanggil tanpa batasan layanan loket. | MEDIUM | Perketat isolasi penyaringan jenis sesi antrian antara Loket Admisi dan Poli Dokter pada proyeksi loket. |
| RISK-004 | **Data historis kosong**: Sesi antrian poli yang terbentuk sebelum fitur ini aktif atau ruangan tanpa prefix akan memiliki `QueuePrefixSnapshot = ""`. | LOW | Terapkan fallback tampilan ke angka urut murni (`NoUrut.ToString()`) apabila snapshot prefix kosong. |

---

# 7. Recommendations

Secara bisnis dan teknis, change request ini **LAYAK DITERAPKAN** di ketiga repositori karena fondasi utamanya (master `HiDok_Ruang`, kolom `QueuePrefixSnapshot`, query display `ClinicQueueDisplayQueries`, dan parser audio `c013`) sudah tersedia. Namun, penerapan harus dilakukan secara terkoordinasi setelah **OQ-001 s.d. OQ-004** diputuskan.

Berikut adalah opsi pendekatan yang direkomendasikan untuk penutupan gap (keputusan final arsitektur ada pada `ica-architect`):

## Option A: Snapshot Prefix di Sesi Antrian (`BILRG_Antrian`) dengan Kontrak API Aditif (Direkomendasikan)

Memperlebar dukungan `BILRG_Antrian.QueuePrefixSnapshot` agar dapat menampung prefix poli (1–2 karakter), mengisi snapshot tersebut saat sesi dokter dibuat di `AntrianFactory`, serta menambahkan properti label string aditif pada response API Bilreg untuk dikonsumsi `c012` dan `c013`.

### Advantages

- Menjaga integritas historis tiket (*audit trail*) karena prefix terekam permanen pada sesi transaksi, tidak berubah meskipun master ruangan diubah di kemudian hari.
- Menghilangkan kebutuhan *join* ulang ke master jadwal/ruang pada saat pemanggilan dan tampilan monitor display.
- Kompatibel penuh (*zero breaking change*) bagi klien yang masih mengandalkan `noAntrian` bertipe angka.

### Disadvantages

- Memerlukan perubahan skema DDL pada kolom `BILRG_Antrian.QueuePrefixSnapshot`.
- Jika dokter berpindah ruang di tengah jam praktek setelah sesi terbuka, snapshot pada header `BILRG_Antrian` tidak otomatis berubah (kecuali disepakati kebijakan pembaruan ruang pada OQ-002).

## Option B: Penggabungan Label Hanya di Layer Presentasi / Query Runtime (Tanpa Snapshot di `BILRG_Antrian`)

Membiarkan `BILRG_Antrian.QueuePrefixSnapshot` tetap kosong untuk sesi dokter, lalu melakukan *lookup* `Ruang.PrefixAntrian` setiap kali query API dipanggil atau menggabungkannya langsung di frontend `c012` dan `c013`.

### Advantages

- Tidak memerlukan perubahan skema tabel database di `b09-bilreg-api`.
- Otomatis mengikuti perubahan ruangan jika dokter dipindahkan ruangannya di tengah hari.

### Disadvantages

- Merusak integritas historis tiket jika master prefix ruang diubah di kemudian hari.
- Menambah beban *join* dan kompleksitas logika penggabungan yang berulang di tiga repositori terpisah (`b09`, `c012`, `c013`).
- Query display (`ClinicQueueDisplayQueries`) tidak dapat memanfaatkan `FormatQueueLabel` bawaan secara seragam tanpa lookup tambahan.

---

# 8. Gap Closure

*Seluruh Open Question (`OQ-001` s.d. `OQ-004`) dan Gap (`GAP-001` s.d. `GAP-007`) telah dibahas bersama pemilik produk melalui sesi klarifikasi terstruktur dan ditutup dengan rincian keputusan sebagai berikut.*

## GAP-001

Status: CLOSED

- **Decision**: Mendukung penyimpanan dan validasi prefix ruang poli hingga 2 huruf kapital `A-Z` pada sesi antrian poli/dokter, sedangkan prefix kosong atau sentinel `"-"` dinormalisasi menjadi kosong (`""` / tanpa prefix).
- **Rationale**: Data master `HiDok_Ruang.prefixAntrian` dapat berisi 1 hingga 2 huruf (contoh: `"A"`, `"OB"`) dan dapat bernilai kosong atau `"-"` pada ruangan tertentu.
- **Impact**: Kapasitas penyimpanan `BILRG_Antrian.QueuePrefixSnapshot` dan aturan normalisasi domain untuk konteks poli perlu mengakomodasi 0 sampai 2 huruf kapital tanpa merusak aturan loket admisi (1 huruf).
- **Architecture Impact**: Perlu penyesuaian skema database (`BILRG_Antrian.QueuePrefixSnapshot` dari `CHAR(1)` ke tipe yang menampung maksimal 2 karakter) serta pemisahan aturan validasi prefix poli dari `AdmissionServicePointModel.NormalizePrefix` yang khusus 1 karakter.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## GAP-002

Status: CLOSED

- **Decision**: Meneruskan `PrefixAntrian` dari data ruang jadwal dokter saat sesi antrian dibuat pada seluruh alur pendaftaran dan booking poli/dokter, dengan dukungan pengisian susulan (*backfill*) satu kali apabila sesi sebelumnya dibuat tanpa prefix (`""`) lalu jadwal dokter mendapat ruang ber-prefix, dan tetap terkunci permanen setelah terisi.
- **Rationale**: Menjamin tiket pasien yang sudah tercetak tidak berubah di tengah hari praktek ketika dokter berpindah ruang, sekaligus menangani kasus jadwal awal yang belum menetapkan ruangan (`RuangId = "-"`).
- **Impact**: Seluruh alur pembuatan dan pembaruan sesi antrian poli (registrasi walk-in, ubah kunjungan, dan booking) perlu membaca dan mewariskan prefix ruang bila tersedia.
- **Architecture Impact**: Perubahan desain pada factory dan application handler yang membuat atau memperbarui `AntrianModel` untuk poli agar mengambil `PrefixAntrian` dari ruang terkait serta aturan immutability bersyarat pada persistensi snapshot.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## GAP-003

Status: CLOSED

- **Decision**: Menetapkan format ringkas tanpa angka nol di depan (contoh: `A3`, `OB12`) khusus untuk Antrian Poli/Dokter, sedangkan Antrian Loket Admisi tetap mempertahankan format 4 digit (`A0001`).
- **Rationale**: Pasien poliklinik terbiasa membaca nomor urut ringkas seperti `A3`, sedangkan loket admisi tetap mengikuti format operasional existing (`A0001`).
- **Impact**: Pemformatan label pada antrian poli menghasilkan string tanpa leading zero, termasuk pembaruan ekspektasi unit test `ClinicQueueGetDisplaySnapshotHandlerTest` yang sebelumnya mengharapkan `"A0001"` / `"A0003"`.
- **Architecture Impact**: Pemisahan strategi pemformatan label antrian pada `AntrianModel` (atau pemformat khusus poli vs loket admisi) agar `AdmissionQueue` tetap 4 digit dan `ClinicQueue` ringkas.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## GAP-004

Status: CLOSED

- **Decision**: Menjaga backward compatibility kontrak API dengan tetap mempertahankan `noAntrian` bertipe `int` pada database dan seluruh payload response, serta menambahkan properti string berprefix secara aditif (misalnya `queueLabel` / `prefixAntrian`) pada layer response DTO untuk endpoint pendaftaran, booking, dan detail antrian.
- **Rationale**: Klien existing (`c012_myhospital_web` dan `c013-kiosk-queue-display-web`) maupun logika pengurutan antrian masih mengandalkan angka urut bertipe integer sehingga perubahan destruktif pada tipe `noAntrian` harus dihindari.
- **Impact**: Klien lama tetap berfungsi tanpa error parsing, sementara klien yang sudah diperbarui dapat langsung membaca label siap tampil dari API tanpa merakit ulang di UI.
- **Architecture Impact**: Perancangan kontrak DTO response aditif pada endpoint `RegController`, `BookingController`, dan `AntrianController` (`GET /api/Antrian/{id}`) serta pemetaan nilainya dari domain di application/query handler.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## GAP-005

Status: CLOSED

- **Decision**: Memperbarui skema validasi kontrak dan alur pencetakan struk serta pengumuman suara pada `c013-kiosk-queue-display-web` agar menggunakan label antrian poli format ringkas (`A3`) secara konsisten di kiosk dan layar display poliklinik.
- **Rationale**: Kiosk self-print dan layar display poliklinik adalah titik interaksi langsung pasien sehingga harus menampilkan dan melafalkan kode antrian yang sama persis dengan nomor di API.
- **Impact**: Skema Zod dan generator struk di `kiosk-web` perlu menerima dan mencetak label berprefix secara opsional/aditif, sementara `display-web` memastikan label ringkas diproses konsisten oleh tampilan dan parser suara (`parseQueueLabel`).
- **Architecture Impact**: Pembaruan kontrak skema `packages/shared-types` dan alur rendering struk di `apps/kiosk-web` serta verifikasi pemutaran suara di `apps/display-web` untuk prefix 1–2 huruf.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## GAP-006

Status: CLOSED

- **Decision**: Memperbarui tampilan antrian pada modul `Admisi` dan `Outpatient` di `c012_myhospital_web` untuk menampilkan label ringkas (`A3`) hasil gabungan server pada bukti registrasi, banner pasien, dan daftar tunggu/pemanggilan poliklinik.
- **Rationale**: Petugas admisi dan poliklinik memerlukan visibilitas nomor antrian berprefix yang seragam dengan struk pasien dan layar tunggu display.
- **Impact**: Komponen UI registrasi rawat jalan dan worklist/pemanggilan poliklinik di `c012_myhospital_web` perlu membaca properti label aditif dari API dengan fallback aman ke `noAntrian`.
- **Architecture Impact**: Pembaruan tipe DTO frontend dan pemetaan tampilan pada modul `Admisi` dan `Outpatient` di `c012_myhospital_web`.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## GAP-007

Status: CLOSED

- **Decision**: Memastikan proyeksi daftar kerja dan query pembacaan `BILRG_Antrian` memisahkan aturan format antara konteks Loket Admisi (4 digit, contoh `A0001`) dan konteks Antrian Poli/Dokter (ringkas, contoh `A3`), tanpa mengganggu query worklist loket admisi.
- **Rationale**: Kedua konteks berbagi tabel `BILRG_Antrian`, sehingga logika proyeksi SQL dan query display tidak boleh menerapkan format poli ke loket admisi atau sebaliknya.
- **Impact**: `AdmissionQueueOperationalProjection` dan worklist admisi tetap mengeluarkan format 4 digit, sedangkan worklist/display poli mengeluarkan format ringkas.
- **Architecture Impact**: Isolasi aturan proyeksi dan pemformatan label berdasarkan jenis konteks antrian pada query/read-model `b09-bilreg-api`.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## OQ-001

Status: CLOSED

- **Decision**: Antrian Poli/Dokter menggunakan format ringkas tanpa angka nol di depan (contoh: `A3`, `OB12`), sedangkan Antrian Loket Admisi tetap menggunakan format 4 digit (`A0001`).
- **Rationale**: Format ringkas lebih mudah dibaca dan disebut untuk antrian dokter per ruangan, sekaligus memenuhi ekspektasi operasional loket admisi yang sudah berjalan.
- **Impact**: Seluruh titik poliklinik di `b09-bilreg-api`, `c012_myhospital_web`, dan `c013-kiosk-queue-display-web` memakai format `A3` ketika prefix tersedia, dan kembali ke angka murni (contoh `3`) jika ruangan tidak memiliki prefix.
- **Architecture Impact**: Pemisahan formatter label antrian poli dari formatter 4 digit loket admisi di domain/query layer `b09-bilreg-api` dan penyesuaian ekspektasi test suite terkait.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## OQ-002

Status: CLOSED

- **Decision**: `QueuePrefixSnapshot` diisi saat sesi antrian dibuat; apabila sesi sebelumnya memiliki snapshot kosong (`""`) karena jadwal belum mempunyai ruangan, snapshot boleh diisi susulan (*backfill*) satu kali saat ruangan ditetapkan; setelah snapshot terisi (tidak kosong), nilai dikunci permanen (*immutable*) untuk hari praktek tersebut.
- **Rationale**: Mencegah ketidaksesuaian nomor antara tiket fisik yang sudah diterima pasien lebih awal dan panggilan di layar display jika terjadi perpindahan ruangan di tengah hari, sambil tetap mengakomodasi jadwal dokter yang baru diberi ruang belakangan.
- **Impact**: Sesi yang sudah mempunyai prefix tidak akan berubah labelnya meski ruangan dokter diganti di tengah hari praktek; sesi awal tanpa prefix otomatis mewarisi prefix ketika ruangan mulai terisi dan pasien berikutnya mendaftar/memanggil.
- **Architecture Impact**: Aturan lifecycle dan persistensi kondisional `QueuePrefixSnapshot` pada aggregate `AntrianModel` serta repositori `AntrianDal` (`Update` hanya mengisi kolom saat nilai tersimpan masih kosong).
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## OQ-003

Status: CLOSED

- **Decision**: Prefix ruang poli yang sah adalah 1 sampai 2 huruf kapital `A-Z` (contoh: `A`, `B`, `OB`). Nilai kosong (`""` / `null`) atau sentinel `"-"` dianggap tidak memiliki prefix dan dinormalisasi menjadi kosong (`""`).
- **Rationale**: Kompatibel dengan parser audio `parseQueueLabel` di `c013-kiosk-queue-display-web` yang memisahkan awalan huruf dan angka urut, serta mencegah karakter placeholder `"-"` muncul di struk atau layar tunggu.
- **Impact**: Data master `HiDok_Ruang.prefixAntrian` bernilai `"-"` atau kosong menghasilkan tampilan nomor urut polos (contoh `3`), sedangkan huruf kecil dinormalisasi menjadi kapital dan input selain 1–2 huruf `A-Z` ditolak oleh domain validator.
- **Architecture Impact**: Validator dan normalizer domain khusus prefix poli di `b09-bilreg-api` yang mendukung panjang 0..2 karakter `[A-Z]` dan memetakan sentinel `"-"`.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

## OQ-004

Status: CLOSED

- **Decision**: Persistensi database tetap menyimpan `NoUrut` bertipe `int` dan `QueuePrefixSnapshot` di kolom terpisah; pada kontrak API `b09-bilreg-api` (`RegController`, `BookingController`, `GET /api/Antrian/{id}`), properti `noAntrian` bertipe `int` tetap dipertahankan dan ditambah properti string aditif (misalnya `queueLabel` / `prefixAntrian`) yang digabung pada layer response DTO (*zero breaking change*).
- **Rationale**: Memastikan nol gangguan (*zero breaking change*) terhadap klien dan kontrak existing saat deployment bertahap lintas 3 repositori, sekaligus menjaga integritas urutan numerik di database.
- **Impact**: Konsumen API tetap menerima `noAntrian` numerik dan dapat langsung mengadopsi field label string aditif di `c012_myhospital_web` maupun `c013-kiosk-queue-display-web`.
- **Architecture Impact**: Penambahan field aditif pada DTO response kontrak `b09-bilreg-api` dan pembaruan skema Zod opsional/aditif pada frontend tanpa mengubah kolom `NoUrut` di database.
- **Resolved By**: User / ica-analyst
- **Resolved Date**: 2026-11-12

---

# 9. Architecture Applicability

## Decision

ARCHITECTURE-REQUIRED

## Rationale

Perubahan ini menyentuh batas lintas konteks (*Patient Tracker* dan *Admisi Rajal*), melibatkan perubahan skema persistensi database (`BILRG_Antrian`), perubahan aturan invariant domain (`AntrianModel`), penambahan kontrak DTO API di `b09-bilreg-api`, serta perubahan lintas 3 repositori (`b09-bilreg-api`, `c012_myhospital_web`, `c013-kiosk-queue-display-web`). Oleh karena itu, definisi target arsitektur formal mutlak diperlukan setelah Gap Closure diselesaikan.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status

READY-FOR-PLANNING

## Notes

Seluruh gap (`GAP-001` s.d. `GAP-007`) dan pertanyaan terbuka (`OQ-001` s.d. `OQ-004`) telah ditutup dengan keputusan bisnis/produk yang tercatat lengkap. `ica-architect` telah meninjau kelengkapan seluruh kriteria penutupan gap dan memberikan persetujuan gerbang **READY-FOR-PLANNING** pada tanggal 2026-11-12 untuk dilanjutkan ke perancangan **ARCHITECTURE** dan penyusunan **IMPLEMENTATION-PLAN**.

---

# 11. References

Referenced artifacts:

- ISSUE: [ANTRIAN-POLI-PREFIX-ISSUE.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-ISSUE.md)
- DOMAIN: [admisi-rajal-domain.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)
- DOMAIN: [TRACKER-DOMAIN.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/pasien-tracker/TRACKER-DOMAIN.md)

Referenced codebase locations:

- `b09-bilreg-api`:
  - [BILRG_Antrian.sql](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_Antrian.sql)
  - [AntrianModel.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs)
  - [AntrianFactory.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianFactory.cs)
  - [AdmissionServicePointModel.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AdmissionServicePointModel.cs)
  - [ClinicQueueDisplayQueries.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/UseCases/ClinicQueueDisplayQueries.cs)
  - [ClinicQueueGetDisplaySnapshotHandlerTest.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/ClinicQueueGetDisplaySnapshotHandlerTest.cs)
  - [AdmissionQueueOperationalProjection.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AdmissionQueueOperationalProjection.cs)
  - [RegJalanWalkInCommand.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs)
  - [BookingCreateCmd.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/BookingFeature/UseCases/BookingCreateCmd.cs)
- `c013-kiosk-queue-display-web`:
  - [clinic.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/packages/shared-types/src/clinic.ts)
  - [index.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/packages/shared-types/src/index.ts)
  - [useKioskSelfPrint.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/apps/kiosk-web/src/composables/useKioskSelfPrint.ts)
  - [registrationReceipt.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/apps/kiosk-web/src/lib/registrationReceipt.ts)
  - [ClinicDisplayLayout.vue](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/apps/display-web/src/components/ClinicDisplayLayout.vue)
  - [clinicAnnouncement.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/apps/display-web/src/lib/clinicAnnouncement.ts)
- `c012_myhospital_web`:
  - [registrasi.ts](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web/src/modules/Admisi/types/registrasi.ts)
  - [antrian.ts](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web/src/modules/Outpatient/types/antrian.ts)
  - [TindakanWorkbenchColumn.vue](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web/src/modules/Outpatient/components/TindakanWorkbenchColumn.vue)
  - [PatientInfoBanner.vue](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web/src/modules/Outpatient/components/PatientInfoBanner.vue)
