//using Farinv.Domain.SalesContext.AntrianFeature;
//using Farinv.Infrastructure.Helpers;
//using Farinv.Infrastructure.SalesContext.AntrianFeature;
//using FluentAssertions;
//using Nuna.Lib.TransactionHelper;
//using Nuna.Lib.ValidationHelper;

//namespace Farinv.Test.SalesContext.AntrianFeature;

//public class AntrianDalTest
//{
//    private readonly AntrianDal _sut = new(ConnStringHelper.GetTestEnv());

//    private static AntrianDto Faker()
//        => new AntrianDto(
//            AntrianId: "ANT001",
//            AntrianDate: new DateTime(2026, 2, 3),
//            SequenceTag: "APT01",
//            NoAntrian: 1,
//            AntrianStatus: (int)AntrianStatusEnum.Taken,
//            PersonName: "-",
//            TakenAt: new DateTime(2026, 2, 3),
//            AssignedAt: new DateTime(3000, 1, 1),
//            PreparedAt: new DateTime(3000, 1, 1),
//            DeliveredAt: new DateTime(3000, 1, 1),
//            CancelAt: new DateTime(3000, 1, 1)
//        );

//    private static IAntrianKey FakerKey()
//        => AntrianModel.Key("ANT001");

//    [Fact]
//    public void InsertTest()
//    {
//        using var trans = TransHelper.NewScope();
//        _sut.Insert(Faker());
//    }

//    [Fact]
//    public void UpdateTest()
//    {
//        using var trans = TransHelper.NewScope();
//        _sut.Update(Faker());
//    }

//    [Fact]
//    public void DeleteTest()
//    {
//        using var trans = TransHelper.NewScope();
//        _sut.Delete(FakerKey());
//    }

//    [Fact]
//    public void GetDataTest()
//    {
//        using var trans = TransHelper.NewScope();
//        _sut.Insert(Faker());
//        var actual = _sut.GetData(FakerKey());
//        actual.Should().BeEquivalentTo(Faker());
//    }

//    [Fact]
//    public void ListDataTest()
//    {
//        using var trans = TransHelper.NewScope();
//        _sut.Insert(Faker());
//        var actual = _sut.ListData(new Periode(new DateTime(2026, 2, 3), new DateTime(2026, 2, 3)));
//        actual.Should().ContainEquivalentOf(Faker());
//    }
//}