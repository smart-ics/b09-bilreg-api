using Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KelurahanAgg;

public interface IKelurahanWriter: INunaWriterWithReturn<KelurahanModel>
{
    public void Delete(IKelurahanKey key);
}

public class KelurahanWriter: IKelurahanWriter
{
    private readonly IKelurahanDal _kelurahanDal;

    public KelurahanWriter(IKelurahanDal kelurahanDal)
    {
        _kelurahanDal = kelurahanDal;
    }

    public KelurahanModel Save(KelurahanModel model)
    {
        var kelurahanDb = _kelurahanDal.GetData(model);
        if (kelurahanDb is null)
            _kelurahanDal.Insert(model);
        else
            _kelurahanDal.Update(model);
        return model;
    }

    public void Delete(IKelurahanKey key)
    {
        _kelurahanDal.Delete(key);
    }
}

public class KelurahanWriterTest
{
    private readonly Mock<IKelurahanDal> _kelurahanDal;
    private readonly KelurahanWriter _sut;

    public KelurahanWriterTest()
    {
        _kelurahanDal = new Mock<IKelurahanDal>();
        _sut = new KelurahanWriter(_kelurahanDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate_Test()
    {
        var expected = KelurahanModel.Create("A", "B", "C");
        _kelurahanDal.Setup(x => x.GetData(It.IsAny<IKelurahanKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _kelurahanDal.Verify(x => x.Update(It.IsAny<KelurahanModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = KelurahanModel.Create("A", "B", "C");
        _kelurahanDal.Setup(x => x.GetData(It.IsAny<IKelurahanKey>()))
            .Returns(null as KelurahanModel);
        _sut.Save(expected);
        _kelurahanDal.Verify(x => x.Insert(It.IsAny<KelurahanModel>()), Times.Once);
    }
}