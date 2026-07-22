# SOP Patient Tracker — Operasi Antrean Admisi

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Patient Tracker

**Spesifikasi bisnis:** [Domain Operasi Antrean Admisi](./TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md)

**Sumber kanonis bahasa Inggris:** [Admission Queue Operations SOP](./TRACKER-ADMISSION-QUEUE-SOP.md)

**Status terminologi aplikasi:** `Kiosk`, `Queue Display`, `Admission Module`, dan `Call` telah ditetapkan oleh konsep operasional yang disetujui. Kontrol lain dijelaskan berdasarkan tindakan operasionalnya karena label UI yang terlihat belum disetujui.

## 1. Tujuan

Menyediakan satu prosedur end-to-end yang dapat diulang untuk menerbitkan admission Queue Label, memanggil Queue Entry menuju Loket yang diotorisasi, memulai Registration Assistance, menyelesaikan Patient Journey yang berlaku, mencatat registration outcome, dan menyelesaikan pelayanan antrean.

SOP ini berlaku bagi Walk-In Patient dan Booking Patient yang Self-Registration-nya memerlukan bantuan.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Patient or Visitor | Manusia | Memilih Service Point yang ditawarkan, menyimpan Queue Label yang diterbitkan, merespons Queue Call, dan menunjukkan bukti yang tersedia. |
| Admission Officer | Manusia | Mengoperasikan Loket yang ditetapkan, memilih Service Point yang diotorisasi, memanggil Queue Entry, memulai Registration Assistance, menyelesaikan Patient Journey yang berlaku, dan mencatat registration outcome. |
| Queue Operations Supervisor | Manusia | Menyelesaikan pengecualian otorisasi, no-show, gangguan display, dan Service Point transfer yang memerlukan persetujuan accountable. |
| Kiosk | Aplikasi | Menampilkan Service Point yang ditawarkan pada Kiosk tersebut, meminta satu Queue Entry, menampilkan Queue Label yang diterbitkan, dan melaporkan status pencetakan. |
| Queue Ticket Printer | Perangkat | Mencetak atau mencetak ulang Queue Label yang diterbitkan Kiosk. |
| Admission Module | Aplikasi | Menampilkan penetapan Loket dan Service Point yang diotorisasi, menyajikan Work List, mencatat tindakan antrean, mendukung Journey Resolution dan registrasi, serta menampilkan outcome yang dapat diamati. |
| Queue Display | Aplikasi | Menampilkan Queue Call saat ini dan Loket tujuan serta, ketika diaktifkan, mengumumkan panggilan melalui audio. |
| Patient Tracker Queue Service | Subsystem | Menyediakan hasil Queue Session, Queue Entry, Queue Label, Queue Call, identity association, dan queue state kepada aplikasi yang berpartisipasi. |
| Admisi Rajal Registration Service | Subsystem | Menyediakan konteks Journey Resolution dan outcome Outpatient Registration yang authoritative. |

## 3. Prasyarat

1. Kapabilitas Admission Queue Operations yang diperlukan telah dirilis untuk penggunaan operasional.

2. Setidaknya satu admission Service Point aktif ditawarkan melalui Kiosk.

3. Service Point aktif memiliki nama dan Queue Prefix yang dapat dikenali, serta Queue Session yang berlaku tersedia.

4. Kiosk dapat meminta Queue Entry dan menampilkan Queue Label yang dihasilkan.

5. Queue Ticket Printer siap, atau prosedur yang disetujui untuk mencatat dan menyampaikan Queue Label yang tidak tercetak tersedia.

6. Queue Display tersedia untuk area tunggu Patient yang berlaku, atau Queue Operations Supervisor telah menyetujui prosedur pemanggilan manual sementara.

7. Admission Officer telah sign in ke Admission Module.

8. Admission Module menampilkan satu penetapan Loket aktif untuk workstation atau session Admission Officer.

9. Loket yang ditetapkan memiliki setidaknya satu Service Point authorization aktif.

