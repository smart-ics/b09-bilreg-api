using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

public interface ITrsBillingRepo :
    ISaveChange<TrsBillType>,
    ILoadEntity<TrsBillType, ITrsBillingKey>,
    IDeleteEntity<ITrsBillingKey>,
    IListData<TrsBillView, IRegKey>
{
    IEnumerable<TrsBillType> ListEntity(IRegKey regKey);
}

public record TrsBillView
{
    public TrsBillView(string trsBillingId, BillModulGroup modulGroup, DateTime tglTrs, RegReff reg, LayananReff layanan, KelasReff kelas, AuditInfoType auditInfo, RekapCetakReff rekapCetak, TrsBillNilaiType nilai, TrsBillKetType keterangan)
    {
        TrsBillingId = trsBillingId;
        ModulGroup = modulGroup;
        TglTrs = tglTrs;
        Reg = reg;
        Layanan = layanan;
        Kelas = kelas;
        AuditInfo = auditInfo;
        RekapCetak = rekapCetak;
        Nilai = nilai;
        Keterangan = keterangan;
    }

    public string TrsBillingId { get; init; }
    public BillModulGroup ModulGroup { get; init; }
    public DateTime TglTrs { get; init; }
    public RegReff Reg { get; init; }
    public LayananReff Layanan { get; init; }
    public KelasReff Kelas { get; init; }
    public AuditInfoType AuditInfo { get; init; }
    public RekapCetakReff RekapCetak { get; init; }
    public TrsBillNilaiType Nilai { get; init; }
    public TrsBillKetType Keterangan { get; init; }
}
