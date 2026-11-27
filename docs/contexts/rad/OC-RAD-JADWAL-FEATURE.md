---
Title: Penjadwalan Pemeriksaan Radiologi (JadwalRadiologi)
Code: OC-RAD-JADWAL
Artifact: FEATURE
Version: 1.0
LastUpdated: 2026-11-25
---

# 1. Purpose

Rumah sakit membutuhkan kapabilitas resmi untuk merencanakan, mengalokasikan, dan mengelola slot waktu penggunaan mesin diagnostik radiologi dan ruang pemeriksaan fisik bagi pasien secara terstruktur sebagai fakta bisnis persisten (*persisted business fact*).

Pemeriksaan radiologi—khususnya modalitas diagnostik canggih dengan kapasitas terbatas dan durasi pemindaian terencana (seperti CT-Scan, MRI, Fluoroskopi, USG khusus, dan tindakan biopsi/intervensi)—mengharuskan kehadiran fisik pasien (*physical presence*) pada mesin pemindaian tertentu. Penjadwalan slot mesin menjadi krusial untuk mencegah perebutan sumber daya (*resource contention/double-booking*), mengoptimalkan utilisasi alat bernilai tinggi, memastikan kepatuhan persiapan pra-tindakan klinis (puasa, hidrasi, atau uji kreatinin darah), serta memberikan kepastian waktu pelayanan bagi pasien rawat jalan, rawat inap, maupun rujukan eksternal.

# 2. Business Outcome

Slot waktu penggunaan mesin radiologi dan ruang pemeriksaan telah resmi dialokasikan untuk pasien berdasarkan permintaan order radiologi yang sah, instruksi persiapan telah diterbitkan, dan alokasi tercatat sebagai fakta bisnis persisten (`Radiology Examination Schedule exists`) berstatus `Scheduled` yang siap digunakan sebagai acuan pemanggilan serta gerbang keselamatan (*clearance gate*) menuju pelaksanaan pemindaian fisik (`RAD-EXAM`), dengan tonggak jadwal dipublikasikan ke pelacakan perjalanan pasien (`ADM-TRACKER`).

# 3. Participating Domains

Berdasarkan arsitektur fungsional MyHosWeb:

- **Radiology (`RAD`)**: Primary Domain (Pemilik Utama). Bertanggung jawab atas pengelolaan kapasitas modalitas, durasi slot pemeriksaan, reservasi jadwal mesin dan ruang bebas benturan (*collision-free*), pencatatan instruksi persiapan pasien, checklist keselamatan pra-tindakan (*safety clearance*), pengendalian siklus hidup jadwal (`Scheduled`, `Completed`, `Cancelled`), serta riwayat pemindahan jadwal (*in-place reschedule*).
- **Admission (`ADM`)**: Contributing Domain. Menyediakan registrasi kunjungan pasien aktif (`ADM-REG`) dan menerima pembaruan status tahapan penjadwalan untuk pemantauan alur layanan terpusat (`ADM-TRACKER`).
- **Pasien (`PAS`)**: Contributing Domain. Menyediakan snapshot identitas resmi pasien (Nomor RM, nama, tanggal lahir, jenis kelamin, dan kontak) melalui `PAS-DATSOS`.
- **Organisasi (`ORG`)**: Contributing Domain. Menyediakan master data mesin fisik dan ruangan radiologi melalui `ORG-LAYANAN`, serta data dokter spesialis radiologi (Sp.Rad) penanggung jawab melalui `ORG-PPA`.
- **Pelayanan Klinis (`RJL` / `RNA` / `IGD`)**: Contributing Domains. Unit pengorder yang menerima lembar konfirmasi jadwal dan panduan persiapan untuk dikoordinasikan kepada pasien atau perawat ruangan.

# 4. Trigger

Petugas administrasi / loket radiologi menerima order radiologi berstatus `Requested / Ordered` pada daftar kerja order radiologi dan mengeksekusi aksi penjadwalan pemeriksaan (*Schedule action*) untuk modalitas terencana.

# 5. Preconditions

