using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmCreateOpnameRequestCmd(
    string PasienId,
    string DokterId,
    string ClinicalNotes,
    string UserId) : IRequest<AdmCreateOpnameRequestResponse>;

public record AdmCreateOpnameRequestResponse(string OpnameRequestId);

public class AdmCreateOpnameRequestHandler : IRequestHandler<AdmCreateOpnameRequestCmd, AdmCreateOpnameRequestResponse>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IPpaRepo _ppaRepo;

    public AdmCreateOpnameRequestHandler(
        IOpnameRequestRepo opnameRequestRepo,
        IPasienRepo pasienRepo,
        IPpaRepo ppaRepo)
    {
        _opnameRequestRepo = opnameRequestRepo;
        _pasienRepo = pasienRepo;
        _ppaRepo = ppaRepo;
    }

    public Task<AdmCreateOpnameRequestResponse> Handle(
        AdmCreateOpnameRequestCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.DokterId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var pasien = AdmisiRanapSupport.LoadPasienReff(_pasienRepo, request.PasienId);
        var dokter = AdmisiRanapSupport.LoadPpaReff(_ppaRepo, request.DokterId);

        var opnameRequest = OpnameRequestModel.Create(
            pasien,
            dokter,
            request.ClinicalNotes ?? "",
            request.UserId);

        _opnameRequestRepo.SaveChanges(opnameRequest);

        return Task.FromResult(new AdmCreateOpnameRequestResponse(opnameRequest.OpnameRequestId));
    }
}
