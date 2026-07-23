namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IAdmissionQueueRolloutDal
{
    bool TableExists(string tableName);
    bool IndexExists(string indexName, string tableName);
}
