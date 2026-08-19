# SOP-TR-09 — Reopen Billing

## Tujuan

Membuka kembali Billing yang telah berstatus **CLOSED** agar Charge Source dapat melakukan perubahan terhadap Financial Charge sebagai tindak lanjut dari hasil Financial Adjustment.

Reopen Billing digunakan apabila koreksi tidak dapat diselesaikan hanya melalui Financial Responsibility Allocation dan memerlukan perubahan pada Financial Charge yang menjadi tanggung jawab Charge Source.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **CLOSED**.
- Billing belum berstatus **FINALIZED**.
- Financial Adjustment telah menentukan bahwa diperlukan perubahan Financial Charge oleh Charge Source.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator meninjau hasil Financial Adjustment yang memerlukan perubahan Financial Charge.
2. Verifikator memilih perintah **Reopen Billing**.
3. Sistem memvalidasi bahwa Billing masih dapat dibuka kembali.
4. Verifikator memasukkan alasan Reopen Billing.
5. Sistem mengubah status Billing dari **CLOSED** menjadi **OPEN**.
6. Sistem kembali mengizinkan seluruh Charge Source membentuk, mengubah, atau membatalkan Financial Charge sesuai kewenangannya.
7. Sistem mencatat Audit Trail Reopen Billing.
8. Setelah seluruh perubahan Financial Charge selesai dilakukan oleh Charge Source, proses dilanjutkan ke **[SOP-TR-02 — Close Bill](SOP-TR-02 — Close Bill.md)**.

---

## Output

Billing berhasil dibuka kembali sehingga Charge Source dapat melakukan perubahan Financial Charge yang diperlukan.

---

## Postcondition

- Billing berstatus **OPEN**.
- Charge Source kembali dapat melakukan pembentukan maupun koreksi Financial Charge.
- Financial Control dihentikan sementara sampai Billing ditutup kembali.
- Tata Rekening menunggu proses **Close Bill** berikutnya sebelum melanjutkan Financial Control.

---

## Business Rules

- Reopen Billing hanya dapat dilakukan sebelum Billing berstatus **FINALIZED**.
- Reopen Billing hanya dilakukan apabila perubahan Financial Charge tidak dapat diselesaikan melalui Financial Responsibility Allocation.
- Reopen Billing tidak mengubah Financial Responsibility Allocation yang telah ada.
- Reopen Billing tidak mengubah Pricing Snapshot dari Financial Charge yang sudah ada.
- Financial Charge baru yang dihasilkan setelah Reopen Billing mengikuti mekanisme pembentukan Financial Charge yang berlaku pada Charge Source.
- Setelah Reopen Billing, seluruh proses Financial Control wajib diulang mulai dari:
  - **[SOP-TR-02 — Close Bill](SOP-TR-02 — Close Bill.md)**
  - **[SOP-TR-03 — Merge Billing](SOP-TR-03 — Merge Billing.md)** (apabila terdapat Merge Request baru atau perubahan Billing Set hasil Merge)
  - **[SOP-TR-04 — Financial Verification](SOP-TR-04 — Financial Verification.md)**
  - dan tahapan berikutnya hingga Finalization.
- Setiap Reopen Billing wajib menghasilkan Audit Trail yang mencatat:
  - Registrasi
  - Waktu Reopen
  - Verifikator
  - Alasan Reopen Billing

---

## Exception

- Billing masih berstatus **OPEN**.
- Billing telah berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Tidak terdapat kebutuhan perubahan Financial Charge.
- Verifikator tidak memiliki hak akses.
- Alasan Reopen Billing belum diisi.
- Validasi Reopen Billing gagal.

---

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |