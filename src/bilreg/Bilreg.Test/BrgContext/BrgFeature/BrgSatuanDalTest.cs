using System;
using System.Collections.Generic;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BrgContext.BrgFeature;

public class BrgSatuanDalTest
{
    private readonly BrgSatuanDal _sut = new(ConnStringHelper.GetTestEnv());

    private static IEnumerable<BrgSatuanDto> FakerList()
        => new List<BrgSatuanDto>
        {
            new BrgSatuanDto(
                fs_kd_barang: "BRG001",
                fs_kd_satuan: "TAB",
                fn_nilai: 1,
                fs_nm_satuan: "Tablet"
            ),
            new BrgSatuanDto(
                fs_kd_barang: "BRG001",
                fs_kd_satuan: "BOX",
                fn_nilai: 10,
                fs_nm_satuan: "Box"
            )
        };

    private static IBrgKey FakerKey()
        => new BrgKey("BRG001");

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

    [Fact]
    public void Insert_DuplicateKonversi_ThrowsArgumentException()
    {
        var duplicateList = new List<BrgSatuanDto>
        {
            new("BRG001", "TAB", 1, "Tablet"),
            new("BRG001", "STR", 1, "Strip")
        };
        var act = () => _sut.Insert(duplicateList);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*duplikat*");
    }
}