using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabExternalFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabExternalFeature;

public record LabExternalGetQuery(string RegExternalId) : IRequest<LabExternalDetailResponse>, ILabExternalKey;

public record LabExternalDetailResponse(
    string RegExternalId,
    string ExternalMrNo,
    string PatientName,
    string Nik,
    DateTime BirthDate,
    int Age,
    string Gender,
    string Phone,
    string Address,
    int ReferralType,
    string ReferralDoctor,
    string ReferralOrigin,
    string ClinicalNotes,
    string BillingAccountId,
    int Status,
    string CancelReason,
    string CancelledUserId,
    DateTime CancelledDate,
    DateTime CreatedDate,
    string CreatedUserId);

public class LabExternalGetHandler : IRequestHandler<LabExternalGetQuery, LabExternalDetailResponse>
{
    private readonly ILabExternalRepo _labExternalRepo;

    public LabExternalGetHandler(ILabExternalRepo labExternalRepo)
    {
        _labExternalRepo = labExternalRepo;
    }

    public Task<LabExternalDetailResponse> Handle(LabExternalGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.RegExternalId, nameof(request.RegExternalId));

        var model = _labExternalRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"LabExternal with Id '{request.RegExternalId}' not found."));

        var response = new LabExternalDetailResponse(
            model.RegExternalId,
            model.ExternalMrNo,
            model.Demographics.PatientName,
            model.Demographics.Nik,
            model.Demographics.BirthDate,
            model.Demographics.CalculateAge(),
            model.Demographics.Gender,
            model.Demographics.Phone,
            model.Demographics.Address,
            (int)model.ReferralInfo.ReferralType,
            model.ReferralInfo.ReferralDoctor,
            model.ReferralInfo.ReferralOrigin,
            model.ReferralInfo.ClinicalNotes,
            model.BillingAccountId,
            (int)model.Status,
            model.CancelInfo.CancelReason,
            model.CancelInfo.CancelledUserId,
            model.CancelInfo.CancelledDate,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Created.UserId);

        return Task.FromResult(response);
    }
}
