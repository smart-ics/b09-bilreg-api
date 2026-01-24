using Bilreg.Domain.AdmisiContext.PpaFeature;
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
        OrderOpReff orderop, PpaReff dokter, PasienReff pasien, RegReff reg, KamarReff kamar,
        PatientConditionEnum kondisiPasien, string postOpNote)
    {
        DischargeOpId = dischargOpId;
        DischargeDate = dischargeDate;
        AuditTrail = auditTrail;
        OrderOp = orderop;
        Dokter = dokter;
        Pasien = pasien;
        Reg = reg;
        KamarTujuan = kamar;
        KondisiPasien = kondisiPasien;
        PostOpNote = postOpNote;
    }
    #endregion

    #region PROPERTIES
    public string DischargeOpId { get; init; }
    public DateTime DischargeDate { get; init; }
    public AuditTrailType AuditTrail { get; init; }
    public OrderOpReff OrderOp { get; init; }
    public PpaReff Dokter { get; init; }
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    public KamarReff KamarTujuan { get; init; }
    public PatientConditionEnum KondisiPasien { get; init; }
    public string PostOpNote { get; init; }
    #endregion

    #region BEHAVIOUR
    #endregion
}