10. Patient atau Visitor memerlukan assisted outpatient registration sebagai Walk-In atau setelah Booking Self-Registration memerlukan bantuan.

## 4. Langkah Operasional

### 4.1 Menerbitkan Queue Label di Kiosk

1. **Patient or Visitor** mendatangi Kiosk dan meninjau admission Service Point yang ditampilkan **Kiosk**.

2. **Patient or Visitor** memilih Service Point yang berlaku, seperti Admisi BPJS atau Admisi Umum.

3. **Kiosk** mengirim satu permintaan pengambilan antrean untuk Service Point yang dipilih dan menampilkan indikator pemrosesan.

4. **Patient Tracker Queue Service** mengembalikan satu Queue Entry beserta Queue Label-nya.

5. **Kiosk** menampilkan Queue Label dan mengirim Queue Label yang sama kepada **Queue Ticket Printer**.

6. **Queue Ticket Printer** mencetak Queue Label; **Kiosk** menampilkan apakah pencetakan berhasil.

7. **Patient or Visitor** menyimpan Queue Label tercetak, atau mencatat Queue Label yang ditampilkan **Kiosk** ketika prosedur tiket tidak tercetak yang disetujui digunakan.

8. **Patient or Visitor** menunggu di area yang dilayani Queue Display yang berlaku.

### 4.2 Menyiapkan Loket Admisi

9. **Admission Officer** membuka Admission Module dan memverifikasi penetapan Loket yang ditampilkan sebelum melayani antrean.

10. **Admission Module** hanya menampilkan Service Point yang diotorisasi untuk Loket yang ditetapkan.

11. **Admission Officer** memilih Service Point yang akan dilayani.

12. **Admission Module** menampilkan Work List aktif untuk Service Point yang dipilih, termasuk setiap Waiting Queue Label.

13. **Admission Officer** memverifikasi bahwa tidak ada Queue Entry lain yang sedang Outstanding atau In Service pada Loket yang ditetapkan sebelum memanggil Queue Entry lain.

### 4.3 Memanggil Patient atau Visitor

14. **Admission Officer** memilih satu Waiting Queue Entry dari Work List dan menjalankan **Call**.

15. **Patient Tracker Queue Service** mencatat Queue Call hanya ketika Queue Entry tetap tersedia dan Loket yang ditetapkan tetap diotorisasi.

16. **Admission Module** menampilkan Queue Call yang berhasil beserta Queue Label dan Loket tujuan.

17. **Queue Display** menampilkan Queue Label dan Loket tujuan serta, ketika audio diaktifkan, mengumumkan panggilan yang sama.

18. **Patient or Visitor** membandingkan Queue Label yang diumumkan dengan Queue Label yang disimpan dan mendatangi Loket yang diumumkan.

19. **Admission Officer** membandingkan Queue Label yang ditunjukkan dengan Queue Call yang Outstanding.

### 4.4 Memulai Registration Assistance

20. **Admission Officer** menjalankan tindakan untuk memulai pelayanan hanya setelah Patient atau Visitor hadir di Loket yang ditetapkan.

21. **Patient Tracker Queue Service** mengakui Queue Call yang Outstanding dan mengubah hasil Queue Entry dari `Waiting` menjadi `In Service`.

22. **Admission Module** menampilkan Queue Entry sebagai `In Service` dan menghapusnya dari Waiting Work List.

23. **Queue Display** berhenti menampilkan panggilan tersebut sebagai panggilan Outstanding setelah menerima hasil yang diperbarui.

### 4.5 Menyelesaikan Patient Journey dan melakukan Registration Assistance

24. **Admission Officer** meminta Patient atau Visitor menunjukkan bukti identitas, booking, dan administrasi yang tersedia.

25. **Admission Officer** mengidentifikasi apakah bantuan mengikuti jalur Booking atau jalur Walk-In.

26. Untuk jalur Booking, **Admission Officer** mencari konteks Journey Resolution yang tersedia menggunakan bukti yang diberikan Patient dan meninjau candidate evidence yang dikembalikan.

