using Bilreg.Domain.AdmPasienContext.PendidikanDkAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmPasienContext.PendidikanDkAgg;

public interface IPendidikanDkWriter : INunaWriterWithReturn<PendidikanDkModel>
{
    void Delete(IPendidikanDkKey pendidikanDkKey);
}

public class PendidikanDkWriter : IPendidikanDkWriter
{
    private readonly IPendidikanDkDal _pendidikanDkDal;

    public PendidikanDkWriter(IPendidikanDkDal pendidikanDkDal)
    {
        _pendidikanDkDal = pendidikanDkDal;
    }

    public PendidikanDkModel Save(PendidikanDkModel model)
    {
        var pendidikanDkDb = _pendidikanDkDal.GetData(model);
        if (pendidikanDkDb is null)
            _pendidikanDkDal.Insert(model);
        else
            _pendidikanDkDal.Update(model);

        return model;
    }

    public void Delete(IPendidikanDkKey pendidikanDkKey)
    {
        _pendidikanDkDal.Delete(pendidikanDkKey);
    }
}