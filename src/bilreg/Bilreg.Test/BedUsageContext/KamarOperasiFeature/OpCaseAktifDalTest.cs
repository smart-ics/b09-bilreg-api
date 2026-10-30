using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OpCaseAktifDalTest
{
    private readonly OpCaseAktifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OpCaseAktifDto Faker()
        => new OpCaseAktifDto(
            OrderOpId: "A",
            OrderOpDate: new DateTime(2023, 1, 1),
            PasienId: "B",
            PasienName: "C",
            DokterPengorderId: "DOC-ORD",
            DokterPengorderName: "Dr. Pengorder",
            DokterOperatorId: "DOC-OPR",
            DokterOperatorName: "Dr. Operator",
            NamaOperasi: "Apendektomi",
            ProsedurTambahan: "Eksplorasi",
            KlasifikasiOperasi: 2,
            PreferedDate: new DateTime(2023, 1, 2),
            EstimasiDurasi: 60,
            UrgencyLevel: 1,
            OpCaseState: 1,
            TglLahir: "2000-01-01",
            Gender: "M");

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

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
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.DokterPengorderId)
                .Excluding(x => x.DokterPengorderName)
                .Excluding(x => x.DokterOperatorId)
                .Excluding(x => x.DokterOperatorName)
                .Excluding(x => x.ProsedurTambahan)
                .Excluding(x => x.KlasifikasiOperasi)
                .Excluding(x => x.EstimasiDurasi)
                .Excluding(x => x.UrgencyLevel)
                .Excluding(x => x.PreferedDate));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt
                .Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender)
                .Excluding(x => x.NamaOperasi)
                .Excluding(x => x.DokterPengorderId)
                .Excluding(x => x.DokterPengorderName)
                .Excluding(x => x.DokterOperatorId)
                .Excluding(x => x.DokterOperatorName)
                .Excluding(x => x.ProsedurTambahan)
                .Excluding(x => x.KlasifikasiOperasi)
                .Excluding(x => x.EstimasiDurasi)
                .Excluding(x => x.UrgencyLevel)
                .Excluding(x => x.PreferedDate));
    }

    [Fact]
    public void ListData_WithFilter_ShouldFilterAndLimitProperly()
    {
        using var trans = TransHelper.NewScope();
        var item1 = Faker() with { OrderOpId = "TEST-ORD-01", OpCaseState = 1, OrderOpDate = new DateTime(2026, 10, 1) };
        var item2 = Faker() with { OrderOpId = "TEST-ORD-02", OpCaseState = 2, OrderOpDate = new DateTime(2026, 10, 2) };
        _sut.Insert(item1);
        _sut.Insert(item2);

        var filterRequested = new OpCaseAktifFilter(OpCaseState: 1);
        var actualRequested = _sut.ListData(filterRequested).ToList();
        actualRequested.Should().Contain(x => x.OrderOpId == "TEST-ORD-01");
        actualRequested.Should().NotContain(x => x.OrderOpId == "TEST-ORD-02");

        var filterLimit = new OpCaseAktifFilter(Limit: 1);
        var actualLimit = _sut.ListData(filterLimit).ToList();
        actualLimit.Should().HaveCount(1);
    }

    [Fact]
    public void ListData_ShouldPrioritizeCitoOverElective()
    {
        using var trans = TransHelper.NewScope();
        var orderOpDal = new OrderOpDal(ConnStringHelper.GetTestEnv());

        var orderElectiveId = $"ORD-ELEC-{Guid.NewGuid():N}"[..26];
        var orderCitoId = $"ORD-CITO-{Guid.NewGuid():N}"[..26];

        var orderElective = new OrderOpDto(
            OrderOpId: orderElectiveId,
            OrderDate: new DateTime(2026, 10, 1, 8, 0, 0),
            RegId: "REG-01",
            PasienId: "PAS-01",
            Icd10Id: "K35.8",
            JenisOperasiId: "-",
            NamaOperasi: "Op Elective",
            DokterId: "DOC-01",
            UrgencyLevel: (int)UrgencyLevelEnum.Elective,
            TarifId: "TRF-01",
            TarifName: "Tarif 1",
            EstimasiDurasi: 60,
            PreferedDate: new DateTime(2026, 10, 10),
            SpecialEquipment: "-",
            DokterPengorderId: "DOC-01",
            LayananPengorderId: "LYN-01",
            DokterOperatorId: "DOC-02",
            ProsedurTambahan: "",
            KlasifikasiOperasi: 1,
            AnestesiPlan: 1,
            PostOpCareUnit: 1,
            AlasanBatal: "",
            CrtUser: "test",
            CrtDate: new DateTime(2026, 10, 1),
            UpdUser: "test",
            UpdDate: new DateTime(2026, 10, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1),
            PasienName: "",
            TglLahir: "",
            Gender: "",
            fs_ket_icd: "",
            fs_nm_jenis_operasi: "",
            fs_nm_peg: "",
            DokterPengorderName: "",
            LayananPengorderName: "",
            DokterOperatorName: ""
        );

        var orderCito = orderElective with
        {
            OrderOpId = orderCitoId,
            OrderDate = new DateTime(2026, 10, 2, 8, 0, 0),
            NamaOperasi = "Op Cito",
            UrgencyLevel = (int)UrgencyLevelEnum.Cito
        };

        orderOpDal.Insert(orderElective);
        orderOpDal.Insert(orderCito);

        var caseElective = Faker() with
        {
            OrderOpId = orderElectiveId,
            OrderOpDate = new DateTime(2026, 10, 1, 8, 0, 0),
            OpCaseState = 1
        };
        var caseCito = Faker() with
        {
            OrderOpId = orderCitoId,
            OrderOpDate = new DateTime(2026, 10, 2, 8, 0, 0),
            OpCaseState = 1
        };

        _sut.Insert(caseElective);
        _sut.Insert(caseCito);

        var result = _sut.ListData(new OpCaseAktifFilter(OpCaseState: 1)).ToList();

        var citoIndex = result.FindIndex(x => x.OrderOpId == orderCitoId);
        var electiveIndex = result.FindIndex(x => x.OrderOpId == orderElectiveId);

        citoIndex.Should().BeGreaterThanOrEqualTo(0);
        electiveIndex.Should().BeGreaterThanOrEqualTo(0);
        citoIndex.Should().BeLessThan(electiveIndex, "Cito (UrgencyLevel = 2) must be returned before Elective (UrgencyLevel = 1) despite later OrderOpDate");
    }
}
