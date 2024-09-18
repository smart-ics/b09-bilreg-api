using Bilreg.Domain.BillContext.TindakanSub.GrupTarifAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.GrupTarifAgg;

public interface IGrupTarifWriter: INunaWriterWithReturn<GrupTarifModel>
{
    public void Delete(IGrupTarifKey key);
}

public class GrupTarifWriter: IGrupTarifWriter
{
    private readonly IGrupTarifDal _grupTarifDal;

    public GrupTarifWriter(IGrupTarifDal grupTarifDal)
    {
        _grupTarifDal = grupTarifDal;
    }

    public GrupTarifModel Save(GrupTarifModel model)
    {
        var grupTarifDb = _grupTarifDal.GetData(model);
        if (grupTarifDb is null)
            _grupTarifDal.Insert(model);
        else 
            _grupTarifDal.Update(model);
        return model;
    }

    public void Delete(IGrupTarifKey key)
    {
        _grupTarifDal.Delete(key);
    }
}

public class GrupTarifWriterTest
{
    private readonly Mock<IGrupTarifDal> _grupTarifDal;
    private readonly GrupTarifWriter _sut;

    public GrupTarifWriterTest()
    {
        _grupTarifDal = new Mock<IGrupTarifDal>();
        _sut = new GrupTarifWriter(_grupTarifDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate()
    {
        var expected = new GrupTarifModel("A", "B");
        _grupTarifDal.Setup(x => x.GetData(It.IsAny<IGrupTarifKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _grupTarifDal.Verify(x => x.Update(It.IsAny<GrupTarifModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert()
    {
        var expected = new GrupTarifModel("A", "B");
        _grupTarifDal.Setup(x => x.GetData(It.IsAny<IGrupTarifKey>()))
            .Returns(null as GrupTarifModel);
        _sut.Save(expected);
        _grupTarifDal.Verify(x => x.Insert(It.IsAny<GrupTarifModel>()), Times.Once);
    }
}