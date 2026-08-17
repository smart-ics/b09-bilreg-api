# Daftar SOP Pelayanan Obat Pasien Rawat Jalan

| Alur kerja | SOP bahasa Inggris | SOP bahasa Indonesia |
|---|---|---|
| `WF-APT-RJ-001` | [Acquire and Map Outpatient Pharmacy Queue](./SOP-APT-RJ-001-Antrian-dan-Mapping-EN.md) | [Menerbitkan Nomor Antrian dan Melakukan Mapping dengan Resep Kerja atau Jual Bebas](./SOP-APT-RJ-001-Antrian-dan-Mapping-ID.md) |
| `WF-APT-RJ-002` | [Accept Outpatient Medication Demand](./SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md) | [Menerima Permintaan Obat Pasien Rawat Jalan](./SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-ID.md) |
| `WF-APT-RJ-003` | [Fulfill Medication for a General Patient](./SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md) | [Melayani Obat Pasien Umum](./SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-ID.md) |
| `WF-APT-RJ-004` | [Fulfill Medication for a BPJS Patient](./SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md) | [Melayani Obat Pasien BPJS](./SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-ID.md) |
| `WF-APT-RJ-005` | [Fulfill Mixed-Coverage Medication](./SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md) | [Melayani Obat dengan Penjaminan Campuran](./SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-ID.md) |
| `WF-APT-RJ-006` | [Coordinate Multiple Medication Demands in One Queue](./SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-EN.md) | [Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrian](./SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-ID.md) |
| `WF-APT-RJ-007` | [Resolve Uncollected Outpatient Medication](./SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md) | [Menangani Obat Rawat Jalan yang Tidak Diambil](./SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-ID.md) |

Dokumen bahasa Inggris merupakan spesifikasi operasional acuan. Dokumen bahasa Indonesia berstatus **Pendamping operasional Bahasa Indonesia** dan menyampaikan ketentuan yang sama dengan istilah yang lazim digunakan oleh petugas rumah sakit dan apotek.

## Aturan Precedence

DOMAIN merupakan sumber tertinggi untuk business truth. WORKFLOW harus mengikuti DOMAIN dan menjadi sumber untuk urutan, keputusan, exception, serta outcome bisnis. SOP harus mengikuti DOMAIN dan WORKFLOW serta menjadi sumber untuk tindakan operator dan respons aplikasi yang dapat diamati.

Jika SOP bahasa Inggris dan Bahasa Indonesia berbeda secara semantik, SOP bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN. Jika SOP bahasa Inggris bertentangan dengan artifact yang lebih tinggi, DOMAIN atau WORKFLOW yang berlaku dan kedua versi SOP harus diperbaiki bersama. Perbedaan terjemahan tidak boleh dibiarkan menjadi dua prosedur operasional yang berbeda.

## Pedoman Istilah

| Istilah sumber | Istilah dalam SOP bahasa Indonesia |
|---|---|
| Apotek | Pelayanan Obat |
| Patient | Pasien |
| Caregiver | Keluarga Pasien |
| Pharmacy Staff | Staf Apotek |
| Pharmacist | Apoteker |
| Queue / Outpatient Queue | Antrian / Antrian Pasien Rawat Jalan |
| Medication Demand | Permintaan Obat |
| Jual Bebas | Jual Bebas |
| Resep Kerja | Resep Kerja |
| Invoice | Invoice |
| Sales Order Item | Item Sales Order |
| Invoice Item | Item Invoice |
| Dispensing Item | Item Dispensing |
| Medication Catalog | Katalog Obat |
| Resep Elektronik | Resep Elektronik |
| Dokter Penulis Resep | Dokter Penulis Resep |
| Baris Resep | Baris Resep |
| Resep Kerja | Resep Kerja |
| Jual Bebas | Jual Bebas |
| Source Traceability | Ketertelusuran Sumber |
| Coverage | Penjaminan |
| Payment Clearance | Status Lunas |
| Coverage Clearance | Persetujuan Penjaminan |
| Dispense Authorized | Dispense Authorized |
| Invoice | Faktur |
| Sales Order Item | Item Sales Order |
| Invoice Item | Item Invoice |
| Dispensing | Dispensing |
| Dispensing Item | Item Dispensing |
| Medication Handover | Penyerahan Obat |
| Outpatient Queue Mapping | Outpatient Queue Mapping |
| Tracker Mapping / Manual Mapping | Tracker Mapping / Manual Mapping |

Nama menu, kode aturan, dan nilai status yang berasal langsung dari sistem tetap ditulis apa adanya dalam tanda backtick agar dapat dicocokkan dengan aplikasi.
