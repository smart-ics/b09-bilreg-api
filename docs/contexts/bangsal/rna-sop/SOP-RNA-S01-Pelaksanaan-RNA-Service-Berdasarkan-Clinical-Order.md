# SOP-RNA-S01 — Pencatatan Pelaksanaan Layanan RUANG RANAP Berdasarkan Clinical Order

## 1. Tujuan

Menerima pekerjaan dari Clinical Order, mencatat pelaksanaan billable dengan Service eligible milik Tarif Context atau pelaksanaan non-billable dengan description, dan mempublikasikan hanya fakta billable tanpa membuat definisi layanan atau keputusan finansial.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| PPA Pemesan | Manusia | Menangani intent dan perubahan Clinical Order melalui CPOE. |
| Kepala Ruangan | Manusia | Menugaskan Performer secara opsional dan menangani hambatan pekerjaan. |
| Performer RUANG RANAP | Manusia | Melaksanakan layanan dan mencatat Service eligible bila billable atau description bila non-billable, bersama Performer serta waktu aktual secara benar. |
| Tarif Context | Sistem | Menyediakan daftar Service aktif yang eligible melalui `Bangsal → Layanan → AllowedLayanan`; tetap memiliki Service Definition dan konfigurasi layanan. |
| CPOE | Sistem | Menyediakan order/occurrence dan menerima fakta pelaksanaan untuk koordinasi order. |
| Tata Rekening | Sistem | Menerima fakta pelaksanaan dan secara mandiri menentukan seluruh konsekuensi billing. |
| Sistem RNA | Sistem | Menerima pekerjaan, menyimpan fakta pelaksanaan, dan mengelola status pengiriman. |

## 3. Prasyarat

- Clinical Order telah diarahkan kepada RUANG RANAP dan memiliki source/occurrence identity yang stabil.
- Bangsal memiliki `LayananId` otoritatif. Untuk eksekusi billable, Tarif dapat mengembalikan sedikitnya satu Service aktif yang eligible untuk Layanan tersebut; eksekusi non-billable akan memakai description tanpa Service.
- RNA tidak menafsirkan order sebagai bukti bahwa layanan telah dilaksanakan.

## 4. Langkah Operasional

1. **Sistem RNA** menerima pekerjaan dari **CPOE** secara idempoten dan menampilkannya pada daftar pekerjaan.
2. **Performer RUANG RANAP** memeriksa pasien, order/occurrence, dan identitas Service yang dirujuk.
3. **Kepala Ruangan** dapat menetapkan Performer bila koordinasi pekerjaan memerlukannya; assignment tidak membuktikan pelaksanaan.
4. Setelah layanan benar-benar dilakukan, **Performer RUANG RANAP** memilih klasifikasi billable/non-billable dan mencatat Service eligible atau description yang sesuai, Performer aktual, dan **Performed At** sebagai `OccurredAt` pada **Sistem RNA**.
5. **Sistem RNA** mencatat **RecordedAt** sebagai waktu persistence sistem, sumber/occurrence, dan identitas fakta secara otomatis. Bila waktu pelaksanaan tidak diketahui, sistem menetapkan `OccurredAt = RecordedAt`.
6. **Sistem RNA** mempublikasikan fakta “Service X dilaksanakan oleh Performer Y pada waktu Z” kepada **CPOE** dan **Tata Rekening** sesuai kontrak integrasi.
7. **Sistem RNA** menampilkan status pengiriman per tujuan tanpa menyimpulkan bahwa order selesai atau bahwa transaksi billing terbentuk.

## 5. Pengecualian Operasional

- Bila order tidak jelas, salah tujuan, berubah, dibatalkan, atau dihentikan sebelum pelaksanaan, **Performer RUANG RANAP** meminta klarifikasi kepada **CPOE**; RNA tidak membuat fakta pelaksanaan.
- Untuk eksekusi billable, Performer memilih Service dari daftar aktif yang eligible bagi Layanan Bangsal; pada simpan, Sistem RNA memvalidasi ulang pilihan tersebut kepada **Tarif Context**. Eksekusi non-billable wajib memuat description dan tidak memilih Service.
- Bila **Tarif Context** tidak dapat menjawab, Sistem RNA menunjukkan `ServiceDependencyUnavailable`; bila query berhasil tetapi tidak menghasilkan Service eligible, sistem menunjukkan `NoEligibleService`. Keduanya menahan simpan billable, tetapi tidak membuat Service Definition lokal dan tidak menghalangi catatan non-billable yang memiliki description.
- Bila layanan tidak dilakukan, RNA hanya memperbarui status koordinasi pekerjaan sesuai fakta sumber dan tidak mempublikasikan Service Execution Fact.
- Bila fakta pelaksanaan salah, gunakan SOP-RNA-S03.
- Bila pengiriman gagal, fakta lokal tetap sah dan statusnya diteruskan ke pemulihan integrasi.

## 6. Kriteria Penyelesaian

- **Sistem RNA** menampilkan klasifikasi, identitas Service bila billable atau description bila non-billable, Performer aktual, `OccurredAt`/Performed At, `RecordedAt`, sumber/occurrence, dan identitas fakta; urutan bisnis menggunakan `OccurredAt`.
- Fakta pelaksanaan tersimpan satu kali dan status pengiriman ke **CPOE** serta **Tata Rekening** terlihat.
- Tidak ada performer rule, quantity/unit rule, documentation requirement, completion criterion, outcome catalogue, Charge Eligibility, atau keputusan finansial yang ditetapkan oleh RNA.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — batas eksekusi dan Service Execution Fact.
- `docs/contexts/tarif/tarif-01-context.md` — kepemilikan identitas dan definisi Service/Tarif.
- `docs/contexts/cpoe/CPOE-DOMAIN.md` — Clinical Order dan koordinasi fulfilment.
- `docs/contexts/TataRekening/01-context.md` — pemisahan operational truth dan financial truth.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — dependensi integrasi GAP-RNA-008 dan GAP-RNA-012.
