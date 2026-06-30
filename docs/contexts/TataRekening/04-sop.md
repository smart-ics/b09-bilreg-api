# SOP TATA REKENING

## 1. TUJUAN

### 1.1 Tujuan Tata Rekening

Tata Rekening adalah bounded context yang bertanggung jawab mengelola **Financial Responsibility** untuk setiap Registrasi Pasien.

Tujuan utama Tata Rekening adalah memastikan bahwa seluruh kewajiban finansial pasien telah diverifikasi, dialokasikan, difinalisasi, dan siap diselesaikan melalui proses pembayaran (Settlement), dengan tetap menjaga konsistensi terhadap sistem akuntansi dan piutang rumah sakit.

Tata Rekening **bukan** bertugas mencatat aktivitas operasional pelayanan. Aktivitas operasional tetap menjadi tanggung jawab subsistem sumber (Charge Source). Tata Rekening hanya mengelola konsekuensi finansial yang timbul dari aktivitas tersebut.

---

### 1.2 Sasaran Bisnis

Tata Rekening dibangun untuk mencapai sasaran berikut:

- Memastikan seluruh Billing Set telah terbentuk secara lengkap dan benar.
- Menghentikan pembentukan Financial Charge sebelum proses Financial Control dimulai.
- Mendukung konsolidasi Billing antar Registrasi melalui mekanisme Merge Billing.
- Memastikan seluruh Financial Charge telah diverifikasi.
- Mengelola Financial Adjustment tanpa mengubah riwayat operasional.
- Menetapkan Financial Responsibility kepada setiap Payer.
- Melakukan Finalize Financial Responsibility sebelum Settlement.
- Menyediakan Financial Projection yang konsisten sebagai dasar Payment Settlement.
- Menjaga konsistensi Financial Truth sepanjang siklus hidup Registrasi.

---

### 1.3 Ruang Lingkup

Tata Rekening mencakup aktivitas berikut:

1. Open Tata Rekening
2. Close Bill
3. Merge Billing
4. Financial Verification
5. Financial Adjustment
6. Financial Responsibility Allocation
7. Finalize Financial Responsibility
8. Cancel Finalization
9. Reopen Billing
10. Settlement Initiation

Tata Rekening tidak mencakup:

- Pencatatan tindakan medis.
- Pencatatan pelayanan operasional.
- Pembentukan Financial Charge oleh Charge Source.
- Penerimaan pembayaran (Kasir).
- Pembentukan jurnal akuntansi (Accounting).

---

### 1.4 Prinsip Dasar

Seluruh proses Tata Rekening mengikuti prinsip-prinsip berikut.

#### Operational Truth ≠ Financial Truth

Aktivitas operasional dan tanggung jawab finansial merupakan dua domain yang berbeda.

Charge Source bertanggung jawab atas kebenaran aktivitas operasional, sedangkan Tata Rekening bertanggung jawab atas kebenaran finansial.

---

#### Financial Responsibility per Registrasi

Seluruh proses Tata Rekening selalu berada dalam ruang lingkup satu Registrasi Pasien.

Setiap Registrasi memiliki satu Tata Rekening yang menjadi otoritas terhadap seluruh Billing Set pada registrasi tersebut.

---

#### Financial Finalization sebelum Settlement

Seluruh kewajiban finansial harus selesai diverifikasi, dialokasikan, dan difinalisasi sebelum dapat diproses oleh Kasir.

Pembayaran tidak boleh dilakukan terhadap Billing yang belum Finalized.

---

#### Financial Projection sebagai Dasar Settlement

Hasil akhir Tata Rekening adalah Financial Projection yang menjadi dasar:

- proses pembayaran,
- distribusi kepada payer,
- serta pembentukan data yang akan digunakan oleh Accounting.

---

#### Compatibility First

Seluruh proses Tata Rekening dirancang agar tetap kompatibel dengan mekanisme Billing, Kasir, dan Accounting yang telah digunakan pada sistem legacy, sehingga migrasi dapat dilakukan secara bertahap tanpa mengganggu operasional rumah sakit.

---

## 2. PRECONDITION

### 2.1 Tujuan

Bagian ini menjelaskan kondisi yang harus dipenuhi sebelum proses Tata Rekening dapat dimulai.

