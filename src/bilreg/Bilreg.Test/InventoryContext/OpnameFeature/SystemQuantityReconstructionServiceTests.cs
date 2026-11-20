using Bilreg.Application.InventoryContext.OpnameFeature.Ports;
using Bilreg.Domain.InventoryContext.OpnameFeature;
using Bilreg.Infrastructure.InventoryContext.OpnameFeature.Services;
using Dapper;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.InventoryContext.OpnameFeature;

public class SystemQuantityReconstructionServiceTests
{
    [Theory]
    [InlineData(100, 0, 0, 100)]       // No movements: Reconstructed = Current
    [InlineData(150, 50, 0, 100)]      // Inbound after cutoff: 150 - 50 = 100
    [InlineData(70, 0, 30, 100)]       // Outbound after cutoff: 70 + 30 = 100
    [InlineData(110, 30, 20, 100)]     // Both inbound & outbound: 110 - 30 + 20 = 100
    [InlineData(50, 50, 0, 0)]         // Item had 0 at cutoff, received 50 after cutoff
    [InlineData(0, 0, 40, 40)]         // Item had 40 at cutoff, completely issued out after cutoff
    public void CalculateReconstructedQuantity_ReturnsExpectedFormulaResult(
        decimal currentQty,
        decimal inAfterCutoff,
        decimal outAfterCutoff,
        decimal expectedReconstructed)
    {
        // Act
        var result = SystemQuantityReconstructionService.CalculateReconstructedQuantity(
            currentQty, inAfterCutoff, outAfterCutoff);

        // Assert
        result.Should().Be(expectedReconstructed);
    }

    [Fact]
    public void BuildQuery_WithoutFilter_GeneratesExpectedCteSql()
    {
        // Arrange
        var cutoff = new DateTime(2026, 11, 20, 8, 30, 0);

        // Act
        var (sql, parameters) = SystemQuantityReconstructionService.BuildQuery("LYN01", cutoff, null);

        // Assert
        sql.Should().Contain("tb_stok s WITH (NOLOCK)");
        sql.Should().Contain("tb_buku b WITH (NOLOCK)");
        sql.Should().Contain("WITH InboundAfterCount AS");
        sql.Should().Contain("OutboundAfterCount AS");
        sql.Should().Contain("(SUM(s.fn_qty) - ISNULL(ib.TotalIn, 0) + ISNULL(ob.TotalOut, 0)) AS ReconstructedQty");
        sql.Should().Contain("WHERE s.fs_kd_layanan = @LayananId");
        sql.Should().Contain("CAST(CONVERT(VARCHAR(10), b.fd_tgl_jam_mutasi, 120) + ' ' + b.fs_jam_mutasi AS DATETIME) > @CutoffTime");
        sql.Should().NotContain("IN @BrgIds");

        parameters.ParameterNames.Should().Contain("LayananId");
        parameters.ParameterNames.Should().Contain("CutoffTime");
        parameters.ParameterNames.Should().NotContain("BrgIds");
    }

    [Fact]
    public void BuildQuery_WithFilterBrgIds_IncludesInClauseInAllCtesAndStok()
    {
        // Arrange
        var cutoff = new DateTime(2026, 11, 20, 8, 30, 0);
        var filter = new[] { "BRG001", "BRG002" };

        // Act
        var (sql, parameters) = SystemQuantityReconstructionService.BuildQuery("LYN01", cutoff, filter);

        // Assert
        sql.Should().Contain("AND b.fs_kd_barang IN @BrgIds");
        sql.Should().Contain("AND s.fs_kd_barang IN @BrgIds");

        parameters.ParameterNames.Should().Contain("BrgIds");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ReconstructBalancesAsync_EmptyLayananId_ReturnsEmptyDictionaryWithoutQuery(string? layananId)
    {
        // Arrange
        var queryExecuted = false;
        var sut = new SystemQuantityReconstructionService((sql, dp, ct) =>
        {
            queryExecuted = true;
            return Task.FromResult<IEnumerable<ReconstructedRowDto>>([]);
        });

        // Act
        var result = await sut.ReconstructBalancesAsync(layananId!, DateTime.Now);

        // Assert
        result.Should().BeEmpty();
        queryExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task ReconstructBalancesAsync_MapsRowsToDictionaryWithItemTupleKey_Correctly()
    {
        // Arrange
        var cutoff = new DateTime(2026, 11, 20, 8, 0, 0);
        var mockRows = new List<ReconstructedRowDto>
        {
            new("BRG001", "BATCH01", "2027-12-31", 120, 20, 0, 100),
            new("BRG001", "BATCH02", "2028-06-30", 50, 0, 10, 60),
            new("BRG002", "BATCH03", "2026-10-15", 30, 10, 15, 35)
        };

        string capturedSql = string.Empty;
        DynamicParameters? capturedParameters = null;

        var sut = new SystemQuantityReconstructionService((sql, dp, ct) =>
        {
            capturedSql = sql;
            capturedParameters = dp;
            return Task.FromResult<IEnumerable<ReconstructedRowDto>>(mockRows);
        });

        // Act
        var result = await sut.ReconstructBalancesAsync("LYN01", cutoff, new[] { "BRG001", "BRG002" });

        // Assert
        result.Should().HaveCount(3);

        var key1 = new ItemTupleKey("BRG001", "BATCH01", "2027-12-31");
        var key2 = new ItemTupleKey("BRG001", "BATCH02", "2028-06-30");
        var key3 = new ItemTupleKey("BRG002", "BATCH03", "2026-10-15");

        result[key1].Should().Be(100m);
        result[key2].Should().Be(60m);
        result[key3].Should().Be(35m);

        capturedSql.Should().NotBeNullOrWhiteSpace();
        capturedParameters.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullSqlDatabase_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new SystemQuantityReconstructionService((Bilreg.Application.InventoryContext.MutasiFeature.ISqlDatabase)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
