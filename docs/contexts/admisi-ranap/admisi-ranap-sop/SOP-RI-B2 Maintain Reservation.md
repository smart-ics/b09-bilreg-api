# SOP-RI-B2 — Maintain Reservation

## 1. Tujuan

Mendokumentasikan proses pemeliharaan **Reservation** yang telah dibuat sebelumnya, agar informasi rencana Rawat Inap tetap sesuai dengan kebutuhan **Patient** sebelum proses **Admission** dilakukan.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses perubahan informasi **Reservation** yang masih aktif dan belum diproses menjadi **Admission**.

---

## 3. Prasyarat

* **Reservation** telah dibuat.
* Status **Reservation** masih **Reserved**.
* **Admission** belum diproses.

---

## 4. Aktor

| Aktor            | Peran                                     |
| ---------------- | ----------------------------------------- |
| Admisi           | Memperbarui informasi **Reservation**     |
| Patient / Family | Menyampaikan perubahan rencana Rawat Inap |

---

## 5. Partisipan

* **Reservation**
* **Opname Request** *(opsional)*

---

## 6. Alur Proses

1. **Patient** atau **Family** menyampaikan perubahan terhadap rencana Rawat Inap kepada petugas **Admisi**.

2. **Admisi** membuka data **Reservation** yang akan diperbarui.

3. **Admisi** mengubah informasi **Reservation** sesuai kebutuhan, seperti tanggal rencana masuk, kelas perawatan, preferensi ruangan, atau informasi administrasi lainnya.

4. Sistem melakukan validasi terhadap perubahan yang dilakukan.

5. Apabila validasi berhasil, sistem menyimpan perubahan **Reservation**.

6. Status **Reservation** tetap **Reserved**.

7. Proses selesai.

---

## 7. Hasil

* Informasi **Reservation** berhasil diperbarui.
* Status **Reservation** tetap **Reserved**.
* **Reservation** tetap siap diproses pada saat **Admission** dilakukan.
* Belum terbentuk **Admission**.
* Belum dilakukan **Placement** maupun **Bed Assignment**.

---

## 8. Aturan Bisnis

**BR-RI-B2-01**

**Reservation** hanya dapat diperbarui selama statusnya masih **Reserved**.

---

**BR-RI-B2-02**

**Reservation** tidak dapat diperbarui setelah diproses menjadi **Admission**.

---

**BR-RI-B2-03**

Perubahan **Reservation** tidak mengubah status **Reservation**.

---

**BR-RI-B2-04**

Perubahan **Reservation** tidak otomatis membuat **Admission**.

---

**BR-RI-B2-05**

Perubahan **Reservation** tidak melakukan **Placement** maupun menentukan **Bed**.

---

**BR-RI-B2-06**

Apabila **Reservation** terkait dengan **Opname Request**, perubahan **Reservation** tidak mengubah maupun membatalkan **Opname Request** tersebut.

---

## 9. Post Condition

* **Reservation** tetap berada pada status **Reserved**.
* Informasi **Reservation** telah diperbarui sesuai perubahan terbaru.
* **Reservation** tetap siap diproses melalui SOP-RI-C1 **Process Admission**.
* Belum terdapat **Admission**, **Placement**, maupun **Bed Assignment**.
