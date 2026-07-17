# SOP-RI-D1 — Waiting List Management

## 1. Tujuan

Mendokumentasikan proses pengelolaan **Waiting List** bagi **Admission** yang belum dapat dilakukan **Bed Assignment** karena belum tersedia **Bed** yang sesuai dengan kebutuhan **Patient**.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pencatatan, pemeliharaan, dan pemantauan **Waiting List** setelah **Admission** dilakukan hingga **Patient** siap diproses ke tahap **Bed Assignment**.

---

## 3. Prasyarat

* **Admission** telah dibuat.
* Status **Admission** masih **Admitted**.
* Belum dilakukan **Bed Assignment**.
* **Bed** yang sesuai belum tersedia, atau rumah sakit memutuskan **Patient** harus menunggu giliran **Bed Assignment**.

---

## 4. Aktor

| Aktor            | Peran                                                                                         |
| ---------------- | --------------------------------------------------------------------------------------------- |
| Admisi           | Mengelola **Waiting List**                                                                    |
| Patient / Family | Menunggu ketersediaan **Bed** serta memberikan informasi apabila terdapat perubahan kebutuhan |

---

## 5. Partisipan

* **Admission**
* **Waiting List**

---

## 6. Alur Proses

1. Setelah **Admission** selesai diproses, **Admisi** memeriksa ketersediaan **Bed** sesuai kebutuhan **Patient**.

2. Apabila **Bed** belum tersedia, **Admisi** memasukkan **Admission** ke dalam **Waiting List**.

3. **Admisi** melengkapi informasi **Waiting List**, seperti prioritas, kelas perawatan, preferensi ruangan, serta informasi lain yang diperlukan.

4. Sistem melakukan validasi terhadap data **Waiting List**.

5. Apabila validasi berhasil, sistem menyimpan **Waiting List** dengan status **Waiting**.

6. **Admisi** secara berkala memantau ketersediaan **Bed**.

7. Apabila **Bed** yang sesuai telah tersedia, **Admission** dikeluarkan dari **Waiting List** dan siap diproses melalui SOP-RI-D2 **Bed Assignment**.

8. Proses selesai.

---

## 7. Hasil

* **Waiting List** berhasil dibuat.
* Status **Waiting List** menjadi **Waiting**.
* **Admission** menunggu proses **Bed Assignment**.
* **Admission** siap diproses ke SOP-RI-D2 **Bed Assignment** setelah **Bed** tersedia.

---

## 8. Aturan Bisnis

**BR-RI-D1-01**

**Waiting List** hanya dapat dibuat untuk **Admission** yang berstatus **Admitted**.

---

**BR-RI-D1-02**

Satu **Admission** hanya dapat memiliki satu **Waiting List** yang masih aktif.

---

**BR-RI-D1-03**

**Waiting List** tidak mengubah status **Admission**.

---

**BR-RI-D1-04**

Masuk ke **Waiting List** tidak melakukan **Bed Assignment** maupun menentukan **Bed**.

---

**BR-RI-D1-05**

Urutan pelayanan **Waiting List** mengikuti kebijakan operasional rumah sakit, seperti prioritas klinis, waktu **Admission**, atau kebijakan lain yang berlaku.

---

**BR-RI-D1-06**

**Admission** yang telah dilakukan **Bed Assignment** secara otomatis tidak lagi berada pada **Waiting List**.

---

## 9. Post Condition

* **Admission** tetap berada pada status **Admitted**.
* **Waiting List** berada pada status **Waiting**.
* Belum terdapat **Bed Assignment**.
* **Admission** siap diproses pada SOP-RI-D2 **Bed Assignment** ketika **Bed** tersedia.
