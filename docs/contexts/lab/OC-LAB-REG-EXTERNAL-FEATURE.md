---
Title: Registrasi Pasien Eksternal Laboratorium
Code: OC-LAB-REG-EXTERNAL
Artifact: FEATURE
Version: 1.0
LastUpdated: 2026-10-13
---

# 1. Purpose

Rumah sakit membutuhkan jalur mandiri yang cepat (*streamlined fast-track*) bagi pasien yang datang langsung ke instalasi laboratorium (*walk-in* atas inisiatif sendiri / APS) maupun pasien yang membawa surat rujukan dari fasilitas kesehatan luar (dokter praktik mandiri, puskesmas, klinik pratama, atau RS lain). Jalur ini memungkinkan pelayanan diagnostik tanpa harus mengantre dan melalui prosedur registrasi kunjungan rumah sakit reguler di loket admisi umum (*Admission / ADM*), sekaligus menjaga integritas master data rekam medis rumah sakit agar tidak tercemar oleh data pasien pemeriksaan sekali datang.

# 2. Business Outcome

Pendaftaran langsung pasien eksternal laboratorium tercatat secara resmi sebagai fakta bisnis persisten (*Direct External Lab Registration exists*) dengan nomor identitas operasional sekali pakai (*one-time use only*), terhubung dengan akun penagihan kasir laboratorium, dan siap digunakan sebagai dasar pemesanan tes diagnostik laboratorium (*OrderLab* / `LAB-ORDER`).

# 3. Participating Domains

Berdasarkan arsitektur fungsional MyHosWeb:

- **Laboratory (`LAB`)**: Primary Domain (Pemilik Utama). Bertanggung jawab atas pencatatan pendaftaran mandiri pasien luar, penerbitan nomor identitas operasional sekali pakai (`LAB-EXTERNAL`), pengelolaan status registrasi eksternal, dan penyediaan konteks pasien bagi pembuatan order laboratorium (`LAB-ORDER`).
- **Tata Rekening (`TRK`)**: Contributing Domain. Menyediakan referensi tarif tindakan laboratorium umum (`TRK-TARIF`), membentuk akun penagihan langsung (`TRK-BILLING`), dan memproses penyelesaian pembayaran kasir penunjang (`TRK-KASIR`).
- **Organisasi (`ORG`)**: Contributing Domain. Menyediakan referensi loket pendaftaran laboratorium (`ORG-LAYANAN`) dan identitas petugas pendaftaran laboratorium (`ORG-PPA` / `ORG-USER`).

# 4. Trigger

Pasien luar mendatangi loket pendaftaran instalasi laboratorium dengan membawa surat rujukan dari fasilitas kesehatan luar atau menyatakan keinginan melakukan pemeriksaan laboratorium atas inisiatif sendiri (Atas Permintaan Sendiri / APS).

# 5. Preconditions

1. Petugas pendaftaran laboratorium telah terotentikasi dan memiliki wewenang operasional pada loket instalasi laboratorium (`ORG-LAYANAN`).
2. Layanan kasir laboratorium dan master tarif pemeriksaan laboratorium (`TRK-TARIF`) tersedia dan aktif.
3. Pasien dapat memberikan informasi identitas minimal (nama lengkap, jenis kelamin, dan tanggal lahir/usia).

# 6. Operational Flow

1. **Penerimaan Pasien & Identifikasi Kedatangan**:
   - Petugas loket laboratorium menerima pasien dan mengidentifikasi kategori kedatangan:
     - *Atas Permintaan Sendiri (APS)*: Pasien datang mandiri tanpa surat rujukan dokter.
     - *Rujukan Fasilitas Luar (External Referral)*: Pasien membawa surat rujukan (petugas mencatat nama dokter perujuk, instansi/klinik pengirim, dan indikasi klinis rujukan).
2. **Perekaman Data Identitas Pasien Luar**:
   - Petugas memasukkan data demografi terbatas pasien: Nama Lengkap, NIK/KTP (opsional jika ada), Tanggal Lahir atau Estimasi Usia, Jenis Kelamin (wajib), Nomor Kontak/WhatsApp aktif, dan Alamat Domisili singkat.
3. **Penyimpanan Registrasi & Penerbitan Identitas Sekali Pakai**:
   - Sistem memvalidasi kelengkapan data minimal.
   - Sistem menerbitkan identifier unik registrasi (`RegExternalId`) dan nomor rekam medis operasional sekali pakai (`ExternalMrNo`).
   - Sistem menyimpan catatan registrasi eksternal dengan status awal `Active`.
4. **Pembentukan Akun Penagihan Kasir**:
   - Sistem membentuk akun penagihan langsung (`BillingAccountId`) di domain Tata Rekening (`TRK-BILLING`) yang terikat pada transaksi registrasi eksternal ini.
5. **Pencetakan Bukti & Transisi ke Pemesanan Pemeriksaan**:
   - Sistem mencetak bukti registrasi eksternal / nomor transaksi bagi pasien.
   - Catatan registrasi eksternal siap dipilih sebagai konteks pasien yang sah untuk pembuatan permintaan pemeriksaan laboratorium (`LAB-ORDER`).
6. **Alur Pembatalan (Opsional / Kondisional)**:
   - Jika pasien membatalkan pemeriksaan sebelum pembayaran kasir diproses dan sebelum spesimen diambil:
     - Petugas loket mengajukan pembatalan dengan menyertakan alasan pembatalan.
     - Sistem memverifikasi ketiadaan transaksi kasir lunas dan ketiadaan spesimen yang telah diambil.
     - Sistem memperbarui status registrasi eksternal menjadi `Cancelled` dan mencatat jejak audit pembatalan.

