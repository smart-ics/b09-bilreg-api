using System.Text.Json;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienLogModel(string pasienId, string activity, string userId): IPasienKey
{
    public string PasienId { get; protected set; } = pasienId;
    public DateTime LogDate { get; protected set; } = DateTime.Now;
    public string Activity { get; protected set; } = activity;
    public string UserId { get; protected set; } = userId;
    public string ChangeLog { get; protected set; } = string.Empty;

    public void SetChangeLog(object? changeLog)
    {
        if (changeLog is not null)
            ChangeLog = JsonSerializer.Serialize(changeLog);
    }
}