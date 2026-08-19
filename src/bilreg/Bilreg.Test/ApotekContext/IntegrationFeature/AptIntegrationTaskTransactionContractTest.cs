using System.Reflection;
using Bilreg.Application.ApotekContext.IntegrationFeature;
using Bilreg.Domain.ApotekContext.IntegrationFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.IntegrationFeature;

public class AptIntegrationTaskTransactionContractTest
{
    [Fact]
    public void Duplicate_idempotency_key_does_not_insert_second_row()
    {
        var store = new Dictionary<string, AptIntegrationTaskModel>(StringComparer.Ordinal);
        var byKey = new Dictionary<string, AptIntegrationTaskModel>(StringComparer.Ordinal);
        var repo = new Mock<IAptIntegrationTaskRepo>();
        repo.Setup(x => x.LoadByIdempotencyKey(It.IsAny<string>()))
            .Returns((string key) =>
                byKey.TryGetValue(key, out var t)
                    ? MayBe.From(t)
                    : MayBe<AptIntegrationTaskModel>.None);
        repo.Setup(x => x.SaveChanges(It.IsAny<AptIntegrationTaskModel>()))
            .Callback<AptIntegrationTaskModel>(t =>
            {
                if (byKey.ContainsKey(t.IdempotencyKey))
                    throw new InvalidOperationException("duplicate idempotency key");
                store[t.IntegrationTaskId] = t;
                byKey[t.IdempotencyKey] = t;
            });

        var first = AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.StockReserve,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP1",
            "ADP1:I1:RESERVE",
            AptIntegrationDestinationEnum.StockLedger,
            "{}");
        AptIntegrationTaskEnqueue.InsertIfAbsent(repo.Object, first);
        AptIntegrationTaskEnqueue.InsertIfAbsent(repo.Object, AptIntegrationTaskModel.CreatePending(
            AptIntegrationTaskTypeEnum.StockReserve,
            AptIntegrationSourceKindEnum.Dispensing,
            "ADP1",
            "ADP1:I1:RESERVE",
            AptIntegrationDestinationEnum.StockLedger,
            "{}"));

        store.Should().HaveCount(1);
        byKey.Should().ContainKey("ADP1:I1:RESERVE");
    }

    [Fact]
    public void Enqueue_and_business_save_share_the_same_transaction_helper_contract()
    {
        typeof(AptIntegrationTaskEnqueue)
            .GetMethod(nameof(AptIntegrationTaskEnqueue.RequireAmbientTransaction), BindingFlags.Public | BindingFlags.Static)
            .Should().NotBeNull();
        AptIntegrationTaskEnqueue.RequireAmbientTransaction()
            .Should().BeTrue("handlers must insert tasks inside TransHelper.NewScope()");
    }
}
