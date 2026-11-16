---
Title: Test Package - Penerapan Prefix Ruangan pada Sesi Antrian Poli/Dokter, Display Antrian, dan Worklist Rawat Jalan
Code: ANTRIAN-POLI-PREFIX
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-11-13
Status: NOT-EXECUTED
TestExecution: [ANTRIAN-POLI-PREFIX-TEST-EXECUTION.md](ANTRIAN-POLI-PREFIX-TEST-EXECUTION.md)
---

# 1. Tujuan Pengujian

Memastikan kode prefix ruangan poliklinik diterapkan secara benar pada seluruh alur
antrian poli: pendaftaran langsung (walk-in), ubah kunjungan, reservasi (booking),
cetakan struk anjungan mandiri, tampilan dan suara pemanggilan monitor klinik,
serta tampilan nomor antrian pada aplikasi pendaftaran dan daftar periksa dokter.

Hasil yang diharapkan secara umum:

- Nomor antrian poli tampil ringkas tanpa nol di depan, contoh: `A3`, `OB12`.
- Apabila ruangan tidak memiliki prefix, nomor antrian tampil polos, contoh: `3`.
- Antrian Loket Admisi tetap memakai format 4 digit (`A0001`) — tidak boleh berubah.
- Antrian IGD tetap tanpa prefix.

Referensi:
- IMPLEMENTATION-PLAN: `ANTRIAN-POLI-PREFIX-IMPLEMENTATION-PLAN.md` (Status: COMPLETED)
- ARCHITECTURE: `ANTRIAN-POLI-PREFIX-ARCHITECTURE.md`
- ISSUE: `ANTRIAN-POLI-PREFIX-ISSUE.md`

---

# 2. Prasyarat Lingkungan

| No | Prasyarat | Keterangan |
|---|---|---|
| P1 | Database SQL Server Bilreg | Migrasi `BILRG_Antrian_M3_QueuePrefixSnapshot_Widen_Alter.sql` sudah dijalankan (kolom `QueuePrefixSnapshot` VARCHAR(5)). |
| P2 | Layanan Bilreg (`Bilreg.Api`) berjalan | Port sesuai konfigurasi lingkungan uji. |
| P3 | Aplikasi anjungan (`kiosk-web`) dan monitor (`display-web`) berjalan | Terhubung ke layanan Bilreg yang sama. |
| P4 | Aplikasi web petugas (`c012_myhospital_web`) berjalan | Modul Admisi dan Outpatient dapat diakses. |
| P5 | Master ruangan siap | Minimal 2 ruangan poli: satu dengan prefix 1 huruf (mis. `A`), satu dengan prefix 2 huruf (mis. `OB`), dan satu tanpa prefix (kosong/`-`). |
| P6 | Jadwal praktek dokter siap | Jadwal dokter terpasang pada ruangan-ruangan di P5, pada hari uji. |
| P7 | Akun pengguna | Akun petugas pendaftaran dengan akses modul Admisi/Outpatient. |
| P8 | Printer thermal kiosk terpasang (opsional) | Bila tidak tersedia, gunakan pratinjau cetak (print preview). |
| P9 | Speaker/audio aktif pada mesin display (opsional) | Untuk TC-07; dapat digantikan verifikasi visual urutan audio bila tidak ada speaker. |

---

# 3. Data Uji

| Kode | Data | Nilai |
|---|---|---|
| D1 | Pasien baru (walk-in) | Nama `TEST PREFIX A`, NIK valid (dewasa), bayar sendiri |
| D2 | Pasien booking | Nama `TEST PREFIX OB`, tanpa No. RM (booking tanpa pasien terdaftar) atau pasien mana pun |
| D3 | Pasien aktif terdaftar | Pasien dari TC-01, untuk uji ubah kunjungan |
| D4 | Ruangan A | Prefix `A` |
| D5 | Ruangan OB | Prefix `OB` |
| D6 | Ruangan tanpa prefix | Prefix kosong / `-` |
| D7 | Jadwal Dokter Poli Umum jam 08:00 | Terpasang di Ruangan A (D4) |
| D8 | Jadwal Dokter Poli Mata jam 09:00 | Terpasang di Ruangan OB (D5) |
| D9 | Jadwal Dokter tanpa ruangan | Ruangan `-`, prefix kosong |

