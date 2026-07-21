using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianEntryModelTest
{
    [Fact]
    public void T01_GivenNotServe_WhenDone_ThrowException()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B");
        var actual = () => entry.Done();
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T02_GivenServed_WhenDone_ThenSuccess()
    {
        var entry = AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Key("-"), "A", "B",
            new DateTime(2025, 8, 3, 6, 51, 0));
        entry.Serve(new DateTime(2025, 8, 3, 7, 0, 0));
        var actual = () => entry.Done(new DateTime(2025, 8, 3, 7, 30, 0));
        actual.Should().NotThrow<ArgumentException>();
    }
}
