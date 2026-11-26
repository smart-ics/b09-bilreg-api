# ISSUE

## Metadata

ID: ISSUE-RAD-JADWAL-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Implementasi Outcome OC-RAD-JADWAL: Penjadwalan Pemeriksaan Radiologi (JadwalRadiologi)

## Source

Reported By: User
Reported Date: 2026-11-25

## Description

Permintaan implementasi dan penyelarasan kapabilitas penjadwalan pemeriksaan radiologi (*radiology examination scheduling*) sebagai fakta bisnis persisten (*persisted business fact*) ke dalam codebase core feature (`RadContext`) berdasarkan spesifikasi kanonikal [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md).

Pemeriksaan radiologi—khususnya modalitas diagnostik canggih berkapasitas terbatas dengan durasi pemindaian terencana (seperti CT-Scan, MRI, Fluoroskopi, USG khusus, dan tindakan biopsi/intervensi)—mengharuskan kehadiran fisik pasien (*physical presence*) pada alat diagnostik dan ruangan pemeriksaan tertentu. Kapabilitas ini dibutuhkan rumah sakit untuk:
1. Merencanakan, memesan, dan mengalokasikan slot waktu penggunaan mesin radiologi dan ruang periksa fisik secara terstruktur guna mencegah tumpang-tindih (*resource contention/double-booking*).
2. Mengikat jadwal secara ketat pada permintaan order radiologi yang sah (`RAD-ORDER` / `OC-RAD-ORDER-RAD`).
3. Mengalokasikan mesin fisik, ruangan periksa (`ORG-LAYANAN`), serta dokter spesialis radiologi (Sp.Rad) penanggung jawab (`ORG-PPA`) jika tindakan memerlukan operator medis langsung.
4. Merekam instruksi persiapan klinis pasien (seperti puasa, hidrasi, atau prasyarat kreatinin lab) dan checklist skrining keselamatan pra-tindakan (*safety clearance*).
5. Mengelola siklus hidup jadwal (penjadwalan baru, pemindahan jadwal/reschedule non-destruktif berjejak audit, pembatalan independen, dan penyelesaian kesiapan pra-pemindaian).
6. Menjadi acuan pemanggilan pasien serta gerbang transisi menuju pelaksanaan pemindaian fisik di mesin (`RAD-EXAM` / `OC-RAD-EXAM`).
7. Mempublikasikan pembaruan status jadwal ke modul pelacakan perjalanan pasien rumah sakit (`ADM-TRACKER`).

## Desired Outcome

Sesuai spesifikasi bisnis pada [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md):

1. **Eksistensi Catatan Jadwal Unik**:
   - Terbentuk catatan jadwal radiologi unik dengan nomor identifikasi persisten (`JadwalRadId`) dan nomor transaksi operasional (`ScheduleNo`).

2. **Keterikatan Wajib pada Order Sah (Order-Driven)**:
   - Jadwal terikat secara sah pada tepat satu `OrderRadiologi` (`RAD-ORDER`) berstatus aktif (`Requested / Ordered`).
   - Seluruh item pemeriksaan dalam satu order dijadwalkan dalam satu sesi slot berkesinambungan dengan durasi total yang diakumulasikan.

3. **Alokasi Sumber Daya Fisik & Ruangan Definitif**:
   - Teralokasikan tepat satu mesin/modalitas fisik aktif dan ruangan radiologi (`ORG-LAYANAN`) sesuai modalitas order, serta dokter spesialis radiologi (Sp.Rad) penanggung jawab jika prosedur membutuhkan operator dokter langsung (`ORG-PPA`).

4. **Alokasi Rentang Waktu Bebas Konflik (No-Overlap Invariant)**:
   - Teralokasikan tanggal periksa, waktu mulai (*StartTime*), dan waktu selesai (*EndTime*) berbasis durasi total tindakan ditambah buffer desinfeksi/persiapan alat (*Turnaround Buffer*).
   - Sistem secara tegas menolak penyimpanan reservasi jika terjadi tabrakan waktu (*double-booking*) pada mesin fisik yang sama.

5. **Panduan Persiapan Pasien & Skrining Keselamatan**:
   - Merekam panduan persiapan pasien (puasa, obat, hasil lab kreatinin/eGFR) dan checklist skrining keselamatan (implan feromagnetik, kehamilan, riwayat alergi kontras).
   - Menyediakan penanda kesiapan klinis pra-pemeriksaan (`IsArrived`, `IsCleared`, dan `ClearanceNotes`).

6. **Pengendalian Siklus Hidup Jadwal**:
   - Status awal jadwal tercatat sebagai `Scheduled`.
   - **Reschedule**: Mendukung perubahan waktu/mesin secara *in-place*, melepaskan slot lama, menaikkan `RescheduleCount`, serta mencatat jejak audit (waktu, petugas, dan alasan).
   - **Pembatalan**: Pembatalan jadwal mengubah status menjadi `Cancelled`, melepaskan slot mesin, dan merekam alasan tanpa membatalkan `OrderRadiologi` yang mendasarinya.
   - **Penyelesaian Kesiapan**: Transisi status menjadi `Completed` hanya sah jika pasien telah tiba (`IsArrived = true`) dan terverifikasi aman (`IsCleared = true`), yang kemudian menandai pelepasan slot jadwal menuju pelaksanaan pemindaian fisik di `RAD-EXAM`.

