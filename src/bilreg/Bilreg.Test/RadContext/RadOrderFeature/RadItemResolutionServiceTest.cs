using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Infrastructure.RadContext.RadOrderFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadItemResolutionServiceTest
{
    private readonly Mock<IRadTarifModalityDal> _dalMock;
    private readonly RadItemResolutionService _sut;

    public RadItemResolutionServiceTest()
    {
        _dalMock = new Mock<IRadTarifModalityDal>();
        _sut = new RadItemResolutionService(_dalMock.Object);
    }

    [Fact]
    public void ResolveItems_WhenEmptyItems_ReturnsFailure()
    {
        // Act
        var result = _sut.ResolveItems([], "CR");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("tidak boleh kosong");
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void ResolveItems_WhenNullItems_ReturnsFailure()
    {
        // Act
        var result = _sut.ResolveItems(null!, "CR");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("tidak boleh kosong");
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void ResolveItems_WhenTarifIdEmpty_ReturnsFailure()
    {
        // Act
        var result = _sut.ResolveItems([new RadOrderItemInputDto(string.Empty, 0)], "CR");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("tidak boleh kosong");
    }

    [Fact]
    public void ResolveItems_WhenTarifNotFound_ReturnsFailure()
    {
        // Arrange
        _dalMock.Setup(x => x.GetData("TRF01")).Returns((RadTarifModalityDto?)null);

        // Act
        var result = _sut.ResolveItems([new RadOrderItemInputDto("TRF01", 0)], "CR");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Tindakan TRF01 tidak ditemukan atau tidak aktif di katalog radiologi.");
    }

    [Fact]
    public void ResolveItems_WhenTarifNotActive_ReturnsFailure()
    {
        // Arrange
        var inactiveDto = new RadTarifModalityDto(
            "TRF01", "Rontgen Thorax", "CR", false,
            "admin", DateTime.Now, "admin", DateTime.Now);
        _dalMock.Setup(x => x.GetData("TRF01")).Returns(inactiveDto);

        // Act
        var result = _sut.ResolveItems([new RadOrderItemInputDto("TRF01", 0)], "CR");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Tindakan TRF01 tidak ditemukan atau tidak aktif di katalog radiologi.");
    }

    [Fact]
    public void ResolveItems_WhenModalityMismatch_ReturnsFailure()
    {
        // Arrange
        var ctDto = new RadTarifModalityDto(
            "TRF02", "CT Scan Kepala", "CT", true,
            "admin", DateTime.Now, "admin", DateTime.Now);
        _dalMock.Setup(x => x.GetData("TRF02")).Returns(ctDto);

        // Act
        var result = _sut.ResolveItems([new RadOrderItemInputDto("TRF02", 0)], "CR");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Tindakan TRF02 memiliki modalitas 'CT' yang berbeda dari modalitas order 'CR'.");
    }

    [Fact]
    public void ResolveItems_WhenValidItems_ReturnsSuccessWithSequentialItemNoAndCorrectModalityAndUrgency()
    {
        // Arrange
        var item1Dto = new RadTarifModalityDto(
            "TRF01", "Rontgen Thorax PA", "CR", true,
            "admin", DateTime.Now, "admin", DateTime.Now);
        var item2Dto = new RadTarifModalityDto(
            "TRF02", "Rontgen Abdomen", "CR", true,
            "admin", DateTime.Now, "admin", DateTime.Now);

        _dalMock.Setup(x => x.GetData("TRF01")).Returns(item1Dto);
        _dalMock.Setup(x => x.GetData("TRF02")).Returns(item2Dto);

        var inputs = new List<RadOrderItemInputDto>
        {
            new("TRF01", 0),
            new("TRF02", 1)
        };

        // Act
        var result = _sut.ResolveItems(inputs, "CR");

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
        result.Modality.Should().Be("CR");
        result.Items.Should().HaveCount(2);

        result.Items[0].ItemNo.Should().Be(1);
        result.Items[0].TarifId.Should().Be("TRF01");
        result.Items[0].TarifName.Should().Be("Rontgen Thorax PA");
        result.Items[0].Modality.Should().Be(RadModalityEnum.CR);
        result.Items[0].Urgency.Should().Be(RadUrgencyEnum.Rutin);

        result.Items[1].ItemNo.Should().Be(2);
        result.Items[1].TarifId.Should().Be("TRF02");
        result.Items[1].TarifName.Should().Be("Rontgen Abdomen");
        result.Items[1].Modality.Should().Be(RadModalityEnum.CR);
        result.Items[1].Urgency.Should().Be(RadUrgencyEnum.Cito);
    }
}
