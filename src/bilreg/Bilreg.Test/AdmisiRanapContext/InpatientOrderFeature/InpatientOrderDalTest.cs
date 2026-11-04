using Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.InpatientOrderFeature;

public class InpatientOrderDalTest
{
    private readonly InpatientOrderDal _sut = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void UT_DAL_01_GivenInpatientOrderDto_WhenInsertAndGetData_ThenRoundTripsSuccessfully()
    {
        using var trans = TransHelper.NewScope();
        var id = GenerateOrderId();
        var dto = CreateTestDto(id, InpatientOrderStatusEnum.Requested, OrderSourceEnum.EmrRawatJalan);

        _sut.Insert(dto);
        var actual = _sut.GetData(InpatientOrderModel.Key(id));

        actual.Should().NotBeNull();
        actual.InpatientOrderId.Should().Be(id);
        actual.OrderSource.Should().Be((int)OrderSourceEnum.EmrRawatJalan);
        actual.OrderStatus.Should().Be((int)InpatientOrderStatusEnum.Requested);
        actual.PasienId.Should().Be(dto.PasienId);
        actual.PasienName.Should().Be(dto.PasienName);
        actual.DokterId.Should().Be("D001");
        actual.DokterName.Should().Be("dr. Budi Sp.PD");
        actual.KelasId.Should().Be("1");
        actual.BangsalId.Should().Be("B01");
        actual.ClinicalNotes.Should().Be("Febris H-4");
        actual.TipeJaminanId.Should().Be("BPJ");
        actual.TipeJaminanName.Should().Be("BPJS Kesehatan");
        actual.JaminanReffId.Should().Be("00012345678");
        actual.FulfilledRegId.Should().Be("-");
        actual.ExternalReferenceId.Should().Be(dto.ExternalReferenceId);
        actual.ExternalSystemSource.Should().Be("EMR_RJ");
    }

    [Fact]
    public void UT_DAL_02_GivenExistingOrder_WhenUpdate_ThenChangesPersistSuccessfully()
    {
        using var trans = TransHelper.NewScope();
        var id = GenerateOrderId();
        var dto = CreateTestDto(id, InpatientOrderStatusEnum.Requested, OrderSourceEnum.AdmisiBooking);
        _sut.Insert(dto);

        var updatedDto = dto with
        {
            OrderStatus = (int)InpatientOrderStatusEnum.Maintained,
            KelasId = "2",
            KelasName = "Kelas 2",
            BangsalId = "B02",
            BangsalName = "Bangsal Melati",
            ClinicalNotes = "Kamar dipindahkan ke Melati",
            UpdUser = "admisi_user",
            UpdDate = new DateTime(2026, 11, 4, 11, 30, 0)
        };
        _sut.Update(updatedDto);

        var actual = _sut.GetData(InpatientOrderModel.Key(id));
        actual.OrderStatus.Should().Be((int)InpatientOrderStatusEnum.Maintained);
        actual.KelasId.Should().Be("2");
        actual.KelasName.Should().Be("Kelas 2");
        actual.BangsalId.Should().Be("B02");
        actual.BangsalName.Should().Be("Bangsal Melati");
        actual.ClinicalNotes.Should().Be("Kamar dipindahkan ke Melati");
        actual.UpdUser.Should().Be("admisi_user");
    }

    [Fact]
    public void UT_DAL_03_GivenExistingOrder_WhenDelete_ThenRowIsRemoved()
    {
        using var trans = TransHelper.NewScope();
        var id = GenerateOrderId();
        var dto = CreateTestDto(id, InpatientOrderStatusEnum.Requested, OrderSourceEnum.AdmisiBooking);
        _sut.Insert(dto);

        _sut.Delete(InpatientOrderModel.Key(id));

        var actual = _sut.GetData(InpatientOrderModel.Key(id));
        actual.Should().BeNull();
    }

    [Fact]
    public void UT_DAL_04_GivenMultipleOrders_WhenListDataWithFilters_ThenFiltersAccurately()
    {
        using var trans = TransHelper.NewScope();
        var id1 = GenerateOrderId();
        var id2 = GenerateOrderId();
        var dto1 = CreateTestDto(id1, InpatientOrderStatusEnum.Requested, OrderSourceEnum.EmrRawatJalan);
        var dto2 = CreateTestDto(id2, InpatientOrderStatusEnum.Maintained, OrderSourceEnum.AdmisiBooking);
        _sut.Insert(dto1);
        _sut.Insert(dto2);

        var filter = new InpatientOrderListFilter(Status: InpatientOrderStatusEnum.Maintained);
        var result = _sut.ListData(filter).ToList();

        result.Should().Contain(x => x.InpatientOrderId == id2);
        result.Should().NotContain(x => x.InpatientOrderId == id1);
    }

    [Fact]
    public void UT_DAL_05_GivenActiveAndInactiveOrders_WhenListActive_ThenReturnsOnlyActive()
    {
        using var trans = TransHelper.NewScope();
        var id1 = GenerateOrderId();
        var id2 = GenerateOrderId();
        var id3 = GenerateOrderId();
        var dto1 = CreateTestDto(id1, InpatientOrderStatusEnum.Requested, OrderSourceEnum.EmrRawatJalan);
        var dto2 = CreateTestDto(id2, InpatientOrderStatusEnum.Maintained, OrderSourceEnum.AdmisiBooking);
        var dto3 = CreateTestDto(id3, InpatientOrderStatusEnum.Cancelled, OrderSourceEnum.EmrRawatJalan);
        _sut.Insert(dto1);
        _sut.Insert(dto2);
        _sut.Insert(dto3);

        var result = _sut.ListActive().ToList();

        result.Should().Contain(x => x.InpatientOrderId == id1);
        result.Should().Contain(x => x.InpatientOrderId == id2);
        result.Should().NotContain(x => x.InpatientOrderId == id3);
    }

    [Fact]
    public void UT_DAL_06_GivenExternalRef_WhenGetByExternalReference_ThenReturnsMatchingRow()
    {
        using var trans = TransHelper.NewScope();
        var id = GenerateOrderId();
        var extRef = $"EXT-{Random.Shared.Next(100000, 999999)}";
        var dto = CreateTestDto(id, InpatientOrderStatusEnum.Requested, OrderSourceEnum.EmrRawatJalan) with
        {
            ExternalReferenceId = extRef,
            ExternalSystemSource = "EMR_RJ"
        };
        _sut.Insert(dto);

        var actual = _sut.GetByExternalReference(extRef, "EMR_RJ");

        actual.Should().NotBeNull();
        actual.InpatientOrderId.Should().Be(id);
        actual.ExternalReferenceId.Should().Be(extRef);
    }

    private static string GenerateOrderId() =>
        $"OPN{DateTime.Now:yy}{Random.Shared.Next(10000000, 99999999)}"[..12];

    private static InpatientOrderDto CreateTestDto(
        string id,
        InpatientOrderStatusEnum status,
        OrderSourceEnum source) =>
        new(
            id,
            (int)source,
            (int)status,
            $"337502{Random.Shared.Next(100000000, 999999999)}"[..15],
            "PASIEN TEST",
            new DateTime(1990, 1, 1),
            "L",
            "D001",
            "dr. Budi Sp.PD",
            DateTime.Today.AddDays(1),
            "1",
            "Kelas 1",
            "B01",
            "Bangsal Mawar",
            "Febris H-4",
            "BPJ",
            "BPJS Kesehatan",
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
