using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.PasienContext.DemografiSub.KotaAgg;

public interface IKotaWriter: INunaWriterWithReturn<KotaModel>
{
    public void Delete(IKotaKey key);
}

public class KotaWriter : IKotaWriter
{
    private readonly IKotaDal _kotaDal;

    public KotaWriter(IKotaDal kotaDal)
    {
        _kotaDal = kotaDal;
    }

    public KotaModel Save(KotaModel model)
    {
        var kotaDb = _kotaDal.GetData(model);
        if (kotaDb is null)
            _kotaDal.Insert(model);
        else
            _kotaDal.Update(model);
        return model;
    }

    public void Delete(IKotaKey key)
    {
        _kotaDal.Delete(key);
    }
}

public class KotaWriterHandler
{
    private readonly Mock<IKotaDal> _kotaDal;
    private readonly KotaWriter _sut;

    public KotaWriterHandler()
    {
        _kotaDal = new Mock<IKotaDal>();
        _sut = new KotaWriter(_kotaDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate_Test()
    {
        var expected = KotaModel.Create("A", "B");
        _kotaDal.Setup(x => x.GetData(It.IsAny<IKotaKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _kotaDal.Verify(x => x.Update(expected), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = KotaModel.Create("A", "B");
        _kotaDal.Setup(x => x.GetData(It.IsAny<IKotaKey>()))
            .Returns(null as KotaModel);
        _sut.Save(expected);
        _kotaDal.Verify(x => x.Insert(expected), Times.Once);
    }
}