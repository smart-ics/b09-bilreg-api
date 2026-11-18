---
Title: Architecture - Penerapan Prefix Ruangan pada Sesi Antrian Poli/Dokter di Bilreg, Display Antrian, dan Worklist Rawat Jalan
Code: ANTRIAN-POLI-PREFIX
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-12
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis penerapan kode prefix ruangan (`HiDok_Ruang.prefixAntrian`) pada sesi antrian pemeriksaan dokter/poliklinik di database Bilreg (`b09-bilreg-api`), kontrak API pendaftaran dan antrian, pencetakan struk dan pemanggilan suara pada anjungan mandiri serta layar monitor (`c013-kiosk-queue-display-web`), dan tampilan antrian pada modul pendaftaran serta daftar periksa rawat jalan (`c012_myhospital_web`).

Realisasi ini mengubah sesi antrian dokter yang sebelumnya selalu memiliki snapshot prefix kosong (`""`) agar mewarisi kode prefix dari ruangan jadwal praktek dokter secara otomatis. Format nomor antrian poli distandarkan menjadi format ringkas tanpa angka nol di depan (contoh: `A3`, `OB12`) tanpa mengubah format 4 digit pada loket admisi (`A0001`). Seluruh perubahan kontrak data bersifat aditif guna menjamin tidak adanya gangguan (*zero breaking change*) pada pemanggil antarmuka yang sudah ada.

Referenced Artifacts:
- ISSUE: [ANTRIAN-POLI-PREFIX-ISSUE.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-ISSUE.md) (`ISSUE-BILREG-ANTRIAN-POLI-PREFIX-001`)
- FEASIBILITY-ASSESSMENT: [ANTRIAN-POLI-PREFIX-FEASIBILITY-ASSESSMENT.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)

---

# 2. Architectural Basis

## Business Context

- DOMAIN: [admisi-rajal-domain.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)
- DOMAIN: [TRACKER-DOMAIN.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/pasien-tracker/TRACKER-DOMAIN.md)

Dalam domain pendaftaran Rawat Jalan, jadwal praktek dokter beroperasi pada ruangan poliklinik tertentu (`HiDok_Ruang`). Pasien yang mendaftar (baik secara langsung/walk-in maupun melalui reservasi/booking) memerlukan tiket fisik, nomor antrian di layar tunggu monitor, serta lafal pemanggilan suara yang konsisten dengan nama ruangan poliklinik tujuan. Konteks Admisi Rajal memiliki data jadwal dokter dan ruangan, sedangkan konteks Patient Tracker mengelola sesi antrian fisik (`AntrianModel`) dan entri nomor urut (`NoUrut`).

## Analysis Input

Arsitektur ini merealisasikan keputusan penutupan kesenjangan (*Gap Closure*) yang telah disetujui pada `FEASIBILITY-ASSESSMENT`:

- **GAP-001 & OQ-003**: Perluasan kapasitas kolom `BILRG_Antrian.QueuePrefixSnapshot` menjadi `VARCHAR(5)` dan normalisasi prefix poli hingga 2 huruf kapital `[A-Z]{1,2}`, dengan pemetaan nilai kosong atau sentinel `"-"` menjadi string kosong (`""`).
- **GAP-002 & OQ-002**: Pewarisan nilai `PrefixAntrian` dari entitas ruang jadwal ke `AntrianModel` melalui `AntrianFactory` pada 4 alur registrasi/booking, dengan dukungan pembaruan susulan (*backfill*) satu kali apabila sesi sebelumnya kosong, serta penguncian permanen (*immutability*) setelah terisi.
- **GAP-003, GAP-007 & OQ-001**: Pemisahan pemformat label antrian poli menjadi format ringkas tanpa leading zero (`${prefix}${noUrut}`, contoh: `"A3"`, `"OB12"`), sementara antrian Loket Admisi tetap mempertahankan format 4 digit (`A0001`).
- **GAP-004 & OQ-004**: Kontrak API aditif pada `b09-bilreg-api`: mempertahankan `noAntrian: int` dan menambahkan properti string aditif (`queueLabel`, `prefixAntrian`) pada DTO registrasi, booking, dan detail antrian.
- **GAP-005**: Penyesuaian skema Zod dan pencetakan struk `kiosk-web` serta verifikasi pemutaran suara `display-web` di `c013-kiosk-queue-display-web`.
- **GAP-006**: Pembaruan komponen UI modul `Admisi` dan `Outpatient` di `c012_myhospital_web` untuk menampilkan label antrian berprefix hasil gabungan server.

