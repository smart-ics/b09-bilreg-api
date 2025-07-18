// using Nuna.Lib.CleanArchHelper;
//
// namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg
// {
//     public interface IGrupKomponenWriter : INunaWriterWithReturn<GrupKomponenType>
//     {
//         public void Delete(IGrupKomponenKey key);
//     }
//     public class GrupKomponenWriter : IGrupKomponenWriter
//     {
//         private readonly IGrupKomponenDal _grupKomponenDal;
//         public GrupKomponenWriter(IGrupKomponenDal grupKomponenDal)
//         {
//             _grupKomponenDal = grupKomponenDal;
//         }
//         public GrupKomponenType Save(GrupKomponenType type)
//         {
//             var grupKomponenDb = _grupKomponenDal.GetData(type);
//             if (grupKomponenDb is null)
//                 _grupKomponenDal.Insert(type);
//             else
//                 _grupKomponenDal.Update(type);
//             return type;
//         }
//         public void Delete(IGrupKomponenKey key)
//         {
//             _grupKomponenDal.Delete(key);
//         }
//     }
// }