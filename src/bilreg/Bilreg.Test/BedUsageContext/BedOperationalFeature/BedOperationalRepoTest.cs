using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.BedOperationalFeature;

public class BedOperationalRepoTest
{
    private static readonly DateTime At =
        new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IBedOperationalDal> _header = new();
    private readonly Mock<IBedReadinessTransactionDal> _transactions = new();
    private readonly Mock<IBedReadinessCorrectionDal> _corrections = new();

    [Fact]
    public void SaveChanges_NewAggregate_InsertsHeaderAndAllFacts()
    {
        var model = Ready();
        SetupEmptyDetails();

        Sut().SaveChanges(model);

        _header.Verify(x => x.Insert(It.Is<BedOperationalDto>(
            d => d.BedId == model.BedId && d.Version == model.Version)), Times.Once);
        _transactions.Verify(x => x.Insert(It.Is<IEnumerable<BedReadinessTransactionDto>>(
            rows => rows.Count() == 1)), Times.Once);
        _corrections.Verify(x => x.Insert(It.Is<IEnumerable<BedReadinessCorrectionDto>>(
            rows => !rows.Any())), Times.Once);
    }

    [Fact]
    public void SaveChanges_SequentialVersion_ConditionallyUpdatesHeader()
    {
        var stored = Create();
        var model = Ready();
        _header.Setup(x => x.GetData(It.IsAny<IBedOperationalKey>()))
            .Returns(BedOperationalDto.FromModel(stored));
        _header.Setup(x => x.UpdateConditional(
                It.IsAny<BedOperationalDto>(), stored.Version))
            .Returns(1);
        SetupEmptyDetails();

        Sut().SaveChanges(model);

        _header.Verify(x => x.UpdateConditional(
            It.Is<BedOperationalDto>(d => d.Version == 1), 0), Times.Once);
        _header.Verify(x => x.Insert(It.IsAny<BedOperationalDto>()), Times.Never);
    }

    [Fact]
    public void SaveChanges_AppendsOnlyUnseenTransactions()
    {
        var cleaning = Cleaning();
        var model = cleaning.VerifyReady(
            "Ready verified",
            "EVIDENCE-2",
            "PEG-2",
            "PEG-3",
            At.AddHours(1),
            At.AddHours(1).AddMinutes(1),
            string.Empty,
            "REQ-2");
        var existing = cleaning.ListReadinessTransaction
            .Select(BedReadinessTransactionDto.FromModel)
            .ToList();
        _header.Setup(x => x.GetData(It.IsAny<IBedOperationalKey>()))
            .Returns(BedOperationalDto.FromModel(cleaning));
        _header.Setup(x => x.UpdateConditional(
                It.IsAny<BedOperationalDto>(), cleaning.Version))
            .Returns(1);
        _transactions.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns(existing);
        _corrections.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns([]);

        Sut().SaveChanges(model);

        _transactions.Verify(x => x.Insert(
            It.Is<IEnumerable<BedReadinessTransactionDto>>(rows =>
                rows.Count() == 1 &&
                rows.Single().RequestId == "REQ-2")), Times.Once);
    }

    [Fact]
    public void SaveChanges_StaleVersion_ThrowsConcurrencyConflict()
    {
        var model = Ready();
        _header.Setup(x => x.GetData(It.IsAny<IBedOperationalKey>()))
            .Returns(BedOperationalDto.FromModel(model) with { Version = 4 });

        var act = () => Sut().SaveChanges(model);

        act.Should().Throw<BedOperationalPersistenceException>()
            .Which.Code.Should().Be("CONCURRENCY_CONFLICT");
        _header.Verify(x => x.UpdateConditional(
            It.IsAny<BedOperationalDto>(), It.IsAny<int>()), Times.Never);
        _transactions.VerifyNoOtherCalls();
        _corrections.VerifyNoOtherCalls();
    }

    [Fact]
    public void LoadEntity_ExistingHeader_ReconstructsAggregate()
    {
        var source = Corrected();
        _header.Setup(x => x.GetData(It.IsAny<IBedOperationalKey>()))
            .Returns(BedOperationalDto.FromModel(source));
        _transactions.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns(source.ListReadinessTransaction
                .Select(BedReadinessTransactionDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns(source.ListReadinessCorrection
                .Select(BedReadinessCorrectionDto.FromModel));

        var result = Sut().LoadEntity(BedOperationalModel.Key(source.BedId));

        result.HasValue.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(source);
    }

    [Fact]
    public void LoadEntity_MissingHeader_ReturnsNone()
    {
        var result = Sut().LoadEntity(BedOperationalModel.Key("BED00009"));

        result.HasValue.Should().BeFalse();
        _transactions.VerifyNoOtherCalls();
        _corrections.VerifyNoOtherCalls();
    }

    [Fact]
    public void LoadEntity_ContradictoryProjection_ThrowsIntegrityConflict()
    {
        var source = Cleaning();
        _header.Setup(x => x.GetData(It.IsAny<IBedOperationalKey>()))
            .Returns(BedOperationalDto.FromModel(source) with
            {
                CurrentReadiness = (int)BedReadinessStatusEnum.Ready
            });
        _transactions.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns(source.ListReadinessTransaction
                .Select(BedReadinessTransactionDto.FromModel));
        _corrections.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns([]);

        var act = () => Sut().LoadEntity(BedOperationalModel.Key(source.BedId));

        act.Should().Throw<BedOperationalPersistenceException>()
            .Which.Code.Should().Be("INTEGRITY_CONFLICT");
    }

    private BedOperationalRepo Sut() =>
        new(_header.Object, _transactions.Object, _corrections.Object);

    private void SetupEmptyDetails()
    {
        _transactions.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns([]);
        _corrections.Setup(x => x.ListData(It.IsAny<IBedOperationalKey>()))
            .Returns([]);
    }

    private static BedOperationalModel Create() =>
        BedOperationalModel.Create(
            "BED00001",
            "B1",
            "K001",
            new OccupancyPolicyReff("POL-1", "Standard"));

    private static BedOperationalModel Cleaning() =>
        Create().RecordReadiness(
            BedReadinessStatusEnum.CleaningRequired,
            BedRestrictionTypeEnum.Cleaning,
            "Cleaning required",
            "EVIDENCE-1",
            "PEG-1",
            At,
            At.AddMinutes(1),
            string.Empty,
            "REQ-1");

    private static BedOperationalModel Ready() =>
        Create().VerifyReady(
            "Ready verified",
            "EVIDENCE-1",
            "PEG-1",
            "PEG-2",
            At,
            At.AddMinutes(1),
            string.Empty,
            "REQ-1");

    private static BedOperationalModel Corrected()
    {
        var ready = Ready();
        return ready.CorrectReadiness(
            ready.ListReadinessTransaction.Single().TransactionId,
            BedReadinessStatusEnum.Blocked,
            BedRestrictionTypeEnum.Safety,
            "Safety restriction remains",
            "Ready fact was incorrect",
            "EVIDENCE-2",
            "PEG-HEAD",
            string.Empty,
            At.AddHours(1),
            At.AddHours(1).AddMinutes(1),
            "REQ-2");
    }
}
