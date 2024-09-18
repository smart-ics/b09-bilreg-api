using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.BillContext.TindakanSub.KomponenTarifAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;

public interface IKomponenTarifWriter: INunaWriterWithReturn<KomponenModel>
{
    public void Delete(IKomponenKey key);
}

public class KomponenTarifWriter: IKomponenTarifWriter
{
    private readonly IKomponenTarifDal _komponenTarifDal;

    public KomponenTarifWriter(IKomponenTarifDal komponenTarifDal)
    {
        _komponenTarifDal = komponenTarifDal;
    }

    public KomponenModel Save(KomponenModel model)
    {
        var komponenTarifDb = _komponenTarifDal.GetData(model);
        if (komponenTarifDb is null)
            _komponenTarifDal.Insert(model);
        else 
            _komponenTarifDal.Update(model);
        return model;
    }

    public void Delete(IKomponenKey key)
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
        var expected = new KomponenModel("A", "B");
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _komponenTarifDal.Verify(x => x.Update(It.IsAny<KomponenModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = new KomponenModel("A", "B");
        _komponenTarifDal.Setup(x => x.GetData(It.IsAny<IKomponenKey>()))
            .Returns(null as KomponenModel);
        _sut.Save(expected);
        _komponenTarifDal.Verify(x => x.Insert(It.IsAny<KomponenModel>()), Times.Once);
    }
}