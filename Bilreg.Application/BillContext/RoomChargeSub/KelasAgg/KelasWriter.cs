using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.BillContext.RoomChargeSub.KelasAgg
{
    public interface IKelasWriter : INunaWriterWithReturn<KelasModel>
    {

    }
    public class KelasWriter : IKelasWriter
    {
        private readonly IKelasDal _kelasDal;
        public KelasWriter(IKelasDal kelasDal)
        {
            _kelasDal = kelasDal;
        }

        public KelasModel Save(KelasModel model)
        {
            var kelasDb = _kelasDal.GetData(model);
            if (kelasDb == null)
                _kelasDal.Insert(model);
            else
                _kelasDal.Update(model);
            return model;
        }

    }
}
