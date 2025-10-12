using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class JadwalPraktekDalTest
{
    private readonly JadwalPraktekDal _sut;

    public JadwalPraktekDalTest()
    {
        _sut = new JadwalPraktekDal(ConnStringHelper.GetTestEnv());
    }

    private static JadwalPraktekType Faker() =>
        new("A", new PetugasMedisReff("B", "C"), 
            new LayananReff("D", "E"), DayOfWeek.Friday, 
            new TimeOnly(1, 2,0), new TimeOnly(4, 5, 0));
    
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
        _sut.Delete(Faker());
    }
    
    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(Faker());
        actual.Value.Should().BeEquivalentTo(Faker(), opt => 
            opt.Excluding(x => x.Dokter.PetugasMedisName)
                .Excluding(x => x.Layanan.LayananName));
    }
    
    [Fact]
    public void UT5_ListDataDokterTest()
    {
        var filter = PetugasMedisType.Key("B");
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(filter);
        actual.Should().ContainEquivalentOf(Faker(), opt => 
            opt.Excluding(x => x.Dokter.PetugasMedisName)
                .Excluding(x => x.Layanan.LayananName));
    }

    [Fact]
    public void UT5_ListDataSmfTest()
    {
        var filter = SmfType.Key("D");
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(filter);
        actual.Should().ContainEquivalentOf(Faker(), opt => 
            opt.Excluding(x => x.Dokter.PetugasMedisName)
                .Excluding(x => x.Layanan.LayananName));
    }
    
    
    [Fact]
    public void UT99_TimeSpan_ToString_Test()
    {
        var duration = new TimeSpan(14, 53, 59);
        var formattedTime = duration.ToString(@"hh\:mm");
        formattedTime.Should().Be("14:53");
    }
    [Fact]
    public void UT99_String_ToTimeSpan()
    {
        const string durationStr = "14:53";
        var duration = TimeSpan.Parse(durationStr);
        duration.Should().Be(new TimeSpan(14, 53, 0));;
    }
}