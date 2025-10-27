namespace Bilreg.Domain.Helpers;

public interface ISequencer
{
    void CreateSequence(string sequenceTag);
    int  GetNextNoUrut(string sequenceTag);
}

public interface ISequencerManual
{
    long GetNextNoUrut(string sequenceTag, string description);
}