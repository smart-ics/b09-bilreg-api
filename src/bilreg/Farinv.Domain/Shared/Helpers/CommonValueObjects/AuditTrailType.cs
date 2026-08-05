namespace Farinv.Domain.Shared.Helpers.CommonValueObjects;

public class AuditTrailType
{
    public AuditTrailType(AuditInfoType created, AuditInfoType modified, AuditInfoType voided)
    {
        Created = created;
        Modified = modified;
        Voided = voided;
        IsVoided = false;
    }
    public AuditInfoType Created { get; init; }
    public AuditInfoType Modified { get; private set; }
    public AuditInfoType Voided { get; private set; }
    public bool IsVoided { get; private set; }

    public void Batal(string userId, DateTime timestamp)
    {
        if (userId.Length == 0 && timestamp.Date == new DateTime(3000, 1, 1))
            throw new ArgumentException("VoidDate-UserId invalid");
        
        Voided = new AuditInfoType(userId, timestamp);
        IsVoided = true;
    }
    
    public void Modif(string userId, DateTime timestamp)
    {
        if (userId.Length == 0 && timestamp.Date == new DateTime(3000, 1, 1))
            throw new ArgumentException("ModifDate-UserId invalid");

        Modified = new AuditInfoType(userId, timestamp);
    }
    
    public static AuditTrailType Default 
    => new AuditTrailType(AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default);
    
    public static AuditTrailType Create(string userId, DateTime created)
    => new AuditTrailType(new AuditInfoType(userId, created), AuditInfoType.Default, AuditInfoType.Default);
}

public record AuditInfoType(string UserId, DateTime Timestamp)
{
    public AuditInfoType(string userId, string tgl, string jam) :
        this(userId, DateTime.ParseExact($"{tgl} {jam}", "yyyy-MM-dd HH:mm:ss", null))
    {
    }
    public static AuditInfoType Default => new("-", new DateTime(3000,1,1));
};