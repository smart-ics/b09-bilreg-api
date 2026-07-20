# SOP-RNA-S02 — Pencatatan Tindakan Ad Hoc atau Independen

## 1. Tujuan

Mencatat tindakan RUANG RANAP yang dilakukan secara sah tanpa Clinical Order prospektif individual, dengan sumber, dasar kewenangan, Performer, waktu pelaksanaan, dan klasifikasi billable atau non-billable yang benar.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Performer RUANG RANAP | Manusia | Melakukan tindakan sesuai kewenangannya dan mencatat fakta pelaksanaan yang sebenarnya. |
| Kepala Ruangan | Manusia | Meninjau pengecualian, membantu memastikan dasar kewenangan tercatat, dan menyerahkan kebutuhan follow-up kepada Penanggung Jawab Klinis / Clinical Governance. |
| Penanggung Jawab Klinis / Clinical Governance | Manusia/Organisasi | Menerima kasus yang memerlukan peninjauan atau akuntabilitas lanjutan melalui prosedur governance yang berlaku. |
| Sistem RNA | Sistem | Menyimpan sumber tindakan, dasar kewenangan, fakta pelaksanaan, dan status publikasi finansial. |
| Tarif Context | Sistem | Menyediakan Service aktif yang eligible bagi Layanan Bangsal untuk tindakan billable. |
| Tata Rekening | Sistem | Menerima fakta pelaksanaan billable melalui prosedur publikasi terpisah dan menentukan konsekuensi finansial. |

## 3. Prasyarat

- Performer RUANG RANAP telah masuk ke aplikasi dan memiliki akses untuk mencatat pelaksanaan pada RUANG RANAP yang bersangkutan.
- Tindakan dilakukan tanpa Clinical Order prospektif individual dan dapat diklasifikasikan sebagai `Ad Hoc` atau `Independen`.
- Identitas pasien, registrasi atau care context, lokasi RUANG RANAP, dan waktu pelaksanaan tersedia.
- Dasar kewenangan tindakan dapat dicatat secara jujur.
- Bangsal memiliki Layanan yang dapat digunakan untuk memperoleh Service eligible ketika tindakan diklasifikasikan billable.

## 4. Langkah Operasional

1. **Performer RUANG RANAP** membuka pencatatan tindakan Ad Hoc atau Independen pada **Sistem RNA**.

2. **Performer RUANG RANAP** memilih sumber tindakan `Ad Hoc` atau `Independen` sesuai fakta yang terjadi.

3. **Performer RUANG RANAP** mencatat identitas pasien, registrasi atau care context, lokasi RUANG RANAP, dasar kewenangan, dan alasan tindakan.

4. **Performer RUANG RANAP** memastikan bahwa pencatatan tidak memakai `ClinicalOrderId` dan tidak dikaitkan sebagai Completion dari Clinical Order yang tidak pernah diterbitkan secara prospektif.

5. **Performer RUANG RANAP** memilih klasifikasi billable atau non-billable.

6. Untuk tindakan billable, **Performer RUANG RANAP** memilih Service aktif yang eligible dan **Tarif Context** menampilkan hasil validasi Service. Untuk tindakan non-billable, **Performer RUANG RANAP** mencatat description tanpa memilih Service.

7. **Performer RUANG RANAP** mencatat Performer aktual dan `OccurredAt` sebagai waktu tindakan benar-benar dilakukan.

8. Bila pencatatan dilakukan setelah waktu tindakan, **Performer RUANG RANAP** mencatat alasan keterlambatan secara benar.

9. **Sistem RNA** mencatat `RecordedAt` sebagai waktu pencatatan, membuat identitas fakta pelaksanaan, dan menampilkan ringkasan fakta kepada **Performer RUANG RANAP**.

10. **Performer RUANG RANAP** memeriksa kembali sumber, dasar kewenangan, klasifikasi, Service atau description, Performer, `OccurredAt`, dan `RecordedAt` yang ditampilkan oleh **Sistem RNA**.

11. Bila tindakan memerlukan peninjauan atau akuntabilitas lanjutan menurut kebijakan rumah sakit, **Kepala Ruangan** menyerahkan kasus beserta referensi fakta RNA kepada **Penanggung Jawab Klinis / Clinical Governance** melalui prosedur governance yang berlaku. Prosedur ini selesai pada penyerahan tersebut dan tidak menetapkan lifecycle follow-up governance.

