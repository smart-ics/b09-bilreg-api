//  TODO: jude-dev-trs-billing
// using Ardalis.GuardClauses;
// using Bilreg.Application.AdmisiContext.RegFeature;
// using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
// using Bilreg.Domain.AdmisiContext.RegFeature;
// using Bilreg.Domain.PasienContext.PasienFeature;
// using Bilreg.Domain.PaymentContext.RegOutFeature;
// using Bilreg.Domain.PaymentContext.TrsBillingFeature;
// using MediatR;
//
// namespace Bilreg.Application.PaymentContext.TrsBillingFeature;
//
// public record TrsbListSummaryBillQuery(string RegId) : IRequest<IEnumerable<TrsbSummaryBillResponse>>, IRegKey;
//
// public record TrsbSummaryBillResponse(
//     int No, string Komponen, decimal Jasa, decimal Obat, decimal SubTotal);
//
// public class TrsbListSummaryBillHandler : IRequestHandler<TrsbListSummaryBillQuery, IEnumerable<TrsbSummaryBillResponse>>
// {
//     private readonly IRegAktifRepo _regAktifRepo;
//     private readonly ITrsBillingRepo _trsBillRepo;
//     private readonly IRegHutangRepo _regHutangRepo;
//     private readonly IRegBiayaRepo _regBiayaRepo;
//     public TrsbListSummaryBillHandler(IRegAktifRepo regAktifRepo,
//         ITrsBillingRepo trsBillingRepo,
//         IRegHutangRepo regHutangRepo,
//         IRegBiayaRepo regBiayaRepo)
//     {
//         _regAktifRepo = regAktifRepo;
//         _trsBillRepo = trsBillingRepo;
//         _regHutangRepo = regHutangRepo;
//         _regBiayaRepo = regBiayaRepo;
//     }
//     public Task<IEnumerable<TrsbSummaryBillResponse>> Handle(TrsbListSummaryBillQuery request, CancellationToken cancellationToken)
//     {
//         Guard.Against.NullOrWhiteSpace(request.RegId);
//         // Load Aggregate Root
//         var reg = _regAktifRepo.LoadEntity(request)
//             .GetValueOrThrow($"Register {request.RegId} not active");
//
//         var listHutang = _regHutangRepo.ListData(PasienModel.Key(reg.Pasien.PasienId))?.ToList() ?? [];
//         var listBiaya = _regBiayaRepo.ListData(request)?.ToList() ?? [];
//         var listBill = _trsBillRepo.ListData(request)?.ToList() ?? [];
//
//         var currentRegNo = GetCurrentRegNo(request.RegId);
//         var (sumJasa, sumObat) = CalculateHutangLalu(listHutang, currentRegNo);
//         var (nilaiAdmin, nilaiMaterai) = ExtractBiayaValues(listBiaya);
//         var processedIds = GetProcessedReffBlIds(listBiaya);
//
//         var jasa = FilterUnprocessedJasa(listBill, processedIds);
//         var obat = listBill.Where(x => x.Modul == 1).ToList();
//         
//         var response = BuildResponse(sumJasa, sumObat, jasa, obat, nilaiAdmin, nilaiMaterai);
//         return Task.FromResult<IEnumerable<TrsbSummaryBillResponse>>(response);
//     }
//
//     #region PRIVATE-HELPER
//     private static string GetCurrentRegNo(string regId) =>
//             regId.Length >= 8 ? regId[^8..] : regId;
//
//     private static (decimal Jasa, decimal Obat) CalculateHutangLalu(IEnumerable<RegHutangType> hutang, string currentRegNo)
//     {
//         var filtered = hutang.Where(x =>
//         {
//             var hutangRegNo = GetCurrentRegNo(x.RegId);
//             return hutangRegNo.CompareTo(currentRegNo) < 0;
//         });
//         return (filtered.Sum(x => x.NilaiJasa), filtered.Sum(x => x.NilaiObat));
//     }
//
//     private static (decimal Admin, decimal Materai) ExtractBiayaValues(IEnumerable<RegBiayaType> biaya)
//     {
//         var lookup = biaya.ToDictionary(x => x.KomponenId, x => x.Nilai);
//         return (
//             lookup.TryGetValue("ADM", out var adm) ? adm : 0,
//             lookup.TryGetValue("MTR", out var mtr) ? mtr : 0
//         );
//     }
//
//     private static HashSet<string> GetProcessedReffBlIds(IEnumerable<RegBiayaType> biaya) =>
//         biaya.Where(x => !string.IsNullOrEmpty(x.ReffBlId))
//              .Select(x => x.ReffBlId)
//              .ToHashSet(StringComparer.OrdinalIgnoreCase);
//
//     private static List<TrsBillingType> FilterUnprocessedJasa(IEnumerable<TrsBillingType> bills, HashSet<string> processedIds) =>
//         bills.Where(x => x.Modul == 0 && !processedIds.Contains(x.TrsBillingId)).ToList();
//
//     private static List<TrsbSummaryBillResponse> BuildResponse(decimal sumJasaHutang, decimal sumObatHutang,
//         List<TrsBillingType> jasa, List<TrsBillingType> obat, decimal admin, decimal materai)
//     {
//         static decimal SumJasa(List<TrsBillingType> list, Func<TrsBillingType, decimal> selector) => list.Sum(selector);
//         static decimal SumObat(List<TrsBillingType> list, Func<TrsBillingType, decimal> selector) => list.Sum(selector);
//         static decimal Total(decimal j, decimal o) => j + o;
//
//         return new()
//         {
//             new(1, "Hutang Lalu", sumJasaHutang, sumObatHutang, Total(sumJasaHutang, sumObatHutang)),
//             new(2, "Total Tagihan", SumJasa(jasa, x => x.SubTotal), SumObat(obat, x => x.SubTotal),
//                 Total(SumJasa(jasa, x => x.SubTotal), SumObat(obat, x => x.SubTotal))),
//             new(3, "Total Diskon", SumJasa(jasa, x => x.Diskon), SumObat(obat, x => x.Diskon),
//                 Total(SumJasa(jasa, x => x.Diskon), SumObat(obat, x => x.Diskon))),
//             new(4, "Total Pajak", SumJasa(jasa, x => x.Tax), SumObat(obat, x => x.Tax),
//                 Total(SumJasa(jasa, x => x.Tax), SumObat(obat, x => x.Tax))),
//             new(5, "Total Biaya", SumJasa(jasa, x => x.Biaya), SumObat(obat, x => x.Biaya),
//                 Total(SumJasa(jasa, x => x.Biaya), SumObat(obat, x => x.Biaya))),
//             new(6, "Administrasi", admin, 0, admin),
//             new(7, "Materai", materai, 0, materai)
//         };
//     }
//     #endregion
// }
