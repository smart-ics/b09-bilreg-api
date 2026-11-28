using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgListStokLokasiQueryTest
{
    private readonly Mock<IStokLokasiDal> _stokLokasiDalMock = new();
    private readonly MutasiBrgListStokLokasiHandler _sut;

    public MutasiBrgListStokLokasiQueryTest()
    {
        _sut = new MutasiBrgListStokLokasiHandler(_stokLokasiDalMock.Object);
    }

    [Fact]
    public async Task Handle_GivenNullRequest_ThrowsArgumentNullException()
    {
        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GivenNullLayananId_ThrowsArgumentNullException()
    {
        // ARRANGE
        var query = new MutasiBrgListStokLokasiQuery(null!);

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.Handle(query, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_GivenEmptyOrWhitespaceLayananId_ThrowsArgumentException(string invalidLayananId)
    {
        // ARRANGE
        var query = new MutasiBrgListStokLokasiQuery(invalidLayananId);

        // ACT & ASSERT
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenDalReturnsNull_ReturnsEmptyList()
    {
        // ARRANGE
        const string layananId = "L01";
        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns((IEnumerable<MutasiBrgStokLokasiView>)null!);

        var query = new MutasiBrgListStokLokasiQuery(layananId);

        // ACT
        var result = await _sut.Handle(query, CancellationToken.None);

        // ASSERT
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenDalReturnsEmpty_ReturnsEmptyList()
    {
        // ARRANGE
        const string layananId = "L01";
        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns([]);

        var query = new MutasiBrgListStokLokasiQuery(layananId);

        // ACT
        var result = await _sut.Handle(query, CancellationToken.None);

        // ASSERT
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FiltersOutItemsWithZeroOrNegativeTotalStok()
    {
        // ARRANGE
        const string layananId = "L01";
        var views = new List<MutasiBrgStokLokasiView>
        {
            new("B01", "Amoxicillin 500mg", 50m, "S01", "Tablet", "R01", 1000m, new List<MutasiBrgStokBatchView>
            {
                new("BATCH-A", new DateTime(2027, 6, 1), 50m, 1000m)
            }),
            new("B02", "Paracetamol 500mg", 0m, "S01", "Tablet", "R01", 800m, new List<MutasiBrgStokBatchView>
            {
                new("BATCH-B", new DateTime(2027, 6, 1), 0m, 800m)
            }),
            new("B03", "Ibuprofen 400mg", -5m, "S01", "Tablet", "R01", 1200m, new List<MutasiBrgStokBatchView>
            {
                new("BATCH-C", new DateTime(2027, 6, 1), -5m, 1200m)
            })
        };

        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns(views);

        var query = new MutasiBrgListStokLokasiQuery(layananId);

        // ACT
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        result.Single().BrgId.Should().Be("B01");
        result.Single().TotalStok.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_FiltersOutBatchesWithZeroOrNegativeQtySisa()
    {
        // ARRANGE
        const string layananId = "L01";
        var views = new List<MutasiBrgStokLokasiView>
        {
            new("B01", "Amoxicillin 500mg", 60m, "S01", "Tablet", "R01", 1000m, new List<MutasiBrgStokBatchView>
            {
                new("BATCH-VALID", new DateTime(2027, 6, 1), 60m, 1000m),
                new("BATCH-EMPTY", new DateTime(2027, 1, 1), 0m, 1000m),
                new("BATCH-NEG", new DateTime(2026, 12, 1), -2m, 1000m)
            })
        };

        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns(views);

        var query = new MutasiBrgListStokLokasiQuery(layananId);

        // ACT
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        var batches = result.Single().Batches;
        batches.Should().HaveCount(1);
        batches.Single().NoBatch.Should().Be("BATCH-VALID");
        batches.Single().QtySisa.Should().Be(60m);
    }

    [Fact]
    public async Task Handle_SortsBatchesInFefoOrder_TglEdAscending()
    {
        // ARRANGE
        const string layananId = "L01";
        var views = new List<MutasiBrgStokLokasiView>
        {
            new("B01", "Amoxicillin 500mg", 150m, "S01", "Tablet", "R01", 1000m, new List<MutasiBrgStokBatchView>
            {
                new("BATCH-LATE", new DateTime(2028, 1, 1), 50m, 1100m),
                new("BATCH-EARLIEST", new DateTime(2026, 12, 31), 40m, 950m),
                new("BATCH-MID", new DateTime(2027, 6, 30), 60m, 1000m)
            })
        };

        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns(views);

        var query = new MutasiBrgListStokLokasiQuery(layananId);

        // ACT
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        var batches = result.Single().Batches;
        batches.Should().HaveCount(3);
        batches[0].NoBatch.Should().Be("BATCH-EARLIEST");
        batches[0].TglEd.Should().Be("2026-12-31");
        batches[1].NoBatch.Should().Be("BATCH-MID");
        batches[1].TglEd.Should().Be("2027-06-30");
        batches[2].NoBatch.Should().Be("BATCH-LATE");
        batches[2].TglEd.Should().Be("2028-01-01");
    }

    [Fact]
    public async Task Handle_WhenBatchHasDefaultTglEd_MapsToEmptyString()
    {
        // ARRANGE
        const string layananId = "L01";
        var views = new List<MutasiBrgStokLokasiView>
        {
            new("B01", "Legacy Medicine", 20m, "S01", "Botol", "R01", 5000m, new List<MutasiBrgStokBatchView>
            {
                new("LEGACY-BATCH", default, 20m, 5000m)
            })
        };

        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns(views);

        var query = new MutasiBrgListStokLokasiQuery(layananId);

        // ACT
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        result.Single().Batches.Single().TglEd.Should().BeEmpty();
    }

    [Theory]
    [InlineData("amox", 1, "B01")]
    [InlineData("AMOX", 1, "B01")]
    [InlineData("  amoxicillin  ", 1, "B01")]
    [InlineData("B01", 1, "B01")]
    [InlineData("b02", 1, "B02")]
    [InlineData("cetirizine", 1, "B02")]
    [InlineData("500", 2, "")]
    [InlineData("NonExistent", 0, "")]
    public async Task Handle_WithKeyword_FiltersByNameOrCode(string keyword, int expectedCount, string expectedSingleId)
    {
        // ARRANGE
        const string layananId = "L01";
        var views = new List<MutasiBrgStokLokasiView>
        {
            new("B01", "Amoxicillin 500mg", 100m, "S01", "Tablet", "R01", 1000m, new List<MutasiBrgStokBatchView>
            {
                new("BAT01", new DateTime(2027, 1, 1), 100m, 1000m)
            }),
            new("B02", "Cetirizine 500mg", 50m, "S01", "Tablet", "R01", 1500m, new List<MutasiBrgStokBatchView>
            {
                new("BAT02", new DateTime(2027, 2, 1), 50m, 1500m)
            })
        };

        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns(views);

        var query = new MutasiBrgListStokLokasiQuery(layananId, keyword);

        // ACT
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(expectedCount);
        if (expectedCount == 1)
        {
            result.Single().BrgId.Should().Be(expectedSingleId);
        }
    }

    [Fact]
    public async Task Handle_GivenValidQuery_MapsAllPropertiesAccurately()
    {
        // ARRANGE
        const string layananId = "4FM02";
        var views = new List<MutasiBrgStokLokasiView>
        {
            new("BRG009", "Ceftriaxone 1g Inj", 75m, "SAT03", "Vial", "REK99", 25000m, new List<MutasiBrgStokBatchView>
            {
                new("BATCH-CFT-01", new DateTime(2027, 8, 15), 50m, 24500m),
                new("BATCH-CFT-02", new DateTime(2027, 12, 20), 25m, 25500m)
            })
        };

        _stokLokasiDalMock
            .Setup(x => x.ListStokLokasi(layananId))
            .Returns(views);

        var query = new MutasiBrgListStokLokasiQuery(layananId);

        // ACT
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        var item = result.Single();
        item.BrgId.Should().Be("BRG009");
        item.BrgName.Should().Be("Ceftriaxone 1g Inj");
        item.TotalStok.Should().Be(75m);
        item.SatuanId.Should().Be("SAT03");
        item.SatuanName.Should().Be("Vial");
        item.GroupRekDkId.Should().Be("REK99");
        item.HppDefault.Should().Be(25000m);

        item.Batches.Should().HaveCount(2);
        item.Batches[0].NoBatch.Should().Be("BATCH-CFT-01");
        item.Batches[0].TglEd.Should().Be("2027-08-15");
        item.Batches[0].QtySisa.Should().Be(50m);
        item.Batches[0].Hpp.Should().Be(24500m);

        item.Batches[1].NoBatch.Should().Be("BATCH-CFT-02");
        item.Batches[1].TglEd.Should().Be("2027-12-20");
        item.Batches[1].QtySisa.Should().Be(25m);
        item.Batches[1].Hpp.Should().Be(25500m);
    }
}
