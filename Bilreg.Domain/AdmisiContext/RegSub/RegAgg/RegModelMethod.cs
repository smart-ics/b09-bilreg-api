using Bilreg.Domain.AdmisiContext.RegSub.RegAgg.ValueObjects;
using CommunityToolkit.Diagnostics;
using FluentAssertions;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.RegSub.RegAgg;

public partial class RegModel
{
}

public class RegModelTest()
{
    [Fact]
    public void T01_GivenVoidReg_WhenSetTglJamTest_ThenThrowEx()
    {
        //  ARRANGE
        var reg = new RegModel("A");
        reg.SetVoidFlag(new ActivityFlagVo(DateTime.Now, "B"));
        //  ACT
        var actual = () => reg.SetTglJamTrs(new TglJamTrsVo(DateTime.Now, "C"));
        //  ASSERT
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T02_GivenVoidReg_WhenSetPasien_ThenThrowEx()
    {
        //  ARRANGE
        var reg = new RegModel("A");
        reg.SetVoidFlag(new ActivityFlagVo(DateTime.Now, "B"));
        var px = new RegPasienVo("C1", "C2", "C3", DateTime.Now, "L");
        //  ACT
        var actual = () => reg.SetPasien(px);
        //  ASSERT
        actual.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T03_GivenVoidReg_WhenSetJaminan_ThenThrowEx()
    {
        //  ARRANGE
        var reg = new RegModel("A");
        reg.SetVoidFlag(new ActivityFlagVo(DateTime.Now, "B"));
        var tipeJmn = new RegTipeJaminanVo("C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8");
        //  ACT
        var actual = () => reg.SetJaminan(tipeJmn);
        //  ASSERT
        actual.Should().Throw<ArgumentException>();
    }
    
    
    [Fact]
    public void T04_GivenVoidReg_WhenSetCaraMasuk_ThenThrowEx()
    {
        //  ARRANGE
        var reg = new RegModel("A");
        reg.SetVoidFlag(new ActivityFlagVo(DateTime.Now, "B"));
        var caraMasuk = new RegCaraMasukVo("C1", "C2", "C3", "C4");
        //  ACT
        var actual = () => reg.SetCaraMasuk(caraMasuk);
        //  ASSERT
        actual.Should().Throw<ArgumentException>();
    }

}