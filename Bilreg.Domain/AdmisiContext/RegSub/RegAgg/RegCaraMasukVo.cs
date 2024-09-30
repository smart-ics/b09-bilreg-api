using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public record RegCaraMasukVo(
    string CaraMasukDkId,
    string CaraMasukDkName,
    string RujukanId,
    string RujukanName,
    string RujukanReffNo,
    DateTime RujukanDate,
    string IcdCode,
    string IcdName,
    string UraianDokter,
    string Anamnese)
{
    private const string CARAMASUK_DATANGSENDIRI_ID = "8";

    public static RegCaraMasukVo Create(CaraMasukDkModel caraMasuk, RujukanModel rujukan,
        string rujukanReffNo, DateTime rujukanDate, string icdCode, string icdName, 
        string uraianDokter, string anamnese)
    {
        //  GUARD
        Guard.IsNotNull(caraMasuk);
        Guard.IsNotEmpty(caraMasuk.CaraMasukDkName);
        
        //  Px Datang Sendiri harus tanpa rujukan
        //  Px Rujukan harus dengan rujukan
        var isDatangSendiri = caraMasuk.CaraMasukDkId == CARAMASUK_DATANGSENDIRI_ID;
        var isRujukanEmpty = rujukan is null || rujukan.RujukanId.Trim() == string.Empty;
        if (isDatangSendiri ^ isRujukanEmpty)
            throw new ArgumentException($"Cara Masuk and Rujukan is invalid");

        if (isDatangSendiri)
            return new RegCaraMasukVo(
                caraMasuk.CaraMasukDkId, caraMasuk.CaraMasukDkName,
                "", "", "", new DateTime(3000, 1, 1), "", "", "", "");
        
        if (rujukan?.CaraMasukDkId != caraMasuk.CaraMasukDkId)
            throw new ArgumentException("Rujukan and Cara Masuk are invalid");
        
        Guard.IsNotNull(rujukan);
        Guard.IsNotNullOrEmpty(rujukan.RujukanId);
        Guard.IsNotEmpty(rujukan.RujukanName);

        return new RegCaraMasukVo(
            caraMasuk.CaraMasukDkId, caraMasuk.CaraMasukDkName, rujukan.RujukanId,
            rujukan.RujukanName, rujukanReffNo, rujukanDate, icdCode, icdName,
            uraianDokter, anamnese);
    }

    protected static RegCaraMasukVo Load(string caraMasukDkId, string caraMasukDkName,
        string rujukanId, string rujukanName, string rujukanReffNo, DateTime rujukanDate,
        string icdCode, string icdName, string uraianDokter, string anamnese)
    {
        return new RegCaraMasukVo(
            caraMasukDkId, caraMasukDkName, rujukanId, rujukanName, rujukanReffNo, rujukanDate,
            icdCode, icdName, uraianDokter, anamnese);
    }
}

public class RegCaraMasukVoTest
{
    private RegCaraMasukVo _sut;

    [Fact]
    public void GivenDatangSendiri_AndRujukanEmpty_WhenCreate_ThenSuccess()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("8", "A");
        _sut = RegCaraMasukVo.Create(caraMasuk, null!, "", new DateTime(3000, 1, 1), "", "", "", "");
    }
    [Fact]
    public void GivenDatangSendiri_ButRujukanNotEmpty_WhenCreate_ThenThrowError()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("8", "A");
        var rujukan = RujukanModel.Create("B", "B1");
        var actual = () => _sut = RegCaraMasukVo.Create(caraMasuk, rujukan, "", new DateTime(3000, 1, 1), "", "", "", "");
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenCaraMasukRujukan_AndRujukanNotEmpty_WhenCreate_ThenSuccess()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("1", "A");
        var rujukan = RujukanModel.Create("B", "B1");
        rujukan.SetCaraMasukDk(CaraMasukDkModel.Create("1", ""));
        _sut = RegCaraMasukVo.Create(caraMasuk, rujukan, "", new DateTime(3000, 1, 1), "", "", "", "");
    }

    [Fact]
    public void GivenCaraMasukRujukan_ButRujukanEmpty_WhenCreate_ThenThrowError()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("1", "A");
        var actual = () => _sut = RegCaraMasukVo.Create(caraMasuk, null!, "", new DateTime(3000, 1, 1), "", "", "", "");
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenCaraMasukRujukan_ButRujukanHasDifferentCaraMasuk_WhenCreate_ThenThrowError()
    {
        //  Arrange
        var caraMasuk = CaraMasukDkModel.Create("1", "A");
        var rujukan = RujukanModel.Create("B", "B1");
        rujukan.SetCaraMasukDk(CaraMasukDkModel.Create("2", ""));
        var actual = () => _sut = RegCaraMasukVo.Create(caraMasuk, rujukan, "", new DateTime(3000, 1, 1), "", "", "", "");
        actual.Should().Throw<ArgumentException>();
    }
}