27. Untuk jalur Booking, **Admission Officer** memilih existing Booking Patient Tracker yang berlaku; **Admisi Rajal Registration Service** dan **Patient Tracker Queue Service** menampilkan apakah asosiasi Queue Entry berhasil.

28. Untuk jalur Walk-In, **Admission Officer** mencari Patient Journey existing yang berlaku menggunakan bukti yang tersedia dan meninjau semua candidate yang dikembalikan.

29. Untuk jalur Walk-In dengan existing journey yang berlaku, **Admission Officer** memilih Patient Tracker tersebut; **Patient Tracker Queue Service** menampilkan apakah asosiasi Queue Entry berhasil.

30. Untuk jalur Walk-In tanpa existing journey yang berlaku, **Admission Officer** melanjutkan Registration Assistance tanpa membuat Tracker hanya dari Queue Number.

31. **Admission Officer** mengisi atau mengonfirmasi informasi registrasi yang diminta Admission Module dan mengirim registration attempt.

32. Untuk Walk-In registration yang membentuk journey baru, **Admisi Rajal Registration Service** membentuk Outpatient Registration dan asosiasi Patient Tracker-nya; **Patient Tracker Queue Service** mengaitkan existing Queue Entry dengan Tracker tersebut.

33. **Admisi Rajal Registration Service** menampilkan Outpatient Registration yang terbentuk atau outcome accountable `Registration Not Established`.

34. **Admission Officer** memverifikasi registration outcome yang ditampilkan dan menyelesaikan komunikasi operasional yang tersisa dengan Patient atau Visitor.

### 4.6 Menyelesaikan pelayanan antrean

35. **Admission Officer** menjalankan tindakan untuk menyelesaikan pelayanan antrean setelah registration outcome yang accountable ditampilkan dan tidak ada Registration Assistance yang tersisa di Loket.

36. **Patient Tracker Queue Service** mengubah hasil Queue Entry dari `In Service` menjadi `Done`.

37. **Admission Module** menampilkan Queue Entry sebagai `Done` dan menghapusnya dari Work List aktif.

38. **Admission Officer** memverifikasi bahwa Loket tidak lagi memiliki Queue Entry yang Outstanding atau In Service dari prosedur ini sebelum memanggil Queue Entry berikutnya.

## 5. Pengecualian Operasional

### 5.1 Tidak ada Service Point yang tersedia di Kiosk

- **Kiosk** menampilkan bahwa tidak ada layanan admisi yang tersedia saat ini dan tidak menerbitkan Queue Label.
- **Patient or Visitor** meminta arahan dari Admission Officer atau Queue Operations Supervisor.
- **Queue Operations Supervisor** mengarahkan Patient atau Visitor menuju Kiosk, Service Point, atau prosedur pengambilan manual yang disetujui dan tersedia.

### 5.2 Queue Label diterbitkan tetapi pencetakan gagal

- **Kiosk** tetap menampilkan Queue Label yang telah diterbitkan dan melaporkan kegagalan pencetakan.
- **Patient or Visitor** tidak meminta Queue Number lain untuk attempt yang sama.
- **Queue Operations Supervisor** atau Admission Officer yang diotorisasi menjalankan tindakan reprint yang disetujui ketika tersedia; **Queue Ticket Printer** mencetak Queue Label yang sama.
- Jika reprint tidak tersedia, **Patient or Visitor** mencatat Queue Label yang ditampilkan dan mengikuti prosedur tiket tidak tercetak yang disetujui.

### 5.3 Hasil pengambilan antrean tidak pasti

- **Kiosk** meminta hasil attempt pengambilan yang sama dan tidak memulai attempt kedua.
- **Patient Tracker Queue Service** mengembalikan Queue Label yang sebelumnya diterbitkan ketika attempt asli berhasil.
- **Kiosk** menampilkan atau mencetak ulang Queue Label tersebut; **Patient or Visitor** tidak memperoleh duplicate Queue Number untuk attempt yang sama.

