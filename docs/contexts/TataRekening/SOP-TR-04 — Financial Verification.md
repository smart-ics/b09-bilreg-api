# SOP-TR-04 — Financial Verification

## Tujuan

Memastikan bahwa seluruh Billing Set dan Financial Responsibility pada Registrasi telah lengkap, benar, dan konsisten sebelum dilakukan Financial Responsibility Allocation.

Financial Verification merupakan proses validasi akhir terhadap Billing Set yang telah berada pada kondisi operasional yang stabil, termasuk hasil Merge Billing apabila dilakukan.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **CLOSED**.
- Proses Merge Billing telah selesai atau tidak diperlukan.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator meninjau seluruh Billing Set pada Registrasi.
2. Verifikator memverifikasi bahwa seluruh Financial Charge telah terbentuk secara lengkap.
3. Verifikator memverifikasi bahwa tidak terdapat Financial Charge yang hilang, ganda, atau tidak semestinya.
4. Verifikator memverifikasi bahwa seluruh hasil Merge Billing (apabila ada) telah berpindah secara benar ke Registrasi tujuan.
5. Verifikator memverifikasi bahwa Billing Set telah sesuai dengan dokumen pendukung dan ketentuan yang berlaku.
6. Apabila ditemukan ketidaksesuaian, proses dilanjutkan ke **[SOP-TR-05 — Financial Adjustment](SOP-TR-05 — Financial Adjustment.md)**.
7. Apabila seluruh Billing Set telah dinyatakan benar, proses dilanjutkan ke **[SOP-TR-06 — Financial Responsibility Allocation](SOP-TR-06 — Financial Responsibility Allocation.md)**.

---

## Output

Status Financial Verification telah ditentukan sebagai:

- **Valid**, atau
- **Memerlukan Financial Adjustment**.

---

## Postcondition

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
- Apabila ditemukan ketidaksesuaian yang memerlukan perubahan Financial Responsibility, proses harus dilanjutkan ke **[SOP-TR-05 — Financial Adjustment](SOP-TR-05 — Financial Adjustment.md)** sebelum Financial Responsibility Allocation dapat dilakukan.

---

## Exception

- Billing belum berstatus **CLOSED**.
- Masih terdapat Merge Request yang berstatus **Pending**.
- Billing telah berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Data Billing Set tidak dapat dimuat.
- Verifikator tidak memiliki hak akses.
  

---

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |