# SOP-RNA-S03 — Koreksi Fakta Pelaksanaan Layanan RUANG RANAP

## 1. Tujuan

Mencatat koreksi atau Entered in Error atas RNA Service Execution Fact secara non-destruktif dan menyampaikan correction notice kepada sistem penerima yang terkait.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Aktor Care-Team yang Berwenang | Manusia | Melaporkan kesalahan, mengusulkan nilai yang benar, dan mencatat alasan koreksi. |
| Performer RUANG RANAP | Manusia | Memberikan klarifikasi mengenai aktivitas, pelaksana, dan waktu pelaksanaan bila diperlukan. |
| Kepala Ruangan Ward Pemilik | Manusia | Memeriksa dan memfinalisasi koreksi biasa yang tidak dilaporkan atau diusulkannya sendiri. |
| Reviewer Independen yang Diberi Wewenang Clinical Governance | Manusia | Meninjau perubahan material, replacement, disputed case, atau Entered in Error dalam proses RNA/Clinical Governance. |
| Sistem RNA | Sistem | Mempertahankan fakta asli, mencatat koreksi secara append-only, dan menampilkan status penyampaiannya. |
| CPOE | Sistem | Menerima ExecutionEvidenceCorrectionNotice, menambahkan riwayat yang dapat dipertanggungjawabkan, dan mencatat conflict atau kebutuhan reconciliation tanpa mengubah terminal status Order Occurrence. |
| Tata Rekening | Sistem | Menerima correction fact dan menampilkan hasil penerimaan untuk tindak lanjut finansial pada konteksnya. |

## 3. Prasyarat

- Aktor Care-Team yang Berwenang telah masuk ke Sistem RNA dan memiliki akses koreksi yang sesuai.
- RNA Service Execution Fact asli dan kesalahan yang diduga dapat diidentifikasi.
- Nilai yang benar, alasan koreksi, dan evidence untuk koreksi material tersedia sebelum finalisasi.
- Jika fakta berasal dari native CPOE, `ClinicalOrderId` dan `OrderOccurrenceId` tersedia pada fakta asli.
- Jika fakta asli pernah dikirim ke Tata Rekening, identitas pengiriman tersebut dapat ditelusuri.

## 4. Langkah Operasional

1. **Aktor Care-Team yang Berwenang** membuka RNA Service Execution Fact asli pada **Sistem RNA**.
2. **Sistem RNA** menampilkan fakta asli beserta riwayat koreksi dan status pengirimannya.
3. **Aktor Care-Team yang Berwenang** memilih tindakan koreksi yang tersedia, lalu mencatat jenis koreksi, nilai yang benar bila ada, structured reason, dan evidence yang diminta oleh Sistem RNA.
4. **Performer RUANG RANAP** memberikan klarifikasi pada catatan koreksi bila aktivitas, Performer, atau waktu pelaksanaan dipertanyakan.
5. **Kepala Ruangan Ward Pemilik** memeriksa koreksi biasa dan memfinalisasinya apabila tidak terjadi self-approval.
6. **Reviewer Independen yang Diberi Wewenang Clinical Governance** meninjau dan memfinalisasi perubahan material, replacement, disputed case, atau Entered in Error melalui proses RNA/Clinical Governance.
7. **Sistem RNA** mempertahankan fakta asli dan menambahkan correction fact yang menampilkan referensi fakta asli, jenis koreksi, nilai yang benar bila ada, alasan, aktor, reviewer bila diperlukan, evidence, dan waktu bisnis.
8. **Sistem RNA** menampilkan fakta pengganti yang merujuk fakta asli untuk replacement, atau menampilkan fakta asli sebagai Entered in Error tanpa menghapus riwayatnya.
9. Jika fakta asli memuat `ClinicalOrderId` dan `OrderOccurrenceId`, **Sistem RNA** mengirim ExecutionEvidenceCorrectionNotice kepada **CPOE** dengan kedua identitas tersebut dan referensi correction fact.
10. **CPOE** menambahkan correction notice pada riwayat yang dapat dipertanggungjawabkan dan menampilkan conflict atau kebutuhan reconciliation; **CPOE** tidak mengubah terminal status Order Occurrence secara otomatis.
11. Jika fakta asli pernah dikirim ke Tata Rekening, **Sistem RNA** mengirim correction fact kepada **Tata Rekening**.
12. **Tata Rekening** menampilkan hasil penerimaan correction fact untuk tindak lanjut pada konteks finansialnya.
13. **Sistem RNA** menampilkan status penerimaan oleh CPOE dan/atau Tata Rekening; **Aktor Care-Team yang Berwenang** memeriksa status tersebut.

## 5. Pengecualian Operasional

- Jika nilai yang benar, kewenangan, independent review, atau evidence material belum memadai, **Sistem RNA** menahan finalisasi dan menampilkan data yang harus dilengkapi; **Aktor Care-Team yang Berwenang** melengkapinya atau meminta review yang sesuai.
- Jika koreksi disengketakan, **Sistem RNA** mencegah finalisasi sebagai koreksi biasa; **Reviewer Independen yang Diberi Wewenang Clinical Governance** melakukan review melalui proses RNA/Clinical Governance.
- Jika kesalahan berada pada isi Clinical Order, Service Definition Tarif, dokumentasi klinis, atau data finansial, **Sistem RNA** mempertahankan fakta RNA; **Aktor Care-Team yang Berwenang** meneruskan koreksi kepada konteks pemilik.
- Jika fakta native CPOE tidak memiliki pasangan `ClinicalOrderId` dan `OrderOccurrenceId` yang valid, **CPOE** menolak asosiasi dan menampilkan kesalahan korelasi; **Aktor Care-Team yang Berwenang** memeriksa identitas sumber pada Sistem RNA.
- Jika pengiriman gagal, **Sistem RNA** mempertahankan correction fact dan menampilkan status gagal; **Aktor Care-Team yang Berwenang** memantau pemulihan tanpa membuat koreksi duplikat.

## 6. Kriteria Penyelesaian

- Sistem RNA menampilkan fakta asli dan correction, replacement, atau Entered in Error fact sebagai satu rantai riwayat yang tidak terhapus.
- Correction fact menampilkan identitas fakta asli, jenis koreksi, alasan, aktor, waktu bisnis, serta reviewer dan evidence bila diperlukan.
- Untuk fakta native CPOE, CPOE menampilkan correction notice pada riwayat serta conflict atau kebutuhan reconciliation untuk `OrderOccurrenceId` yang bersangkutan.
- Terminal status Order Occurrence pada CPOE tetap tidak berubah oleh correction notice.
- Sistem RNA menampilkan status penerimaan oleh CPOE dan/atau Tata Rekening yang dapat ditelusuri.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — business truth koreksi non-destruktif RNA Service Execution Fact.
- `docs/contexts/cpoe/CPOE-DOMAIN.md` — business truth lifecycle dan terminal status Order Occurrence CPOE.
- `docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md` — kontrak ExecutionEvidenceCorrectionNotice dan reconciliation.
- `docs/contexts/bangsal/RNA-ARCHITECTURE.md` — realisasi teknis koreksi dan integrasi RNA.
- `docs/contexts/TataRekening/01-context.md` — pemisahan operational truth dan financial truth.