### 5.4 Penetapan Loket atau Service Point authorization tidak tersedia

- **Admission Module** tidak menampilkan Service Point yang terdampak sebagai tersedia untuk dilayani di Loket tersebut.
- **Admission Officer** tidak memanggil Queue Entry dari Service Point yang tidak tersedia.
- **Queue Operations Supervisor** memperbaiki penetapan operasional atau mengarahkan Admission Officer menuju Loket yang diotorisasi.

### 5.5 Queue Entry telah dipanggil atau diklaim di tempat lain

- **Patient Tracker Queue Service** menolak hasil pemanggilan yang konflik.
- **Admission Module** menampilkan bahwa Queue Entry tidak lagi tersedia dan me-refresh Work List.
- **Admission Officer** memverifikasi Work List yang diperbarui dan memilih Queue Entry lain yang tersedia; petugas tidak melanjutkan pelayanan dari claim yang ditolak.

### 5.6 Patient atau Visitor tidak hadir setelah Call

- **Admission Officer** menjalankan tindakan Recall untuk Queue Entry yang sama ketika Call Attempt lain sesuai.
- **Patient Tracker Queue Service** mempertahankan Queue Label yang sama dan mencatat Call Attempt tambahan.
- **Queue Display** menampilkan dan, bila diaktifkan, mengumumkan Recall dengan Queue Label dan Loket yang sama.
- Ketika no-show threshold yang berlaku tercapai, **Admission Officer** meminta disposition dari **Queue Operations Supervisor**.
- **Queue Operations Supervisor** memilih disposition yang disetujui: mempertahankan Queue Entry sebagai `Waiting` untuk pelayanan kemudian atau menyimpulkannya sebagai `Withdrawn`/No-Show.
- **Admission Module** menampilkan disposition yang dihasilkan; **Admission Officer** tidak menandai Queue Entry sebagai `Done` hanya karena Patient atau Visitor tidak hadir.

### 5.7 Patient atau Visitor memilih Service Point yang tidak berlaku

- **Admission Officer** berhenti sebelum memulai Registration Assistance ketika ketidaksesuaian dikenali saat Queue Entry masih `Waiting`.
- **Admission Officer** meminta Service Point transfer kepada **Queue Operations Supervisor**.
- **Queue Operations Supervisor** menyetujui atau menolak transfer menggunakan bukti yang tersedia.
- Ketika disetujui, **Patient Tracker Queue Service** menampilkan Queue Entry asli sebagai `Withdrawn` dan menerbitkan Queue Entry serta Queue Label pengganti untuk Service Point yang berlaku.
- **Admission Module** menampilkan hubungan dengan Queue Entry asli; **Admission Officer** menyampaikan Queue Label pengganti dan lokasi menunggu kepada **Patient or Visitor**.

### 5.8 Queue Display atau audio tidak tersedia

- **Queue Display** menampilkan kondisi tidak tersedia ketika dapat diamati.
- **Admission Officer** menghentikan Queue Call baru kecuali **Queue Operations Supervisor** telah menyetujui prosedur pemanggilan manual sementara.
- Dalam prosedur manual yang disetujui, **Admission Officer** menyampaikan Queue Label dan Loket yang sama tanpa mengubah Queue Entry atau menerbitkan Queue Number lain.
- **Queue Operations Supervisor** mengembalikan pemanggilan berbasis display secara normal ketika Queue Display tersedia kembali.

### 5.9 Booking evidence tidak membuktikan Patient Tracker yang berlaku

- **Admission Officer** meminta bukti tambahan yang diberikan Patient dan meninjau setiap Journey Candidate yang berlaku.
- **Admission Officer** tidak mengasosiasikan Queue Entry hanya dari Booking QR dan tidak membuat Patient Tracker pengganti untuk jalur Booking.
- Jika Booking Patient Tracker yang berlaku tidak dapat diselesaikan, **Admission Officer** mempertahankan Queue Entry sebagai `In Service` sambil mengeskalasi pengecualian identity resolution berdasarkan prosedur registrasi yang disetujui.

