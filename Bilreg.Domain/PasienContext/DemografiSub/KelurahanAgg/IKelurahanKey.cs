using CommunityToolkit.Diagnostics;
using Xunit;

namespace Bilreg.Domain.PasienContext.DemografiSub.KelurahanAgg;

public interface IKelurahanKey
{
    string KelurahanId { get; }
}

public class KelurahanKey : IKelurahanKey
{
    public KelurahanKey(string kelurahanId)
    {
        Guard.IsNotNullOrWhiteSpace(kelurahanId);
        KelurahanId = kelurahanId;
    }

    public string KelurahanId { get; }
}

public class KelurahanKeyTests
{
    [Fact]
    public void Constructor_WithValidKelurahanId_SetsKelurahanIdProperty()
    {
        // Arrange
        var id = "A";

        // Act
        var key = new KelurahanKey(id);

        // Assert
        Assert.Equal(id, key.KelurahanId);
    }

    [Theory]
    [InlineData(null, "ArgumentNullException")]
    [InlineData("", "ArgumentException")]
    [InlineData("  ", "ArgumentException")]
    public void Constructor_WithIncorrectKelurahanId_ThrowEx(string invalidId, string ex)
    {
        switch (ex)
        {
            case "ArgumentNullException":
                Assert.Throws<ArgumentNullException>(() => new KelurahanKey(invalidId));
                break;
            case "ArgumentException":
                Assert.Throws<ArgumentException>(() => new KelurahanKey(invalidId));
                break;
        }
    }
}