using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
