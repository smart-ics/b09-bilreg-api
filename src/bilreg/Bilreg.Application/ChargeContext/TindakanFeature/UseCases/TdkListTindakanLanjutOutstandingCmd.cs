using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

//  M03-F01 P2-S04 — outstanding order query for supervision.
//  Without RegId lists outstanding orders across visits; with RegId
//  scopes to a single visit. Outstanding = Proposed or Sent (not yet
//  received), per TindakanLanjutModel.IsOutstanding.
public record TdkListTindakanLanjutOutstandingCmd(string? RegId = null)
    : IRequest<IEnumerable<TdkTindakanLanjutOutstandingResponse>>;

public record TdkTindakanLanjutOutstandingResponse(
    string TindakanLanjutId, DateTime TindakanLanjutDate,
    string RegId, string PasienId, string PasienName,
    string OrderType, string LayananId, string LayananName,
    int OrderState,
    IEnumerable<TdkTindakanLanjutItemResponse> ListItem);

public record TdkTindakanLanjutItemResponse(
    int ItemNo, string ItemCode, string ItemName, decimal Qty, string Note);

public class TdkListTindakanLanjutOutstandingHandler
    : IRequestHandler<TdkListTindakanLanjutOutstandingCmd,
        IEnumerable<TdkTindakanLanjutOutstandingResponse>>
{
    private readonly ITindakanLanjutRepo _tindakanLanjutRepo;

    public TdkListTindakanLanjutOutstandingHandler(ITindakanLanjutRepo tindakanLanjutRepo)
    {
        _tindakanLanjutRepo = tindakanLanjutRepo;
    }

    public Task<IEnumerable<TdkTindakanLanjutOutstandingResponse>> Handle(
        TdkListTindakanLanjutOutstandingCmd request, CancellationToken cancellationToken)
    {
        IEnumerable<TindakanLanjutModel> listOrder = string.IsNullOrWhiteSpace(request.RegId)
            ? _tindakanLanjutRepo.ListOutstandingAll()
            : _tindakanLanjutRepo.ListOutstanding(RegModel.Key(request.RegId));

        var result = (listOrder?.ToList() ?? []).Select(ToResponse);
        return Task.FromResult(result);
    }

    private static TdkTindakanLanjutOutstandingResponse ToResponse(TindakanLanjutModel order)
    {
        return new TdkTindakanLanjutOutstandingResponse(
            order.TindakanLanjutId, order.TindakanLanjutDate,
            order.Reg.RegId, order.Reg.PasienId, order.Reg.PasienName,
            order.OrderType, order.Layanan.LayananId, order.Layanan.LayananName,
            (int)order.OrderState,
            (order.ListItem?.ToList() ?? []).Select(x =>
                new TdkTindakanLanjutItemResponse(
                    x.ItemNo, x.ItemCode, x.ItemName, x.Qty, x.Note)));
    }
}
