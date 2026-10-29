using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabExternalFeature;
using MediatR;

namespace Bilreg.Application.LabContext.LabExternalFeature;

public record LabExternalListQuery(
    string? SearchTerm = null,
    DateTime? Date1 = null,
    DateTime? Date2 = null,
    int? Status = null) : IRequest<List<LabExternalListItemResponse>>;

public record LabExternalListItemResponse(
    string RegExternalId,
    string ExternalMrNo,
    string PatientName,
    string Nik,
    DateTime BirthDate,
    int Age,
    string Gender,
    string Phone,
    int ReferralType,
    string ReferralOrigin,
    string BillingAccountId,
    int Status,
    DateTime CreatedDate);

public class LabExternalListHandler : IRequestHandler<LabExternalListQuery, List<LabExternalListItemResponse>>
{
    private readonly ILabExternalRepo _labExternalRepo;

    public LabExternalListHandler(ILabExternalRepo labExternalRepo)
    {
        _labExternalRepo = labExternalRepo;
    }

    public Task<List<LabExternalListItemResponse>> Handle(LabExternalListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        var entities = _labExternalRepo.ListData(request.SearchTerm, request.Date1, request.Date2, request.Status) ?? [];

        var response = entities.Select(x => new LabExternalListItemResponse(
            x.RegExternalId,
            x.ExternalMrNo,
            x.Demographics.PatientName,
            x.Demographics.Nik,
            x.Demographics.BirthDate,
            x.Demographics.CalculateAge(),
            x.Demographics.Gender,
            x.Demographics.Phone,
            (int)x.ReferralInfo.ReferralType,
            x.ReferralInfo.ReferralOrigin,
            x.BillingAccountId,
            (int)x.Status,
            x.AuditTrail.Created.Timestamp)).ToList();

        return Task.FromResult(response);
    }
}
