# DOMAIN.md — IGD Visit

## 1. Overview

`IGD Visit` adalah bounded-context yang menangani proses pelayanan pasien pada Instalasi Gawat Darurat (IGD).

Bounded-context ini berfokus pada:

* pelayanan klinis emergensi,
* triage,
* observasi pasien,
* pengelolaan bed IGD,
* tindakan medis,
* penggunaan BHP,
* hingga discharge pasien.

Seluruh proses operasional IGD menggunakan:

```text id="g9wvq5"
IgdVisitId
```

sebagai identitas utama pelayanan.

Registrasi administratif (`RegId`) bersifat terpisah dan dapat dilakukan setelah pelayanan medis dimulai.

---

# 2. Core Principles

## 2.1 Clinical Flow First

Pelayanan medis emergensi tidak boleh tertunda oleh proses administratif.

Pasien dapat:

* dilakukan triage,
* ditempatkan ke bed,
* mendapatkan tindakan,
* menggunakan BHP,

meskipun registrasi administratif belum selesai.

---

## 2.2 IgdVisit sebagai Operational Identity

Seluruh aktivitas operasional IGD menggunakan:

```text id="h0sh4u"
IgdVisitId
```

dan bukan:

```text id="oz7d6p"
RegId
```

---

## 2.3 Administrative Registration is Supporting Process

Registrasi administratif digunakan untuk:

* billing,
* legalitas administrasi,
* integrasi sistem legacy.

Billing hanya dapat dilakukan apabila `IgdVisit` telah memiliki `RegId`.

---

## 2.4 Bed adalah Shared Operational Resource

Bed IGD adalah resource operasional yang:

* terbatas,
* dinamis,
* dapat berubah sewaktu-waktu berdasarkan prioritas kegawatan pasien.

Pasien dengan prioritas lebih rendah dapat dipindahkan apabila dibutuhkan untuk pasien emergensi prioritas lebih tinggi.

---

# 3. Workflow Overview

```text id="39ay5k"
Pasien Datang
    ↓
Daftar IGD Visit
    ↓
Assign Dokter
    ↓
Assessment Triage
    ↓
┌─────────────────────────────┐
│ Redirect ke Rawat Jalan ?   │
└─────────────────────────────┘
        ↓ Ya                           ↓ Tidak
Redirect Rawat Jalan             Assign Bed
                                         ↓
                                  Observasi/Tindakan
                                         ↓
                                 Registrasi Administratif
                                         ↓
                                     Discharge
```

---

# 4. Ubiquitous Language

| Term                 | Description                              |
| -------------------- | ---------------------------------------- |
| IgdVisit             | Identitas operasional pelayanan IGD      |
| Visitor              | Identitas awal pasien sebelum registrasi |
| Register             | Registrasi administratif resmi           |
| Triage               | Assessment tingkat kegawatan pasien      |
| Bed IGD              | Tempat observasi/tindakan pasien         |
| Observed             | Kondisi pasien sedang menempati bed      |
| Redirect Rawat Jalan | Pengalihan pasien ke layanan rawat jalan |
| Discharge            | Penyelesaian pelayanan IGD               |
| Void Visit           | Pembatalan visit IGD                     |
| Tindakan IGD         | Tindakan medis pasien                    |
| BHP                  | Barang habis pakai medis                 |

---

# 5. Aggregate Design

# 5.1 Aggregate Root — IgdVisit

`IgdVisit` adalah aggregate root utama pada bounded-context IGD.

Aggregate ini menjadi source of truth untuk:

* current operational state pasien,
* current doctor,
* current triage result,
* current occupancy,
* current administrative state.

---

## Responsibilities

`IgdVisit` bertanggung jawab untuk:

* menyimpan current state pelayanan pasien,
* mengontrol state transition,
* menjaga invariant domain,
* menjadi pusat orkestrasi operasional IGD.

---

## Properties

| Property            | Description                          |
| ------------------- | ------------------------------------ |
| IgdVisitId          | Identifier visit                     |
| AuditTrail          | Audit create/update                  |
| Visitor             | Identitas awal pasien                |
| Dokter              | Dokter aktif                         |
| HasTriage           | Status apakah triage sudah dilakukan |
| Triage              | Hasil triage pasien                  |
| AdministrativeState | State administratif visit            |
| Reg                 | Registrasi administratif             |
| Redirection         | Informasi redirect rawat jalan       |
| BedId               | Bed aktif pasien                     |
| ListEvent           | Audit event operasional              |

