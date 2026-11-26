using System.Data;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.MutasiFeature;

public interface ITerimaMutasiRepo :
    ISaveChange<TerimaMutasiModel>,
    ILoadEntity<TerimaMutasiModel, ITerimaMutasiKey>
{
    void SaveChanges(TerimaMutasiModel model, IDbConnection conn, IDbTransaction trans);
    MayBe<TerimaMutasiModel> LoadByMutasiBrgId(string mutasiBrgId);
    IEnumerable<TerimaMutasiHeaderView> ListData(Periode filter, string layananTujuanId = "");
}

public record TerimaMutasiHeaderView(
    string TerimaMutasiId,
    DateTime TerimaMutasiDate,
    TerimaMutasiStateEnum State,
    string MutasiBrgId,
    string OrderMutasiId,
    LayananReff LayananAsal,
    LayananReff LayananTujuan,
    string ReceiverUserId,
    string Note,
    string RejectionReason) : ITerimaMutasiKey;
