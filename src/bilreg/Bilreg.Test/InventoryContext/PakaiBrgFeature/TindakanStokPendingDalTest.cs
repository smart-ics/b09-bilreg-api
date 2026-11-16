using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.InventoryContext.PakaiBrgFeature;

public class TindakanStokPendingDalTest
{
    private readonly TindakanStokPendingDal _sut = new(ConnStringHelper.GetTestEnv());

    private static TindakanStokPendingDto CreateSampleDto(
        string pendingId,
        string tindakanId,
        string tindakanBarangId,
        int noUrut,
        string regId = "REG01",
        string layananId = "LY01",
        string brgId = "BRG01",
        decimal qtyPending = 5m,
        decimal qtySettled = 0m,
        bool statusPotStok = false,
        string pakaiBrgId = "",
        bool isVoid = false,
        DateTime crtDate = default)
    {
        return new TindakanStokPendingDto
        {
            PendingId = pendingId,
            TindakanId = tindakanId,
            TindakanBarangId = tindakanBarangId,
            NoUrut = noUrut,
            RegId = regId,
            LayananId = layananId,
            BrgId = brgId,
            SatuanKecilId = "PCS",
            QtyPending = qtyPending,
            QtySettled = qtySettled,
            StatusPotStok = statusPotStok,
            PakaiBrgId = pakaiBrgId,
            IsVoid = isVoid,
            SettledDate = new DateTime(3000, 1, 1),
            VoidDate = new DateTime(3000, 1, 1),
            CrtUser = "TEST_USER",
            CrtDate = crtDate == default ? DateTime.Now : crtDate,
            UpdUser = string.Empty,
            UpdDate = new DateTime(3000, 1, 1)
        };
    }