7. **Penerbitan Bukti & Integrasi Pelacakan**:
   - Sistem dapat menerbitkan/mencetak Lembar Konfirmasi Jadwal beserta panduan persiapan pasien.
   - Status tonggak jadwal tersinkronisasi ke pemantauan perjalanan pasien (`ADM-TRACKER`).

## Current Situation

1. **Spesifikasi Kanonikal**:
   - Dokumen spesifikasi outcome telah ditetapkan secara kanonikal pada [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md) (Versi 1.0, Draft, LastUpdated: 2026-11-23).
   - Definisi kapabilitas domain telah didokumentasikan pada [08-RADIOLOGI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/08-RADIOLOGI-DOMAIN.md) (`RAD-JADWAL`).

2. **Backend `b09-bilreg-api`**:
   - Konteks bounded `RadContext` telah memiliki implementasi untuk manajemen order (`RadOrderFeature` pada domain model, application use cases, persistence SQL, dan controller) serta spesifikasi pembebanan billing `OC-RAD-ORDER-CHARGE`.
   - Namun, entitas dan use cases untuk penjadwalan pemeriksaan radiologi (`RAD-JADWAL`) belum diimplementasikan pada `RadContext` di backend (belum terdapat domain model jadwal, repository, migrasi tabel SQL, use case scheduling/reschedule/cancel/clearance, maupun API endpoint).

3. **Frontend `c012_myhospital_web`**:
   - Modul `src/modules/Radiology` memiliki komponen tampilan awal seperti [RadSchedulingPanel.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/components/RadSchedulingPanel.vue) dan [TindakanRadiologi.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/views/TindakanRadiologi.vue), namun saat ini masih menggunakan mock data dan belum terintegrasi secara persisten dengan backend API penjadwalan.

## Evidence

- **Authoritative Outcome Specification**:
  - [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md) (Versi 1.0, Draft, LastUpdated: 2026-11-23)
- **Domain Canonical References**:
  - [08-RADIOLOGI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/08-RADIOLOGI-DOMAIN.md) (`RAD-JADWAL`, `RAD-ORDER`, `RAD-EXAM`, `RAD-EXPERTISE`)
  - [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md) (`PAS-DATSOS`)
  - [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md) (`ORG-LAYANAN`, `ORG-PPA`)
  - [03-ADMISSION-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/03-ADMISSION-DOMAIN.md) (`ADM-REG`, `ADM-TRACKER`)
- **Existing Backend References (`b09-bilreg-api`)**:
  - `RadContext` API: [RadOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/RadContext/RadOrderController.cs)
  - `RadContext` Domain: [Bilreg.Domain/RadContext/RadOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature)
  - `RadContext` Docs: [OC-RAD-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-ORDER-CHARGE-FEATURE.md)
- **Existing Frontend References (`c012_myhospital_web`)**:
  - [RadSchedulingPanel.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/components/RadSchedulingPanel.vue)
  - [TindakanRadiologi.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/views/TindakanRadiologi.vue)

## Notes

- Dokumen ini merupakan artefak intake formal yang bersifat netral terhadap solusi teknis (*solution-neutral*), berfokus mencatat APA yang diminta dan kondisi sistem saat ini tanpa mendikte arsitektur teknis atau implementasi kode.
- Ruang lingkup di luar batas (*Out of Scope*):
  - **Penerbitan Order Pemeriksaan & Penetapan Item Tindakan**: Wewenang `RAD-ORDER` (`OC-RAD-ORDER-RAD`).
  - **Pelaksanaan Pemindaian Fisik & Akuisisi Gambar**: Pengoperasian alat, scanning, protokol slice, dan pengiriman citra ke PACS (wewenang `RAD-EXAM` / `OC-RAD-EXAM`).
  - **Penyusunan Ekspertise & Verifikasi Hasil**: Wewenang `RAD-EXPERTISE` (`OC-RAD-EXPERTISE`).
  - **Pendaftaran Registrasi Kunjungan Rumah Sakit**: Wewenang `ADM-REG` dan `BPJ-VCLAIM`.
  - **Pembebanan Biaya Tindakan & Pembayaran Kasir**: Wewenang `TRK-BILLING` dan `TRK-KASIR`.
  - **Jadwal Dinas / Shift Tenaga Medis**: Wewenang modul SDM di `ORG-PPA`.
- Alur kerja berikutnya untuk CHANGE-REQUEST ini diteruskan ke peran **ica-analyst** untuk tahapan **Discovery** dan **FEASIBILITY-ASSESSMENT** (Analisis Kelayakan).
