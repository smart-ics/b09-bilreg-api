using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionServicePointModelTest
{
    [Theory]
    [InlineData("a", "A")]
    [InlineData(" Z ", "Z")]
    public void Create_NormalizesPrefix(string input, string expected) =>
        AdmissionServicePointModel.Create("ADM", "Admission", input).QueuePrefix.Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("AA")]
    [InlineData("1")]
    [InlineData("Å")]
    public void Create_RejectsInvalidPrefix(string input) =>
        FluentActions.Invoking(() => AdmissionServicePointModel.Create("ADM", "Admission", input))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void QueueSession_SnapshotsPrefixAndFormatsFullRange()
    {
        var sequencer = new Mock<ISequencer>();
        var master = AdmissionServicePointModel.Create("ADM", "Admission", "a");
        var queue = new AntrianFactory(sequencer.Object).Create(master, new DateOnly(2026, 7, 23));

        queue.QueuePrefixSnapshot.Should().Be("A");
        queue.FormatQueueLabel(1).Should().Be("A0001");
        queue.FormatQueueLabel(9999).Should().Be("A9999");
        queue.Invoking(x => x.FormatQueueLabel(10000)).Should().Throw<ArgumentOutOfRangeException>();
        master.ChangePrefix("B").QueuePrefix.Should().Be("B");
        queue.QueuePrefixSnapshot.Should().Be("A");
    }

    [Fact]
    public void HistoricalSession_WithoutSnapshot_HasNoFabricatedLabel()
    {
        var queue = new AntrianModel("id", new DateOnly(2026, 7, 23), TimeOnly.MinValue,
            TimeOnly.MaxValue, "tag", "legacy", ServicePointType.Default, [], Mock.Of<ISequencer>());
        queue.FormatQueueLabel(27).Should().BeNull();
    }
}
