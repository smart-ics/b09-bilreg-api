using System.Text.Json;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienLogModel(string pasienId, string activity, string userId): IPasienKey
{
    public string PasienId { get; protected set; } = pasienId;
    public string LogDate { get; protected set; } = DateTime.Now.ToString(DateFormatEnum.YMD_HMS);
    public string Activity { get; protected set; } = activity;
    public string UserId { get; protected set; } = userId;
    public string ChangeLog { get; protected set; } = string.Empty;
    
    public void SetPasienId(string pasienId) => PasienId = pasienId;
    public void SetChangeLog(object? changeLog)
    {
        if (changeLog is not null)
            ChangeLog = JsonSerializer.Serialize(changeLog);
    }
}