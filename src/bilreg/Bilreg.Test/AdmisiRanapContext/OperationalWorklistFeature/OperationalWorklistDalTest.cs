using Bilreg.Application.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.OperationalWorklistFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.OperationalWorklistFeature;

public class OperationalWorklistDalTest
{
    private readonly OperationalWorklistDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly InpatientOrderDal _orderDal = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void UT01_BuildInpatientOrderBranch_ContainsDirectInsuranceColumnsWithoutJoin()
    {
        var sql = OperationalWorklistDal.BuildInpatientOrderBranch(includeTerminal: false);

        sql.Should().Contain("FROM BILRG_AdmInpatientOrder aa");
        sql.Should().Contain("aa.TipeJaminanId");
        sql.Should().Contain("aa.TipeJaminanName");
        sql.Should().NotContain("BILRG_AdmOpnameRequestInsurance");
        sql.Should().NotContain("ta_tipe_jaminan");
    }

    [Fact]
    public void UT02_BuildInpatientOrderBranch_ContainsDynamicJenisCaseExpression()
    {
        var sql = OperationalWorklistDal.BuildInpatientOrderBranch(includeTerminal: false);

        sql.Should().Contain("WHEN aa.OrderSource IN (1, 2) THEN 'opname'");
        sql.Should().Contain("ELSE 'reservation'");
    }

    [Fact]
    public void UT03_BuildInpatientOrderBranch_AppliesTerminalFilterCorrectly()
    {
        var sqlWithoutTerminal = OperationalWorklistDal.BuildInpatientOrderBranch(includeTerminal: false);
        var sqlWithTerminal = OperationalWorklistDal.BuildInpatientOrderBranch(includeTerminal: true);

        sqlWithoutTerminal.Should().Contain("aa.OrderStatus NOT IN (@OrderCancelled, @OrderFulfilled)");
        sqlWithTerminal.Should().NotContain("aa.OrderStatus NOT IN (@OrderCancelled, @OrderFulfilled)");
    }

    [Theory]
    [InlineData("opname", "AND aa.OrderSource IN (1, 2)")]
    [InlineData("reservation", "AND aa.OrderSource IN (3, 4)")]
    [InlineData(null, "")]
    public void UT04_BuildInpatientOrderBranch_AppliesOrderSourceFilterForOpnameAndReservation(
        string? jenisFilter,
        string expectedFragment)
    {
        var sql = OperationalWorklistDal.BuildInpatientOrderBranch(includeTerminal: false, jenisFilter);

        if (!string.IsNullOrEmpty(expectedFragment))
            sql.Should().Contain(expectedFragment);
        else
        {
            sql.Should().NotContain("AND aa.OrderSource IN (1, 2)");
            sql.Should().NotContain("AND aa.OrderSource IN (3, 4)");
        }
    }

    [Fact]
    public void UT05_OperationalWorklistDal_List_MapsJenisDynamicallyAndReadsInsuranceFlat()
    {
        using var trans = TransHelper.NewScope();
        var clinicalId = GenerateOrderId();
        var bookingId = GenerateOrderId();

        var clinicalOrder = CreateTestDto(
            clinicalId,
            InpatientOrderStatusEnum.Requested,
            OrderSourceEnum.EmrRawatJalan,
            "BPJ",
            "BPJS Kesehatan");

        var bookingOrder = CreateTestDto(
            bookingId,
            InpatientOrderStatusEnum.Maintained,
            OrderSourceEnum.AdmisiBooking,
            "ASR",
            "Asuransi Mandiri Inhealth");

        _orderDal.Insert(clinicalOrder);
        _orderDal.Insert(bookingOrder);

        var filter = new OperationalWorklistFilter(
            SearchTerm: clinicalOrder.PasienId,
            IncludeTerminal: true);

        var results = _sut.List(filter).ToList();

        var clinicalResult = results.FirstOrDefault(r => r.ItemId == clinicalId);
        clinicalResult.Should().NotBeNull();
        clinicalResult!.Jenis.Should().Be("opname");
        clinicalResult.TipeJaminanId.Should().Be("BPJ");
        clinicalResult.TipeJaminanName.Should().Be("BPJS Kesehatan");

        var bookingFilter = new OperationalWorklistFilter(
            SearchTerm: bookingOrder.PasienId,
            IncludeTerminal: true);

        var bookingResults = _sut.List(bookingFilter).ToList();
        var bookingResult = bookingResults.FirstOrDefault(r => r.ItemId == bookingId);
        bookingResult.Should().NotBeNull();
        bookingResult!.Jenis.Should().Be("reservation");
        bookingResult.TipeJaminanId.Should().Be("ASR");
        bookingResult.TipeJaminanName.Should().Be("Asuransi Mandiri Inhealth");
    }

    [Fact]
    public void UT06_OperationalWorklistDal_List_FiltersByJenisAccurately()
    {
        using var trans = TransHelper.NewScope();
        var clinicalId = GenerateOrderId();
        var bookingId = GenerateOrderId();
        var commonMr = $"337502{Random.Shared.Next(100000000, 999999999)}"[..15];

        var clinicalOrder = CreateTestDto(
            clinicalId,
            InpatientOrderStatusEnum.Requested,
            OrderSourceEnum.EmrRawatJalan,
            "BPJ",
            "BPJS",
            commonMr);

        var bookingOrder = CreateTestDto(
            bookingId,
            InpatientOrderStatusEnum.Requested,
            OrderSourceEnum.AdmisiBooking,
            "UMU",
            "Umum / Pribadi",
            commonMr);

        _orderDal.Insert(clinicalOrder);
        _orderDal.Insert(bookingOrder);

        // Filter only opname
        var opnameOnly = _sut.List(new OperationalWorklistFilter(
            Jenis: "opname",
            SearchTerm: commonMr,
            IncludeTerminal: true)).ToList();

        opnameOnly.Should().Contain(x => x.ItemId == clinicalId && x.Jenis == "opname");
        opnameOnly.Should().NotContain(x => x.ItemId == bookingId);

        // Filter only reservation
        var reservationOnly = _sut.List(new OperationalWorklistFilter(
            Jenis: "reservation",
            SearchTerm: commonMr,
            IncludeTerminal: true)).ToList();

        reservationOnly.Should().Contain(x => x.ItemId == bookingId && x.Jenis == "reservation");
        reservationOnly.Should().NotContain(x => x.ItemId == clinicalId);
    }

    [Fact]
    public void UT07_OperationalWorklistDal_List_ExcludesTerminalStatusesWhenIncludeTerminalFalse()
    {
        using var trans = TransHelper.NewScope();
        var fulfilledId = GenerateOrderId();
        var cancelledId = GenerateOrderId();
        var requestedId = GenerateOrderId();
        var commonMr = $"337502{Random.Shared.Next(100000000, 999999999)}"[..15];

        var fulfilledOrder = CreateTestDto(
            fulfilledId,
            InpatientOrderStatusEnum.Fulfilled,
            OrderSourceEnum.EmrRawatJalan,
            "BPJ",
            "BPJS",
            commonMr);

        var cancelledOrder = CreateTestDto(
            cancelledId,
            InpatientOrderStatusEnum.Cancelled,
            OrderSourceEnum.AdmisiBooking,
            "BPJ",
            "BPJS",
            commonMr);

        var requestedOrder = CreateTestDto(
            requestedId,
            InpatientOrderStatusEnum.Requested,
            OrderSourceEnum.EmrIgd,
            "BPJ",
            "BPJS",
            commonMr);

        _orderDal.Insert(fulfilledOrder);
        _orderDal.Insert(cancelledOrder);
        _orderDal.Insert(requestedOrder);

        var activeOnly = _sut.List(new OperationalWorklistFilter(
            SearchTerm: commonMr,
            IncludeTerminal: false)).ToList();

        activeOnly.Should().Contain(x => x.ItemId == requestedId);
        activeOnly.Should().NotContain(x => x.ItemId == fulfilledId);
        activeOnly.Should().NotContain(x => x.ItemId == cancelledId);
    }

    private static string GenerateOrderId() =>
        $"OPN{DateTime.Now:yy}{Random.Shared.Next(10000000, 99999999)}"[..12];

    private static InpatientOrderDto CreateTestDto(
        string id,
        InpatientOrderStatusEnum status,
        OrderSourceEnum source,
        string tipeJaminanId = "BPJ",
        string tipeJaminanName = "BPJS Kesehatan",
        string? pasienId = null) =>
        new(
            id,
            (int)source,
            (int)status,
            pasienId ?? $"337502{Random.Shared.Next(100000000, 999999999)}"[..15],
            "PASIEN WORKLIST TEST",
            new DateTime(1990, 1, 1),
            "L",
            "D001",
            "dr. Budi Sp.PD",
            DateTime.Today.AddDays(1),
            "1",
            "Kelas 1",
            "B01",
            "Bangsal Mawar",
            "Catatan Klinis",
            tipeJaminanId,
            tipeJaminanName,
            "00012345678",
            "-",
            $"REF-{Random.Shared.Next(10000, 99999)}",
            "EMR_RJ",
            "test_user",
            DateTime.Now,
            "",
            new DateTime(3000, 1, 1),
            "",
            new DateTime(3000, 1, 1));
}
