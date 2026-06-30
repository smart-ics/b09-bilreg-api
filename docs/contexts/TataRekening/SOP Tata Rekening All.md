# SOP TATA REKENING

## Daftar SOP (SOP-TR-01 s/d SOP-TR-10)

| Kode | SOP | Fase | Wajib |
|------|-----|------|-------|
| [SOP-TR-01](#sop-tr-01) | Open Tata Rekening | Operational Freeze | Ya |
| [SOP-TR-02](#sop-tr-02) | Close Bill | Operational Freeze | Ya |
| [SOP-TR-03](#sop-tr-03) | Merge Billing | Financial Control | Opsional |
| [SOP-TR-04](#sop-tr-04) | Financial Verification | Financial Control | Ya |
| [SOP-TR-05](#sop-tr-05) | Financial Adjustment | Financial Control | Opsional |
| [SOP-TR-06](#sop-tr-06) | Financial Responsibility Allocation | Financial Control | Ya |
| [SOP-TR-07](#sop-tr-07) | Finalize Financial Responsibility | Financial Control | Ya |
| [SOP-TR-08](#sop-tr-08) | Cancel Finalization | Financial Control | Opsional |
| [SOP-TR-09](#sop-tr-09) | Reopen Billing | Financial Control | Opsional |
| [SOP-TR-10](#sop-tr-10) | Settlement Initiation | Settlement | Ya |

## SOP-TR-01 — Open Tata Rekening

### Tujuan

Membuka Tata Rekening untuk suatu Registrasi sebagai awal proses Financial Control serta mempersiapkan seluruh informasi yang diperlukan sebelum dilakukan proses Close Bill, Merge Billing, dan Financial Verification.

---

### Aktor

    **Verifikator**

---

### Precondition

- Registrasi telah dibuat.

- Registrasi masih aktif.

- Billing berstatus **OPEN**.

- Registrasi belum berstatus **LUNAS**.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator memilih Registrasi yang akan diproses.

2. Sistem memuat Tata Rekening Registrasi.

3. Sistem memuat seluruh Billing Set Registrasi.

4. Sistem memuat Financial Projection yang masih berlaku (apabila ada).

5. Sistem memuat informasi Payer.

6. Sistem mencari Merge Request yang masih berstatus **Pending**, dengan kondisi:

   - **TargetReg = Registrasi yang sedang dibuka**, atau

   - **TargetReg masih kosong**, tetapi **PatientId** sama dengan Registrasi yang sedang dibuka.

7. Sistem menampilkan Ringkasan Tata Rekening beserta daftar Merge Request yang ditemukan (apabila ada).

8. Proses dilanjutkan ke **[SOP-TR-02 — Close Bill](#sop-tr-02)**.

---

### Output

Tata Rekening berhasil dibuka dan seluruh informasi Financial Responsibility siap diproses.

Apabila terdapat Merge Request yang masih Pending, sistem menampilkannya sebagai kandidat Merge Billing.

---

### Postcondition

- Tata Rekening berada pada Registrasi yang dipilih.

- Seluruh Billing Set telah dimuat.

- Financial Projection telah dimuat.

- Daftar Merge Request yang relevan telah dimuat.

- Tata Rekening siap memasuki proses **Close Bill**.

---

### Business Rules

- Membuka Tata Rekening tidak mengubah Billing Set.

- Membuka Tata Rekening tidak mengubah Financial Responsibility.

- Membuka Tata Rekening tidak mengubah Billing Lifecycle.

- Membuka Tata Rekening tidak mengubah status Merge Request.

- Seluruh data yang ditampilkan merupakan kondisi terkini pada saat Tata Rekening dibuka.

---

### Exception

- Registrasi tidak ditemukan.

- Registrasi telah dibatalkan.

- Registrasi telah berstatus **LUNAS**.

- Verifikator tidak memiliki hak akses.

- Data Tata Rekening gagal dimuat.

---

## SOP-TR-02 — Close Bill

### Tujuan

Menutup Billing untuk menghentikan pembentukan Financial Charge dari seluruh Charge Source sehingga Billing berada dalam kondisi stabil dan siap memasuki proses Merge Billing maupun Financial Verification.

---

### Aktor

    **Verifikator**

---

### Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **OPEN**.

- Verifikator telah meninjau seluruh Billing Set.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator meninjau kesiapan Billing untuk ditutup.

2. Verifikator memastikan seluruh transaksi operasional yang menghasilkan Financial Charge telah selesai.

3. Verifikator memilih perintah **Close Bill**.

4. Sistem memvalidasi bahwa Billing dapat ditutup.

5. Sistem mengubah status Billing dari **OPEN** menjadi **CLOSED**.

6. Sistem menghentikan pembentukan Financial Charge baru dari seluruh Charge Source.

7. Apabila terdapat Merge Request berstatus **Pending**, proses dilanjutkan ke **[SOP-TR-03 — Merge Billing](#sop-tr-03)**.

8. Apabila tidak terdapat Merge Request, proses langsung dilanjutkan ke **[SOP-TR-04 — Financial Verification](#sop-tr-04)**.


---

### Output

Billing berhasil ditutup dan berada pada kondisi stabil untuk memasuki proses Financial Control.

---

### Postcondition

- Billing berstatus **CLOSED**.

- Charge Source tidak dapat lagi menghasilkan Financial Charge baru.

- Billing Set berada pada kondisi operasional yang stabil.

- Billing siap dilakukan Merge Billing (apabila diperlukan) atau langsung memasuki Financial Verification.

---

### Business Rules

- Close Bill merupakan batas antara proses operasional dan proses Financial Control.

- Close Bill tidak mengubah Billing Set.

- Close Bill tidak melakukan Merge Billing.

- Close Bill tidak melakukan Financial Verification.

- Close Bill tidak melakukan Financial Adjustment.

- Close Bill tidak melakukan Financial Responsibility Allocation.

- Close Bill tidak melakukan Finalize Financial Responsibility.

- Close Bill tidak menghasilkan Settlement.

- Apabila setelah Close Bill masih diperlukan Financial Charge baru, Billing harus melalui **[SOP-TR-09 — Reopen Billing](#sop-tr-09)** sebelum Charge Source dapat menambahkan Financial Charge kembali.

---

### Exception

- Billing telah berstatus **CLOSED**.

- Billing telah berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Verifikator tidak memiliki hak akses.

- Masih terdapat transaksi operasional yang belum selesai sehingga Billing belum dapat ditutup.

- Validasi Close Bill gagal.

---

## SOP-TR-03 — Merge Billing

### Tujuan

Menggabungkan Billing Set dari satu atau lebih Registrasi sumber ke Registrasi tujuan sehingga seluruh Financial Charge menjadi bagian dari Tata Rekening Registrasi tujuan sebelum dilakukan Financial Verification.

Merge Billing merupakan proses pemindahan kepemilikan Financial Responsibility antar Registrasi sesuai Merge Request yang masih berstatus **Pending**.

Contoh penggunaan:

- Rawat Jalan → Rawat Inap

- IGD → Rawat Inap

- Bayi → Ibu

- Koreksi Registrasi

---

### Aktor

   **Verifikator**

---

### Precondition

- Tata Rekening Registrasi tujuan telah dibuka.

- Billing Registrasi tujuan berstatus **CLOSED**.

- Sistem menemukan satu atau lebih Merge Request berstatus **Pending**.

- Seluruh Registrasi sumber masih memiliki Billing yang dapat dipindahkan.

- Registrasi tujuan belum berstatus **FINALIZED**.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Sistem menampilkan daftar Merge Request yang dapat dieksekusi.

2. Verifikator memilih Merge Request yang akan diproses.

3. Sistem memuat Billing Set dari seluruh Registrasi sumber.

4. Sistem menampilkan pratinjau hasil Merge Billing.

5. Sistem memvalidasi seluruh Business Rules Merge Billing.

6. Verifikator mengonfirmasi proses Merge Billing.

7. Sistem memindahkan seluruh Billing Set dari Registrasi sumber ke Registrasi tujuan.

8. Sistem memperbarui kepemilikan Billing Set ke Registrasi tujuan.

9. Sistem meregenerasi Financial Projection Registrasi tujuan.

10. Sistem mengirim permintaan **Transfer Receivable** kepada Accounting untuk memindahkan kepemilikan Accounts Receivable dari Registrasi sumber ke Registrasi tujuan.

11. Sistem mengubah status Merge Request menjadi **Executed**.

12. Sistem mencatat Audit Trail Merge Billing.

13. Proses dilanjutkan ke **[SOP-TR-04 — Financial Verification](#sop-tr-04)**.

---

### Output

Billing Set Registrasi tujuan telah mencakup seluruh Billing hasil Merge dan Financial Responsibility telah dipindahkan secara konsisten.

---

### Postcondition

- Seluruh Billing Set Registrasi sumber telah berpindah ke Registrasi tujuan.

- Registrasi sumber tidak lagi memiliki Billing aktif.

- Financial Projection Registrasi tujuan telah diregenerasi.

- Accounting telah menerima permintaan Transfer Receivable.

- Merge Request berstatus **Executed**.

- Tata Rekening siap memasuki proses **Financial Verification**.

---

### Business Rules

- Merge Billing hanya dapat dilakukan apabila Billing Registrasi tujuan telah berstatus **CLOSED**.

- Merge Billing hanya dapat dilakukan sebelum Registrasi tujuan berstatus **FINALIZED**.

- Merge Billing hanya dapat dilakukan terhadap Merge Request yang berstatus **Pending**.

- Registrasi sumber tidak boleh berstatus **LUNAS**.

- Billing yang telah diselesaikan (settled) tidak dapat di-Merge.

- Merge Billing tidak mengubah Financial Charge.

- Merge Billing tidak mengubah Pricing Snapshot.

- Merge Billing tidak mengubah Accounting Snapshot.

- Merge Billing hanya memindahkan kepemilikan Financial Responsibility dari Registrasi sumber ke Registrasi tujuan.

- Seluruh Billing Set Registrasi sumber harus berpindah sebagai satu kesatuan.

- Setiap Merge Billing wajib menghasilkan Audit Trail yang mencatat:

  - Merge Request

  - Registrasi sumber

  - Registrasi tujuan

  - Waktu eksekusi

  - Verifikator

- Apabila Transfer Receivable gagal, seluruh proses Merge Billing harus dibatalkan (rollback).

---

### Exception

- Merge Request tidak ditemukan.

- Merge Request telah berstatus **Executed**.

- Merge Request telah dibatalkan.

- Registrasi sumber tidak ditemukan.

- Registrasi tujuan tidak ditemukan.

- Registrasi sumber telah berstatus **LUNAS**.

- Registrasi tujuan telah berstatus **FINALIZED**.

- Billing Registrasi tujuan belum berstatus **CLOSED**.

- Registrasi sumber tidak memiliki Billing yang dapat dipindahkan.

- Gagal memperbarui kepemilikan Billing.

- Gagal meregenerasi Financial Projection.

- Gagal melakukan **Transfer Receivable** pada Accounting.

- Verifikator tidak memiliki hak akses.

---

## SOP-TR-04 — Financial Verification

### Tujuan

Memastikan bahwa seluruh Billing Set dan Financial Responsibility pada Registrasi telah lengkap, benar, dan konsisten sebelum dilakukan Financial Responsibility Allocation.

Financial Verification merupakan proses validasi akhir terhadap Billing Set yang telah berada pada kondisi operasional yang stabil, termasuk hasil Merge Billing apabila dilakukan.

---

### Aktor

     **Verifikator**

---

### Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **CLOSED**.

- Proses Merge Billing telah selesai atau tidak diperlukan.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator meninjau seluruh Billing Set pada Registrasi.

2. Verifikator memverifikasi bahwa seluruh Financial Charge telah terbentuk secara lengkap.

3. Verifikator memverifikasi bahwa tidak terdapat Financial Charge yang hilang, ganda, atau tidak semestinya.

4. Verifikator memverifikasi bahwa seluruh hasil Merge Billing (apabila ada) telah berpindah secara benar ke Registrasi tujuan.

5. Verifikator memverifikasi bahwa Billing Set telah sesuai dengan dokumen pendukung dan ketentuan yang berlaku.

6. Apabila ditemukan ketidaksesuaian, proses dilanjutkan ke **[SOP-TR-05 — Financial Adjustment](#sop-tr-05)**.

7. Apabila seluruh Billing Set telah dinyatakan benar, proses dilanjutkan ke **[SOP-TR-06 — Financial Responsibility Allocation](#sop-tr-06)**.

---

### Output

Status Financial Verification telah ditentukan sebagai:

- **Valid**, atau

- **Memerlukan Financial Adjustment**.

---

### Postcondition

- Seluruh Billing Set telah diverifikasi.

- Ketidaksesuaian Financial Responsibility telah diidentifikasi apabila ada.

- Tata Rekening siap melanjutkan ke Financial Adjustment atau Financial Responsibility Allocation.

---

## Business Rules

- Financial Verification hanya dapat dilakukan setelah Billing berstatus **CLOSED**.

- Apabila terdapat Merge Request, seluruh Merge Billing harus telah selesai sebelum Financial Verification dimulai.

- Financial Verification tidak menambah, mengubah, atau menghapus Financial Charge.

- Financial Verification tidak mengubah Billing Set.

- Financial Verification tidak mengubah Financial Responsibility Allocation.

- Financial Verification tidak mengubah Billing Lifecycle.

- Apabila ditemukan ketidaksesuaian yang memerlukan perubahan Financial Responsibility, proses harus dilanjutkan ke **[SOP-TR-05 — Financial Adjustment](#sop-tr-05)** sebelum Financial Responsibility Allocation dapat dilakukan.

---

### Exception

- Billing belum berstatus **CLOSED**.

- Masih terdapat Merge Request yang berstatus **Pending**.

- Billing telah berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Data Billing Set tidak dapat dimuat.

- Verifikator tidak memiliki hak akses.

---

## SOP-TR-05 — Financial Adjustment

### Tujuan

Melakukan koreksi terhadap Billing Set dan Financial Responsibility agar mencerminkan Financial Truth yang sebenarnya sebelum dilakukan Financial Responsibility Allocation.

Financial Adjustment digunakan untuk menyelesaikan ketidaksesuaian yang ditemukan selama Financial Verification tanpa mengubah riwayat operasional yang menjadi tanggung jawab Charge Source.

---

### Aktor

     **Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **CLOSED**.

- Financial Verification telah selesai dilakukan.

- Ditemukan ketidaksesuaian yang memerlukan Financial Adjustment.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator mengidentifikasi Billing atau Financial Responsibility yang memerlukan koreksi.

2. Verifikator menentukan jenis Financial Adjustment yang akan dilakukan.

3. Sistem memvalidasi bahwa Financial Adjustment diperbolehkan.

4. Sistem menerapkan Financial Adjustment pada Billing Set.

5. Sistem meregenerasi Financial Projection apabila diperlukan.

6. Apabila Financial Adjustment memerlukan pembentukan atau perubahan Financial Charge oleh Charge Source, proses dilanjutkan ke **[SOP-TR-09 — Reopen Billing](#sop-tr-09)**.

7. Apabila Financial Adjustment telah selesai dan tidak memerlukan perubahan dari Charge Source, proses dilanjutkan ke **[SOP-TR-06 — Financial Responsibility Allocation](#sop-tr-06)**.

---

### Output

Financial Adjustment berhasil diterapkan pada Billing Set Registrasi.

---

## Postcondition

- Billing Set telah diperbarui sesuai hasil Financial Adjustment.

- Financial Projection telah diregenerasi apabila diperlukan.

- Billing tetap berstatus **CLOSED**, kecuali proses dilanjutkan ke Reopen Billing.

- Tata Rekening siap melanjutkan ke Reopen Billing atau Financial Responsibility Allocation.

---

## Business Rules

- Financial Adjustment hanya dapat dilakukan sebelum Billing berstatus **FINALIZED**.

- Financial Adjustment tidak mengubah riwayat operasional pelayanan.

- Koreksi terhadap aktivitas operasional tetap menjadi tanggung jawab Charge Source.

- Financial Adjustment dapat berupa:

  - Manual Charge.

  - Billing Correction.

  - Waive.

  - Subsidy.

  - Merge Billing Correction.

- Financial Adjustment dapat meregenerasi Financial Projection.

- Billing hanya boleh dibuka kembali melalui **[SOP-TR-09 — Reopen Billing](#sop-tr-09)** apabila Financial Adjustment memerlukan pembentukan atau perubahan Financial Charge oleh Charge Source.

- Setelah Reopen Billing selesai, proses Financial Control harus diulang mulai dari **[SOP-TR-02 — Close Bill](#sop-tr-02)**.

---

### Exception

- Billing telah berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Financial Adjustment tidak valid.

- Gagal memperbarui Billing Set.

- Gagal meregenerasi Financial Projection.

- Verifikator tidak memiliki hak akses.

---

## SOP-TR-06 — Financial Responsibility Allocation

### Tujuan

Menetapkan tanggung jawab pembayaran (Financial Responsibility) kepada setiap Payer serta mendistribusikannya secara proporsional ke seluruh Billing Set sebelum dilakukan Finalize Financial Responsibility.

Financial Responsibility Allocation memastikan seluruh Financial Charge telah memiliki penanggung jawab pembayaran yang jelas dan total alokasi sama dengan total tagihan Registrasi.

---

### Aktor

     **Verifikator**

---

### Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **CLOSED**.

- Financial Verification telah selesai.

- Seluruh Financial Adjustment telah selesai atau tidak diperlukan.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator meninjau Billing Set yang akan dialokasikan.

2. Verifikator menentukan komposisi tanggung jawab pembayaran untuk setiap Payer.

3. Sistem memvalidasi bahwa seluruh Billing telah memiliki alokasi yang lengkap.

4. Sistem menghitung distribusi Financial Responsibility secara proporsional ke seluruh Billing Set.

5. Sistem membentuk atau meregenerasi Financial Projection berdasarkan hasil Allocation.

6. Sistem menampilkan hasil Financial Responsibility Allocation.

7. Verifikator memverifikasi hasil Allocation.

8. Apabila Allocation telah sesuai, proses dilanjutkan ke **[SOP-TR-07 — Finalize Financial Responsibility](#sop-tr-07)**.

9. Apabila Allocation belum sesuai, Verifikator melakukan perbaikan Allocation hingga seluruh Financial Responsibility tervalidasi.

---

### Output

Financial Responsibility Allocation berhasil ditetapkan dan Financial Projection telah terbentuk sesuai hasil Allocation.

---

### Postcondition

- Seluruh Billing Set telah memiliki alokasi Financial Responsibility.

- Total Allocation sama dengan total Billing Registrasi.

- Financial Projection telah diregenerasi sesuai Allocation.

- Tata Rekening siap memasuki proses **Finalize Financial Responsibility**.

---

### Business Rules

- Financial Responsibility Allocation hanya dapat dilakukan sebelum Billing berstatus **FINALIZED**.

- Seluruh Billing harus memiliki Payer yang jelas.

- Total Allocation harus sama dengan total Billing Registrasi.

- Financial Responsibility dapat dialokasikan kepada satu atau lebih Payer sesuai kebijakan rumah sakit.

- Distribusi Allocation ke Billing Set dilakukan secara proporsional oleh sistem.

- Setiap perubahan Allocation wajib diikuti dengan regenerasi Financial Projection.

- Financial Responsibility Allocation tidak mengubah Billing Set.

- Financial Responsibility Allocation tidak mengubah Financial Charge.

- Financial Responsibility Allocation tidak mengubah Pricing Snapshot.

- Financial Responsibility Allocation tidak mengubah Accounting Snapshot.

---

### Exception

- Billing telah berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Total Allocation tidak sama dengan total Billing Registrasi.

- Masih terdapat Billing yang belum memiliki Payer.

- Gagal meregenerasi Financial Projection.

- Verifikator tidak memiliki hak akses.

---

## SOP-TR-07 — Finalize Financial Responsibility

### Tujuan

Mengunci hasil Financial Responsibility Allocation sehingga Billing siap memasuki proses Settlement.

Finalize Financial Responsibility merupakan batas akhir proses Financial Control, dimana kepemilikan Financial Responsibility telah ditetapkan dan tidak dapat diubah tanpa melalui proses Cancel Finalization.

---

### Aktor

     **Verifikator**

---

### Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **CLOSED**.

- Financial Verification telah selesai.

- Seluruh Financial Adjustment telah selesai atau tidak diperlukan.

- Financial Responsibility Allocation telah selesai.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator meninjau kembali hasil Financial Responsibility Allocation.

2. Sistem memvalidasi bahwa seluruh Billing telah memiliki Financial Responsibility yang lengkap.

3. Sistem memvalidasi bahwa total Allocation sama dengan total Billing Registrasi.

4. Sistem memvalidasi bahwa Financial Projection telah berhasil diregenerasi.

5. Verifikator memilih perintah **Finalize Financial Responsibility**.

6. Sistem mengubah status Billing menjadi **FINALIZED**.

7. Sistem mengunci Financial Responsibility Allocation.

8. Sistem menjadikan Financial Projection sebagai dasar Settlement.

9. Proses dilanjutkan ke **[SOP-TR-08 — Cancel Finalization](#sop-tr-08)** apabila diperlukan, atau **[SOP-TR-10 — Settlement Initiation](#sop-tr-10)**.

---

### Output

Billing berhasil difinalisasi dan siap diproses untuk Settlement.

---

### Postcondition

- Billing berstatus **FINALIZED**.

- Financial Responsibility Allocation telah terkunci.

- Financial Projection menjadi dasar Settlement.

- Billing siap memasuki proses Settlement.

- Perubahan Financial Responsibility tidak diperbolehkan kecuali melalui proses **Cancel Finalization**.

---

### Business Rules

- Finalization hanya dapat dilakukan setelah Financial Verification selesai.

- Seluruh Billing harus telah memiliki Financial Responsibility.

- Total Allocation harus sama dengan total Billing Registrasi.

- Financial Projection harus telah berhasil diregenerasi sebelum Finalization dilakukan.

- Setelah Finalization, Financial Responsibility Allocation tidak dapat diubah.

- Setelah Finalization, Billing Set tidak dapat diubah kecuali melalui proses **Cancel Finalization** atau **Reopen Billing** sesuai Business Rules yang berlaku.

- Finalization tidak melakukan Settlement maupun pembayaran.

- Finalization tidak mengubah Financial Charge, Pricing Snapshot, maupun Accounting Snapshot.

---

### Exception

- Billing belum berstatus **CLOSED**.

- Financial Verification belum selesai.

- Financial Responsibility Allocation belum lengkap.

- Total Allocation tidak sama dengan total Billing Registrasi.

- Financial Projection gagal dibentuk.

- Billing telah berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Verifikator tidak memiliki hak akses.

---

## SOP-TR-08 — Cancel Finalization

### Tujuan

Membatalkan status **FINALIZED** sehingga Financial Responsibility Allocation dapat diperbaiki kembali sebelum dilakukan Settlement.

Cancel Finalization digunakan apabila setelah Finalization ditemukan kebutuhan untuk mengubah Financial Responsibility tanpa mengubah riwayat operasional maupun Financial Charge.

---

### Aktor

     **Verifikator**

---

### Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **FINALIZED**.

- Belum terdapat Payment Settlement.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator memilih perintah **Cancel Finalization**.

2. Sistem memvalidasi bahwa Billing masih dapat dibatalkan Finalization-nya.

3. Sistem memastikan belum terdapat Payment Settlement.

4. Verifikator memasukkan alasan Cancel Finalization.

5. Sistem mengubah status Billing dari **FINALIZED** menjadi **CLOSED**.

6. Sistem membuka kembali Financial Responsibility Allocation.

7. Sistem mempertahankan Billing Set dan Financial Charge tanpa perubahan.

8. Sistem mencatat Audit Trail yang berisi alasan pembatalan, waktu, dan identitas Verifikator.

9. Proses dilanjutkan ke **[SOP-TR-06 — Financial Responsibility Allocation](#sop-tr-06)**.

---

### Output

Status Finalization berhasil dibatalkan dan Billing kembali ke status **CLOSED** sehingga Financial Responsibility dapat dialokasikan kembali.

---

### Postcondition

- Billing berstatus **CLOSED**.

- Financial Responsibility Allocation tidak lagi terkunci.

- Financial Projection tetap menjadi representasi Allocation terakhir sampai dilakukan perubahan Allocation.

- Tata Rekening siap dilakukan Financial Responsibility Allocation kembali.

---

### Business Rules

- Cancel Finalization hanya dapat dilakukan sebelum terdapat Payment Settlement.

- Cancel Finalization tidak mengubah Billing Set.

- Cancel Finalization tidak mengubah Financial Charge.

- Cancel Finalization tidak mengubah Pricing Snapshot.

- Cancel Finalization tidak mengubah Accounting Snapshot.

- Financial Responsibility Allocation dapat diubah kembali setelah Cancel Finalization.

- Financial Projection hanya diregenerasi apabila terjadi perubahan Financial Responsibility Allocation.

- Setiap Cancel Finalization wajib menghasilkan Audit Trail yang mencatat:

  - Registrasi

  - Waktu pembatalan

  - Verifikator

  - Alasan pembatalan

- Setelah Allocation selesai diperbarui, Billing harus melalui **[SOP-TR-07 — Finalize Financial Responsibility](#sop-tr-07)** kembali sebelum dapat memasuki Settlement.

---

### Exception

- Billing belum berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Payment Settlement telah dilakukan.

- Verifikator tidak memiliki hak akses.

- Alasan Cancel Finalization belum diisi.

- Validasi Cancel Finalization gagal.

---

## SOP-TR-09 — Reopen Billing

### Tujuan

Membuka kembali Billing yang telah berstatus **CLOSED** agar Charge Source dapat melakukan perubahan terhadap Financial Charge sebagai tindak lanjut dari hasil Financial Adjustment.

Reopen Billing digunakan apabila koreksi tidak dapat diselesaikan hanya melalui Financial Responsibility Allocation dan memerlukan perubahan pada Financial Charge yang menjadi tanggung jawab Charge Source.

---

### Aktor

     **Verifikator**

---

### Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **CLOSED**.

- Billing belum berstatus **FINALIZED**.

- Financial Adjustment telah menentukan bahwa diperlukan perubahan Financial Charge oleh Charge Source.

- Verifikator memiliki hak akses Tata Rekening.

---

=### Workflow

1. Verifikator meninjau hasil Financial Adjustment yang memerlukan perubahan Financial Charge.

2. Verifikator memilih perintah **Reopen Billing**.

3. Sistem memvalidasi bahwa Billing masih dapat dibuka kembali.

4. Verifikator memasukkan alasan Reopen Billing.

5. Sistem mengubah status Billing dari **CLOSED** menjadi **OPEN**.

6. Sistem kembali mengizinkan seluruh Charge Source membentuk, mengubah, atau membatalkan Financial Charge sesuai kewenangannya.

7. Sistem mencatat Audit Trail Reopen Billing.

8. Setelah seluruh perubahan Financial Charge selesai dilakukan oleh Charge Source, proses dilanjutkan ke **[SOP-TR-02 — Close Bill](#sop-tr-02)**.

---

### Output

Billing berhasil dibuka kembali sehingga Charge Source dapat melakukan perubahan Financial Charge yang diperlukan.

---

### Postcondition

- Billing berstatus **OPEN**.

- Charge Source kembali dapat melakukan pembentukan maupun koreksi Financial Charge.

- Financial Control dihentikan sementara sampai Billing ditutup kembali.

- Tata Rekening menunggu proses **Close Bill** berikutnya sebelum melanjutkan Financial Control.

---

### Business Rules

- Reopen Billing hanya dapat dilakukan sebelum Billing berstatus **FINALIZED**.

- Reopen Billing hanya dilakukan apabila perubahan Financial Charge tidak dapat diselesaikan melalui Financial Responsibility Allocation.

- Reopen Billing tidak mengubah Financial Responsibility Allocation yang telah ada.

- Reopen Billing tidak mengubah Pricing Snapshot dari Financial Charge yang sudah ada.

- Financial Charge baru yang dihasilkan setelah Reopen Billing mengikuti mekanisme pembentukan Financial Charge yang berlaku pada Charge Source.

- Setelah Reopen Billing, seluruh proses Financial Control wajib diulang mulai dari:

  - **[SOP-TR-02 — Close Bill](#sop-tr-02)**

  - **[SOP-TR-03 — Merge Billing](#sop-tr-03)** (apabila terdapat Merge Request baru atau perubahan Billing Set hasil Merge)

  - **[SOP-TR-04 — Financial Verification](#sop-tr-04)**

  - dan tahapan berikutnya hingga Finalization.

- Setiap Reopen Billing wajib menghasilkan Audit Trail yang mencatat:

  - Registrasi

  - Waktu Reopen

  - Verifikator

  - Alasan Reopen Billing

---

### Exception

- Billing masih berstatus **OPEN**.

- Billing telah berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Tidak terdapat kebutuhan perubahan Financial Charge.

- Verifikator tidak memiliki hak akses.

- Alasan Reopen Billing belum diisi.

- Validasi Reopen Billing gagal.

---

## SOP-TR-10 — Settlement Initiation


### Tujuan

Menyerahkan Billing yang telah berstatus **FINALIZED** kepada Kasir untuk memulai proses **Payment Settlement**.

Settlement Initiation merupakan batas akhir tanggung jawab Tata Rekening sebagai pengelola **Financial Control** dan awal tanggung jawab Kasir sebagai pelaksana **Payment Settlement**.

---

### Aktor

     **Verifikator**

---

### Precondition

- Tata Rekening telah dibuka.

- Billing berstatus **FINALIZED**.

- Financial Responsibility Allocation telah dikunci.

- Financial Projection telah tersedia.

- Belum terdapat Payment Settlement.

- Verifikator memiliki hak akses Tata Rekening.

---

### Workflow

1. Verifikator meninjau kembali hasil Finalize Financial Responsibility.

2. Sistem memvalidasi bahwa Billing telah memenuhi seluruh persyaratan Settlement.

3. Sistem memastikan Financial Projection tersedia sebagai dasar Payment Settlement.

4. Verifikator memilih perintah **Settlement Initiation**.

5. Sistem menyerahkan otoritas Settlement kepada Kasir.

6. Sistem menandai Tata Rekening telah menyelesaikan proses Financial Control.

7. Proses dilanjutkan ke SOP Kasir untuk **Payment Settlement**.

---

### Output

Billing berhasil diserahkan kepada Kasir dan siap diproses pada tahap Payment Settlement.

---

### Postcondition

- Billing tetap berstatus **FINALIZED**.

- Financial Responsibility Allocation tetap terkunci.

- Financial Projection menjadi dasar Payment Settlement.

- Tata Rekening tidak lagi menjadi proses aktif pada Registrasi tersebut.

- Otoritas proses berpindah kepada Kasir.

---

### Business Rules

- Settlement Initiation hanya dapat dilakukan terhadap Billing yang berstatus **FINALIZED**.

- Settlement Initiation tidak mengubah Billing Set.

- Settlement Initiation tidak mengubah Financial Charge.

- Settlement Initiation tidak mengubah Financial Responsibility Allocation.

- Settlement Initiation tidak mengubah Financial Projection.

- Settlement Initiation tidak melakukan proses pembayaran.

- Setelah Settlement Initiation, seluruh proses pembayaran menjadi tanggung jawab Kasir sesuai SOP Kasir.

- Apabila sebelum Payment Settlement diperlukan perubahan Financial Responsibility, proses harus diawali dengan **[SOP-TR-08 — Cancel Finalization](#sop-tr-08)**.

---

### Exception

- Billing belum berstatus **FINALIZED**.

- Billing telah berstatus **LUNAS**.

- Financial Projection belum tersedia atau tidak valid.

- Payment Settlement telah dimulai.

- Verifikator tidak memiliki hak akses.

- Gagal menyerahkan Billing kepada Kasir.

---
