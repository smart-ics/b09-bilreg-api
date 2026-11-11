using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmUpdateInpatientVisitCmd(
    string RegId,
    string DokterId,
    string ProsedurMasukInapId,
    string JenisInapId,
    string RegIbu,
    string UserId,
    string? CaraMasukDkId = null) : IRequest, IRegKey;

public class AdmUpdateInpatientVisitHandler : IRequestHandler<AdmUpdateInpatientVisitCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegInapRepo _regInapRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IProsedureMasukInapRepo _prosedurMasukInapRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegAktifRepo? _regAktifRepo;
    private readonly ICaraMasukDkRepo? _caraMasukDkRepo;

    public AdmUpdateInpatientVisitHandler(
        IRegRepo regRepo,
        IRegInapRepo regInapRepo,
        IPpaRepo ppaRepo,
        IProsedureMasukInapRepo prosedurMasukInapRepo,
        ILayananRepo layananRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider,
        IRegAktifRepo? regAktifRepo = null,
        ICaraMasukDkRepo? caraMasukDkRepo = null)
    {
        _regRepo = regRepo;
        _regInapRepo = regInapRepo;
        _ppaRepo = ppaRepo;
        _prosedurMasukInapRepo = prosedurMasukInapRepo;
        _layananRepo = layananRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _regAktifRepo = regAktifRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
    }

    public Task Handle(AdmUpdateInpatientVisitCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.DokterId);
        Guard.Against.NullOrWhiteSpace(request.ProsedurMasukInapId);
        Guard.Against.NullOrWhiteSpace(request.JenisInapId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        if (request.JenisInapId is not (InpatientAgePolicy.JENIS_INAP_UMUM
            or InpatientAgePolicy.JENIS_INAP_OBSTETRI
            or InpatientAgePolicy.JENIS_INAP_PERINATAL))
        {
            throw new ArgumentException($"Jenis Inap '{request.JenisInapId}' tidak valid.", nameof(request.JenisInapId));
        }

        if (request.JenisInapId == InpatientAgePolicy.JENIS_INAP_PERINATAL && string.IsNullOrWhiteSpace(request.RegIbu))
        {
            throw new ArgumentException("No Reg Ibu wajib diisi untuk jenis inap Perinatal.", nameof(request.RegIbu));
        }

        var reg = _regRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");

        if (!reg.IsAktif)
        {
            throw new InvalidOperationException($"Registrasi '{request.RegId}' sudah tidak aktif.");
        }

        var regInap = _regInapRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi Inap '{request.RegId}' tidak ditemukan.");

        if (regInap.IsTerminal)
        {
            throw new InvalidOperationException($"Registrasi Inap '{request.RegId}' sudah berstatus terminal.");
        }

        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId))
            .GetValueOrThrow($"Dokter '{request.DokterId}' tidak ditemukan.");

        var prosedur = _prosedurMasukInapRepo.LoadEntity(ProsedurMasukInapType.Key(request.ProsedurMasukInapId))
            .GetValueOrThrow($"Prosedur Masuk Inap '{request.ProsedurMasukInapId}' tidak ditemukan.");

        var layanan = _layananRepo.LoadEntity(LayananType.Key(reg.Layanan.LayananId))
            .GetValueOrThrow($"Layanan '{reg.Layanan.LayananId}' tidak ditemukan.");

        var jenisInap = JenisInapType.FromId(request.JenisInapId);
        var regIbu = request.JenisInapId == InpatientAgePolicy.JENIS_INAP_PERINATAL
            ? request.RegIbu.Trim()
            : string.Empty;

        var snapshotJson = AuditLogSnapshotJson.Serialize(regInap);
        var occurredAt = _tglJamProvider.Now;
        var today = DateOnly.FromDateTime(occurredAt);

        if (regInap.Dpjp.PpaId != dokter.PpaId)
        {
            regInap.ChangePrimaryDpjp(dokter.ToReff(), today);
        }

        regInap.SetProsedurMasukInap(prosedur);
        reg.AssignInpatientVisitTo(dokter, layanan);
        reg.SetJenisInap(jenisInap, regIbu);

        if (!string.IsNullOrWhiteSpace(request.CaraMasukDkId) && _caraMasukDkRepo != null)
        {
            var caraMasuk = _caraMasukDkRepo.LoadEntity(CaraMasukDkType.Key(request.CaraMasukDkId))
                .GetValueOrThrow($"Cara Masuk '{request.CaraMasukDkId}' tidak ditemukan.");

            if (!caraMasuk.RequiresRujukan)
            {
                reg.SpecifyCaraMasuk(caraMasuk, RujukanType.Default);
            }
            else
            {
                var rujukan = RujukanType.Default with
                {
                    RujukanId = reg.Rujukan?.RujukanId ?? "-",
                    RujukanName = reg.Rujukan?.RujukanName ?? "-",
                    CaraMasukDk = caraMasuk
                };
                reg.SpecifyCaraMasuk(caraMasuk, rujukan);
            }
        }

        using (var trans = TransHelper.NewScope())
        {
            _regRepo.SaveChanges(reg);
            _regInapRepo.SaveChanges(regInap);
            _regAktifRepo?.SaveChanges(RegAktifModel.CreateFromReg(reg));

            _auditRepo.SaveChanges(AuditLog.Create(
                request.UserId,
                occurredAt,
                "UPDATE",
                nameof(RegInapModel),
                request.RegId,
                originalDataJson: snapshotJson));

            trans.Complete();
        }

        return Task.CompletedTask;
    }
}
