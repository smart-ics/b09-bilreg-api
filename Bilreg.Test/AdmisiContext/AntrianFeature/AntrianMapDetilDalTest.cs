using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapDetilDalTest
{
    private readonly AntrianMapDetilDal _sut = new(ConnStringHelper.GetTestEnv());

    private AntrianMapDetilDto Faker()
    {
        var tgl = new DateTime(2026, 4, 22);
        var antrianMapId = "ABABABA";
        var dto = new AntrianMapDetilDto(
            antrianMapId,
            "DOK001",
            "LYN01",
            tgl.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "08:00",
            1m,
            "N",
            "MR001",
            "Pasien A",
            "TRS001",
            false,
            "-",
            "-"
        );

        return dto;
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var detil = Faker();
        _sut.Insert(detil);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var detil = Faker();
        _sut.Insert(detil);

        _sut.Delete(AntrianMapModel.Key(detil.fs_kd_antrian_map));
    }

    [Fact]
    public void ListDataByKeyTest()
    {
        using var trans = TransHelper.NewScope();
        var detil = Faker();
        _sut.Insert(detil);

        var actual = _sut.ListData(AntrianMapModel.Key(detil.fs_kd_antrian_map));

        actual.Should().BeEquivalentTo(new List<AntrianMapDetilDto> { detil }, opt => opt
            .Excluding(x => x.fs_nm_dokter)
            .Excluding(x => x.fs_nm_layanan)
        );
    }

    [Fact]
    public void ListDataByDateTest()
    {
        using var trans = TransHelper.NewScope();
        var detil = Faker();
        _sut.Insert(detil);

        var date = DateTime.ParseExact(detil.fd_tgl_jadwal, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var actual = _sut.ListData(date);

        actual.Should().ContainEquivalentOf(detil, opt => opt
            .Excluding(x => x.fs_nm_dokter)
            .Excluding(x => x.fs_nm_layanan)
        );
    }
}

