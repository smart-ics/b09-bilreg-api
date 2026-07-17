# SOP-RNA-S04 — Publikasi Service Execution Fact kepada Tata Rekening

> Nama file dipertahankan untuk kompatibilitas referensi. SOP ini tidak lagi menetapkan atau menyerahkan Charge Eligibility.

> Kontrak `RNA-TATA-REKENING-INTEGRATION.md` telah disetujui dan menutup GAP-RNA-012/ARCH-017 pada tingkat desain; pelaksanaan teknisnya masih merupakan dependensi implementasi.

## 1. Tujuan

Mempublikasikan Service Execution Fact yang diklasifikasikan billable kepada pemilik Tindakan secara idempoten; eksekusi non-billable tidak dipublikasikan dan tidak membuat Tindakan.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Performer RUANG RANAP | Manusia | Memastikan fakta Service, Performer, dan Performed At telah dicatat dengan benar. |
| Sistem RNA | Sistem | Membentuk payload fakta, mengirim secara idempoten, dan menyimpan status delivery/acknowledgement. |
| Pemilik Tindakan / Tata Rekening | Sistem | Membuat atau menemukan satu Tindakan per ServiceExecutionFactId dan memiliki seluruh lifecycle finansial berikutnya. |
| Aktor Pemulihan Integrasi | Manusia/Sistem | Menangani retry atau reconciliation tanpa mengubah arti fakta RNA. |

## 3. Prasyarat

- Service Execution Fact telah tersimpan dengan stable fact identity.
- Fakta diklasifikasikan billable dan memuat Service eligible, Performer, Performed At, dan korelasi sumber/occurrence. Eksekusi non-billable berhenti di RNA dengan description.
- Fakta menggunakan `OccurredAt` sebagai waktu bisnis otoritatif dan `RecordedAt` hanya sebagai waktu persistence untuk audit/technical tracing; bila waktu bisnis tidak diketahui, `OccurredAt = RecordedAt`.
- Kontrak inbound Tata Rekening mendukung idempotency, acknowledgement, dan correction fact.

## 4. Langkah Operasional

1. **Sistem RNA** mengambil Service Execution Fact yang sudah committed.
2. **Sistem RNA** membentuk payload billable minimum tanpa tariff, package, coverage, atau nilai finansial.
3. **Sistem RNA** mengirim fakta kepada pemilik **Tindakan** menggunakan `ServiceExecutionFactId` yang stabil.
4. Pemilik **Tindakan** membuat satu Tindakan atau mengembalikan Tindakan yang sudah ada, lalu mengirim acknowledgement beserta `TindakanId`; setelah `FINALIZED`/`LUNAS`, hasilnya `ReconciliationRequired` tanpa perubahan otomatis.
5. **Sistem RNA** menyimpan status Pending, Acknowledged, Failed, atau Rejected sebagai metadata integrasi.
6. Untuk koreksi, **Sistem RNA** mengirim correction fact baru yang merujuk fact identity dan revision sebelumnya.

## 5. Pengecualian Operasional

- Bila payload tidak memenuhi kontrak, fakta RNA tidak diubah menjadi keputusan eligibility; masalah ditampilkan sebagai integration dependency/rejection.
- Bila Tata Rekening tidak tersedia, pengiriman diulang dengan fact identity yang sama dan tidak menduplikasi pelaksanaan.
- Bila fakta sumber salah, koreksi dilakukan melalui SOP-RNA-S03 sebelum correction fact dikirim.
- Rejection finansial atau hasil evaluasi non-billable bukan error RNA dan tidak mengubah fakta bahwa layanan dilaksanakan.

## 6. Kriteria Penyelesaian

- Fakta tersimpan tetap otoritatif di RNA dan status delivery/acknowledgement terlihat.
- Retry menggunakan identitas fakta yang sama; koreksi menggunakan revision baru yang merujuk fakta asli.
- RNA menampilkan klasifikasi billable/non-billable dan linked `TindakanId`, tetapi tidak mengontrol tariff, package, coverage, amount, adjustment, journal, payment, atau settlement.

## 7. Referensi

- `docs/contexts/bangsal/RNA-DOMAIN.md` — Service Execution Fact dan batas finansial.
- `docs/contexts/TataRekening/01-context.md` — pemisahan operational truth dan financial truth.
- `docs/contexts/tarif/tarif-01-context.md` — identitas Service/Tarif.
- `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` — GAP-RNA-012 telah tertutup pada tingkat desain melalui kontrak yang disetujui.
