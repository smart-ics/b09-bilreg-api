using Bilreg.Domain.PasienContext.DemografiSub.KecamatanAgg;
using FluentAssertions;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KecamatanAgg;

public interface IKecamatanWriter: INunaWriterWithReturn<KecamatanModel>
{
    public void Delete(IKecamatanKey key);
}

public class KecamatanWriter : IKecamatanWriter
{
    private readonly IKecamatanDal _kecamatanDal;

    public KecamatanWriter(IKecamatanDal kecamatanDal)
    {
        _kecamatanDal = kecamatanDal;
    }

    public KecamatanModel Save(KecamatanModel model)
    {
        var kecamatanDb = _kecamatanDal.GetData(model);
        if (kecamatanDb is null)
            _kecamatanDal.Insert(model);
        else
            _kecamatanDal.Update(model);
        return model;
    }

    public void Delete(IKecamatanKey key)
    {
        _kecamatanDal.Delete(key);
    }
}

public class KecamatanWriterTest
{
    private readonly Mock<IKecamatanDal> _kecamatanDal;
    private readonly KecamatanWriter _sut;
    
    public KecamatanWriterTest()
    {
        _kecamatanDal = new Mock<IKecamatanDal>();
        _sut = new KecamatanWriter(_kecamatanDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate_Test()
    {
        var expected = KecamatanModel.Create("A", "B");
        _kecamatanDal.Setup(x => x.GetData(It.IsAny<IKecamatanKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _kecamatanDal.Verify(x => x.Update(It.IsAny<KecamatanModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = KecamatanModel.Create("A", "B");
        _kecamatanDal.Setup(x => x.GetData(It.IsAny<IKecamatanKey>()))
            .Returns(null as KecamatanModel);
        _sut.Save(expected);
        _kecamatanDal.Verify(x => x.Insert(It.IsAny<KecamatanModel>()), Times.Once);
    }
}