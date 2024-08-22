using Bilreg.Domain.AdmPasienContext.PekerjaanDkAgg;
using Bilreg.Domain.AdmPasienContext.SukuAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmPasienContext.PekerjaanDkAgg
{
    public interface IPekerjaanDkDal :
        IInsert<PekerjaanDkModel>,
        IUpdate<PekerjaanDkModel>,
        IDelete<IPekerjaanDkKey>,
        IGetData<PekerjaanDkModel, IPekerjaanDkKey>,
        IListData<PekerjaanDkModel>
    {
    }
}
