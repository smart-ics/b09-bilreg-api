using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Reflection;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P2-S5 — Phase A reconstruction claim: short TX, one Reconstructing winner, no history/calc.
/// Disposable <c>devTest</c> only; never writes <c>tb_stok</c> / <c>tb_buku</c>.
/// </summary>
public class ReconstructionClaimServiceTest
{
    private readonly IStockLedgerScopeStateRepo _scopeRepo;
    private readonly ReconstructionClaimService _sut;

    public ReconstructionClaimServiceTest()
    {
        var options = ConnStringHelper.GetTestEnv();
        _scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        _sut = new ReconstructionClaimService(new TransHelperUnitOfWork(), _scopeRepo);
    }

    [Fact]
    public void Claim_WhenScopeMissing_InsertsReconstructingAndCommits()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("MISS");
        Cleanup(key);

        try
        {
            var result = _sut.Claim(key);

            result.Outcome.Should().Be(ReconstructionClaimOutcomeEnum.Claimed);
            result.ScopeState.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructing);
            result.ScopeState.HasSynchronizationPosition.Should().BeFalse();
            result.ScopeState.InconsistencyReason.Should().BeNull();

            var loaded = _scopeRepo.LoadEntity(key).Value;
            loaded.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructing);
            loaded.SynchronizationState.Should().Be(SynchronizationStateEnum.Current);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_FromNotReconstructed_TransitionsToReconstructing()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("NR");
        Cleanup(key);

        try
        {
            _scopeRepo.SaveChanges(StockLedgerScopeStateModel.CreateNotReconstructed(key));

            var result = _sut.Claim(key);

            result.Outcome.Should().Be(ReconstructionClaimOutcomeEnum.Claimed);
            _scopeRepo.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_FromReconstructionRequired_TransitionsToReconstructing()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("REQ");
        Cleanup(key);

        try
        {
            var required = StockLedgerScopeStateModel
                .CreateNotReconstructed(key)
                .RequireReconstruction();
            _scopeRepo.SaveChanges(required);

            var result = _sut.Claim(key);

            result.Outcome.Should().Be(ReconstructionClaimOutcomeEnum.Claimed);
            _scopeRepo.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_WhenAlreadyReconstructing_ReturnsAlreadyClaimedWithoutCorruption()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("HOLD");
        Cleanup(key);

        try
        {
            var first = _sut.Claim(key);
            first.Outcome.Should().Be(ReconstructionClaimOutcomeEnum.Claimed);

            var second = _sut.Claim(key);

            second.Outcome.Should().Be(ReconstructionClaimOutcomeEnum.AlreadyClaimed);
            second.ScopeState.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructing);
            _scopeRepo.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task Claim_ConcurrentDuplicate_SingleReconstructingWinner()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RACE");
        Cleanup(key);

        try
        {
            // Seed a durable claimable row so both racers take the conditional-update path.
            _scopeRepo.SaveChanges(StockLedgerScopeStateModel.CreateNotReconstructed(key));

            var bag = new ConcurrentBag<ReconstructionClaimResult>();
            var errors = new ConcurrentBag<Exception>();

            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                try
                {
                    // Each racer gets its own service/repo instances (independent connections).
                    var racer = CreateFreshSut();
                    bag.Add(racer.Claim(key));
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));

            errors.Should().BeEmpty();
            bag.Should().HaveCount(8);
            bag.Count(x => x.Outcome == ReconstructionClaimOutcomeEnum.Claimed).Should().Be(1);
            bag.Count(x => x.Outcome == ReconstructionClaimOutcomeEnum.AlreadyClaimed).Should().Be(7);

            var durable = _scopeRepo.LoadEntity(key).Value;
            durable.ReconstructionStatus.Should().Be(ReconstructionStatusEnum.Reconstructing);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_FromReconstructed_IsRejected()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("DONE");
        Cleanup(key);

        try
        {
            var reconstructed = StockLedgerScopeStateModel
                .CreateNotReconstructed(key)
                .RequireReconstruction()
                .BeginReconstruction()
                .CompleteReconstruction(
                    SynchronizationPositionType.CreateFromUtf8Token("fp-done", "fingerprint-v1"),
                    "basis-1");
            _scopeRepo.SaveChanges(reconstructed);

            var act = () => _sut.Claim(key);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*BeginReconstruction*Reconstructed*");

            _scopeRepo.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructed);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_FromInconsistent_IsRejected()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("INC");
        Cleanup(key);

        try
        {
            var inconsistent = StockLedgerScopeStateModel
                .CreateNotReconstructed(key)
                .RequireReconstruction()
                .BeginReconstruction()
                .MarkReconstructionInconsistent("fixture inconsistency");
            _scopeRepo.SaveChanges(inconsistent);

            var act = () => _sut.Claim(key);
            act.Should().Throw<InvalidOperationException>();

            _scopeRepo.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Inconsistent);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_Service_HasNoLegacyReadOrBaselineCalculatorDependencies()
    {
        var ctor = typeof(ReconstructionClaimService)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();

        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

        paramTypes.Should().BeEquivalentTo(
        [
            typeof(Bilreg.Application.Shared.IUnitOfWork),
            typeof(IStockLedgerScopeStateRepo)
        ]);

        paramTypes.Should().NotContain(t =>
            t.Name.Contains("LegacyStockRead", StringComparison.OrdinalIgnoreCase)
            || t.Name.Contains("BaselineCalculator", StringComparison.OrdinalIgnoreCase)
            || t.Name.Contains("BasisCalculator", StringComparison.OrdinalIgnoreCase)
            || t.Name.Contains("Movement", StringComparison.OrdinalIgnoreCase)
            || t.Name.Contains("Position", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Claim_DoesNotModifyLegacyStockRows()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        StockLedgerSchemaFixture.EnsureLegacyStockTables();

        var key = NewScopeKey("LEG");
        Cleanup(key);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();

        var stokBefore = CountRows(conn, "tb_stok");
        var bukuBefore = CountRows(conn, "tb_buku");

        try
        {
            var result = _sut.Claim(key);
            result.Outcome.Should().Be(ReconstructionClaimOutcomeEnum.Claimed);

            CountRows(conn, "tb_stok").Should().Be(stokBefore);
            CountRows(conn, "tb_buku").Should().Be(bukuBefore);

            // Claim path writes Scope coexistence only — no Layer/Position rows for this scope.
            CountScopeLayers(conn, key).Should().Be(0);
            CountScopePositions(conn, key).Should().Be(0);
            _scopeRepo.LoadEntity(key).Value.ReconstructionStatus
                .Should().Be(ReconstructionStatusEnum.Reconstructing);
        }
        finally
        {
            Cleanup(key);
        }
    }

    private static ReconstructionClaimService CreateFreshSut()
    {
        var options = ConnStringHelper.GetTestEnv();
        var repo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        return new ReconstructionClaimService(new TransHelperUnitOfWork(), repo);
    }

    private static StockLedgerScopeKeyType NewScopeKey(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var shortTag = tag.Length <= 3 ? tag : tag[..3];
        // Column widths: BrgId VARCHAR(13), ReceiptSourceId VARCHAR(10).
        return StockLedgerScopeKeyType.Create(
            $"BRGS5{shortTag}{ulid}"[..13],
            $"DOS5{shortTag}{ulid}"[..10]);
    }

    private static void Cleanup(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        conn.Execute(
            """
            DELETE FROM BILRG_StokLedgerScope
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
            """,
            new { key.BrgId, key.ReceiptSourceId });
    }

    private static int CountRows(SqlConnection conn, string table)
        => conn.ExecuteScalar<int>($"SELECT COUNT(1) FROM {table}");

    private static int CountScopeLayers(SqlConnection conn, IStockLedgerScopeKey key)
        => conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokLayer
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
            """,
            new { key.BrgId, key.ReceiptSourceId });

    private static int CountScopePositions(SqlConnection conn, IStockLedgerScopeKey key)
        => conn.ExecuteScalar<int>(
            """
            SELECT COUNT(1) FROM BILRG_StokPosition
            WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId
            """,
            new { key.BrgId, key.ReceiptSourceId });
}
