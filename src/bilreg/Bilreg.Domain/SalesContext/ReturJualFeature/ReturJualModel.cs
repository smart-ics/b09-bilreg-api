using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.SalesContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public class ReturJualModel : IReturJualKey
{
    public string ReturJualId { get; private set; }
    public string PenjualanId { get; private set; }
    public LayananReff Layanan { get; private set; }
    public string Reason { get; private set; }
    public RegReff Register { get; private set; }
    public PasienReff Pasien { get; private set; }
    public TipeJaminanReff TipeJaminan { get; private set; }
    public NilaiReturJualType Nilai { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }
}
