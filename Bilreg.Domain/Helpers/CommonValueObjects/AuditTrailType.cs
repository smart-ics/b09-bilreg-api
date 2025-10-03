namespace Emr25.Domain.HelpersContext.CommonValueObjects;

public class AuditTrailType
{
    private AuditTrailType(AuditInfoType created, AuditInfoType modified, AuditInfoType voided)
    {
        Created = created;
        Modified = modified;
        Voided = voided;
    }
    public AuditInfoType Created { get; init; }
    public AuditInfoType Modified { get; private set; }
    public AuditInfoType Voided { get; private set; }

    public void Batal(string userId, DateTime timestamp)
    {
        Voided = new AuditInfoType(userId, timestamp);
    }
    public void Modif(string userId, DateTime timestamp)
    {
        Modified = new AuditInfoType(userId, timestamp);
    }
    
    public static AuditTrailType Default 
    => new AuditTrailType(AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default);
    
    public static AuditTrailType Create(string userId, DateTime created)
    => new AuditTrailType(new AuditInfoType(userId, created), AuditInfoType.Default, AuditInfoType.Default);
}