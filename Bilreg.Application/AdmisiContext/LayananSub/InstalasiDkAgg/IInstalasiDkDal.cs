﻿using Bilreg.Domain.AdmisiContext.LayananSub.InstalasiDkAgg;
using Nuna.Lib.DataAccessHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.AdmisiContext.LayananSub.InstalasiDkAgg
{
    public interface IInstalasiDkDal :
        IGetData<InstalasiDkModel, IInstalasiDkKey>,
        IListData<InstalasiDkModel>
    {
    }
}
