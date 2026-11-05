using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmissionDalTest
{
    private readonly AdmissionDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly PasienDal _pasienDal = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void UT_DAL_01_GivenAdmissionDto_WhenInsertAndGetData_ThenRoundTripsSuccessfully()
    {
        using var trans = TransHelper.NewScope();
        var regId = GenerateRegId();
        var pasienId = GeneratePasienId15("1234");
        var dto = CreateTestDto(regId, pasienId, AdmissionStatusEnum.Admitted);

        _sut.Insert(dto);
        var actual = _sut.GetData(AdmissionModel.Key(regId));

        actual.Should().NotBeNull();
        actual.RegId.Should().Be(regId);
        actual.PasienId.Should().Be(pasienId);
        actual.AdmissionStatus.Should().Be((int)AdmissionStatusEnum.Admitted);
        actual.BangsalId.Should().Be("B01");
        actual.KelasDkId.Should().Be("1");
    }

    [Fact]
    public void UT_DAL_02_GivenStatusAndPasienId_WhenListDataWithoutSearch_ThenFiltersCorrectly()
    {
        using var trans = TransHelper.NewScope();
        var regId1 = GenerateRegId();
        var regId2 = GenerateRegId();
        var pasienId1 = GeneratePasienId15("2001");
        var pasienId2 = GeneratePasienId15("2002");

        var dto1 = CreateTestDto(regId1, pasienId1, AdmissionStatusEnum.Admitted);
        var dto2 = CreateTestDto(regId2, pasienId2, AdmissionStatusEnum.Completed);
        _sut.Insert(dto1);
        _sut.Insert(dto2);

        // Filter by PasienId exact
        var filterPasien = new AdmissionListFilter(PasienId: pasienId1);
        var listPasien = _sut.ListData(filterPasien).ToList();
        listPasien.Should().Contain(x => x.RegId == regId1);
        listPasien.Should().NotContain(x => x.RegId == regId2);

        // Filter by Status
        var filterStatus = new AdmissionListFilter(Status: AdmissionStatusEnum.Completed, PasienId: pasienId2);
        var listStatus = _sut.ListData(filterStatus).ToList();
        listStatus.Should().Contain(x => x.RegId == regId2);
        listStatus.Should().NotContain(x => x.RegId == regId1);
    }

    [Fact]
    public void UT_DAL_03_Given15DigitPasienIdSearch_WhenListData_ThenMatchesFullRM()
    {
        using var trans = TransHelper.NewScope();
        var regId = GenerateRegId();
        var pasienId = GeneratePasienId15("3344");
        var dto = CreateTestDto(regId, pasienId, AdmissionStatusEnum.Admitted);
        _sut.Insert(dto);

        var filter = new AdmissionListFilter(Search: pasienId);
        var result = _sut.ListData(filter).ToList();

        result.Should().Contain(x => x.RegId == regId && x.PasienId == pasienId);
    }

    [Fact]
    public void UT_DAL_04_Given6DigitShortRMSearch_WhenListData_ThenMatchesRight6Digits()
    {
        using var trans = TransHelper.NewScope();
        var regId = GenerateRegId();
        var shortRm = "005566";
        var pasienId = $"337502200{shortRm}"; // 15 digits ending in 005566
        var dto = CreateTestDto(regId, pasienId, AdmissionStatusEnum.Admitted);
        _sut.Insert(dto);

        var filter = new AdmissionListFilter(Search: shortRm);
        var result = _sut.ListData(filter).ToList();

        result.Should().Contain(x => x.RegId == regId && x.PasienId == pasienId);
    }

    [Fact]
    public void UT_DAL_05_GivenFormattedShortRMSearch_WhenListData_ThenSanitizesAndMatches()
    {
        using var trans = TransHelper.NewScope();
        var regId = GenerateRegId();
        var shortRm = "007788";
        var formattedSearch = "00-77-88";
        var pasienId = $"337502200{shortRm}";
        var dto = CreateTestDto(regId, pasienId, AdmissionStatusEnum.Admitted);
        _sut.Insert(dto);

        var filter = new AdmissionListFilter(Search: formattedSearch);
        var result = _sut.ListData(filter).ToList();

        result.Should().Contain(x => x.RegId == regId && x.PasienId == pasienId);
    }

    [Fact]
    public void UT_DAL_06_GivenRegIdSearch_WhenListData_ThenMatchesRegId()
    {
        using var trans = TransHelper.NewScope();
        var regId = GenerateRegId();
        var pasienId = GeneratePasienId15("8899");
        var dto = CreateTestDto(regId, pasienId, AdmissionStatusEnum.Admitted);
        _sut.Insert(dto);

        var filter = new AdmissionListFilter(Search: regId);
        var result = _sut.ListData(filter).ToList();

        result.Should().Contain(x => x.RegId == regId);
    }

    [Fact]
    public void UT_DAL_07_GivenPatientNameSearch_WhenListData_ThenMatchesJoinedTcMrName()
    {
        using var trans = TransHelper.NewScope();
        var regId = GenerateRegId();
        var pasienId = GeneratePasienId15("9900");
        var uniqueName = $"NURHALIZA {Random.Shared.Next(1000, 9999)}";

        var pasien = CreateTestPasien(pasienId, uniqueName);
        _pasienDal.Insert(pasien);

        var dto = CreateTestDto(regId, pasienId, AdmissionStatusEnum.Admitted);
        _sut.Insert(dto);

        var filter = new AdmissionListFilter(Search: "NURHALIZA");
        var result = _sut.ListData(filter).ToList();

        result.Should().Contain(x => x.RegId == regId && x.PasienName.Contains("NURHALIZA"));
    }

    [Fact]
    public void UT_DAL_08_GivenSearchWithSpecialChars_WhenListData_ThenSanitizesSafelyWithoutSqlError()
    {
        using var trans = TransHelper.NewScope();
        var regId = GenerateRegId();
        var shortRm = "008811";
        var pasienId = $"337502200{shortRm}";
        var dto = CreateTestDto(regId, pasienId, AdmissionStatusEnum.Admitted);
        _sut.Insert(dto);

        var dirtySearch = "  !@#$ 008811 %^&* ";
        var filter = new AdmissionListFilter(Search: dirtySearch);
        var result = _sut.ListData(filter).ToList();

        result.Should().Contain(x => x.RegId == regId);
    }

    [Fact]
    public void UT_DAL_09_GivenNullOrEmptySearch_WhenListData_ThenPreservesCompatibilityAndRespectsTop50()
    {
        using var trans = TransHelper.NewScope();
        var filter = new AdmissionListFilter(Status: AdmissionStatusEnum.Admitted);
        var result = _sut.ListData(filter).ToList();

        result.Should().NotBeNull();
        result.Count.Should().BeLessThanOrEqualTo(50);
    }

    private static string GenerateRegId() => $"RG{Random.Shared.Next(10000000, 99999999)}";

    private static string GeneratePasienId15(string suffix) => $"33750220000{suffix}"[..15];

    private static AdmissionDto CreateTestDto(string regId, string pasienId, AdmissionStatusEnum status) =>
        new(
            regId,
            (int)status,
            (int)AdmissionSourceEnum.Admission,
            pasienId,
            "",
            "3000-01-01",
            "",
            "1",
            "Kelas 1",
            "B01",
            "Bangsal Mawar",
            new DateTime(2026, 11, 2),
            "test-user",
            DateTime.Now,
            "",
            new DateTime(3000, 1, 1),
            "",
            new DateTime(3000, 1, 1));

    private static PasienDto CreateTestPasien(string pasienId, string pasienName) =>
        new(
            pasienId, pasienName, "1990-01-01", "P", "-", "-", "-", "-",
            "-", "-", "-", "-", "-", "-",
            "-", "-", "-", "-", "-", "-",
            "-", "-", "-", "-", "-", "-", "-",
            "-", "-", "-", "-", "-", "-", true, "-", "-", "-", "-", "-", "-",
            "-", "-", "-", "-", "-", "-", "-");
}
