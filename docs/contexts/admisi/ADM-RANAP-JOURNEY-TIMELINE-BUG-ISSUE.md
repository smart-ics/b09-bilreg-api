# ISSUE

## Metadata

ID: ISS-ADM-REG-008
Type: BUG
Status: Open
Title: Nilai Timestamp occurredAt Selalu Identik pada Peristiwa Timeline Perjalanan Pasien Rawat Inap (Admisi Ranap Journey)

## Source

Reported By: ica-issuer (Review Temuan & Analisis Data Journey D:\fix_timeline_journey.md)
Reported Date: 2026-10-19
Severity: Major

## Description

Pada endpoint detail perjalanan pasien rawat inap (`GET /api/admisi-ranap/journeys/{journeyId}`), nilai timestamp `occurredAt` pada elemen array `timeline[]` bernilai identik/sama untuk peristiwa-peristiwa yang terjadi pada siklus hidup entitas yang sama, meskipun pada basis data waktu penciptaan (*creation date / CrtDate*) dan waktu perubahan status (*update/realization date / UpdDate*) memiliki selisih waktu yang nyata.

Contoh kasus teramati pada reservasi rawat inap:
- Peristiwa `kind 3 (Reservasi dibuat)`
- Peristiwa `kind 6 (Reservasi direalisasikan)`
- Peristiwa `kind 7 (Admisi didaftarkan)`

Ketiga peristiwa tersebut menghasilkan nilai `occurredAt` yang sama persis (misal `2026-02-11T09:18:51.38`), padahal pada basis data tercatat pembuatan reservasi terjadi ~2 menit lebih awal (`2026-02-11T09:16:57.76`).

Hal ini menyebabkan visualisasi kronologis pada antarmuka pengguna (`JourneyTimeline.vue`) menampilkan stempel waktu yang seolah-olah terjadi secara simultan/bersamaan pada detik yang sama, mengurangi keandalan audit trail dan nilai guna riwayat kronologis bagi pengguna operasional admisi.

## Desired Outcome

1. Setiap peristiwa (*timeline event*) dalam riwayat perjalanan pasien rawat inap merefleksikan stempel waktu keterjadian (*occurredAt*) yang sebenarnya sesuai titik peristiwa bisnis:
   - Peristiwa pembentukan (pembuatan Opname Request, pembuatan Reservasi) menggunakan stempel waktu saat entitas dibuat (*creation time*).
   - Peristiwa perubahan status (pemenuhan/pembatalan Opname Request, pemeliharaan/realisasi/pembatalan Reservasi) menggunakan stempel waktu saat perubahan status terjadi (*status update time*).
   - Peristiwa pendaftaran admisi dan perubahannya menggunakan waktu pendaftaran (*admission date*) dan waktu pembaruan (*update time*) yang bersesuaian.
2. Frontend (`c012_myhospital_web`) dapat menampilkan urutan kronologis perjalanan pasien rawat inap dengan stempel waktu yang akurat, informatif, dan tidak redundan.

## Current Situation

1. **Backend (`b09-bilreg-api`)**:
   - Kontrak Fact agregasi (`OpnameRequestJourneyFact`, `ReservationJourneyFact`, `AdmissionJourneyFact`) pada `JourneyContracts.cs` hanya menyimpan satu stempel waktu status (`StatusAt` / `AdmissionAt`).
   - Lapisan DAL (`JourneyDal.cs` dan `JourneyDal.Batch.cs`) hanya mengambil satu nilai waktu (`UpdDate == VoidSentinel ? CrtDate : UpdDate`), sehingga nilai `CrtDate` terbuang saat data pernah diperbarui.
   - Resolver (`Release1JourneyStageResolver.cs:BuildTimeline`) memakai ulang atribut timestamp tunggal tersebut untuk membangun event pembuatan maupun event pembaruan status.
2. **Frontend (`c012_myhospital_web`)**:
   - Komponen `JourneyTimeline.vue` me-render langsung array `timeline` dari respons API tanpa modifikasi waktu. Tampilan riwayat di antarmuka menampilkan waktu yang persis sama pada beberapa titik tahapan rawat inap.

## Evidence

- **Dokumen Temuan & Analisis Sumber**: `D:\fix_timeline_journey.md`
- **Contoh Rekaman Data Basis Data**:
  - Data Journey ID: `rsv:RSV097XD67QY` (terhubung ke Admisi `RGA6R7FJSX` dan Waiting List `WTL097XMC9XY`)
  - Tabel `BILRG_AdmReservation`: `CrtDate = 2026-02-11 09:16:57.760`, `UpdDate = 2026-02-11 09:18:51.380`, `Status = 2 (Realized)`
  - Tabel `BILRG_AdmAdmission`: `AdmissionDate = 2026-02-11 09:18:51.380`, `CrtDate = 2026-02-11 09:18:51.380`, `UpdDate = 3000-01-01`, `Status = 0 (Admitted)`
  - Respons API `GET /api/admisi-ranap/journeys/rsv:RSV097XD67QY`:
    - `kind 3 (Reservasi dibuat)`: `occurredAt = 2026-02-11T09:18:51.38` (Seharusnya: `09:16:57.76`)
    - `kind 6 (Reservasi direalisasikan)`: `occurredAt = 2026-02-11T09:18:51.38`
    - `kind 7 (Admisi didaftarkan)`: `occurredAt = 2026-02-11T09:18:51.38`
- **Kode Program Terkait**:
  - Kontrak: `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/JourneyContracts.cs`
  - Akses Data: `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs` & `JourneyDal.Batch.cs`
  - Logika Timeline: `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolver.cs`
  - Komponen Presentasi: `c012_myhospital_web/src/modules/Admisi/components/ranap/journey/JourneyTimeline.vue`

## Notes

- Klasifikasi masalah: **BUG**.
- Perbaikan diharapkan bersifat *backward-compatible* terhadap struktur kontrak wire JSON (`kind`, `occurredAt`, `relatedRecordId`, `summary`), hanya mengoreksi akurasi nilai `occurredAt`.
- Dokumen rencana kerja awal telah dipersiapkan pada `D:\fix_timeline_journey.md` mencakup tahapan penambahan field fakta, pengisian DAL, pemetaan resolver, dan pengujian unit/integrasi.
