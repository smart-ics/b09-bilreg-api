using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class LokasiBerkasDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly LokasiBerkasDal _sut;

    public LokasiBerkasDalTest()
    {
        _sut = new LokasiBerkasDal(_opt);
    }

    [Fact]
    public void UT1_InsertAndGetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = LokasiBerkasDto.Create("LK01", "Rak Filing A Lantai 1", "POL01");
        _sut.Insert(dto);

        var actual = _sut.GetData("LK01");
        actual.Should().NotBeNull();
        actual!.FS_KD_LOKASI_BERKAS.Should().Be("LK01");
        actual.FS_NM_LOKASI_BERKAS.Should().Be("Rak Filing A Lantai 1");
        actual.FS_KD_LAYANAN.Should().Be("POL01");
        actual.LokasiBerkasId.Should().Be("LK01");
        actual.LokasiBerkasName.Should().Be("Rak Filing A Lantai 1");
    }

    [Fact]
    public void UT2_ListAllTest()
    {
        using var trans = TransHelper.NewScope();
        var dto1 = LokasiBerkasDto.Create("LK01", "Rak A", "POL01");
        var dto2 = LokasiBerkasDto.Create("LK02", "Rak B", "POL02");
        _sut.Insert(dto1);
        _sut.Insert(dto2);

        var list = _sut.ListAll().ToList();
        list.Should().Contain(x => x.FS_KD_LOKASI_BERKAS == "LK01");
        list.Should().Contain(x => x.FS_KD_LOKASI_BERKAS == "LK02");
    }

    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var dto = LokasiBerkasDto.Create("LK01", "Rak A", "POL01");
        _sut.Insert(dto);

        _sut.Delete("LK01");

        var actual = _sut.GetData("LK01");
        actual.Should().BeNull();
    }
}
