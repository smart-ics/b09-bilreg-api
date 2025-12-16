using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class StartOpModel : IStartOpKey
{
    private readonly List<ScheduleOpPpaType> _listPpa;

    #region CREATION
    public StartOpModel(string startOpId, DateTime startOpTime, AuditTrailType auditTrail,
        OrderOpReff orderOp, ScheduleOpReff scheduleOp, KamarReff kamarOp,
        PasienReff pasien, RegReff reg, PpaReff teamLead, IEnumerable<ScheduleOpPpaType> listPpa)
    {
        StartOpId = startOpId;
        StartOpTime = startOpTime;
        AuditTrail = auditTrail;
        OrderOp = orderOp;
        ScheduleOp = scheduleOp;
        KamarOp = kamarOp;
        Pasien = pasien;
        Reg = reg;
        TeamLead = teamLead;
        _listPpa = listPpa.ToList() ?? [];
    }

    public static StartOpModel Default =>
        new StartOpModel("-", new DateTime(3000, 1, 1, 0, 0, 0), AuditTrailType.Default,
            OrderOpModel.Default.ToReff(), ScheduleOpModel.Default.ToReff(),
            KamarType.Default.ToReff(), PasienModel.Default.ToReff(),
            RegModel.Default.ToReff(), PpaType.Default.ToReff(), []);
    #endregion

    #region PROPERTIES
    public string StartOpId { get; init; }
    public DateTime StartOpTime { get; init; }
    public AuditTrailType AuditTrail { get; init; }

    public OrderOpReff OrderOp { get; init; }
    public ScheduleOpReff ScheduleOp { get; init; }
    public KamarReff KamarOp { get; init; }
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    public PpaReff TeamLead { get; init; }
    public IEnumerable<ScheduleOpPpaType> ListPpa => _listPpa;
    #endregion
}

public interface IStartOpKey
{
    string StartOpId { get; }
}
