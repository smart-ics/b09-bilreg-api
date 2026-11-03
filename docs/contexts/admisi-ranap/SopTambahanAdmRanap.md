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
- **Register Ibu**: kode registrasi ibu yang menjadi rujukan bagi pasien perinatal (apabila ibu dirawat).

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
| `fs_reg_ibu` | Kode registrasi ibu, kondisional untuk jenis inap perinatal (kosong jika ibu tidak dirawat) |


### 5.2 Tabel Terkait Lain
| Tabel | Fungsi |
|---|---|
| `ta_reg_inap` | Data lanjutan registrasi inap (cara masuk inap, booking bed, dll.) |
| `tc_mr` | Data pasien/MR, termasuk relasi ibu-anak untuk perinatal |

## 6. Aturan Penentuan Jenis Inap

### 6.1 Pasien Perinatal
1. Pasien berumur kurang dari 1 bulan (0 tahun, 0 bulan) secara default diarahkan ke jenis inap **perinatal (`"3"`)**, namun petugas dapat memilih jenis inap **umum (`"1"`)** apabila diperlukan.
2. Pada jenis inap perinatal, sistem secara otomatis mencoba mengambil nomor register ibu aktif dari data MR ibu jika tersedia.
3. **Kondisionalitas Register Ibu**:
   - Jika ibu tidak sedang dirawat inap di rumah sakit (misal rujukan luar, readmisi bayi, atau kondisi sosial), Register Ibu dapat dikosongkan dan pendaftaran tetap dapat diproses (`fs_reg_ibu = ""`).
   - Jika Register Ibu diisi (baik otomatis atau manual), maka wajib memenuhi validasi integritas berikut:
     a. Register ibu harus terdaftar di database.
     b. Register ibu tidak boleh void (`fd_tgl_void == "3000-01-01"`).
     c. Register ibu tidak boleh sudah keluar/pulang (`fd_tgl_keluar == "3000-01-01"`).
     d. Register ibu harus memiliki jenis inap **obstetri (`"2"`)**.
4. Jika Register Ibu diisi namun salah satu syarat validasi integritas tidak terpenuhi, registrasi ditolak.

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
3. Jika umur pasien < 1 bulan:
   - Sistem secara default menetapkan jenis inap = perinatal (`3`), namun petugas dapat memilih umum (`1`).
   - Jika jenis inap perinatal dipilih, sistem mencoba mengambil register ibu aktif dari MR ibu.
4. Jika tidak memenuhi kriteria perinatal:
   - Sistem set jenis inap = umum (`1`), kecuali petugas memilih obstetri (`2`).
5. Jika jenis inap perinatal:
   - Jika register ibu terisi (otomatis maupun manual), sistem memvalidasi:
     - register ibu ada di database,
     - tidak void,
     - tidak keluar/pulang,
     - jenis inap ibu = obstetri (`2`).
   - Jika register ibu tidak terisi (ibu tidak dirawat inap), registrasi tetap diizinkan.
6. Jika validasi register ibu gagal, registrasi ditolak.
7. Simpan data ke `ta_registrasi` dengan `fs_kd_jenis_inap` dan `fs_reg_ibu` (berisi nomor registrasi ibu jika ada, atau string kosong jika tidak ada).

## 8. Validasi Utama

| Kondisi | Hasil |
|---|---|
| Umur < 1 bulan (default) | Default perinatal (`3`), dapat dipilih umum (`1`) |
| Umur ≥ 1 bulan | Default umum (`1`), kecuali dipilih obstetri (`2`) |
| Perinatal tanpa register ibu (ibu tidak dirawat) | Diterima (`fs_kd_jenis_inap = "3"`, `fs_reg_ibu = ""`) |
| Perinatal dengan register ibu valid | Diterima (`fs_kd_jenis_inap = "3"`, `fs_reg_ibu = "{regIdIbu}"`) |
| Register ibu tidak ditemukan di database | Ditolak |
| Register ibu void | Ditolak |
| Register ibu sudah keluar | Ditolak |
| Register ibu bukan obstetri | Ditolak dengan pesan “JENIS INAP IBU invalid” |

## 9. Output yang Diharapkan
- `ta_registrasi.fs_kd_jenis_inap` selalu terisi sesuai aturan.
- Untuk perinatal, `ta_registrasi.fs_reg_ibu` terisi registrasi ibu yang valid jika ibu dirawat, atau string kosong jika ibu tidak dirawat.