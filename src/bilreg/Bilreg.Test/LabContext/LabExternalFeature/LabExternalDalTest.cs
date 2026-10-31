using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabExternalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.LabContext.LabExternalFeature;

public class LabExternalDalTest
{
    private readonly LabExternalDal _sut = new(ConnStringHelper.GetTestEnv());

    private static LabExternalDto FakerDto(string regExternalId = "LEX26A14B8C1D", string externalMrNo = "EX-202610-00001")
        => new(
            RegExternalId: regExternalId,
            ExternalMrNo: externalMrNo,
            PatientName: "John Doe",
            Nik: "3171019988770001",
            BirthDate: new DateTime(1985, 3, 15),
            Gender: "L",
            Phone: "081298765432",
            Address: "Jl. Sudirman No. 45",
            ReferralType: (int)LabReferralTypeEnum.Aps,
            ReferralDoctor: "Dr. Pratama",
            ReferralOrigin: "Klinik Pratama",
            ClinicalNotes: "Check kolesterol",
            BillingAccountId: regExternalId,
            Status: (int)LabExternalStatusEnum.Active,
            CancelReason: "",
            CancelledUserId: "",
            CancelledDate: new DateTime(3000, 1, 1),
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 10, 14, 9, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1));

    private static bool IsTableAvailable()
    {
        try
        {
            using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
            conn.Open();
            var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BILRG_LabExternal'");
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void FromModel_And_ToModel_RoundTripsAllFieldsAccurately()
    {
        // Arrange
        var demographics = new LabExternalDemographicsType(
            "Jane Smith",
            "3172021122330002",
            new DateTime(1992, 7, 24),
            "P",
            "081311223344",
            "Jl. Kebon Jeruk No. 8");

        var referral = new LabExternalReferralInfoType(
            LabReferralTypeEnum.FaskesLuar,
            "Dr. Santoso",
            "RS Siloam",
            "Evaluasi hematologi");

        var audit = new AuditInfoType("USER_ADMIN", new DateTime(2026, 10, 14, 10, 15, 0));

        var model = LabExternalModel.Create(
            "LEX26A14B8C99",
            "EX-202610-00099",
            demographics,
            referral,
            "LEX26A14B8C99",
            audit);

        // Act
        var dto = LabExternalDto.FromModel(model);
        var restored = dto.ToModel();

        // Assert
        dto.RegExternalId.Should().Be("LEX26A14B8C99");
        dto.ExternalMrNo.Should().Be("EX-202610-00099");
        dto.PatientName.Should().Be("Jane Smith");
        dto.Nik.Should().Be("3172021122330002");
        dto.BirthDate.Should().Be(new DateTime(1992, 7, 24));
        dto.Gender.Should().Be("P");
        dto.Phone.Should().Be("081311223344");
        dto.Address.Should().Be("Jl. Kebon Jeruk No. 8");
        dto.ReferralType.Should().Be((int)LabReferralTypeEnum.FaskesLuar);
        dto.ReferralDoctor.Should().Be("Dr. Santoso");
        dto.ReferralOrigin.Should().Be("RS Siloam");
        dto.ClinicalNotes.Should().Be("Evaluasi hematologi");
        dto.BillingAccountId.Should().Be("LEX26A14B8C99");
        dto.Status.Should().Be((int)LabExternalStatusEnum.Active);
        dto.CrtUser.Should().Be("USER_ADMIN");
        dto.CrtDate.Should().Be(new DateTime(2026, 10, 14, 10, 15, 0));

        restored.RegExternalId.Should().Be(model.RegExternalId);
        restored.ExternalMrNo.Should().Be(model.ExternalMrNo);
        restored.Demographics.PatientName.Should().Be("Jane Smith");
        restored.ReferralInfo.ReferralDoctor.Should().Be("Dr. Santoso");
        restored.Status.Should().Be(LabExternalStatusEnum.Active);
    }

    [Fact]
    public void ToModel_WhenVoidedInAuditTrail_SetsIsVoidedTrue()
    {
        // Arrange
        var dto = FakerDto() with
        {
            VodUser = "VOID_USER",
            VodDate = new DateTime(2026, 10, 15, 12, 0, 0)
        };

        // Act
        var model = dto.ToModel();

        // Assert
        model.AuditTrail.IsVoided.Should().BeTrue();
        model.AuditTrail.Voided.UserId.Should().Be("VOID_USER");
        model.AuditTrail.Voided.Timestamp.Should().Be(new DateTime(2026, 10, 15, 12, 0, 0));
    }

    [Fact]
    public void InsertAndGetData_RoundTripsAccurately_WhenDatabaseAvailable()
    {
        if (!IsTableAvailable())
            return;

        using var trans = TransHelper.NewScope();
        var dto = FakerDto("LEXTEST0001", "EXT-TEST-00001");
        _sut.Insert(dto);

        var actual = _sut.GetData(LabExternalModel.Key("LEXTEST0001"));
        actual.Should().NotBeNull();
        actual.RegExternalId.Should().Be("LEXTEST0001");
        actual.ExternalMrNo.Should().Be("EXT-TEST-00001");
        actual.PatientName.Should().Be("John Doe");
        actual.Nik.Should().Be("3171019988770001");
        actual.Status.Should().Be((int)LabExternalStatusEnum.Active);
    }

    [Fact]
    public void GetByExternalMrNo_ReturnsCorrectDto_WhenDatabaseAvailable()
    {
        if (!IsTableAvailable())
            return;

        using var trans = TransHelper.NewScope();
        var dto = FakerDto("LEXTEST0002", "EXT-TEST-00002");
        _sut.Insert(dto);

        var actual = _sut.GetByExternalMrNo("EXT-TEST-00002");
        actual.Should().NotBeNull();
        actual!.RegExternalId.Should().Be("LEXTEST0002");
        actual.ExternalMrNo.Should().Be("EXT-TEST-00002");
    }

    [Fact]
    public void Update_UpdatesFields_WhenDatabaseAvailable()
    {
        if (!IsTableAvailable())
            return;

        using var trans = TransHelper.NewScope();
        var dto = FakerDto("LEXTEST0003", "EXT-TEST-00003");
        _sut.Insert(dto);

        var updatedDto = dto with
        {
            PatientName = "John Updated",
            Status = (int)LabExternalStatusEnum.Completed,
            UpdUser = "U2",
            UpdDate = new DateTime(2026, 10, 15, 10, 0, 0)
        };
        _sut.Update(updatedDto);

        var actual = _sut.GetData(LabExternalModel.Key("LEXTEST0003"));
        actual.PatientName.Should().Be("John Updated");
        actual.Status.Should().Be((int)LabExternalStatusEnum.Completed);
        actual.UpdUser.Should().Be("U2");
    }

    [Fact]
    public void ListData_ReturnsMatchingRecords_WhenDatabaseAvailable()
    {
        if (!IsTableAvailable())
            return;

        using var trans = TransHelper.NewScope();
        var dto = FakerDto("LEXTEST0004", "EXT-TEST-00004");
        _sut.Insert(dto);

        var results = _sut.ListData("John", null, null, null).ToList();
        results.Should().Contain(x => x.RegExternalId == "LEXTEST0004");
    }

    [Fact]
    public void Delete_RemovesRecord_WhenDatabaseAvailable()
    {
        if (!IsTableAvailable())
            return;

        using var trans = TransHelper.NewScope();
        var dto = FakerDto("LEXTEST0005", "EXT-TEST-00005");
        _sut.Insert(dto);

        _sut.Delete(LabExternalModel.Key("LEXTEST0005"));
        var actual = _sut.GetData(LabExternalModel.Key("LEXTEST0005"));
        actual.Should().BeNull();
    }
}
