using Ardalis.GuardClauses;
using Bilreg.Domain.Shared.Helpers;

namespace Bilreg.Application.LabContext.LabResultFeature;

public interface ILabPaRegisterSequencer
{
    string Generate(string disciplineCode, DateTime? timestamp = null);
    string NextNumber(string disciplineCode, DateTime? timestamp = null);
}

public class LabPaRegisterSequencer : ILabPaRegisterSequencer
{
    private readonly ISequencer _sequencer;

    public LabPaRegisterSequencer(ISequencer sequencer)
    {
        _sequencer = sequencer ?? throw new ArgumentNullException(nameof(sequencer));
    }

    public string Generate(string disciplineCode, DateTime? timestamp = null)
        => NextNumber(_sequencer, disciplineCode, timestamp);

    public string NextNumber(string disciplineCode, DateTime? timestamp = null)
        => NextNumber(_sequencer, disciplineCode, timestamp);

    public static string Generate(ISequencer sequencer, string disciplineCode, DateTime? timestamp = null)
        => NextNumber(sequencer, disciplineCode, timestamp);

    public static string NextNumber(ISequencer sequencer, string disciplineCode, DateTime? timestamp = null)
    {
        Guard.Against.Null(sequencer, nameof(sequencer));
        Guard.Against.NullOrWhiteSpace(disciplineCode, nameof(disciplineCode));

        var discUpper = disciplineCode.Trim().ToUpperInvariant();
        var discClean = discUpper;
        if (discClean.StartsWith("DISC-") || discClean.StartsWith("DISC_"))
            discClean = discClean[5..];

        string prefix;
        if (discClean.Contains("PAH") || discClean.Contains("HISTO") || discClean == "H")
        {
            prefix = "H";
        }
        else if (discClean.Contains("PAS") || discClean.Contains("SITO") || discClean.Contains("CYTO") || discClean == "S")
        {
            prefix = "S";
        }
        else if (discClean.Contains("H") && !discClean.Contains("S"))
        {
            prefix = "H";
        }
        else if (discClean.Contains("S") && !discClean.Contains("H"))
        {
            prefix = "S";
        }
        else
        {
            throw new ArgumentException(
                $"Kode disiplin '{disciplineCode}' tidak dikenali untuk registrasi Patologi Anatomi (harus mengandung 'PAH'/'H' atau 'PAS'/'S').",
                nameof(disciplineCode));
        }

        var time = timestamp ?? DateTime.Now;
        var yy = time.ToString("yy");
        var sequenceTag = $"PA_{prefix}_{yy}";
        var nextNo = sequencer.GetNextNoUrut(sequenceTag);

        return $"{prefix}.{yy}.{nextNo:D4}";
    }
}
