using Bilreg.Domain.AdmPasienContext.PropinsiAgg;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmPasienContext.PropinsiAgg;

public class PropinsiDto
{
    public string fs_kd_propinsi { get; set; }
    public string fs_nm_propinsi { get; set; }

    public PropinsiModel ToModel()
    {
        return PropinsiModel.Create(fs_kd_propinsi, fs_nm_propinsi);
    }
}

public class PropinsiDalTest
{
    private readonly PropinsiDal _sut;

    public PropinsiDalTest()
    {
        _sut = new PropinsiDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(PropinsiModel.Create("A", "B"));
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(PropinsiModel.Create("A", "B"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(PropinsiModel.Create("A",""));
    }
    
    [Fact]
    public void GetTest()
    {
        using var trans = TransHelper.NewScope();
        var exp = PropinsiModel.Create("A", "B");
        _sut.Insert(exp);
        var actual = _sut.GetData(exp);
        actual.Should().BeEquivalentTo(exp);
    }
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var exp = PropinsiModel.Create("A", "B");
        _sut.Insert(exp);
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(new List<PropinsiModel>(){exp});
    }
}