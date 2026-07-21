# SOP-RNA-S01 — Penyelesaian Clinical Order oleh RUANG RANAP

## 1. Tujuan

Menangani satu Clinical Order aktif yang ditujukan kepada RUANG RANAP sampai order berakhir sebagai `Completed`, `Not Performed`, atau `Cancelled`, dengan `ClinicalOrderId` sebagai identitas order dan catatan pelaksanaan RNA tetap menjadi bukti pelaksanaan yang otoritatif.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Receiver RUANG RANAP | Manusia | Meninjau Clinical Order aktif untuk RUANG RANAP, memeriksa informasi order, dan mengoordinasikan pekerjaan kepada Performer. |
| Performer RUANG RANAP | Manusia | Memeriksa Clinical Order, melaksanakan aktivitas sesuai kewenangan, mencatat bukti pelaksanaan RNA, dan mencatat Completion. |
| PPA Pemesan | Manusia | Memberikan penjelasan operasional atas instruksi yang belum cukup jelas dan memastikan order dibatalkan serta diganti bila informasi inti order salah. |
| Cancelling Actor | Manusia | Membatalkan Clinical Order aktif melalui CPOE sesuai kewenangan dan mencatat alasan pembatalan. |
| Penanggung Jawab Klinis / Clinical Governance | Manusia/Organisasi | Menangani rekonsiliasi ketika aktivitas telah terjadi tetapi Clinical Order sudah memiliki disposisi final yang bertentangan. |
| Sistem RNA | Sistem | Menampilkan pekerjaan aktif RUANG RANAP, menyimpan bukti pelaksanaan, dan menampilkan hasil akhir Clinical Order. |
| CPOE | Sistem | Menyediakan Clinical Order aktif berdasarkan `ClinicalOrderId` dan mencatat `Completed`, `Not Performed`, atau `Cancelled` sebagai disposisi final. |
| Tarif Context | Sistem | Menyediakan Service aktif yang eligible bagi Layanan Bangsal ketika bukti pelaksanaan RNA diklasifikasikan billable. |
| Tata Rekening | Sistem | Menerima fakta pelaksanaan billable melalui prosedur publikasi yang terpisah dan menentukan konsekuensi finansial. |

## 3. Prasyarat

- Receiver RUANG RANAP atau Performer RUANG RANAP telah masuk ke aplikasi dan memiliki akses ke Destination RUANG RANAP yang bersangkutan.
- Clinical Order memiliki `ClinicalOrderId`, berstatus `Active`, dan ditujukan kepada Destination RUANG RANAP yang bersangkutan.
- Clinical Order menampilkan pasien, care context, Order Item, Clinical Indication, Priority, dan Order Instruction yang diperlukan.
- Satu `ClinicalOrderId` merepresentasikan satu aktivitas klinis yang diminta.
- Clinical Order diperlakukan sebagai instruksi klinis, bukan sebagai bukti bahwa aktivitas telah dilaksanakan.

## 4. Langkah Operasional

### 4.1 Meninjau pekerjaan aktif

1. **Receiver RUANG RANAP** membuka daftar pekerjaan Clinical Order untuk Destination RUANG RANAP pada **Sistem RNA**.

2. **Sistem RNA** menampilkan hanya Clinical Order berstatus `Active` yang menjadi tanggung jawab Destination tersebut.

3. **Receiver RUANG RANAP** membuka satu pekerjaan dan memeriksa `ClinicalOrderId`, identitas pasien, care context, Order Item, Clinical Indication, Priority, dan Order Instruction.

4. **Receiver RUANG RANAP** memastikan bahwa pasien, aktivitas, dan Destination sesuai sebelum pekerjaan dilaksanakan.

5. **Receiver RUANG RANAP** mengoordinasikan pekerjaan kepada **Performer RUANG RANAP** yang sesuai. Penetapan atau pengambilan pekerjaan belum merupakan bukti pelaksanaan dan tidak mengubah status Clinical Order.

### 4.2 Mencatat Completion

6. **Performer RUANG RANAP** membuka pekerjaan menggunakan `ClinicalOrderId` dan memastikan Clinical Order masih berstatus `Active` sebelum pelaksanaan.

