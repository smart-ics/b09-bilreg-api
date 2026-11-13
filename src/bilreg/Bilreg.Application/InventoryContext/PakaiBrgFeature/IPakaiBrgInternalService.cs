using MediatR;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature;

public record PakaiBrgCreateFromTindakanItem(
    string BrgId,
    decimal Qty,
    string NoBatch = "",
    DateTime? TglEd = null,
    decimal Hpp = 0m,
    string KeteranganItem = "");

public record PakaiBrgCreateFromTindakanCommand(
    string RegId,
    string LayananId,
    string UserId,
    string KeteranganHeader,
    IEnumerable<PakaiBrgCreateFromTindakanItem> ListItem
) : IRequest<PakaiBrgCreateFromTindakanResult>;

public record PakaiBrgCreateFromTindakanItemResult(
    int NoUrut,
    string BrgId,
    decimal Qty,
    string NoBatch,
    DateTime TglEd,
    decimal Hpp,
    decimal SubTotal);

public record PakaiBrgCreateFromTindakanResult(
    string PakaiBrgId,
    DateTime TglPakai,
    decimal TotalNilai,
    int JumlahItem,
    IReadOnlyList<PakaiBrgCreateFromTindakanItemResult> Items);

public record PakaiBrgVoidFromTindakanCommand(
    string PakaiBrgId,
    string UserId,
    string AlasanVoid = "Batal tindakan"
) : IRequest<PakaiBrgVoidFromTindakanResult>;

public record PakaiBrgVoidFromTindakanResult(
    string PakaiBrgId,
    bool IsVoid,
    DateTime TglVoid,
    string AlasanVoid);

public interface IPakaiBrgInternalService
{
    Task<PakaiBrgCreateFromTindakanResult> CreateFromTindakan(
        PakaiBrgCreateFromTindakanCommand command,
        CancellationToken cancellationToken = default);

    Task<PakaiBrgVoidFromTindakanResult> VoidFromTindakan(
        PakaiBrgVoidFromTindakanCommand command,
        CancellationToken cancellationToken = default);
}
