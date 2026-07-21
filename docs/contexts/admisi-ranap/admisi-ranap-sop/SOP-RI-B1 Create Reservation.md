# SOP-RI-B1 — Create Reservation

## 1. Tujuan

Mendokumentasikan proses pembuatan **Reservation** sebagai rencana administrasi Rawat Inap bagi **Patient** yang akan menjalani perawatan pada waktu yang telah ditentukan.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pembuatan **Reservation** sebelum **Admission** dilakukan, baik berdasarkan **Opname Request** maupun permintaan Rawat Inap terjadwal.

---

## 3. Prasyarat

* **Patient** telah terdaftar pada sistem.
* Salah satu kondisi berikut terpenuhi:

  * Terdapat **Opname Request** yang masih berstatus **Requested**, atau
  * Rumah sakit mengizinkan pembuatan **Reservation** tanpa **Opname Request** (misalnya untuk Rawat Inap elektif).
* **Admission** belum diproses.

---

## 4. Aktor

| Aktor            | Peran                                   |
| ---------------- | --------------------------------------- |
| Admisi           | Membuat **Reservation**                 |
| Patient / Family | Memberikan informasi rencana Rawat Inap |

---

## 5. Partisipan

* **Reservation**
* **Opname Request** *(opsional)*

---

## 6. Alur Proses

1. **Admisi** menerima permintaan reservasi Rawat Inap.

2. **Admisi** melakukan verifikasi identitas **Patient** dan informasi rencana Rawat Inap.

3. Apabila reservasi berasal dari keputusan klinis, **Admisi** memilih **Opname Request** yang akan digunakan sebagai dasar reservasi.

4. **Admisi** mengisi informasi **Reservation**, seperti tanggal rencana masuk, kelas perawatan, preferensi ruangan, serta informasi lain yang diperlukan.

5. Sistem melakukan validasi terhadap data **Reservation**.

6. Apabila validasi berhasil, sistem menyimpan **Reservation** dengan status **Reserved**.

7. Proses selesai.

---

## 7. Hasil

* **Reservation** berhasil dibuat.
* Status **Reservation** menjadi **Reserved**.
* Belum terbentuk **Admission**.
* Belum dilakukan **Placement** maupun **Bed Assignment**.

---

## 8. Aturan Bisnis

**BR-RI-B1-01**

**Reservation** dapat dibuat berdasarkan **Opname Request** ataupun tanpa **Opname Request**, sesuai kebijakan rumah sakit.

---

**BR-RI-B1-02**

Satu **Reservation** hanya berlaku untuk satu rencana Rawat Inap.

---

**BR-RI-B1-03**

Pembuatan **Reservation** tidak otomatis membuat **Admission**.

---

**BR-RI-B1-04**

Pembuatan **Reservation** tidak melakukan **Bed Assignment** maupun menentukan **Bed**.

---

**BR-RI-B1-05**

**Reservation** dapat diubah atau dibatalkan sebelum proses **Admission** dilakukan.

---

## 9. Post Condition

* **Reservation** berada pada status **Reserved**.
* **Reservation** siap diproses pada SOP-RI-C1 **Process Admission**.
* Belum terdapat **Admission**, **Placement**, maupun **Bed Assignment**.

---

### Architectural note

One modeling decision should be made before writing the remaining Reservation SOPs:

Should **Reservation** be **optional** or **mandatory** before **Admission**?

I recommend making it **optional**. This accommodates both common admission flows:

1. **Elective admission**: `Opname Request → Reservation → Admission`
2. **Emergency/direct admission** (e.g., IGD): `Opname Request → Admission`

This keeps the model flexible while preserving the distinction that **Reservation** is a planning activity rather than an administrative requirement.
