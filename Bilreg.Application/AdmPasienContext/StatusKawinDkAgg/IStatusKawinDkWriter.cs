using Bilreg.Application.AdmPasienContext.StatusKawinAgg;
using Bilreg.Domain.AdmPasienContext.StatusKawinDkAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmPasienContext.StatusKawinDkAgg;

public interface IStatusKawinDkWriter : INunaWriterWithReturn<StatusKawinDkModel>
{
    void Delete(IStatusKawinDkKey statuskawinDkKey);
}
public class StatusKawinDkWriter : IStatusKawinDkWriter
{
    private readonly IStatusKawinDkDal _statuskawinDkDal;

    public StatusKawinDkWriter(IStatusKawinDkDal statusKawinDkDal)
    {
        _statuskawinDkDal = statusKawinDkDal;
    }

    public StatusKawinDkModel Save(StatusKawinDkModel model)
    {
        var statuskawinDkDb = _statuskawinDkDal.GetData(model);
        if (statuskawinDkDb is null)
        {
            _statuskawinDkDal.Insert(model);
        }
        else
        {
            _statuskawinDkDal.Update(model);
        }
        return model;
    }

    public void Delete(IStatusKawinDkKey statuskawinDkKey)
    {
        _statuskawinDkDal.Delete(statuskawinDkKey);
    }

}