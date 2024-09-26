using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienLogDto(): PasienLogModel(string.Empty, string.Empty, string.Empty)
{
    public string PasienId { get => base.PasienId; set => base.PasienId = value; }
    public string LogDate { get => base.LogDate; set => base.LogDate = value; }
    public string Activity { get => base.Activity; set => base.Activity = value; }
    public string UserId { get => base.UserId; set => base.UserId = value; }
    public string ChangeLog { get => base.ChangeLog; set => base.ChangeLog = value; }
}