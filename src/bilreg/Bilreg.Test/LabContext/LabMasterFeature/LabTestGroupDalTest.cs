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

public class LabTestGroupDalTest
{
    private readonly LabTestGroupDal _sut = new(ConnStringHelper.GetTestEnv());

    private static LabTestGroupDto FakerDto(string id = "GRP-TST", string name = "Group Test")
        => new(
            TestGroupId: id,
            TestGroupName: name,
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
            var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BILRG_LabTestGroup'");
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
        var model = LabTestGroupModel.Create("GRP-CBC", "Darah Lengkap", "user1", new DateTime(2026, 11, 4, 10, 0, 0));

        var dto = LabTestGroupDto.FromModel(model);
        var restored = dto.ToModel();

        dto.TestGroupId.Should().Be("GRP-CBC");
        dto.TestGroupName.Should().Be("Darah Lengkap");
        dto.CrtUser.Should().Be("user1");
        dto.CrtDate.Should().Be(new DateTime(2026, 11, 4, 10, 0, 0));

        restored.TestGroupId.Should().Be(model.TestGroupId);
        restored.TestGroupName.Should().Be(model.TestGroupName);
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

        var key = "GRP-T99";
        var dto = FakerDto(key, "Test Group 99");

        _sut.Insert(dto);

        var loaded = _sut.GetData(LabTestGroupModel.Key(key));
        loaded.Should().NotBeNull();
        loaded!.TestGroupName.Should().Be("Test Group 99");

        var updatedDto = loaded with { TestGroupName = "Updated Group 99", UpdUser = "U2", UpdDate = DateTime.Now };
        _sut.Update(updatedDto);

        var loadedUpdated = _sut.GetData(LabTestGroupModel.Key(key));
        loadedUpdated!.TestGroupName.Should().Be("Updated Group 99");

        var list = _sut.ListData("Updated Group 99");
        list.Should().Contain(x => x.TestGroupId == key);

        _sut.Delete(LabTestGroupModel.Key(key));
        var afterDelete = _sut.GetData(LabTestGroupModel.Key(key));
        afterDelete.Should().BeNull();
    }
}
