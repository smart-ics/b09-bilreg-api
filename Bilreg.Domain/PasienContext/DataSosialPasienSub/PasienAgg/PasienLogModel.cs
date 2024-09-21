using System.Text.Json;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienLogModel(string pasienId, DateTime logDate, string activity, string userId): IPasienKey
{
    public string PasienId { get; protected set; } = pasienId;
    public DateTime LogDate { get; protected set; } = logDate;
    public string Activity { get; protected set; } = activity;
    public string UserId { get; protected set; } = userId;
    public string ChangeLog { get; protected set; } = string.Empty;

    public void SetLogDate(DateTime time) => LogDate = time;
    public void SetChangeLog(object changeLog) => ChangeLog = JsonSerializer.Serialize(changeLog);
}