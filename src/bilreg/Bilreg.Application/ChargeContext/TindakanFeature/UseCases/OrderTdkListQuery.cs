// using Bilreg.Application.AdmisiContext.RegFeature;
// using Bilreg.Domain.AdmisiContext.LayananFeature;
// using Bilreg.Domain.AdmisiContext.RegFeature;
// using Bilreg.Domain.ChargeContext.TindakanFeature;
// using Bilreg.Domain.PasienContext.PasienFeature;
// using MediatR;
//
// namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
//
// public record OrderTdkListQuery(string RegId, string LayananId) : 
//     IRequest<IEnumerable<OrderTdkListResponse>>, IRegKey, ILayananKey;
//
// public record OrderTdkListResponse(
//     string OrderId,
//     string OrderDate,
//     string DokterOrderId,
//     string DokterOrderName,
//     string TindakanId,
//     string TindakanDate,
//     string ReffDate,
//     string TarifId,
//     string TarifName,
//     string Ppa);
//
// public class OrderTdkListHandler : IRequestHandler<OrderTdkListQuery, IEnumerable<OrderTdkListResponse>>
// {
//     private readonly IOrderTdkRepo _orderTdkRepo;
//     private readonly ITindakanRepo _tdkRepo;
//     private readonly IRegRepo _regRepo;
//     public OrderTdkListHandler(IOrderTdkRepo orderTdkRepo, 
//         ITindakanRepo tdkRepo, 
//         IRegRepo regRepo)
//     {
//         _orderTdkRepo = orderTdkRepo;
//         _tdkRepo = tdkRepo;
//         _regRepo = regRepo;
//     }
//
//     public Task<IEnumerable<OrderTdkListResponse>> Handle(OrderTdkListQuery request, CancellationToken cancellationToken)
//     {
//         var reg = _regRepo.LoadEntity(request)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Register {request.RegId} not valid")
//             );
//         var listOrder = _orderTdkRepo.ListData(PasienModel.Key(reg.Pasien.PasienId))?.ToList() ?? [];
//         var listTdk = _tdkRepo.ListData(reg)?.ToList() ?? [];
//
//         var listOrderLyn = listOrder.Where(x => x.Layanan.LayananId == request.LayananId)?.ToList() ?? [];
//         var listTdkLyn = listTdk.Where(x => x.Layanan.LayananId == request.LayananId)?.ToList() ?? [];
//
//         var result = GenResponse(listOrderLyn, listTdkLyn)?.ToList() ?? [];
//
//         return Task.FromResult(result.AsEnumerable());
//     }
//
//     private IEnumerable<OrderTdkListResponse> GenResponse(List<OrderTdkModel> listOrder, List<TindakanView> listTdk)
//     {
//         var result =
//             from order in listOrder
//             join tdk in listTdk
//                 on order.OrderTdkId equals tdk.OrderTdk.OrderTdkId
//                 into tdkJoin
//             from tdk in tdkJoin.DefaultIfEmpty()
//             
//             select new OrderTdkListResponse(
//                 OrderId: order.OrderTdkId,
//                 OrderDate: order.OrderTdkDate.ToString("yyyy-MM-dd HH:mm:ss"),
//                 DokterOrderId: order.DokterOrder.PpaId,
//                 DokterOrderName: order.DokterOrder.PpaName,
//
//                 TindakanId: tdk is null ? "-" : tdk.TindakanId,
//                 TindakanDate: tdk is null
//                     ? "3000-01-01"
//                     : tdk.TindakanDate.ToString("yyyy-MM-dd HH:mm:ss"),
//
//                 ReffDate: order.OrderTdkDate.ToString("yyyy-MM-dd HH:mm:ss"),
//
//                 TarifId: tdk is null
//                     ? order.Tarif.TarifId
//                     : tdk.Tarif.TarifId,
//
//                 TarifName: tdk is null
//                     ? (!string.IsNullOrWhiteSpace(order.Tarif.TarifName)
//                         ? order.Tarif.TarifName
//                         : order.FreeTextOrder)
//                     : tdk.Tarif.TarifName,
//                 Ppa: order.DokterOrder.PpaName
//             );
//         return result;
//     }
// }