---

# 4. Kasus Uji

## TC-01 — Pendaftaran Walk-In dengan Ruangan Berprefix 1 Huruf (Happy Path)

**Tujuan**: Pasien walk-in pada jadwal ruangan berprefix `A` mendapat nomor antrian berprefix.

**Prasyarat**: P1–P7; jadwal D7; data D1.

**Langkah**:
1. Buka aplikasi pendaftaran, buka modul **Admisi**.
2. Pendaftaran Rawat Jalan walk-in: pilih pasien D1, dokter pada jadwal D7 (jam 08:00), bayar sendiri.
3. Simpan pendaftaran, amati layar bukti/hasil registrasi.

**Hasil Diharapkan**:
- Bukti registrasi menampilkan nomor antrian berprefix ringkas: `A1` (pasien pertama), `A2`, dst. — **tanpa nol di depan** (bukan `A0001`).
- Kolom nomor antrian numerik di sistem tetap menampilkan angka polos (1, 2, 3) tanpa huruf.

---

## TC-02 — Pendaftaran Walk-In dengan Ruangan Berprefix 2 Huruf

**Tujuan**: Prefix 2 huruf (`OB`) diteruskan dengan benar.

**Prasyarat**: P1–P7; jadwal D8; data D1 (atau pasien lain).

**Langkah**:
1. Daftarkan pasien walk-in pada jadwal D8 (Poli Mata, jam 09:00, ruangan `OB`).
2. Amati bukti registrasi.

**Hasil Diharapkan**:
- Nomor antrian tampil `OB1`, `OB2`, dst. (ringkas, tanpa nol di depan).

---

## TC-03 — Pendaftaran Walk-In pada Ruangan Tanpa Prefix

**Tujuan**: Ruangan tanpa prefix menghasilkan nomor polos.

**Prasyarat**: Jadwal D9 (ruangan `-`).

**Langkah**:
1. Daftarkan pasien walk-in pada jadwal D9.
2. Amati bukti registrasi.

**Hasil Diharapkan**:
- Nomor antrian tampil polos: `1`, `2`, dst. — tidak ada huruf, tidak ada nol di depan.

---

## TC-04 — Reservasi (Booking) Mendapat Label Berprefix

**Tujuan**: Booking pada jadwal berprefix mengembalikan label antrian berprefix.

**Prasyarat**: Jadwal D7 dan D8; data D2.

**Langkah**:
1. Buat booking baru untuk pasien D2 pada jadwal D8 (ruangan `OB`).
2. Amati hasil booking (layar konfirmasi booking).

**Hasil Diharapkan**:
- Hasil booking menampilkan nomor antrian berprefix `OB<n>` (ringkas).
- Booking pada jadwal D7 menampilkan `A<n>`.

---

## TC-05 — Ubah Kunjungan ke Jadwal Berprefix Lain

**Tujuan**: Saat kunjungan pasien diubah ke poli/jadwal berbeda, nomor antrian mengikuti prefix ruangan tujuan.

**Prasyarat**: Pasien hasil TC-01 (sudah terdaftar di Poli Umum ruangan `A`).

**Langkah**:
1. Buka modul Admisi, lakukan **Ubah Kunjungan** untuk pasien tersebut ke jadwal D8 (Poli Mata, ruangan `OB`).
2. Amati hasil ubah kunjungan.

**Hasil Diharapkan**:
- Nomor antrian baru mengikuti prefix ruangan tujuan: `OB<n>` (ringkas).
- Tidak muncul gabungan prefix lama+baru (mis. tidak ada `AOB2`).

---

## TC-06 — Detail Antrian (Query) Menyertakan Label

**Tujuan**: Endpoint detail antrian mengirim label siap tampil dan prefix.

**Prasyarat**: P2; ada minimal 1 pasien terdaftar pada sesi jadwal D7 (dari TC-01).

