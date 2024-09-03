using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Nuna.Lib.CleanArchHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public interface IPetugasMedisWriter : INunaWriterWithReturn<PetugasMedisModel>
{
}
public class PetugasMedisWriter : IPetugasMedisWriter
{
    private readonly IPetugasMedisDal _petugasMedisDal;
    private readonly IPetugasMedisLayananDal _petugasMedisLayananDal;
    private readonly IPetugasMedisSatTugasDal _petugasMedisSatTugasDal;

    public PetugasMedisWriter(IPetugasMedisDal petugasMedisDal, 
        IPetugasMedisLayananDal petugasMedisLayananDal, 
        IPetugasMedisSatTugasDal petugasMedisSatTugasDal)
    {
        _petugasMedisDal = petugasMedisDal;
        _petugasMedisLayananDal = petugasMedisLayananDal;
        _petugasMedisSatTugasDal = petugasMedisSatTugasDal;
    }

    public PetugasMedisModel Save(PetugasMedisModel model)
    {
        model.SyncId();

        var petugasMedisDb = _petugasMedisDal.GetData(model);
        if (petugasMedisDb is null)
            _petugasMedisDal.Insert(model);
        else
            _petugasMedisDal.Update(model);

        _petugasMedisLayananDal.Delete(model);
        _petugasMedisSatTugasDal.Delete(model);

        _petugasMedisLayananDal.Insert(model.ListLayanan);
        _petugasMedisSatTugasDal.Insert(model.ListSatTugas);

        return model;
    }
}