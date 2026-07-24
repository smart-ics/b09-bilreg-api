namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAdmissionQueueRolloutRepo
{
    bool TableExists(string tableName);
    bool IndexExists(string indexName, string tableName);
}
