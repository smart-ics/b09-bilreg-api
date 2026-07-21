# SOP-RNA-A03 — Pengelolaan Rooming-In

## 1. Tujuan

Membentuk dan mengakhiri Rooming-In khusus Mother dan Baby dengan registrasi serta histori akomodasi tetap terpisah.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Perawat Ruangan | Manusia | Berwenang memvalidasi relasi dan mengaktifkan atau mengakhiri Rooming-In. |
| Kepala Ruangan | Manusia | Memiliki kewenangan yang sama dan menangani pengecualian operasional. |
| Patient Social Data | Sistem eksternal | Menjadi sumber relasi: Baby Medical Record mereferensikan Mother Medical Record. |
| Sistem | Sistem | Menjaga dua registrasi/histori terpisah dan membatasi bed pada satu Primary Occupant serta satu Associated Occupant (Baby). |

## 3. Prasyarat

- Mother dan Baby mempunyai registrasi yang berbeda.
- Mother memiliki alokasi Primary aktif.
- Patient Social Data membuktikan Baby Medical Record mereferensikan Mother Medical Record.
- Petugas adalah Perawat Ruangan atau Kepala Ruangan yang berwenang.

## 4. Langkah Operasional

1. **Perawat Ruangan** atau **Kepala Ruangan** membuka registrasi Mother dan Baby pada **Sistem**.
2. **Sistem** memvalidasi relasi pada **Patient Social Data** bahwa Baby Medical Record mereferensikan Mother Medical Record.
3. Aktor memeriksa Mother sebagai Primary Occupant dan memastikan bed belum memiliki Associated Occupant lain.
4. Aktor mengaktifkan Rooming-In pada **Sistem**.
5. **Sistem** membuat asosiasi Mother-Baby dan alokasi Baby yang terpisah sebagai Associated Occupant tanpa menaikkan kapasitas bed.
6. Ketika Rooming-In berakhir, **Perawat Ruangan** atau **Kepala Ruangan** mengakhiri asosiasi dan menetapkan hasil alokasi Baby melalui SOP pelepasan atau penempatan yang berlaku.

## 5. Pengecualian Operasional

- Bila hubungan Mother-Baby tidak tersedia atau tidak cocok pada **Patient Social Data**, **Sistem** menolak aktivasi.
- Bila aktor bukan **Perawat Ruangan** atau **Kepala Ruangan**, **Sistem** menolak aktivasi.
- Bila bed telah mempunyai Associated Occupant, **Sistem** menolak aktivasi; Primary Occupant tidak boleh lebih dari satu dan kapasitas bed tidak ditambah.
- Bila Mother dan Baby harus dipisahkan, aktor mengakhiri asosiasi dan memastikan hasil akomodasi Baby jelas.

## 6. Kriteria Penyelesaian

- **Sistem** menampilkan dua registrasi dan histori akomodasi terpisah dengan satu Mother Primary Occupant dan satu Baby Associated Occupant pada bed yang sama.
- Kapasitas bed tidak bertambah. RNA tidak menghitung BOR.
- Asumsi billing legacy “one room charge only” tercatat sebagai asumsi dokumentasi sampai konfirmasi legacy; tidak ada keputusan billing di RNA.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — rooming-in dan akomodasi.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-004 CLOSED.
