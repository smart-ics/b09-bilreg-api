using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmUpdateInpatientVisitCmd(
    string RegId,
    string DokterId,
    string ProsedurMasukInapId,
    string CaraMasukDkId,
    string JenisInapId,
    string RegIbu,
    string UserId) : IRequest, IRegKey;

public record AdmUpdateInpatientVisitBody(
    string DokterId,
    string ProsedurMasukInapId,
    string CaraMasukDkId,
    string JenisInapId,
    string RegIbu,
    string UserId = "");

public class AdmUpdateInpatientVisitHandler : IRequestHandler<AdmUpdateInpatientVisitCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegInapRepo _regInapRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IProsedureMasukInapRepo _prosedurMasukInapRepo;
    private readonly ICaraMasukDkRepo _caraMasukRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmUpdateInpatientVisitHandler(
        IRegRepo regRepo,
        IRegInapRepo regInapRepo,
        IPpaRepo ppaRepo,
        IProsedureMasukInapRepo prosedurMasukInapRepo,
        ICaraMasukDkRepo caraMasukRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _regRepo = regRepo;
        _regInapRepo = regInapRepo;
        _ppaRepo = ppaRepo;
        _prosedurMasukInapRepo = prosedurMasukInapRepo;
        _caraMasukRepo = caraMasukRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmUpdateInpatientVisitCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.DokterId);
        Guard.Against.NullOrWhiteSpace(request.ProsedurMasukInapId);
        Guard.Against.NullOrWhiteSpace(request.CaraMasukDkId);
        Guard.Against.NullOrWhiteSpace(request.JenisInapId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        // 1. Validasi Perinatal dan Integritas Registrasi Ibu
        if (request.JenisInapId == InpatientAgePolicy.JENIS_INAP_PERINATAL)
        {
            if (string.IsNullOrWhiteSpace(request.RegIbu))
                throw new ArgumentException("Nomor registrasi ibu wajib diisi untuk jenis inap Perinatal.");

            var mother = _regRepo.LoadEntity(new RegKey(request.RegIbu))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"Registrasi ibu '{request.RegIbu}' tidak ditemukan.")
                );

            if (mother.RegVoidAudit.Timestamp.Date != new DateTime(3000, 1, 1))
                throw new InvalidOperationException("Registrasi ibu sudah berstatus void.");

            if (mother.RegKeluarAudit.Timestamp.Date != new DateTime(3000, 1, 1))
                throw new InvalidOperationException("Registrasi ibu sudah berstatus pulang/keluar.");

            if (mother.JenisInap?.JenisInapId != InpatientAgePolicy.JENIS_INAP_OBSTETRI)
                throw new InvalidOperationException("JENIS INAP IBU invalid: Registrasi ibu harus berjenis inap Obstetri.");
        }

        // 2. Load entitas pasien (reg & regInap)
        var reg = _regRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");

        var regInap = _regInapRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi rawat inap '{request.RegId}' tidak ditemukan.");

        // 3. Resolve master data pendukung
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId))
            .GetValueOrThrow($"Dokter '{request.DokterId}' tidak ditemukan.");

        var prosedur = _prosedurMasukInapRepo.LoadEntity(ProsedurMasukInapType.Key(request.ProsedurMasukInapId))
            .GetValueOrThrow($"Prosedur Masuk Inap '{request.ProsedurMasukInapId}' tidak ditemukan.");

        var caraMasuk = _caraMasukRepo.LoadEntity(CaraMasukDkType.Key(request.CaraMasukDkId))
            .GetValueOrThrow($"Cara Masuk '{request.CaraMasukDkId}' tidak ditemukan.");

        // 4. Snapshots & timestamp untuk audit
        var occurredAt = _tglJamProvider.Now;
        var auditInfo = new AuditInfoType(request.UserId, occurredAt);
        var regSnapshot = AuditLogSnapshotJson.Serialize(reg);
        var regInapSnapshot = AuditLogSnapshotJson.Serialize(regInap);

        // 5. Update RegInapModel (ta_reg_inap)
        if (regInap.Dpjp.PpaId != dokter.PpaId)
        {
            var effectiveDate = DateOnly.FromDateTime(occurredAt);
            var currentPrimary = regInap.ListDokter.FirstOrDefault(x =>
                x.IsActive && x.DokterRole == DokterRoleEnum.Dpjp && x.DpjpResponsibility == DpjpResponsibilityEnum.Primary);
            if (currentPrimary is not null && effectiveDate < currentPrimary.AssignDate)
            {
                effectiveDate = currentPrimary.AssignDate;
            }
            regInap.ChangePrimaryDpjp(dokter.ToReff(), effectiveDate);
        }
        regInap.ChangeProsedurMasuk(prosedur);

        // 6. Update RegModel (ta_registrasi)
        var targetRegIbu = request.JenisInapId == InpatientAgePolicy.JENIS_INAP_PERINATAL
            ? request.RegIbu
            : string.Empty;
        var jenisInap = JenisInapType.FromId(request.JenisInapId);

        if (reg.Dokter.PpaId != dokter.PpaId)
        {
            reg.ChangePrimaryDpjp(dokter.ToReff());
        }
        reg.SetCaraMasukDk(caraMasuk);
        reg.SetJenisInap(jenisInap, targetRegIbu);

        // 7. Simpan dalam transaksional scope
        using var trans = TransHelper.NewScope();

        _regInapRepo.SaveChanges(regInap);
        _regRepo.SaveChanges(reg);

        _auditRepo.SaveChanges(AuditLog.Create(
            auditInfo,
            "UPDATE",
            nameof(RegInapModel),
            regInap.RegId,
            originalDataJson: regInapSnapshot));

        _auditRepo.SaveChanges(AuditLog.Create(
            auditInfo,
            "UPDATE",
            nameof(RegModel),
            reg.RegId,
            originalDataJson: regSnapshot));

        trans.Complete();

        return Task.CompletedTask;
    }
}
