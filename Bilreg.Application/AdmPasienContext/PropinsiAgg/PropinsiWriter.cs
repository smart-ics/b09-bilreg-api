using Bilreg.Domain.AdmPasienContext.PropinsiAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.AdmPasienContext.PropinsiAgg;

public interface IPropinsiWriter : INunaWriterWithReturn<PropinsiModel>
{
    void Delete(IPropinsiKey propinsiKey);
}
public class PropinsiWriter : IPropinsiWriter
{
    private readonly IPropinsiDal _PropinsiDal;

    public PropinsiWriter(IPropinsiDal PropinsiDal)
    {
        _PropinsiDal = PropinsiDal;
    }

    public PropinsiModel Save(PropinsiModel model)
    {
        var propinsiDb = _PropinsiDal.GetData(model);
        if (propinsiDb is null)
            _PropinsiDal.Insert(model);
        else
            _PropinsiDal.Update(model);
        return model;
    }

    public void Delete(IPropinsiKey PropinsiKey)
    {
        _PropinsiDal.Delete(PropinsiKey);
    }
}

public class PropinsiWriterTest
{
    private readonly PropinsiWriter _sut;
    private readonly Mock<IPropinsiDal> _propinsiDal;

    public PropinsiWriterTest()
    {
        _propinsiDal = new Mock<IPropinsiDal>();
        _sut = new PropinsiWriter(_propinsiDal.Object);
    }

    [Fact]
    public void GivenNotExistData_ThenInsert()
    {
        _propinsiDal.Setup(x => x.GetData(It.IsAny<IPropinsiKey>()))
            .Returns(null as PropinsiModel);
        _sut.Save(PropinsiModel.Create("A", "B"));
        _propinsiDal.Verify(x => x.Insert(It.IsAny<PropinsiModel>()), Times.Once);
    }

    [Fact]
    public void GivenExistData_ThenUpdate()
    {
        _propinsiDal.Setup(x => x.GetData(It.IsAny<IPropinsiKey>()))
            .Returns(PropinsiModel.Create("A", "B"));
        _sut.Save(PropinsiModel.Create("A", "B"));
        _propinsiDal.Verify(x => x.Update(It.IsAny<PropinsiModel>()), Times.Once);
        
    }
}