using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianEntryModelTest
{
    [Fact]
    public void T01_GivenNotServe_WhenDone_ThrowException()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"));
        var actual = () => entry.Done();
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T02_GivenServed_WhenDone_ThenSuccess()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"));
        entry.Serve();
        var actual = () => entry.Done();
        actual.Should().NotThrow<ArgumentException>();
    }
}