```text
DOMAIN (admisi-rajal & pasien-tracker)
        +
FEASIBILITY-ASSESSMENT (ANTRIAN-POLI-PREFIX-FEASIBILITY-ASSESSMENT)
        ↓
    ARCHITECTURE (ANTRIAN-POLI-PREFIX-ARCHITECTURE)
```

---

# 3. Scope

## Included

1. **Skema Database (`b09-bilreg-api`)**:
   - Perubahan definisi kolom `BILRG_Antrian.QueuePrefixSnapshot` dari `CHAR(1)` menjadi `VARCHAR(5)`.
   - Skrip migrasi DDL SQL Server idempotent untuk memperbarui tabel operasional yang sudah ada.
2. **Domain Layer (`b09-bilreg-api`)**:
   - Pembuatan normalizer prefix poli di `AntrianModel` yang mendukung 0..2 huruf kapital `A-Z` dan memetakan sentinel `"-"` menjadi `""`.
   - Penyediaan method pemformatan label ringkas poli `FormatClinicQueueLabel(int noUrut)`.
   - Pembaruan `AntrianFactory.Create` untuk jadwal praktek (`JadwalPraktekEffective` dan `JadwalPraktekType`) agar meneruskan `Ruang.PrefixAntrian`.
   - Dukungan method domain pembaruan prefix bersyarat pada `AntrianModel` (`TryUpdatePrefix`) jika snapshot masih kosong.
3. **Data Access Layer (`b09-bilreg-api`)**:
   - Penambahan method `UpdatePrefixIfEmpty` pada `IAntrianDal` dan implementasi SQL Dapper `AntrianDal`.
4. **Application & Use Case Layer (`b09-bilreg-api`)**:
   - Alur walk-in: `RegJalanWalkInCommand` meneruskan prefix ruang ke `AntrianFactory` dan mengembalikan response berlabel.
   - Alur ubah kunjungan: `RegJalanUbahKunjunganCmd` meneruskan prefix ruang ke antrian tujuan.
   - Alur booking: `BookingCreateCmd` dan `BookingCreateFromHidokCommand` meneruskan prefix ruang ke `AntrianFactory`.
   - Alur display: `ClinicQueueDisplayQueries` memformat nomor menggunakan format ringkas poli (`A3`).
   - Alur query antrian: `QueGetAntrianQuery` menyertakan `QueueLabel` dan `PrefixAntrian` pada `QueGetAntrianResponse`.
5. **API & Contract Layer (`b09-bilreg-api`)**:
   - Penambahan properti string aditif `QueueLabel` dan `PrefixAntrian` pada `RegJalanCreateResponse`, `BookingCreateResponse`, dan `QueGetAntrianResponse`.
6. **Kiosk & Display Frontend (`c013-kiosk-queue-display-web`)**:
   - Pembaruan skema `returnCreateWalkInSchema` di `packages/shared-types` dengan properti aditif opsional `queueLabel` dan `prefixAntrian`.
   - Pembaruan cetak struk registrasi mandiri di `kiosk-web` (`useKioskSelfPrint.ts`, `registrationReceipt.ts`) untuk menampilkan label berprefix.
   - Verifikasi pemutaran suara di `display-web` (`clinicAnnouncement.ts`, `ClinicDisplayLayout.vue`) untuk string `A3` dan `OB12`.
7. **Web Client Frontend (`c012_myhospital_web`)**:
   - Pembaruan skema Zod DTO pada modul `Admisi` (`registrasi.ts`, `booking.ts`) dan `Outpatient` (`antrian.ts`).
   - Tampilan nomor antrian pada bukti registrasi Admisi dan daftar tunggu/banner pasien Outpatient (`TindakanWorkbenchColumn.vue`, `PatientInfoBanner.vue`).