### 5.10 Terjadi konflik asosiasi Tracker

- **Patient Tracker Queue Service** menolak association attempt yang kalah dan mempertahankan asosiasi yang berhasil lebih dahulu.
- **Admission Module** menampilkan konflik dan me-refresh Queue Entry serta konteks Journey Resolution.
- **Admission Officer** memverifikasi asosiasi yang diperbarui dan tidak secara otomatis mencoba ulang dengan Patient Tracker lain.

### 5.11 Data registrasi dapat dikoreksi

- **Admisi Rajal Registration Service** menampilkan masalah validasi tanpa melaporkan Registration yang telah terbentuk.
- **Admission Officer** mengoreksi data yang ditampilkan saat Queue Entry tetap `In Service` dan mengirim ulang registration attempt.
- **Admission Officer** tidak menyelesaikan pelayanan antrean sampai registration outcome yang accountable ditampilkan.

### 5.12 Registration tidak terbentuk setelah penyelesaian accountable

- **Admisi Rajal Registration Service** menampilkan `Registration Not Established` beserta penjelasan operasional yang tersedia.
- **Admission Officer** menyampaikan outcome dan tindakan selanjutnya yang disetujui kepada Patient atau Visitor.
- Ketika tidak ada Registration Assistance lebih lanjut, **Admission Officer** menyelesaikan pelayanan antrean; **Admission Module** menampilkan Queue Entry sebagai `Done` tanpa menampilkan Outpatient Registration sebagai telah terbentuk.

## 6. Kriteria Penyelesaian

Prosedur selesai ketika semua hasil yang dapat diamati dan berlaku tersedia:

1. Patient atau Visitor menerima satu Queue Label untuk Service Point yang dipilih.

2. Queue Call menampilkan Queue Label yang sama dan Loket tujuan.

3. Queue Entry berubah menjadi `In Service` hanya setelah Patient atau Visitor hadir dan Registration Assistance dimulai.

4. Jalur Booking menggunakan kembali existing Booking Patient Tracker yang berlaku, atau jalur Walk-In memilih Patient Tracker existing yang berlaku atau mengasosiasikan entry dengan Tracker yang dibentuk oleh Registration.

5. Admission Module menampilkan Outpatient Registration yang terbentuk atau `Registration Not Established`.

6. Queue Entry menampilkan `Done` setelah Registration Assistance berakhir, atau menampilkan `Withdrawn` ketika prosedur yang disetujui mengakhiri keikutsertaan sebelum mulai pelayanan.

7. Queue Entry yang selesai atau Withdrawn tidak terdapat dalam Work List Waiting dan In Service yang aktif.

8. Loket yang ditetapkan tidak memiliki Queue Entry yang Outstanding atau In Service yang tersisa dari prosedur ini.

## 7. Referensi

| Otoritas | Dokumen |
|---|---|
| Spesifikasi bisnis antrean admisi | [TRACKER-ADMISSION-QUEUE-DOMAIN.md](./TRACKER-ADMISSION-QUEUE-DOMAIN.md) |
| Pendamping bisnis antrean admisi Bahasa Indonesia | [TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md](./TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md) |
| Spesifikasi bisnis induk Patient Tracker | [TRACKER-DOMAIN.md](./TRACKER-DOMAIN.md) |
| Spesifikasi bisnis Admisi Rajal | [admisi-rajal-domain.md](../admisi-rajal/admisi-rajal-domain.md) |
| Interpretasi operational event | [operational-events.md](../../concepts/operational-events.md) |
| Bukti implementation gap saat ini | [tracker-admission-queue-late-identification-gap-analysis.md](./tracker-admission-queue-late-identification-gap-analysis.md) |
| Target technical architecture | [TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md) |

Target architecture membedakan perilaku yang saat ini telah diimplementasikan dari kapabilitas multi-Service-Point, Loket, Kiosk, Queue Call, Queue Display, no-show, dan transfer yang masih belum tersedia.
