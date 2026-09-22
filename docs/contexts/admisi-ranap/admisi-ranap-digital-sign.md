# Admisi Ranap Digital Sign Tracking

**Status:** Implemented (S1) — September 2026
**Context:** `docs/contexts/admisi-ranap/admisi-ranap-domain.md` (`Admission` = satu episode inap administratif, `BR-RI-004`)

Dokumen ini mencakup **pencatatan korelasi** `signingRequestId` PenaEl per dokumen rawat inap (tracking saja).
Identity sync (`mr` → `SignerId`) tetap milik `docs/contexts/pasien-digital-sign/pasien-digital-sign-resolve.md`
dan tidak berubah. Bilreg **tidak** menyimpan PDF (`BR-PDS-012`), **tidak** memanggil PenaEl,
dan **tidak** memirror status lifecycle PenaEl (`Created → Approved/Rejected → Signed` tetap milik PenaEl).

---

## 1. Position in the flow

```text
MyHospital Web                    Bilreg (b09)                        PenaEl
     │  resolve(mr)                     │                                 │
     │ ── GET /api/digital-sign/ ──────▶│  (PasienContext, unchanged)     │
     │      patient-signers/resolve     │                                 │
     │                                  │                                 │
     │  allocate → upload → create ──────────────────────────────────────▶│
     │  (langsung ke Middleware/Cloud,  │                                 │
     │   patientSignerId, hisReference, │                                 │
     │   document.externalDocumentId)   │                                 │
     │                                  │                                 │
     │  record mapping                  │                                 │
     │ ── POST /api/admisi-ranap/ ─────▶│  BILRG_AdmDigitalSign           │
     │      digital-sign                │                                 │
     │                                  │                                 │
     │  lookup signingRequestId         │                                 │
     │ ── GET /api/admisi-ranap/ ──────▶│                                 │
     │      digital-sign(?list)         │                                 │
     │                                  │                                 │
     │  download signed.pdf ─────────────────────────────────────────────▶│
     │  GET {Middleware}/api/signing-requests/{signingRequestId}/signed   │
```

Tanpa tabel ini, `signingRequestId` hanya hidup di memori frontend dan hilang
saat halaman tertutup — Web tidak bisa memanggil ulang `GET .../signed`.

---

## 2. Key decisions (S1)

| # | Decision |
|---|---|
| 1 | `dokumenId` = `document.externalDocumentId` (`HIS-DOC-xxx`) pada body Create PenaEl |
| 2 | `hisReference` = kunci korelasi HIS yang dikirim Web secara eksplisit; independen dan boleh berbeda dengan `RegId` |
| 3 | Kardinalitas 1 `RegId` → N `dokumenId` (`UNIQUE(RegId, DokumenId)`) |
| 4 | Record idempotent: kirim ulang body sama → sukses tanpa duplikat; `(RegId, DokumenId)` dengan `SigningRequestId` berbeda → `409` konflik |
| 5 | `GET` hanya 2 varian: tunggal (`regId` + `dokumenId` → 1 row) dan list (`/list?regId=` → array N dokumen) |

---

## 3. Contract summary

| Item | Value |
|---|---|
| BFF record | `POST /api/admisi-ranap/digital-sign` (JWT-authorized, `AdmisiRanapEnabledFilter`) |
| BFF get single | `GET /api/admisi-ranap/digital-sign?regId=&dokumenId=` — `200` row, `400` param kosong, `404` tidak ditemukan (`JSend` 404 seperti `JourneyController`) |
| BFF get list | `GET /api/admisi-ranap/digital-sign/list?regId=` — `200` array (kosong bila tidak ada), `400` bila `regId` kosong |
| Record failures | `404` Admission tidak ada, `422` validasi, `409` konflik |
| Auth | JWT di depan (sama seperti controller AdmisiRanap lain); tidak ada PenaEl key di S1 |

### 3.1 POST body

```json
{
  "regId": "RG00000001",
  "hisReference": "HIS-ENC-999",
  "dokumenId": "HIS-DOC-2026-001",
  "signingRequestId": "0194a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b",
  "signerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "fileName": "informed-consent.pdf",
  "userId": "admisi01"
}
```

`hisReference` wajib diisi (boleh berbeda dengan `regId`).
`signingRequestId` wajib UUID (nilai dari response Middleware allocate).

### 3.2 GET single response (`200`)

```json
{
  "status": "success",
  "data": {
    "signingRequestId": "0194a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b",
    "regId": "RG00000001",
    "hisReference": "HIS-ENC-999",
    "dokumenId": "HIS-DOC-2026-001",
    "fileName": "informed-consent.pdf"
  }
}
```

