using Bilreg.Domain.AdmPasienContext.KabupatenAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.KabupatenAgg;

public interface IKabupatenWriter : INunaWriterWithReturn<KabupatenModel>
{
    void Delete(IKabupatenKey key);
}
public class KabupatenWriter : IKabupatenWriter
{
    private readonly IKabupatenDal _kabupatenDal;

    public KabupatenWriter(IKabupatenDal kabupatenDal)
    {
        _kabupatenDal = kabupatenDal;
    }

    public KabupatenModel Save(KabupatenModel model)
    {
        var kabupatenDb = _kabupatenDal.GetData(model);
        if (kabupatenDb is null)
            _kabupatenDal.Insert(model);
        else
            _kabupatenDal.Update(model);
        return model;
    }

    public void Delete(IKabupatenKey key)
    {
        _kabupatenDal.Delete(key);
    }
}

public class KabupateWriterTest
{
    private readonly KabupatenWriter _sut;
    private readonly Mock<IKabupatenDal> _kabupatenDal;

    public KabupateWriterTest()
    {
        _kabupatenDal = new Mock<IKabupatenDal>();
        _sut = new KabupatenWriter(_kabupatenDal.Object);
    }

    [Fact]
    public void GivenExisitingData_ThenUpdate()
    {
        _kabupatenDal.Setup(x => x.GetData(It.IsAny<KabupatenModel>()))
            .Returns(KabupatenModel.Create("A","B"));
        var act = _sut.Save(KabupatenModel.Create("A","B"));
        _kabupatenDal.Verify(x => x.Update(It.IsAny<KabupatenModel>()), Times.Once);
    }
    [Fact]
    public void GivenNotExistData_ThenInsert()
    {
        _kabupatenDal.Setup(x => x.GetData(It.IsAny<KabupatenModel>()))
            .Returns(null as KabupatenModel);
        var act = _sut.Save(KabupatenModel.Create("A","B"));
        _kabupatenDal.Verify(x => x.Insert(It.IsAny<KabupatenModel>()), Times.Once);
    }
    
}