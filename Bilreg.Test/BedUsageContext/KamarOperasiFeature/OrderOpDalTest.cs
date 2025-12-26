using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OrderOpDalTest
{
    private readonly OrderOpDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OrderOpDto Faker()
        => new OrderOpDto(
            OrderOpId: "A",
            OrderDate: new DateTime(2024, 1, 1, 10, 0, 0),
            RegId: "B",
            PasienId: "C",
            Icd10Id: "D",
            JenisOperasiId: "E",
            NamaOperasi: "F",
            DokterId: "G",
            EstimasiDurasi: 120,
            PreferedDate: new DateTime(2024, 1, 2, 8, 0, 0),
            SpecialEquipment: "H",
            CrtUser: "I",
            CrtDate: new DateTime(2024, 1, 1, 10, 0, 0),
            UpdUser: "J",
            UpdDate: new DateTime(2024, 1, 1, 10, 0, 0),
            VodUser: "K",
            VodDate: new DateTime(3000, 1, 1),
            PasienName: "L",
            TglLahir: "2000-01-01",
            Gender: "M",
            fs_ket_icd: "N",
            fs_nm_jenis_operasi: "O",
            fs_nm_peg: "P",
            UrgencyLevel: (int)UrgencyLevelEnum.Urgent
        );

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1),new DateTime(2024, 1, 31));

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
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.fs_ket_icd)
                      .Excluding(x => x.fs_nm_jenis_operasi)
                      .Excluding(x => x.fs_nm_peg));
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var orderOp = Faker();
        _sut.Insert(orderOp);

        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(orderOp,
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender)
                      .Excluding(x => x.fs_ket_icd)
                      .Excluding(x => x.fs_nm_jenis_operasi)
                      .Excluding(x => x.fs_nm_peg));
    }
}
