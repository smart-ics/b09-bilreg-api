using Bilreg.Domain.AdmPasienContext.SukuAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.SukuAgg
{
    public interface ISukuDal :
        IInsert<SukuModel>,
        IUpdate<SukuModel>,
        IDelete<ISukuKey>,
        IGetData<SukuModel, ISukuKey>,
        IListData<SukuModel>
    {
    }
}
