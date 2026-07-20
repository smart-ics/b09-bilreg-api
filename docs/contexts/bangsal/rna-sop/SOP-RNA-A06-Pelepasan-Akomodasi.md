# SOP-RNA-A06 — Pelepasan dan Koreksi Akomodasi

## 1. Tujuan

Mengakhiri satu alokasi akomodasi ketika tujuan penggunaannya telah berakhir, atau mengoreksi Accommodation Fact secara append-only tanpa mengubah atau menghapus fakta asli.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Kepala Ruangan | Manusia | Menyetujui pelepasan dan menangani pengecualian. |
| Head Nurse | Manusia | Satu-satunya aktor yang berwenang membuat Accommodation Correction Fact untuk fakta yang masih dimiliki Ward yang sama. |
| Perawat Ruangan | Manusia | Memeriksa kondisi aktual, mencatat pelepasan, dan meneruskan kondisi bed. |
| Sistem | Sistem | Menyimpan waktu, alasan, riwayat pelepasan, Accommodation Correction Fact append-only, status finalisasi Tata Rekening, dan kondisi bed lanjutan. |
| Tata Rekening | Bounded context | Menyediakan status `FINALIZED`; setelah status ini, koreksi biasa tidak lagi diizinkan oleh RNA. |

## 3. Prasyarat

- Alokasi masih aktif dan alasan pelepasan tersedia.
- Untuk pelepasan biasa ketika perawatan berlanjut, lokasi pengganti jelas. Untuk perpindahan antar-ward, SOP-RNA-A05 berlaku dan tanggung jawab kembali ke Admisi Waiting List setelah Release.
- Petugas memiliki hak akses sesuai perannya.
- Untuk koreksi, fakta asli adalah Assignment, Internal Transfer, Release, Retained Accommodation, Rooming-In, atau Bed Readiness yang berlaku dan masih dimiliki Ward yang sama.
- Untuk koreksi, Tata Rekening belum berstatus `FINALIZED`.

## 4. Langkah Operasional

1. **Perawat Ruangan** membuka detail alokasi aktif pada **Sistem**.
2. **Perawat Ruangan** memeriksa bahwa penggunaan akomodasi benar-benar berakhir.
3. Bila alokasi merupakan lokasi klinis dan perawatan berlanjut, **Kepala Ruangan** memastikan lokasi pengganti jelas atau memilih alasan perpindahan antar-ward yang menjalankan Release-to-Waiting-List melalui SOP-RNA-A05.
4. **Kepala Ruangan** menyetujui alasan pelepasan.
5. **Perawat Ruangan** mencatat waktu berakhir aktual dan melepaskan alokasi pada **Sistem**.
6. **Sistem** menyimpan riwayat alokasi dan menampilkan kondisi bed pascapelepasan.
7. **Perawat Ruangan** melanjutkan penanganan kondisi bed melalui SOP-RNA-A07.
8. Bila pelepasan terkait discharge, **Sistem** memastikan tidak ada Retained Accommodation lain yang masih Active; seluruhnya wajib dilepas.

### Koreksi Accommodation Fact

9. **Head Nurse** membuka fakta akomodasi asli dan memeriksa bahwa fakta tersebut masih dimiliki Ward yang sama.
10. **Sistem** memeriksa status Tata Rekening. Bila status `FINALIZED`, **Sistem** menolak koreksi biasa dan mengarahkan perubahan ke proses administratif di luar RNA.
11. **Head Nurse** memasukkan fakta koreksi, alasan, dan waktu koreksi. **Sistem** mempertahankan fakta asli tanpa perubahan atau penghapusan dan menambahkan Accommodation Correction Fact yang mereferensikannya.
12. **Sistem RNA** mempublikasikan Accommodation Correction Fact kepada bounded context downstream yang berwenang. Setiap bounded context merekonsiliasi datanya sendiri.

## 5. Pengecualian Operasional

- Bila pasien hanya sementara tidak berada di ruang rawat, **Perawat Ruangan** tidak melepas alokasi hanya karena ketidakhadiran tersebut.
- Bila notifikasi Admisi dari SOP-RNA-A05 gagal setelah Release committed, RNA tidak mengaktifkan kembali alokasi; kewajiban notifikasi masuk pemulihan integrasi.
- Bila discharge masih memiliki Retained Accommodation aktif, **Sistem** menahan penyelesaian akomodasi discharge sampai seluruhnya dilepas.
- Bila fakta asli dimiliki Ward lain, **Sistem** menolak koreksi. Head Nurse tidak boleh mengoreksi fakta milik Ward lain.
- Bila Tata Rekening `FINALIZED`, **Sistem** menolak koreksi biasa. Perubahan berikutnya hanya melalui proses administratif di luar RNA.

## 6. Kriteria Penyelesaian

- **Sistem** menampilkan alokasi sebagai dilepas dengan alasan, aktor, dan waktu aktual.
- Kondisi bed pascapelepasan tercatat untuk proses kesiapan bed.
- Bila dikoreksi, fakta asli tetap terlihat utuh dan Accommodation Correction Fact baru memperlihatkan Head Nurse, alasan, waktu, serta referensi fakta asli. Status publikasi downstream terlihat tanpa RNA merekonsiliasi data downstream tersebut.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — pelepasan, koreksi append-only, dan kesiapan bed.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A07-Pemulihan-Bed-Readiness.md`.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-006 CLOSED.
