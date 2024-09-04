using Bilreg.Domain.BillContext.RekapCetakSub.GrupRekapCetakAgg;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bilreg.Application.BillContext.RekapCetakSub.GrupRekapCetakAgg;
public interface IGrupRekapCetakWriter : INunaWriterWithReturn<GrupRekapCetakModel>
{
    public void Delete(IGrupRekapCetakKey key);
}
public class GrupRekapCetakWriter : IGrupRekapCetakWriter
{
    private readonly IGrupRekapCetakDal _grupRekapCetakDal;
    public GrupRekapCetakWriter(IGrupRekapCetakDal grupRekapCetakDal)
    {
        _grupRekapCetakDal = grupRekapCetakDal;
    }

    public GrupRekapCetakModel Save(GrupRekapCetakModel model)
    {
        var grupRekapCetakDB = _grupRekapCetakDal.GetData(model);
        if (grupRekapCetakDB is null)
            _grupRekapCetakDal.Insert(model);
        else
            _grupRekapCetakDal.Update(model);
        return model;
    }

    public void Delete(IGrupRekapCetakKey key)
    {
        _grupRekapCetakDal.Delete(key);
    }
}

