namespace Farinv.Domain.Shared.Helpers.CommonValueObjects;

public record ApprovalType(string UserId, DateTime Timestamp)
{
    public ApprovalType(string userId, string tgl, string jam) :
        this(userId, DateTime.ParseExact($"{tgl} {jam}", "yyyy-MM-dd HH:mm:ss", null))
    {
    }

    public static ApprovalType Default => new("-", new DateTime(3000, 1, 1));
}
