# SOP-TR-05 — Financial Adjustment

## Tujuan

Melakukan koreksi terhadap Billing Set dan Financial Responsibility agar mencerminkan Financial Truth yang sebenarnya sebelum dilakukan Financial Responsibility Allocation.

Financial Adjustment digunakan untuk menyelesaikan ketidaksesuaian yang ditemukan selama Financial Verification tanpa mengubah riwayat operasional yang menjadi tanggung jawab Charge Source.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **CLOSED**.
- Financial Verification telah selesai dilakukan.
- Ditemukan ketidaksesuaian yang memerlukan Financial Adjustment.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator mengidentifikasi Billing atau Financial Responsibility yang memerlukan koreksi.
2. Verifikator menentukan jenis Financial Adjustment yang akan dilakukan.
3. Sistem memvalidasi bahwa Financial Adjustment diperbolehkan.
4. Sistem menerapkan Financial Adjustment pada Billing Set.
5. Sistem meregenerasi Financial Projection apabila diperlukan.
6. Apabila Financial Adjustment memerlukan pembentukan atau perubahan Financial Charge oleh Charge Source, proses dilanjutkan ke **[SOP-TR-09 — Reopen Billing](SOP-TR-09 — Reopen Billing.md)**.
7. Apabila Financial Adjustment telah selesai dan tidak memerlukan perubahan dari Charge Source, proses dilanjutkan ke **[SOP-TR-06 — Financial Responsibility Allocation](SOP-TR-06 — Financial Responsibility Allocation.md)**.

---

## Output

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
- Billing hanya boleh dibuka kembali melalui **[SOP-TR-09 — Reopen Billing](SOP-TR-09 — Reopen Billing.md)** apabila Financial Adjustment memerlukan pembentukan atau perubahan Financial Charge oleh Charge Source.
- Setelah Reopen Billing selesai, proses Financial Control harus diulang mulai dari **[SOP-TR-02 — Close Bill](SOP-TR-02 — Close Bill.md)**.

---

## Exception

- Billing telah berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Financial Adjustment tidak valid.
- Gagal memperbarui Billing Set.
- Gagal meregenerasi Financial Projection.
- Verifikator tidak memiliki hak akses.

---

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |
  