Tata Rekening hanya dapat dijalankan apabila Registrasi telah memiliki Financial Responsibility yang dapat diverifikasi. Sistem harus memastikan bahwa seluruh kondisi awal telah terpenuhi agar proses Financial Verification, Financial Finalization, dan Settlement dapat berlangsung secara konsisten.

---

### 2.2 Aktor

Aktor utama yang menjalankan SOP ini adalah: **Verifikator**

Verifikator memiliki kewenangan untuk:

- Open Tata Rekening
- Close Bill
- Merge Billing
- Financial Verification
- Financial Adjustment
- Financial Responsibility Allocation
- Finalize Financial Responsibility
- Cancel Finalization
- Reopen Billing
- Settlement Initiation

---

### 2.3 Kondisi Awal

Sebelum Tata Rekening dibuka, seluruh kondisi berikut harus terpenuhi.

#### Registrasi

- Registrasi pasien telah dibuat.
- Registrasi masih aktif.
- Registrasi belum berstatus LUNAS.

---

#### Billing

- Billing Set untuk Registrasi telah tersedia.
- Billing berada pada status **OPEN** atau **CLOSED**.
- Billing belum berada pada status **FINALIZED** apabila akan dilakukan perubahan finansial.

---

#### Charge Source

Seluruh Charge Source yang menjadi dasar tagihan telah menyelesaikan proses pembentukan Financial Charge.

Contoh Charge Source:

- Pelayanan Medis
- Rawat Inap
- Farmasi
- Laboratorium
- Radiologi
- Transport
- Unit pelayanan lainnya

---

#### Hak Akses

Verifikator memiliki hak akses Tata Rekening sesuai kebijakan rumah sakit.

---

### 2.4 Kondisi yang Tidak Memenuhi Syarat

Tata Rekening tidak boleh dijalankan apabila:

- Registrasi telah berstatus LUNAS.
- Registrasi telah dibatalkan.
- Billing sedang diproses oleh workflow lain yang menyebabkan perubahan finansial tidak diperbolehkan.
- Verifikator tidak memiliki hak akses yang sesuai.

---

### 2.5 Hasil Setelah Precondition Terpenuhi

Apabila seluruh kondisi telah terpenuhi, proses Tata Rekening dilakukan melalui tahapan berikut:

1. Open Tata Rekening
2. Close Bill
3. Merge Billing (bila diperlukan)
4. Financial Verification
5. Financial Adjustment (bila diperlukan)
6. Financial Responsibility Allocation
7. Finalize Financial Responsibility
8. Settlement Initiation

---

## 3. WORKFLOW

### 3.1 Gambaran Umum

Workflow Tata Rekening menggambarkan perjalanan **Financial Responsibility** sejak Registrasi memasuki proses Tata Rekening hingga Billing siap diselesaikan melalui proses **Payment Settlement**.

Workflow ini terdiri atas **tiga fase utama**, yaitu:

1. **Operational Freeze Phase**
2. **Financial Control Phase**
3. **Settlement Phase**

Pembagian ini menegaskan bahwa proses operasional pelayanan, proses pengendalian finansial, dan proses pembayaran merupakan tiga domain aktivitas yang berbeda namun saling berkesinambungan.

---

### 3.2 Workflow Overview

```text
══════════════════════════════════════════════════════
        OPERATIONAL FREEZE PHASE
══════════════════════════════════════════════════════

Open Tata Rekening
        │
        ▼
Close Bill

══════════════════════════════════════════════════════
         FINANCIAL CONTROL PHASE
══════════════════════════════════════════════════════

Merge Billing (Optional)
        │
        ▼
Financial Verification
        │
        ▼
Financial Adjustment (Optional)
        │
        ▼
Financial Responsibility Allocation
        │
        ▼
Finalize Financial Responsibility
        │
        ├──────────────► Cancel Finalization (Optional)
        │                      │
        │                      ▼
        │      Financial Responsibility Allocation
        │
        ▼

══════════════════════════════════════════════════════
           SETTLEMENT PHASE
══════════════════════════════════════════════════════

Settlement Initiation
        │
        ▼
Payment Settlement
        │
        ▼
LUNAS
```

---

## 3.3 Operational Freeze Phase

Operational Freeze Phase merupakan batas antara proses operasional pelayanan dengan proses Financial Control.

Selama Billing masih berstatus **OPEN**, seluruh Charge Source masih diperbolehkan membentuk, mengubah, maupun membatalkan Financial Charge sesuai kewenangannya.

Contoh Charge Source meliputi:

