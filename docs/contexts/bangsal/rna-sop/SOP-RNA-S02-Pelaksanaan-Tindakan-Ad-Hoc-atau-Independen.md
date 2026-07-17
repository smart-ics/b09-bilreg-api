# SOP-RNA-S02 — Pencatatan Pelaksanaan Tindakan Ad Hoc atau Independen

## 1. Tujuan

Mencatat secara benar tindakan RUANG RANAP yang telah dilakukan tanpa Clinical Order prospektif individual, menggunakan Service Tarif yang eligible bila billable atau description bila non-billable.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Performer RUANG RANAP | Manusia | Melakukan tindakan dalam kewenangannya dan mencatat fakta aktual. |
| Kepala Ruangan | Manusia | Mengawasi pengecualian dan memastikan follow-up diserahkan kepada accountable authorizer. |
| Dokter Penanggung Jawab Klinis/Attending | Manusia | Menjadi accountable authorizer; dokter on-call yang ditunjuk mengambil kewajiban bila attending tidak tersedia. |
| Dokter On-Call/Ketua Layanan dan Clinical Governance | Manusia/Organisasi | Menerima eskalasi, melakukan acknowledgement atas tugas, dan menyelesaikan atau merekonsiliasi follow-up sesuai kewenangan. |
| CPOE | Sistem | Mengelola assignment, acknowledgement, eskalasi, deadline, `Authorization Overdue`, dan late review untuk otorisasi lanjutan. |
| Tarif Context | Sistem | Menyediakan Service aktif yang eligible melalui `Bangsal → Layanan → AllowedLayanan` dan tetap memiliki Service Definition. |
| Tata Rekening | Sistem | Menerima fakta pelaksanaan dan menentukan seluruh konsekuensi billing. |
| Sistem RNA | Sistem | Menyimpan sumber, dasar kewenangan, fakta pelaksanaan, dan status pengiriman. |

## 3. Prasyarat

- Tindakan benar-benar dilakukan dalam kewenangan profesional atau dasar kewenangan yang dapat dinyatakan secara jujur.
- Bangsal memiliki `LayananId` otoritatif. Eksekusi billable dapat memilih Service aktif yang eligible dari Tarif; eksekusi non-billable akan memakai description tanpa Service.
- RNA tidak membuat Clinical Order retrospektif atau Service Definition lokal.

## 4. Langkah Operasional

1. **Performer RUANG RANAP** memilih sumber Ad Hoc atau Independen dan mencatat dasar kewenangannya pada **Sistem RNA**.
2. **Performer RUANG RANAP** memilih klasifikasi billable atau non-billable. Untuk billable, Performer memilih Service aktif yang eligible dari **Tarif Context** dan Sistem RNA memvalidasinya ulang saat simpan. Untuk non-billable, Performer mengisi description tanpa Service.
3. Setelah tindakan dilakukan, **Performer RUANG RANAP** mencatat Performer aktual dan **Performed At** sebagai `OccurredAt`.
4. **Sistem RNA** mencatat `RecordedAt` sebagai waktu persistence sistem, identitas fakta, dan kronologi late entry bila waktu pencatatan berbeda. Bila waktu tindakan tidak diketahui, sistem menetapkan `OccurredAt = RecordedAt`.
5. Bila exceptional-order accountability diperlukan, **Sistem RNA** menyerahkan `ExceptionalExecutionRecorded` beserta `OccurredAt`, kronologi, dan authority basis kepada **CPOE** tanpa membuat order prospektif palsu.
6. **CPOE** menetapkan deadline paling lambat **24 jam sejak `OccurredAt` atau sebelum discharge, mana yang lebih dahulu**, dan menugaskan dokter penanggung jawab klinis/attending; bila tidak tersedia, kewajiban berpindah ke dokter on-call yang ditunjuk.
7. Bila belum diselesaikan, **CPOE** mengeskalasi accountable authorizer → dokter on-call/ketua layanan → Clinical Governance. Penerima eskalasi melakukan acknowledgement penerimaan tugas; acknowledgement bukan keputusan authorization.
8. Bila deadline terlewati, **CPOE** menetapkan status terminal `Authorization Overdue` dan mempertahankan kewajiban rekonsiliasi. Review setelah deadline dicatat sebagai late review, bukan authorization prospektif atau tepat waktu.
9. **Sistem RNA** mempublikasikan Service Execution Fact kepada **Tata Rekening** tanpa keputusan eligibility atau data finansial.

## 5. Pengecualian Operasional

- Bila dasar kewenangan tidak tersedia dan bukan keadaan darurat, **Performer RUANG RANAP** tidak melanjutkan tindakan dan meminta arahan melalui konteks pemilik otorisasi.
- Dalam keadaan darurat, dasar darurat dan kronologi dicatat; **CPOE** atau pemilik governance tetap mengelola akuntabilitas lanjutan.
- Status `Authorization Overdue`, acknowledgement, atau late review tidak menghapus, mengubah, atau membatalkan fakta pelaksanaan RNA.
- Bila **Tarif Context** tidak tersedia, Sistem RNA menampilkan `ServiceDependencyUnavailable`; bila query berhasil tetapi tidak ada Service eligible, sistem menampilkan `NoEligibleService`. Keduanya menahan simpan billable dan tidak boleh dipaksakan ke Service lokal; catatan non-billable dengan description tetap dapat disimpan.
- Bila catatan salah atau duplikat, gunakan SOP-RNA-S03.
- Kegagalan pengiriman tidak menghapus fakta pelaksanaan yang sudah benar.

## 6. Kriteria Penyelesaian

- **Sistem RNA** menampilkan sumber, dasar kewenangan, klasifikasi, identitas Service bila billable atau description bila non-billable, Performer, `OccurredAt`/Performed At, `RecordedAt`, dan identitas fakta; urutan bisnis menggunakan `OccurredAt`.
- Status pengiriman ke **CPOE** bila berlaku dan ke **Tata Rekening** terlihat.
- Untuk subsequent authorization, status assignment, acknowledgement, authorization, escalation, `Authorization Overdue`, dan late review dari CPOE terlihat tanpa direpresentasikan sebagai keputusan RNA.
- RNA tidak menetapkan outcome catalogue, completion criterion, Charge Eligibility, atau konsekuensi finansial.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — Ad Hoc/Independent execution dan fakta pelaksanaan.
- `docs/contexts/tarif/tarif-01-context.md` — kepemilikan Service Definition.
- `docs/contexts/cpoe/CPOE-DOMAIN.md` — exceptional-order accountability.
- `docs/contexts/TataRekening/01-context.md` — financial authority.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-010 CLOSED serta dependensi integrasi.