1. Order radiologi terdaftar aktif dengan status `Requested / Ordered` pada `OC-RAD-ORDER-RAD` dan belum pernah dibatalkan atau selesai dipindai.
2. Seluruh item tindakan dalam order radiologi termasuk dalam kategori modalitas yang memerlukan reservasi waktu (CT, MRI, USG, Fluoroskopi, atau Biopsi). Kasus Cito/Emergency dan X-Ray rutin dikecualikan dari kewajiban reservasi di muka.
3. Mesin radiologi fisik yang dipilih berstatus aktif, operasional, dan tidak dalam jadwal pemeliharaan (*maintenance/calibration*) di `ORG-LAYANAN`.
4. Episode registrasi kunjungan pasien aktif (`RegId` pada `ADM-REG`) atau terikat pada nomor rekam medis pasien yang valid (`PAS-DATSOS`) untuk advance booking terencana.

# 6. Operational Flow

1. **Pemilihan Order Masuk & Peninjauan Klinis**:
   - Petugas loket radiologi membuka daftar kerja order radiologi yang belum terjadwal (`Requested / Ordered`).
   - Petugas memilih order, memeriksa rincian item tindakan, estimasi durasi total yang dibutuhkan, dan modalitas pemeriksaan (`CT`, `MRI`, `USG`, dll.).
2. **Pemeriksaan Kalender & Alokasi Slot Bebas Konflik**:
   - Petugas membuka tampilan kalender ketersediaan slot mesin diagnostik yang sesuai dengan modalitas order.
   - Petugas memilih mesin fisik (`EquipmentId`), ruangan pemeriksaan (`RoomId`), dokter spesialis radiologi penanggung jawab (`DoctorRadiologyId`), serta menentukan tanggal, waktu mulai (*StartTime*), dan waktu selesai (*EndTime*).
   - Sistem memvalidasi bahwa tidak ada jadwal aktif lain yang tumpang tindih (*no collision*) pada mesin dan rentang waktu yang sama.
3. **Pencatatan Panduan Persiapan & Skrining Keselamatan**:
   - Petugas mencatat instruksi persiapan pasien yang wajib dipenuhi (misal: puasa minimal 6 jam, hidrasi cukup, atau prasyarat serum kreatinin / eGFR untuk kontras intravena).
   - Petugas mengisi checklist skrining keselamatan (riwayat implan feromagnetik/pacemaker untuk MRI, status kehamilan, dan riwayat alergi kontras iodin).
4. **Penerbitan Jadwal & Konfirmasi Pasien**:
   - Sistem mencatat jadwal secara persisten dengan menghasilkan nomor unik `JadwalRadId` dan nomor transaksi `ScheduleNo`.
   - Status order radiologi diperbarui menjadi `Scheduled`.
   - Sistem menerbitkan/mencetak Lembar Konfirmasi Jadwal beserta panduan persiapan pasien.
   - Sistem mempublikasikan pembaruan tonggak perjalanan pasien ke `ADM-TRACKER`.
5. **Penjadwalan Ulang (Reschedule) - Jika Diperlukan**:
   - Atas permintaan pasien atau kendala operasional, petugas dapat memindahkan jadwal ke slot waktu atau mesin lain.
   - Sistem memvalidasi ketiadaan konflik pada slot baru, melepaskan slot lama, menaikkan `RescheduleCount`, serta mencatat alasan pemindahan dan identitas petugas dalam jejak audit *in-place*.
6. **Pembatalan Jadwal - Jika Diperlukan**:
   - Petugas dapat membatalkan jadwal dengan mencatat kategori alasan pembatalan (`Pasien Batal`, `Kondisi Pasien Tidak Stabil`, `Mesin Rusak`, atau `No-Show`).
   - Sistem mengubah status jadwal menjadi `Cancelled` dan melepaskan slot mesin.
   - Pembatalan jadwal mengembalikan status order kembali ke `Requested` tanpa membatalkan `OrderRadiologi`.
