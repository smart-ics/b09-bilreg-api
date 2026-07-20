using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;
using Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.BedUsageContext.RnaServiceExecutionFeature;

public class RnaServiceExecutionRepoTest
{
    private readonly Mock<IRnaServiceExecutionDal> _header = new();
    private readonly Mock<IServiceWorkSourceRevisionDal> _revisions = new();
    private readonly Mock<IServiceExecutionFactDal> _facts = new();
    private readonly Mock<IExecutionCorrectionDal> _corrections = new();

    [Fact]
    public void SaveChanges_NewAggregate_InsertsHeaderAndFact()
    {
        var model = Executed(); SetupDetails();

        Sut().SaveChanges(model);

        _header.Verify(x => x.Insert(It.Is<RnaServiceExecutionDto>(d => d.ServiceExecutionId == model.ServiceExecutionId)), Times.Once);
        _facts.Verify(x => x.Insert(It.Is<IEnumerable<ServiceExecutionFactDto>>(x => x.Count() == 1)), Times.Once);
    }

    [Fact]
    public void LoadEntity_ReconstructsFactHistory()
    {
        var source = Executed();
        _header.Setup(x => x.GetData(It.IsAny<IRnaServiceExecutionKey>())).Returns(RnaServiceExecutionDto.FromModel(source));
        _revisions.Setup(x => x.ListData(It.IsAny<IRnaServiceExecutionKey>())).Returns([]);
        _facts.Setup(x => x.ListData(It.IsAny<IRnaServiceExecutionKey>())).Returns(source.ListExecutionFact.Select(x => ServiceExecutionFactDto.FromModel(source.ServiceExecutionId, x)));
        _corrections.Setup(x => x.ListData(It.IsAny<IRnaServiceExecutionKey>())).Returns([]);

        var actual = Sut().LoadEntity(RnaServiceExecutionModel.Key(source.ServiceExecutionId));

        actual.HasValue.Should().BeTrue();
        actual.Value.CurrentExecutionFact!.ServiceExecutionFactId.Should().Be(source.CurrentExecutionFact!.ServiceExecutionFactId);
    }

    private void SetupDetails()
    {
        _revisions.Setup(x => x.ListData(It.IsAny<IRnaServiceExecutionKey>())).Returns([]);
        _facts.Setup(x => x.ListData(It.IsAny<IRnaServiceExecutionKey>())).Returns([]);
        _corrections.Setup(x => x.ListData(It.IsAny<IRnaServiceExecutionKey>())).Returns([]);
    }

    private RnaServiceExecutionRepo Sut() => new(_header.Object, _revisions.Object, _facts.Object, _corrections.Object);
    private static RnaServiceExecutionModel Executed()
    {
        var at = new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);
        return RnaServiceExecutionModel.CreateOrdered("RG1", "P1", "C1", "W1", "O1", "OC1", "OB1", "CPOE", "SF1", 1)
            .RecordExecution(BillableClassificationEnum.NonBillable, null, "Edukasi", "PEG1", at, at, "PEG1", null);
    }
}
