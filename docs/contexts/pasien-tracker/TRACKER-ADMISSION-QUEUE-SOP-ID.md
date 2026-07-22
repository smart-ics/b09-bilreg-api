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
| Patient or Visitor | Manusia | Memilih Service Point aktif yang ditampilkan lokal, menyimpan Queue Label yang diterbitkan, merespons Queue Call, dan menunjukkan bukti yang tersedia. |
| Admission Officer | Manusia | Mengoperasikan Loket dari konfigurasi workstation, dapat memilih setiap Service Point aktif, memanggil Queue Entry, memulai Registration Assistance, menyelesaikan Patient Journey yang berlaku, dan mencatat registration outcome. |
| Queue Operations Supervisor | Manusia | Menyelesaikan pengecualian otorisasi, no-show, gangguan display, dan Service Point transfer yang memerlukan persetujuan accountable. |
| Kiosk | Aplikasi | Menampilkan Service Point dari konfigurasi lokal, meminta satu Queue Entry dengan ClientRequestId opsional, menampilkan Queue Label yang diterbitkan, dan melaporkan status pencetakan. |
| Queue Ticket Printer | Perangkat | Mencetak atau mencetak ulang Queue Label yang diterbitkan Kiosk. |
| Admission Module | Aplikasi | Menggunakan Loket dari konfigurasi workstation, menampilkan Service Point aktif dan Work List, mencatat tindakan antrean, mendukung Journey Resolution dan registrasi, serta menampilkan outcome yang dapat diamati. |
| Queue Display | Aplikasi | Menampilkan Queue Call saat ini dan Loket tujuan serta, ketika diaktifkan, mengumumkan panggilan melalui audio. |
| Patient Tracker Queue Service | Subsystem | Menyediakan hasil Queue Session, Queue Entry, Queue Label, Queue Call, identity association, dan queue state kepada aplikasi yang berpartisipasi. |
| Admisi Rajal Registration Service | Subsystem | Menyediakan konteks Journey Resolution dan outcome Outpatient Registration yang authoritative. |

## 3. Prasyarat

1. Kapabilitas Admission Queue Operations yang diperlukan telah dirilis untuk penggunaan operasional.

2. Setidaknya satu admission Service Point aktif terdapat dalam konfigurasi lokal Kiosk.

3. Service Point berstatus aktif serta memiliki nama dan Queue Prefix yang dapat dikenali. Queue Session hariannya dapat dibentuk secara lazy oleh valid intake pertama.

4. Kiosk dapat meminta Queue Entry dan menampilkan Queue Label yang dihasilkan.

5. Queue Ticket Printer siap, atau prosedur yang disetujui untuk mencatat dan menyampaikan Queue Label yang tidak tercetak tersedia.

6. Queue Display tersedia untuk area tunggu Patient yang berlaku, atau Queue Operations Supervisor telah menyetujui prosedur pemanggilan manual sementara.

7. Admission Officer telah sign in ke Admission Module.

8. Admission Module memperoleh identitas Loket yang valid dari konfigurasi workstation tepercaya.

9. Setiap Loket terkonfigurasi dapat melayani setiap admission Service Point aktif dalam V1.

10. Patient atau Visitor memerlukan assisted outpatient registration sebagai Walk-In atau setelah Booking Self-Registration memerlukan bantuan.

## 4. Langkah Operasional

### 4.1 Menerbitkan Queue Label di Kiosk

1. **Patient or Visitor** mendatangi Kiosk dan meninjau admission Service Point yang ditampilkan **Kiosk**.

2. **Patient or Visitor** memilih Service Point yang berlaku, seperti Admisi BPJS atau Admisi Umum.

3. **Kiosk** mengirim satu permintaan pengambilan antrean untuk Service Point yang dipilih tanpa memberikan Business Date otoritatif dan menampilkan indikator pemrosesan.

4. **Patient Tracker Queue Service** menyelesaikan Business Date dari server, memverifikasi ServicePointId aktif, memuat atau membentuk dedicated Queue Session, menaikkan LastQueueNumber secara atomik, lalu mengembalikan Queue Entry beserta Queue Label-nya. Jika ClientRequestId diberikan, identifier yang sama mengembalikan hasil existing.

5. **Kiosk** menampilkan Queue Label dan mengirim Queue Label yang sama kepada **Queue Ticket Printer**.

6. **Queue Ticket Printer** mencetak Queue Label; **Kiosk** menampilkan apakah pencetakan berhasil.

7. **Patient or Visitor** menyimpan Queue Label tercetak, atau mencatat Queue Label yang ditampilkan **Kiosk** ketika prosedur tiket tidak tercetak yang disetujui digunakan.

