using Bilreg.Domain.IgdContext.IgdVisitFeature;

namespace Bilreg.Application.IgdContext.IgdVisitFeature.TriageEngine;

public record TriageEngineResult(
    TriageMethodEnum Method,
    TriageLevelEnum Level,
    TriageColorEnum Color,
    DateTime LastTriageAt,
    DateTime? NextReTriageAt);

public interface ITriageMethodEngine
{
    TriageMethodEnum Method { get; }
    TriageEngineResult Calculate(AtsAssessmentType assessment, DateTime assessedAt);
}

public interface ITriageMethodEngineResolver
{
    ITriageMethodEngine Resolve(TriageMethodEnum method);
}

public class TriageMethodEngineResolver : ITriageMethodEngineResolver
{
    private readonly IReadOnlyDictionary<TriageMethodEnum, ITriageMethodEngine> _engines;

    public TriageMethodEngineResolver(IEnumerable<ITriageMethodEngine> engines)
    {
        _engines = engines.ToDictionary(x => x.Method, x => x);
    }

    public ITriageMethodEngine Resolve(TriageMethodEnum method)
    {
        if (!_engines.TryGetValue(method, out var engine))
            throw new ArgumentException($"Triage method '{method.ToCode()}' is not registered.");
        return engine;
    }
}

public class AtsTriageEngine : ITriageMethodEngine
{
    public TriageMethodEnum Method => TriageMethodEnum.Ats;

    public TriageEngineResult Calculate(AtsAssessmentType assessment, DateTime assessedAt)
    {
        var gcsTotal = assessment.GcsEyeScore + assessment.GcsMotorScore + assessment.GcsVoiceScore;
        var maxScore = new[]
        {
            assessment.AirwaysScore,
            assessment.BreathingScore,
            assessment.BloodCirculationScore,
            DetermineGcsSeverity(gcsTotal)
        }.Max();

        var level = maxScore switch
        {
            5 => TriageLevelEnum.Ats1,
            4 => TriageLevelEnum.Ats2,
            3 => TriageLevelEnum.Ats3,
            2 => TriageLevelEnum.Ats4,
            _ => TriageLevelEnum.Ats5
        };

        var color = level switch
        {
            TriageLevelEnum.Ats1 or TriageLevelEnum.Ats2 => TriageColorEnum.Red,
            TriageLevelEnum.Ats3 => TriageColorEnum.Yellow,
            TriageLevelEnum.Ats4 or TriageLevelEnum.Ats5 => TriageColorEnum.Green,
            _ => TriageColorEnum.Unknown,
        };

        var interval = level.ToReAssessmentInterval();
        return new TriageEngineResult(
            Method: Method,
            Level: level,
            Color: color,
            LastTriageAt: assessedAt,
            NextReTriageAt: interval.HasValue ? assessedAt.Add(interval.Value) : null);
    }

    private static int DetermineGcsSeverity(int gcs)
    {
        return gcs switch
        {
            <= 8 => 5,
            <= 12 => 3,
            < 15 => 1,
            _ => 0
        };
    }
}
