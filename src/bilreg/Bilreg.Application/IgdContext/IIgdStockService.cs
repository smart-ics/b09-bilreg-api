using Bilreg.Domain.IgdContext.TindakanIgdFeature;

namespace Bilreg.Application.IgdContext;

public record IgdBomProcessResult(
    string PakaiBrgId,
    bool IsChargeBarang,
    IReadOnlyList<TindakanIgdBarangType> ListBarang);

public interface IIgdStockService
{
    Task<string> DeductBhpAsync(
        string layananId,
        string brgId,
        decimal qty,
        string userId,
        string keterangan,
        CancellationToken cancellationToken = default);

    Task<IgdBomProcessResult> ProcessTindakanBomAsync(
        string tindakanIgdId,
        string tarifId,
        int tindakanQty,
        string layananId,
        string regId,
        string userId,
        CancellationToken cancellationToken = default);

    Task VoidTindakanStockAsync(
        string tindakanIgdId,
        string headerPakaiBrgId,
        IEnumerable<string>? itemPakaiBrgIds,
        string userId,
        string voidReason,
        DateTime voidDate,
        CancellationToken cancellationToken = default);
}
