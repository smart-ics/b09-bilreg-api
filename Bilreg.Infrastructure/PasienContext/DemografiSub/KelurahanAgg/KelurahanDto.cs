// using Bilreg.Domain.PasienContext.DemografiSub.KabupatenAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
// using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
//
// namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KelurahanAgg;
//
// public class KelurahanDto
// {
//     public string KelurahanId { get; set; }
//     
//     public string KelurahanName { get; set; }
//     
//     public string KodePos { get; set; }
//     
//     public string KecamatanId { get; set; }
//     
//     public string KecamatanName { get; set; }
//     
//     public string KabupatenId { get; set; }
//     
//     public string KabupatenName { get; set; }
//     
//     public string PropinsiId { get; set; }
//     
//     public string PropinsiName { get; set; }
//
//     public KelurahanModel ToModel()
//     {
//         var propinsi = new PropinsiType(PropinsiId, PropinsiName);
//         var kabupaten = new KabupatenType(KabupatenId, KabupatenName, propinsi);
//         var kecamatan = new KecamatanType(KecamatanId, KecamatanName, kabupaten);
//         var kelurahan = new KelurahanModel(KelurahanId, KelurahanName, KodePos, kecamatan);
//         return kelurahan;
//     }
// }