7. **Verifikasi Kesiapan Hari-H (Clearance Gate to Completed)**:
   - Pada hari pemeriksaan, pasien melapor tiba di instalasi radiologi (`IsArrived = true`).
   - Petugas/radiografer memeriksa kepatuhan persiapan dan checklist keselamatan klinis (`IsCleared = true`, dengan `ClearanceNotes`).
   - Jika lolos, status jadwal ditutup menjadi `Completed`, menandai pemanfaatan slot selesai dan pasien dialihkan ke daftar antrean pelaksanaan pemindaian fisik di mesin (`RAD-EXAM`).

# 7. Domain Orchestration

1. **`Radiology` $\rightarrow$ `Admission`**:
   - `RAD-JADWAL` membaca konteks kunjungan aktif pasien (`ADM-REG`).
   - `RAD-JADWAL` mengirimkan notifikasi event tonggak (*milestone events*: Terjadwal, Dijadwalkan Ulang, Selesai, Dibatalkan) ke `ADM-TRACKER` untuk pemantauan alur pasien rumah sakit secara terpusat.
2. **`Radiology` $\rightarrow$ `Organisasi`**:
   - `RAD-JADWAL` memvalidasi ketersediaan dan status operasional mesin diagnostik fisik dan ruangan periksa pada `ORG-LAYANAN`.
   - `RAD-JADWAL` menautkan penugasan dokter spesialis radiologi (Sp.Rad) penanggung jawab melalui data kredensial di `ORG-PPA`.
3. **`Radiology` $\rightarrow$ `Pasien`**:
   - `RAD-JADWAL` mengonsumsi snapshot data sosial pasien (`PAS-DATSOS`) untuk pencetakan lembar konfirmasi jadwal dan kontak konfirmasi kehadiran.
4. **`Radiology (RAD-JADWAL)` $\rightarrow$ `Radiology (RAD-ORDER)`**:
   - `RAD-JADWAL` mengikat `OrderRadId` yang sah dan mengunci seluruh item order dalam satu sesi jadwal terpadu.
   - Transisi jadwal ke `Scheduled` memperbarui status order; pembatalan jadwal mengembalikan order ke `Requested`.
5. **`Radiology (RAD-JADWAL)` $\rightarrow$ `Radiology (RAD-EXAM)`**:
   - Transisi jadwal menjadi `Completed` (setelah `IsArrived` dan `IsCleared` bernilai `true`) menjadi gerbang otorisasi bagi radiografer untuk memulai pemanggilan dan akuisisi citra di mesin (`RAD-EXAM`).

# 8. Constraints

1. **Invarian 1 Order = 1 Slot Jadwal Terpadu**: Seluruh item pemeriksaan dalam satu order radiologi dijadwalkan dalam satu sesi slot berkesinambungan pada mesin yang sama dengan durasi total yang diakumulasikan.
2. **Invarian Ketiadaan Konflik Waktu (No-Overlap Rule)**: Satu mesin fisik pada rentang waktu `StartTime` hingga `EndTime` hanya dapat dialokasikan untuk tepat satu jadwal pasien. Sistem wajib menolak penyimpanan jika terjadi benturan waktu (*double-booking*).
3. **Pengecualian Selektif (Selective Scheduling)**: Pemeriksaan X-Ray konvensional rutin dan seluruh order prioritas CITO / Emergency dari IGD/ICU diizinkan langsung diproses ke antrean pemindaian fisik (`RAD-EXAM`) tanpa kewajiban reservasi slot di `RAD-JADWAL`.
4. **Pemisahan Wewenang terhadap Eksekusi Mesin & Ekspertise**: `RAD-JADWAL` hanya mengelola reservasi kapasitas dan gerbang kesiapan pra-tindakan. Pengoperasian tombol eksposur, pengaturan protokol pemindaian di mesin, serta pembacaan hasil ekspertise berada di luar batas `RAD-JADWAL`.
5. **Pencatatan Audit Reschedule Non-Destruktif**: Pemindahan jadwal wajib mempertahankan identitas `JadwalRadId` yang sama, melepaskan alokasi slot lama, menaikkan `RescheduleCount`, dan mencatat riwayat pemindahan secara transparan.