7. **Performer RUANG RANAP** melaksanakan aktivitas yang diminta sesuai Order Instruction dan kewenangan profesionalnya.

8. **Performer RUANG RANAP** mencatat bukti pelaksanaan pada **Sistem RNA** dengan `ClinicalOrderId`, Performer aktual, waktu pelaksanaan aktual, serta informasi Service atau description yang diwajibkan oleh prosedur RNA.

9. Untuk pelaksanaan billable, **Performer RUANG RANAP** memilih Service aktif yang eligible dan **Tarif Context** mengembalikan hasil validasi Service. Untuk pelaksanaan non-billable, **Performer RUANG RANAP** mencatat description tanpa memilih Service.

10. **Sistem RNA** menyimpan bukti pelaksanaan sebagai catatan otoritatif milik RNA dan menampilkan identitas catatan pelaksanaan kepada **Performer RUANG RANAP**.

11. **Performer RUANG RANAP** mencatat Completion untuk `ClinicalOrderId` yang sama pada **CPOE**, dengan Performer aktual, waktu pelaksanaan aktual, dan referensi bukti pelaksanaan RNA.

12. **CPOE** memeriksa bahwa Clinical Order masih `Active` dan bahwa Performer berasal dari Destination yang bertanggung jawab, lalu mencatat disposisi `Completed`.

13. **Sistem RNA** menampilkan Clinical Order sebagai `Completed` dan mengeluarkannya dari daftar pekerjaan aktif. Bila pelaksanaan diklasifikasikan billable, publikasi ke Tata Rekening dilanjutkan melalui SOP-RNA-S04.

### 4.3 Mencatat Not Performed

14. Bila aktivitas tidak dilaksanakan, **Receiver RUANG RANAP** atau **Performer RUANG RANAP** memilih hasil `Not Performed` untuk `ClinicalOrderId` yang masih `Active` dan mencatat alasan yang dapat dipertanggungjawabkan.

15. **CPOE** memeriksa bahwa aktor berasal dari Destination yang bertanggung jawab, mencatat alasan, dan menetapkan Clinical Order sebagai `Not Performed`.

16. **Sistem RNA** menampilkan Clinical Order sebagai `Not Performed` dan mengeluarkannya dari daftar pekerjaan aktif. **Sistem RNA** tidak membuat bukti pelaksanaan atau publikasi billable karena aktivitas tidak dilaksanakan.

### 4.4 Menangani Cancellation

17. Bila Clinical Order tidak lagi valid atau diperlukan dan belum memiliki Fulfilment Outcome, **Cancelling Actor** membatalkan `ClinicalOrderId` melalui **CPOE** dan mencatat alasan pembatalan.

18. **CPOE** mencatat aktor, waktu, dan alasan, lalu menetapkan Clinical Order sebagai `Cancelled`.

19. **Sistem RNA** menampilkan Clinical Order sebagai `Cancelled` dan mengeluarkannya dari daftar pekerjaan aktif. **Receiver RUANG RANAP** dan **Performer RUANG RANAP** tidak melaksanakan aktivitas dari order tersebut.

20. Bila aktivitas masih diperlukan dengan pasien, care context, Order Item, atau Destination yang berbeda, **Cancelling Actor** memastikan bahwa Clinical Order baru diterbitkan melalui prosedur CPOE yang berlaku. Order yang telah `Cancelled` tidak diubah atau diaktifkan kembali.

## 5. Pengecualian Operasional

