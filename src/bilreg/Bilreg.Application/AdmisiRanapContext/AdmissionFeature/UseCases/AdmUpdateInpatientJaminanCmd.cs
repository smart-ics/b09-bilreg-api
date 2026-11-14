using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmUpdateInpatientJaminanCmd(
    string RegId,
    string TipeJaminanId,
    string PolisId,
    string? SjpNo,
    string? PesertaJaminanId,
    string? SjpId,
    string UserId) : IRequest, IRegKey, ITipeJaminanKey;

public class AdmUpdateInpatientJaminanHandler : IRequestHandler<AdmUpdateInpatientJaminanCmd>
{
    private const string BAYAR_SENDIRI = "00000";

    private readonly IRegRepo _regRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegAktifRepo? _regAktifRepo;

    public AdmUpdateInpatientJaminanHandler(
        IRegRepo regRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider,
        IRegAktifRepo? regAktifRepo = null)
    {
        _regRepo = regRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
        _regAktifRepo = regAktifRepo;
    }

    public Task Handle(AdmUpdateInpatientJaminanCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.TipeJaminanId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var reg = _regRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");

        if (!reg.IsAktif)
        {
            throw new InvalidOperationException($"Registrasi '{request.RegId}' sudah tidak aktif.");
        }

        var tipeJmn = _tipeJaminanRepo.LoadEntity(TipeJaminanType.Key(request.TipeJaminanId))
            .GetValueOrThrow($"Tipe Jaminan '{request.TipeJaminanId}' tidak ditemukan.");

        PolisModel polis;
        if (request.TipeJaminanId == BAYAR_SENDIRI)
        {
            polis = PolisModel.Default;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.PolisId))
            {
                throw new ArgumentException("PolisId wajib diisi jika tipe jaminan bukan umum/bayar sendiri.", nameof(request.PolisId));
            }

            var patientPolicies = _polisRepo.ListData(PasienModel.Key(reg.Pasien.PasienId));
            if (patientPolicies.All(x => x.PolisId != request.PolisId))
            {
                throw new ArgumentException($"Pasien '{reg.Pasien.PasienName}' tidak memiliki polis '{request.PolisId}'.");
            }

            polis = _polisRepo.LoadEntity(PolisModel.Key(request.PolisId))
                .GetValueOrThrow($"Polis '{request.PolisId}' tidak ditemukan.");
        }

        var snapshotJson = AuditLogSnapshotJson.Serialize(reg);
        var occurredAt = _tglJamProvider.Now;

        reg.ApplyJaminan(tipeJmn, polis);

        var hasEligibility = !string.IsNullOrWhiteSpace(request.SjpNo)
            || !string.IsNullOrWhiteSpace(request.PesertaJaminanId)
            || !string.IsNullOrWhiteSpace(request.SjpId);

        if (hasEligibility)
        {
            reg.SetEligibility(
                request.SjpNo ?? string.Empty,
                request.PesertaJaminanId ?? string.Empty,
                request.SjpId ?? string.Empty);
        }

        using (var trans = TransHelper.NewScope())
        {
            _regRepo.SaveChanges(reg);
            _regAktifRepo?.SaveChanges(RegAktifModel.CreateFromReg(reg));

            _auditRepo.SaveChanges(AuditLog.Create(
                request.UserId,
                occurredAt,
                "UPDATE",
                nameof(RegModel),
                request.RegId,
                originalDataJson: snapshotJson));

            trans.Complete();
        }

        return Task.CompletedTask;
    }
}
