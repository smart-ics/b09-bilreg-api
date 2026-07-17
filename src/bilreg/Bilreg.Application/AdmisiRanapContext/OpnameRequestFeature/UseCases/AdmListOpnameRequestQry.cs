using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.OpnameRequestFeature.UseCases;

public record AdmListOpnameRequestQry(OpnameRequestStatusEnum? Status = null)
    : IRequest<AdmListOpnameRequestResponse>;

public record AdmListOpnameRequestResponse(IReadOnlyList<AdmOpnameRequestListItem> Items);

public record AdmOpnameRequestListItem(
    string OpnameRequestId,
    OpnameRequestStatusEnum OpnameRequestStatus,
    string PasienId,
    string PasienName,
    string DokterId,
    string DokterName,
    DateTime CrtDate);

public class AdmListOpnameRequestHandler : IRequestHandler<AdmListOpnameRequestQry, AdmListOpnameRequestResponse>
{
    private readonly IOpnameRequestRepo _opnameRequestRepo;

    public AdmListOpnameRequestHandler(IOpnameRequestRepo opnameRequestRepo) =>
        _opnameRequestRepo = opnameRequestRepo;

    public Task<AdmListOpnameRequestResponse> Handle(
        AdmListOpnameRequestQry request,
        CancellationToken cancellationToken)
    {
        var items = _opnameRequestRepo
            .ListData(new OpnameRequestListFilter(request.Status))
            .Select(Map)
            .ToList();

        return Task.FromResult(new AdmListOpnameRequestResponse(items));
    }

    private static AdmOpnameRequestListItem Map(OpnameRequestModel opname) =>
        new(
            opname.OpnameRequestId,
            opname.OpnameRequestStatus,
            opname.Pasien.PasienId,
            opname.Pasien.PasienName,
            opname.Dokter.PpaId,
            opname.Dokter.PpaName,
            opname.AuditTrail.Created.Timestamp);
}
