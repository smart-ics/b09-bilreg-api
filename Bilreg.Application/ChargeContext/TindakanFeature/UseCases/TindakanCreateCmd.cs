// using Ardalis.GuardClauses;
// using Bilreg.Application.AdmisiContext.LayananFeature;
// using Bilreg.Application.AdmisiContext.RegFeature;
// using Bilreg.Application.ChargeContext.TarifFeature;
// using Bilreg.Application.PasienContext.PasienFeature;
// using Bilreg.Domain.AdmisiContext.LayananFeature;
// using Bilreg.Domain.AdmisiContext.RegFeature;
// using Bilreg.Domain.ChargeContext.TarifFeature;
// using Bilreg.Domain.ChargeContext.TindakanFeature;
// using Bilreg.Domain.PasienContext.PasienFeature;
// using MediatR;
//
// namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
//
// public record TindakanCreateCmd(
//     string RegId, string LayananId, string TipeTarifId,
//     string OrderTdkId, string KelasId, int JenisTindakan,
//     TindakanTarifDto Tarif, string UserId) 
//     : IRequest<TindakanCreateRespose>, 
//     IRegKey, ILayananKey, IOrderTdkKey;
//
// public record TindakanCreateRespose(string TindakanId);
//
// public class TindakanCreateHandler : IRequestHandler<TindakanCreateCmd, TindakanCreateRespose>
// {
//     private readonly IRegRepo _regRepo;
//     private readonly ILayananRepo _layananRepo;
//     private readonly ITipeTarifRepo _tipeTarifRepo;
//     private readonly INilaiTarifRepo _nilaiTarifRepo;
//     private readonly IOrderTdkRepo _orderTdkRepo;
//     private readonly ITindakanRepo _tindakanRepo;
//     private readonly IPasienRepo _pasienRepo;
//     private readonly ITarifRepo _tarifRepo;
//     private readonly ITindakanFactory _tdkFactory;
//     public TindakanCreateHandler(IRegRepo regRepo,
//         ILayananRepo layananRepo,
//         ITipeTarifRepo tipeTarifRepo,
//         INilaiTarifRepo nilaiTarifRepo,
//         IOrderTdkRepo orderTdkRepo,
//         ITindakanRepo tindakanRepo,
//         IPasienRepo pasienRepo,
//         ITarifRepo tarifRepo,
//         ITindakanFactory tdkFactory)
//     {
//         _regRepo = regRepo;
//         _layananRepo = layananRepo;
//         _tipeTarifRepo = tipeTarifRepo;
//         _nilaiTarifRepo = nilaiTarifRepo;
//         _orderTdkRepo = orderTdkRepo;
//         _tindakanRepo = tindakanRepo;
//         _pasienRepo = pasienRepo;
//         _tarifRepo = tarifRepo;
//         _tdkFactory = tdkFactory;
//     }
//
//     public Task<TindakanCreateRespose> Handle(TindakanCreateCmd request, CancellationToken cancellationToken)
//     {
//         // GUARD
//         Guard.Against.NullOrWhiteSpace(request.RegId);
//         Guard.Against.NullOrWhiteSpace(request.LayananId);
//         Guard.Against.NullOrWhiteSpace(request.TipeTarifId);
//         Guard.Against.NullOrWhiteSpace(request.KelasId);
//
//         var reg = LoadReg(request);
//         var pasien = LoadPasien(PasienModel.Key(reg.Pasien.PasienId));
//         var layanan = LoadLayanan(request);
//         var tipeTarif = LoadTipeTarif(TipeTarifType.Key(request.TipeTarifId));
//         var orderTdk = LoadOrderTdk(request);
//         var tarif = LoadTarif(TarifType.Key(request.Tarif.Tarif.TarifId));
//         var nilaiTarifCompKey = NilaiTarifType.KeyComposite(tarif.TarifId, request.TipeTarifId, request.KelasId);
//         var nilaiTarif = LoadNilaiTaif(nilaiTarifCompKey);
//
//         var tdkTarif = _tdkFactory.BuildTindakanTarif(tarif, nilaiTarif, request.Tarif);
//         var tindakan = _tdkFactory.Create((JenisTindakanEnum)request.JenisTindakan, orderTdk,
//             pasien, reg, layanan, tipeTarif, tdkTarif, request.UserId);
//
//         if (orderTdk.OrderTdkId != "-")
//         {
//             orderTdk.Execute(request.UserId);
//             _orderTdkRepo.SaveChanges(orderTdk);
//         }
//         
//         _tindakanRepo.SaveChanges(tindakan);
//
//         return Task.FromResult(new TindakanCreateRespose(tindakan.TindakanId));
//     }
//
//     #region PRIVATE-HELPER
//
//     private RegModel LoadReg(IRegKey key)
//     {
//         var result = _regRepo.LoadEntity(key)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Register {key.RegId} not found")
//             );
//         return result;
//     }
//
//     private PasienModel LoadPasien(IPasienKey pasienKey)
//     {
//         var pasien = _pasienRepo.LoadEntity(pasienKey)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"pasien {pasienKey.PasienId} not found")
//             );
//         return pasien;
//     }
//
//     private LayananType LoadLayanan(ILayananKey lynKey)
//     {
//         var layanan = _layananRepo.LoadEntity(lynKey)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Layanan {lynKey.LayananId} not found")
//             );
//         return layanan;  
//     }
//     
//     private TipeTarifType LoadTipeTarif(ITipeTarifKey tpTarifKey)
//     {
//         var tipeTarif = _tipeTarifRepo.LoadEntity(tpTarifKey)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Tipe Tarif {tpTarifKey.TipeTarifId} not found")
//             );
//         return tipeTarif;
//     }
//     private OrderTdkModel LoadOrderTdk(IOrderTdkKey orderKey)
//     {
//         var orderTdk = _orderTdkRepo.LoadEntity(orderKey)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => OrderTdkModel.Default
//             );
//         return orderTdk;
//     }
//     
//     private TarifType LoadTarif(ITarifKey tarifKey)
//     {
//         var tarif = _tarifRepo.LoadEntity(tarifKey)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Tarif {tarifKey.TarifId} not found")
//             );
//         return tarif;
//     }
//     private NilaiTarifType LoadNilaiTaif(INilaiTarifCompositKey nilaiKey)
//     {
//         var nilaiTarif = _nilaiTarifRepo.LoadEntity(nilaiKey)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => throw new KeyNotFoundException($"Nilai Tarif {nilaiKey.TarifId} not found")
//             );
//         return nilaiTarif;
//     }
//    
//
//     #endregion
// }