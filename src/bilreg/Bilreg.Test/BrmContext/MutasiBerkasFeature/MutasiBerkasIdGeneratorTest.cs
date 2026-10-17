using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.AutoNumberHelper;
using Xunit;

namespace Bilreg.Test.BrmContext.MutasiBerkasFeature;

public class MutasiBerkasIdGeneratorTest
{
    private readonly Mock<INunaCounterDecDal> _counterDalMock = new();

    [Theory]
    [InlineData(MutasiBerkasStatusEnum.Dispatched, "MK")]
    [InlineData(MutasiBerkasStatusEnum.Received, "MP")]
    [InlineData(MutasiBerkasStatusEnum.Returned, "MM")]
    [InlineData(MutasiBerkasStatusEnum.Missing, "MH")]
    public void Generate_ShouldReturnCorrectPrefixAnd10CharactersLength(MutasiBerkasStatusEnum status, string expectedPrefix)
    {
        // Arrange
        _counterDalMock.Setup(x => x.GetNewDecNumber(expectedPrefix)).Returns(12345);
        var sut = new MutasiBerkasIdGenerator(_counterDalMock.Object);

        // Act
        var result = sut.Generate(status);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Length.Should().Be(10);
        result.Should().StartWith(expectedPrefix);
        result.Should().Be($"{expectedPrefix}00012346");
    }

    [Fact]
    public void Generate_WhenCounterIsNew_ShouldInsertSequence1AndReturnFormattedId()
    {
        // Arrange
        _counterDalMock.Setup(x => x.GetNewDecNumber("MK")).Returns(0);
        var sut = new MutasiBerkasIdGenerator(_counterDalMock.Object);

        // Act
        var result = sut.Generate(MutasiBerkasStatusEnum.Dispatched);

        // Assert
        result.Should().Be("MK00000001");
        result.Length.Should().Be(10);
        _counterDalMock.Verify(x => x.InsertNewDecNumber("MK", 1), Times.Once);
        _counterDalMock.Verify(x => x.UpdateNewDecNumber(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public void Generate_WhenCounterExists_ShouldUpdateSequenceAndReturnFormattedId()
    {
        // Arrange
        _counterDalMock.Setup(x => x.GetNewDecNumber("MP")).Returns(99);
        var sut = new MutasiBerkasIdGenerator(_counterDalMock.Object);

        // Act
        var result = sut.Generate(MutasiBerkasStatusEnum.Received);

        // Assert
        result.Should().Be("MP00000100");
        result.Length.Should().Be(10);
        _counterDalMock.Verify(x => x.UpdateNewDecNumber("MP", 100), Times.Once);
        _counterDalMock.Verify(x => x.InsertNewDecNumber(It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public void Generate_WhenCounterDalIsNull_ShouldFallbackToGeneratedSequenceAndMaintain10Chars()
    {
        // Arrange
        var sut = new MutasiBerkasIdGenerator(null);

        // Act
        var result = sut.Generate(MutasiBerkasStatusEnum.Returned);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Length.Should().Be(10);
        result.Should().StartWith("MM");
    }

    [Fact]
    public void Generate_WhenCounterDalThrows_ShouldCatchAndFallbackGracefully()
    {
        // Arrange
        _counterDalMock.Setup(x => x.GetNewDecNumber("MH")).Throws(new InvalidOperationException("DB unavailable"));
        var sut = new MutasiBerkasIdGenerator(_counterDalMock.Object);

        // Act
        var result = sut.Generate(MutasiBerkasStatusEnum.Missing);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Length.Should().Be(10);
        result.Should().StartWith("MH");
    }

    [Theory]
    [InlineData(MutasiBerkasStatusEnum.Permintaan)]
    [InlineData(MutasiBerkasStatusEnum.Void)]
    public void Generate_WhenStatusNotSupported_ShouldThrowArgumentOutOfRangeException(MutasiBerkasStatusEnum unsupportedStatus)
    {
        // Arrange
        var sut = new MutasiBerkasIdGenerator(_counterDalMock.Object);

        // Act
        var act = () => sut.Generate(unsupportedStatus);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("status");
    }
}
