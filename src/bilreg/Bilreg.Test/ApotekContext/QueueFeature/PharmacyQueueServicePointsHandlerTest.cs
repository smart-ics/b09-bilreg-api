using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Domain.ApotekContext.QueueFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueServicePointsHandlerTest
{
    private readonly Mock<IPharmacyServicePointRepo> _repoMock = new();
    private readonly PharmacyQueueServicePointsHandler _sut;

    public PharmacyQueueServicePointsHandlerTest()
    {
        _sut = new PharmacyQueueServicePointsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_WithoutDepo_CallsListActive_AndMapsResponses()
    {
        // Arrange
        var list = new List<PharmacyServicePointModel>
        {
            PharmacyServicePointModel.Rehydrate("SP01", "DEPO01", "Non Racikan", "A", true),
            PharmacyServicePointModel.Rehydrate("SP02", "DEPO01", "Racikan", "B", true)
        };
        _repoMock.Setup(r => r.ListActive()).Returns(list);

        // Act
        var result = await _sut.Handle(new PharmacyQueueServicePointsQry(), default);

        // Assert
        result.Should().HaveCount(2);
        result[0].ServicePointId.Should().Be("SP01");
        result[0].DisplayName.Should().Be("Non Racikan");
        result[0].QueuePrefix.Should().Be("A");
        result[0].Active.Should().BeTrue();
        _repoMock.Verify(r => r.ListActive(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDepo_CallsListByDepo_AndFiltersActive()
    {
        // Arrange
        var list = new List<PharmacyServicePointModel>
        {
            PharmacyServicePointModel.Rehydrate("SP01", "DEPO01", "Non Racikan", "A", true),
            PharmacyServicePointModel.Rehydrate("SP03", "DEPO01", "Layanan Tutup", "D", false)
        };
        _repoMock.Setup(r => r.ListByDepo("DEPO01")).Returns(list);

        // Act
        var resultActiveOnly = await _sut.Handle(new PharmacyQueueServicePointsQry("DEPO01", ActiveOnly: true), default);
        var resultAll = await _sut.Handle(new PharmacyQueueServicePointsQry("DEPO01", ActiveOnly: false), default);

        // Assert
        resultActiveOnly.Should().ContainSingle();
        resultActiveOnly[0].ServicePointId.Should().Be("SP01");

        resultAll.Should().HaveCount(2);
        _repoMock.Verify(r => r.ListByDepo("DEPO01"), Times.Exactly(2));
    }
}
