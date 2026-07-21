# SOP-RNA-S03 — Koreksi Fakta Pelaksanaan Layanan RUANG RANAP

## 1. Tujuan

Mengoreksi atau menginvalidasi RNA Service Execution Fact secara non-destruktif. Untuk fakta yang terkait `ClinicalOrderId`, prosedur ini juga memastikan CPOE memperbarui Fulfilment Outcome Validity tanpa membuka kembali atau mengganti disposisi `Completed` dari Clinical Order.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Aktor Care-Team yang Berwenang | Manusia | Melaporkan kesalahan, mengusulkan nilai yang benar, dan memberikan alasan koreksi. |
| Performer RUANG RANAP | Manusia | Memberikan klarifikasi mengenai aktivitas, pelaksana, dan waktu pelaksanaan bila diperlukan. |
| Kepala Ruangan Ward Pemilik | Manusia | Memeriksa dan memfinalisasi koreksi biasa yang tidak dilaporkan atau diusulkannya sendiri. |
| Reviewer Independen yang Diberi Wewenang Clinical Governance | Manusia | Melakukan second review untuk perubahan material, replacement, disputed case, atau Entered in Error. |
| Responsible Clinician | Manusia | Meninjau outcome CPOE yang `Invalidated` dan memutuskan apakah Clinical Order baru diperlukan. |
| Sistem RNA | Sistem | Mempertahankan fakta asli, mencatat koreksi secara append-only, dan menyampaikan fakta koreksi kepada konsumen. |
| CPOE | Sistem | Mengasosiasikan koreksi dengan `ClinicalOrderId`, mempertahankan status `Completed`, dan mengelola Fulfilment Outcome Validity serta Outcome Review. |
| Tata Rekening | Sistem | Menerima koreksi fakta pelaksanaan dan menentukan dampak finansial secara independen. |

## 3. Prasyarat

- RNA Service Execution Fact asli dan kesalahan yang diduga dapat diidentifikasi.
- Pelapor, finalizer, dan reviewer memiliki kewenangan yang sesuai; self-approval tidak diperbolehkan.
- Nilai yang benar, alasan koreksi, dan evidence untuk koreksi material tersedia sebelum finalisasi.
- Jika fakta terkait order, `ClinicalOrderId` tersedia dan Clinical Order telah `Completed` dengan referensi pada fakta pelaksanaan tersebut.
- Koreksi telah diklasifikasikan sebagai salah satu dari:
  - **Correction** — aktivitas tetap terjadi dan fakta asli perlu diperbaiki;
  - **Replacement** — aktivitas tetap terjadi, tetapi fakta otoritatif pengganti harus dibuat; atau
  - **Entered in Error** — fakta asli tidak sah dan tidak ada fakta pengganti yang saat ini mengonfirmasi aktivitas.

## 4. Langkah Operasional

1. **Aktor Care-Team yang Berwenang** melaporkan kesalahan dan membuka RNA Service Execution Fact asli pada **Sistem RNA**.
2. Pelapor mencatat nilai yang benar, jenis koreksi, structured reason, dan evidence yang diwajibkan.
3. **Performer RUANG RANAP** memberikan klarifikasi bila identitas aktivitas, Performer, atau waktu pelaksanaan dipertanyakan.
4. **Kepala Ruangan Ward Pemilik** memeriksa dan memfinalisasi Correction biasa jika tidak melakukan self-approval.
5. Perubahan Patient, Registration, Service, `ClinicalOrderId`, source-order, Replacement, Entered in Error, disputed case, atau perubahan material lain harus memperoleh persetujuan **Reviewer Independen yang Diberi Wewenang Clinical Governance**.
6. **Sistem RNA** mempertahankan fakta asli dan menambahkan correction fact yang memuat referensi fakta asli, jenis koreksi, nilai yang benar bila ada, alasan, aktor, reviewer bila diwajibkan, evidence, dan waktu bisnis.
7. Untuk Replacement, **Sistem RNA** membuat fakta pelaksanaan pengganti yang merujuk fakta asli. Untuk Entered in Error, sistem menandai fakta asli tidak valid tanpa menghapusnya.
8. Jika correction fact memiliki `ClinicalOrderId`, **Sistem RNA** menyampaikannya kepada **CPOE** sebagai fakta otoritatif dari Destination. Correction fact yang sama tidak boleh diasosiasikan lebih dari satu kali.
9. Untuk Correction atau Replacement yang tetap mengonfirmasi aktivitas telah terjadi, **CPOE**:
   - mempertahankan Clinical Order berstatus `Completed`;
   - mengubah Fulfilment Outcome Validity menjadi `Corrected`; dan
   - menunjuk correction atau replacement fact terbaru sebagai referensi bukti otoritatif terkini.
