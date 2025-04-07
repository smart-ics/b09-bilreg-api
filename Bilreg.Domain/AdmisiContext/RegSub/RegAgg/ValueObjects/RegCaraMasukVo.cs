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