- Pelayanan Medis
- Rawat Inap
- Farmasi
- Laboratorium
- Radiologi
- Transport
- Unit pelayanan lainnya

Setelah Billing diubah menjadi **CLOSED**, seluruh Charge Source tidak lagi diperbolehkan menghasilkan Financial Charge baru.

Tahap ini bertujuan menghasilkan Billing Set yang stabil sebelum memasuki proses Financial Control.

Operational Freeze Phase terdiri dari:

- Open Tata Rekening
- Close Bill

---

### 3.3.1 Open Tata Rekening

Verifikator membuka Tata Rekening pada Registrasi yang akan diproses.

Sistem mempersiapkan seluruh informasi yang diperlukan selama proses Financial Control, antara lain:

- Billing Set
- Financial Projection
- Informasi Payer
- Merge Request yang masih berstatus **Pending** dan relevan terhadap Registrasi

Apabila ditemukan Merge Request, sistem menampilkannya sebagai kandidat Merge Billing.

Tahap ini belum melakukan perubahan terhadap Billing maupun Financial Responsibility.

---

### 3.3.2 Close Bill

Close Bill merupakan proses menghentikan pembentukan Financial Charge dari seluruh Charge Source.

Tujuan Close Bill adalah:

- menghentikan pembentukan Financial Charge baru;
- memastikan Billing Set berada pada kondisi stabil;
- menjadi batas antara domain operasional dan domain finansial.

Close Bill **bukan** merupakan:

- Merge Billing;
- Financial Verification;
- Finalize Financial Responsibility;
- Settlement;
- Payment.

Apabila setelah Close Bill masih diperlukan perubahan Financial Charge, Billing harus dibuka kembali melalui **Reopen Billing**, kemudian seluruh proses Financial Control diulang mulai dari **Close Bill**.

---

### 3.4 Financial Control Phase

Financial Control Phase merupakan inti proses Tata Rekening.

Pada fase ini Billing Set telah berada dalam kondisi operasional yang stabil sehingga seluruh aktivitas difokuskan pada pengelolaan **Financial Truth**.

Aktivitas pada fase ini meliputi:

- konsolidasi Billing antar Registrasi melalui Merge Billing;
- verifikasi Financial Charge;
- koreksi Financial Responsibility apabila diperlukan;
- penetapan tanggung jawab pembayaran;
- pembentukan Financial Projection;
- Finalize Financial Responsibility.

Financial Control Phase terdiri dari:

- Merge Billing (Optional)
- Financial Verification
- Financial Adjustment (Optional)
- Financial Responsibility Allocation
- Finalize Financial Responsibility

---

### 3.4.1 Merge Billing (Optional)

Merge Billing digunakan untuk menggabungkan Billing Set dari satu atau lebih Registrasi sumber ke Registrasi tujuan berdasarkan Merge Request yang masih berstatus **Pending**.

Contoh penggunaan:

- Rawat Jalan → Rawat Inap
- IGD → Rawat Inap
- Bayi → Ibu
- Koreksi Registrasi

Setelah Merge Billing selesai:

- Billing Set Registrasi tujuan diperbarui;
- Financial Projection diregenerasi;
- Accounting menerima permintaan Transfer Receivable;
- Merge Request berubah menjadi **Executed**.

Apabila tidak terdapat Merge Request, proses langsung dilanjutkan ke Financial Verification.

---

### 3.4.2 Financial Verification

Verifikator memastikan bahwa seluruh Billing Set telah lengkap, benar, dan konsisten.

Verifikasi meliputi antara lain:

- seluruh Financial Charge telah terbentuk;
- tidak terdapat Financial Charge yang hilang;
- tidak terdapat Financial Charge ganda;
- hasil Merge Billing telah sesuai;
- Billing Set sesuai dengan dokumen pendukung.

Apabila ditemukan ketidaksesuaian, proses dilanjutkan ke Financial Adjustment.

---

### 3.4.3 Financial Adjustment (Optional)

Financial Adjustment digunakan untuk melakukan koreksi terhadap Billing Set maupun Financial Responsibility tanpa mengubah riwayat operasional yang menjadi tanggung jawab Charge Source.

Contoh Financial Adjustment:

- Manual Charge
- Billing Correction
- Waive
- Subsidy
- Merge Billing Correction

Apabila Financial Adjustment memerlukan perubahan Financial Charge oleh Charge Source, Billing harus dibuka kembali melalui **Reopen Billing**.

