# SOP Penentuan Jenis Inap Rawat Inap

## 1. Tujuan
Menstandarkan penentuan jenis inap pasien rawat inap menjadi:
- **Umum** (bukan perinatal/obstetri)
- **Obstetri**
- **Perinatal**

serta memastikan relasi ibu-bayi pada kasus perinatal tervalidasi.

## 2. Ruang Lingkup
Berlaku pada proses registrasi rawat inap, validasi registrasi, dan pencatatan data pada tabel master dan transaksi registrasi.

## 3. Definisi
- **Jenis Inap Umum (kode `1`)**: pasien rawat inap biasa, bukan obstetri/perinatal.
- **Jenis Inap Obstetri (kode `2`)**: pasien ibu yang dirawat terkait kehamilan/persalinan.
- **Jenis Inap Perinatal (kode `3`)**: pasien bayi/neonatus yang dirawat dan terkait dengan registrasi ibu.
- **Register Ibu**: kode registrasi ibu yang menjadi rujukan bagi pasien perinatal.

## 4. Master Data Terkait

### 4.1 Tabel Master: `ta_jenis_inap`
| Field | Keterangan |
|---|---|
| `fs_kd_jenis_inap` | Kode jenis inap: `1` umum, `2` obstetri, `3` perinatal |
| `fs_nm_jenis_inap` | Nama jenis inap |

## 5. Tabel Transaksi Terkait

### 5.1 `ta_registrasi`
Field utama terkait jenis inap:
| Field | Keterangan |
|---|---|
| `fs_kd_reg` | Kode registrasi |
| `fs_mr` | No. MR pasien |
| `fs_kd_jenis_inap` | Jenis inap: `1` umum, `2` obstetri, `3` perinatal |
| `fs_reg_ibu` | Kode registrasi ibu, wajib untuk jenis inap perinatal |


### 5.2 Tabel Terkait Lain
| Tabel | Fungsi |
|---|---|
| `ta_reg_inap` | Data lanjutan registrasi inap (cara masuk inap, booking bed, dll.) |
| `tc_mr` | Data pasien/MR, termasuk relasi ibu-anak untuk perinatal |

## 6. Aturan Penentuan Jenis Inap

### 6.1 Pasien Perinatal
Pasien ditentukan sebagai **perinatal** jika:
1. Umur pasien kurang dari 1 bulan (0 tahun, 0 bulan).
2. Sistem otomatis set `fs_kd_jenis_inap = "3"`.
3. Sistem otomatis mengambil register ibu dari data MR ibu.
4. Register ibu harus terdaftar dan aktif.
5. Register ibu harus memiliki jenis inap **obstetri (`"2"`)**.
6. Register ibu tidak boleh void.
7. Register ibu tidak boleh sudah keluar.

Jika salah satu syarat tidak terpenuhi, registrasi perinatal ditolak.

### 6.2 Pasien Obstetri
Pasien ditentukan sebagai **obstetri** jika:
1. Jenis inap dipilih/ditetapkan sebagai `"2"`.
2. Pasien merupakan ibu yang dirawat terkait kehamilan/persalinan.
3. Register obstetri dapat menjadi rujukan bagi pasien perinatal.

### 6.3 Pasien Bukan Perinatal/Obstetri (Umum)
Pasien ditentukan sebagai **umum** jika:
1. Umur pasien 1 bulan atau lebih, atau
2. Tidak memenuhi kriteria perinatal, dan
3. Jenis inap diset `"1"`.

## 7. Alur Proses Bisnis

1. Petugas membuka registrasi rawat inap.
2. Sistem membaca data pasien dari `tc_mr`.
3. Jika pasien baru dan umur < 1 bulan:
   - Sistem otomatis set jenis inap = perinatal (`3`).
   - Sistem mengambil register ibu aktif.
4. Jika tidak memenuhi kriteria perinatal:
   - Sistem set jenis inap = umum (`1`), kecuali petugas memilih obstetri (`2`).
5. Jika jenis inap perinatal:
   - Sistem validasi register ibu:
     - register ibu ada,
     - tidak void,
     - tidak keluar,
     - jenis inap ibu = obstetri (`2`).
6. Jika validasi gagal, registrasi ditolak.
7. Jika valid, simpan ke `ta_registrasi` dengan `fs_kd_jenis_inap` dan `fs_reg_ibu` bila perinatal.

## 8. Validasi Utama

| Kondisi | Hasil |
|---|---|
| Umur < 1 bulan | Otomatis perinatal |
| Umur ≥ 1 bulan | Default umum, kecuali dipilih obstetri |
| Perinatal tanpa register ibu | Ditolak |
| Register ibu void | Ditolak |
| Register ibu sudah keluar | Ditolak |
| Register ibu bukan obstetri | Ditolak dengan pesan “JENIS INAP IBU invalid” |

## 9. Output yang Diharapkan
- `ta_registrasi.fs_kd_jenis_inap` selalu terisi sesuai aturan.
- Untuk perinatal, `ta_registrasi.fs_reg_ibu` terisi dan valid.