8. **Pengujian Unit**:
   - Penyesuaian ekspektasi unit test `ClinicQueueGetDisplaySnapshotHandlerTest` dari `"A0001"` menjadi `"A1"`.
   - Unit test domain `AntrianModel` untuk validasi prefix 0..2 karakter dan pemformatan ringkas.
   - Unit test handler `RegJalanWalkInCommand` dan `BookingCreateCmd` untuk memastikan prefix tersimpan dan label terisi.

## Excluded

1. Perubahan alur Pendaftaran Rawat Darurat (IGD): IGD tidak menggunakan prefix antrian dan tetap mengirim string kosong.
2. Perubahan alur Antrian Loket Admisi: Loket admisi tetap menggunakan format 4 digit (`A0001`) dan validator 1 karakter.
3. Modifikasi relasi tabel master `HiDok_Ruang`: Kolom `prefixAntrian` di tabel master sudah ada dan tidak diubah strukturnya.
4. Perubahan wire contract event bus MassTransit / NuGet package `MyHospital.MsgContract`.
5. Perubahan destruktif pada tipe kolom fisik `NoUrut` di database maupun tipe properti `noAntrian: int` pada seluruh kontrak API.

---

# 4. Technical Decisions

### TD-01: Skema Database & Relaksasi Panjang Kolom Snapshot
Kolom `BILRG_Antrian.QueuePrefixSnapshot` diperluas dari `CHAR(1)` menjadi `VARCHAR(5)` dengan nilai default string kosong:
```sql
ALTER TABLE BILRG_Antrian ALTER COLUMN QueuePrefixSnapshot VARCHAR(5) NOT NULL;
```
Panjang 5 karakter memberikan toleransi yang aman untuk prefix 1–2 karakter (misal: `"A"`, `"OB"`) dan menghindari pemotongan data (*string truncation*), sekaligus tetap efisien dalam penyimpanan baris.

### TD-02: Normalisasi Prefix Ruangan Poli pada Lapisan Domain
Aturan validasi prefix pada `AdmissionServicePointModel.NormalizePrefix` (yang mewajibkan tepat 1 huruf `A-Z`) tetap dipertahankan khusus untuk Loket Admisi. Untuk Antrian Poli/Dokter, dibuat method normalisasi baru pada `AntrianModel`:
```csharp
public static string NormalizeClinicPrefix(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw) || raw.Trim() == "-")
        return string.Empty;

    var normalized = raw.Trim().ToUpperInvariant();
    if (normalized.Length > 2 || !normalized.All(char.IsAsciiLetterUpper))
        throw new ArgumentException(
            $"Clinic queue prefix must be 1 to 2 uppercase ASCII letters (A-Z), got '{raw}'.",
            nameof(raw));

    return normalized;
}
```
Aturan ini secara deterministik memetakan `null`, whitespace, atau tanda minus `"-"` menjadi string kosong (`""`), serta memastikan hanya karakter alfabet murni `[A-Z]` yang tersimpan sebagai snapshot.

### TD-03: Pewarisan Prefix Ruang pada Factory Sesi Antrian
Method `AntrianFactory.Create` untuk jadwal dokter diekstrak langsung dari properti `Ruang.PrefixAntrian` pada objek jadwal:
```csharp
public AntrianModel Create(DateOnly antrianDate, JadwalPraktekEffective effective)
{
    // ...
    var prefix = effective.Ruang?.PrefixAntrian ?? string.Empty;
    return new AntrianModel(newId, antrianDate, effective.JamMulai,
        effective.JamSelesai, sequenceTag, antrianDesc, servicePoint,
        new List<AntrianEntryModel>(), _antrianSequencer, queuePrefixSnapshot: prefix);
}

public AntrianModel Create(DateOnly antrianDate, JadwalPraktekType jadwalPraktek)
{
    // ...
    var prefix = jadwalPraktek.Ruang?.PrefixAntrian ?? string.Empty;
    return new AntrianModel(newId, antrianDate, jadwalPraktek.JamMulai,
        jadwalPraktek.JamSelesai, sequenceTag, antrianDesc, servicePoint,
        new List<AntrianEntryModel>(), _antrianSequencer, queuePrefixSnapshot: prefix);
}
```
Kedua overload tersebut sudah memiliki akses ke entitas `RuangType` yang membawa `PrefixAntrian` dari master `HiDok_Ruang`, sehingga tidak memerlukan query tambahan ke database (*zero-query addition*).

