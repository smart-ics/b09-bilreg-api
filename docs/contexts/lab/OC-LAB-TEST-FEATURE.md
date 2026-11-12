---
Title: Definisi Pemeriksaan Laboratorium (Lab Test Definition)
Code: OC-LAB-TEST
Artifact: FEATURE
Version: 1.2
LastUpdated: 2026-10-16
---

# 1. Purpose

Menyediakan acuan baku pemeriksaan laboratorium operasional terstandarisasi yang menghubungkan katalog tarif tindakan rumah sakit (`TRK-TARIF`) dengan standar pelaksanaan pengujian laboratorium melalui pemetaan relasi 1:1. Definisi pemeriksaan laboratorium bertindak sebagai cetak biru (*blueprint*) pengujian yang menetapkan persyaratan pra-analitik (kebutuhan tepat satu jenis spesimen biologis dan satu jenis wadah penampung) serta susunan parameter analitik terurut untuk mendukung alur pemesanan pemeriksaan (`OC-LAB-ORDER-LAB`), persiapan dan pengambilan spesimen (`OC-LAB-SAMPLE-COLLECTION`), serta perancah perekaman dan pelaporan hasil analisis laboratorium (`OC-LAB-HASIL-LAB`).

# 2. Business Outcome

Definisi pemeriksaan laboratorium telah ditetapkan, dihubungkan secara sah ke tepat satu tarif tindakan aktif yang berlaku di Tata Rekening (relasi 1:1), dan diaktivasi sebagai acuan operasional resmi sehingga siap menjadi rujukan pemesanan, pengambilan spesimen, dan perancah hasil laboratorium secara deterministik tanpa mengubah riwayat transaksi pelayanan masa lalu.

# 3. Participating Domains

- **Laboratory (`LAB`)**: Primary Domain (Pemilik Utama). Menetapkan spesifikasi pemeriksaan klinis, merangkai susunan parameter pengujian, menetapkan standar kebutuhan wadah dan spesimen, serta mengelola siklus keaktifan definisi pemeriksaan (`LAB-TEST` / `LAB-ORDER`).
- **Tata Rekening (`TRK`)**: Contributing Domain. Menyediakan master tarif tindakan resmi (`TRK-TARIF`) sebagai rujukan pemetaan finansial 1:1 bagi pemeriksaan laboratorium aktif, serta menerima pembebanan biaya tindakan (`TRK-BILLING`).
- **Pelayanan Klinis (`EMR` / `RJL` / `RNA` / `IGD`)**: Downstream Consumer. Mengajukan permintaan pemeriksaan laboratorium bagi pasien berdasarkan katalog pemeriksaan/tarif yang terhubung secara deterministik.

# 4. Trigger

Penanggung jawab laboratorium atau petugas tata kelola laboratorium yang berwenang menginisiasi perumusan definisi pemeriksaan baru, memperbarui konfigurasi pemeriksaan yang ada, atau mengaktifkan/menonaktifkan pemeriksaan dalam katalog operasional rumah sakit.

# 5. Preconditions

1. Tarif tindakan resmi yang dirujuk telah terdaftar dan berstatus aktif di domain Tata Rekening (`TRK-TARIF`).
2. Tarif tindakan tersebut belum terikat pada definisi pemeriksaan laboratorium lain yang sedang berstatus aktif (relasi 1:1).
3. Seluruh komponen analitik yang dirangkai ke dalam pemeriksaan telah terdaftar sah dan berstatus aktif pada katalog komponen laboratorium (`LabComponentMaster`).
4. Pengguna memiliki hak otorisasi tata kelola katalog laboratorium.

# 6. Operational Flow

1. **Perumusan Identitas & Pra-Analitik Pemeriksaan**:
   - Pengelola menetapkan kode dan nama operasional resmi pemeriksaan laboratorium.
   - Pengelola menentukan satu jenis spesimen biologis (darah vena, urine sewaktu, dll.) dan satu jenis tabung penampung (*vacutainer*).
2. **Pemetaan ke Tarif Tindakan Resmi (1:1)**:
   - Pengelola menghubungkan definisi pemeriksaan ke tepat satu tarif tindakan aktif di Tata Rekening.
   - Sistem memvalidasi bahwa tarif tersebut belum digunakan oleh pemeriksaan aktif lain.
3. **Penyusunan Parameter Analitik Terurut**:
   - Pengelola merangkai parameter pengujian analitik dengan urutan tampilan (`SequenceNo`) yang unik dan teratur.
   - Menetapkan tingkat kewajiban pengisian hasil (*mandatory* atau opsional), penandaan nilai kritis/abnormal, dan teks rentang rujukan khusus pemeriksaan (bila ada).
4. **Validasi Integritas & Aktivasi Operasional**:
   - Sistem memverifikasi bahwa pemeriksaan memiliki sekurang-kurangnya satu parameter analitik aktif dan tarif rujukan aktif yang bebas konflik.
   - Sistem menetapkan status operasional pemeriksaan menjadi Aktif.
5. **Konsumsi Pemesanan Laboratorium**:
   - Alur pemesanan laboratorium (`OC-LAB-ORDER-LAB`) meresolusi tarif tindakan yang dipesan menjadi spesifikasi tes, kebutuhan spesimen, tabung flebotomi, dan kerangka hasil analitik secara deterministik.

