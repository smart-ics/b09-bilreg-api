using System.Data.SqlClient;
using Bilreg.Application.LabContext.LabMasterFeature;
using Bilreg.Domain.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.LabContext.LabMasterFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabMasterFeature;

public class LabAntibioticDalTest
{
    private readonly LabAntibioticDal _sut = new(ConnStringHelper.GetTestEnv());

    private static LabAntibioticDto FakerDto(
        string id = "ANTI-TST",
        string name = "Amikacin Test",
        string diskConc = "30 ug",
        string resisten = "<=14",
        string intermediate = "15-16",
        string sensitif = ">=17",
        int displayOrder = 10)
        => new(
            AntibioticId: id,
            AntibioticName: name,
            DiskConcentration: diskConc,
            ResistenRange: resisten,
            IntermediateRange: intermediate,
            SensitifRange: sensitif,
            DisplayOrder: displayOrder,
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
            var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BILRG_LabAntibiotic'");
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
        var model = LabAntibioticModel.Create(
            "ANTI-01",
            "Amikacin",
            "30 ug",
            "<=14",
            "15-16",
            ">=17",
            10,
            "user1",
            new DateTime(2026, 11, 4, 10, 0, 0));

        var dto = LabAntibioticDto.FromModel(model);
        var restored = dto.ToModel();

        dto.AntibioticId.Should().Be("ANTI-01");
        dto.AntibioticName.Should().Be("Amikacin");
        dto.DiskConcentration.Should().Be("30 ug");
        dto.ResistenRange.Should().Be("<=14");
        dto.IntermediateRange.Should().Be("15-16");
        dto.SensitifRange.Should().Be(">=17");
        dto.DisplayOrder.Should().Be(10);
        dto.CrtUser.Should().Be("user1");
        dto.CrtDate.Should().Be(new DateTime(2026, 11, 4, 10, 0, 0));

        restored.AntibioticId.Should().Be(model.AntibioticId);
        restored.AntibioticName.Should().Be(model.AntibioticName);
        restored.DiskConcentration.Should().Be(model.DiskConcentration);
        restored.ResistenRange.Should().Be(model.ResistenRange);
        restored.IntermediateRange.Should().Be(model.IntermediateRange);
        restored.SensitifRange.Should().Be(model.SensitifRange);
        restored.DisplayOrder.Should().Be(model.DisplayOrder);
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

        var key = "ANTI-T99";
        var dto = FakerDto(key, "Test Antibiotic 99", "10 ug", "<=10", "11-15", ">=16", 99);

        _sut.Insert(dto);

        var loaded = _sut.GetData(LabAntibioticModel.Key(key));
        loaded.Should().NotBeNull();
        loaded!.AntibioticName.Should().Be("Test Antibiotic 99");
        loaded.DiskConcentration.Should().Be("10 ug");
        loaded.ResistenRange.Should().Be("<=10");
        loaded.IntermediateRange.Should().Be("11-15");
        loaded.SensitifRange.Should().Be(">=16");
        loaded.DisplayOrder.Should().Be(99);

        var updatedDto = loaded with
        {
            AntibioticName = "Updated Antibiotic 99",
            DiskConcentration = "20 ug",
            ResistenRange = "<=12",
            IntermediateRange = "13-17",
            SensitifRange = ">=18",
            DisplayOrder = 100,
            UpdUser = "U2",
            UpdDate = DateTime.Now
        };
        _sut.Update(updatedDto);

        var loadedUpdated = _sut.GetData(LabAntibioticModel.Key(key));
        loadedUpdated.Should().NotBeNull();
        loadedUpdated!.AntibioticName.Should().Be("Updated Antibiotic 99");
        loadedUpdated.DiskConcentration.Should().Be("20 ug");
        loadedUpdated.ResistenRange.Should().Be("<=12");
        loadedUpdated.IntermediateRange.Should().Be("13-17");
        loadedUpdated.SensitifRange.Should().Be(">=18");
        loadedUpdated.DisplayOrder.Should().Be(100);

        var list = _sut.ListData("Updated Antibiotic 99");
        list.Should().Contain(x => x.AntibioticId == key);

        var listAll = _sut.ListData();
        listAll.Should().Contain(x => x.AntibioticId == key);

        _sut.Delete(LabAntibioticModel.Key(key));
        var afterDelete = _sut.GetData(LabAntibioticModel.Key(key));
        afterDelete.Should().BeNull();
    }
}
