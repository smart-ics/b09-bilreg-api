using Bilreg.Domain.RadContext.RadOrderFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadOrderChildModelsTest
{
    [Fact]
    public void RadModalityEnum_HasExpectedDicomValues()
    {
        ((int)RadModalityEnum.CR).Should().Be(1);
        ((int)RadModalityEnum.DX).Should().Be(2);
        ((int)RadModalityEnum.CT).Should().Be(3);
        ((int)RadModalityEnum.MR).Should().Be(4);
        ((int)RadModalityEnum.US).Should().Be(5);
    }

    [Fact]
    public void RadOrderStatusEnum_HasExpectedValues()
    {
        ((int)RadOrderStatusEnum.Requested).Should().Be(1);
        ((int)RadOrderStatusEnum.Scheduled).Should().Be(2);
        ((int)RadOrderStatusEnum.Cancelled).Should().Be(3);
    }

    [Fact]
    public void RadUrgencyEnum_HasExpectedValues()
    {
        ((int)RadUrgencyEnum.Rutin).Should().Be(0);
        ((int)RadUrgencyEnum.Cito).Should().Be(1);
    }

    [Fact]
    public void RadOrderSourceEnum_HasExpectedValues()
    {
        ((int)RadOrderSourceEnum.Internal).Should().Be(0);
        ((int)RadOrderSourceEnum.External).Should().Be(1);
    }

    [Fact]
    public void RadOrderItemModel_SetsPropertiesCorrectly()
    {
        var item = new RadOrderItemModel("RDO001", 1, "TRF01", "Rontgen Thorax", RadModalityEnum.CR, RadUrgencyEnum.Rutin);

        item.OrderId.Should().Be("RDO001");
        item.ItemNo.Should().Be(1);
        item.TarifId.Should().Be("TRF01");
        item.TarifName.Should().Be("Rontgen Thorax");
        item.Modality.Should().Be(RadModalityEnum.CR);
        item.Urgency.Should().Be(RadUrgencyEnum.Rutin);
        item.IsCito.Should().BeFalse();
    }

    [Fact]
    public void RadOrderItemModel_IsCito_ReturnsTrueWhenUrgencyIsCito()
    {
        var item = RadOrderItemModel.Create("RDO001", 1, "TRF01", "CT Brain Cito", RadModalityEnum.CT, RadUrgencyEnum.Cito);

        item.IsCito.Should().BeTrue();
    }

    [Fact]
    public void RadOrderItemModel_CreateWithoutOrderId_DefaultsCorrectly()
    {
        var item = RadOrderItemModel.Create("TRF01", "USG Abdomen", RadModalityEnum.US);

        item.OrderId.Should().BeEmpty();
        item.ItemNo.Should().Be(0);
        item.TarifId.Should().Be("TRF01");
        item.TarifName.Should().Be("USG Abdomen");
        item.Modality.Should().Be(RadModalityEnum.US);
        item.Urgency.Should().Be(RadUrgencyEnum.Rutin);
        item.IsCito.Should().BeFalse();
    }

    [Fact]
    public void RadTarifModalityModel_SetsPropertiesCorrectly()
    {
        var model = new RadTarifModalityModel("TRF01", "MRI Lumbal", RadModalityEnum.MR, true);

        model.TarifId.Should().Be("TRF01");
        model.TarifName.Should().Be("MRI Lumbal");
        model.Modality.Should().Be(RadModalityEnum.MR);
        model.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RadTarifModalityModel_Create_SetsPropertiesWithDefaults()
    {
        var model = RadTarifModalityModel.Create("TRF02", "Thorax AP/Lat", RadModalityEnum.DX);

        model.TarifId.Should().Be("TRF02");
        model.TarifName.Should().Be("Thorax AP/Lat");
        model.Modality.Should().Be(RadModalityEnum.DX);
        model.IsActive.Should().BeTrue();
    }
}