Setelah seluruh perubahan operasional selesai dilakukan, proses Financial Control dimulai kembali dari **Close Bill**.

---

### 3.4.4 Financial Responsibility Allocation

Verifikator menetapkan tanggung jawab pembayaran kepada setiap Payer.

Contoh Payer:

- Pasien
- BPJS
- Asuransi
- Perusahaan
- Subsidi Rumah Sakit

Sistem kemudian mendistribusikan Financial Responsibility tersebut secara proporsional ke seluruh Billing Set dan membentuk Financial Projection sebagai dasar Settlement.

---

### 3.4.5 Finalize Financial Responsibility

Finalize Financial Responsibility merupakan tahap terakhir pada Financial Control Phase.

Sebelum Finalization dilakukan, sistem memastikan bahwa:

- Financial Verification telah selesai;
- seluruh Financial Adjustment telah selesai atau tidak diperlukan;
- seluruh Billing telah memiliki Financial Responsibility;
- total Allocation sama dengan total Billing;
- Financial Projection telah berhasil diregenerasi.

Setelah Finalization berhasil:

- Billing berstatus **FINALIZED**;
- Financial Responsibility Allocation menjadi terkunci;
- Financial Projection menjadi dasar Payment Settlement;
- Billing siap diserahkan kepada Kasir melalui Settlement Initiation.

Apabila setelah Finalization diperlukan perubahan Financial Responsibility, proses dilakukan melalui **Cancel Finalization** sebelum dilakukan Allocation kembali.

---

## 3.5 Settlement Phase

Settlement Phase merupakan proses penyelesaian Financial Responsibility yang telah difinalisasi.

Seluruh proses pada fase ini berada di bawah tanggung jawab Kasir.

Tata Rekening tidak lagi melakukan perubahan terhadap Billing Set maupun Financial Responsibility yang telah berstatus **FINALIZED**.

Tahap ini terdiri dari:

- Settlement Initiation
- Payment Settlement

---

### 3.5.1 Settlement Initiation

Settlement Initiation merupakan proses penyerahan Billing yang telah berstatus **FINALIZED** kepada Kasir untuk memulai Payment Settlement.

Tahap ini menjadi batas akhir tanggung jawab Tata Rekening sebagai pengelola Financial Control dan awal tanggung jawab Kasir sebagai pelaksana Payment Settlement.

Settlement Initiation tidak melakukan pembayaran dan tidak mengubah Financial Responsibility.

---

### 3.5.2 Payment Settlement

Kasir menerima pembayaran berdasarkan Financial Responsibility yang telah difinalisasi.

Pembayaran dapat dilakukan:

- satu kali; atau
- beberapa kali,

sesuai kebijakan rumah sakit.

Setiap pembayaran akan mengurangi Financial Responsibility hingga seluruh kewajiban Registrasi dinyatakan selesai.

---

### 3.5.3 LUNAS

Status **LUNAS** menandakan bahwa seluruh Financial Responsibility Registrasi telah diselesaikan.

Setelah mencapai status **LUNAS**:

- Billing tidak dapat diubah;
- Financial Responsibility tidak dapat diubah;
- Finalization tidak dapat dibatalkan;
- Billing dianggap telah selesai;
- Proses Tata Rekening untuk Registrasi tersebut berakhir.
  
---

## 4. DETAIL WORKFLOW SOP

Bagian ini menjelaskan urutan pelaksanaan setiap SOP dalam proses Tata Rekening.

Workflow dibagi menjadi tiga fase utama:

1. **Operational Freeze Phase**
2. **Financial Control Phase**
3. **Settlement Phase**

Setiap SOP memiliki dokumen tersendiri yang menjelaskan tujuan, aktor, precondition, workflow, business rules, serta exception secara rinci.

---

### 4.1 Operational Freeze Phase

Operational Freeze Phase merupakan tahap persiapan sebelum Financial Control dimulai.

Tujuan fase ini adalah memastikan bahwa Billing Set telah berada pada kondisi operasional yang stabil sehingga tidak ada lagi Financial Charge baru yang dibentuk oleh Charge Source.

Fase ini terdiri dari dua SOP.

| Kode       | SOP                | Mandatory |
|------------|--------------------|-----------|
| SOP-TR-01  | [Open Tata Rekening](<SOP-TR-01 — Open Tata Rekening.md>) | Yes       |
| SOP-TR-02  | [Close Bill](<SOP-TR-02 — Close Bill.md>)         | Yes       |

