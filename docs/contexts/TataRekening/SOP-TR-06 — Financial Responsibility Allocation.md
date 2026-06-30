# SOP-TR-06 — Financial Responsibility Allocation

## Tujuan

Menetapkan tanggung jawab pembayaran (Financial Responsibility) kepada setiap Payer serta mendistribusikannya secara proporsional ke seluruh Billing Set sebelum dilakukan Finalize Financial Responsibility.

Financial Responsibility Allocation memastikan seluruh Financial Charge telah memiliki penanggung jawab pembayaran yang jelas dan total alokasi sama dengan total tagihan Registrasi.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **CLOSED**.
- Financial Verification telah selesai.
- Seluruh Financial Adjustment telah selesai atau tidak diperlukan.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator meninjau Billing Set yang akan dialokasikan.
2. Verifikator menentukan komposisi tanggung jawab pembayaran untuk setiap Payer.
3. Sistem memvalidasi bahwa seluruh Billing telah memiliki alokasi yang lengkap.
4. Sistem menghitung distribusi Financial Responsibility secara proporsional ke seluruh Billing Set.
5. Sistem membentuk atau meregenerasi Financial Projection berdasarkan hasil Allocation.
6. Sistem menampilkan hasil Financial Responsibility Allocation.
7. Verifikator memverifikasi hasil Allocation.
8. Apabila Allocation telah sesuai, proses dilanjutkan ke **SOP-TR-07 — Finalize Financial Responsibility**.
9. Apabila Allocation belum sesuai, Verifikator melakukan perbaikan Allocation hingga seluruh Financial Responsibility tervalidasi.

---

## Output

Financial Responsibility Allocation berhasil ditetapkan dan Financial Projection telah terbentuk sesuai hasil Allocation.

---

## Postcondition

- Seluruh Billing Set telah memiliki alokasi Financial Responsibility.
- Total Allocation sama dengan total Billing Registrasi.
- Financial Projection telah diregenerasi sesuai Allocation.
- Tata Rekening siap memasuki proses **Finalize Financial Responsibility**.

---

## Business Rules

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

## Exception

- Billing telah berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Total Allocation tidak sama dengan total Billing Registrasi.
- Masih terdapat Billing yang belum memiliki Payer.
- Gagal meregenerasi Financial Projection.
- Verifikator tidak memiliki hak akses.