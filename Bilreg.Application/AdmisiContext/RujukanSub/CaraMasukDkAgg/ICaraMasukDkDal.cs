using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg
{
    public interface ICaraMasukDkDal :
        IGetData<CaraMasukDkModel, ICaraMasukDkKey>,
        IListData<CaraMasukDkModel>
    {

    }
}
