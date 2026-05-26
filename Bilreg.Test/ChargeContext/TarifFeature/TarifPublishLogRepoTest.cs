using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TarifPublishLogRepoTest
{
    private readonly Mock<ITarifPublishLogDal> _logDalMock = new();
    private readonly Mock<ITarifPublishLogDetailDal> _detailDalMock = new();
    private readonly TarifPublishLogRepo _repository;

    public TarifPublishLogRepoTest()
    {
        _repository = new TarifPublishLogRepo(_logDalMock.Object, _detailDalMock.Object);
    }

    [Fact]
    public void UT1_GivenLogWithDetails_WhenInsert_ThenPersistsHeaderAndDetails()
    {
        var log = new TarifPublishLogType(
            "LOG001",
            "POL001",
            "user1",
            DateTime.Now,
            1,
            "note",
            [new TarifPublishLogDetailType(1, "T01", "K1", "01", "NT001", 100m)]);

        _repository.Insert(log);

        _logDalMock.Verify(x => x.Insert(It.IsAny<TarifPublishLogDto>()), Times.Once);
        _detailDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TarifPublishLogDetailDto>>()), Times.Once);
    }

    [Fact]
    public void UT2_GivenStoredLog_WhenLoadEntity_ThenReturnsAggregate()
    {
        var log = new TarifPublishLogType(
            "LOG001", "POL001", "user1", DateTime.Now, 1, "note",
            [new TarifPublishLogDetailType(1, "T01", "K1", "01", "NT001", 100m)]);
        var headerDto = TarifPublishLogDto.FromModel(log);
        var detailDto = TarifPublishLogDetailDto.FromModel(log.PublishLogId, log.Details.First());

        _logDalMock.Setup(x => x.GetData(It.IsAny<ITarifPublishLogKey>())).Returns(headerDto);
        _detailDalMock.Setup(x => x.ListData(It.IsAny<ITarifPublishLogKey>())).Returns([detailDto]);

        var result = _repository.LoadEntity(TarifPublishLogType.Key("LOG001"));

        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: loaded => loaded.Details.Should().HaveCount(1),
            onNone: () => Assert.Fail("Expected log"));
    }
}
