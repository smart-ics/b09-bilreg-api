using Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.JaminanAgg;

public class JaminanDal: IJaminanDal
{
    private readonly DatabaseOptions _opt;

    public JaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(JaminanModel model)
    {
        throw new NotImplementedException();
    }

    public void Update(JaminanModel model)
    {
        throw new NotImplementedException();
    }

    public JaminanModel GetData(IJaminanKey key)
    {
        throw new NotImplementedException();
    }
}

public class JaminanDalTest
{
    private readonly JaminanDal _sut;

    public JaminanDalTest()
    {
        _sut = new JaminanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        
    }
    
    [Fact]
    public void UpdateTest()
    {
        
    }
    
    [Fact]
    public void GetDataTest()
    {
        
    }
}