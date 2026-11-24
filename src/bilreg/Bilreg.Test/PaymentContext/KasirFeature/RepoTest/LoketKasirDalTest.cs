using Bilreg.Infrastructure.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.RepoTest;

public class LoketKasirDalTest
{
    private readonly LoketKasirDal _sut;

    public LoketKasirDalTest()
    {
        _sut = new LoketKasirDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void ListAktif_WhenCalled_ReturnsActiveLoketKasir()
    {
        // Act
        var result = _sut.ListAktif().ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(x => x.IsAktif);
        result.Select(x => x.LoketId).Should().Contain(["LOK-INAP", "LOK-IGD", "LOK-POLI"]);

        var inap = result.First(x => x.LoketId == "LOK-INAP");
        inap.NamaLoket.Should().Be("Loket Kasir Rawat Inap");
        inap.CoaId.Should().Be("1110201");

        var igd = result.First(x => x.LoketId == "LOK-IGD");
        igd.NamaLoket.Should().Be("Loket Kasir IGD & Penunjang");
        igd.CoaId.Should().Be("1110202");

        var poli = result.First(x => x.LoketId == "LOK-POLI");
        poli.NamaLoket.Should().Be("Loket Kasir Poliklinik");
        poli.CoaId.Should().Be("1110203");
    }

    [Fact]
    public void ListData_WhenCalled_ReturnsAllLoketKasir()
    {
        // Act
        var result = _sut.ListData().ToList();

        // Assert
        result.Should().NotBeEmpty();
        result.Select(x => x.LoketId).Should().Contain(["LOK-INAP", "LOK-IGD", "LOK-POLI"]);
    }

    [Fact]
    public void GetData_WhenFound_ReturnsMatchingLoket()
    {
        // Act
        var result = _sut.GetData("LOK-INAP");

        // Assert
        result.Should().NotBeNull();
        result!.LoketId.Should().Be("LOK-INAP");
        result.NamaLoket.Should().Be("Loket Kasir Rawat Inap");
        result.CoaId.Should().Be("1110201");
        result.NamaCoa.Should().Be("KAS KASIR RAWAT INAP");
        result.IsAktif.Should().BeTrue();
    }

    [Fact]
    public void GetData_WhenNotFound_ReturnsNull()
    {
        // Act
        var result = _sut.GetData("NONEXISTENT");

        // Assert
        result.Should().BeNull();
    }
}
