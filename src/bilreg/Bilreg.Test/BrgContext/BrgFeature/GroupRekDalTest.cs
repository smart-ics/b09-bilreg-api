using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BrgContext.BrgFeature;

public class GroupRekDalTest
{
    private readonly GroupRekDal _sut = new(ConnStringHelper.GetTestEnv());

    private static GroupRekDto Faker()
        => new GroupRekDto("A", "B", "C", "D", "01", "111", "112", "113", "114", "115");

    private static IGroupRekKey FakerKey()
        => GroupRekType.Default with { GroupRekId = "A" };

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt.Excluding(x => x.fs_nm_grup_rek_dk));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.fs_nm_grup_rek_dk));
    }

    [Fact]
    public void ToModel_MapsCoaAndRekapCetakFields()
    {
        var dto = Faker();
        var model = dto.ToModel();

        model.GroupRekId.Should().Be("A");
        model.GroupRekName.Should().Be("B");
        model.GroupRekDk.GroupRekDkId.Should().Be("C");
        model.RekapCetakId.Should().Be("01");
        model.RekJual.CoaId.Should().Be("111");
        model.RekJualRajal.CoaId.Should().Be("112");
        model.RekDisc.CoaId.Should().Be("113");
        model.RekTax.CoaId.Should().Be("114");
        model.RekPersediaan.CoaId.Should().Be("115");

        var roundTrippedDto = GroupRekDto.FromModel(model);
        roundTrippedDto.Should().BeEquivalentTo(dto, opt => opt.Excluding(x => x.fs_nm_grup_rek_dk));
    }
}