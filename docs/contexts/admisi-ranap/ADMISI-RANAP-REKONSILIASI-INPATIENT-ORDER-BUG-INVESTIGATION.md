# BUG-INVESTIGATION

## Context

Issue: [ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-ISSUE.md) (ISSUE-ADMISI-RANAP-REKONSILIASI-ORDER-001)  
Problem Summary: Pasien hasil pendaftaran reservasi (contoh: "SRI AJENG RETNO", No. RM `00-00-60-63`, order `OPN0A31CDRTQ`) terkunci pada status badge merah **"Perlu Pemeriksaan Data"** (`NeedsReconciliation`) alih-alih berstatus **"Menunggu Diterima Bangsal"** (`WardAcceptanceRequired`). Selain itu, panel utama (*Journey Workspace*) mengalami *infinite loading / hang* (menampilkan *skeleton* tanpa henti) saat kartu pasien diklik dari worklist panel samping kiri.  

Evidence Source:
1. Tangkapan layar worklist admisi: [admisi-ranap-perlu-pemeriksaan-data-hang.png](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/docs/modules/admisi/evidence/admisi-ranap-perlu-pemeriksaan-data-hang.png)
2. Implementasi validasi integritas: [JourneyIntegrityValidator.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/JourneyIntegrityValidator.cs#L43-L67)
3. Implementasi penentu tahap: [Release1JourneyStageResolver.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolver.cs#L51-L60) & [Release1JourneySqlStageExpression.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/Release1JourneySqlStageExpression.cs#L38-L45)
4. Implementasi DAL kueri fakta: [JourneyDal.Batch.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs#L248-L267) & [JourneyDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs#L713-L775)
5. Implementasi pendaftaran admisi: [AdmissionRegistrationOrchestrator.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs#L254-L260)
6. Implementasi state pemuatan frontend: [useJourneyQueryState.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyQueryState.ts#L90-L110)

## Current State

1. **Pendaftaran Pasien**:
   - Pengguna membuat reservasi rawat inap baru dan sistem menerbitkan pesanan terpadu pada `BILRG_AdmInpatientOrder` dengan ID `OPN0A31CDRTQ` (`OrderSource = 3` / `AdmisiBooking`).
   - Pengguna memproses registrasi admisi rawat inap untuk pasien tersebut melalui endpoint `POST /api/admission/from-reservation`.
   - Data berhasil tersimpan di tabel `ta_registrasi`, `ta_reg_inap`, `BILRG_BedWaitingList`, dan `BILRG_AdmAdmission`.

2. **Tampilan Worklist Pasien Aktif**:
   - Kartu pasien muncul di daftar panel kiri worklist dengan nomor RM `00-00-60-63`.
   - Kartu pasien menampilkan badge merah bertuliskan **"Perlu Pemeriksaan Data"** dengan pesan:
     > *"Perlu rekonsiliasi data — Otoritas Release 1 tidak dapat menentukan tahap operasional secara aman..."*
   - Status antrean bangsal yang sesungguhnya sudah terbentuk di `BILRG_BedWaitingList` (`WaitingListStatusEnum.Waiting`) tidak terefleksikan.

3. **Perilaku Klik Kartu Pasien (Hang / Infinite Loading)**:
   - Saat pengguna mengeklik kartu pasien tersebut, panel kanan (*Journey Workspace*) tidak menampilkan informasi detail perjalanan, melainkan tertahan pada status *skeleton loading* secara terus-menerus.
   - Pengguna mempersepsikan sistem "sangat lama menampilkan data" atau *hang*, karena halaman tidak kunjung selesai memuat.

## Problem Analysis

Investigasi mendalam pada alur eksekusi backend dan integrasi antarmuka frontend menemukan dua rantai masalah teknis yang saling terkait:

### 1. Akar Masalah Label Merah "Perlu Pemeriksaan Data" (`NeedsReconciliation`)

- **Pemetaan Ganda Sumber di Lapisan Data (`JourneyDal.Batch.cs` dan `JourneyDal.cs`)**:
  Pada kueri proyeksi data admisi, terdapat logika SQL yang memasukkan nilai `InpatientOrderId` ke dalam dua field warisan secara serentak:
  ```sql
  SELECT a.RegId, a.AdmissionStatus, a.PasienId,
         COALESCE(NULLIF(a.InpatientOrderId,'-'), a.OpnameRequestId) AS OpnameRequestId,
         COALESCE(NULLIF(a.InpatientOrderId,'-'), a.ReservationId) AS ReservationId,
         ...
  FROM BILRG_AdmAdmission a
  ```
  Karena entitas admisi membawa `InpatientOrderId = 'OPN0A31CDRTQ'`, proyeksi ini menghasilkan:
  - `dto.OpnameRequestId = 'OPN0A31CDRTQ'`
  - `dto.ReservationId = 'OPN0A31CDRTQ'`

- **Dual-Source Feeding pada `AdmissionRegistrationOrchestrator.cs`**:
  Pada alur registrasi reservasi:
  ```csharp
  var admission = CreateAdmission(order.Pasien, request.KelasDkId, request.BangsalId,
      null, order.InpatientOrderId, request.UserId, occurredAt, bangsal, inpatientOrderId: order.InpatientOrderId);
  ```
  Nilai order dioperkan ke parameter `reservationId` lama sekaligus `inpatientOrderId`.

- **Pemicu Pelanggaran Integritas pada `JourneyIntegrityValidator.cs`**:
  Metode `ValidateAdmissionSources` mengevaluasi:
  ```csharp
  var hasOpn = JourneyIdFactory.HasValue(admission.OpnameRequestId); // Bernilai TRUE ('OPN0A31CDRTQ')
  var hasRsv = JourneyIdFactory.HasValue(admission.ReservationId);   // Bernilai TRUE ('OPN0A31CDRTQ')
  if (hasOpn && hasRsv)
  {
      issues.Add(Issue(
          JourneyReconciliationCodes.AdmissionHasBothSources,
          $"Admission {admission.RegId} references both an Opname Request and a Reservation."));
  }
  ```
  Sistem mendeteksi bahwa satu admisi mereferensikan dua sumber sekaligus, yang di dalam aturan Release 1 diklasifikasikan sebagai *blocking integrity issue* fatal (`AdmissionHasBothSources`).

- **Pemaksaan Tahap Operasional oleh `Release1JourneyStageResolver.cs`**:
  Berdasarkan klausul prioritas:
  ```csharp
  if (integrity.HasBlockingIssues)
      return Reconciliation(facts, admission, integrity);
  ```
  Evaluasi antrean bangsal (`ActiveWaitingListStatus == Waiting`) diabaikan dan tahap operasional langsung dipaksa menjadi `JourneyOperationalStage.NeedsReconciliation` (label merah "Perlu Pemeriksaan Data").

---

### 2. Akar Masalah Hang / Infinite Skeleton Loading saat Kartu Diklik

- **Mismatch JourneyId antara Worklist dan Detail**:
  - Di worklist panel kiri, kartu pasien dibentuk dengan JourneyId awal `rsv:OPN0A31CDRTQ`.
  - Ketika kartu diklik, frontend mengatur `selectedJourneyId = "rsv:OPN0A31CDRTQ"` dan mengirim permintaan `GET /api/admisi-ranap/journeys/rsv:OPN0A31CDRTQ`.
  - Backend mengeksekusi `LoadReservationJourneyAsync` dan memanggil `JourneyIdFactory.Derive(facts, integrity)`.
  - Karena `integrity.HasBlockingIssues` bernilai `true` (akibat issue dual-source di atas), `JourneyIdFactory.Derive` mengaktifkan fallback ID:
    ```csharp
    if (integrity.HasBlockingIssues)
    {
        var primaryAdmission = SelectPrimaryAdmission(facts);
        if (primaryAdmission is not null)
            return FromRegId(primaryAdmission.RegId); // Mengembalikan "reg:RG..."
    }
    ```
  - Backend mengembalikan data detail perjalanan dengan atribut `journeyId: "reg:RG0A31..."` (bukan `rsv:OPN0A31CDRTQ`).

- **Perangkap State Loading di Frontend (`useJourneyQueryState.ts`)**:
  Pada composable penanganan state query di `c012_myhospital_web`:
  ```typescript
  const data = query.data.value as { journeyId?: string } | null | undefined
  const dataMatchesSelection = data?.journeyId === journeyId // "reg:RG..." === "rsv:OPN..." -> FALSE

  if (!dataMatchesSelection) {
    if (readBool(query.isPending) || readBool(query.isFetching) || data == null) {
      return { kind: 'loading' }
    }
    return { kind: 'loading' } // Terkunci di state loading!
  }
  ```
  Karena ID data yang diterima dari backend (`reg:...`) tidak cocok dengan ID kartu yang sedang dipilih (`rsv:...`), frontend berasumsi bahwa respons yang tiba bukanlah untuk kartu terpilih, sehingga frontend **terus-menerus mengembalikan status `{ kind: 'loading' }`**. Hal ini menyebabkan komponen [JourneyWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/journey/JourneyWorkspace.vue#L106-L115) terus merender *skeleton placeholder* tanpa batas waktu (*hang*).

---

### 3. Evaluasi Kolom Warisan `OpnameRequestId` & `ReservationId` pada `BILRG_AdmAdmission`

- Di basis data, tabel `BILRG_AdmAdmission` memiliki kolom `InpatientOrderId` yang telah diperkenalkan untuk menggantikan kolom lama `OpnameRequestId` dan `ReservationId`.
- Konstruktor domain `AdmissionModel.Admit` telah memiliki overload yang hanya menerima `inpatientOrderId` (mengisi `OpnameRequestId` dan `ReservationId` dengan tanda `-`).
- Namun, lapisan orkestrasi pendaftaran (`AdmissionRegistrationOrchestrator.cs`), DAL penyimpanan (`AdmissionDal.cs`), dan DAL proyeksi perjalanan (`JourneyDal.cs`, `JourneyDal.Batch.cs`) masih aktif membaca, mengisi, dan memvalidasi kedua kolom warisan tersebut.

## Affected Components

1. **Modul Backend Admisi Ranap (`b09-bilreg-api`)**:
   - `Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases.AdmissionRegistrationOrchestrator`: Alur `ProcessReservation` dan `ProcessOpnameRequest` masih memasok parameter ke field lama.
   - `Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature.AdmissionDal`: Metode `Insert`, `Update`, dan pemetaan parameter masih melibatkan `OpnameRequestId` dan `ReservationId`.
   - `Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature.JourneyDal.Batch`: Metode `BuildAdmissionSelectSql` yang memproyeksikan `InpatientOrderId` ke `OpnameRequestId` dan `ReservationId`.
   - `Bilreg.Infrastructure.AdmisiRanapContext.JourneyFeature.JourneyDal`: Metode kueri `FindAdmissionByOpnameAsync`, `FindAdmissionByReservationAsync`, dan CTE `AdmissionRoots`.
   - `Bilreg.Application.AdmisiRanapContext.JourneyFeature.JourneyIntegrityValidator`: Aturan `ValidateAdmissionSources` yang masih mendasarkan validasi integritas pada keberadaan kedua kolom warisan.
   - `Bilreg.Application.AdmisiRanapContext.JourneyFeature.JourneyIdFactory`: Logika derivasi JourneyId fallback pada saat terjadi anomali integritas.

2. **Skema Basis Data**:
   - Tabel `BILRG_AdmAdmission`: Kolom `OpnameRequestId` dan `ReservationId` masih ada di skema fisik.

3. **Modul Frontend Konsol Admisi Ranap (`c012_myhospital_web`)**:
   - `src/modules/Admisi/composables/useJourneyQueryState.ts`: Mekanisme pencocokan `data.journeyId === selectedJourneyId`.
   - `src/modules/Admisi/components/ranap/journey/JourneyWorkspace.vue`: Tampilan *skeleton loading* saat state `loading`.

## Impact Assessment

- **Dampak Bisnis**: Petugas admisi rawat inap terhambat dalam memantau dan memvalidasi status pasien karena pasien tidak masuk ke tahap "Menunggu Diterima Bangsal", melainkan terkunci pada peringatan rekonsiliasi data darurat.
- **Dampak Operasional**: Petugas tidak dapat membuka rincian perjalanan pasien (*Journey Workspace*) dari panel daftar pasien karena antarmuka mengalami *freeze/hang* pada *skeleton loader*.
- **Dampak Teknis**: Munculnya sinyal anomali integritas palsu (*false-positive integrity contradiction*) yang merusak alur state machine Release 1 dan memicu kegagalan sinkronisasi ID antara lapisan presentasi dan lapisan data.

## Assumptions

1. Tabel `BILRG_AdmInpatientOrder` merupakan sumber kebenaran tunggal (*single source of truth*) untuk seluruh pesanan rawat inap, dengan atribut `OrderSource` sebagai pembeda asal (Rawat Jalan / IGD / Booking Reservasi / Eksternal).
2. Kolom fisik `OpnameRequestId` dan `ReservationId` pada tabel `BILRG_AdmAdmission` di basis data tidak perlu di-DROP secara DDL mendadak demi keselamatan integritas riwayat lama, namun harus dilepaskan sepenuhnya dari logika bisnis aktif dan kueri DAL.
3. Pesanan rawat inap yang telah berstatus `Fulfilled` (`OrderStatus = 2`) terhubung secara sah 1-ke-1 dengan satu pendaftaran rawat inap (`RegId`).

## Open Questions

1. *Tidak ada pertanyaan terbuka yang memblokir.* Alur domain dan teknis telah sepenuhnya terkonfirmasi melalui penelusuran kode sumber dan bukti transaksi nyata.

## Recommended Decision

- Melepaskan ketergantungan logika bisnis admisi dan perjalanan rawat inap dari kolom warisan `OpnameRequestId` dan `ReservationId`, serta mengalihkan seluruh referensi sumber pesanan ke `InpatientOrderId`.
- Menghapus pemetaan ganda `COALESCE` di seluruh kueri DAL `JourneyDal` dan `JourneyDal.Batch` agar satu order tidak lagi diproyeksikan sebagai dua sumber yang saling bertentangan.
- Memperbarui aturan validasi integritas `JourneyIntegrityValidator` agar mengenali `InpatientOrderId` dan menggunakan `OrderSource` sebagai penentu keabsahan sumber admisi, sehingga menghilangkan deteksi *false-positive* `AdmissionHasBothSources`.

## Decision

Mengadopsi penyelarasan terpadu berbasis `InpatientOrderId` pada seluruh siklus hidup admisi ranap:
1. **Orkestrasi Pendaftaran**: Menggunakan overload `AdmissionModel.Admit` murni dengan `inpatientOrderId`, mengosongkan parameter `opnameRequestId` dan `reservationId` (diberi tanda `-` atau kosong).
2. **Pembersihan Lapisan DAL**: Menghilangkan logika `COALESCE` ganda pada kueri `BuildAdmissionSelectSql`, `FindAdmissionByReservationAsync`, `FindAdmissionByOpnameAsync`, dan CTE `AdmissionRoots`.
3. **Penyelarasan Validasi Integritas**: Memperbarui `JourneyIntegrityValidator.cs` agar mengevaluasi keabsahan admisi berdasarkan `InpatientOrderId` dan relasi ke `BILRG_AdmInpatientOrder`, bukan berdasarkan benturan antara `OpnameRequestId` dan `ReservationId`.
4. **Konsistensi JourneyId**: Memastikan ID perjalanan yang dihasilkan konsisten (`rsv:OPN...` untuk booking/reservasi, `opn:OPN...` untuk permintaan opname), sehingga sinkron dengan `selectedJourneyId` pada konsol antarmuka frontend.

## Decision Rationale

1. **Menghilangkan Akar Masalah Integritas**: Dengan tidak lagi memetakan `InpatientOrderId` ke kedua field lama, kondisi `hasOpn && hasRsv` tidak akan pernah terpenuhi secara palsu, sehingga `integrity.HasBlockingIssues` bernilai `false` dan tahap operasional dapat meluncur dengan benar ke `WardAcceptanceRequired` ("Menunggu Diterima Bangsal").
2. **Menyelesaikan Hang/Infinite Loading di Frontend**: Tanpa adanya *blocking issue*, backend tidak akan mengubah JourneyId menjadi fallback `reg:RG...`, melainkan tetap mempertahankan `rsv:OPN...`. Hal ini membuat `data.journeyId === selectedJourneyId` bernilai `true` di frontend, sehingga status pemuatan langsung beralih ke `ready` dan workspace langsung tampil seketika.
3. **Pembersihan Arsitektur Teknis**: Melanjutkan arah refactoring konsolidasi tabel `BILRG_AdmInpatientOrder` secara konsisten di seluruh lapisan (domain, aplikasi, infrastruktur, dan presentasi).

## Architecture Applicability

### Decision

**ARCHITECTURE-REQUIRED**

### Rationale

Perbaikan ini membutuhkan penyesuaian formal pada target state arsitektur teknis karena:
- Melibatkan perubahan kontrak kepemilikan dan relasi data persistensi pada `BILRG_AdmAdmission` (pelepasan ketergantungan kolom warisan `OpnameRequestId`/`ReservationId` menuju `InpatientOrderId`).
- Mengubah spesifikasi aturan validasi integritas lintas-entitas pada `JourneyIntegrityValidator` dan penentu tahap operasional `Release1JourneySqlStageExpression` / `Release1JourneyStageResolver`.
- Membutuhkan rencana implementasi terkoordinasi antara perubahan domain, repositori DAL, dan pengujian regresi integritas perjalanan Release 1.
