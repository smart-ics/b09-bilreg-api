using System.Globalization;

namespace Bilreg.Domain.Shared.Helpers.CommonValueObjects;

public class AuditTrailType
{
    public AuditTrailType(AuditInfoType created, AuditInfoType modified, AuditInfoType voided)
    {
        Created = created;
        Modified = modified;
        Voided = voided;
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
    
    public static AuditTrailType Default 
    => new AuditTrailType(AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default);
    
    public static AuditTrailType Create(string userId, DateTime created)
    => new AuditTrailType(new AuditInfoType(userId, created), AuditInfoType.Default, AuditInfoType.Default);
}

public record AuditInfoType(string UserId, DateTime Timestamp)
{
    private readonly string _userId = UserId?.Trim() ?? string.Empty;

    public string UserId
    {
        get => _userId;
        init => _userId = value?.Trim() ?? string.Empty;
    }

    public AuditInfoType(string userId, string tgl, string jam) :
        this(
            string.IsNullOrWhiteSpace(userId) ? string.Empty : userId,
            ParseTimestampOrSentinel(userId, tgl, jam))
    {
    }

    private static DateTime ParseTimestampOrSentinel(string userId, string tgl, string jam)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new DateTime(3000, 1, 1);

        var combined = $"{tgl?.Trim()} {jam?.Trim()}".Trim();
        if (DateTime.TryParseExact(combined, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
            return dtExact;

        if (DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtParsed))
            return dtParsed;

        return DateTime.ParseExact($"{tgl} {jam}", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(UserId) || Timestamp.Date >= new DateTime(3000, 1, 1);

    public static AuditInfoType Default => new("", new DateTime(3000, 1, 1));
}