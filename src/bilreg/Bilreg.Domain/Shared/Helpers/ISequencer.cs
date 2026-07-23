namespace Bilreg.Domain.Shared.Helpers;

public interface ISequencer
{
    void CreateSequence(string sequenceTag);
    int  GetNextNoUrut(string sequenceTag);
    int GetNextNoUrut(string sequenceTag, int maxValue);
}

public class SequenceExhaustedException : InvalidOperationException
{
    public SequenceExhaustedException(string sequenceTag, int maxValue, Exception? innerException = null)
        : base($"Sequence '{sequenceTag}' is exhausted at {maxValue}.", innerException)
    {
    }
}

public interface ISequencerManual
{
    long GetNextNoUrut(string sequenceTag, string description);
}