---

#### [SOP-TR-01 — Open Tata Rekening](<SOP-TR-01 — Open Tata Rekening.md>)

Membuka Tata Rekening pada Registrasi yang akan diproses.

Sistem mempersiapkan seluruh informasi Financial Control, meliputi:

- Billing Set
- Financial Projection
- Informasi Payer
- Merge Request yang masih Pending

Output utama:

- Tata Rekening siap diproses.
- Daftar Merge Request telah tersedia apabila ada.

---

#### [SOP-TR-02 — Close Bill](<SOP-TR-02 — Close Bill.md>)

Menutup Billing sehingga seluruh Charge Source tidak lagi dapat membentuk Financial Charge baru.

Close Bill menjadi batas antara proses operasional dengan proses Financial Control.

Output utama:

- Billing berstatus **CLOSED**.
- Billing Set berada pada kondisi stabil.
- Billing siap memasuki Merge Billing atau Financial Verification.

---

### 4.2 Financial Control Phase

Financial Control Phase merupakan inti proses Tata Rekening.

Pada fase ini Verifikator melakukan seluruh aktivitas pengendalian Financial Responsibility terhadap Billing Set yang telah stabil.

Fase ini terdiri dari lima aktivitas utama dan dua aktivitas opsional.

|Kode|SOP|Mandatory|
|-----|-----|-----------|
|SOP-TR-03|[Merge Billing](<SOP-TR-03 — Merge Billing.md>)|Optional|
|SOP-TR-04|[Financial Verification](<SOP-TR-04 — Financial Verification.md>)|Yes|
|SOP-TR-05|[Financial Adjustment](<SOP-TR-05 — Financial Adjustment.md>)|Optional|
|SOP-TR-06|[Financial Responsibility Allocation](<SOP-TR-06 — Financial Responsibility Allocation.md>)|Yes|
|SOP-TR-07|[Finalize Financial Responsibility](<SOP-TR-07 — Finalize Financial Responsibility.md>)|Yes|
|SOP-TR-08|[Cancel Finalization](<SOP-TR-08 — Cancel Finalization.md>)|Optional|
|SOP-TR-09|[Reopen Billing](<SOP-TR-09 — Reopen Billing.md>)|Optional|

---

#### [SOP-TR-03 — Merge Billing (Optional)](<SOP-TR-03 — Merge Billing.md>)

Menggabungkan Billing Set dari satu atau lebih Registrasi sumber ke Registrasi tujuan berdasarkan Merge Request yang masih Pending.

Contoh penggunaan:

- Rawat Jalan → Rawat Inap
- IGD → Rawat Inap
- Bayi → Ibu
- Koreksi Registrasi

Output utama:

- Billing Set telah terkonsolidasi.
- Financial Projection diregenerasi.
- Merge Request menjadi **Executed**.
- Accounting menerima permintaan Transfer Receivable.

---

#### [SOP-TR-04 — Financial Verification](<SOP-TR-04 — Financial Verification.md>)

Melakukan verifikasi terhadap Billing Set yang telah selesai dikonsolidasikan.

Verifikasi memastikan bahwa:

- seluruh Financial Charge telah terbentuk;
- tidak terdapat Billing yang hilang;
- tidak terdapat Billing ganda;
- hasil Merge Billing telah benar.

Apabila ditemukan ketidaksesuaian, proses dilanjutkan ke Financial Adjustment.

---

#### [SOP-TR-05 — Financial Adjustment (Optional)](<SOP-TR-05 — Financial Adjustment.md>)

Melakukan koreksi terhadap Financial Truth tanpa mengubah riwayat operasional Charge Source.

Contoh Financial Adjustment:

- Manual Charge
- Billing Correction
- Waive
- Subsidy
- Merge Billing Correction

Apabila koreksi memerlukan perubahan Financial Charge oleh Charge Source, Billing harus dibuka kembali melalui Reopen Billing.

---

#### [SOP-TR-06 — Financial Responsibility Allocation](<SOP-TR-06 — Financial Responsibility Allocation.md>)

Menetapkan tanggung jawab pembayaran kepada setiap Payer.

Sistem kemudian mendistribusikan Allocation tersebut secara proporsional ke seluruh Billing Set dan membentuk Financial Projection.

