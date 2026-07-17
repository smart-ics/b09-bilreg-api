# SOP-RI-A1 — Create Opname Request

## 1. Tujuan

Mendokumentasikan keputusan klinis seorang **Doctor** bahwa seorang **Patient** memerlukan perawatan Rawat Inap melalui pembuatan **Opname Request**, sehingga dapat diproses lebih lanjut oleh bagian **Admisi**.

---

## 2. Ruang Lingkup

SOP ini berlaku untuk seluruh proses pembuatan **Opname Request** yang berasal dari keputusan klinis **Doctor**, baik dari Poli, IGD, maupun unit pelayanan lain yang berwenang.

---

## 3. Prasyarat

* **Patient** telah terdaftar pada sistem.
* **Doctor** telah melakukan pemeriksaan klinis.
* **Doctor** memutuskan bahwa **Patient** memerlukan Rawat Inap.

---

## 4. Aktor

| Aktor  | Peran                                                     |
| ------ | --------------------------------------------------------- |
| Doctor | Membuat **Opname Request**                                |
| Admisi | Menerima **Opname Request** untuk diproses pada SOP-RI-C1 |

---

## 5. Partisipan

* **Opname Request**
* **Admission Inbox** *(atau **Admission Request**, apabila nantinya tetap dipertahankan sebagai participant)*

---

## 6. Alur Proses

1. **Doctor** melakukan evaluasi terhadap **Patient**.

2. Apabila berdasarkan pertimbangan klinis **Patient** memerlukan Rawat Inap, **Doctor** membuat **Opname Request**.

3. Sistem melakukan validasi terhadap data **Opname Request**.

4. Apabila validasi berhasil, sistem menyimpan **Opname Request** dengan status **Requested**.

5. Sistem menempatkan **Opname Request** ke dalam **Admission Inbox** sehingga dapat diproses oleh petugas **Admisi**.

6. Proses selesai.

---

## 7. Hasil

* **Opname Request** berhasil dibuat.
* Status **Opname Request** menjadi **Requested**.
* **Opname Request** tersedia pada **Admission Inbox**.
* Belum terbentuk **Admission**.

---

## 8. Aturan Bisnis

**BR-RI-A1-01**

Hanya **Doctor** yang dapat membuat **Opname Request**.

---

**BR-RI-A1-02**

Satu **Patient** dapat memiliki lebih dari satu **Opname Request**, sepanjang masing-masing mewakili kebutuhan Rawat Inap yang berbeda.

---

**BR-RI-A1-03**

Pembuatan **Opname Request** tidak otomatis membuat **Admission**.

---

**BR-RI-A1-04**

Setelah **Opname Request** dibuat, proses administrasi dilanjutkan melalui SOP-RI-C1 **Process Admission**.

---

**BR-RI-A1-05**

Pembuatan **Opname Request** tidak melakukan **Bed Assignment** maupun menentukan **Bed**.

---

## 9. Post Condition

* **Opname Request** berada pada status **Requested**.
* **Admisi** dapat memproses **Opname Request** sesuai SOP-RI-C1.
* Belum terdapat **Waiting List** maupun **Bed Assignment**.
