using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class OrderOpModel
{
    public string OrderOp { get; init; }
    public DateTime OrderDate { get; init; }
    public AuditTrailType AuditTrail { get; private set; }
    
    public PasienReff Pasien { get; init; }
    public RegReff Reg { get; init; }
    
    public Icd10Type Icd10 { get; init; }
    public JenisOperasiType JenisOperasi { get; init; }
}