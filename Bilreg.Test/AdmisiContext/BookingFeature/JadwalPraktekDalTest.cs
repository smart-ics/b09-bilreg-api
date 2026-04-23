using System.Diagnostics;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekDalTest
{
    private readonly JadwalPraktekDal _sut;

    public JadwalPraktekDalTest()
    {
        _sut = new JadwalPraktekDal(ConnStringHelper.GetTestEnv());
    }

    private static JadwalPraktekDto Faker() =>
        new("A", "B", "C", "J", 5, "08:00", "10:00", 20, "","", "D", "E", "F", "G", "H", "I", "J");
    private static IJadwalPraktekKey Key => JadwalPraktekType.Key("A"); 
    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }
    
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(Key);
    }
    
    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(Key);
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt
                .Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName)
                .Excluding(x => x.LayananDkId)
                .Excluding(x => x.LayananDkName)
                .Excluding(x => x.GroupSpesialisId)
                .Excluding(x => x.GroupSpesialisName))
            ;
    }

    [Fact]
    public void UT5_ListDataDokterTest()
    {
        var filter = PpaType.Key("B");
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(filter);
        actual.Should().ContainEquivalentOf(Faker(), opt => 
            opt.Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName)
                .Excluding(x => x.LayananDkId)
                .Excluding(x => x.LayananDkName)
                .Excluding(x => x.GroupSpesialisId)
                .Excluding(x => x.GroupSpesialisName))
            ;
    }
}