### TD-04: Aturan Immutability Bersyarat (*Conditional Backfill*)
Jika sesi antrian dokter telah terbentuk saat jadwal dokter belum memiliki ruangan (`RuangId = "-"` atau `prefixAntrian = ""`), snapshot awal bernilai kosong (`""`). Ketika ruangan kemudian ditetapkan dan pasien berikutnya mendaftar/booking, sistem diperbolehkan mengisi snapshot yang kosong tersebut satu kali. Namun, jika snapshot sudah memiliki nilai terisi, nilainya tidak boleh ditimpa kembali.

Realisasi SQL pada `AntrianDal.UpdatePrefixIfEmpty`:
```sql
UPDATE BILRG_Antrian
SET QueuePrefixSnapshot = @QueuePrefixSnapshot
WHERE AntrianId = @AntrianId
  AND (QueuePrefixSnapshot IS NULL OR QueuePrefixSnapshot = '');
```
Realisasi ini menjamin integritas tiket fisik: tiket ber-prefix yang sudah tercetak tidak akan pernah berubah prefix-nya di tengah hari meskipun dokter bertukar ruangan.

### TD-05: Strategi Pemformatan Label Antrian Poli (Format Ringkas)
Pada `AntrianModel`, pemformatan label antrian poli dilakukan secara ringkas tanpa angka nol di depan:
```csharp
public string FormatClinicQueueLabel(int noUrut)
{
    return string.IsNullOrEmpty(QueuePrefixSnapshot)
        ? noUrut.ToString()
        : $"{QueuePrefixSnapshot}{noUrut}";
}
```
Sedangkan method `FormatQueueLabel(int noUrut)` tetap mempertahankan format 4 digit (`$"{QueuePrefixSnapshot}{noUrut:D4}"`) khusus untuk Loket Admisi.

Pada `ClinicQueueDisplayQueries`:
```csharp
var nomorAntrean = queue?.FormatClinicQueueLabel(call.NoUrut) ?? call.NoUrut.ToString();
```
Perubahan ini menyelaraskan tampilan monitor poli dengan ucapan suara anjungan tanpa leading zero (`A1`, `A2`, `A3`).

### TD-06: Evolusi Kontrak API Bersifat Aditif (*Zero Breaking Change*)
Semua kontrak respons DTO di `b09-bilreg-api` tetap mempertahankan properti `NoAntrian` / `noAntrian` bertipe integer, dan menambahkan properti string aditif dengan nilai default:
1. `RegJalanCreateResponse`:
   ```csharp
   public record RegJalanCreateResponse(
       string RegId,
       int NoAntrian,
       string QueueLabel = "",
       string PrefixAntrian = "");
   ```
2. `BookingCreateResponse`:
   ```csharp
   public record BookingCreateResponse(
       string BookingId,
       int NoAntrian,
       bool IsDuplicated,
       bool IsCreated,
       string PasienTrackerId,
       IEnumerable<TrkJourneyCandidateDto> Candidates,
       string QueueLabel = "",
       string PrefixAntrian = "");
   ```
3. `QueGetAntrianResponse`:
   ```csharp
   public record QueGetAntrianResponse(
       string AntrianId,
       int NoAntrian,
       string PasienTrackerId,
       RegReff Reg,
       PasienReff Pasien,
       string Umur,
       TipeJaminanReff TipeJaminan,
       int StatusAntrian,
       string StatusAntrianString,
       DateTime? ServedAt,
       DateTime? DoneAt,
       string QueueLabel = "",
       string PrefixAntrian = "");
   ```

