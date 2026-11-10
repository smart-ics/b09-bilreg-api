using System.Data.SqlClient;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabTestDefinitionFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabTestDefinitionFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabTestDefinitionFeature;

public class LabTestDefinitionDalTest
{
    private readonly LabTestDefinitionDal _sut = new(ConnStringHelper.GetTestEnv());

    private static LabTestDefinitionDto FakerDto(
        string id = "LTD9999",
        string tarifId = "TR999",
        string labTestCode = "T99",
        int serviceType = 2,
        string disciplineId = "DISC-PAH",
        string testGroupId = "GRP-BIO") =>
        new(
            TestDefinitionId: id,
            TarifId: tarifId,
            TarifCode: "T-999",
            TarifName: "Tarif Biopsi 99",
            LabTestCode: labTestCode,
            LabTestName: "Biopsi Test 99",
            SpecimenType: "Jaringan",
            VacutainerType: (int)VacutainerTypeEnum.None,
            IsActive: true,
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 11, 4, 10, 0, 0),
            UpdUser: "",
            UpdDate: new DateTime(3000, 1, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1),
            ServiceType: serviceType,
            DisciplineId: disciplineId,
            TestGroupId: testGroupId);

    private static bool IsTableAvailable()
    {
        try
        {
            using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
            conn.Open();
            var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BILRG_LabTestDefinition'");
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void FromModel_And_ToModel_RoundTripsClassificationFields()
    {
        var model = LabTestDefinitionModel.CreateNew(
            "LTD0001", "TR01", "T01", "Tarif PA", "PA-BIO", "Biopsi", "Jaringan",
            VacutainerTypeEnum.None, true, "admin", new DateTime(2026, 11, 4),
            [new LabTestComponentModel(1, "MLC0001", "", false, true)],
            new Dictionary<string, Bilreg.Domain.LabContext.LabComponentMasterFeature.LabComponentMasterModel>
            {
                ["MLC0001"] = new("MLC0001", null, "BIO", "Biopsi", Bilreg.Domain.LabContext.LabResultFeature.LabResultTypeEnum.Narrative, "", true, true)
            },
            serviceType: LaboratoryServiceType.AnatomicalPathology,
            disciplineId: "DISC-PAH",
            testGroupId: "GRP-BIO");

        var dto = LabTestDefinitionDto.FromModel(model);
        dto.ServiceType.Should().Be(2);
        dto.DisciplineId.Should().Be("DISC-PAH");
        dto.TestGroupId.Should().Be("GRP-BIO");

        var restored = dto.ToModel(model.Components);
        restored.ServiceType.Should().Be(LaboratoryServiceType.AnatomicalPathology);
        restored.DisciplineId.Should().Be("DISC-PAH");
        restored.TestGroupId.Should().Be("GRP-BIO");
        restored.VacutainerType.Should().Be(VacutainerTypeEnum.None);
    }

    [Fact]
    public void DatabaseCrud_WhenTableAvailable_PersistsAndReadsClassificationFields()
    {
        if (!IsTableAvailable())
            return;

        using var trans = TransHelper.NewScope();

        var key = "LTD9999";
        var dto = FakerDto(key, "TR999", "T99", serviceType: 3, disciplineId: "DISC-MIK", testGroupId: "GRP-MIC");

        _sut.Insert(dto);

        var loaded = _sut.GetData(LabTestDefinitionModel.Key(key));
        loaded.Should().NotBeNull();
        loaded!.ServiceType.Should().Be(3);
        loaded.DisciplineId.Should().Be("DISC-MIK");
        loaded.TestGroupId.Should().Be("GRP-MIC");

        var updatedDto = loaded with
        {
            ServiceType = 2,
            DisciplineId = "DISC-PAH",
            TestGroupId = "GRP-BIO",
            UpdUser = "U2",
            UpdDate = DateTime.Now
        };
        _sut.Update(updatedDto);

        var loadedUpdated = _sut.GetData(LabTestDefinitionModel.Key(key));
        loadedUpdated!.ServiceType.Should().Be(2);
        loadedUpdated.DisciplineId.Should().Be("DISC-PAH");
        loadedUpdated.TestGroupId.Should().Be("GRP-BIO");

        _sut.Delete(LabTestDefinitionModel.Key(key));
        var afterDelete = _sut.GetData(LabTestDefinitionModel.Key(key));
        afterDelete.Should().BeNull();
    }
}
