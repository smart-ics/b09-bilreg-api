// using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
// using Nuna.Lib.CleanArchHelper;
//
// namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
//
// public class PetugasMedisFactory : AggFactory<PetugasMedisModel, IPetugasMedisKey>
// {
//     private readonly IPetugasMedisDal _petugasMedisDal;
//     private readonly IPetugasMedisLayananDal _petugasMedisLayananDal;
//     private readonly IPetugasMedisSatTugasDal _petugasMedisSatTugasDal;
//
//     public PetugasMedisFactory(IPetugasMedisDal petugasMedisDal, 
//         IPetugasMedisLayananDal petugasMedisLayananDal, 
//         IPetugasMedisSatTugasDal petugasMedisSatTugasDal)
//     {
//         _petugasMedisDal = petugasMedisDal;
//         _petugasMedisLayananDal = petugasMedisLayananDal;
//         _petugasMedisSatTugasDal = petugasMedisSatTugasDal;
//     }
//
//     protected override PetugasMedisModel LoadAggregate(IPetugasMedisKey key)
//     {
//         var petugasMedis = _petugasMedisDal.GetData(key)
//             ?? throw new KeyNotFoundException($"{key.PetugasMedisId} not found");
//
//         var listLayanan = _petugasMedisLayananDal.ListData(key)
//             ?? new List<PetugasMedisLayananType>();
//         petugasMedis.Attach(listLayanan);
//
//         var listSatTugas = _petugasMedisSatTugasDal.ListData(key)
//             ?? new List<PetugasMedisSatTugasType>();
//         petugasMedis.Attach(listSatTugas);
//
//         return petugasMedis;
//     }
// }