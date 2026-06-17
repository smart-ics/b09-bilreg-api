using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillingRepoTest
{
    private const string BillingId = "BIL-REPO-001";
    private const string RegId = "RG-REPO-01";

    private readonly Mock<ITrsBillingDal> _billingDalMock;
    private readonly Mock<ITrsBilling2Dal> _billing2DalMock;
    private readonly TrsBillingRepo _sut;

    public TrsBillingRepoTest()
    {
        _billingDalMock = new Mock<ITrsBillingDal>();
        _billing2DalMock = new Mock<ITrsBilling2Dal>();
        _sut = new TrsBillingRepo(_billingDalMock.Object, _billing2DalMock.Object);
    }

    private static TrsBill2CoaType ValidPdpCoa => new(
        new CoaType("PPDP-01", ""),
        new CoaType("PDPT-01", ""),
        CoaType.Default,
        CoaType.Default,
        CoaType.Default,
        CoaType.Default);

    private static TrsBillType BuildBill(
        string billingId = BillingId,
        IEnumerable<TrsBill2TransEventType>? transactions = null,
        IEnumerable<TrsBill2DischargeEventType>? discharges = null,
        IEnumerable<TrsBill2PaymentEventType>? payments = null)
    {
        var trans = TrsBill2TransEventType.Create(
            1,
            new TrsBill2KomponenType("DT-001", "Detil"),
            TrsBillJenisBayarType.Pdp,
            50_000m,
            PpaType.Default.ToReff(),
            ValidPdpCoa);

        return new TrsBillType(
            billingId,
            BillModulGroup.Jasa,
            new DateTime(2026, 6, 17, 10, 0, 0),
            new RegReff(RegId, "MR-01", "Pasien Repo"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            new AuditInfoType("USR-01", new DateTime(2026, 6, 17, 10, 0, 0)),
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(50_000m, 0m, 0m, 0m),
            new TrsBillKetType("Ket", "Ket2", "REF-01", 1m, "MAIN-01"),
            transactions ?? [trans],
            discharges ?? [],
            payments ?? []);
    }

    private static TrsBillingDto BuildHeaderDto(string billingId = BillingId) =>
        TrsBillingDto.FromModel(BuildBill(billingId, transactions: [], discharges: [], payments: []));

    private static TaTrsBilling2Dto BuildTransBill2Dto(string billingId = BillingId) =>
        TaTrsBilling2Dto.FromModelTrans(
            new TrsBill2TransEventType(
                1,
                new TrsBill2KomponenType("DT-001", "Detil"),
                TrsBillJenisBayarType.Pdp,
                50_000m,
                new PpaReff("MED-01", "Dr"),
                ValidPdpCoa),
            billingId,
            modul: 0);

    private static ITrsBillingKey BillingKey(string billingId = BillingId) =>
        TrsBillType.Key(billingId);

    [Fact]
    public void GivenNewBill_WhenSaveChanges_ThenInsertHeaderAndReplaceBill2Rows()
    {
        // Given
        var model = BuildBill();
        _billingDalMock
            .Setup(x => x.GetData(It.IsAny<ITrsBillingKey>()))
            .Returns((TrsBillingDto)null!);

        // When
        _sut.SaveChanges(model);

        // Then
        _billingDalMock.Verify(x => x.Insert(It.Is<TrsBillingDto>(d => d.fs_kd_trs == BillingId)), Times.Once);
        _billingDalMock.Verify(x => x.Update(It.IsAny<TrsBillingDto>()), Times.Never);
        _billing2DalMock.Verify(x => x.Delete(It.Is<ITrsBillingKey>(k => k.TrsBillingId == BillingId)), Times.Once);
        _billing2DalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TaTrsBilling2Dto>>()), Times.Once);
    }

    [Fact]
    public void GivenExistingBill_WhenSaveChanges_ThenUpdateHeaderAndReplaceBill2Rows()
    {
        // Given
        var model = BuildBill();
        _billingDalMock
            .Setup(x => x.GetData(It.IsAny<ITrsBillingKey>()))
            .Returns(BuildHeaderDto());

        // When
        _sut.SaveChanges(model);

        // Then
        _billingDalMock.Verify(x => x.Update(It.Is<TrsBillingDto>(d => d.fs_kd_trs == BillingId)), Times.Once);
        _billingDalMock.Verify(x => x.Insert(It.IsAny<TrsBillingDto>()), Times.Never);
        _billing2DalMock.Verify(x => x.Delete(It.Is<ITrsBillingKey>(k => k.TrsBillingId == BillingId)), Times.Once);
        _billing2DalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TaTrsBilling2Dto>>()), Times.Once);
    }

    [Fact]
    public void GivenBillWithTransDischargeAndPayment_WhenSaveChanges_ThenInsertFourBill2Rows()
    {
        // Given
        var discharge = new TrsBill2DischargeEventType(
            2,
            new TrsBill2KomponenType("DT-002", "Detil 2"),
            TrsBillJenisBayarType.Kas,
            20_000m,
            new PpaReff("MED-02", "Dr 2"),
            "KSR-01",
            "RO-OLD",
            new DateTime(2026, 6, 18));
        var payment = new TrsBill2PaymentEventType(
            3,
            new TrsBill2KomponenType("DT-003", "Detil 3"),
            TrsBillJenisBayarType.Hut,
            new PaymentType("PAY-001", "Hutang", false),
            10_000m,
            new DateTime(2026, 6, 19),
            new PpaReff("MED-03", "Dr 3"));
        var model = BuildBill(discharges: [discharge], payments: [payment]);

        List<TaTrsBilling2Dto>? captured = null;
        _billingDalMock
            .Setup(x => x.GetData(It.IsAny<ITrsBillingKey>()))
            .Returns((TrsBillingDto)null!);
        _billing2DalMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<TaTrsBilling2Dto>>()))
            .Callback<IEnumerable<TaTrsBilling2Dto>>(rows => captured = rows.ToList());

        // When
        _sut.SaveChanges(model);

        // Then
        captured.Should().NotBeNull();
        captured!.Should().HaveCount(4);
        captured.Should().Contain(x => x.fn_no_urut == 1);
        captured.Should().Contain(x => x.fn_no_urut == 2);
        captured.Should().Contain(x => x.fn_no_urut == 3 && x.fs_kd_jenis_bayar == "HUT");
        captured.Should().Contain(x => x.fn_no_urut == 3 && x.fs_kd_jenis_bayar == "KAS");
    }

    [Fact]
    public void GivenHeaderExists_WhenLoadEntity_ThenReturnsMappedBill()
    {
        // Given
        var header = BuildHeaderDto();
        var bill2 = new List<TaTrsBilling2Dto> { BuildTransBill2Dto() };
        _billingDalMock.Setup(x => x.GetData(It.IsAny<ITrsBillingKey>())).Returns(header);
        _billing2DalMock.Setup(x => x.ListData(It.IsAny<ITrsBillingKey>())).Returns(bill2);

        // When
        var result = _sut.LoadEntity(BillingKey());

        // Then
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: bill =>
            {
                bill.TrsBillingId.Should().Be(BillingId);
                bill.Reg.RegId.Should().Be(RegId);
                bill.ListTransaction.Should().HaveCount(1);
                bill.ListDischarge.Should().BeEmpty();
                bill.ListPayment.Should().BeEmpty();
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void GivenHeaderNotFound_WhenLoadEntity_ThenReturnsNone()
    {
        // Given
        _billingDalMock
            .Setup(x => x.GetData(It.IsAny<ITrsBillingKey>()))
            .Returns((TrsBillingDto)null!);

        // When
        var result = _sut.LoadEntity(BillingKey());

        // Then
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void GivenHeaderExistsAndBill2IsNull_WhenLoadEntity_ThenReturnsBillWithEmptyEvents()
    {
        // Given
        _billingDalMock.Setup(x => x.GetData(It.IsAny<ITrsBillingKey>())).Returns(BuildHeaderDto());
        _billing2DalMock
            .Setup(x => x.ListData(It.IsAny<ITrsBillingKey>()))
            .Returns((IEnumerable<TaTrsBilling2Dto>)null!);

        // When
        var result = _sut.LoadEntity(BillingKey());

        // Then
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: bill =>
            {
                bill.ListTransaction.Should().BeEmpty();
                bill.ListDischarge.Should().BeEmpty();
                bill.ListPayment.Should().BeEmpty();
            },
            onNone: () => Assert.Fail("Expected Some but got None"));
    }

    [Fact]
    public void GivenBillingKey_WhenDeleteEntity_ThenDeleteBothTables()
    {
        // Given
        var key = BillingKey();

        // When
        _sut.DeleteEntity(key);

        // Then
        _billingDalMock.Verify(x => x.Delete(key), Times.Once);
        _billing2DalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void GivenBillingDtos_WhenListData_ThenReturnsViews()
    {
        // Given
        var dtos = new List<TrsBillingDto>
        {
            BuildHeaderDto("BIL-REPO-002"),
            BuildHeaderDto("BIL-REPO-003")
        };
        _billingDalMock
            .Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns(dtos);

        // When
        var result = _sut.ListData(RegModel.Key(RegId)).ToList();

        // Then
        result.Should().HaveCount(2);
        result[0].TrsBillingId.Should().Be("BIL-REPO-002");
        result[1].TrsBillingId.Should().Be("BIL-REPO-003");
    }

    [Fact]
    public void GivenEmptyDalResult_WhenListData_ThenReturnsEmptyList()
    {
        // Given
        _billingDalMock
            .Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns(new List<TrsBillingDto>());

        // When
        var result = _sut.ListData(RegModel.Key(RegId)).ToList();

        // Then
        result.Should().BeEmpty();
    }

    [Fact]
    public void GivenNullDalResult_WhenListData_ThenReturnsEmptyList()
    {
        // Given
        _billingDalMock
            .Setup(x => x.ListData(It.IsAny<IRegKey>()))
            .Returns((IEnumerable<TrsBillingDto>)null!);

        // When
        var result = _sut.ListData(RegModel.Key(RegId)).ToList();

        // Then
        result.Should().BeEmpty();
    }
}
