using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using Xunit;

namespace Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;

public interface IJaminanWriter: INunaWriterWithReturn<JaminanModel>
{
}

public class JaminanWriter: IJaminanWriter
{
    private readonly IJaminanDal _jaminanDal;

    public JaminanWriter(IJaminanDal jaminanDal)
    {
        _jaminanDal = jaminanDal;
    }

    public JaminanModel Save(JaminanModel model)
    {
        var existingJaminan = _jaminanDal.GetData(model);
        if (existingJaminan is null)
            _jaminanDal.Insert(model);
        else 
            _jaminanDal.Update(model);
        return model;
    }
}

public class JaminanWriterTest
{
    private readonly Mock<IJaminanDal> _jaminanDal;
    private readonly JaminanWriter _sut;

    public JaminanWriterTest()
    {
        _jaminanDal = new Mock<IJaminanDal>();
        _sut = new JaminanWriter(_jaminanDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate_Test()
    {
        var expected = JaminanModel.Create("A", "B");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _jaminanDal.Verify(x => x.Update(It.IsAny<JaminanModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = JaminanModel.Create("A", "B");
        _jaminanDal.Setup(x => x.GetData(It.IsAny<IJaminanKey>()))
            .Returns(null as JaminanModel);
        _sut.Save(expected);
        _jaminanDal.Verify(x => x.Insert(It.IsAny<JaminanModel>()), Times.Once);
    }
}