---

# 5.2 Administrative State

| State      | Description               |
| ---------- | ------------------------- |
| Daftar     | Visit baru dibuat         |
| Registered | Sudah memiliki registrasi |
| Redirected | Dialihkan ke rawat jalan  |
| Discharged | Pelayanan selesai         |

---

# 5.3 Derived State

## HasObserved

```text id="tzdjq1"
BedId != Default
```

Jika pasien memiliki bed aktif maka pasien dianggap sedang:

* observasi,
* tindakan,
* atau monitoring di IGD.

---

# 6. Supporting Entities

Berikut adalah tambahan section yang menurut saya cukup untuk melengkapi `DOMAIN.md` Anda tanpa membuatnya terlalu panjang. Tambahkan saja setelah section `# 6.1 IgdVisitTriage` atau rename section tersebut menjadi `# 6.1 TriageRecord`.

---

# 6.1 TriageRecord

Menyimpan histori assessment triage pasien.

Satu `IgdVisit` dapat memiliki banyak `TriageRecord`.

`TriageRecord` bersifat immutable dan append-only.

Assessment lama tidak boleh diubah.

---

## Responsibilities

* menyimpan detail assessment triage,
* menyimpan hasil scoring dan klasifikasi triage,
* menyimpan histori re-assessment,
* mendukung audit klinis dan medico-legal.

---

## Assessment Components

Assessment ATS terdiri dari:

* Airways
* Breathing
* Blood Circulation
* GCS (Eye, Motor, Voice)

---

## Triage Result

Hasil assessment menghasilkan:

* ATS Level

    * ATS1
    * ATS2
    * ATS3
    * ATS4
    * ATS5

dan dikonversikan menjadi:

* Red
* Yellow
* Green

Khusus warna `Black` bersifat manual override oleh dokter dan tidak berasal dari hasil scoring ATS.

---

## Re-Assessment

Pasien dapat dilakukan triage ulang (`Re-Assessment Triage`) apabila diperlukan.

Setiap re-assessment:

* membuat `TriageRecord` baru,
* memperbarui current triage state pada `IgdVisit`,
* tetap menyimpan histori assessment sebelumnya.

---

## Current Triage State

`IgdVisit` hanya menyimpan current/latest triage state untuk kebutuhan operasional realtime.

History lengkap assessment disimpan pada `TriageRecord`.

---

## Triage Monitoring

Setiap hasil triage memiliki rekomendasi waktu reassessment.

Contoh:

| ATS  | Re-Assessment         |
| ---- | --------------------- |
| ATS1 | Continuous Monitoring |
| ATS2 | 15 menit              |
| ATS3 | 30 menit              |
| ATS4 | 60 menit              |
| ATS5 | 120 menit             |

---

## Operational Monitoring State

`IgdVisit` menyimpan:

* LastTriageAt
* NextReTriageAt

untuk kebutuhan:

* dashboard monitoring,
* countdown re-triage,
* overdue monitoring.

Frontend hanya menampilkan countdown realtime.

Perhitungan waktu reassessment dilakukan oleh backend.

---

# 6.1.1 Triage Method

Sistem mendukung konsep multiple triage methods.

Contoh:

* ATS
* ESI
* CTAS
* MTS

Saat ini implementasi aktif menggunakan metode `ATS`.

Perhitungan scoring dan klasifikasi dilakukan oleh `Triage Method Engine`.

---

# 6.2 IgdVisitEvent

Menyimpan histori event operasional pasien IGD.

## Responsibilities

* audit operasional,
* timeline aktivitas,
* tracking perubahan operasional.

Event bukan event sourcing.

Event hanya audit trail operasional.

---

# 6.3 BedIgd

Representasi master dan current occupancy bed IGD.

## Responsibilities

* menyimpan current occupancy,
* menyimpan availability bed,
* menjadi source of truth occupancy.

---

## Bed State

| State       | Description             |
| ----------- | ----------------------- |
| Active      | Bed siap digunakan      |
| Occupied    | Sedang digunakan pasien |
| Maintenance | Bed maintenance         |
| Dirty       | Membutuhkan cleaning    |

---

# 6.4 PakaiBed

Riwayat penggunaan bed pasien.

## Responsibilities