### TD-07: Strategi Konsumsi di Frontend (`c012` & `c013`)
Di sisi client:
- Komponen membaca nilai berurutan: `queueLabel || (prefixAntrian ? `${prefixAntrian}${noAntrian}` : noAntrian.toString())`.
- Skema Zod pendaftaran dan antrian menambahkan properti opsional `.optional()` sehingga tetap valid saat menerima payload dari server versi lama maupun versi baru:
  ```typescript
  queueLabel: z.string().optional().default(''),
  prefixAntrian: z.string().optional().default(''),
  ```
- Parser suara `parseQueueLabel` pada `c013-kiosk-queue-display-web` menggunakan regex `^([A-Za-z]+)(\d+)$`, yang secara otomatis memisahkan `"A3"` menjadi prefix `"A"` dan angka `"3"`, sehingga memutar audio `letters/A.wav` diikuti `numbers/3.wav`.

---

# 5. Component Responsibilities

| Komponen | Repositori | Tanggung Jawab |
|---|---|---|
| `BILRG_Antrian.sql` & Skrip Migrasi | `b09-bilreg-api` | Menampung snapshot prefix ruangan hingga 5 karakter (`VARCHAR(5)`). |
| `AntrianModel` | `b09-bilreg-api` | Memvalidasi prefix poli (0..2 huruf kapital A-Z), menyimpan snapshot, memformat label ringkas poli, dan mengontrol pengisian bersyarat. |
| `AntrianFactory` | `b09-bilreg-api` | Mengekstrak `Ruang.PrefixAntrian` dari entitas jadwal dokter dan meneruskannya saat instansiasi `AntrianModel`. |
| `AntrianDal` | `b09-bilreg-api` | Menyimpan `QueuePrefixSnapshot` saat INSERT dan mengeksekusi update bersyarat saat backfill snapshot kosong. |
| `RegJalanWalkInCommand` / `Handler` | `b09-bilreg-api` | Menginstansiasi sesi antrian dengan prefix ruang jadwal dan mengembalikan response registrasi berlabel. |
| `RegJalanUbahKunjunganCmd` / `Handler` | `b09-bilreg-api` | Meneruskan prefix ruang ke sesi antrian tujuan saat kunjungan poli diubah. |
| `BookingCreateCmd` / `BookingCreateFromHidokCommand` | `b09-bilreg-api` | Menginstansiasi sesi antrian booking dengan prefix ruang jadwal dan mengembalikan response booking berlabel. |
| `ClinicQueueDisplayQueries` | `b09-bilreg-api` | Memformat nomor panggilan dan nomor tunggu pada layar display klinik menggunakan format ringkas poli. |
| `QueGetAntrianQuery` / `Handler` | `b09-bilreg-api` | Menyertakan `QueueLabel` dan `PrefixAntrian` pada response daftar antrian per sesi. |
| `packages/shared-types` | `c013-kiosk-queue-display-web` | Menyediakan tipe data DTO dan skema Zod dengan properti aditif `queueLabel` dan `prefixAntrian`. |
| `apps/kiosk-web` (`useKioskSelfPrint`, `registrationReceipt`) | `c013-kiosk-queue-display-web` | Merender struk tiket registrasi mandiri dengan nomor antrian berprefix ringkas (`A3`). |
| `apps/display-web` (`ClinicDisplayLayout`, `clinicAnnouncement`) | `c013-kiosk-queue-display-web` | Menampilkan label antrian pada monitor poli dan memvalidasi kelancaran pemanggilan suara terpisah. |
| `src/modules/Admisi` | `c012_myhospital_web` | Menampilkan label antrian berprefix pada hasil registrasi dan bukti pendaftaran rawat jalan. |
| `src/modules/Outpatient` | `c012_myhospital_web` | Menampilkan label antrian berprefix pada daftar periksa workbench dan banner pasien poliklinik. |

---

# 6. Integration Design

