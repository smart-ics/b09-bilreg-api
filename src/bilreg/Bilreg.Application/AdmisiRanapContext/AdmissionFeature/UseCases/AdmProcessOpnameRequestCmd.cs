using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessOpnameRequestCmd(
    string OpnameRequestId,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest<AdmProcessAdmissionResponse>, IOpnameRequestKey;

public class AdmProcessOpnameRequestHandler : IRequestHandler<AdmProcessOpnameRequestCmd, AdmProcessAdmissionResponse>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IOpnameRequestRepo _opnameRequestRepo;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;

    public AdmProcessOpnameRequestHandler(
        IAdmissionRepo admissionRepo,
        IOpnameRequestRepo opnameRequestRepo,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo)
    {
        _admissionRepo = admissionRepo;
        _opnameRequestRepo = opnameRequestRepo;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
    }

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessOpnameRequestCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OpnameRequestId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var opname = _opnameRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Opname Request '{request.OpnameRequestId}' tidak ditemukan.");
        var pasien = opname.Pasien;

        var activeAdmissions = _admissionRepo
            .ListData(new AdmissionListFilter(PasienId: pasien.PasienId))
            .Where(a => a.AdmissionStatus is not AdmissionStatusEnum.Completed
                and not AdmissionStatusEnum.Cancelled)
            .ToList();

        if (activeAdmissions.Count > 0)
            throw new InvalidOperationException(
                $"Pasien '{pasien.PasienId}' masih memiliki admission aktif ({activeAdmissions[0].RegId}).");

        if (opname.OpnameRequestStatus != OpnameRequestStatusEnum.Requested)
            throw new InvalidOperationException(
                $"Opname Request '{opname.OpnameRequestId}' harus Requested untuk diproses (status: {opname.OpnameRequestStatus}).");

        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);

        var opnameSnapshot = AuditLogSnapshotJson.Serialize(opname);

        var admission = AdmissionModel.Admit(
            pasien,
            kelas,
            bangsal,
            opname.OpnameRequestId,
            null,
            request.UserId);

        var fulfilled = opname.Fulfill(admission.RegId, request.UserId);

        using var trans = TransHelper.NewScope();
        _admissionRepo.SaveChanges(admission);
        _opnameRequestRepo.SaveChanges(fulfilled);

        _auditRepo.SaveChanges(AuditLog.Create(
            admission.AuditTrail.Created,
            "CREATE",
            nameof(AdmissionModel),
            admission.RegId));

        _auditRepo.SaveChanges(AuditLog.Create(
            fulfilled.AuditTrail.Modified,
            "UPDATE",
            nameof(OpnameRequestModel),
            fulfilled.OpnameRequestId,
            originalDataJson: opnameSnapshot));

        trans.Complete();

        return Task.FromResult(new AdmProcessAdmissionResponse(
            admission.RegId,
            admission.AdmissionStatus));
    }
}
