using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg.RujukanModel;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public interface IRujukanWriter : INunaWriterWithReturn<RujukanModel>
{
}
public class RujukanWriter : IRujukanWriter
{
    private readonly IRujukanDal _rujukanDal;

    public RujukanWriter(IRujukanDal rujukanDal)
    {
        _rujukanDal = rujukanDal;
    }

    public RujukanModel Save(RujukanModel model)
    {
        var existingRujukan = _rujukanDal.GetData(model);
        if (existingRujukan is null)
            _rujukanDal.Insert(model);
        else
            _rujukanDal.Update(model);
        return model;
    }
}
