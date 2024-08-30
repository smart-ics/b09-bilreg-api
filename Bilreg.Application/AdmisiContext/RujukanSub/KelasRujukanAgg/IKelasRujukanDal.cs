using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.RujukanSub.KelasRujukanAgg
{
    public interface IKelasRujukanDal :
        IListData<KelasRujukanModel>,
        IGetData<KelasRujukanModel, IKelasRujukanKey>
    {
    }
}
