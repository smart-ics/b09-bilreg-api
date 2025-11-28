using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.AdmisiContext.BookingFeature;
using Bilreg.Infrastructure.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class BookingDalTest
{
    private readonly BookingDal _sut = new(ConnStringHelper.GetTestEnv());

    private static BookingDto Faker()
        => new BookingDto(
            BookingId: "A",
            BookingDate: new DateTime(2024, 1, 1),
            PasienName: "B",
            TglLahir: new DateTime(2000, 1, 1),
            Gender: "C",
            Alamat: "D",
            PasienId: "D1",
            RegId: "D2",
            TglBerobat: new DateTime(2024, 1, 2),
            JamPraktek: "E",
            LayananId: "F",
            DokterId: "G",
            NoAntrian: 1,
            CrtUser: "H",
            CrtDate: new DateTime(2024, 1, 1, 10, 0, 0),
            UpdUser: "I",
            UpdDate: new DateTime(2024, 1, 1, 10, 0, 0),
            VodUser: "J",
            VodDate: new DateTime(3000, 1, 1),
            LayananName: "K",
            DokterName: "L"
        );

    private static IBookingKey FakerKey()
        => BookingModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1),new DateTime(2024, 1, 31)
        );

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt
                .Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var booking = Faker();
        _sut.Insert(booking);
        
        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(booking,
            opt => opt
                .Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName));
    }

    [Fact]
    public void ListPerTglBerobatTest()
    {
        using var trans = TransHelper.NewScope();
        var booking = Faker();
        _sut.Insert(booking);
        
        var actual = _sut.ListPerTglBerobat(FakerPeriode());
        actual.Should().ContainEquivalentOf(booking,
            opt => opt
                .Excluding(x => x.DokterName)
                .Excluding(x => x.LayananName));
    }
}