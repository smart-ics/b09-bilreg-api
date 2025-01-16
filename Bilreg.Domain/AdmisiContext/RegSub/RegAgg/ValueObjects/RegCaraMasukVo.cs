using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;
using ArgumentException = System.ArgumentException;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;

public record RegCaraMasukVo
{
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";

    public string CaraMasukDkId { get; }
    public string CaraMasukDkName { get; }
    public string RujukanId { get; }
    public string RujukanName { get; }
    
    public RegCaraMasukVo (CaraMasukDkModel caraMasuk, RujukanModel? rujukan) 
    {
        CaraMasukDkId = caraMasuk.CaraMasukDkId;
        CaraMasukDkName = caraMasuk.CaraMasukDkName;
        RujukanId = rujukan?.RujukanId??string.Empty;
        RujukanName = rujukan?.RujukanName??string.Empty;
        
        Validate();
        
        if (CaraMasukDkId != CARAMASUK_DATANGSENDIRI_ID)
            Guard.IsTrue(rujukan?.CaraMasukDkId == CaraMasukDkId);
    }

    public RegCaraMasukVo(string caraMasukDkId, string caraMasukDkName, string rujukanId, string rujukanName) 
    {
        CaraMasukDkId = caraMasukDkId;
        CaraMasukDkName = caraMasukDkName;
        RujukanId = rujukanId;
        RujukanName = rujukanName;

        Validate();
    }

    private void Validate()
    {
        Guard.IsNotNullOrEmpty(CaraMasukDkName);
        Guard.IsNotNullOrEmpty(CaraMasukDkName);

        if (CaraMasukDkId == CARAMASUK_DATANGSENDIRI_ID)
            ValidateDatangSendiri();
        else
            ValidateRujukan();
    }

    private void ValidateDatangSendiri()
    {
        Guard.IsNullOrEmpty(RujukanId);
        Guard.IsNullOrEmpty(RujukanName);
    }

    private void ValidateRujukan()
    {
        Guard.IsNotNullOrEmpty(RujukanId);
        Guard.IsNotNullOrEmpty(RujukanName);
    }
}

public class RegCaraMasukVoTest
{
    private RegCaraMasukVo _sut = null!;

    [Fact]
    public void T01_GivenDatangSendiri_AndRujukanEmpty_WhenCreate_ThenSuccess()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("8", "A");
        _sut = new RegCaraMasukVo(caraMasuk, null!);
    }
    [Fact]
    public void T02_GivenDatangSendiri_ButRujukanNotEmpty_WhenCreate_ThenThrowError()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("8", "A");
        var rujukan = RujukanModel.Create("B", "B1");
        var actual = () => _sut = new RegCaraMasukVo(caraMasuk, rujukan);
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T03_GivenCaraMasukRujukan_AndRujukanNotEmpty_WhenCreate_ThenSuccess()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("1", "A");
        var rujukan = RujukanModel.Create("B", "B1");
        rujukan.SetCaraMasukDk(CaraMasukDkModel.Create("1", ""));
        _sut = new RegCaraMasukVo(caraMasuk, rujukan);
    }

    [Fact]
    public void T04_GivenCaraMasukRujukan_ButRujukanEmpty_WhenCreate_ThenThrowError()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("1", "A");
        var actual = () => _sut = new RegCaraMasukVo(caraMasuk, null!);
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T05_GivenCaraMasukRujukan_ButRujukanHasDifferentCaraMasuk_WhenCreate_ThenThrowError()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("1", "A");
        var rujukan = RujukanModel.Create("B", "B1");
        rujukan.SetCaraMasukDk(CaraMasukDkModel.Create("2", ""));
        var actual = () => _sut = new RegCaraMasukVo(caraMasuk, rujukan);
        actual.Should().Throw<ArgumentException>();
    }
}