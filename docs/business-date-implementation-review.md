# Business Date implementation review

## Result

Business time is sourced from SQL Server once per scoped `TglJamProvider`. The provider keeps the SQL value as its system base, starts a `Stopwatch`, and returns the base plus elapsed time. In Fixed mode it replaces only the date portion of the base value. Fixed mode is allowed in every hosting environment; the startup warning and frontend banner communicate that simulation is active.

The read-only status endpoint is `GET /api/system/business-date`. There is no mutation endpoint.

## Files added

- `docs/business-date-implementation-review.md`
- `src/bilreg/Bilreg.Api/Configurations/BusinessDateStartup.cs`
- `src/bilreg/Bilreg.Api/Controllers/System/BusinessDateController.cs`
- `src/bilreg/Bilreg.Application/Shared/BusinessDateFeature/GetBusinessDateStatusQry.cs`
- `src/bilreg/Bilreg.Application/Shared/IBusinessDateStatus.cs`
- `src/bilreg/Bilreg.Infrastructure/Shared/Helpers/BusinessDateMode.cs`
- `src/bilreg/Bilreg.Infrastructure/Shared/Helpers/BusinessDateOptions.cs`
- `src/bilreg/Bilreg.Infrastructure/Shared/Helpers/BusinessDateOptionsValidator.cs`
- `src/bilreg/Bilreg.Infrastructure/Shared/Helpers/ISqlServerClock.cs`
- `src/bilreg/Bilreg.Infrastructure/Shared/Helpers/SqlServerClock.cs`
- `src/bilreg/Bilreg.Test/Shared/BusinessDateFeature/BusinessDateOptionsValidatorTest.cs`
- `src/bilreg/Bilreg.Test/Shared/BusinessDateFeature/GetBusinessDateStatusHandlerTest.cs`
- `src/bilreg/Bilreg.Test/Shared/BusinessDateFeature/TglJamProviderTest.cs`
- `src/bilreg/Bilreg.Test/Shared/TestTglJamProvider.cs`
- `src/bilreg/Bilreg.Test/GlobalUsings.cs`

## Files modified

There are 151 modified source/test files plus the documentation-index update. The exact scope is:

- API: `InfrastructureService.cs`, `Program.cs`, `appsettings.json`, `TataRekeningController.cs`.
- Application: all affected business-time handlers under `AdmisiContext`, `AdmisiRanapContext`, `BedUsageContext`, `ChargeContext`, `IgdContext`, `LabContext`, `PasienContext`, and `PaymentContext`; plus the Journey, patient-balance, registration-debt interfaces and services that now accept the captured business date.
- Domain: `AntrianEntryModel`, `AntrianFactory`, `AntrianModel`, `PasienTrackerModel`, `BookingModel`, `JadwalPraktekHarianType`, `RegModel`, `AdmissionModel`, `OpnameRequestModel`, `ReservationModel`, `WaitingListModel`, the operating-room models, RNA execution models, tariff/order/action models, `ClinicalOrderModel`, IGD action model, laboratory models, patient models, patient balance, billing models, `AuditLog`, and `UmurHelper`.
- Infrastructure: `JourneyDal`, `NilaiTarifDal`, `LegacyOutstandingReceivableReader`, `RegHutangDal`, `RegHutangRepo`, and `TglJamProvider`.
- Tests: Antrian, Journey API, Lab order/release/definition, and patient-balance tests updated to supply deterministic business timestamps.
- Documentation: `docs/ARTIFACTS.md` links this review.

Use `git diff --name-status` for the literal path-by-path manifest; no unrelated module redesign was made.

## Configuration

`appsettings.json` now contains:

```json
"BusinessDate": {
  "Mode": "System",
  "FixedDate": null
}
```

`BusinessDateOptions` is bound with `ValidateOnStart`. Validation rejects Fixed without FixedDate and System with FixedDate. It deliberately does not restrict Fixed mode by hosting environment. `ITglJamProvider`, its SQL clock, and the endpoint status facade are scoped.

## Migrated direct current-time access

All active Domain current-clock reads were removed. The migrated occurrences covered:

- admission, opname request, reservation, waiting-list creation/update/cancellation;
- outpatient/emergency registration, active-registration, queue entry/service completion, booking and patient tracker events;
- operating-room orders, schedules, assignments, start/discharge and state history;
- tariffs, treatment orders/actions and IGD treatment;
- lab order creation, collection, defer/activate, charge, release, cancel, terminate, result record/amend/verify, definition lifecycle, and Oware domain state transitions;
- patient creation/update, age/today rules, patient balance, billing and Tata Rekening transitions;
- audit/domain-event timestamps that belong to business operations;
- daily Journey age/projection reads, legacy receivable/debt date filters, and tariff date reads.

Each Application use case captures one timestamp and reuses it. Multiple provider reads found in the same physical file belong to separate handlers/methods, not the same use case.

## Direct current-time access intentionally retained