**Langkah**:
1. Dari aplikasi petugas (modul Outpatient) atau tool API (mis. Swagger/Postman), ambil detail antrian sesi jadwal D7 (`GET api/Antrian/{id}`).
2. Periksa isi respons.

**Hasil Diharapkan**:
- Respons memuat `noAntrian` berupa angka (mis. `1`) **dan** `queueLabel` berupa teks berprefix (mis. `"A1"`), serta `prefixAntrian` (`"A"`).
- Aplikasi tidak gagal memuat data (skema tetap kompatibel).

---

## TC-07 — Monitor Klinik: Tampilan dan Suara Pemanggilan

**Tujuan**: Monitor poli menampilkan label ringkas dan suara memanggil huruf lalu angka.

**Prasyarat**: P3, P9; pasien dari TC-01/TC-02 dalam status menunggu panggil.

**Langkah**:
1. Buka monitor display poli (display-web) pada layar klinik Poli Umum / Poli Mata.
2. Panggil pasien dari loket petugas.
3. Amati tampilan kartu panggilan dan dengarkan suara.

**Hasil Diharapkan**:
- Kartu panggilan menampilkan `A3` / `OB12` (ringkas, tanpa nol di depan).
- Suara menyebut huruf satu per satu lalu angka, contoh `OB12`: "O", "B", "satu", "dua".
- Tidak ada lafal aneh (mis. "O-B-satu-dua" terputus, atau dibaca "OB" sebagai kata).

---

## TC-08 — Struk Tiket Anjungan Mandiri (Kiosk)

**Tujuan**: Struk cetak mandiri menampilkan nomor berprefix.

**Prasyarat**: P3, P8; kiosk terhubung ke layanan Bilreg.

**Langkah**:
1. Lakukan pendaftaran mandiri (walk-in) di kiosk pada jadwal D7 atau D8.
2. Ambil struk cetak (atau lihat pratinjau cetak).

**Hasil Diharapkan**:
- Struk mencetak nomor antrian berprefix ringkas (`A3` / `OB12`), bukan format 4 digit.

---

## TC-09 — Banner Pasien & Daftar Periksa Dokter (Web Petugas)

**Tujuan**: Modul Outpatient menampilkan label berprefix selaras dengan tiket pasien.

**Prasyarat**: P4; pasien dari TC-01/TC-02.

**Langkah**:
1. Buka modul **Outpatient**, buka daftar periksa dokter (workbench tindakan) untuk Poli Umum / Poli Mata.
2. Periksa baris pasien pada daftar tunggu/pemanggilan.
3. Buka banner identitas pasien pasien tersebut.

**Hasil Diharapkan**:
- Daftar periksa menampilkan nomor antrian berprefix (`A3` / `OB12`) sesuai tiket fisik.
- Banner pasien menampilkan nomor antrian berprefix yang sama dengan tiket pasien.

---

## TC-10 — Aturan Bisnis: Snapshot Tidak Ditimpa (Immutability)

**Tujuan**: Sesi antrian yang sudah memiliki prefix tidak berubah meski ruangan jadwal diubah di tengah hari.

**Prasyarat**: Sesi jadwal D7 sudah berjalan dengan prefix `A` dan pasien terdaftar.

**Langkah**:
1. Ubah master ruangan jadwal D7 menjadi ruangan `OB` (prefix berubah) **setelah** sesi terbentuk.
2. Daftarkan pasien walk-in baru pada jadwal D7 yang sama.
3. Amati nomor antrian pasien baru dan pasien lama.

**Hasil Diharapkan**:
- Sesi antrian tetap memakai prefix lama (`A`): pasien baru mendapat `A<n+1>`, tiket pasien lama tetap `A<n>` — tidak tercampur `OB`.
- (Backfill hanya terjadi bila sesi dibentuk saat ruangan masih kosong; lihat TC-11.)

---

## TC-11 — Backfill Satu Kali pada Sesi yang Awalnya Kosong

**Tujuan**: Sesi yang terbentuk saat ruangan belum ditentukan mendapat prefix saat ruangan kemudian ditetapkan.

