using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg
{
    public interface ITipeJaminanWriter : INunaWriterWithReturn<TipeJaminanModel>
    {
    }

    public class TipeJaminanWriter : ITipeJaminanWriter
    {
        private readonly ITipeJaminanDal _tipeJaminanDal;

        public TipeJaminanWriter(ITipeJaminanDal tipeJaminanDal)
        {
            _tipeJaminanDal = tipeJaminanDal;
        }

        public TipeJaminanModel Save(TipeJaminanModel model)
        {
            var tipeJaminanDb = _tipeJaminanDal.GetData(model);
            if (tipeJaminanDb is null)
                _tipeJaminanDal.Insert(model);
            else
                _tipeJaminanDal.Update(model);

            return model;
        }
    }

}
