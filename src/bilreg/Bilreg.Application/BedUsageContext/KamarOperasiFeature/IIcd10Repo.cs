using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature
{
    public interface IIcd10Repo :
        ISaveChange<Icd10Type>,
        ILoadEntity<Icd10Type, IIcd10Key>
    {
    }
}