| Source | Target | Purpose | Protokol / Kontrak |
|---|---|---|---|
| `RegJalanWalkInCommand` | `AntrianFactory` | Meneruskan prefix ruangan ke pembuatan sesi antrian | In-process C# call via `effective.Ruang.PrefixAntrian` |
| `BookingCreateCmd` | `AntrianFactory` | Meneruskan prefix ruangan jadwal reservasi ke sesi antrian | In-process C# call via `jadwal.Ruang.PrefixAntrian` |
| `AntrianDal` | SQL Server `BILRG_Antrian` | Persistensi dan pembaruan bersyarat snapshot prefix | Dapper SQL: `INSERT` & `UPDATE ... WHERE QueuePrefixSnapshot = ''` |
| `RegController` | Client (`c012` & `c013`) | Mengembalikan hasil pendaftaran dengan nomor dan label antrian | REST JSON: `RegJalanCreateResponse` (`noAntrian`, `queueLabel`, `prefixAntrian`) |
| `BookingController` | Client (`c012` & `c013`) | Mengembalikan hasil booking dengan nomor dan label antrian | REST JSON: `BookingCreateResponse` (`noAntrian`, `queueLabel`, `prefixAntrian`) |
| `AntrianController` (`GET /api/Antrian/{id}`) | Client (`c012`) | Mengembalikan daftar entri antrian per sesi beserta label siap tampil | REST JSON: `QueGetAntrianResponse` (`noAntrian`, `queueLabel`, `prefixAntrian`) |
| `kiosk-web` | Printer Thermal Kiosk | Mencetak nomor antrian berprefix pada struk pendaftaran mandiri | HTML print template / `window.print()` |
| `display-web` | Audio Hardware | Memutar panggilan suara antrian berprefix | HTML5 Web Audio / WAV sequencing (`letters/*.wav` + `numbers/*.wav`) |

---

# 7. Data Ownership

| Data | Pemilik Tunggal (*Single Owner*) | Keterangan |
|---|---|---|
| Master Prefix Ruangan (`HiDok_Ruang.prefixAntrian`) | Bounded Context Admisi Rajal (`b09-bilreg-api`) | Dikelola melalui modul master ruangan. Hanya huruf kapital 1–2 karakter atau kosong. |
| Sesi Antrian & Snapshot Prefix (`BILRG_Antrian.QueuePrefixSnapshot`) | Bounded Context Patient Tracker (`b09-bilreg-api`) | Snapshot disalin saat sesi dibuat. Bersifat tulis-sekali (*write-once*) atau backfill satu kali saat ruangan baru ditentukan. |
| Nomor Urut Antrian (`BILRG_AntrianEntry.NoUrut`) | Bounded Context Patient Tracker (`b09-bilreg-api`) | Bernilai integer berurutan (`1..N`). Tidak boleh memuat huruf atau karakter formatting. |
| Format Tampilan Label Antrian Poli (`QueueLabel`) | Presentation / Response DTO Layer | Digabung saat query/respons dibentuk (`FormatClinicQueueLabel`). Klien mengonsumsi string hasil gabungan ini. |

---

# 8. Database Design

## New Tables

*Tidak ada tabel baru yang dibuat.*

## Modified Tables

| Table | Perubahan | Alasan |
|---|---|---|
| `BILRG_Antrian` | `ALTER COLUMN QueuePrefixSnapshot VARCHAR(5) NOT NULL DEFAULT('')` | Memperluas kolom dari `CHAR(1)` agar mampu menampung prefix ruang 1–2 huruf (seperti `"A"`, `"OB"`). |

## Relationships

*Tidak ada perubahan relasi kunci asing (foreign key). Relasi antara `BILRG_Antrian` dan `BILRG_AntrianEntry` tetap menggunakan `AntrianId`.*

## Migration Considerations

1. **Skrip Migrasi DDL Idempotent**:
   ```sql
   IF EXISTS (
       SELECT 1 FROM sys.columns 
       WHERE object_id = OBJECT_ID('BILRG_Antrian') 
         AND name = 'QueuePrefixSnapshot' 
         AND max_length = 1
   )
   BEGIN
       ALTER TABLE BILRG_Antrian ALTER COLUMN QueuePrefixSnapshot VARCHAR(5) NOT NULL;
   END
   ```
2. **Kompatibilitas Data Existing**:
   - Data antrian hari-hari lampau yang tersimpan `CHAR(1)` (misal `' '` atau `'A'`) otomatis terkonversi secara aman tanpa kehilangan data.
   - Sesi dokter masa lampau yang tersimpan string kosong `""` tetap terbaca dan beroperasi normal dengan format numerik polos.

