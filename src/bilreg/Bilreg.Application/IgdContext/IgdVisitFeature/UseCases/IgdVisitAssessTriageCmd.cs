using Ardalis.GuardClauses;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.IgdVisitFeature.TriageEngine;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;

public record IgdVisitAssessTriageCmd(
    string IgdVisitId,
    int AirwaysScore,
    int BreathingScore,
    int BloodCirculationScore,
    int GcsEyeScore,
    int GcsMotorScore,
    int GcsVoiceScore,
    bool IsManualOverrideBlack,
    string OverrideReason,
    string Notes,
    string UserId)
    : IRequest<IgdVisitAssessTriageResponse>, IIgdVisitKey;

public record IgdVisitAssessTriageResponse(
    string IgdVisitId,
    int NoTriage,
    string TriageMethod,
    string TriageLevel,
    string TriageColor,
    DateTime LastTriageAt,
    DateTime NextReTriageAt,
    string SmassAssessmentId,
    string SmassGenerationStatus);

public class IgdVisitAssessTriageHandler : IRequestHandler<IgdVisitAssessTriageCmd, IgdVisitAssessTriageResponse>
{
    private readonly IIgdVisitRepo _igdVisitRepo;
    private readonly ITriageMethodEngineResolver _engineResolver;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IIgdVisitSmassTaskRepo _smassTaskRepo;
    private readonly ISmassAssessmentGateway _smassGateway;
    private readonly IgdVisitOptions _igdVisitOptions;

    public IgdVisitAssessTriageHandler(IIgdVisitRepo igdVisitRepo, ITriageMethodEngineResolver engineResolver,
        ITglJamProvider tglJamProvider, IIgdVisitSmassTaskRepo smassTaskRepo,
        ISmassAssessmentGateway smassGateway, IOptions<IgdVisitOptions> igdVisitOptions)
    {
        _igdVisitRepo = igdVisitRepo;
        _engineResolver = engineResolver;
        _tglJamProvider = tglJamProvider;
        _smassTaskRepo = smassTaskRepo;
        _smassGateway = smassGateway;
        _igdVisitOptions = igdVisitOptions.Value;
    }

    public async Task<IgdVisitAssessTriageResponse> Handle(IgdVisitAssessTriageCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.IgdVisitId, nameof(request.IgdVisitId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.OutOfRange(request.AirwaysScore, nameof(request.AirwaysScore), 0, 2);
        Guard.Against.OutOfRange(request.BreathingScore, nameof(request.BreathingScore), 0, 5);
        Guard.Against.OutOfRange(request.BloodCirculationScore, nameof(request.BloodCirculationScore), 0, 4);
        Guard.Against.OutOfRange(request.GcsEyeScore, nameof(request.GcsEyeScore), 1, 4);
        Guard.Against.OutOfRange(request.GcsMotorScore, nameof(request.GcsMotorScore), 1, 6);
        Guard.Against.OutOfRange(request.GcsVoiceScore, nameof(request.GcsVoiceScore), 1, 5);

        var visit = _igdVisitRepo.LoadEntity(request).GetValueOrThrow($"IgdVisit '{request.IgdVisitId}' not found");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        var assessment = new AtsAssessmentType(
            request.AirwaysScore,
            request.BreathingScore,
            request.BloodCirculationScore,
            request.GcsEyeScore,
            request.GcsMotorScore,
            request.GcsVoiceScore);

        var engine = _engineResolver.Resolve(TriageMethodEnum.Ats);
        var triageResult = engine.Calculate(assessment, audit.Timestamp);
        visit.AssessTriage(
            assessment: assessment,
            method: triageResult.Method,
            level: triageResult.Level,
            color: triageResult.Color,
            notes: request.Notes ?? "-",
            isManualOverrideBlack: request.IsManualOverrideBlack,
            overrideByUserId: request.UserId,
            overrideReason: request.OverrideReason ?? "-",
            nextReTriageAt: triageResult.NextReTriageAt,
            audit: audit);

        using (var trans = TransHelper.NewScope())
        {
            _igdVisitRepo.SaveChanges(visit);
            trans.Complete();
        }

        // §6.6 steps 6–9: post-commit SMASS hook (toggle → task upsert → gateway → task save).
        // The HTTP call happens after the TransHelper scope has been disposed and never
        // propagates an exception into this handler (BR-10).
        var smass = await IgdVisitSmassGenerationHook.RunAsync(
            _smassTaskRepo,
            _smassGateway,
            _igdVisitOptions,
            visit.IgdVisitId,
            visit.Triage,
            cancellationToken);

        return new IgdVisitAssessTriageResponse(
            visit.IgdVisitId,
            visit.Triage.NoTriage,
            visit.Triage.Method.ToCode(),
            visit.Triage.Level.ToCode(),
            visit.Triage.Color.ToCode(),
            visit.LastTriageAt,
            visit.NextReTriageAt,
            smass.AssessmentId,
            smass.Status);
    }
}