Output utama:

- Financial Responsibility telah lengkap.
- Financial Projection telah terbentuk.

---

#### [SOP-TR-07 — Finalize Financial Responsibility](<SOP-TR-07 — Finalize Financial Responsibility.md>)

Mengunci hasil Financial Responsibility Allocation sehingga Billing siap memasuki proses Settlement.

Setelah Finalization:

- Billing berstatus **FINALIZED**.
- Allocation menjadi terkunci.
- Financial Projection menjadi dasar Payment Settlement.

---

#### [SOP-TR-08 — Cancel Finalization (Optional)](<SOP-TR-08 — Cancel Finalization.md>)

Membatalkan status **FINALIZED** sehingga Financial Responsibility Allocation dapat diperbaiki kembali.

Cancel Finalization hanya diperbolehkan sebelum terdapat Payment Settlement.

Setelah pembatalan, proses kembali ke: **[SOP-TR-06 — Financial Responsibility Allocation](<SOP-TR-06 — Financial Responsibility Allocation.md>)**

---

#### [SOP-TR-09 — Reopen Billing (Optional)](<SOP-TR-09 — Reopen Billing.md>)

Membuka kembali Billing agar Charge Source dapat melakukan perubahan terhadap Financial Charge.

Reopen Billing hanya dilakukan apabila Financial Adjustment memerlukan perubahan operasional pada Charge Source.

Setelah seluruh perubahan selesai dilakukan, proses Financial Control diulang mulai dari: **[SOP-TR-02 — Close Bill](<SOP-TR-02 — Close Bill.md>)**

---

### 4.3 Settlement Phase

Settlement Phase merupakan tahap akhir Tata Rekening.

Pada fase ini tanggung jawab proses berpindah dari Verifikator kepada Kasir.

| Kode | SOP | Mandatory |
|------|-----|-----------|
| SOP-TR-10 | [Settlement Initiation](<SOP-TR-10 — Settlement Initiation.md>) | Yes |

---

#### [SOP-TR-10 — Settlement Initiation](<SOP-TR-10 — Settlement Initiation.md>)

Menyerahkan Billing yang telah berstatus **FINALIZED** kepada Kasir untuk memulai proses Payment Settlement.

Settlement Initiation merupakan batas akhir tanggung jawab Tata Rekening dan awal tanggung jawab Kasir.

Output utama:

- Billing siap diproses pada Payment Settlement.
- Financial Responsibility menjadi dasar pembayaran.
- Tata Rekening telah menyelesaikan proses Financial Control.

---

### 4.4 Workflow Summary

Urutan normal proses Tata Rekening adalah sebagai berikut:

```text
SOP-TR-01
Open Tata Rekening
        │
        ▼
SOP-TR-02
Close Bill
        │
        ▼
SOP-TR-03
Merge Billing (Optional)
        │
        ▼
SOP-TR-04
Financial Verification
        │
        ▼
SOP-TR-05
Financial Adjustment (Optional)
        │
        ▼
SOP-TR-06
Financial Responsibility Allocation
        │
        ▼
SOP-TR-07
Finalize Financial Responsibility
        │
        ▼
SOP-TR-10
Settlement Initiation
        │
        ▼
Payment Settlement
        │
        ▼
LUNAS
```

Exception workflow:

```text
Finalize Financial Responsibility
        │
        ▼
Cancel Finalization
        │
        ▼
Financial Responsibility Allocation
```

```text
Financial Adjustment
        │
        ▼
Reopen Billing
        │
        ▼
Close Bill
        │
        ▼
Merge Billing (Optional)
        │
        ▼
Financial Verification
```

Dokumen [SOP-TR-01 — Open Tata Rekening](<SOP-TR-01 — Open Tata Rekening.md>) sampai [SOP-TR-10 — Settlement Initiation](<SOP-TR-10 — Settlement Initiation.md>) menjadi referensi utama untuk setiap aktivitas pada workflow Tata Rekening.

---

## RELATED DOCUMENTS

| Document | Description |
|----------|-------------|
| **[01-context.md](01-context.md)** | Business Context |
| **[02-domain.md](02-domain.md)** | Domain Model |
| **[03-design.md](03-design.md)** | Architecture & Persistence |
| **[tata-rekening-domain-gap-analysis-report.md](tata-rekening-domain-gap-analysis-report.md)** | Domain gap analysis (implementation vs artifact) |