---

# 9. Cross-Cutting Concerns

### Concurrency & Immutability
- Snapshot prefix ditulis saat pembuatan sesi antrian dokter.
- Jika terjadi kondisi balapan (*race condition*) saat dokter ditetapkan ruangannya, method `UpdatePrefixIfEmpty` menggunakan predikat atomik SQL `WHERE (QueuePrefixSnapshot IS NULL OR QueuePrefixSnapshot = '')` sehingga tidak ada risiko penimpaan snapshot yang sudah valid.

### Backward Compatibility (Zero Breaking Change)
- Seluruh endpoint API tetap menyertakan `noAntrian: int`. Klien pihak ketiga atau endpoint integrasi lama yang hanya membaca `noAntrian` tidak mengalami kegagalan parsing.
- Penambahan properti string `queueLabel` dan `prefixAntrian` menggunakan parameter opsional/default pada record C# sehingga binary calling di level application code tetap kompatibel.

### Audio & Visual Accessibility
- Layar display poliklinik menampilkan nomor secara ringkas (`A3`) dengan kontras tinggi sesuai standar keterbacaan antrian rumah sakit.
- Mesin suara di `c013-kiosk-queue-display-web` memanggil huruf satu per satu (misal: "O", "B", "satu", "dua" untuk `OB12`) yang sudah didukung secara alami oleh fungsi `parseQueueLabel`.

---

# 10. Implementation Constraints

1. **Pragmatic Tactical DDD**: Seluruh aturan validasi prefix harus berada di domain entity `AntrianModel` dan factory `AntrianFactory`, bukan tercecer di controller atau UI.
2. **Dapper & SQL Server**: Akses data ke tabel `BILRG_Antrian` harus menggunakan query SQL eksplisit melalui Dapper di `AntrianDal`, tanpa menggunakan ORM berat.
3. **Pemisahan Konteks Format**: Dilarang menggunakan formatter 4 digit (`A0001`) pada antrian poli, dan dilarang mengubah formatter antrian loket admisi menjadi format ringkas. Keduanya harus terisolasi.
4. **Vue 3 Composition API**: Seluruh perubahan UI di `c012_myhospital_web` dan `c013-kiosk-queue-display-web` harus menggunakan `<script setup lang="ts">`, mematuhi aturan import yang ada, dan tidak mengubah alur state pinia yang sudah stabil.
5. **No Redundant Test Executions**: Verifikasi unit test hanya dijalankan secara terarah pada file pengujian yang relevan (`ClinicQueueGetDisplaySnapshotHandlerTest`, `AntrianModelTest`, dll.) dan tidak menjalankan seluruh suite secara masif.

---

# 11. Acceptance Conditions

1. `BILRG_Antrian.QueuePrefixSnapshot` mampu menampung string hingga 2 karakter `A-Z`.
2. Saat pendaftaran pasien Rawat Jalan Walk-In atau By Booking pada jadwal yang memiliki ruang ber-prefix (misal `"A"` atau `"OB"`), sesi antrian di `BILRG_Antrian` tersimpan dengan `QueuePrefixSnapshot` yang sesuai.
3. Respons endpoint `POST api/Reg/rajalWalkIn`, `POST api/Reg/rajalByBooking`, `POST api/Booking`, dan `GET api/Antrian/{id}` mengembalikan properti `noAntrian` bertipe integer dan `queueLabel` bertipe string berprefix ringkas (contoh: `"A3"`).
4. Layar monitor `display-web` menampilkan label ringkas `"A3"` dan mesin suara memutar lafal `"A"` lalu `"3"`.
5. Cetakan struk anjungan mandiri `kiosk-web` mencetak nomor antrian berprefix ringkas (`"A3"`).
6. Modul `Admisi` dan `Outpatient` pada `c012_myhospital_web` menampilkan nomor antrian berprefix pada bukti registrasi, workbench pemeriksaan dokter, dan banner pasien.
7. Alur Loket Admisi tetap beroperasi dengan format 4 digit (`A0001`) tanpa terpengaruh oleh format antrian poli.
8. Alur IGD tetap beroperasi tanpa prefix antrian.
