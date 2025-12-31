using Bilreg.Domain.AdmisiContext.LayananFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface IStokRepo :
    IListData<StokView, ILayananKey, string>
{
}

public record StokView(string BrgId, string BrgName, decimal Qty, string Satuan);