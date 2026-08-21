# SOP-TR-07 — Finalize Financial Responsibility

## Tujuan

Mengunci hasil Financial Responsibility Allocation sehingga Billing siap memasuki proses Settlement.

Finalize Financial Responsibility merupakan batas akhir proses Financial Control, dimana kepemilikan Financial Responsibility telah ditetapkan dan tidak dapat diubah tanpa melalui proses Cancel Finalization.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **CLOSED**.
- Financial Verification telah selesai.
- Seluruh Financial Adjustment telah selesai atau tidak diperlukan.
- Financial Responsibility Allocation telah selesai.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator meninjau kembali hasil Financial Responsibility Allocation.
2. Sistem memvalidasi bahwa seluruh Billing telah memiliki Financial Responsibility yang lengkap.
3. Sistem memvalidasi bahwa total Allocation sama dengan total Billing Registrasi.
4. Sistem memvalidasi bahwa Financial Projection telah berhasil diregenerasi.
5. Verifikator memilih perintah **Finalize Financial Responsibility**.
6. Sistem mengubah status Billing menjadi **FINALIZED**.
7. Sistem mengunci Financial Responsibility Allocation.
8. Sistem menjadikan Financial Projection sebagai dasar Settlement.
9. Proses dilanjutkan ke **[SOP-TR-08 — Cancel Finalization](SOP-TR-08 — Cancel Finalization.md)** apabila diperlukan, atau **[SOP-TR-10 — Settlement Initiation](SOP-TR-10 — Settlement Initiation.md)**.

---

## Output

Billing berhasil difinalisasi dan siap diproses untuk Settlement.

---

## Postcondition

- Billing berstatus **FINALIZED**.
- Financial Responsibility Allocation telah terkunci.
- Financial Projection menjadi dasar Settlement.
- Billing siap memasuki proses Settlement.
- Perubahan Financial Responsibility tidak diperbolehkan kecuali melalui proses **Cancel Finalization**.

---

## Business Rules

- Finalization hanya dapat dilakukan setelah Financial Verification selesai.
- Seluruh Billing harus telah memiliki Financial Responsibility.
- Total Allocation harus sama dengan total Billing Registrasi.
- Financial Projection harus telah berhasil diregenerasi sebelum Finalization dilakukan.
- Setelah Finalization, Financial Responsibility Allocation tidak dapat diubah.
- Setelah Finalization, Billing Set tidak dapat diubah kecuali melalui proses **Cancel Finalization** atau **Reopen Billing** sesuai Business Rules yang berlaku.
- Finalization tidak melakukan Settlement maupun pembayaran.
- Finalization tidak mengubah Financial Charge, Pricing Snapshot, maupun Accounting Snapshot.

---

## Exception

- Billing belum berstatus **CLOSED**.
- Financial Verification belum selesai.
- Financial Responsibility Allocation belum lengkap.
- Total Allocation tidak sama dengan total Billing Registrasi.
- Financial Projection gagal dibentuk.
- Billing telah berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Verifikator tidak memiliki hak akses.

---

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |