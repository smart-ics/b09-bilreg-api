// using Bilreg.Domain.AdmisiContext.LayananFeature;
// using Bilreg.Domain.AdmisiContext.RegFeature;
// using Bilreg.Domain.ChargeContext.TarifFeature;
// using Bilreg.Domain.ChargeContext.TindakanFeature;
// using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
// using MediatR;
//
// namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
//
// public record TindakanGetQuery(string TindakanId) : IRequest<TindakanGetResponse>, ITindakanKey;
//
// public record TindakanGetResponse(string TindakanId,
//     string TindakanDate,
//     int JenisTindakan,
//     string JenisTindakanName,
//     AuditTrailType AuditTrail,
//     OrderTindakanReff OrderTindakan,
//     RegReff Reg,
//     LayananReff Layanan,
//     TipeTarifReff TipeTarif,
//     TindakanTarifModel Tarif);
//
// public class TindakanGetHandler : IRequestHandler<TindakanGetQuery, TindakanGetResponse>
// {
//     private readonly ITindakanRepo _tdkRepo;
//
//     public TindakanGetHandler(ITindakanRepo tdkRepo)
//     {
//         _tdkRepo = tdkRepo;
//     }
//
//     public Task<TindakanGetResponse> Handle(TindakanGetQuery request, CancellationToken cancellationToken)
//     {
//         var tdk = _tdkRepo.LoadEntity(request)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Tindakan {request.TindakanId} not found")
//             );
//         
//         var result = new TindakanGetResponse(
//             tdk.TindakanId, tdk.TindakanDate.ToString("yyyy-MM-dd HH:mm:ss"),
//             (int)tdk.JenisTindakan, tdk.JenisTindakan.ToString(), tdk.AuditTrail, tdk.OrderTindakan,
//             tdk.Reg, tdk.Layanan, tdk.TipeTarif, tdk.Tarif);
//         
//         return Task.FromResult(result);
//     }
// }
