using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.DigitalSignFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;

public record AdmRecordDigitalSignCmd(
    string RegId,
    string HisReference,
    string DokumenId,
    string SigningRequestId,
    string SignerId,
    string FileName,
    string UserId) : IRequest<AdmRecordDigitalSignResponse>, IRegKey;

public record AdmRecordDigitalSignResponse(string SigningRequestId);

public class AdmRecordDigitalSignHandler : IRequestHandler<AdmRecordDigitalSignCmd, AdmRecordDigitalSignResponse>
{
    private readonly IRanapDigitalSignRepo _digitalSignRepo;
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmRecordDigitalSignHandler(
        IRanapDigitalSignRepo digitalSignRepo,
        IAdmissionRepo admissionRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _digitalSignRepo = digitalSignRepo;
        _admissionRepo = admissionRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<AdmRecordDigitalSignResponse> Handle(
        AdmRecordDigitalSignCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.DokumenId);
        Guard.Against.NullOrWhiteSpace(request.SigningRequestId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var regId = request.RegId.Trim();
        var dokumenId = request.DokumenId.Trim();
        var signingRequestId = request.SigningRequestId.Trim();
        var hisReference = (request.HisReference ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(hisReference) || hisReference == "-")
            throw new ArgumentException("hisReference wajib diisi.", nameof(request.HisReference));

        var admission = _admissionRepo.LoadEntity(AdmissionModel.Key(regId))
            .GetValueOrThrow($"Admission '{regId}' tidak ditemukan.");

        var existingById = _digitalSignRepo.LoadEntity(RanapDigitalSignModel.Key(signingRequestId));
        if (existingById.HasValue)
        {
            var existing = existingById.Value;
            if (!string.Equals(existing.RegId, regId, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(existing.DokumenId, dokumenId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"SigningRequestId '{signingRequestId}' sudah tercatat untuk RegId '{existing.RegId}' DokumenId '{existing.DokumenId}'.");
            return Task.FromResult(new AdmRecordDigitalSignResponse(existing.SigningRequestId));
        }

        var existingByDoc = _digitalSignRepo.LoadByRegDokumen(regId, dokumenId);
        if (existingByDoc.HasValue)
        {
            var existing = existingByDoc.Value;
            if (!string.Equals(existing.SigningRequestId, signingRequestId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"RegId '{regId}' DokumenId '{dokumenId}' sudah tercatat dengan SigningRequestId berbeda.");
            return Task.FromResult(new AdmRecordDigitalSignResponse(existing.SigningRequestId));
        }

        var pasien = admission.Pasien ?? new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-");
        var occurredAt = _tglJamProvider.Now;
        var model = RanapDigitalSignModel.CatatCreated(
            regId,
            hisReference,
            dokumenId,
            signingRequestId,
            pasien,
            request.SignerId ?? string.Empty,
            request.FileName ?? string.Empty,
            request.UserId.Trim(),
            occurredAt);

        _digitalSignRepo.SaveChanges(model);

        _auditRepo.SaveChanges(AuditLog.Create(
            model.AuditTrail.Created,
            "CREATE",
            nameof(RanapDigitalSignModel),
            model.SigningRequestId));

        return Task.FromResult(new AdmRecordDigitalSignResponse(model.SigningRequestId));
    }
}
