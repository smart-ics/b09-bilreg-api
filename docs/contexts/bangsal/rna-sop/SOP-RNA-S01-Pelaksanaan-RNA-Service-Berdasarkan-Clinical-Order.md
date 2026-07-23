# SOP-RNA-S01 — Penyelesaian Order Occurrence oleh RUANG RANAP

## 1. Tujuan

Menangani satu Order Occurrence aktif yang ditujukan kepada RUANG RANAP sampai occurrence tercatat `Completed` atau `Not Performed`, serta menangani pekerjaan RNA yang dibatalkan oleh CPOE, dengan `OrderOccurrenceId` sebagai identitas pelaksanaan dan `ClinicalOrderId` sebagai korelasi induk.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Receiver RUANG RANAP | Manusia | Meninjau Order Occurrence aktif untuk RUANG RANAP, memeriksa informasi pekerjaan, dan mengoordinasikan pekerjaan kepada Performer. |
| Performer RUANG RANAP | Manusia | Memeriksa Order Occurrence, melaksanakan aktivitas sesuai kewenangan, mencatat bukti pelaksanaan RNA, dan mencatat Completion. |
| PPA Pemesan | Manusia | Memberikan penjelasan operasional atas instruksi yang belum cukup jelas dan memastikan order dibatalkan serta diganti bila informasi inti order salah. |
| Cancelling Actor | Manusia | Membatalkan Clinical Order aktif melalui CPOE sesuai kewenangan dan mencatat alasan pembatalan. |
| Penanggung Jawab Klinis / Clinical Governance | Manusia/Organisasi | Menangani rekonsiliasi ketika aktivitas telah terjadi tetapi Clinical Order sudah memiliki disposisi final yang bertentangan. |
| Sistem RNA | Sistem | Menampilkan pekerjaan aktif per `OrderOccurrenceId`, menyimpan bukti pelaksanaan, dan menampilkan hasil setiap occurrence. |
| CPOE | Sistem | Menyediakan Order Occurrence aktif beserta `ClinicalOrderId` induknya, mencatat Fulfilment Evidence per occurrence, dan memberitahukan pembatalan pekerjaan yang masih aktif. |
| Tarif Context | Sistem | Menyediakan Service aktif yang eligible bagi Layanan Bangsal ketika bukti pelaksanaan RNA diklasifikasikan billable. |
| Tata Rekening | Sistem | Menerima fakta pelaksanaan billable melalui prosedur publikasi yang terpisah dan menentukan konsekuensi finansial. |

## 3. Prasyarat

- Receiver RUANG RANAP atau Performer RUANG RANAP telah masuk ke aplikasi dan memiliki akses ke Destination RUANG RANAP yang bersangkutan.
- Pekerjaan memiliki `OrderOccurrenceId`, `ClinicalOrderId` induk, berstatus `Active`, dan ditujukan kepada Destination RUANG RANAP yang bersangkutan.
- Pekerjaan menampilkan pasien, care context, Order Item, Clinical Indication, Priority, Order Instruction, dan Planned Execution Time yang diperlukan.
- Clinical Order diperlakukan sebagai instruksi klinis, bukan sebagai bukti bahwa aktivitas telah dilaksanakan.

## 4. Langkah Operasional

### 4.1 Meninjau pekerjaan aktif

1. **Receiver RUANG RANAP** membuka daftar pekerjaan Order Occurrence untuk Destination RUANG RANAP pada **Sistem RNA**.

2. **Sistem RNA** menampilkan setiap Order Occurrence berstatus `Active` yang menjadi tanggung jawab Destination tersebut sebagai pekerjaan tersendiri.

3. **Receiver RUANG RANAP** membuka satu pekerjaan dan memeriksa `OrderOccurrenceId`, `ClinicalOrderId` induk, Planned Execution Time, identitas pasien, care context, Order Item, Clinical Indication, Priority, dan Order Instruction.

4. **Receiver RUANG RANAP** memastikan bahwa pasien, aktivitas, dan Destination sesuai sebelum pekerjaan dilaksanakan.

5. **Receiver RUANG RANAP** mengoordinasikan pekerjaan kepada **Performer RUANG RANAP** yang sesuai. Penetapan atau pengambilan pekerjaan belum merupakan bukti pelaksanaan dan tidak mengubah status Clinical Order.

### 4.2 Mencatat Completion

6. **Performer RUANG RANAP** membuka pekerjaan menggunakan `OrderOccurrenceId` dan memastikan occurrence masih berstatus `Active` sebelum pelaksanaan.

7. **Performer RUANG RANAP** melaksanakan aktivitas yang diminta sesuai Order Instruction dan kewenangan profesionalnya.

8. **Performer RUANG RANAP** mencatat bukti pelaksanaan pada **Sistem RNA** dengan `OrderOccurrenceId`, `ClinicalOrderId` induk, Performer aktual, waktu pelaksanaan aktual, serta informasi Service atau description yang diwajibkan oleh prosedur RNA.

