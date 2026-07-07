using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using MediatR;
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
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public AdmProcessOpnameRequestHandler(
        IAdmissionRepo admissionRepo,
        IOpnameRequestRepo opnameRequestRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo)
    {
        _admissionRepo = admissionRepo;
        _opnameRequestRepo = opnameRequestRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task<AdmProcessAdmissionResponse> Handle(
        AdmProcessOpnameRequestCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OpnameRequestId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var opname = AdmisiRanapSupport.LoadOpnameRequest(_opnameRequestRepo, request);
        var pasien = opname.Pasien;

        AdmisiRanapSupport.EnsureNoActiveAdmission(_admissionRepo, pasien.PasienId);

        if (opname.OpnameRequestStatus != OpnameRequestStatusEnum.Requested)
            throw new InvalidOperationException(
                $"Opname Request '{opname.OpnameRequestId}' harus Requested untuk diproses (status: {opname.OpnameRequestStatus}).");

        var kelas = AdmisiRanapSupport.LoadKelasReff(_kelasRepo, request.KelasId);
        var bangsal = AdmisiRanapSupport.LoadBangsalReff(_bangsalRepo, request.BangsalId);

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
        trans.Complete();

        return Task.FromResult(new AdmProcessAdmissionResponse(
            admission.RegId,
            admission.AdmissionStatus));
    }
}