8. **Patient or Visitor** menunggu di area yang dilayani Queue Display yang berlaku.

### 4.2 Menyiapkan Loket Admisi

9. **Admission Officer** membuka Admission Module dan memverifikasi Loket dari konfigurasi workstation sebelum melayani antrean.

10. **Admission Module** menampilkan Service Point aktif; V1 tidak menerapkan filter otorisasi per Loket.

11. **Admission Officer** memilih Service Point yang akan dilayani.

12. **Admission Module** menampilkan Admisi Rajal Work List aktif yang diperkaya untuk Service Point terpilih dengan menyusun Admission Queue Worklist Projection khusus antrean milik Patient Tracker bersama konteks Booking, identitas, Registration, dan administratif. Keanggotaan antrean, state, call state, LoketKey, timestamp, Queue Label, indikator Priority, dan TrackerId opsional tetap menjadi kebenaran Patient Tracker. Priority boleh memengaruhi tampilan/sorting tetapi tidak pernah memilih entry otomatis.

13. **Admission Officer** memverifikasi bahwa tidak ada Queue Entry lain yang sedang Outstanding atau In Service pada Loket terkonfigurasi sebelum memanggil Queue Entry lain.

### 4.3 Memanggil Patient atau Visitor

14. **Admission Officer** memilih satu Waiting Queue Entry dari Work List dan menjalankan **Call**.

15. **Patient Tracker Queue Service** secara conditional mencatat Queue Call, menaikkan CallCount, memperbarui `BILRG_AdmLoketCurrentCall`, dan menaikkan AnnouncementVersion dalam transaksi yang sama ketika audio diperlukan.

16. **Admission Module** menampilkan Queue Call yang berhasil beserta Queue Label dan Loket tujuan.

17. Setelah post-commit SignalR refresh hint atau polling periodik, **Queue Display** memuat ulang current state. Display menampilkan Queue Label dan Loket tujuan serta memainkan audio hanya untuk AnnouncementVersion yang baru diamati.

18. **Patient or Visitor** membandingkan Queue Label yang diumumkan dengan Queue Label yang disimpan dan mendatangi Loket yang diumumkan.

19. **Admission Officer** membandingkan Queue Label yang ditunjukkan dengan Queue Call yang Outstanding.

### 4.4 Memulai Registration Assistance

20. **Admission Officer** menjalankan tindakan untuk memulai pelayanan hanya setelah Patient atau Visitor hadir di Loket terkonfigurasi.

21. **Patient Tracker Queue Service** mengakui Queue Call yang Outstanding dan mengubah hasil Queue Entry dari `Waiting` menjadi `In Service`.

22. **Admission Module** menampilkan Queue Entry sebagai `In Service` dan menghapusnya dari Waiting Work List.

23. **Queue Display** berhenti menampilkan panggilan sebagai Outstanding setelah refresh atau polling periodik memuat ulang current state terbaru.

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

33. **Admisi Rajal Registration Service** mempersistenkan dan menampilkan satu Registration Outcome final: `Established` dengan RegId, atau `NotEstablished` dengan ReasonCode wajib. Outcome mempertahankan OutcomeId, QueueEntryId, Explanation ketika diisi, DecidedAt, dan DecidedBy.

34. **Admission Officer** memverifikasi registration outcome yang ditampilkan dan menyelesaikan komunikasi operasional yang tersisa dengan Patient atau Visitor.

### 4.6 Menyelesaikan pelayanan antrean

35. **Admission Officer** menjalankan tindakan untuk menyelesaikan pelayanan antrean hanya setelah Registration Outcome final dipersistenkan dan ditampilkan serta tidak ada Registration Assistance yang tersisa di Loket.

36. **Patient Tracker Queue Service** mengubah hasil Queue Entry dari `In Service` menjadi `Done`.

37. **Admission Module** menampilkan Queue Entry sebagai `Done` dan menghapusnya dari Work List aktif.

38. **Admission Officer** memverifikasi bahwa Loket tidak lagi memiliki Queue Entry yang Outstanding atau In Service dari prosedur ini sebelum memanggil Queue Entry berikutnya.

## 5. Pengecualian Operasional

### 5.1 Tidak ada Service Point yang tersedia di Kiosk

- **Kiosk** menampilkan bahwa tidak ada layanan admisi yang tersedia saat ini dan tidak menerbitkan Queue Label.
- **Patient Tracker Queue Service** menolak intake ketika ServicePointId yang dikirim tidak aktif. Konfigurasi offering lokal Kiosk bukan otoritas server.
- **Patient or Visitor** meminta arahan dari Admission Officer atau Queue Operations Supervisor.
- **Queue Operations Supervisor** mengarahkan Patient atau Visitor menuju Kiosk, Service Point, atau prosedur pengambilan manual yang disetujui dan tersedia.

