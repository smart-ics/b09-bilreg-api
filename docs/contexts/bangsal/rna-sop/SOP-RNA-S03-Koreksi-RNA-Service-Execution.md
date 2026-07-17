# SOP-RNA-S03 — Koreksi Fakta Pelaksanaan Layanan RUANG RANAP

## 1. Tujuan

Memperbaiki Service Execution Fact yang keliru tanpa menghapus riwayat asli dan mempublikasikan koreksi kepada konsumen fakta.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Aktor Care-Team yang Berwenang | Manusia | Melaporkan error dan mengusulkan koreksi atas fakta milik RNA. |
| Performer RUANG RANAP | Manusia | Memberikan klarifikasi atas Service, Performer, atau Performed At bila diperlukan. |
| Kepala Ruangan Ward Pemilik | Manusia | Memfinalisasi koreksi biasa, tetapi tidak boleh menyetujui laporan atau koreksinya sendiri. |
| Reviewer Independen yang Diberi Wewenang Clinical Governance | Manusia | Melakukan second review untuk perubahan identitas Patient/Registration/Service/source-order, replacement, Entered in Error, disputed case, dan koreksi material lain. |
| Sistem RNA | Sistem | Menyimpan fakta asli, koreksi, alasan, dan status publikasi. |
| CPOE | Sistem | Menerima koreksi fakta yang terkait Clinical Order. |
| Tata Rekening | Sistem | Menerima koreksi fakta pelaksanaan dan menentukan sendiri dampak finansialnya. |

## 3. Prasyarat

- Fakta asli dan kesalahan yang diduga dapat diidentifikasi.
- Pelapor, finalizer, dan reviewer memiliki kewenangan yang sesuai; self-approval tidak diperbolehkan.
- Koreksi dibatasi pada fakta milik RNA: Service reference, Performer, `OccurredAt`/Performed At, source/occurrence correlation, atau validitas record. Waktu koreksi sendiri memakai `OccurredAt`; `RecordedAt` tetap waktu persistence.

## 4. Langkah Operasional

1. **Aktor Care-Team yang Berwenang** melaporkan kesalahan dan membuka fakta asli pada **Sistem RNA**.
2. Pelapor mengusulkan fakta yang benar, structured reason, dan evidence untuk koreksi material.
3. **Kepala Ruangan Ward Pemilik** memeriksa dan dapat memfinalisasi koreksi biasa bila bukan pelapor/pengusul koreksi tersebut.
4. Perubahan identitas Patient/Registration/Service/source-order, replacement, **Entered in Error**, disputed case, dan koreksi material lain wajib memperoleh second review dari **Reviewer Independen yang Diberi Wewenang Clinical Governance**.
5. **Sistem RNA** mempertahankan fakta asli lalu menambahkan revision/correction fact dengan complete audit identity, aktor, reviewer bila diperlukan, alasan, evidence material, dan waktu koreksi.
6. Bila tindakan benar-benar terjadi tetapi identitas faktanya harus diganti, dibuat Service Execution Fact pengganti yang merujuk fakta asli.
7. **Sistem RNA** mempublikasikan correction fact kepada **CPOE** bila terkait order dan kepada **Tata Rekening** bila fakta asli pernah dikirim.
8. **Sistem RNA** menampilkan acknowledgement tiap tujuan; RNA tidak menentukan koreksi billing.

## 5. Pengecualian Operasional

- Bila fakta yang benar, kewenangan, independent review, atau evidence material belum jelas, finalisasi ditahan dan fakta asli tetap tersimpan.
- Koreksi yang disengketakan tidak dapat difinalisasi sebagai koreksi biasa oleh Kepala Ruangan; kasus mengikuti second review independen.
- Bila kesalahan berada pada Clinical Order, Service Definition Tarif, dokumentasi klinis, atau data finansial, koreksi dilakukan oleh konteks pemilik; RNA hanya memperbarui referensi setelah menerima fakta otoritatif.
- Bila pengiriman koreksi gagal, correction fact lokal tetap sah dan masuk pemulihan integrasi.

## 6. Kriteria Penyelesaian

- Fakta asli dan correction/replacement fact tampil sebagai satu rantai riwayat.
- Koreksi memuat identitas fakta asli, nilai yang benar, structured reason, complete audit identity, aktor, waktu, serta reviewer/evidence bila material.
- Status pengiriman koreksi ke **CPOE** dan/atau **Tata Rekening** terlihat tanpa menyimpulkan dampak order atau billing.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — koreksi non-destruktif Service Execution Fact.
- `docs/contexts/cpoe/CPOE-DOMAIN.md` — koreksi koordinasi Clinical Order.
- `docs/contexts/TataRekening/01-context.md` — pemisahan operational truth dan financial truth.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-011 CLOSED.
