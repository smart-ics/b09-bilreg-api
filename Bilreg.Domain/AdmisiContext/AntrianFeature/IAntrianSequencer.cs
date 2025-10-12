namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

public interface IAntrianSequencer
{
    void CreateSequence(string sequenceTag);
    int  GetNextNoUrut(string sequenceTag);
}

