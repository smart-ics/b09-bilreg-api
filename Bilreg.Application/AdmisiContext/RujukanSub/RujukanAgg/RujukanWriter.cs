using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Moq;
using Nuna.Lib.CleanArchHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg.RujukanModel;

namespace Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
public interface IRujukanWriter : INunaWriterWithReturn<RujukanModel>
{
}
public class RujukanWriter : IRujukanWriter
{
    private readonly IRujukanDal _rujukanDal;

    public RujukanWriter(IRujukanDal rujukanDal)
    {
        _rujukanDal = rujukanDal;
    }

    public RujukanModel Save(RujukanModel model)
    {
        var existingRujukan = _rujukanDal.GetData(model);
        if (existingRujukan is null)
            _rujukanDal.Insert(model);
        else
            _rujukanDal.Update(model);
        return model;
    }
}

public class RujukanWriterTest
{
    private readonly Mock<IRujukanDal> _rujukanDal;
    private readonly RujukanWriter _sut;

    public RujukanWriterTest()
    {
        _rujukanDal = new Mock<IRujukanDal>();
        _sut = new RujukanWriter(_rujukanDal.Object);
    }

    [Fact]
    public void GivenExistingData_ThenUpdate_Test()
    {
        var expected = RujukanModel.Create("A", "B");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(expected);
        _sut.Save(expected);
        _rujukanDal.Verify(x => x.Update(It.IsAny<RujukanModel>()), Times.Once);
    }

    [Fact]
    public void GivenNonExistingData_ThenInsert_Test()
    {
        var expected = RujukanModel.Create("A", "B");
        _rujukanDal.Setup(x => x.GetData(It.IsAny<IRujukanKey>()))
            .Returns(null as RujukanModel);
        _sut.Save(expected);
        _rujukanDal.Verify(x => x.Insert(It.IsAny<RujukanModel>()), Times.Once);
    }
}