* menyimpan histori penggunaan bed,
* menyimpan waktu check-in/check-out,
* mendukung audit occupancy.

---

# 6.5 RedirectRajal

Transaksi pengalihan pasien ke rawat jalan.

---

# 6.6 TindakanIgd

Transaksi tindakan medis pasien.

---

# 6.7 BhpIgd

Transaksi pemakaian barang habis pakai pasien.

---

# 7. Business Rules

---

# 7.1 Triage Rule

Assessment triage dilakukan oleh dokter jaga IGD.

Input data ke sistem dapat dilakukan oleh:

* dokter,
* perawat,
* admin IGD.

Triage terdiri dari:

* assessment,
* scoring,
* classification,
* reassessment monitoring.

Pasien dapat dilakukan `Re-Assessment Triage` sesuai kebutuhan klinis.


---

# 7.2 Delayed Registration Rule

Pelayanan medis dapat dilakukan tanpa registrasi administratif.

Registrasi dapat dilakukan setelah tindakan medis dimulai.

---

# 7.3 Billing Rule

Billing hanya dapat dilakukan apabila:

```text id="sh4obv"
IgdVisit telah memiliki RegId
```

---

# 7.4 Bed Occupancy Rule

Satu bed hanya boleh ditempati satu pasien aktif.

---

# 7.5 Bed Priority Rule

Pasien dengan prioritas kegawatan lebih tinggi dapat mengambil prioritas penggunaan bed dibanding pasien prioritas lebih rendah.

---

# 7.6 Void Visit Rule

Visit hanya dapat di-void apabila:

* belum terdapat transaksi tindakan,
* belum terdapat transaksi BHP.

---

# 7.7 Discharge Rule

Pasien hanya dapat discharge apabila:

* telah memiliki registrasi administratif,
* sudah tidak menempati bed aktif.

---

# 8. Concurrency Rules

## 8.1 BedIgd adalah Source of Truth

Occupancy bed ditentukan oleh:

```text id="hljlwm"
BedIgd.IgdVisitId
```

---

## 8.2 Assign Bed Must Validate Current Occupancy

Proses assign bed wajib memastikan:

* bed masih kosong,
* bed masih active,
* occupancy belum berubah.

---

## 8.3 Current State vs Transaction History

## Current State

Digunakan untuk operasional realtime:

* IgdVisit
* BedIgd

## Transaction History

Digunakan untuk histori:

* PakaiBed
* TriageAssessment
* TindakanIgd
* BhpIgd
* RedirectRajal

---

# 9. Integration Notes

# 9.1 Register (Legacy HIS)

Registrasi administratif tetap menggunakan sistem legacy.

IGD hanya menyimpan mapping:

* RegId
* PasienId

---

# 9.2 Billing (Legacy HIS)

Billing dilakukan pada sistem legacy.

IGD menyediakan data:

* tindakan,
* BHP,
* occupancy,
* registrasi.

---

# 10. Use Cases

| Code  | Use Case           |
| ----- | ------------------ |
| UC01  | DaftarIgdVisit     |
| UC02  | AssignDokter       |
| UC03  | AssessTriage       |
| UC04  | AssignBed          |
| UC04a | CheckOut           |
| UC05  | RedirectRawatJalan |
| UC06  | Tindakan           |
| UC07  | PakaiObatBhp       |
| UC08  | AssignRegister     |
| UC09  | Discharge          |
| UC10  | VoidVisit          |

---

# 11. Architectural Notes

## 11.1 Design Philosophy

Design menggunakan:

* Rich Domain Model,
* Explicit Orchestration,
* Current State + Transaction History,
* Tactical DDD,
* Clean Architecture.

---

## 11.2 Avoid Over Engineering

Sistem tidak menggunakan:

* Event Sourcing,
* Distributed Saga,
* Complex Workflow Engine.

Pendekatan yang digunakan:

* explicit transaction,
* explicit orchestration,
* maintainable monolith architecture.

---

# 12. Future Considerations

Potensi pengembangan berikutnya:

* Take Over Dokter
* Bed Reservation
* Multi Bed Transfer
* Observation Monitoring
* Emergency Escalation
* Transfer Rawat Inap
* ICU Transfer
* Death Handling
* Medical Resume
* Nursing Notes
* Vital Sign Monitoring
* Integration BPJS / Insurance
* Queue Dashboard
* Real-time Occupancy Dashboard
