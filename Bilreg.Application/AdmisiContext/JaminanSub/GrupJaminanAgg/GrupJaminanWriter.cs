using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;

public interface IGrupJaminanWriter: INunaWriterWithReturn<GrupJaminanModel>
{
    public void Delete(IGrupJaminanKey key);
}

public class GrupJaminanWriter: IGrupJaminanWriter
{
    private readonly IGrupJaminanDal _grupJaminanDal;

    public GrupJaminanWriter(IGrupJaminanDal grupJaminanDal)
    {
        _grupJaminanDal = grupJaminanDal;
    }

    public GrupJaminanModel Save(GrupJaminanModel model)
    {
        var grupJaminanDb = _grupJaminanDal.GetData(model);
        if (grupJaminanDb is null)
            _grupJaminanDal.Insert(model);
        else
            _grupJaminanDal.Update(model);
        return model;
    }

    public void Delete(IGrupJaminanKey key)
    {
        _grupJaminanDal.Delete(key);
    }
}

public class GrupJaminanWriterTest
{
    private readonly Mock<IGrupJaminanDal> _grupJaminanDal;
    private readonly GrupJaminanWriter _sut;

    public GrupJaminanWriterTest()
    {
        _grupJaminanDal = new Mock<IGrupJaminanDal>();
        _sut = new GrupJaminanWriter(_grupJaminanDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate_Test()
    {
        var expected = GrupJaminanModel.Create("A", "B", "C");
        _grupJaminanDal.Setup(x => x.GetData(It.IsAny<IGrupJaminanKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _grupJaminanDal.Verify(x => x.Update(It.IsAny<GrupJaminanModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = GrupJaminanModel.Create("A", "B", "C");
        _grupJaminanDal.Setup(x => x.GetData(It.IsAny<IGrupJaminanKey>()))
            .Returns(null as GrupJaminanModel);
        _sut.Save(expected);
        _grupJaminanDal.Verify(x => x.Insert(It.IsAny<GrupJaminanModel>()), Times.Once);
    }
}