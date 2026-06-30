# SOP-TR-10 — Settlement Initiation

## Tujuan

Menyerahkan Billing yang telah berstatus **FINALIZED** kepada Kasir untuk memulai proses **Payment Settlement**.

Settlement Initiation merupakan batas akhir tanggung jawab Tata Rekening sebagai pengelola **Financial Control** dan awal tanggung jawab Kasir sebagai pelaksana **Payment Settlement**.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **FINALIZED**.
- Financial Responsibility Allocation telah dikunci.
- Financial Projection telah tersedia.
- Belum terdapat Payment Settlement.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator meninjau kembali hasil Finalize Financial Responsibility.
2. Sistem memvalidasi bahwa Billing telah memenuhi seluruh persyaratan Settlement.
3. Sistem memastikan Financial Projection tersedia sebagai dasar Payment Settlement.
4. Verifikator memilih perintah **Settlement Initiation**.
5. Sistem menyerahkan otoritas Settlement kepada Kasir.
6. Sistem menandai Tata Rekening telah menyelesaikan proses Financial Control.
7. Proses dilanjutkan ke SOP Kasir untuk **Payment Settlement**.

---

## Output

Billing berhasil diserahkan kepada Kasir dan siap diproses pada tahap Payment Settlement.

---

## Postcondition

- Billing tetap berstatus **FINALIZED**.
- Financial Responsibility Allocation tetap terkunci.
- Financial Projection menjadi dasar Payment Settlement.
- Tata Rekening tidak lagi menjadi proses aktif pada Registrasi tersebut.
- Otoritas proses berpindah kepada Kasir.

---

## Business Rules

- Settlement Initiation hanya dapat dilakukan terhadap Billing yang berstatus **FINALIZED**.
- Settlement Initiation tidak mengubah Billing Set.
- Settlement Initiation tidak mengubah Financial Charge.
- Settlement Initiation tidak mengubah Financial Responsibility Allocation.
- Settlement Initiation tidak mengubah Financial Projection.
- Settlement Initiation tidak melakukan proses pembayaran.
- Setelah Settlement Initiation, seluruh proses pembayaran menjadi tanggung jawab Kasir sesuai SOP Kasir.
- Apabila sebelum Payment Settlement diperlukan perubahan Financial Responsibility, proses harus diawali dengan **[SOP-TR-08 — Cancel Finalization](<SOP-TR-08 — Cancel Finalization.md>)**.

---

## Exception

- Billing belum berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Financial Projection belum tersedia atau tidak valid.
- Payment Settlement telah dimulai.
- Verifikator tidak memiliki hak akses.
- Gagal menyerahkan Billing kepada Kasir.

---

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |