using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Param;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ApotekContext.Shared;

public class CollectionWindowDaysProviderTest
{
    [Fact]
    public void GetDays_WhenParameterMissing_ReturnsDefaultSeven()
    {
        var dal = new Mock<IParamSistemDal>();
        dal.Setup(x => x.GetData(ApotekSystemParameters.CollectionWindowDaysKey))
            .Throws(new InvalidOperationException("not found"));

        var days = new CollectionWindowDaysProvider(dal.Object).GetDays();
        days.Should().Be(7);
        days.Should().Be(ApotekSystemParameters.DefaultCollectionWindowDays);
    }

    [Fact]
    public void Pharmacy_and_dtu_layanan_ids_are_distinct()
    {
        ApotekLocationIds.PharmacyUnitLayananId.Should().Be("LYAPT");
        ApotekLocationIds.DispensingTemporaryUnitLayananId.Should().Be("LYDTU");
        ApotekLocationIds.PharmacyServicePointId.Should().Be("APT");
        ApotekLocationIds.PharmacyUnitLayananId
            .Should().NotBe(ApotekLocationIds.DispensingTemporaryUnitLayananId);
    }

    [Fact]
    public void GetDays_WhenParameterPresent_ReturnsConfiguredValue()
    {
        var dal = new Mock<IParamSistemDal>();
        dal.Setup(x => x.GetData(ApotekSystemParameters.CollectionWindowDaysKey))
            .Returns(new ParamSistemModel(
                ApotekSystemParameters.CollectionWindowDaysKey,
                "window",
                "10"));

        new CollectionWindowDaysProvider(dal.Object).GetDays().Should().Be(10);
    }

    [Fact]
    public void Collection_window_seed_defaults_to_seven_and_does_not_alter_antrian_entry()
    {
        var sqlRoot = FindSqlRoot();
        var seed = File.ReadAllText(Path.Combine(sqlRoot, "BILRG_Apt_Seed_CollectionWindow.sql"));
        seed.Should().Contain("APT_COLLECTION_WINDOW_DAYS");
        seed.Should().Contain("'7'");

        var antrianAlter = Directory.EnumerateFiles(sqlRoot, "*.sql")
            .Select(File.ReadAllText)
            .Any(text => text.Contains("ALTER TABLE", StringComparison.OrdinalIgnoreCase)
                         && text.Contains("BILRG_AntrianEntry", StringComparison.OrdinalIgnoreCase));
        antrianAlter.Should().BeFalse();
    }

    private static string FindSqlRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Bilreg.SqlDb", "ApotekContext");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Unable to locate Bilreg.SqlDb/ApotekContext.");
    }
}
