using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public interface IKotaWriter: INunaWriterWithReturn<KotaModel>
{
    public void Delete(IKotaKey key);
}

public class KotaWriter : IKotaWriter
{
    private readonly IKotaDal _kotaDal;

    public KotaWriter(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }

    public KotaModel Save(KotaModel model)
    {
        var kotaDb = _kotaDal.GetData(model);
        if (kotaDb is null)
            _kotaDal.Insert(model);
        else
            _kotaDal.Update(model);
        return model;
    }

    public void Delete(IKotaKey key)
    {
        _kotaDal.Delete(key);
    }
}