# 7. Domain Orchestration

1. **Laboratory (`LAB`) → Tata Rekening (`TRK`)**:
   - Saat pendaftaran eksternal divalidasi dan disimpan oleh `LAB-EXTERNAL`, subsistem mengorkestrasi pembentukan akun tagihan kasir penunjang ke `TRK-BILLING`.
   - `TRK-BILLING` mengembalikan referensi `BillingAccountId` untuk diasosiasikan dengan entitas `RegExternal`.
2. **Laboratory (`LAB`) Internal Orchestration (`LAB-EXTERNAL` → `LAB-ORDER`)**:
   - `LAB-ORDER` mengonsumsi entitas `RegExternal` sebagai sumber data snapshot pasien dan konteks registrasi yang sah untuk menerbitkan order pemeriksaan laboratorium pasien luar.
3. **Isolasi terhadap Domain Pasien (`PAS`) dan Admission (`ADM`)**:
   - Orkestrasi sengaja **tidak melibatkan** `PAS-DATSOS` maupun `ADM-REG`.
   - Pendaftaran eksternal tidak membuat data pasien master baru di `PAS-DATSOS` dan tidak membuat episode kunjungan rumah sakit reguler di `ADM-REG`.

# 8. Constraints

1. **Non-Contamination of Hospital Master Data**:
   - Registrasi eksternal beroperasi secara terisolasi dalam batas domain laboratorium dan dilarang membuat entitas rekam medis permanen di Domain Pasien (`PAS-DATSOS`).
2. **One-Time Identity Validity**:
   - Nomor identitas eksternal (`ExternalMrNo`) hanya sah untuk episode transaksi laboratorium terkait dan dilarang digunakan untuk pendaftaran rawat jalan, rawat inap, atau IGD reguler rumah sakit.
3. **Prasyarat Klinis Analitik (Clinical Range Prerequisite)**:
   - Jenis kelamin dan tanggal lahir (atau estimasi usia) wajib terisi lengkap agar instrumen laboratorium dapat menetapkan rentang nilai rujukan analitik (*reference range*) yang akurat.
4. **Prinsip Kasir Mandiri (Cash-First Principle)**:
   - Pembayaran biaya tindakan laboratorium pada akun penagihan (`TRK-BILLING` / `TRK-KASIR`) diselesaikan sebelum pengambilan spesimen atau sebelum hasil pemeriksaan diserahkan secara resmi.
5. **Non-Destructive Cancellation**:
   - Pembatalan registrasi eksternal dilakukan secara *soft-cancel* dengan status `Cancelled` serta mencatat identitas petugas, waktu pembatalan, dan alasan resmi pembatalan.

# 9. Exceptions

- **EX-01: Pembatalan Registrasi dengan Tagihan Kasir yang Telah Dibayar**:
  - *Pemicu*: Pengguna berupaya membatalkan registrasi eksternal yang tagihannya telah dibayar di kasir (`TRK-KASIR`).
  - *Perilaku*: Sistem menolak pembatalan dan mewajibkan penyelesaian prosedur pengembalian dana (*void / refund*) di kasir terlebih dahulu.
- **EX-02: Pembatalan Registrasi dengan Spesimen yang Telah Diambil**:
  - *Pemicu*: Pengguna berupaya membatalkan registrasi eksternal yang spesimen order lab-nya telah diambil (`Collected`).
  - *Perilaku*: Sistem menolak pembatalan registrasi karena sampel biologis telah masuk dalam penanganan analitik.
- **EX-03: Data Identitas Minimal Tidak Lengkap**:
  - *Pemicu*: Pasien menolak memberikan nama lengkap, jenis kelamin, atau estimasi usia.
  - *Perilaku*: Sistem menolak penyimpanan registrasi eksternal dan menampilkan pesan validasi identitas minimal.
- **EX-04: Format Kontak Tidak Valid untuk Hasil Digital**:
  - *Pemicu*: Nomor telepon/WhatsApp tidak sesuai format standar saat pasien meminta pengiriman hasil digital.
  - *Perilaku*: Sistem memberikan peringatan validasi format nomor kontak.

# 10. Acceptance Criteria

- **AC-01**: Sistem berhasil mencatat registrasi eksternal baru dengan identifier unik `RegExternalId` dan menerbitkan nomor identitas operasional sekali pakai (`ExternalMrNo`).
- **AC-02**: Catatan registrasi merekam secara lengkap nama pasien luar, jenis kelamin, tanggal lahir/usia, nomor kontak aktif, serta kategori kedatangan (APS atau Rujukan Luar beserta detail perujuk).
- **AC-03**: Terkonfirmasi bahwa registrasi eksternal tidak menyisipkan atau memodifikasi data pasien pada master data rekam medis permanen rumah sakit (`PAS-DATSOS`).
- **AC-04**: Keberhasilan pendaftaran eksternal secara otomatis membentuk akun penagihan langsung pada sistem penagihan Tata Rekening (`TRK-BILLING`).
- **AC-05**: Entitas registrasi eksternal yang berstatus `Active` dapat dipilih dan digunakan sebagai konteks pasien yang sah pada pembuatan order laboratorium (`LAB-ORDER`).
- **AC-06**: Upaya pembatalan registrasi yang memiliki pembayaran kasir lunas atau spesimen yang telah diambil berhasil dicegah oleh sistem.
- **AC-07**: Pembatalan registrasi yang memenuhi syarat berhasil mengubah status menjadi `Cancelled` dan mencatat alasan pembatalan dalam jejak audit.
- **AC-08**: Sistem mencegah penggunaan nomor identitas eksternal sekali pakai untuk pendaftaran kunjungan reguler rumah sakit.