# 7. Domain Orchestration

1. **Katalog & Otorisasi Tata Rekening (`LAB-TEST` ↔ `TRK-TARIF`)**:
   - `LAB-TEST` memvalidasi keberadaan, keaktifan, dan keunikan pemetaan tarif ke `TRK-TARIF` saat pendaftaran maupun aktivasi definisi tes.
2. **Penyediaan Cetak Biru Pemesanan (`LAB-TEST` ↔ `LAB-ORDER`)**:
   - `LAB-ORDER` mengonsumsi metadata definisi tes saat order diterima untuk menguraikan item tes, kebutuhan tabung (*vacutainer*), jenis spesimen, dan susunan komponen analitik yang disalin (*snapshotted*) ke transaksi order secara deterministik berbasis `TarifId`.
3. **Penyelarasan Tagihan Kasir (`LAB-ORDER` ↔ `TRK-BILLING`)**:
   - Saat order ditagihkan, `LAB-ORDER` mengirimkan pembebanan biaya tindakan ke `TRK-BILLING`.

# 8. Constraints

1. **Pemetaan Tarif 1:1 Aktif**:
   - Satu `TarifId` hanya boleh terikat pada tepat satu `LabTestDefinition` yang aktif (`IsActive = true`).
   - Pendaftaran atau pengaktifan definisi pemeriksaan baru yang mereferensikan `TarifId` yang sudah digunakan oleh definisi pemeriksaan aktif lain wajib ditolak.
2. **Kardinalitas 1 Tes = 1 Tabung & 1 Spesimen**:
   - Setiap definisi pemeriksaan laboratorium hanya mengikat tepat satu jenis spesimen biologis dan satu wadah penampung. Pemeriksaan multi-wadah didekomposisi pada tingkat pemesanan menjadi beberapa pemeriksaan terpisah.
3. **Prasyarat Aktivasi Pemeriksaan**:
   - Definisi pemeriksaan hanya dapat diaktifkan jika memiliki sekurang-kurangnya satu parameter analitik yang valid dan aktif.
4. **Integritas Struktur Komponen**:
   - Nomor urut tampilan (`SequenceNo`) dan komponen analitik wajib unik dalam satu pemeriksaan (tidak boleh ada urutan bertabrakan atau komponen ganda).
5. **Kekebalan Riwayat Transaksi (Non-Retroactivity)**:
   - Penyesuaian atau penonaktifan definisi pemeriksaan tidak mengubah struktur transaksi order atau lembar hasil yang sudah terbit sebelumnya.

# 9. Exceptions

| Kode | Kondisi Pemicu | Perilaku yang Diharapkan |
| :--- | :--- | :--- |
| **EX-LTD-01** | Tarif tindakan rujukan tidak ditemukan atau tidak aktif di Tata Rekening saat pendaftaran/aktivasi. | Penyimpanan atau aktivasi definisi pemeriksaan ditolak. |
| **EX-LTD-02** | `TarifId` yang dipilih sudah terikat pada definisi pemeriksaan aktif lain. | Penyimpanan atau aktivasi definisi pemeriksaan ditolak karena konflik relasi 1:1. |
| **EX-LTD-03** | Komponen analitik yang dipilih tidak terdaftar atau tidak aktif. | Penyusunan baris parameter ditolak. |
| **EX-LTD-04** | Ditemukan duplikasi urutan tampilan atau duplikasi komponen analitik. | Penyimpanan konfigurasi pemeriksaan ditolak. |
| **EX-LTD-05** | Upaya pengaktifan pemeriksaan yang belum memiliki komponen analitik aktif. | Pengaktifan diblokir hingga sekurang-kurangnya satu komponen aktif ditambahkan. |

# 10. Acceptance Criteria

| # | Kriteria Penerimaan | Kategori |
| :--- | :--- | :--- |
| **AC-01** | Definisi pemeriksaan berhasil dicatat dengan pengenal unik resmi, terhubung ke tepat satu tarif tindakan sah secara 1:1, serta memuat ketentuan satu jenis spesimen dan wadah penampung yang valid. | *Completeness* |
| **AC-02** | Susunan komponen analitik tersimpan teratur sesuai urutan tampilan tanpa duplikasi urutan maupun komponen, lengkap dengan aturan kewajiban pengisian dan penandaan abnormal. | *Correctness* |
| **AC-03** | Sistem menolak aktivasi pemeriksaan jika tarif yang dirujuk sudah digunakan oleh definisi pemeriksaan aktif lain (konflik 1:1) atau jika belum memiliki parameter pengujian aktif. | *Constraint* |
| **AC-04** | Penyesuaian atau penonaktifan definisi pemeriksaan terbukti tidak memengaruhi susunan pemeriksaan pada order dan lembar hasil yang telah diterbitkan sebelumnya (*non-retroactive*). | *Non-Retroactivity* |
| **AC-05** | Alur pemesanan laboratorium berhasil menguraikan `TarifId` yang dipesan menjadi detail tes, kebutuhan wadah flebotomi, jenis spesimen, dan perancah hasil secara deterministik. | *Integration* |
