using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RoomRateFeature
{
    public class TipeKamarRepo : ITipeKamarRepo
    {
        private readonly ITipeKamarDal _tipeKamarDal;

        public TipeKamarRepo(ITipeKamarDal tipeKamarDal)
        {
            _tipeKamarDal = tipeKamarDal;
        }

        public void DeleteEntity(ITipeKamarKey key)
        {
            _tipeKamarDal.Delete(key);
        }

        public IEnumerable<TipeKamarType> ListData()
        {
            var listDto = _tipeKamarDal.ListData()?.ToList() ?? [];
            var result = listDto.Select(x => x.ToModel());
            return result;
        }

        public MayBe<TipeKamarType> LoadEntity(ITipeKamarKey key)
        {
            var result = _tipeKamarDal.GetData(key);
            var model = result?.ToModel();
            return MayBe.From(model!);
        }

        public void SaveChanges(TipeKamarType model)
        {
            LoadEntity(model)
                .Match(
                    onSome: _ => _tipeKamarDal.Update(TipeKamarDto.FromModel(model)),
                    onNone: () => _tipeKamarDal.Insert(TipeKamarDto.FromModel(model))
                );
        }
    }
}
