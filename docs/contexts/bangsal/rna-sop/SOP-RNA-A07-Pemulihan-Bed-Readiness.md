# SOP-RNA-A07 — Pemulihan Kesiapan Bed

## 1. Tujuan

Merekam transaksi operasional Bed Readiness sampai bed dapat kembali siap digunakan setelah kebutuhan pembersihan, pemeriksaan, perbaikan, atau pembatasan selesai. Status operasional saat ini yang ditampilkan pada Bed adalah proyeksi transaksi readiness terakhir.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Kepala Ruangan | Manusia | Mengawasi kesiapan bed dan mengeskalasi hambatan. |
| Perawat Ruangan | Manusia | Menilai kondisi bed, meminta pemulihan, dan memverifikasi hasil sesuai kewenangannya. |
| Petugas Housekeeping | Manusia | Melakukan pembersihan menurut workflow miliknya dan dapat menyerahkan bukti atau referensi penyelesaian. |
| Petugas Maintenance/Fasilitas | Manusia | Menangani masalah teknis atau keselamatan menurut workflow miliknya dan dapat menyerahkan bukti atau referensi penyelesaian. |
| Sistem | Sistem | Mencatat transaksi Bed Readiness RNA dan memproyeksikan status operasional saat ini pada Bed. |

## 3. Prasyarat

- Bed telah dilepas atau terdapat temuan yang membuatnya tidak siap digunakan.
- Identitas bed dan kondisi awal dapat diakses.
- Petugas memiliki hak akses sesuai tanggung jawabnya.

## 4. Langkah Operasional

1. **Perawat Ruangan** membuka riwayat dan status proyeksi Bed pada **Sistem**, lalu merekam transaksi Bed Readiness untuk kondisi awal atau kebutuhan pembersihan, pemeriksaan, perbaikan, atau pembatasan. Transaksi memuat bed, status baru, waktu bisnis, aktor, dan alasan yang berlaku.
2. Bila diperlukan, **Perawat Ruangan** mengarahkan permintaan ke workflow Housekeeping atau Maintenance yang berwenang. RNA tidak mengelola tugas atau workflow tersebut.
3. **Petugas Housekeeping** atau **Petugas Maintenance/Fasilitas** menyelesaikan pekerjaan dalam workflow miliknya dan menyerahkan bukti atau referensi penyelesaian yang dapat diautentikasi.
4. **Perawat Ruangan** merekam transaksi readiness lanjutan yang relevan, termasuk bukti atau referensi yang diterima, dan memeriksa bahwa tidak ada kondisi yang menghalangi penggunaan bed.
5. **Kepala Ruangan** atau **Perawat Ruangan** yang berwenang memverifikasi `Ready` secara eksplisit dan merekam transaksi dengan Readiness Status `Ready`, `OccurredAt` sebagai waktu bisnis readiness, `RecordedAt` sebagai waktu persistence sistem, dirinya sebagai Responsible Actor/verifikator, Reason bila diperlukan, dan Evidence bila tersedia. Bila waktu bisnis tidak diketahui, sistem menetapkan `OccurredAt = RecordedAt`.
6. **Sistem** menyimpan setiap transaksi dalam Bed Readiness History RNA dan memperbarui status operasional saat ini pada Bed sebagai proyeksi transaksi terakhir.

## 5. Pengecualian Operasional

- Bila ada risiko keselamatan, **Perawat Ruangan** merekam transaksi `Blocked` atau `Out Of Service` dengan alasan dan **Kepala Ruangan** mengeskalasi kepada **Petugas Maintenance/Fasilitas**.
- Bila pembersihan selesai tetapi pemeriksaan atau perbaikan belum selesai, **Perawat Ruangan** merekam atau mempertahankan transaksi status non-Ready yang sesuai; bed tidak diproyeksikan Ready.
- Bila terdapat alokasi aktif yang bertentangan dengan kesiapan bed, **Kepala Ruangan** menahan transaksi `Ready` dan merekonsiliasi data alokasi.
- Current-state `Ready` pada Bed master tanpa transaksi verifikasi yang sesuai tidak diterima sebagai audit trail otoritatif; petugas harus merekonsiliasi riwayat, bukan mengubah flag saja.

## 6. Kriteria Penyelesaian

- Bed Readiness History yang otoritatif memuat setiap perubahan beserta `OccurredAt`, `RecordedAt`, Responsible Actor, Readiness Status, Reason opsional, dan Evidence opsional; urutan bisnis menggunakan `OccurredAt`.
- **Sistem** memproyeksikan bed sebagai siap digunakan dari transaksi `Ready` terakhir, beserta aktor/verifikator dan waktu bisnis verifikasi.
- Bila pekerjaan belum selesai, proyeksi Bed tetap tidak tersedia dengan alasan dan penanggung jawab yang jelas.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — bed readiness.
- `docs/contexts/bangsal/RNA-ARCHITECTURE.md` — batas tanggung jawab Housekeeping dan Maintenance.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — model Bed Readiness transaksional yang telah difinalkan.