**Prasyarat**: Jadwal D9 (ruangan `-`) sudah memiliki sesi dengan pasien terdaftar (nomor polos).

**Langkah**:
1. Tetapkan ruangan berprefix `A` (D4) pada jadwal D9 tersebut.
2. Daftarkan pasien walk-in baru pada jadwal yang sama.

**Hasil Diharapkan**:
- Snapshot sesi terisi `A` satu kali; pasien baru mendapat label `A<n+1>`.
- Pasien yang mendaftar sebelum backfill tetap tercatat nomor polos (tiket lama tidak berubah).

---

## TC-12 — Validasi: Prefix Tidak Valid

**Tujuan**: Nilai prefix ruangan yang tidak sesuai aturan tidak merusak alur.

**Prasyarat**: Akses master ruangan.

**Langkah**:
1. Coba isi prefix ruangan dengan nilai tidak valid (lebih dari 2 huruf, mis. `POLIKLINIK`, atau mengandung angka, mis. `A1`).
2. Amati perilaku penyimpanan master dan alur pendaftaran pada ruangan tersebut.

**Hasil Diharapkan**:
- Master ruangan menolak atau membatasi nilai (maksimal 2 huruf kapital `A-Z`); jika sistem memotong defensif (mis. `POLIKLINIK` → `PO`), pendaftaran tetap berjalan tanpa error dan label memakai nilai yang sah.
- Tidak terjadi kegagalan pendaftaran, booking, maupun tampilan.

---

## TC-13 — Regresi: Antrian Loket Admisi Tetap 4 Digit

**Tujuan**: Memastikan antrian loket admisi tidak terpengaruh.

**Prasyarat**: P1, P2; antrean loket admisi aktif (penambahan pasien ke antrean loket).

**Langkah**:
1. Buat tambahkan pasien ke antrean loket admisi (alur loket yang sudah ada).
2. Amati tampilan nomor pada layar/daftar kerja loket.

**Hasil Diharapkan**:
- Nomor tetap format 4 digit dengan prefix 1 huruf, contoh `A0001`, `A0002`.
- Tidak muncul format ringkas di loket admisi.

---

## TC-14 — Regresi: IGD Tanpa Prefix

**Tujuan**: Alur rawat darurat tidak mengirim prefix.

**Prasyarat**: P1, P2; alur IGD aktif.

**Langkah**:
1. Lakukan pendaftaran rawat darurat untuk satu pasien.
2. Periksa data antrian yang dikirim/tersimpan untuk pasien tersebut.

**Hasil Diharapkan**:
- Prefix antrian kosong; tidak ada huruf pada nomor antrian IGD.

---

## TC-15 — Kompatibilitas API Lama (Zero Breaking Change)

**Tujuan**: Klien yang hanya membaca `noAntrian` tidak gagal.

**Prasyarat**: P2; tool API.

**Langkah**:
1. Panggil `POST api/Reg/rajalWalkIn`, `POST api/Booking`, dan `GET api/Antrian/{id}`.
2. Periksa struktur respons JSON.

**Hasil Diharapkan**:
- `noAntrian` tetap bertipe angka; properti baru `queueLabel` dan `prefixAntrian` ada sebagai teks (boleh kosong bila tanpa prefix).
- Tidak ada properti lama yang hilang atau berubah tipe.

---

# 5. Urutan Eksekusi yang Disarankan

1. TC-12 (validasi master) → TC-01 → TC-03 → TC-11 (backfill) → TC-02 → TC-04 → TC-05
2. TC-06 → TC-07 → TC-08 → TC-09
3. TC-10 (immutability, setelah sesi berjalan)
4. Regresi terakhir: TC-13 → TC-14 → TC-15

---

# 6. Kriteria Lulus

- Seluruh kasus uji TC-01 s.d. TC-15 menghasilkan sesuai "Hasil Diharapkan".
- Tidak ada error pada layar, log layanan, maupun cetakan.
- Format 4 digit loket admisi dan alur IGD tidak berubah.