- `Bilreg.Api/Controllers/UserController.cs`: `DateTime.UtcNow` is JWT expiration (technical/security time).
- `Bilreg.Api/Configurations/DefaultExampleSchemaFilter.cs`: example generation only.
- `Bilreg.Application/ChargeContext/TarifFeature/UseCases/TrfImportNilaiTarifCmd.cs`: import duration and operational migration metadata.
- `Bilreg.Application/LabContext/LabOwareFeature/UseCases/LabOwareQueueEnqueueCmd.cs`: outbound queue scheduling/retry time.
- `Bilreg.Application/LabContext/LabOwareFeature/LabOwareQueueProcessor.cs`: queue lease/retry processing time.
- `Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs`: idempotency-ledger processing time; the cancellation itself uses captured Business Time.
- `Bilreg.Application/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolver.cs`: generated projection diagnostic/audit time.
- `Bilreg.Infrastructure/PaymentContext/TataRekeningFeature/BilrgMergeRequestDto.cs`: persistence `CrtDate`/`UpdDate` metadata.
- `Bilreg.Infrastructure/PaymentContext/PasienBalanceFeature/BilrgTataRekPasienBalanceDto.cs`: persistence metadata.
- `Bilreg.Infrastructure/ChargeContext/TarifFeature/TarifOperationalStateRepo.cs` and `TarifOperationalStateDal.cs`: migration operational-state metadata.
- `Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultPdfRenderer.cs`: document generation timestamp.
- Commented legacy examples in `PasienKtpDto.cs`, `TindakanFactory.cs`, and `AntrianMapHdrMigrasiCmd.cs` are non-executable.

All `new DateTime(...)` occurrences were reviewed. They are sentinel/default values (predominantly 3000-01-01), explicit range bounds, parsed cursor values, or caller-supplied/static dates; none obtains the current time.

## ITglJamProvider review

There are 92 Application consumer files: AdmisiContext 17, AdmisiRanapContext 12, BedUsageContext 8, ChargeContext 13, IgdContext 16, LabContext 15, PasienContext 3, and PaymentContext 8. Every consumer represents business time: operational dates, effective dates, patient age/today rules, state-transition occurrence time, or business audit/event time.

No Domain type references `ITglJamProvider`. Infrastructure implements it but does not consume it for business decisions. API only registers it; the endpoint depends on the Application `IBusinessDateStatus` facade. No existing consumer was found that requires unsimulated SQL Server time, so none needed to be split out. Technical-time consumers remain on the real clock as listed above.

## Domain signature changes

Explicit `DateTime`/`DateOnly` inputs were added to the creation and transition methods in these aggregates/services:

- Queue: `AntrianFactory.Create`, `AntrianModel.AddEntry`, `AntrianEntryModel.Create/Serve/Done`, `PasienTrackerModel.Create/AddEvent`.
- Admission: `AdmissionModel.Admit/CreateFromLegacyRegistration/Update/Cancel`, `OpnameRequestModel.Create/Cancel`, `ReservationModel.Create/Maintain/Cancel`, `WaitingListModel.Create/Update/Close`.
- Registration: Booking creation/cancellation, daily-practice creation/cancellation, and registration cancellation/update methods.
- Operating room: order/schedule creation and mutation, PPA assignment/removal, case transitions, start and discharge.
- Charge: tariff-policy lifecycle, treatment-order creation/cancellation, and treatment creation/update/void.
- IGD/RNA/CPOE: visit/action/service execution and correction transitions.
- Lab: order lifecycle, result record/amend/verify, test-definition lifecycle, and Oware queue domain transitions.
- Patient/payment: patient factories/mutations, balance replacement, Tata Rekening transitions, bill factories/domain service, audit log creation, and age calculation reference date.

Application call sites pass the captured timestamp explicitly. Provider injection is mandatory in every Application constructor; tests supply a deterministic provider.

## Verification

- `dotnet build src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore -m:1 --verbosity:minimal`: succeeded.
- `dotnet test ... --filter FullyQualifiedName~BusinessDateFeature`: 10 passed, 0 failed.
- Provider tests cover System, Fixed, running time, preserved time of day, and one SQL initialization per provider scope.
- Validation tests cover both invalid configuration combinations and confirm that a valid Fixed configuration is allowed.
- Endpoint response tests cover System and Fixed payloads.
- The integration-style admission test starts with SQL time `2026-07-20 10:15:30`, fixes the business date to `2025-05-03`, captures the Application timestamp once, and verifies admission plus its business audit are dated `2025-05-03 10:15:30`.
- Static audit result: Domain has zero active direct current-clock access and zero `ITglJamProvider` references.

## Remaining technical debt

- Some Domain timestamp parameters retain deterministic `= default` compatibility defaults. Removing those defaults is a follow-up hardening step after all external callers are confirmed migrated.
- Existing repository nullable warnings outside this feature remain; no new clock behavior depends on them.
