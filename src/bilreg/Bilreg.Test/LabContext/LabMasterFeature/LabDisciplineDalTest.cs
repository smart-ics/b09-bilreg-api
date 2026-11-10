using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabDisciplineDalTest
{
    private readonly LabDisciplineDal _sut = new(ConnStringHelper.GetTestEnv());

    private static LabDisciplineDto FakerDto(string id = "DISC-TST", string name = "Discipline Test")
        => new(
            DisciplineId: id,
            DisciplineName: name,
            CrtUser: "U1",
            CrtDate: new DateTime(2026, 11, 4, 9, 0, 0),
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
            var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BILRG_LabDiscipline'");
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
        var model = LabDisciplineModel.Create("DISC-HEM", "Hematologi", "user1", new DateTime(2026, 11, 4, 10, 0, 0));

        var dto = LabDisciplineDto.FromModel(model);
        var restored = dto.ToModel();

        dto.DisciplineId.Should().Be("DISC-HEM");
        dto.DisciplineName.Should().Be("Hematologi");
        dto.CrtUser.Should().Be("user1");
        dto.CrtDate.Should().Be(new DateTime(2026, 11, 4, 10, 0, 0));

        restored.DisciplineId.Should().Be(model.DisciplineId);
        restored.DisciplineName.Should().Be(model.DisciplineName);
        restored.AuditTrail.Created.UserId.Should().Be("user1");
        restored.AuditTrail.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void ToModel_WhenVoidedInAuditTrail_SetsIsVoidedTrue()
    {
        var dto = FakerDto() with
        {
            VodUser = "VOID_USER",
            VodDate = new DateTime(2026, 11, 4, 12, 0, 0)
        };

        var restored = dto.ToModel();

        restored.AuditTrail.IsVoided.Should().BeTrue();
        restored.AuditTrail.Voided.UserId.Should().Be("VOID_USER");
    }

    [Fact]
    public void DatabaseCrud_WhenTableAvailable_ExecutesSuccessfully()
    {
        if (!IsTableAvailable())
            return;

        using var trans = TransHelper.NewScope();

        var key = "DISC-T99";
        var dto = FakerDto(key, "Test Discipline 99");

        _sut.Insert(dto);

        var loaded = _sut.GetData(LabDisciplineModel.Key(key));
        loaded.Should().NotBeNull();
        loaded!.DisciplineName.Should().Be("Test Discipline 99");

        var updatedDto = loaded with { DisciplineName = "Updated Discipline 99", UpdUser = "U2", UpdDate = DateTime.Now };
        _sut.Update(updatedDto);

        var loadedUpdated = _sut.GetData(LabDisciplineModel.Key(key));
        loadedUpdated!.DisciplineName.Should().Be("Updated Discipline 99");

        var list = _sut.ListData("Updated Discipline 99");
        list.Should().Contain(x => x.DisciplineId == key);

        _sut.Delete(LabDisciplineModel.Key(key));
        var afterDelete = _sut.GetData(LabDisciplineModel.Key(key));
        afterDelete.Should().BeNull();
    }
}