### 5.2 Queue Label diterbitkan tetapi pencetakan gagal

- **Kiosk** tetap menampilkan Queue Label yang telah diterbitkan dan melaporkan kegagalan pencetakan.
- **Patient or Visitor** tidak meminta Queue Number lain untuk attempt yang sama.
- **Queue Operations Supervisor** atau Admission Officer yang diotorisasi menjalankan tindakan reprint yang disetujui ketika tersedia; **Queue Ticket Printer** mencetak Queue Label yang sama.
- Jika reprint tidak tersedia, **Patient or Visitor** mencatat Queue Label yang ditampilkan dan mengikuti prosedur tiket tidak tercetak yang disetujui.

### 5.3 Hasil pengambilan antrean tidak pasti

- Ketika ClientRequestId diberikan, **Kiosk** meminta hasil menggunakan identifier yang sama dan tidak memulai attempt kedua. Tanpa identifier tersebut, recovery retry-safe tidak dijamin.
- **Patient Tracker Queue Service** mengembalikan Queue Label yang sebelumnya diterbitkan ketika attempt asli berhasil.
- **Kiosk** menampilkan atau mencetak ulang Queue Label tersebut; **Patient or Visitor** tidak memperoleh duplicate Queue Number untuk attempt yang sama.

### 5.4 Konfigurasi Loket workstation tidak tersedia

- **Admission Module** memblokir Call dan Recall ketika LoketKey hilang atau diketahui duplikat.
- **Admission Officer** tidak memanggil Queue Entry sampai konfigurasi valid.
- **Queue Operations Supervisor** memperbaiki konfigurasi workstation terkendali atau mengarahkan Admission Officer ke Loket lain yang dikonfigurasi unik. Penggantian nama PC membutuhkan pembaruan konfigurasi terkendali.

### 5.5 Queue Entry telah dipanggil atau diklaim di tempat lain

- **Patient Tracker Queue Service** menolak hasil pemanggilan yang konflik.
- **Admission Module** menampilkan bahwa Queue Entry tidak lagi tersedia dan me-refresh Work List.
- **Admission Officer** memverifikasi Work List yang diperbarui dan memilih Queue Entry lain yang tersedia; petugas tidak melanjutkan pelayanan dari claim yang ditolak.

### 5.6 Patient atau Visitor tidak hadir setelah Call

- **Admission Officer** menjalankan Recall untuk Queue Entry yang sama ketika panggilan lain sesuai.
- **Patient Tracker Queue Service** mempertahankan Queue Label yang sama, menaikkan CallCount, memperbarui `BILRG_AdmLoketCurrentCall`, dan menaikkan AnnouncementVersion ketika Recall membutuhkan audio. Riwayat detail Call Attempt tidak disimpan dalam V1.
- **Queue Display** menampilkan dan, bila diaktifkan, mengumumkan Recall dengan Queue Label dan Loket yang sama.
- CallCount hanya informasional. Sistem tidak menghitung threshold, menetapkan No-Show, menunda entry, menerapkan aturan “lima pasien berikutnya”, atau memilih entry berikutnya.
- **Admission Officer** secara manual meminta disposition dari **Queue Operations Supervisor** sesuai kebijakan rumah sakit.
- **Queue Operations Supervisor** memilih disposition yang disetujui: mempertahankan Queue Entry sebagai `Waiting` untuk pelayanan kemudian atau menyimpulkannya sebagai `Withdrawn`/No-Show.
- **Admission Module** menampilkan disposition yang dihasilkan; **Admission Officer** tidak menandai Queue Entry sebagai `Done` hanya karena Patient atau Visitor tidak hadir.

### 5.7 Patient atau Visitor memilih Service Point yang tidak berlaku

- **Admission Officer** berhenti sebelum memulai Registration Assistance ketika ketidaksesuaian dikenali saat Queue Entry masih `Waiting`.
- **Admission Officer** meminta pengalihan ke Service Point lain kepada **Queue Operations Supervisor**.
- **Queue Operations Supervisor** menyetujui atau menolak pengalihan menggunakan bukti yang tersedia. Jika disetujui, entry asal menerima disposition nonaktif eksplisit dan entry tujuan baru dibuat dengan Priority, CreationReason `Redirected`, serta SourceAntrianEntryId wajib.
- Ketika disetujui, **Patient Tracker Queue Service** menampilkan Queue Entry asli sebagai `Withdrawn` dan menerbitkan Queue Entry serta Queue Label pengganti untuk Service Point yang berlaku.
- **Admission Module** menampilkan indikator Priority dan hubungan dengan Queue Entry asli. Priority tidak memaksa urutan pemanggilan; **Admission Officer** mempertahankan otoritas pemilihan serta menyampaikan Queue Label pengganti dan lokasi menunggu.