9. Untuk pelaksanaan billable, **Performer RUANG RANAP** memilih Service aktif yang eligible dan **Tarif Context** mengembalikan hasil validasi Service. Untuk pelaksanaan non-billable, **Performer RUANG RANAP** mencatat description tanpa memilih Service.

10. **Sistem RNA** menyimpan bukti pelaksanaan sebagai catatan otoritatif milik RNA dan menampilkan identitas catatan pelaksanaan kepada **Performer RUANG RANAP**.

11. **Performer RUANG RANAP** mencatat Completion untuk `OrderOccurrenceId` yang sama pada **CPOE**, dengan Performer aktual, waktu pelaksanaan aktual, dan referensi bukti pelaksanaan RNA.

12. **CPOE** memeriksa bahwa Order Occurrence masih `Active` dan bahwa Performer berasal dari Destination yang bertanggung jawab, lalu menampilkan occurrence sebagai `Completed` dan memperbarui Completion Progress pada Clinical Order induk.

13. **Sistem RNA** menampilkan Order Occurrence sebagai `Completed` dan hanya mengeluarkan occurrence tersebut dari daftar pekerjaan aktif. Order Occurrence lain pada `ClinicalOrderId` yang sama tetap tampil bila masih `Active`. Bila pelaksanaan diklasifikasikan billable, publikasi ke Tata Rekening dilanjutkan melalui SOP-RNA-S04.

### 4.3 Mencatat Not Performed

14. Bila aktivitas tidak dilaksanakan, **Receiver RUANG RANAP** atau **Performer RUANG RANAP** memilih hasil `Not Performed` untuk `OrderOccurrenceId` yang masih `Active` dan mencatat alasan yang dapat dipertanggungjawabkan.

15. **CPOE** memeriksa bahwa aktor berasal dari Destination yang bertanggung jawab, mencatat Fulfilment Evidence beserta alasan, menampilkan occurrence sebagai `Not Performed`, dan memperbarui Completion Progress pada Clinical Order induk.

16. **Sistem RNA** menampilkan Order Occurrence sebagai `Not Performed` dan hanya mengeluarkan occurrence tersebut dari daftar pekerjaan aktif. Order Occurrence lain pada `ClinicalOrderId` yang sama tetap tampil bila masih `Active`. **Sistem RNA** tidak membuat Service Execution Fact atau publikasi billable karena aktivitas tidak dilaksanakan.

### 4.4 Menangani Cancellation

17. Bila Clinical Order tidak lagi valid atau diperlukan dan belum memiliki Fulfilment Outcome, **Cancelling Actor** membatalkan `ClinicalOrderId` melalui **CPOE** dan mencatat alasan pembatalan.

18. **CPOE** mencatat aktor, waktu, dan alasan, menetapkan Clinical Order sebagai `Cancelled`, lalu mengirimkan daftar `OrderOccurrenceId` yang berubah dari `Active` menjadi `Cancelled` kepada **Sistem RNA**.

19. **Sistem RNA** mencocokkan daftar `OrderOccurrenceId` dari **CPOE**, menampilkan setiap pekerjaan RNA yang tercantum sebagai source-cancelled, dan mengeluarkannya dari daftar pekerjaan aktif. **Receiver RUANG RANAP** dan **Performer RUANG RANAP** tidak membuat Fulfilment Evidence atau Service Execution Fact untuk pekerjaan tersebut. Occurrence yang telah `Completed` atau `Not Performed` tetap ditampilkan dengan hasil sebelumnya.

20. Bila aktivitas masih diperlukan dengan pasien, care context, Order Item, atau Destination yang berbeda, **Cancelling Actor** memastikan bahwa Clinical Order baru diterbitkan melalui prosedur CPOE yang berlaku. Order yang telah `Cancelled` tidak diubah atau diaktifkan kembali.

## 5. Pengecualian Operasional

