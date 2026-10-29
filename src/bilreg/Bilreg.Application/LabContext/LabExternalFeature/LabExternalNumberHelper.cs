using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.Shared.Helpers;
using Nuna.Lib.AutoNumberHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabExternalFeature;

public static class LabExternalNumberHelper
{
    public static string GenerateExternalMrNo(ISequencer sequencer, DateTime timestamp)
    {
        Guard.Against.Null(sequencer, nameof(sequencer));
        var periodKey = $"EXT-{timestamp:yyyyMM}";
        var nextNo = sequencer.GetNextNoUrut(periodKey);
        return $"{periodKey}-{nextNo:D5}";
    }

    public static string GenerateRegExternalId()
    {
        return NunaId.NewLegacyCompact(LabExternalModel.IdPrefix);
    }
}
