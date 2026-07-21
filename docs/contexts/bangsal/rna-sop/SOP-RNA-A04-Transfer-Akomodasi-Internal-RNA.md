# SOP-RNA-A04 — Transfer Akomodasi Internal RUANG RANAP

## 1. Tujuan

Memindahkan pasien antar-room atau antar-bed dalam RUANG RANAP yang sama dengan riwayat alokasi tetap utuh.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Kepala Ruangan | Manusia | Menyetujui transfer dan menangani pengecualian. |
| Perawat Ruangan | Manusia | Memeriksa asal dan tujuan, mengoordinasikan perpindahan, serta mencatat waktu aktual. |
| Sistem | Sistem | Menampilkan status bed dan menyimpan alokasi asal maupun tujuan. |
| Pasien atau Perwakilan Pasien | Manusia | Memberikan informasi atau persetujuan bila diperlukan. |

## 3. Prasyarat

- Akomodasi klinis asal aktif dan tanggung jawab pasien tetap pada RUANG RANAP yang sama.
- Alasan transfer dan kandidat tujuan tersedia untuk diperiksa.
- Petugas memiliki hak akses sesuai perannya.

## 4. Langkah Operasional

1. **Perawat Ruangan** membuka detail akomodasi pasien pada **Sistem** dan memeriksa alasan transfer.
2. **Perawat Ruangan** memeriksa Mandatory Bed Assignability pada bed tujuan: bed ada dan aktif, berada pada Ward tujuan yang benar, Ready, tidak memiliki alokasi aktif yang bertentangan, serta kapasitas tersedia menurut occupancy policy. Tidak ada pembatasan operasional tambahan yang diterapkan oleh RNA.
3. **Kepala Ruangan** memilih dan menyetujui akomodasi tujuan pada **Sistem**.
4. **Perawat Ruangan** mengoordinasikan perpindahan pasien dan mencatat waktu pindah aktual.
5. **Sistem** mengaktifkan akomodasi tujuan sebagai lokasi klinis pasien dan menyimpan hubungan dengan alokasi asal.
6. **Kepala Ruangan** menetapkan alokasi asal untuk dilepas atau dipertahankan melalui SOP-RNA-A02.
7. Bila alokasi asal dilepas, **Perawat Ruangan** meneruskan kondisi bed asal ke SOP-RNA-A07.

## 5. Pengecualian Operasional

- Bila bed tujuan tidak memenuhi Mandatory Bed Assignability, **Kepala Ruangan** menunda atau membatalkan transfer; alokasi asal tetap aktif. RNA tidak menolak transfer karena gender, isolasi, peralatan, atau kebijakan operasional lokal.
- Bila perpindahan fisik terjadi tetapi pencatatan tujuan belum selesai, **Perawat Ruangan** segera merekonsiliasi lokasi aktual pada **Sistem**.
- Bila perpindahan mengubah RUANG RANAP, **Kepala Ruangan** menghentikan prosedur internal ini dan menggunakan Release-to-Waiting-List pada SOP-RNA-A05; tidak ada alur langsung RNA-to-RNA.

## 6. Kriteria Penyelesaian

- **Sistem** menampilkan akomodasi tujuan sebagai lokasi klinis aktif.
- Alokasi asal tampil sebagai dilepas atau retained dengan hasil yang jelas.
- Kondisi bed asal telah diteruskan bila alokasi asal dilepas.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — transfer internal dan kesiapan bed.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A02-Pengelolaan-Retained-Accommodation.md`.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A07-Pemulihan-Bed-Readiness.md`.
