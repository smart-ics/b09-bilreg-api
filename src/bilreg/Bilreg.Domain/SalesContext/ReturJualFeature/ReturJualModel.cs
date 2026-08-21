using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Domain.SalesContext.ReturJualFeature;

public class ReturJualModel : IReturJualKey
{
    private readonly List<ReturJualItemType> _listItem;

    public ReturJualModel(
        string returJualId,
        PenjualanReff penjualan,
        LayananReff layanan,
        string reason,
        TipeJaminanReff tipeJaminan,
        NilaiReturJualType nilai,
        AuditTrailType auditTrail,
        IEnumerable<ReturJualItemType> listItem)
    {
        ReturJualId = returJualId;
        Penjualan = penjualan;
        Layanan = layanan;
        Reason = reason;
        TipeJaminan = tipeJaminan;
        Nilai = nilai;
        AuditTrail = auditTrail;
        _listItem = listItem.ToList();
    }

    public string ReturJualId { get; private set; }
    public PenjualanReff Penjualan { get; private set; }
    public LayananReff Layanan { get; private set; }
    public string Reason { get; private set; }
    public TipeJaminanReff TipeJaminan { get; init; }
    public NilaiReturJualType Nilai { get; private set; }
    public AuditTrailType AuditTrail { get; private set; }
    public IEnumerable<ReturJualItemType> ListItem => _listItem;
}
