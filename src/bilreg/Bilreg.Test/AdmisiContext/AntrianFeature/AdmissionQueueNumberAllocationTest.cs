using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionQueueNumberAllocationTest
{
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly DateTime _createdAt = new(2026, 7, 23, 8, 0, 0);

    [Fact]
    public void FirstAllocation_UsesCanonicalTagAndAdmissionBound()
    {
        var queue = CreateQueue("BPJS", new DateOnly(2026, 7, 23));
        _sequencer.Setup(x => x.GetNextNoUrut(
                "AN2607230000_BPJS", AntrianModel.AdmissionQueueNumberMax))
            .Returns(1);

        var entry = queue.AddAdmissionEntry(_createdAt);

        entry.NoUrut.Should().Be(1);
        _sequencer.Verify(x => x.GetNextNoUrut(
            "AN2607230000_BPJS", 9999), Times.Once);
        _sequencer.Verify(x => x.GetNextNoUrut(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Allocation9999_IsAccepted()
    {
        var queue = CreateQueue("UMUM", new DateOnly(2026, 7, 23));
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>(), 9999)).Returns(9999);

        queue.AddAdmissionEntry(_createdAt).NoUrut.Should().Be(9999);
    }

    [Fact]
    public void AllocationAbove9999_IsRejectedDefensively()
    {
        var queue = CreateQueue("UMUM", new DateOnly(2026, 7, 23));
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>(), 9999)).Returns(10000);

        var act = () => queue.AddAdmissionEntry(_createdAt);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*outside the supported range 1-9999*");
    }

    [Fact]
    public void SequenceGap_IsAccepted()
    {
        var queue = CreateQueue("UMUM", new DateOnly(2026, 7, 23));
        _sequencer.Setup(x => x.GetNextNoUrut(It.IsAny<string>(), 9999)).Returns(37);

        queue.AddAdmissionEntry(_createdAt).NoUrut.Should().Be(37);
    }

    [Fact]
    public void ServicePointAndBusinessDate_ProduceSeparateCanonicalTags()
    {
        CreateQueue("BPJS", new DateOnly(2026, 7, 23)).SequenceTag
            .Should().Be("AN2607230000_BPJS");
        CreateQueue("UMUM", new DateOnly(2026, 7, 23)).SequenceTag
            .Should().Be("AN2607230000_UMUM");
        CreateQueue("BPJS", new DateOnly(2026, 7, 24)).SequenceTag
            .Should().Be("AN2607240000_BPJS");
    }

    private AntrianModel CreateQueue(string servicePointCode, DateOnly businessDate)
    {
        var factory = new AntrianFactory(_sequencer.Object);
        return factory.Create(
            new ServicePointType(servicePointCode, servicePointCode), businessDate);
    }
}
