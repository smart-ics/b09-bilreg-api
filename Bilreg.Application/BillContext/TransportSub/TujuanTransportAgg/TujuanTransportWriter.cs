using Bilreg.Domain.BillContext.TransportSub.TujuanTransportAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.TujuanTransportAgg;

public interface ITujuanTransportWriter : INunaWriterWithReturn<TujuanTransportModel>
{
    public void Delete(ITujuanTransportKey key);
}
public class TujuanTransportWriter: ITujuanTransportWriter
{
    private readonly ITujuanTransportDal _tujuanTransportDal;

    public TujuanTransportWriter(ITujuanTransportDal tujuanTransportDal)
    {
        _tujuanTransportDal = tujuanTransportDal;
    }

    public TujuanTransportModel Save(TujuanTransportModel model)
    {
        var tujuanTransport = _tujuanTransportDal.GetData(model);
        if (tujuanTransport is not null) 
            _tujuanTransportDal.Update(model);
        else
            _tujuanTransportDal.Insert(model);
        return model;
    }

    public void Delete(ITujuanTransportKey key)
    {
        _tujuanTransportDal.Delete(key);
    }
}

public class TujuanTransportWriterTest
{
    private readonly Mock<ITujuanTransportDal> _tujuanTransportDal;
    private readonly TujuanTransportWriter _sut;

    public TujuanTransportWriterTest()
    {
        _tujuanTransportDal = new Mock<ITujuanTransportDal>();
        _sut = new TujuanTransportWriter(_tujuanTransportDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate()
    {
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _tujuanTransportDal.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _tujuanTransportDal.Verify(x => x.Update(It.IsAny<TujuanTransportModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert()
    {
        var expected = new TujuanTransportModel("A", "B", 10, false);
        _tujuanTransportDal.Setup(x => x.GetData(It.IsAny<ITujuanTransportKey>()))
            .Returns(null as TujuanTransportModel);
        _sut.Save(expected);
        _tujuanTransportDal.Verify(x => x.Insert(It.IsAny<TujuanTransportModel>()), Times.Once);
    }
}