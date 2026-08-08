using System.Collections.Concurrent;
using System.Data.SqlClient;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

/// <summary>
/// P3-S6 — Synchronization claim: one SynchronizationRequired winner; resume only when allowed.
/// Disposable <c>devTest</c> only.
/// </summary>
[Collection("StockLedgerP3S4")]
public class SynchronizationClaimServiceTest
{
    private readonly IStockLedgerScopeStateRepo _scopeRepo;
    private readonly SynchronizationClaimService _sut;

    public SynchronizationClaimServiceTest()
    {
        var options = ConnStringHelper.GetTestEnv();
        _scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        _sut = new SynchronizationClaimService(new TransHelperUnitOfWork(), _scopeRepo);
    }

    [Fact]
    public void Claim_FromCurrent_TransitionsToSynchronizationRequired()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("CUR");
        Cleanup(key);

        try
        {
            SeedReconstructedCurrent(key);

            var result = _sut.Claim(key, allowResume: false);

            result.Outcome.Should().Be(SynchronizationClaimOutcomeEnum.Claimed);
            result.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.SynchronizationRequired);
            _scopeRepo.LoadEntity(key).Value.SynchronizationState
                .Should().Be(SynchronizationStateEnum.SynchronizationRequired);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_WhenAlreadySynchronizationRequired_WithoutResume_ReturnsAlreadyClaimed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("HOLD");
        Cleanup(key);

        try
        {
            SeedReconstructedCurrent(key);
            _sut.Claim(key, allowResume: false).Outcome.Should().Be(SynchronizationClaimOutcomeEnum.Claimed);

            var second = _sut.Claim(key, allowResume: false);

            second.Outcome.Should().Be(SynchronizationClaimOutcomeEnum.AlreadyClaimed);
            second.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.SynchronizationRequired);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public void Claim_WhenAlreadySynchronizationRequired_WithResume_ReturnsClaimed()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RSM");
        Cleanup(key);

        try
        {
            SeedReconstructedCurrent(key);
            _sut.Claim(key, allowResume: false).Outcome.Should().Be(SynchronizationClaimOutcomeEnum.Claimed);

            var resume = _sut.Claim(key, allowResume: true);

            resume.Outcome.Should().Be(SynchronizationClaimOutcomeEnum.Claimed);
            resume.ScopeState.SynchronizationState.Should().Be(SynchronizationStateEnum.SynchronizationRequired);
        }
        finally
        {
            Cleanup(key);
        }
    }

    [Fact]
    public async Task Claim_ConcurrentFromCurrent_SingleSynchronizationRequiredWinner()
    {
        StockLedgerSchemaFixture.EnsureSchema();
        var key = NewScopeKey("RACE");
        Cleanup(key);

        try
        {
            SeedReconstructedCurrent(key);

            var bag = new ConcurrentBag<SynchronizationClaimResult>();
            var errors = new ConcurrentBag<Exception>();

            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                try
                {
                    var racer = CreateFreshSut();
                    bag.Add(racer.Claim(key, allowResume: false));
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }
            }));

            errors.Should().BeEmpty();
            bag.Should().HaveCount(8);
            bag.Count(x => x.Outcome == SynchronizationClaimOutcomeEnum.Claimed).Should().Be(1);
            bag.Count(x => x.Outcome == SynchronizationClaimOutcomeEnum.AlreadyClaimed).Should().Be(7);

            _scopeRepo.LoadEntity(key).Value.SynchronizationState
                .Should().Be(SynchronizationStateEnum.SynchronizationRequired);
        }
        finally
        {
            Cleanup(key);
        }
    }

    private void SeedReconstructedCurrent(IStockLedgerScopeKey key)
    {
        var opaque = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var position = SynchronizationPositionType.Create(
            opaque,
            LegacyReconstructionBasisCalculator.AlgorithmVersion);
        var scope = StockLedgerScopeStateModel
            .CreateNotReconstructed(key)
            .RequireReconstruction()
            .BeginReconstruction()
            .CompleteReconstruction(position, LegacyReconstructionBasisCalculator.AlgorithmVersion);
        _scopeRepo.SaveChanges(scope);
    }

    private static SynchronizationClaimService CreateFreshSut()
    {
        var options = ConnStringHelper.GetTestEnv();
        var scopeRepo = new StockLedgerScopeStateRepo(new StockLedgerScopeDal(options));
        return new SynchronizationClaimService(new TransHelperUnitOfWork(), scopeRepo);
    }

    private static StockLedgerScopeKeyType NewScopeKey(string tag)
    {
        var ulid = Ulid.NewUlid().ToString();
        var tag3 = tag.Length <= 3 ? tag : tag[..3];
        return StockLedgerScopeKeyType.Create(
            (tag3 + ulid)[..13],
            (tag3 + ulid)[3..13]);
    }

    private static void Cleanup(IStockLedgerScopeKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        conn.Execute(
            "DELETE FROM BILRG_StokLedgerScope WHERE BrgId = @BrgId AND ReceiptSourceId = @ReceiptSourceId",
            new { key.BrgId, key.ReceiptSourceId });
    }
}
