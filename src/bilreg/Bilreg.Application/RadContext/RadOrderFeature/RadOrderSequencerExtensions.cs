using Bilreg.Domain.Shared.Helpers;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public static class RadOrderSequencerExtensions
{
    public static string GetNextNo(this ISequencer sequencer, string prefix, DateTime now)
    {
        var nextNo = sequencer.GetNextNoUrut(prefix);
        return $"{prefix}{now:yyMM}{nextNo:D6}";
    }
}
