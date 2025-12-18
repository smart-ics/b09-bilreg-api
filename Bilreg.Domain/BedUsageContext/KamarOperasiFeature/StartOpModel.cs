using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class StartOpModel : IStartOpKey
{
    #region CREATION
    public StartOpModel(string startOpId, DateTime startOpTime, AuditTrailType auditTrail,
        OrderOpReff orderOp, ScheduleOpReff scheduleOp, KamarReff kamarOp,
        PasienReff pasien, RegReff reg)
    {
        StartOpId = startOpId;
        StartOpTime = startOpTime;
        AuditTrail = auditTrail;
        OrderOp = orderOp;
        ScheduleOp = scheduleOp;
        KamarOp = kamarOp;
        Pasien = pasien;
        Reg = reg;
    }

    public static StartOpModel Default =>
        new StartOpModel("-", new DateTime(3000, 1, 1, 0, 0, 0), AuditTrailType.Default,
            OrderOpModel.Default.ToReff(), ScheduleOpModel.Default.ToReff(),
            KamarType.Default.ToReff(), PasienModel.Default.ToReff(),
            RegModel.Default.ToReff());
    public static StartOpModel Key(string id) =>
        new StartOpModel(id, new DateTime(3000, 1, 1, 0, 0, 0), AuditTrailType.Default,
            OrderOpModel.Default.ToReff(), ScheduleOpModel.Default.ToReff(),
            KamarType.Default.ToReff(), PasienModel.Default.ToReff(),
            RegModel.Default.ToReff());

    public static StartOpModel CreateFromSchedule(ScheduleOpModel scheduleOp, DateTime tglOp,
        KamarType kamar, string userId)
    {
        var newId = Ulid.NewUlid().ToString();
        var audit = new AuditTrailType(new AuditInfoType(userId, DateTime.Now),
            AuditInfoType.Default, AuditInfoType.Default);
        var order = scheduleOp.OrderOp;
        var pasien = scheduleOp.Pasien;
        var reg = scheduleOp.Reg;
        return new StartOpModel(newId, tglOp, audit, order, scheduleOp.ToReff(), kamar.ToReff(),
            pasien, reg);
    }
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
    #endregion
}

public interface IStartOpKey
{
    string StartOpId { get; }
}
