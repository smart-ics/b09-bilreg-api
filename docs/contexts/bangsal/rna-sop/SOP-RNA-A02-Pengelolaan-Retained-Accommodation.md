# SOP-RNA-A02 — Pengelolaan Retained Accommodation

## 1. Tujuan

Mempertahankan alokasi akomodasi lama tetap Active ketika pasien aktif menerima perawatan di akomodasi lain, lalu memastikan alokasi tersebut dilepas saat discharge.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Perawat Ruangan | Manusia | Berwenang membentuk atau melepaskan Retained Accommodation dan memastikan fakta aktual tercatat. |
| Kepala Ruangan | Manusia | Memiliki kewenangan yang sama dan mengawasi penyelesaian seluruh retained allocation saat discharge. |
| Sistem | Sistem | Menjaga alokasi retained tetap Active, menghitungnya sebagai pemakai kapasitas, mempublikasikan fakta akomodasi, dan mencegah discharge selesai selama retained allocation masih aktif. |

## 3. Prasyarat

- Alokasi lama masih Active.
- Pasien sedang atau akan aktif menerima perawatan di akomodasi lain.
- Petugas adalah Perawat Ruangan atau Kepala Ruangan yang berwenang.

## 4. Langkah Operasional

1. **Perawat Ruangan** atau **Kepala Ruangan** membuka detail akomodasi pasien dan memilih alokasi lama yang tetap akan dipertahankan.
2. Aktor tersebut menetapkan Retained Accommodation dengan alasan dan waktu bisnis pada **Sistem**.
3. **Sistem** menjaga alokasi tersebut tetap `Active`, tidak lagi menampilkannya sebagai Clinical Accommodation, dan tetap menghitungnya sebagai pemakai kapasitas bed.
4. **Sistem** terus menghasilkan fakta akomodasi retained bagi downstream; RNA tidak menilai billing atas fakta tersebut.
5. Retained Accommodation tetap Active selama encounter berjalan dan tidak dilepas sebelum discharge.
6. Saat discharge, **Sistem** menampilkan seluruh Retained Accommodation yang masih aktif dan mewajibkan **Perawat Ruangan** atau **Kepala Ruangan** melepaskan semuanya melalui SOP-RNA-A06 sebelum penyelesaian akomodasi discharge.

## 5. Pengecualian Operasional

- Bila aktor bukan **Perawat Ruangan** atau **Kepala Ruangan**, **Sistem** menolak pembentukan retained allocation.
- Bila akomodasi lain untuk perawatan aktif belum dapat diidentifikasi, aktor tidak membentuk Retained Accommodation.
- Bila discharge diminta ketika retained allocation masih aktif, **Sistem** menahan penyelesaian akomodasi discharge dan menampilkan seluruh alokasi yang wajib dilepas.

## 6. Kriteria Penyelesaian

- **Sistem** menampilkan retained allocation sebagai Active, pemakai kapasitas, dan penghasil fakta akomodasi dengan alasan, aktor, serta waktu yang jelas.
- Riwayat alokasi tetap utuh dan seluruh Retained Accommodation telah Released pada discharge.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — retained accommodation dan transfer.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A04-Transfer-Akomodasi-Internal-RNA.md`.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A05-Transfer-Antar-RNA.md`.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-003 CLOSED.
