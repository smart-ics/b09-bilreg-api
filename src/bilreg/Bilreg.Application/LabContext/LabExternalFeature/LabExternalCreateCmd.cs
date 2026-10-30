using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabExternalFeature;

public record LabExternalCreateCmd(
    string UserId,
    string PatientName,
    string? Nik,
    DateTime BirthDate,
    string Gender,
    string? Phone,
    string? Address,
    LabReferralTypeEnum ReferralType,
    string? ReferralDoctor,
    string? ReferralOrigin,
    string? ClinicalNotes) : IRequest<LabExternalCreateResponse>;

public record LabExternalCreateResponse(
    string RegExternalId,
    string ExternalMrNo,
    string BillingAccountId);

public class LabExternalCreateHandler : IRequestHandler<LabExternalCreateCmd, LabExternalCreateResponse>
{
    private readonly ILabExternalRepo _labExternalRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ISequencer _sequencer;
    private readonly ITglJamProvider _tglJamProvider;

    public LabExternalCreateHandler(
        ILabExternalRepo labExternalRepo,
        ITataRekeningRepo tataRekeningRepo,
        ISequencer sequencer,
        ITglJamProvider tglJamProvider)
    {
        _labExternalRepo = labExternalRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _sequencer = sequencer;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabExternalCreateResponse> Handle(
        LabExternalCreateCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.PatientName, nameof(request.PatientName));
        Guard.Against.NullOrWhiteSpace(request.Gender, nameof(request.Gender));
        Guard.Against.Default(request.BirthDate, nameof(request.BirthDate));

        var now = _tglJamProvider.Now;
        var regExternalId = LabExternalNumberHelper.GenerateRegExternalId();
        var externalMrNo = LabExternalNumberHelper.GenerateExternalMrNo(_sequencer, now);

        var demographics = new LabExternalDemographicsType(
            request.PatientName,
            request.Nik ?? string.Empty,
            request.BirthDate,
            request.Gender,
            request.Phone ?? string.Empty,
            request.Address ?? string.Empty);

        var referralInfo = new LabExternalReferralInfoType(
            request.ReferralType,
            request.ReferralDoctor ?? string.Empty,
            request.ReferralOrigin ?? string.Empty,
            request.ClinicalNotes ?? string.Empty);

        var audit = new AuditInfoType(request.UserId, now);

        using var trans = TransHelper.NewScope();

        var billingAccount = TataRekeningModel.Create(regExternalId);
        _tataRekeningRepo.SaveChanges(billingAccount);

        var labExternal = LabExternalModel.Create(
            regExternalId,
            externalMrNo,
            demographics,
            referralInfo,
            regExternalId,
            audit);
        _labExternalRepo.SaveChanges(labExternal);

        trans.Complete();

        var response = new LabExternalCreateResponse(regExternalId, externalMrNo, regExternalId);
        return Task.FromResult(response);
    }
}
