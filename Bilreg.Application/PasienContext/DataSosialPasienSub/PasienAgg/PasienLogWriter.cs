using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public interface IPasienLogWriter: INunaWriterWithReturn<PasienLogModel>
{
}

public class PasienLogWriter: IPasienLogWriter
{
    private readonly IPasienLogDal _pasienLogDal;

    public PasienLogWriter(IPasienLogDal pasienLogDal)
    {
        _pasienLogDal = pasienLogDal;
    }

    public PasienLogModel Save(PasienLogModel model)
    {
        if (model.ChangeLog != string.Empty)
            _pasienLogDal.Insert(model);
        return model;
    }
}