### 5.8 Queue Display atau audio tidak tersedia

- **Queue Display** menampilkan kondisi tidak tersedia ketika dapat diamati.
- **Queue Display** memuat ulang `BILRG_AdmLoketCurrentCall` setelah reconnect dan pada setiap interval polling terkonfigurasi; SignalR hanya merupakan refresh trigger.
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
- Masalah validasi tidak membuat Registration Outcome atau OutcomeId.
- **Admission Officer** tidak menyelesaikan pelayanan antrean sampai registration outcome yang accountable ditampilkan.

### 5.12 Registration tidak terbentuk setelah penyelesaian accountable

- **Admission Officer** secara eksplisit memutuskan Result final `NotEstablished` dan memilih ReasonCode wajib setelah penyelesaian accountable; Result ini tidak disimpulkan dari error, timeout, atau tidak adanya Registration.
- **Admisi Rajal Registration Service** mempersistenkan OutcomeId, QueueEntryId, Result `NotEstablished`, ReasonCode, Explanation ketika diisi, DecidedAt, dan DecidedBy tanpa RegId.
- **Admisi Rajal Registration Service** menampilkan outcome `Registration Not Established` yang telah dipersistenkan beserta penjelasan operasional yang tersedia.
- **Admission Officer** menyampaikan outcome dan tindakan selanjutnya yang disetujui kepada Patient atau Visitor.
- Ketika tidak ada Registration Assistance lebih lanjut, **Admission Officer** menyelesaikan pelayanan antrean menggunakan OutcomeId tersebut; **Admission Module** menampilkan Queue Entry sebagai `Done` tanpa menampilkan Outpatient Registration sebagai telah terbentuk.
- Queue Entry dapat tetap Anonymous ketika tidak ada Patient Journey yang dibentuk; outcome dan queue completion tidak membentuknya.

### 5.13 Queue Session telah mengalokasikan Queue Number 9999

- **Patient Tracker Queue Service** menolak alokasi Queue Number berikutnya dari Queue Session tersebut setelah Queue Number 9999 dialokasikan.
- **Patient Tracker Queue Service** tidak mengulang counter, menerbitkan nomor di atas 9999, mengubah format Queue Label, atau membuat session kedua untuk Service Point dan Business Date yang sama.
- **Kiosk** menampilkan bahwa Service Point yang dipilih tidak dapat menerima intake lanjutan pada Business Date saat ini dan tidak otomatis mencoba ulang menggunakan tanggal pilihan client.
- **Queue Operations Supervisor** mengarahkan Patient atau Visitor menuju Service Point lain yang aktif ditawarkan ketika berlaku secara operasional; jika tidak, intake Service Point tersebut dilanjutkan hanya ketika server menyelesaikan Business Date berikutnya dan session berikutnya dapat dibentuk secara lazy.
- Queue Label yang telah diterbitkan dari Queue Session yang habis tetap tidak berubah dan tetap valid untuk Queue Entry yang sudah ada.

## 6. Kriteria Penyelesaian

Prosedur selesai ketika semua hasil yang dapat diamati dan berlaku tersedia:

1. Patient atau Visitor menerima satu Queue Label untuk Service Point yang dipilih.

2. Queue Call menampilkan Queue Label yang sama dan Loket tujuan.

3. Queue Entry berubah menjadi `In Service` hanya setelah Patient atau Visitor hadir dan Registration Assistance dimulai.

4. Jalur Booking menggunakan kembali existing Booking Patient Tracker yang berlaku, atau jalur Walk-In memilih Patient Tracker existing yang berlaku atau mengasosiasikan entry dengan Tracker yang dibentuk oleh Registration.

5. Admission Module menampilkan Registration Outcome final yang telah dipersistenkan dengan OutcomeId stabil: `Established` dengan RegId, atau `NotEstablished` dengan ReasonCode.

6. Queue Entry menampilkan `Done` setelah Registration Assistance berakhir, atau menampilkan `Withdrawn` ketika prosedur yang disetujui mengakhiri keikutsertaan sebelum mulai pelayanan.

7. Queue Entry yang selesai atau Withdrawn tidak terdapat dalam Work List Waiting dan In Service yang aktif.

8. Loket terkonfigurasi tidak memiliki Queue Entry yang Outstanding atau In Service yang tersisa dari prosedur ini.

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