    [Fact]
    public void Insert_SingleItem_CanBeRetrievedByGetData()
    {
        using var trans = TransHelper.NewScope();
        var dto = CreateSampleDto("PND_SNG_01", "TDK_PND_01", "TB_PND_01", 1);

        _sut.Insert(dto);

        var actual = _sut.GetData(dto.PendingId);
        actual.Should().NotBeNull();
        actual!.PendingId.Should().Be("PND_SNG_01");
        actual.TindakanId.Should().Be("TDK_PND_01");
        actual.TindakanBarangId.Should().Be("TB_PND_01");
        actual.NoUrut.Should().Be(1);
        actual.RegId.Should().Be("REG01");
        actual.LayananId.Should().Be("LY01");
        actual.BrgId.Should().Be("BRG01");
        actual.SatuanKecilId.Should().Be("PCS");
        actual.QtyPending.Should().Be(5m);
        actual.QtySettled.Should().Be(0m);
        actual.StatusPotStok.Should().BeFalse();
        actual.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void Insert_MultipleItems_ListByTindakanIdReturnsOrderedByNoUrut()
    {
        using var trans = TransHelper.NewScope();
        var tindakanId = "TDK_PND_MULTI";
        var items = new List<TindakanStokPendingDto>
        {
            CreateSampleDto("PND_M_02", tindakanId, "TB_02", 2, brgId: "BRG02"),
            CreateSampleDto("PND_M_01", tindakanId, "TB_01", 1, brgId: "BRG01"),
            CreateSampleDto("PND_M_03", tindakanId, "TB_03", 3, brgId: "BRG03")
        };

        _sut.Insert(items);

        var actual = _sut.ListByTindakanId(tindakanId).ToList();
        actual.Should().HaveCount(3);
        actual[0].NoUrut.Should().Be(1);
        actual[0].PendingId.Should().Be("PND_M_01");
        actual[1].NoUrut.Should().Be(2);
        actual[1].PendingId.Should().Be("PND_M_02");
        actual[2].NoUrut.Should().Be(3);
        actual[2].PendingId.Should().Be("PND_M_03");
    }

    [Fact]
    public void ListFifoAktif_FiltersActiveUnsettledNonVoid_OrderedByCrtDate()
    {
        using var trans = TransHelper.NewScope();
        var brgId = "BRG_FIFO_01";
        var layananId = "LYF01";
        var baseTime = new DateTime(2026, 11, 12, 10, 0, 0);

        var items = new List<TindakanStokPendingDto>
        {
            // Active item 2 (later)
            CreateSampleDto("PND_F_02", "TDK_02", "TB_02", 1, layananId: layananId, brgId: brgId, crtDate: baseTime.AddMinutes(10)),
            // Active item 1 (earlier)
            CreateSampleDto("PND_F_01", "TDK_01", "TB_01", 1, layananId: layananId, brgId: brgId, crtDate: baseTime),
            // Settled item (should not be in FIFO aktif)
            CreateSampleDto("PND_F_SETTLED", "TDK_03", "TB_03", 1, layananId: layananId, brgId: brgId, statusPotStok: true, pakaiBrgId: "PK01", crtDate: baseTime.AddMinutes(5)),
            // Void item (should not be in FIFO aktif)
            CreateSampleDto("PND_F_VOID", "TDK_04", "TB_04", 1, layananId: layananId, brgId: brgId, isVoid: true, crtDate: baseTime.AddMinutes(1)),
            // Different BrgId (should not be in FIFO aktif)
            CreateSampleDto("PND_F_DIFF_BRG", "TDK_05", "TB_05", 1, layananId: layananId, brgId: "BRG_OTHER", crtDate: baseTime.AddMinutes(2))
        };
        _sut.Insert(items);

        var actual = _sut.ListFifoAktif(brgId, layananId).ToList();

        actual.Should().HaveCount(2);
        actual[0].PendingId.Should().Be("PND_F_01");
        actual[1].PendingId.Should().Be("PND_F_02");
    }

    [Fact]
    public void MarkSettled_UpdatesStatusPotStok_PakaiBrgId_SettledDate()
    {
        using var trans = TransHelper.NewScope();
        var pendingId = "PND_SETTLE_01";
        var dto = CreateSampleDto(pendingId, "TDK_01", "TB_01", 1, qtyPending: 10m);
        _sut.Insert(dto);

        var settleDate = new DateTime(2026, 11, 12, 11, 30, 0);
        _sut.MarkSettled(pendingId, "PK_SETTLE_123", 10m, settleDate, "SETTLE_USER");

        var actual = _sut.GetData(pendingId);
        actual.Should().NotBeNull();
        actual!.StatusPotStok.Should().BeTrue();
        actual.PakaiBrgId.Should().Be("PK_SETTLE_123");
        actual.QtySettled.Should().Be(10m);
        actual.SettledDate.Should().BeCloseTo(settleDate, TimeSpan.FromSeconds(1));
        actual.UpdUser.Should().Be("SETTLE_USER");
    }

    [Fact]
    public void MarkVoid_ByPendingId_SetsIsVoidTrueAndVoidDate()
    {
        using var trans = TransHelper.NewScope();
        var pendingId = "PND_VOID_01";
        var dto = CreateSampleDto(pendingId, "TDK_01", "TB_01", 1);
        _sut.Insert(dto);

        var voidDate = new DateTime(2026, 11, 12, 12, 0, 0);
        _sut.MarkVoid(pendingId, voidDate, "VOID_USER");

        var actual = _sut.GetData(pendingId);
        actual.Should().NotBeNull();
        actual!.IsVoid.Should().BeTrue();
        actual.VoidDate.Should().BeCloseTo(voidDate, TimeSpan.FromSeconds(1));
        actual.UpdUser.Should().Be("VOID_USER");
    }

    [Fact]
    public void MarkVoidByTindakanId_SetsIsVoidTrueForEntireTindakan()
    {
        using var trans = TransHelper.NewScope();
        var tindakanId = "TDK_VOID_ALL";
        var items = new List<TindakanStokPendingDto>
        {
            CreateSampleDto("PND_VA_01", tindakanId, "TB_01", 1),
            CreateSampleDto("PND_VA_02", tindakanId, "TB_02", 2)
        };
        _sut.Insert(items);

        var voidDate = new DateTime(2026, 11, 12, 12, 30, 0);
        _sut.MarkVoidByTindakanId(tindakanId, voidDate, "VOID_ALL_USER");

        var actual = _sut.ListByTindakanId(tindakanId).ToList();
        actual.Should().HaveCount(2);
        actual.Should().OnlyContain(x => x.IsVoid);
        actual[0].UpdUser.Should().Be("VOID_ALL_USER");
    }
}
