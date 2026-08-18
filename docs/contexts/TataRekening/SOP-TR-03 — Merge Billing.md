# SOP-TR-03 — Merge Billing

## Tujuan

Menggabungkan Billing Set dari satu atau lebih Registrasi sumber ke Registrasi tujuan sehingga seluruh Financial Charge menjadi bagian dari Tata Rekening Registrasi tujuan sebelum dilakukan Financial Verification.

Merge Billing merupakan proses pemindahan kepemilikan Financial Responsibility antar Registrasi sesuai Merge Request yang masih berstatus **Pending**.

Contoh penggunaan:

- Rawat Jalan → Rawat Inap
- IGD → Rawat Inap
- Bayi → Ibu
- Koreksi Registrasi

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening Registrasi tujuan telah dibuka.
- Billing Registrasi tujuan berstatus **CLOSED**.
- Sistem menemukan satu atau lebih Merge Request berstatus **Pending**.
- Seluruh Registrasi sumber masih memiliki Billing yang dapat dipindahkan.
- Registrasi tujuan belum berstatus **FINALIZED**.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

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
13. Proses dilanjutkan ke **[SOP-TR-04 — Financial Verification](SOP-TR-04 — Financial Verification.md)**.

---

## Output

Billing Set Registrasi tujuan telah mencakup seluruh Billing hasil Merge dan Financial Responsibility telah dipindahkan secara konsisten.

---

## Postcondition

- Seluruh Billing Set Registrasi sumber telah berpindah ke Registrasi tujuan.
- Registrasi sumber tidak lagi memiliki Billing aktif.
- Financial Projection Registrasi tujuan telah diregenerasi.
- Accounting telah menerima permintaan Transfer Receivable.
- Merge Request berstatus **Executed**.
- Tata Rekening siap memasuki proses **Financial Verification**.

---

## Business Rules

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

## Exception

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

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |