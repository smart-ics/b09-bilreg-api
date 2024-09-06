using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public interface IKomponenTarifWriter: INunaWriterWithReturn<KomponenTarifModel>
{
    public void Delete(IKomponenTarifKey key);
}

public class KomponenTarifWriter: IKomponenTarifWriter
{
    private readonly IKomponenTarifDal _komponenTarifDal;

    public KomponenTarifWriter(IKomponenTarifDal komponenTarifDal)
    {
        _komponenTarifDal = komponenTarifDal;
    }

    public KomponenTarifModel Save(KomponenTarifModel model)
    {
        var komponenTarifDb = _komponenTarifDal.GetData(model);
        if (komponenTarifDb is null)
            _komponenTarifDal.Insert(model);
        else 
            _komponenTarifDal.Update(model);
        return model;
    }

    public void Delete(IKomponenTarifKey key)
    {
        _komponenTarifDal.Delete(key);
    }
}

public class KomponenTarifWriterTest
{
    private readonly Mock<IKomponenTarifDal> _komponenTarifDal;
    private readonly KomponenTarifWriter _sut;

    public KomponenTarifWriterTest()
    {
        _komponenTarifDal = new Mock<IKomponenTarifDal>();
        _sut = new KomponenTarifWriter(_komponenTarifDal.Object);
    }
    
    [Fact]
    public void GivenExistingData_ThenUpdate_Test()
    {
        var expected = new KomponenTarifModel("A", "B");
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenTarifKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _komponenTarifDal.Verify(x => x.Update(It.IsAny<KomponenTarifModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = new KomponenTarifModel("A", "B");
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenTarifKey>()))
            .Returns(null as KomponenTarifModel);
        _sut.Save(expected);
        _komponenTarifDal.Verify(x => x.Insert(It.IsAny<KomponenTarifModel>()), Times.Once);
    }
}