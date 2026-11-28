using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class PreOpClearanceChecklistDalTest
{
    private readonly PreOpClearanceChecklistDal _sut = new(ConnStringHelper.GetTestEnv());

    private static List<PreOpClearanceChecklistDto> FakerList(string clearanceId = "CLR-TEST-001")
        =>
        [
            new(clearanceId, 1, PreOpChecklistItemType.PatientIdentity, (int)ChecklistItemStatusEnum.Verified, "Identitas cocok"),
            new(clearanceId, 2, PreOpChecklistItemType.InformedConsent, (int)ChecklistItemStatusEnum.Cleared, "Ditandatangani")
        ];

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(FakerList());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        const string clearanceId = "CLR-TEST-DEL";
        _sut.Insert(FakerList(clearanceId));

        _sut.Delete(clearanceId);

        var actual = _sut.ListData(clearanceId);
        actual.Should().BeEmpty();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        const string clearanceId = "CLR-TEST-LIST";
        var item2 = new PreOpClearanceChecklistDto(
            clearanceId, 2, PreOpChecklistItemType.InformedConsent, (int)ChecklistItemStatusEnum.Cleared, "Signed");
        var item1 = new PreOpClearanceChecklistDto(
            clearanceId, 1, PreOpChecklistItemType.PatientIdentity, (int)ChecklistItemStatusEnum.Verified, "OK");

        _sut.Insert([item2, item1]);

        var actual = _sut.ListData(clearanceId).ToList();

        actual.Should().HaveCount(2);
        actual[0].Should().BeEquivalentTo(item1);
        actual[1].Should().BeEquivalentTo(item2);
        actual.Should().BeInAscendingOrder(x => x.NoUrut);
    }
}
