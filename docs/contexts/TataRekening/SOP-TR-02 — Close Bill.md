# SOP-TR-02 — Close Bill

## Tujuan

Menutup Billing untuk menghentikan pembentukan Financial Charge dari seluruh Charge Source sehingga Billing berada dalam kondisi stabil dan siap memasuki proses Merge Billing maupun Financial Verification.

---

## Aktor

**Verifikator**

---

## Precondition

- Tata Rekening telah dibuka.
- Billing berstatus **OPEN**.
- Verifikator telah meninjau seluruh Billing Set.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator meninjau kesiapan Billing untuk ditutup.
2. Verifikator memastikan seluruh transaksi operasional yang menghasilkan Financial Charge telah selesai.
3. Verifikator memilih perintah **Close Bill**.
4. Sistem memvalidasi bahwa Billing dapat ditutup.
5. Sistem mengubah status Billing dari **OPEN** menjadi **CLOSED**.
6. Sistem menghentikan pembentukan Financial Charge baru dari seluruh Charge Source.
7. Apabila terdapat Merge Request berstatus **Pending**, proses dilanjutkan ke **SOP-TR-03 — Merge Billing**.
8. Apabila tidak terdapat Merge Request, proses langsung dilanjutkan ke **SOP-TR-04 — Financial Verification**.

---

## Output

Billing berhasil ditutup dan berada pada kondisi stabil untuk memasuki proses Financial Control.

---

## Postcondition

- Billing berstatus **CLOSED**.
- Charge Source tidak dapat lagi menghasilkan Financial Charge baru.
- Billing Set berada pada kondisi operasional yang stabil.
- Billing siap dilakukan Merge Billing (apabila diperlukan) atau langsung memasuki Financial Verification.

---

## Business Rules

- Close Bill merupakan batas antara proses operasional dan proses Financial Control.
- Close Bill tidak mengubah Billing Set.
- Close Bill tidak melakukan Merge Billing.
- Close Bill tidak melakukan Financial Verification.
- Close Bill tidak melakukan Financial Adjustment.
- Close Bill tidak melakukan Financial Responsibility Allocation.
- Close Bill tidak melakukan Finalize Financial Responsibility.
- Close Bill tidak menghasilkan Settlement.
- Apabila setelah Close Bill masih diperlukan Financial Charge baru, Billing harus melalui **SOP-TR-09 — Reopen Billing** sebelum Charge Source dapat menambahkan Financial Charge kembali.

---

## Exception

- Billing telah berstatus **CLOSED**.
- Billing telah berstatus **FINALIZED**.
- Billing telah berstatus **LUNAS**.
- Verifikator tidak memiliki hak akses.
- Masih terdapat transaksi operasional yang belum selesai sehingga Billing belum dapat ditutup.
- Validasi Close Bill gagal.