12. Untuk fakta billable, **Sistem RNA** menampilkan bahwa fakta siap dilanjutkan kepada **Tata Rekening** melalui SOP-RNA-S04. Fakta non-billable tetap tersimpan di RNA tanpa publikasi finansial.

## 5. Pengecualian Operasional

- Bila sebelum tindakan dilakukan **Performer RUANG RANAP** tidak dapat menyatakan dasar kewenangan yang sah dan keadaan tidak darurat, **Performer RUANG RANAP** tidak melanjutkan tindakan dan meminta arahan kepada **Kepala Ruangan** atau **Penanggung Jawab Klinis / Clinical Governance**.
- Bila tindakan telah dilakukan dalam keadaan darurat, **Performer RUANG RANAP** mencatat dasar keadaan darurat dan kronologi sebenarnya. **Kepala Ruangan** menyerahkan kebutuhan follow-up kepada **Penanggung Jawab Klinis / Clinical Governance** tanpa membuat Clinical Order retrospektif.
- Bila **Performer RUANG RANAP** menemukan bahwa Clinical Order prospektif yang sesuai sebenarnya sudah `Active`, aktor tidak menggunakan prosedur S02. **Performer RUANG RANAP** menggunakan `ClinicalOrderId` tersebut dan mengikuti SOP-RNA-S01.
- Bila **Tarif Context** tidak tersedia, **Sistem RNA** menampilkan `ServiceDependencyUnavailable`. Bila tidak ada Service eligible, **Sistem RNA** menampilkan `NoEligibleService`. **Performer RUANG RANAP** tidak membuat Service lokal atau memilih Service yang tidak eligible; pencatatan non-billable tetap menggunakan description tanpa Service.
- Bila fakta yang sama telah tercatat, **Sistem RNA** menampilkan fakta yang sudah ada dan **Performer RUANG RANAP** tidak membuat fakta pelaksanaan kedua.
- Bila fakta yang tersimpan salah, **Performer RUANG RANAP** tidak menghapus atau menimpanya dan mengikuti SOP-RNA-S03.
- Bila publikasi finansial gagal, **Sistem RNA** tetap menampilkan fakta pelaksanaan yang telah tersimpan. **Performer RUANG RANAP** tidak mengulangi tindakan atau membuat fakta baru; pemulihan publikasi mengikuti SOP-RNA-S04.

## 6. Kriteria Penyelesaian

Prosedur selesai bila:

- **Sistem RNA** menampilkan identitas fakta pelaksanaan, sumber `Ad Hoc` atau `Independen`, dasar kewenangan, pasien, registrasi atau care context, lokasi, Performer, `OccurredAt`, dan `RecordedAt`.
- Untuk tindakan billable, **Sistem RNA** menampilkan Service eligible dan status bahwa fakta siap dipublikasikan melalui SOP-RNA-S04.
- Untuk tindakan non-billable, **Sistem RNA** menampilkan description tanpa Service dan tanpa publikasi finansial.
- Bila peninjauan lanjutan diperlukan, penyerahan referensi fakta RNA kepada **Penanggung Jawab Klinis / Clinical Governance** telah tercatat menurut prosedur governance yang berlaku.
- Tidak ada `ClinicalOrderId`, Clinical Order retrospektif, atau status Clinical Order yang dibuat dari tindakan tersebut.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — kepemilikan bukti pelaksanaan Ad Hoc/Independen oleh RNA.
- `docs/contexts/cpoe/CPOE-DOMAIN.md` — batas CPOE sebagai pemilik Clinical Order prospektif; tindakan dalam SOP ini tidak menjadi Clinical Order.
- `docs/contexts/tarif/tarif-01-context.md` — kepemilikan Service Definition dan Service eligible.
- `docs/contexts/TataRekening/01-context.md` — kepemilikan konsekuensi finansial.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md` — pelaksanaan yang memiliki Clinical Order aktif.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S03-Koreksi-RNA-Service-Execution.md` — koreksi fakta pelaksanaan RNA.
- `docs/contexts/bangsal/rna-sop/SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md` — publikasi fakta billable kepada Tata Rekening.
