using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Unit tests handler <see cref="GenerateHnaBatchHandler"/> (ARCHITECTURE TD-04, AC-PRC-02).
/// Memvalidasi matriks Cara Generate x Metode Agregasi, penerbitan nomor <c>HN...</c>,
/// perlakuan EX-PRC-01, dan penyimpanan atomik via repo (in-memory Moq, tanpa database).
/// </summary>
public class GenerateHnaBatchHandlerTest
{
    private const string BarangA = "BRG-A";
    private const string BarangB = "BRG-B";

    private static readonly DateTime TglBerlaku = new(2026, 7, 1);

    private readonly Mock<IPurchaseReceiptHnaQuery> _queryMock = new();
    private readonly Mock<IHnaBatchRepo> _repoMock = new();

    private GenerateHnaBatchHandler CreateSut() => new(_queryMock.Object, _repoMock.Object);

    private static PurchaseReceiptHnaDto Receipt(
        string receiptId,
        int noUrut,
        string barangId,
        DateTime tgl,
        decimal harga,
        decimal diskon,
        decimal tax,
        decimal konversi = 1m)
    {
        var hargaKecil = konversi > 1m ? Math.Round(harga / konversi, 2) : harga;
        return new PurchaseReceiptHnaDto(
            receiptId, noUrut, barangId, "TAB", konversi,
            harga, diskon, tax, 1m, harga, harga, tgl, hargaKecil);
    }

    /// <summary>Dua faktur historis contoh dari outcome (Faktur A 20 Juni, Faktur B 25 Juni).</summary>
    private static List<PurchaseReceiptHnaDto> TwoReceipts(string barangId) =>
    [
        Receipt("DO-A", 1, barangId, new DateTime(2026, 6, 20), 1000m, 250m, 100m),
        Receipt("DO-B", 2, barangId, new DateTime(2026, 6, 25), 1050m, 262.5m, 105m)
    ];

