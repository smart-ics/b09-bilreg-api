# SOP-RNA-A05 — Release ke Waiting List untuk Perpindahan Antar-RUANG RANAP

## 1. Tujuan

Melepas akomodasi pada RUANG RANAP asal dan memberitahu Admisi agar pasien kembali diproses melalui Admission Waiting List sebelum RUANG RANAP tujuan menetapkan akomodasi.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Perawat Ruangan asal | Manusia | Memeriksa kondisi aktual dan mencatat Release akomodasi asal. |
| Kepala Ruangan asal | Manusia | Menyetujui Release dan mengawasi pengecualian. |
| Sistem RNA | Sistem | Menyimpan Release, memulai readiness bed, dan mengirim fakta Release kepada Admisi. |
| Admisi | Bounded context | Memiliki Waiting List dan seluruh tanggung jawab operasional setelah Release, termasuk routing, cancellation, dan assignment ke tujuan. |
| RUANG RANAP tujuan | Manusia/Sistem | Meninjau Waiting List Admisi melalui SOP-RNA-A01 seperti pasien lainnya; tidak memiliki alur RNA-to-RNA khusus. |
| EMR | Bounded context | Memiliki konten klinis antar-ward. |

## 3. Prasyarat

- Pasien memiliki Clinical Accommodation aktif pada RUANG RANAP asal.
- Kebutuhan perpindahan antar-ward dan alasan Release telah tersedia.
- Perawat Ruangan atau Kepala Ruangan asal memiliki kewenangan Release.
- Identitas registrasi dan fakta minimum untuk notifikasi Admisi tersedia.

## 4. Langkah Operasional

1. **Perawat Ruangan asal** membuka Clinical Accommodation pasien dan memilih Release untuk perpindahan antar-ward.
2. **Kepala Ruangan asal** memeriksa alasan serta menyetujui Release.
3. **Perawat Ruangan asal** mencatat waktu bisnis Release pada **Sistem RNA**.
4. **Sistem RNA** melepaskan Clinical Accommodation asal, menyimpan riwayat dan audit, serta merekam kondisi readiness pascapelepasan.
5. Dalam transaksi lokal yang sama, **Sistem RNA** membentuk kewajiban notifikasi Release yang stabil kepada **Admisi**.
6. Setelah Release, **Admisi** mengambil kembali tanggung jawab operasional melalui Waiting List. RNA tidak membuat transfer queue atau keputusan tujuan.
7. **RUANG RANAP tujuan** meninjau Waiting List milik **Admisi** dan menjalankan SOP-RNA-A01 seperti pasien lainnya. Bila Ward menolak, tanggung jawab tetap pada Admisi; bila Bed Assignment berhasil, Admisi menutup Waiting List dan tanggung jawab berpindah ke RNA tujuan.
8. Konten klinis antar-ward dikelola pada **EMR**, di luar RNA.

## 5. Pengecualian Operasional

- Bila Release belum committed, RNA asal tetap menampilkan alokasi sebagai Active dan tidak mengirim fakta Release.
- Bila notifikasi Admisi gagal setelah Release committed, Release tidak dibatalkan. **Sistem RNA** menampilkan kewajiban integrasi untuk retry/reconciliation dengan identitas Release yang sama; RNA tidak mengambil kembali tanggung jawab transfer.
- Bila transfer dibatalkan, pasien kembali melalui proses Waiting List yang sama di **Admisi**; RNA tidak membuat alur cancellation-return terpisah.
- Tidak ada Accepted state, bed reservation, atau ownership RUANG RANAP tujuan sebelum Bed Assignment berhasil. Penolakan Ward hanya mengembalikan hasil review kepada Admisi dan tidak memindahkan tanggung jawab.
- `InterWardAccommodationReleased` membuat Waiting List baru secara idempoten atau memakai entry aktif yang cocok. `DestinationWardId` opsional bila tujuan belum ditetapkan secara otoritatif; Admisi tetap melakukan routing.
- Koreksi/Entered in Error setelah Waiting List terbentuk tidak menghapus atau membatalkan entry secara otomatis; kedua fakta dipertahankan sebagai `ReconciliationRequired`.

## 6. Kriteria Penyelesaian

- Clinical Accommodation asal berstatus Released dengan alasan, aktor, dan waktu bisnis yang jelas.
- Kondisi Bed asal telah masuk ke Bed Readiness History.
- Notifikasi Release kepada Admisi tercatat sebagai Pending, Acknowledged, atau Failed/Recoverable dengan stable identity.
- RNA tidak memiliki transfer queue, destination decision, atau operational responsibility setelah Release.
- Penempatan tujuan, bila terjadi, terlihat sebagai hasil pemrosesan Waiting List Admisi dan SOP-RNA-A01 yang terpisah.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — Inter-RUANG RANAP Release to Waiting List.
- `docs/contexts/admisi-ranap/admisi-ranap-domain.md` — Admission dan Waiting List ownership.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A01-Pemrosesan-Waiting-List-dan-Penetapan-Akomodasi.md`.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A06-Pelepasan-Akomodasi.md`.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-A07-Pemulihan-Bed-Readiness.md`.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-005 CLOSED.
