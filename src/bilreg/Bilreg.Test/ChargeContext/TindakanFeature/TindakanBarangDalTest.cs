using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanBarangDalTest
{
    private readonly TindakanBarangDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TindakanBarangDto CreateSampleDto(
        string tindakanBarangId,
        string tindakanId,
        int noUrut,
        string brgId = "BRG01",
        decimal qtyBom = 2m,
        decimal qtyKecilRequested = 2m,
        decimal qtyKecilDeducted = 2m,
        decimal qtyKecilPending = 0m,
        bool isChargeBarang = true,
        decimal hargaJualTotal = 50000m,
        string pakaiBrgId = "PK001",
        string trsBillingId = "TB001",
        bool isVoid = false)
    {
        return new TindakanBarangDto
        {
            TindakanBarangId = tindakanBarangId,
            TindakanId = tindakanId,
            NoUrut = noUrut,
            BrgId = brgId,
            SatuanBomId = "PCS",
            QtyBom = qtyBom,
            SatuanKecilId = "PCS",
            QtyKecilRequested = qtyKecilRequested,
            QtyKecilDeducted = qtyKecilDeducted,
            QtyKecilPending = qtyKecilPending,
            IsChargeBarang = isChargeBarang,
            HargaJualTotal = hargaJualTotal,
            PakaiBrgId = pakaiBrgId,
            TrsBillingId = trsBillingId,
            IsVoid = isVoid,
            CRTTGL = "2026-11-12",
            CRTJAM = "10:00:00",
            CRTIPA = "127.0.0.1",
            CRTVER = "1.0",
            CRTUSR = "TESTER",
            UPDTGL = "3000-01-01",
            UPDJAM = "00:00:00",
            UPDIPA = string.Empty,
            UPDVER = string.Empty,
            UPDUSR = string.Empty
        };
    }

    [Fact]
    public void Insert_SingleItem_CanBeRetrievedByGetData()
    {
        using var trans = TransHelper.NewScope();
        var dto = CreateSampleDto("TB_SNG_01", "TDK_SNG_01", 1);

        _sut.Insert(dto);

        var actual = _sut.GetData(dto.TindakanBarangId);
        actual.Should().NotBeNull();
        actual!.TindakanBarangId.Should().Be("TB_SNG_01");
        actual.TindakanId.Should().Be("TDK_SNG_01");
        actual.NoUrut.Should().Be(1);
        actual.BrgId.Should().Be("BRG01");
        actual.QtyBom.Should().Be(2m);
        actual.QtyKecilRequested.Should().Be(2m);
        actual.QtyKecilDeducted.Should().Be(2m);
        actual.QtyKecilPending.Should().Be(0m);
        actual.IsChargeBarang.Should().BeTrue();
        actual.HargaJualTotal.Should().Be(50000m);
        actual.PakaiBrgId.Should().Be("PK001");
        actual.TrsBillingId.Should().Be("TB001");
        actual.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void Insert_MultipleItems_ListDataReturnsOrderedByNoUrut()
    {
        using var trans = TransHelper.NewScope();
        var tindakanId = "TDK_MULTI_01";
        var items = new List<TindakanBarangDto>
        {
            CreateSampleDto("TB_M_03", tindakanId, 3, "BRG03"),
            CreateSampleDto("TB_M_01", tindakanId, 1, "BRG01"),
            CreateSampleDto("TB_M_02", tindakanId, 2, "BRG02")
        };

        _sut.Insert(items);

        var actual = _sut.ListByTindakanId(tindakanId).ToList();
        actual.Should().HaveCount(3);
        actual[0].NoUrut.Should().Be(1);
        actual[0].TindakanBarangId.Should().Be("TB_M_01");
        actual[1].NoUrut.Should().Be(2);
        actual[1].TindakanBarangId.Should().Be("TB_M_02");
        actual[2].NoUrut.Should().Be(3);
        actual[2].TindakanBarangId.Should().Be("TB_M_03");
    }

    [Fact]
    public void MarkVoid_SetsIsVoidToTrueAndUpdatesAudit()
    {
        using var trans = TransHelper.NewScope();
        var tindakanId = "TDK_VOID_01";
        var items = new List<TindakanBarangDto>
        {
            CreateSampleDto("TB_V_01", tindakanId, 1, isVoid: false),
            CreateSampleDto("TB_V_02", tindakanId, 2, isVoid: false)
        };
        _sut.Insert(items);

        _sut.MarkVoid(tindakanId, "USER_VOID");

        var actual = _sut.ListByTindakanId(tindakanId).ToList();
        actual.Should().HaveCount(2);
        actual.Should().OnlyContain(x => x.IsVoid);
        actual[0].UPDUSR.Should().Be("USER_VOID");
    }
}
