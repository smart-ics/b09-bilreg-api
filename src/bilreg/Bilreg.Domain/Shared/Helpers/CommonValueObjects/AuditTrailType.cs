namespace Bilreg.Domain.Shared.Helpers.CommonValueObjects;

public class AuditTrailType
{
    public AuditTrailType(AuditInfoType created, AuditInfoType modified, AuditInfoType voided)
    {
        Created = created;
        Modified = modified;
        Voided = voided;
        IsVoided = !string.IsNullOrWhiteSpace(voided.UserId) && voided.Timestamp.Date != new DateTime(3000, 1, 1);
    }
    public AuditInfoType Created { get; init; }
    public AuditInfoType Modified { get; private set; }
    public AuditInfoType Voided { get; private set; }
    public bool IsVoided { get; private set; }

    public void Batal(string userId, DateTime timestamp)
    {
        if (userId.Length == 0 || timestamp.Date == new DateTime(3000, 1, 1))
            throw new ArgumentException("VoidDate-UserId invalid");
        
        Voided = new AuditInfoType(userId, timestamp);
        IsVoided = true;
    }
    public void Modif(string userId, DateTime timestamp)
    {
        if (userId.Length == 0 || timestamp.Date == new DateTime(3000, 1, 1))
            throw new ArgumentException("ModifDate-UserId invalid");

        Modified = new AuditInfoType(userId, timestamp);
    }

    public AuditTrailType Update(string userId, DateTime timestamp)
    {
        Modif(userId, timestamp);
        return this;
    }
    
    public static AuditTrailType Default 
    => new AuditTrailType(AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default);
    
    public static AuditTrailType Create(string userId, DateTime created)
    => new AuditTrailType(new AuditInfoType(userId, created), AuditInfoType.Default, AuditInfoType.Default);
}

public record AuditInfoType(string UserId, DateTime Timestamp)
{
    public AuditInfoType(string userId, string tgl, string jam) :
        this(string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim(), ParseDateTime(tgl, jam))
    {
    }

    private static DateTime ParseDateTime(string? tgl, string? jam)
    {
        if (string.IsNullOrWhiteSpace(tgl) || tgl.Trim().StartsWith("3000-01-01") || tgl.Trim().StartsWith("3000/01/01"))
            return new DateTime(3000, 1, 1);

        var datePart = tgl.Trim();
        var timePart = string.IsNullOrWhiteSpace(jam) ? "00:00:00" : jam.Trim();

        if (timePart.Length == 5)
            timePart += ":00";

        if (DateTime.TryParseExact($"{datePart} {timePart}", "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var dt))
            return dt;

        if (DateTime.TryParse($"{datePart} {timePart}", out dt))
            return dt;

        if (DateTime.TryParse(datePart, out dt))
            return dt;

        return new DateTime(3000, 1, 1);
    }

    public static AuditInfoType Default => new("", new DateTime(3000,1,1));
};
