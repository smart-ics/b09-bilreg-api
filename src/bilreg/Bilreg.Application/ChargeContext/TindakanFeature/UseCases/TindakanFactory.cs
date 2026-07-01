// using Bilreg.Application.AdmisiContext.PpaFeature;
// using Bilreg.Domain.AdmisiContext.LayananFeature;
// using Bilreg.Domain.AdmisiContext.PpaFeature;
// using Bilreg.Domain.AdmisiContext.RegFeature;
// using Bilreg.Domain.ChargeContext.TarifFeature;
// using Bilreg.Domain.ChargeContext.TindakanFeature;
// using Bilreg.Domain.PasienContext.PasienFeature;
// using Bilreg.Domain.Shared.Helpers;
// using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
//
// namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
//
// public interface ITindakanFactory : INunaFactory<TindakanModel>
// {
//     TindakanModel Create(JenisTindakanEnum jenisTindakan,
//         OrderTdkModel orderTindakan,
//         PasienModel pasien,
//         RegModel reg,
//         LayananType layanan,
//         TipeTarifType tipeTarif,
//         TindakanTarifModel tarif,
//         string userId);
//     TindakanTarifModel BuildTindakanTarif(TarifType tarif,
//         NilaiTarifType nilaiTarif, TindakanTarifDto tarifKomp);
//
// }
//     
// public class TindakanFactory : ITindakanFactory
// {
//     private readonly IPpaRepo _ppaRepo;
//     public TindakanFactory(IPpaRepo ppaRepo)
//     {
//         _ppaRepo = ppaRepo;
//     }
//
//     public TindakanModel Create(JenisTindakanEnum jenisTindakan, OrderTdkModel orderTindakan, 
//         PasienModel pasien, RegModel reg, LayananType layanan, TipeTarifType tipeTarif,
//         TindakanTarifModel tarif, string userId)
//     {
//         var auditTrail = AuditTrailType.Create(userId, DateTime.Now);
//         var newId = Ulid.NewUlid().ToString();
//         return new TindakanModel(newId, DateTime.Now, jenisTindakan, auditTrail, orderTindakan.ToReff(),
//             pasien.ToReff(), reg.ToReff(), layanan.ToReff(), tipeTarif.ToReff(), tarif);
//     }
//     public TindakanTarifModel BuildTindakanTarif(TarifType tarif, NilaiTarifType nilaiTarif, TindakanTarifDto tarifKomp)
//     {
//         var tindakanTarif = new TindakanTarifModel(tarif, []);
//         tarifKomp.listKomponen
//             .Select(d =>
//             {
//                 var kompo = nilaiTarif.ListKomponen
//                     .FirstOrDefault(x => x.Komponen.KomponenId == d.KomponenId)
//                     ?? NilaiTarifKomponenType.Default;
//
//                 var komponen = new KomponenType(
//                     kompo.Komponen.KomponenId, kompo.Komponen.KomponenName,
//                     GroupKomponenType.Default, []);
//
//                 var ppa = string.IsNullOrWhiteSpace(d.PpaId)
//                     ? PpaType.Default
//                     : GetPpa(PpaType.Key(d.PpaId));
//
//                 return new
//                 {
//                     Komponen = komponen,
//                     Ppa = ppa,
//                     Qty = d.qty,
//                     Nilai = kompo.Nilai
//                 };
//             })
//             .ToList()
//             .ForEach(x =>
//             {
//                 tindakanTarif.SetKomponen(x.Komponen, x.Ppa, x.Qty, x.Nilai);
//             });
//
//         return tindakanTarif;
//     }
//     private PpaType GetPpa(IPpaKey ppakey)
//     {
//         return _ppaRepo.LoadEntity(ppakey)
//             .Match(
//                 onSome: x => x,
//                 onNone: () => PpaType.Default);
//     }
// }
//
//
// public record TindakanTarifDto(TarifReff Tarif, IEnumerable<TindakanTarifKompomnenDto> listKomponen);
//
// public record TindakanTarifKompomnenDto(string KomponenId, string PpaId, int qty);