- Bila Order Occurrence tidak ditemukan, bukan milik Destination RUANG RANAP, atau tidak lagi `Active`, **Sistem RNA** menahan pencatatan outcome dan **Receiver RUANG RANAP** memeriksa kembali `OrderOccurrenceId` beserta `ClinicalOrderId` induknya. Aktor tidak membuat pekerjaan pengganti dengan identitas baru secara manual.
- Bila pasien, care context, Order Item, atau Destination pada order tidak benar, **Receiver RUANG RANAP** tidak mengubah order. **Receiver RUANG RANAP** menghubungi **Cancelling Actor** agar order dibatalkan; bila aktivitas masih diperlukan, Clinical Order baru harus diterbitkan.
- Bila Order Instruction tidak cukup jelas untuk pelaksanaan yang aman, **Receiver RUANG RANAP** menahan pelaksanaan dan menghubungi **PPA Pemesan**. Hasil akhirnya harus tetap dicatat sebagai Completion, Not Performed dengan alasan, atau Cancellation; tidak ada status klarifikasi terpisah pada CPOE.
- Bila Service billable tidak dapat divalidasi oleh **Tarif Context**, **Sistem RNA** menahan pencatatan bukti pelaksanaan billable dan menampilkan masalah dependensi kepada **Performer RUANG RANAP**. Aktor tidak membuat Service lokal atau memilih Service yang tidak eligible.
- Bila bukti pelaksanaan telah tersimpan tetapi Completion belum berhasil dicatat pada **CPOE**, **Sistem RNA** tetap mempertahankan bukti pelaksanaan dan menampilkan bahwa outcome CPOE belum tercatat. **Performer RUANG RANAP** tidak mengulangi pelaksanaan dan mengulangi pencatatan Completion untuk `OrderOccurrenceId`, `ClinicalOrderId`, serta bukti pelaksanaan yang sama setelah CPOE tersedia.
- Bila Clinical Order menjadi `Cancelled` setelah aktivitas terlanjur dilakukan tetapi sebelum Completion tercatat, **Sistem RNA** mempertahankan bukti pelaksanaan. **Performer RUANG RANAP** tidak mengubah status final CPOE atau mengulangi aktivitas dan menyerahkan kasus kepada **Penanggung Jawab Klinis / Clinical Governance** untuk rekonsiliasi.
- Bila pencatatan Completion, Not Performed, atau Cancellation diulang dengan fakta yang sama, **CPOE** menampilkan disposisi yang telah tercatat tanpa membuat outcome kedua. Bila fakta berbeda atau status telah final, **CPOE** menolak perubahan dan menampilkan konflik kepada aktor.
- Bila bukti pelaksanaan RNA keliru, **Performer RUANG RANAP** tidak menghapusnya dan mengikuti SOP-RNA-S03. Dampak koreksi terhadap Clinical Order yang telah final harus mengikuti kebijakan CPOE yang telah disetujui; aktor tidak mengubah outcome final secara manual.

## 6. Kriteria Penyelesaian

Prosedur selesai bila salah satu hasil berikut terlihat:

- `Completed`: CPOE menampilkan `OrderOccurrenceId`, `ClinicalOrderId` induk, Performer, waktu pelaksanaan, dan referensi bukti pelaksanaan RNA; Sistem RNA menampilkan bukti pelaksanaan yang sama.
- `Not Performed`: CPOE menampilkan `OrderOccurrenceId`, `ClinicalOrderId` induk, aktor, waktu, dan alasan; tidak ada Service Execution Fact atau publikasi billable yang dibuat.
- `Cancelled`: CPOE menampilkan `ClinicalOrderId`, Cancelling Actor, waktu, dan alasan; tidak ada Fulfilment Outcome atau bukti pelaksanaan baru yang dibuat.

Hasil menurut jenis Clinical Order:

- **Single-Occurrence Order:** setelah occurrence menjadi `Completed` atau `Not Performed`, CPOE menampilkan Clinical Order induk dengan status final yang sesuai dan tidak ada occurrence aktif yang tersisa.
- **Scheduled Order:** setelah occurrence terpilih menjadi `Completed` atau `Not Performed`, CPOE dapat tetap menampilkan Clinical Order induk sebagai `Active`; **Sistem RNA** tetap menampilkan setiap occurrence lain yang masih `Active` sebagai pekerjaan tersendiri.
- **Order Cancellation:** **Sistem RNA** menampilkan source-cancelled hanya untuk `OrderOccurrenceId` yang tercantum dalam pemberitahuan pembatalan CPOE; occurrence yang sudah final mempertahankan hasil sebelumnya.

Untuk semua hasil final:

- Order Occurrence yang final tidak lagi tampil pada daftar pekerjaan aktif RUANG RANAP; occurrence lain dari Clinical Order yang sama tetap tampil bila masih `Active`.
- Riwayat order dan alasan yang diwajibkan tetap dapat ditinjau.
- Order Occurrence yang final tidak diubah, diaktifkan kembali, atau diberi outcome kedua.

## 7. Referensi

- `docs/contexts/cpoe/CPOE-DOMAIN.md` — business truth Clinical Order, Order Occurrence, Destination Worklist, fulfilment, cancellation, dan Completion Progress.
- `docs/contexts/cpoe/CPOE-ARCHITECTURE.md` — realisasi teknis use case, authorization, transaction, concurrency, dan idempotency.
- `docs/contexts/bangsal/RNA-DOMAIN.md` — bukti pelaksanaan RUANG RANAP dan batas kepemilikan RNA.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S03-Koreksi-RNA-Service-Execution.md` — koreksi bukti pelaksanaan RNA.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md` — publikasi fakta pelaksanaan billable kepada Tata Rekening.