Tidak ditemukan → `404` `JSend(404, "Not Found", "DigitalSign regId '...' dokumenId '...' was not found.")`
(pola `Bilreg.Api/Controllers/AdmisiRanapContext/JourneyController.cs:77-79`).

---

## 4. Files

| Concern | Path |
|---|---|
| Aggregate + key | `Bilreg.Domain/AdmisiRanapContext/DigitalSignFeature/RanapDigitalSignModel.cs`, `IRanapDigitalSignKey.cs` |
| Repo port | `Bilreg.Application/AdmisiRanapContext/DigitalSignFeature/IRanapDigitalSignRepo.cs` |
| Record use case | `Bilreg.Application/AdmisiRanapContext/DigitalSignFeature/UseCases/AdmRecordDigitalSignCmd.cs` |
| Lookup use case | `Bilreg.Application/AdmisiRanapContext/DigitalSignFeature/UseCases/AdmGetDigitalSignQry.cs` |
| DTO + DAL + Repo | `Bilreg.Infrastructure/AdmisiRanapContext/DigitalSignFeature/RanapDigitalSignDto.cs`, `RanapDigitalSignDal.cs`, `RanapDigitalSignRepo.cs` |
| Controller actions | `Bilreg.Api/Controllers/PasienContext/DigitalSignFeature/DigitalSignController.cs` (`RecordRanapDigitalSign`, `GetRanapDigitalSign`, `ListRanapDigitalSign`, route `~/api/admisi-ranap/digital-sign`) |
| Table script | `Bilreg.SqlDb/AdmisiRanapContext/DigitalSignFeature/BILRG_AdmDigitalSign.sql` |
| Tests | `Bilreg.Test/AdmisiRanapContext/DigitalSignFeature/` (model, record handler, get handler), `Bilreg.Test/PasienContext/DigitalSignFeature/RanapDigitalSignActionTest.cs` |

Catatan penempatan: action menumpang `DigitalSignController` (yang sudah tampil di Scalar)
dengan rute konteks sendiri via prefix `~/`, masing-masing memakai
`AdmisiRanapEnabledFilter` per-action. Tidak ada perubahan pada resolve flow.

---

## 5. Persistence

Tabel `BILRG_AdmDigitalSign` (pola `BILRG_AdmAdmission.sql` + `docs/DATABASE.md`):

* PK `SigningRequestId VARCHAR(36)` (UUID dari Middleware, bukan `NunaId`)
* `RegId VARCHAR(10)` logical ref ke `BILRG_AdmAdmission.RegId` (tanpa FK DB)
* `HisReference VARCHAR(50)`, `DokumenId VARCHAR(50)`, `PasienId`, `SignerId`, `FileName`
* `UNIQUE(RegId, DokumenId)`, index `(RegId)`
* Audit wajib `CrtUser/CrtDate/UpdUser/UpdDate/VodUser/VodDate`; tanpa NULL; tanpa FK
* Script idempotent (`IF OBJECT_ID` / `IF COL_LENGTH` / `IF NOT EXISTS`) dan memuat
  guard `DROP COLUMN` untuk kolom yang dihapus dari desain awal
  (`DigitalSignStatus`, `DocumentType`, `Title`, `DocumentHash`)

DI otomatis via Scrutor Nuna markers (`IInsert`/`IUpdate`/`IGetData`/`ISaveChange`/`ILoadEntity`);
tidak ada registrasi manual di `InfrastructureService.cs`.

---

## 6. Non-goals (S2/S3, ditunda)

* Proxy `GET .../{signingRequestId}/signed` (S2)
* Webhook `SigningRequestApproved/Signed` untuk update status (S3)
* Backfill data lama; perubahan ke `BILRG_AdmAdmission`, Waiting List, `tc_mr`, resolve endpoint

## 7. References

* `docs/contexts/pasien-digital-sign/pasien-digital-sign-resolve.md` — identity sync (tidak berubah)
* `docs/contexts/admisi-ranap/admisi-ranap-domain.md` — `Admission`, `BR-RI-004`
* `docs/contexts/admisi-ranap/admisi-ranap-architecture.md` — 1 aggregate per boundary, CQS, repo per aggregate
* PenaEl `b19-penael/docs/02-features/signing-request/SEQUENCE.md` — allocate → upload → create, `GET .../signed`
* HiDok `digital-sign-contract-hospital-publish.md` — `patientSignerId`, `hisReference`, `document.externalDocumentId`
