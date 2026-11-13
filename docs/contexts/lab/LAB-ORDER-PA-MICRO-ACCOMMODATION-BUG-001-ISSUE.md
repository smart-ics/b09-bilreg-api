# ISSUE

## Metadata

ID: ISSUE-LAB-ORDER-PA-MICRO-ACCOMMODATION-BUG-001
Type: BUG
Status: RESOLVED
Title: Kegagalan Pembuatan Order Laboratorium dari EMR Akibat ArgumentNullException pada Pemeriksaan Idempotensi GetByEmrOrderId

## Source

Reported By: Human Tester via ica-tester
Reported Date: 2026-11-04
Originating Test Execution: [LAB-ORDER-PA-MICRO-ACCOMMODATION-TEST-EXECUTION.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-TEST-EXECUTION.md) (TC-LPAM-01)
Related Feature: LAB-ORDER-PA-MICRO-ACCOMMODATION
Severity: BLOCKER

## Description

Saat dokter/perawat mengirimkan permintaan laboratorium baru dari modul EMR canvas (`LabOrderCreateCanvas.vue`), sistem gagal memproses dan menyimpan pesanan. Permintaan HTTP `POST /api/LabContext/LabOrderFeature/fromEmr` mengembalikan respon galat HTTP 400 Bad Request dengan pesan `"Value cannot be null. (Parameter 'source')"`. Hal ini memblokir seluruh proses penerbitan order lab internal dari EMR.

## Desired Outcome

Sistem berhasil memproses dan menyimpan permintaan laboratorium dari EMR tanpa galat, menerbitkan nomor order lab yang valid, menyimpan snapshot klasifikasi layanan/disiplin/grup tes pada item order, dan mengembalikan respon sukses HTTP 200 dengan informasi order terbit.

## Current Situation

Pengecekan awal keberadaan order eksisting berdasarkan `EmrOrderId` (`GetByEmrOrderId`) di backend mengalami runtime exception ketika order belum pernah dibuat di database, menyebabkan handler melempar `ArgumentNullException` dan seluruh alur pembuatan order terhenti.

## Evidence

- **Test Execution Reference**: [LAB-ORDER-PA-MICRO-ACCOMMODATION-TEST-EXECUTION.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-TEST-EXECUTION.md) (TC-LPAM-01, Status: FAIL).
- **API Endpoint**: `POST /api/LabContext/LabOrderFeature/fromEmr`
- **Request Payload**:
  ```json
  {
      "emrOrderId": "LAB-INT-261104-151806-FWXH",
      "regId": "RGA7AQBUR1",
      "patientId": "337502200000001",
      "patientName": "AKSADARU MAHAMERU, AN",
      "birthDate": "2024-06-06",
      "gender": "L",
      "items": [
          {
              "tarifId": "LAB0103002",
              "tarifCode": "LAB0103002",
              "tarifName": "HEMOGLOBIN (Hb.)",
              "isCito": false
          },
          {
              "tarifId": "LAB0101006",
              "tarifCode": "LAB0101006",
              "tarifName": "SGOT",
              "isCito": false
          }
      ],
      "orderNo": "",
      "userId": "SPR001",
      "dokterPerujukId": "DR00103",
      "dokterPerujukName": "MIRA ASTUTI, dr. Sp. PD, Finasim",
      "layananPerujukId": "3RI02",
      "layananPerujukName": "ST.ANNA",
      "clinicalNote": "Evaluasi anemia dan fungsi hati rutin",
      "urgency": 0,
      "birthDateYmd": "2024-06-06",
      "allowDuplicateConfirmation": false
  }
  ```
- **Response Body**:
  ```json
  {
      "status": "AQ_INVALID_REQUEST",
      "code": "400",
      "data": "Value cannot be null. (Parameter 'source')"
  }
  ```
- **Observed Trace**:
  ```text
  System.ArgumentNullException: Value cannot be null. (Parameter 'source')
     at System.Linq.ThrowHelper.ThrowArgumentNullException(ExceptionArgument argument)
     at System.Linq.Enumerable.TryGetFirst[TSource](IEnumerable`1 source, Boolean& found)
     at System.Linq.Enumerable.FirstOrDefault[TSource](IEnumerable`1 source)
     at Bilreg.Infrastructure.LabContext.LabOrderFeature.LabOrderDal.GetByEmrOrderId(String emrOrderId) in LabOrderDal.cs:line 176
  ```

## Notes

- Intake ini berasal langsung dari kegagalan verifikasi manusia pada pengujian `TC-LPAM-01`.
- Perilaku terjadi pada query DAL `LabOrderDal.GetByEmrOrderId` di mana hasil pembacaan data kosong dari helper DAL dieksekusi langsung dengan operator LINQ `.FirstOrDefault()`.
- Investigasi teknis terperinci dianalisis pada [LAB-ORDER-PA-MICRO-ACCOMMODATION-BUG-001-BUG-INVESTIGATION.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-BUG-001-BUG-INVESTIGATION.md).
- **Resolution**: Diperbaiki pada `LabOrderDal.cs` dengan beralih ke `conn.QueryFirstOrDefault<LabOrderDto>(sql, dp)` dan ditambahkan unit test pada `LabOrderDalTest.cs`. Diverifikasi ulang pada pengujian manusia `TC-LPAM-01` dengan hasil **PASS** pada 2026-11-04.