    private void SetupHistory(params (string BarangId, IEnumerable<PurchaseReceiptHnaDto> Rows)[] entries)
    {
        var map = entries.ToDictionary(x => x.BarangId, x => x.Rows.ToList());
        _queryMock
            .Setup(x => x.GetProcurementHistory(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .Returns((string id, DateTime? start, DateTime? end) =>
                map.TryGetValue(id, out var rows) ? rows : new List<PurchaseReceiptHnaDto>());
    }

    [Theory]
    [InlineData(CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.Terakhir, 1050d)]
    [InlineData(CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.Terbesar, 1050d)]
    [InlineData(CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.RataRata, 1025d)]
    [InlineData(CaraGenerateHnaEnum.HppPlusTax, MetodeGenHnaEnum.Terakhir, 1155d)]
    [InlineData(CaraGenerateHnaEnum.HppPlusTax, MetodeGenHnaEnum.Terbesar, 1155d)]
    [InlineData(CaraGenerateHnaEnum.HppPlusTax, MetodeGenHnaEnum.RataRata, 1127.5d)]
    [InlineData(CaraGenerateHnaEnum.HppPlusTaxMinDisc, MetodeGenHnaEnum.Terakhir, 892.5d)]
    [InlineData(CaraGenerateHnaEnum.HppPlusTaxMinDisc, MetodeGenHnaEnum.Terbesar, 892.5d)]
    [InlineData(CaraGenerateHnaEnum.HppPlusTaxMinDisc, MetodeGenHnaEnum.RataRata, 871.25d)]
    public async Task Handle_CaraXMetode_ProducesNineCombinationMatrix(
        CaraGenerateHnaEnum cara, MetodeGenHnaEnum metode, double expectedHnaGen)
    {
        SetupHistory((BarangA, TwoReceipts(BarangA)));

        var result = await CreateSut().Handle(
            new GenerateHnaBatchCommand([BarangA], cara, metode, TglBerlaku, "PETUGAS01"),
            CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].HnaGen.Should().Be((decimal)expectedHnaGen);
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<HnaBatchModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PopulatesHnaVariants_AndIssuesHnBatchNumber()
    {
        SetupHistory((BarangA, TwoReceipts(BarangA)));
        HnaBatchModel? saved = null;
        _repoMock
            .Setup(x => x.SaveChanges(It.IsAny<HnaBatchModel>()))
            .Callback<HnaBatchModel>(b => saved = b);

        var result = await CreateSut().Handle(
            new GenerateHnaBatchCommand([BarangA], CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.Terakhir,
                TglBerlaku, "PETUGAS01"),
            CancellationToken.None);

        result.HnaBatchId.Should().StartWith("HN");
        result.ItemCount.Should().Be(1);

        var item = result.Items.Single();
        item.BarangId.Should().Be(BarangA);
        item.NoUrut.Should().Be(1);
        item.HnaAkhir.Should().Be(1050m);   // faktur termutakhir
        item.HnaMax.Should().Be(1050m);
        item.HnaRata.Should().Be(1025m);    // (1000 + 1050) / 2
        item.DiskonGen.Should().Be(262.5m); // faktur acuan termutakhir

        saved.Should().NotBeNull();
        saved!.HnaBatchId.Should().Be(result.HnaBatchId);
        saved.ListItem.Should().ContainSingle();
        saved.ListItem[0].TglBerlaku.Should().Be(TglBerlaku);
        saved.ListItem[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UsesBaseUnitPrice_WhenReceiptIsInPackageUnit()
    {
        // Harga kemasan 10.000 untuk Box isi 10 -> HPP satuan kecil 1.000.
        var rows = new[] { Receipt("DO-PKG", 1, BarangA, new DateTime(2026, 6, 20), 10000m, 0m, 0m, konversi: 10m) };
        SetupHistory((BarangA, rows));

        var result = await CreateSut().Handle(
            new GenerateHnaBatchCommand([BarangA], CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.Terakhir,
                TglBerlaku, "PETUGAS01"),
            CancellationToken.None);

        result.Items.Single().HnaGen.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_MultipleBarang_AddsAllItems_SavedAtomicallyOnce()
    {
        SetupHistory(
            (BarangA, TwoReceipts(BarangA)),
            (BarangB, [Receipt("DO-C", 1, BarangB, new DateTime(2026, 6, 27), 500m, 0m, 0m)]));
        HnaBatchModel? saved = null;
        _repoMock
            .Setup(x => x.SaveChanges(It.IsAny<HnaBatchModel>()))
            .Callback<HnaBatchModel>(b => saved = b);

        var result = await CreateSut().Handle(
            new GenerateHnaBatchCommand([BarangA, BarangB], CaraGenerateHnaEnum.Hpp,
                MetodeGenHnaEnum.Terbesar, TglBerlaku, "PETUGAS01"),
            CancellationToken.None);

        result.ItemCount.Should().Be(2);
        result.Items.Select(x => x.BarangId).Should().BeEquivalentTo(new[] { BarangA, BarangB });
        saved!.ListItem.Should().HaveCount(2);
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<HnaBatchModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoProcurementHistory_ThrowsAndDoesNotSave()
    {
        SetupHistory((BarangA, []));

        var act = async () => await CreateSut().Handle(
            new GenerateHnaBatchCommand([BarangA], CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.Terakhir,
                TglBerlaku, "PETUGAS01"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*EX-PRC-01*");
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<HnaBatchModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyBarangIds_ThrowsArgumentException()
    {
        var act = async () => await CreateSut().Handle(
            new GenerateHnaBatchCommand([], CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.Terakhir,
                TglBerlaku, "PETUGAS01"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _queryMock.Verify(
            x => x.GetProcurementHistory(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DefaultTglBerlaku_ThrowsArgumentException()
    {
        var act = async () => await CreateSut().Handle(
            new GenerateHnaBatchCommand([BarangA], CaraGenerateHnaEnum.Hpp, MetodeGenHnaEnum.Terakhir,
                default, "PETUGAS01"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
