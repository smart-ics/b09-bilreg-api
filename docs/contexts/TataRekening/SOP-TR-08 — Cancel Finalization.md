# SOP-TR-08 — Cancel Finalization

## Tujuan

Membatalkan status **FINALIZED** sehingga Financial Responsibility Allocation dapat diperbaiki kembali sebelum dilakukan Settlement.

Cancel Finalization digunakan apabila setelah Finalization ditemukan kebutuhan untuk mengubah Financial Responsibility tanpa mengubah riwayat operasional maupun Financial Charge.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **FINALIZED**.
- Belum terdapat Payment Settlement.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator memilih perintah **Cancel Finalization**.
2. Sistem memvalidasi bahwa Billing masih dapat dibatalkan Finalization-nya.
3. Sistem memastikan belum terdapat Payment Settlement.
4. Verifikator memasukkan alasan Cancel Finalization.
5. Sistem mengubah status Billing dari **FINALIZED** menjadi **CLOSED**.
6. Sistem membuka kembali Financial Responsibility Allocation.
7. Sistem mempertahankan Billing Set dan Financial Charge tanpa perubahan.
8. Sistem mencatat Audit Trail yang berisi alasan pembatalan, waktu, dan identitas Verifikator.
9. Proses dilanjutkan ke **[SOP-TR-06 — Financial Responsibility Allocation](<SOP-TR-06 — Financial Responsibility Allocation.md>)**.

---

## Output

Status Finalization berhasil dibatalkan dan Billing kembali ke status **CLOSED** sehingga Financial Responsibility dapat dialokasikan kembali.

---

## Postcondition

- Billing berstatus **CLOSED**.
- Financial Responsibility Allocation tidak lagi terkunci.
- Financial Projection tetap menjadi representasi Allocation terakhir sampai dilakukan perubahan Allocation.
- Tata Rekening siap dilakukan Financial Responsibility Allocation kembali.

---

## Business Rules

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
- Setelah Allocation selesai diperbarui, Billing harus melalui **[SOP-TR-07 — Finalize Financial Responsibility](<SOP-TR-07 — Finalize Financial Responsibility.md>)** kembali sebelum dapat memasuki Settlement.

---

## Exception

- Billing belum berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Payment Settlement telah dilakukan.
- Verifikator tidak memiliki hak akses.
- Alasan Cancel Finalization belum diisi.
- Validasi Cancel Finalization gagal.

---

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |