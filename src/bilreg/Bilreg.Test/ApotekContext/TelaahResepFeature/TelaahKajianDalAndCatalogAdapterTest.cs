using Bilreg.Api.Configurations;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature;

public class TelaahKajianDalAndCatalogAdapterTest
{
    private readonly TelaahKajianDal _kajianDal;
    private readonly KemkesKajianResepDal _kemkesDal;
    private readonly KemkesKajianResepCatalogAdapter _adapter;

    public TelaahKajianDalAndCatalogAdapterTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _kajianDal = new TelaahKajianDal(opt);
        _kemkesDal = new KemkesKajianResepDal(opt);
        _adapter = new KemkesKajianResepCatalogAdapter(_kemkesDal);
    }

    [Fact]
    public void TelaahKajianDal_Insert_ListData_and_Delete_work_against_devTest()
    {
        var testId = "ATR" + Guid.NewGuid().ToString("N")[..8];
        var key = TelaahResepModel.Key(testId);

        try
        {
            var dtos = new List<TelaahKajianDto>
            {
                new(testId, 1, "1.1", "Administrasi 1", "1", "Persyaratan Administrasi", true, ""),
                new(testId, 2, "3.5", "Interaksi Obat", "3", "Persyaratan Klinis", true, "Perlu monitor interaksi")
            };

            _kajianDal.Insert(dtos);

            var loaded = _kajianDal.ListData(key).ToList();
            loaded.Should().HaveCount(2);
            loaded[0].NoUrut.Should().Be(1);
            loaded[0].KajianResepId.Should().Be("1.1");
            loaded[0].IsYes.Should().BeTrue();
            loaded[0].CatatanKajian.Should().BeEmpty();

            loaded[1].NoUrut.Should().Be(2);
            loaded[1].KajianResepId.Should().Be("3.5");
            loaded[1].IsYes.Should().BeTrue();
            loaded[1].CatatanKajian.Should().Be("Perlu monitor interaksi");
        }
        finally
        {
            _kajianDal.Delete(key);
            var remaining = _kajianDal.ListData(key).ToList();
            remaining.Should().BeEmpty();
        }
    }

    [Fact]
    public void KemkesKajianResepDal_ListData_returns_13_items_from_devTest()
    {
        var items = _kemkesDal.ListData().ToList();
        items.Should().HaveCount(13);
        items.Select(x => x.KajianResepId).Should().ContainInOrder(
            "1.1", "1.2", "1.3", "1.4",
            "2.1", "2.2", "2.3", "2.4",
            "3.1", "3.2", "3.3", "3.4", "3.5");
        items.All(x => !string.IsNullOrWhiteSpace(x.GrupKajianResepName)).Should().BeTrue();
    }

    [Fact]
    public void KemkesKajianResepCatalogAdapter_GetAll_returns_13_domain_models_with_correct_polarity()
    {
        var models = _adapter.GetAll();
        models.Should().HaveCount(13);

        models.Select(x => x.NoUrut).Should().Equal(Enumerable.Range(1, 13));
        models.Select(x => x.KajianResepId).Should().ContainInOrder(
            "1.1", "1.2", "1.3", "1.4",
            "2.1", "2.2", "2.3", "2.4",
            "3.1", "3.2", "3.3", "3.4", "3.5");

        // Positive polarity items (1.1 - 3.1) default to true
        for (var i = 0; i < 9; i++)
        {
            models[i].DefaultIsYes.Should().BeTrue();
            models[i].IsYes.Should().BeTrue();
            models[i].RequiresNote.Should().BeFalse();
        }

        // Negative polarity items (3.2 - 3.5) default to false
        for (var i = 9; i < 13; i++)
        {
            models[i].DefaultIsYes.Should().BeFalse();
            models[i].IsYes.Should().BeFalse();
            models[i].RequiresNote.Should().BeFalse();
        }
    }

    [Fact]
    public void DI_Container_resolves_port_and_dal_interfaces()
    {
        var services = new ServiceCollection();
        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Database:ServerName", "dev.smart-ics.com" },
            { "Database:DbName", "devTest" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        services.AddInfrastructure(configuration);

        using var sp = services.BuildServiceProvider();

        var catalogPort = sp.GetService<IKemkesKajianResepCatalogPort>();
        catalogPort.Should().NotBeNull();
        catalogPort.Should().BeOfType<KemkesKajianResepCatalogAdapter>();

        var kajianDal = sp.GetService<ITelaahKajianDal>();
        kajianDal.Should().NotBeNull();
        kajianDal.Should().BeOfType<TelaahKajianDal>();

        var kemkesDal = sp.GetService<IKemkesKajianResepDal>();
        kemkesDal.Should().NotBeNull();
        kemkesDal.Should().BeOfType<KemkesKajianResepDal>();
    }
}