# 9. Exceptions

1. **EX-01: Konflik Tumpang-Tindih Mesin (Slot Collision)**: Pengguna memilih mesin dan rentang waktu yang telah terisi oleh jadwal aktif lain. Sistem menolak penyimpanan dan menampilkan peringatan konflik waktu.
2. **EX-02: Mesin Sedang Perawatan (Maintenance / Breakdown)**: Pengguna memilih mesin yang non-aktif atau dalam kalibrasi di `ORG-LAYANAN`. Sistem memblokir pemilihan mesin dan meminta memilih mesin alternatif yang setara.
3. **EX-03: Order Radiologi Tidak Valid / Dibatalkan**: Pengguna menjadwalkan order yang tidak ditemukan, berstatus `Cancelled`, atau sudah dieksekusi. Sistem menolak pembuatan jadwal.
4. **EX-04: Skrining Keselamatan Tidak Lolos (Contraindicated)**: Pasien memiliki kontraindikasi medis mutlak (misal: implan feromagnetik aktif pada MRI) atau hasil eGFR tidak memenuhi syarat kontras tanpa instruksi nefrolog. Sistem menahan jadwal tetap pada status `Scheduled`, menolak transisi ke `Completed`, dan mencatat rincian kontraindikasi pada `ClearanceNotes`.
5. **EX-05: Pasien Tidak Hadir (No-Show Timeout)**: Pasien tidak melapor hingga batas toleransi waktu kedatangan terlampaui. Petugas menandai jadwal sebagai `Cancelled` dengan kategori alasan `No-Show`, melepaskan slot mesin, dan mengembalikan order ke status `Requested`.
6. **EX-06: Kerusakan Mesin Mendadak (Sudden Machine Failure)**: Mesin mengalami kerusakan mendadak saat beberapa jadwal telah terdaftar pada hari tersebut. Sistem menyediakan kapabilitas pemindahan mesin/jadwal massal (*reassignment*) ke mesin sejenis atau penjadwalan ulang terkoordinasi.

# 10. Acceptance Criteria

1. **AC-01**: Sistem berhasil membentuk rekaman jadwal baru dengan identifier unik `JadwalRadId`, `ScheduleNo`, dan mengikat `OrderRadId` yang sah berstatus `Requested / Ordered`.
2. **AC-02**: Sistem berhasil mengalokasikan mesin fisik spesifik, ruang pemeriksaan, dan dokter Sp.Rad penanggung jawab (bila relevan) dengan waktu mulai dan selesai otomatis berbasis estimasi durasi total ditambah buffer desinfeksi.
3. **AC-03**: Sistem secara otomatis menolak penyimpanan jadwal jika terdapat benturan rentang waktu (*collision*) pada mesin fisik yang sama.
4. **AC-04**: Order berprioritas Cito atau modalitas X-Ray rutin dapat diproses langsung ke antrean `RAD-EXAM` tanpa kewajiban reservasi slot di `RAD-JADWAL`.
5. **AC-05**: Sistem dapat mencetak Lembar Konfirmasi Jadwal beserta instruksi persiapan klinis dan checklist skrining keselamatan pra-tindakan.
6. **AC-06**: Penjadwalan ulang (*Reschedule*) berhasil memperbarui tanggal/jam/mesin, membebaskan slot lama, menaikkan `RescheduleCount`, dan mencatat alasan pemindahan dalam jejak audit *in-place*.
7. **AC-07**: Pembatalan jadwal berhasil mengubah status menjadi `Cancelled` dan membebaskan slot mesin tanpa membatalkan `OrderRadiologi` yang mendasarinya (mengembalikan order ke `Requested`).
8. **AC-08**: Pasien yang telah tiba (`IsArrived = true`) dan terverifikasi aman (`IsCleared = true`) berhasil ditransisikan status jadwalnya menjadi `Completed`, menyerahkan referensi jadwal ke `RAD-EXAM`, serta mempublikasikan status tonggak ke `ADM-TRACKER`.
