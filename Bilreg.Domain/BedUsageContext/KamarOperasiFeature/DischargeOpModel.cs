using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public interface IDischargeOpKey
{
    string DischargeOpId { get; }
}

public class DischargeOpModel : IDischargeOpKey
{
    #region CREATION
    public DischargeOpModel(string dischargOpId, DateTime dischargeDate, AuditTrailType auditTrail, 
        OrderOpReff orderop, PasienReff pasien, RegReff reg, KamarReff kamar, 
        PatientConditionEnum kondisiPasien)
    {
        DischargeOpId = dischargOpId;
        DischargeDate = dischargeDate;
        AuditTrail = auditTrail;
        OrderOp = orderop;
        Pasien = pasien;
        Reg = reg;
        KamarTujuan = kamar;
        KondisiPasien = kondisiPasien;
    }
    #endregion

    #region PROPERTIES
    public string DischargeOpId { get; init; }
    public DateTime DischargeDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public OrderOpReff OrderOp { get; init; }
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    public KamarReff KamarTujuan { get; init; }
    public PatientConditionEnum KondisiPasien { get; init; }
    #endregion

    #region BEHAVIOUR
    #endregion
}