- Bila Clinical Order tidak ditemukan, bukan milik Destination RUANG RANAP, atau tidak lagi `Active`, **Sistem RNA** menahan pencatatan outcome dan **Receiver RUANG RANAP** memeriksa kembali `ClinicalOrderId`. Aktor tidak membuat pekerjaan pengganti dengan identitas baru secara manual.
- Bila pasien, care context, Order Item, atau Destination pada order tidak benar, **Receiver RUANG RANAP** tidak mengubah order. **Receiver RUANG RANAP** menghubungi **Cancelling Actor** agar order dibatalkan; bila aktivitas masih diperlukan, Clinical Order baru harus diterbitkan.
- Bila Order Instruction tidak cukup jelas untuk pelaksanaan yang aman, **Receiver RUANG RANAP** menahan pelaksanaan dan menghubungi **PPA Pemesan**. Hasil akhirnya harus tetap dicatat sebagai Completion, Not Performed dengan alasan, atau Cancellation; tidak ada status klarifikasi terpisah pada CPOE.
- Bila Service billable tidak dapat divalidasi oleh **Tarif Context**, **Sistem RNA** menahan pencatatan bukti pelaksanaan billable dan menampilkan masalah dependensi kepada **Performer RUANG RANAP**. Aktor tidak membuat Service lokal atau memilih Service yang tidak eligible.
- Bila bukti pelaksanaan telah tersimpan tetapi Completion belum berhasil dicatat pada **CPOE**, **Sistem RNA** tetap mempertahankan bukti pelaksanaan dan menampilkan bahwa outcome CPOE belum tercatat. **Performer RUANG RANAP** tidak mengulangi pelaksanaan dan mengulangi pencatatan Completion untuk `ClinicalOrderId` serta bukti pelaksanaan yang sama setelah CPOE tersedia.
- Bila Clinical Order menjadi `Cancelled` setelah aktivitas terlanjur dilakukan tetapi sebelum Completion tercatat, **Sistem RNA** mempertahankan bukti pelaksanaan. **Performer RUANG RANAP** tidak mengubah status final CPOE atau mengulangi aktivitas dan menyerahkan kasus kepada **Penanggung Jawab Klinis / Clinical Governance** untuk rekonsiliasi.
- Bila pencatatan Completion, Not Performed, atau Cancellation diulang dengan fakta yang sama, **CPOE** menampilkan disposisi yang telah tercatat tanpa membuat outcome kedua. Bila fakta berbeda atau status telah final, **CPOE** menolak perubahan dan menampilkan konflik kepada aktor.
- Bila bukti pelaksanaan RNA keliru, **Performer RUANG RANAP** tidak menghapusnya dan mengikuti SOP-RNA-S03. Dampak koreksi terhadap Clinical Order yang telah final harus mengikuti kebijakan CPOE yang telah disetujui; aktor tidak mengubah outcome final secara manual.

## 6. Kriteria Penyelesaian

Prosedur selesai bila salah satu hasil berikut terlihat:

- `Completed`: CPOE menampilkan `ClinicalOrderId`, Performer, waktu pelaksanaan, dan referensi bukti pelaksanaan RNA; Sistem RNA menampilkan bukti pelaksanaan yang sama.
- `Not Performed`: CPOE menampilkan `ClinicalOrderId`, aktor, waktu, dan alasan; tidak ada bukti pelaksanaan atau publikasi billable yang dibuat.
- `Cancelled`: CPOE menampilkan `ClinicalOrderId`, Cancelling Actor, waktu, dan alasan; tidak ada Fulfilment Outcome atau bukti pelaksanaan baru yang dibuat.

Untuk semua hasil final:

- Clinical Order tidak lagi tampil pada daftar pekerjaan aktif RUANG RANAP.
- Riwayat order dan alasan yang diwajibkan tetap dapat ditinjau.
- Clinical Order yang final tidak diubah, diaktifkan kembali, atau diberi outcome kedua.

## 7. Referensi

- `docs/contexts/cpoe/CPOE-DOMAIN.md` — business truth Clinical Order, `ClinicalOrderId`, lifecycle, final disposition, dan batas execution/financial.
- `docs/contexts/cpoe/CPOE-ARCHITECTURE.md` — realisasi teknis use case, authorization, transaction, concurrency, dan idempotency.
- `docs/contexts/bangsal/RNA-DOMAIN.md` — bukti pelaksanaan RUANG RANAP dan batas kepemilikan RNA.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S03-Koreksi-RNA-Service-Execution.md` — koreksi bukti pelaksanaan RNA.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md` — publikasi fakta pelaksanaan billable kepada Tata Rekening.
