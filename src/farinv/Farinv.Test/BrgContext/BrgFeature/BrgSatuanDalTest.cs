using Farinv.Domain.BrgContext.BrgFeature;
using Farinv.Infrastructure.BrgContext.BrgFeature;
using Farinv.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Test.BrgContext.BrgFeature;

public class BrgSatuanDalTest
{
    private readonly BrgSatuanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<BrgSatuanDto> FakerList()
        => new List<BrgSatuanDto>
        {
            new BrgSatuanDto(
                fs_kd_barang: "A",
                fs_kd_satuan: "B",
                fn_nilai: 1,
                fs_nm_satuan: "C"
            ),
            new BrgSatuanDto(
                fs_kd_barang: "A",
                fs_kd_satuan: "D",
                fn_nilai: 2,
                fs_nm_satuan: "E"
            )
        };

    private static IBrgKey FakerKey()
        => BrgObatType.Key("A");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
        var actual = _sut.ListData(FakerKey());
        actual.Should().BeEquivalentTo(FakerList(),
            opt => opt.Excluding(x => x.fs_nm_satuan));
    }
}