10. Untuk Entered in Error tanpa replacement otoritatif, **CPOE**:
    - mempertahankan Clinical Order berstatus `Completed`;
    - mengubah Fulfilment Outcome Validity menjadi `Invalidated`; dan
    - mewajibkan Outcome Review tanpa mengubah order menjadi `Active`, `Not Performed`, atau `Cancelled`.
11. **Responsible Clinician** menyelesaikan Outcome Review dengan mencatat salah satu keputusan beserta alasan dan waktu bisnis:
    - menerbitkan Clinical Order baru bila aktivitas masih diperlukan secara klinis; atau
    - menyatakan tidak diperlukan order lanjutan.
12. Jika fakta asli pernah disampaikan kepada **Tata Rekening**, **Sistem RNA** menyampaikan correction fact. **Tata Rekening** menentukan sendiri apakah koreksi finansial diperlukan.
13. **Sistem RNA** menampilkan status penerimaan correction fact oleh CPOE dan/atau Tata Rekening. Kegagalan pengiriman tetap terlihat sampai dipulihkan.

## 5. Pengecualian Operasional

- Jika nilai yang benar, kewenangan, independent review, atau evidence material belum memadai, finalisasi ditahan dan fakta asli tetap tersimpan.
- Koreksi yang disengketakan tidak boleh difinalisasi sebagai koreksi biasa oleh Kepala Ruangan; second review independen diwajibkan.
- Jika kesalahan berada pada isi Clinical Order, Service Definition Tarif, dokumentasi klinis, atau data finansial, koreksi dilakukan oleh konteks pemilik. RNA tidak mengubah fakta milik konteks tersebut.
- Jika `ClinicalOrderId` tidak ditemukan atau tidak menunjuk Clinical Order `Completed` yang sesuai, CPOE menolak asosiasi dan kasus diperiksa sebagai kesalahan korelasi; RNA tetap mempertahankan correction fact.
- Jika pengiriman correction fact gagal, fakta koreksi lokal tetap sah dan masuk pemulihan integrasi. Pengiriman ulang tidak boleh membuat koreksi ganda.
- Jika Responsible Clinician belum menyelesaikan Outcome Review, Clinical Order tetap `Completed` dengan Fulfilment Outcome Validity `Invalidated` dan penanda review wajib tetap terlihat.

## 6. Kriteria Penyelesaian

- Fakta asli dan correction, replacement, atau Entered in Error fact tampil sebagai satu rantai riwayat yang tidak terhapus.
- Correction fact memuat identitas fakta asli, jenis koreksi, alasan, aktor, waktu bisnis, serta reviewer dan evidence bila diwajibkan.
- Jika terkait `ClinicalOrderId`, CPOE tetap menampilkan Clinical Order sebagai `Completed` dan Fulfilment Outcome Validity sebagai `Corrected` atau `Invalidated` sesuai jenis koreksi.
- Untuk outcome `Corrected`, CPOE menunjuk bukti otoritatif terkini.
- Untuk outcome `Invalidated`, Outcome Review telah mencatat keputusan Responsible Clinician; sebelum keputusan tersebut tersedia, prosedur belum selesai dan pekerjaan klinis wajib tetap terlihat.
- Status penerimaan oleh CPOE dan/atau Tata Rekening dapat ditelusuri tanpa RNA menentukan dampak lifecycle order atau finansial di luar aturan di atas.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — kepemilikan dan koreksi non-destruktif RNA Service Execution Fact.
- `docs/contexts/cpoe/CPOE-DOMAIN.md` — finalitas Clinical Order, Fulfilment Outcome Correction, Fulfilment Outcome Validity, dan Outcome Review.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md` — Completion, Not Performed, dan Cancellation untuk pelaksanaan berbasis `ClinicalOrderId`.
- `docs/contexts/TataRekening/01-context.md` — pemisahan operational truth dan financial truth.
