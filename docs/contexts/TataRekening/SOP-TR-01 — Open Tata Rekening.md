# SOP-TR-01 — Open Tata Rekening

## Tujuan

Membuka Tata Rekening untuk suatu Registrasi sebagai awal proses Financial Control serta mempersiapkan seluruh informasi yang diperlukan sebelum dilakukan proses Close Bill, Merge Billing, dan Financial Verification.

---

## Aktor

**Verifikator**

---

## Precondition

- Registrasi telah dibuat.
- Registrasi masih aktif.
- Billing berstatus **OPEN**.
- Registrasi belum berstatus **LUNAS**.
- Verifikator memiliki hak akses Tata Rekening.

---

## Workflow

1. Verifikator memilih Registrasi yang akan diproses.
2. Sistem memuat Tata Rekening Registrasi.
3. Sistem memuat seluruh Billing Set Registrasi.
4. Sistem memuat Financial Projection yang masih berlaku (apabila ada).
5. Sistem memuat informasi Payer.
6. Sistem mencari Merge Request yang masih berstatus **Pending**, dengan kondisi:
   - **TargetReg = Registrasi yang sedang dibuka**, atau
   - **TargetReg masih kosong**, tetapi **PatientId** sama dengan Registrasi yang sedang dibuka.
7. Sistem menampilkan Ringkasan Tata Rekening beserta daftar Merge Request yang ditemukan (apabila ada).
8. Proses dilanjutkan ke **[SOP-TR-02 — Close Bill](<SOP-TR-02 — Close Bill.md>)**.

---

## Output

Tata Rekening berhasil dibuka dan seluruh informasi Financial Responsibility siap diproses.

Apabila terdapat Merge Request yang masih Pending, sistem menampilkannya sebagai kandidat Merge Billing.

---

## Postcondition

- Tata Rekening berada pada Registrasi yang dipilih.
- Seluruh Billing Set telah dimuat.
- Financial Projection telah dimuat.
- Daftar Merge Request yang relevan telah dimuat.
- Tata Rekening siap memasuki proses **Close Bill**.

---

## Business Rules

- Membuka Tata Rekening tidak mengubah Billing Set.
- Membuka Tata Rekening tidak mengubah Financial Responsibility.
- Membuka Tata Rekening tidak mengubah Billing Lifecycle.
- Membuka Tata Rekening tidak mengubah status Merge Request.
- Seluruh data yang ditampilkan merupakan kondisi terkini pada saat Tata Rekening dibuka.

---

## Exception

- Registrasi tidak ditemukan.
- Registrasi telah dibatalkan.
- Registrasi telah berstatus **LUNAS**.
- Verifikator tidak memiliki hak akses.
- Data Tata Rekening gagal dimuat.

---

## Related Documents

| Document | Description |
|----------|-------------|
| [04-sop.md](04-sop.md) | SOP index & workflow overview |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |
