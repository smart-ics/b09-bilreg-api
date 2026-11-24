using Bilreg.Domain.ApotekContext.QueueFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class ApotekLoketServicePointModelTest
{
    [Fact]
    public void Create_initializes_model_with_normalized_values()
    {
        var model = ApotekLoketServicePointModel.Create("  APT-RCV-01  ", "  SP-FAR-BPJS  ", " admin ");

        model.LoketKey.Should().Be("APT-RCV-01");
        model.ServicePointId.Should().Be("SP-FAR-BPJS");
        model.CrtUser.Should().Be("admin");
        model.CrtDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_defaults_actor_to_system_when_empty()
    {
        var model = ApotekLoketServicePointModel.Create("APT-RCV-01", "SP-FAR-BPJS", "");

        model.CrtUser.Should().Be("SYSTEM");
    }

    [Fact]
    public void Create_throws_when_required_fields_missing()
    {
        Action blankLoket = () => ApotekLoketServicePointModel.Create("", "SP-FAR-BPJS");
        blankLoket.Should().Throw<ArgumentException>().WithParameterName("loketKey");

        Action blankServicePoint = () => ApotekLoketServicePointModel.Create("APT-RCV-01", "   ");
        blankServicePoint.Should().Throw<ArgumentException>().WithParameterName("servicePointId");
    }

    [Fact]
    public void Rehydrate_restores_all_properties_including_audit()
    {
        var fixedDate = new DateTime(2026, 10, 26, 8, 30, 0);
        var model = ApotekLoketServicePointModel.Rehydrate("APT-RCV-01", "SP-FAR-UMUM", "SUPERVISOR", fixedDate);

        model.LoketKey.Should().Be("APT-RCV-01");
        model.ServicePointId.Should().Be("SP-FAR-UMUM");
        model.CrtUser.Should().Be("SUPERVISOR");
        model.CrtDate.Should().Be(fixedDate);
    }

    [Fact]
    public void Equality_and_HashCode_based_on_loketKey_and_servicePointId_case_insensitive()
    {
        var model1 = ApotekLoketServicePointModel.Create("apt-rcv-01", "sp-far-bpjs");
        var model2 = ApotekLoketServicePointModel.Create("APT-RCV-01", "SP-FAR-BPJS");
        var model3 = ApotekLoketServicePointModel.Create("APT-RCV-02", "SP-FAR-BPJS");

        model1.Should().Be(model2);
        model1.GetHashCode().Should().Be(model2.GetHashCode());
        model1.Should().NotBe(model3);
    }
}
