using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OrderOpModelTest
{
    private static RegModel CreateSampleReg() => RegModel.Default;

    private static LayananType CreateSampleLayanan()
    {
        return new LayananType(
            "LYN-001",
            "Klinik Bedah",
            true,
            InstalasiType.Default.ToReff(),
            LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default,
            InstalasiDkType.Default,
            Bilreg.Domain.AccountingContext.UnitFeature.UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));
    }

    private static PpaType CreateSamplePpa(string id = "PPA-001", string name = "dr. Pengorder, Sp.B")
    {
        return new PpaType(
            id,
            name,
            "Sp.B",
            SmfType.Default,
            GroupSpesialisType.Default,
            new List<PpaLayananType>(),
            new List<PpaSatTugasType>(),
            new List<ContactType>());
    }

    [Fact]
    public void FactoryMethod_CreateByPasien_ShouldBeRemovedFromDomain()
    {
        // Assert: TD-01 mandates total elimination of CreateByPasien from OrderOpModel
        var method = typeof(OrderOpModel).GetMethod("CreateByPasien");
        method.Should().BeNull("CreateByPasien factory method must be completely removed from OrderOpModel");
    }

    [Fact]
    public void CreateByReg_WithValidParameters_ShouldInstantiateOrderOpModelWithDefaults()
    {
        // Arrange
        var reg = CreateSampleReg();
        var layanan = CreateSampleLayanan();
        var dokter = CreateSamplePpa();
        var userId = "user-nurse";
        var createdAt = new DateTime(2026, 10, 16, 8, 30, 0);

        // Act
        var sut = OrderOpModel.CreateByReg(reg, layanan, dokter, userId, createdAt);

        // Assert
        sut.Should().NotBeNull();
        sut.OrderOpId.Should().NotBeNullOrWhiteSpace();
        sut.OrderOpId.Length.Should().Be(26);
        sut.OrderDate.Should().Be(createdAt);
        sut.Pasien.Should().Be(reg.Pasien);
        sut.Reg.Should().Be(reg.ToReff());
        sut.DokterPengorder.Should().Be(dokter.ToReff());
        sut.LayananPengorder.Should().Be(layanan.ToReff());
        sut.DokterOperator.Should().Be(PpaReff.Default);
        sut.Dokter.Should().Be(PpaReff.Default);
        sut.ProsedurTambahan.Should().BeEmpty();
        sut.KlasifikasiOperasi.Should().Be(KlasifikasiOperasiEnum.Kecil);
        sut.AnestesiPlan.Should().Be(AnestesiPlanEnum.General);
        sut.PostOpCareUnit.Should().Be(PostOpCareUnitEnum.Ward);
        sut.AlasanBatal.Should().BeEmpty();
        sut.UrgencyLevel.Should().Be(UrgencyLevelEnum.Elective);
        sut.TarifOperasi.Should().Be(TarifReff.Default);
        sut.ListResource.Should().BeEmpty();
        sut.AuditTrail.Created.UserId.Should().Be(userId);
        sut.AuditTrail.Created.Timestamp.Should().Be(createdAt);
        sut.AuditTrail.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void CreateByReg_UsingReffOverload_ShouldInstantiateProperly()
    {
        // Arrange
        var reg = CreateSampleReg();
        var layananReff = new LayananReff("LYN-002", "IGD");
        var dokterReff = new PpaReff("DOC-002", "dr. Cito, Sp.B");
        var userId = "user-dok";
        var createdAt = DateTime.Now;

        // Act
        var sut = OrderOpModel.CreateByReg(reg, layananReff, dokterReff, userId, createdAt);

        // Assert
        sut.DokterPengorder.Should().Be(dokterReff);
        sut.LayananPengorder.Should().Be(layananReff);
    }

    [Fact]
    public void CreateByReg_NullReg_ShouldThrowArgumentNullException()
    {
        // Arrange
        var layanan = CreateSampleLayanan();
        var dokter = CreateSamplePpa();

        // Act & Assert
        FluentActions.Invoking(() => OrderOpModel.CreateByReg(null!, layanan, dokter, "u1"))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateByReg_NullLayananPengorder_ShouldThrowArgumentNullException()
    {
        // Arrange
        var reg = CreateSampleReg();
        var dokter = CreateSamplePpa();

        // Act & Assert
        FluentActions.Invoking(() => OrderOpModel.CreateByReg(reg, (LayananType)null!, dokter, "u1"))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateByReg_NullDokterPengorder_ShouldThrowArgumentNullException()
    {
        // Arrange
        var reg = CreateSampleReg();
        var layanan = CreateSampleLayanan();

        // Act & Assert
        FluentActions.Invoking(() => OrderOpModel.CreateByReg(reg, layanan, (PpaType)null!, "u1"))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SetProsedur_ValidValues_ShouldUpdateProperties()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act
        sut.SetProsedur("Apendektomi Laparoskopi", "Adhesiolisis", KlasifikasiOperasiEnum.Sedang);

        // Assert
        sut.NamaOperasi.Should().Be("Apendektomi Laparoskopi");
        sut.ProsedurTambahan.Should().Be("Adhesiolisis");
        sut.KlasifikasiOperasi.Should().Be(KlasifikasiOperasiEnum.Sedang);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetProsedur_NullOrWhitespaceNamaOperasi_ShouldThrowException(string? invalidName)
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act & Assert
        FluentActions.Invoking(() => sut.SetProsedur(invalidName, "Sekunder", KlasifikasiOperasiEnum.Khusus))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetProsedur_NullProsedurTambahan_ShouldDefaultToEmptyString()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act
        sut.SetProsedur("Mastektomi", null!, KlasifikasiOperasiEnum.Besar);

        // Assert
        sut.NamaOperasi.Should().Be("Mastektomi");
        sut.ProsedurTambahan.Should().BeEmpty();
        sut.KlasifikasiOperasi.Should().Be(KlasifikasiOperasiEnum.Besar);
    }

    [Fact]
    public void SetRencanaAnestesiDanPerawatan_ShouldUpdateProperties()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act
        sut.SetRencanaAnestesiDanPerawatan(AnestesiPlanEnum.RegionalSpinal, PostOpCareUnitEnum.ICU);

        // Assert
        sut.AnestesiPlan.Should().Be(AnestesiPlanEnum.RegionalSpinal);
        sut.PostOpCareUnit.Should().Be(PostOpCareUnitEnum.ICU);
    }

    [Fact]
    public void SetDokterOperator_ShouldUpdateDokterOperatorAndDokter()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");
        var operatorDoc = new PpaReff("DOC-OPR-1", "dr. Operator, Sp.OT");

        // Act
        sut.SetDokterOperator(operatorDoc);

        // Assert
        sut.DokterOperator.Should().Be(operatorDoc);
        sut.Dokter.Should().Be(operatorDoc);
    }

    [Fact]
    public void SetDokterOperator_Null_ShouldDefaultToDefaultPpaReff()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act
        sut.SetDokterOperator((PpaReff)null!);

        // Assert
        sut.DokterOperator.Should().Be(PpaReff.Default);
        sut.Dokter.Should().Be(PpaReff.Default);
    }

    [Fact]
    public void SetUrgencyLevel_ShouldUpdateUrgency()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act
        sut.SetUrgencyLevel(UrgencyLevelEnum.Cito);

        // Assert
        sut.UrgencyLevel.Should().Be(UrgencyLevelEnum.Cito);
    }

    [Fact]
    public void SetTarif_NullTarif_ShouldDefaultToDefaultTarifReff()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act
        sut.SetTarif((TarifReff)null!);

        // Assert
        sut.TarifOperasi.Should().Be(TarifReff.Default);
    }

    [Fact]
    public void OperationalRequest_ValidParameters_ShouldUpdateOperationalInfo()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");
        var operatorDoc = CreateSamplePpa("DOC-SURG", "dr. Bedah");
        var preferedDate = DateTime.Today.AddDays(1);
        var requestedAt = DateTime.Today;

        // Act
        sut.OperationalRequest(operatorDoc, 90, preferedDate, "Fluoroscopy, Harmonik Scalpel", requestedAt);

        // Assert
        sut.DokterOperator.Should().Be(operatorDoc.ToReff());
        sut.Dokter.Should().Be(operatorDoc.ToReff());
        sut.EstimasiDurasiInMinutes.Should().Be(90);
        sut.PreferedDate.Should().Be(preferedDate);
        sut.SpecialEquipment.Should().Be("Fluoroscopy, Harmonik Scalpel");
    }

    [Fact]
    public void OperationalRequest_PreferedDateBeforeRequestedAt_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");
        var operatorDoc = CreateSamplePpa();

        // Act & Assert
        FluentActions.Invoking(() => sut.OperationalRequest(operatorDoc, 60, DateTime.Today.AddDays(-1), "C-Arm", DateTime.Today))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Prefered Date invalid*");
    }

    [Fact]
    public void ResourceManagement_AddAndRemoveResources_ShouldManageChildCollection()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        var resource1 = OrderOpResourceModel.Create(
            "RES-01", sut.OrderOpId, OrderOpResourceTypeEnum.Darah, "PRC", 2, "Kantong", "O Rh+");
        var resource2 = OrderOpResourceModel.Create(
            "RES-02", sut.OrderOpId, OrderOpResourceTypeEnum.Implan, "Screw 4.0", 4, "Pcs", "Titanium");

        // Act: Add using Model
        sut.AddResource(resource1);
        sut.AddResource(resource2);

        // Act: Add using Factory Overload
        sut.AddResource(OrderOpResourceTypeEnum.AlatKhusus, "Set Laparoskopi", 1, "Set", "HD Camera");

        // Assert
        sut.ListResource.Should().HaveCount(3);
        sut.ListResource.Should().Contain(resource1);
        sut.ListResource.Should().Contain(resource2);

        // Act: Remove
        sut.RemoveResource("RES-01");

        // Assert
        sut.ListResource.Should().HaveCount(2);
        sut.ListResource.Should().NotContain(resource1);
    }

    [Fact]
    public void SetResources_ShouldReplaceExistingResources()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");
        sut.AddResource(OrderOpResourceTypeEnum.Darah, "PRC", 1, "Kantong");

        var newResources = new List<OrderOpResourceModel>
        {
            OrderOpResourceModel.Create("R1", sut.OrderOpId, OrderOpResourceTypeEnum.Implan, "Plate", 1, "Pcs", ""),
            OrderOpResourceModel.Create("R2", sut.OrderOpId, OrderOpResourceTypeEnum.AlatKhusus, "Harmonic", 1, "Unit", "")
        };

        // Act
        sut.SetResources(newResources);

        // Assert
        sut.ListResource.Should().HaveCount(2);
        sut.ListResource.Should().BeEquivalentTo(newResources);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_NullOrWhitespaceAlasanBatal_ShouldThrowArgumentException(string? invalidAlasan)
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act & Assert
        FluentActions.Invoking(() => sut.Cancel("user-cancel", invalidAlasan!, DateTime.Now))
            .Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_NullOrWhitespaceUserId_ShouldThrowArgumentException(string? invalidUser)
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");

        // Act & Assert
        FluentActions.Invoking(() => sut.Cancel(invalidUser!, "Kondisi pasien tidak stabil", DateTime.Now))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cancel_ValidParameters_ShouldSetVoidAuditAndStateCancelled()
    {
        // Arrange
        var sut = OrderOpModel.CreateByReg(CreateSampleReg(), CreateSampleLayanan(), CreateSamplePpa(), "u1");
        var userId = "user-doctor";
        var cancelledAt = new DateTime(2026, 10, 16, 14, 0, 0);
        var alasanBatal = "Pasien demam tinggi dan batuk aktif, ditunda hingga evaluasi paru";

        // Act
        sut.Cancel(userId, alasanBatal, cancelledAt);

        // Assert
        sut.AlasanBatal.Should().Be(alasanBatal);
        sut.OrderOpState.Should().Be(OpCaseStateEnum.Cancelled);
        sut.AuditTrail.IsVoided.Should().BeTrue();
        sut.AuditTrail.Voided.UserId.Should().Be(userId);
        sut.AuditTrail.Voided.Timestamp.Should().Be(cancelledAt);
    }
}
