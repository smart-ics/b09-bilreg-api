using Bilreg.Domain.AdmPasienContext.PekerjaanAgg;
using Bilreg.Domain.AdmPasienContext.SukuAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.PekerjaanAgg
{
    public interface IPekerjaanDal :
        IInsert<PekerjaanModel>,
        IUpdate<PekerjaanModel>,
        IDelete<IPekerjaanKey>,
        IGetData<PekerjaanModel, IPekerjaanKey>,
        IListData<PekerjaanModel>
    {
    }
}
