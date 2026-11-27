using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.RadContext.RadOrderFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadOrderRepoTest
{
    private readonly Mock<IRadOrderTableDal> _orderDal = new();
    private readonly Mock<IRadOrderItemDal> _itemDal = new();
    private readonly RadOrderRepo _sut;

    public RadOrderRepoTest()
    {
        _sut = new RadOrderRepo(_orderDal.Object, _itemDal.Object);
    }

    private static AuditInfoType TestAudit(string userId = "U1")
        => new(userId, new DateTime(2026, 11, 24, 9, 0, 0));

    private static PatientSnapshotType EmrSnapshot(
        string regId = "REG001",
        string patientId = "MR0001",
        string patientName = "Pasien Uji Coba")
        => new(
            regId,
            patientId,
            patientName,
            new DateTime(1990, 1, 15),
            "L",
            36);

    private static RadOrderItemModel CreateItem(
        string tarifId = "TRF01",
        string tarifName = "Thorax AP",
        RadModalityEnum modality = RadModalityEnum.CR,
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
        => RadOrderItemModel.Create(tarifId, tarifName, modality, urgency);

    private static RadOrderModel CreateTestModel()
    {
        return RadOrderModel.CreateFromEmr(
            emrOrderId: "EMR-RAD-001",
            patient: EmrSnapshot(),
            modality: RadModalityEnum.CR,
            items: [CreateItem()],
            orderNo: "RAD2611000001",
            audit: TestAudit(),
            dokterPengorderId: "DOK01",
            dokterPengorderName: "dr. Radiologi Spesialis",
            layananPengorderId: "POLI01",
            layananPengorderName: "Poli Rawat Jalan",
            clinicalNote: "Batuk pilek kronis",
            urgency: RadUrgencyEnum.Rutin);
    }

    [Fact]
    public void SaveChanges_NewEntity_CallsInsertAndReplacesItems()
    {
        // Arrange
        var model = CreateTestModel();
        _orderDal.Setup(x => x.GetData(model)).Returns((RadOrderDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _orderDal.Verify(x => x.Insert(It.Is<RadOrderDto>(d => d.OrderId == model.OrderId && d.EmrOrderId == model.EmrOrderId)), Times.Once);
        _orderDal.Verify(x => x.Update(It.IsAny<RadOrderDto>()), Times.Never);
        _itemDal.Verify(x => x.Delete(model), Times.Once);
        _itemDal.Verify(x => x.Insert(It.Is<IEnumerable<RadOrderItemDto>>(items => items.Count() == 1)), Times.Once);
    }

    [Fact]
    public void SaveChanges_ExistingEntity_CallsUpdateAndReplacesItems()
    {
        // Arrange
        var model = CreateTestModel();
        var existingDto = RadOrderDto.FromModel(model);
        _orderDal.Setup(x => x.GetData(model)).Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _orderDal.Verify(x => x.Update(It.Is<RadOrderDto>(d => d.OrderId == model.OrderId)), Times.Once);
        _orderDal.Verify(x => x.Insert(It.IsAny<RadOrderDto>()), Times.Never);
        _itemDal.Verify(x => x.Delete(model), Times.Once);
        _itemDal.Verify(x => x.Insert(It.Is<IEnumerable<RadOrderItemDto>>(items => items.Count() == 1)), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenFound_ReturnsAggregatedModel()
    {
        // Arrange
        var model = CreateTestModel();
        var dto = RadOrderDto.FromModel(model);
        var itemDto = RadOrderItemDto.FromModel(model.OrderId, model.Items.First());

        _orderDal.Setup(x => x.GetData(It.IsAny<IRadOrderKey>())).Returns(dto);
        _itemDal.Setup(x => x.ListData(It.IsAny<IRadOrderKey>())).Returns([itemDto]);

        // Act
        var result = _sut.LoadEntity(model);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.OrderId.Should().Be(model.OrderId);
                m.EmrOrderId.Should().Be(model.EmrOrderId);
                m.OrderNo.Should().Be(model.OrderNo);
                m.Modality.Should().Be(RadModalityEnum.CR);
                m.Items.Should().HaveCount(1);
                m.Items[0].TarifId.Should().Be("TRF01");
                m.Patient.RegId.Should().Be("REG001");
                m.Patient.PatientName.Should().Be("Pasien Uji Coba");
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        // Arrange
        var key = new RadOrderDto { OrderId = "NON_EXISTING" };
        _orderDal.Setup(x => x.GetData(key)).Returns((RadOrderDto)null!);

        // Act
        var result = _sut.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadByEmrOrderId_WhenFound_ReturnsAggregatedModel()
    {
        // Arrange
        var model = CreateTestModel();
        var dto = RadOrderDto.FromModel(model);
        var itemDto = RadOrderItemDto.FromModel(model.OrderId, model.Items.First());

        _orderDal.Setup(x => x.GetByEmrOrderId("EMR-RAD-001")).Returns(dto);
        _itemDal.Setup(x => x.ListData(It.IsAny<IRadOrderKey>())).Returns([itemDto]);

        // Act
        var result = _sut.LoadByEmrOrderId("EMR-RAD-001");

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.OrderId.Should().Be(model.OrderId);
                m.EmrOrderId.Should().Be("EMR-RAD-001");
                m.Items.Should().HaveCount(1);
            },
            onNone: () => Assert.Fail("Expected Some"));
    }

    [Fact]
    public void LoadByEmrOrderId_WhenNotFound_ReturnsNone()
    {
        // Arrange
        _orderDal.Setup(x => x.GetByEmrOrderId("NOT_FOUND")).Returns((RadOrderDto)null!);

        // Act
        var result = _sut.LoadByEmrOrderId("NOT_FOUND");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void ListByRegId_ReturnsAggregatedModels()
    {
        // Arrange
        var model = CreateTestModel();
        var dto = RadOrderDto.FromModel(model);
        var itemDto = RadOrderItemDto.FromModel(model.OrderId, model.Items.First());

        _orderDal.Setup(x => x.ListByRegId("REG001")).Returns([dto]);
        _itemDal.Setup(x => x.ListData(It.IsAny<IRadOrderKey>())).Returns([itemDto]);

        // Act
        var results = _sut.ListByRegId("REG001").ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].OrderId.Should().Be(model.OrderId);
        results[0].Patient.RegId.Should().Be("REG001");
        results[0].Items.Should().HaveCount(1);
    }

    [Fact]
    public void ToModel_MapsAllFieldsAndAuditTrailCorrectly()
    {
        // Arrange
        var dto = new RadOrderDto(
            OrderId: "RDO26110001",
            EmrOrderId: "EMR-RAD-009",
            OrderNo: "RAD26110009",
            OrderSource: 1, // External
            Modality: "CT",
            RadOrderStatus: 2, // Scheduled
            Urgency: 1, // Cito
            RegId: "REG999",
            PatientId: "MR999",
            PatientName: "Pasien Radiologi",
            BirthDate: new DateTime(1980, 5, 10),
            Gender: "P",
            AgeAtOrder: 46,
            DokterPengorderId: "DOK99",
            DokterPengorderName: "dr. Pengorder",
            LayananPengorderId: "LAY99",
            LayananPengorderName: "Poli Bedah",
            ClinicalNote: "Trauma kepala",
            CancelledReason: "Salah input",
            CancelledDate: new DateTime(2026, 11, 24, 10, 0, 0),
            CancelledUserId: "USER_CANCEL",
            CrtUser: "USER_CRT",
            CrtDate: new DateTime(2026, 11, 24, 8, 0, 0),
            UpdUser: "USER_UPD",
            UpdDate: new DateTime(2026, 11, 24, 9, 0, 0),
            VodUser: "USER_VOD",
            VodDate: new DateTime(2026, 11, 24, 11, 0, 0),
            BillingTindakanId: "TDK001",
            BillingLastError: "Koneksi timeout");

        var items = new List<RadOrderItemModel>
        {
            RadOrderItemModel.Create("TRF_CT", "CT Head Scan", RadModalityEnum.CT, RadUrgencyEnum.Cito)
        };

        // Act
        var model = dto.ToModel(items);

        // Assert
        model.OrderId.Should().Be("RDO26110001");
        model.EmrOrderId.Should().Be("EMR-RAD-009");
        model.OrderNo.Should().Be("RAD26110009");
        model.OrderSource.Should().Be(RadOrderSourceEnum.External);
        model.Modality.Should().Be(RadModalityEnum.CT);
        model.RadOrderStatus.Should().Be(RadOrderStatusEnum.Scheduled);
        model.Urgency.Should().Be(RadUrgencyEnum.Cito);
        model.Patient.RegId.Should().Be("REG999");
        model.Patient.PatientId.Should().Be("MR999");
        model.Patient.PatientName.Should().Be("Pasien Radiologi");
        model.Patient.BirthDate.Should().Be(new DateTime(1980, 5, 10));
        model.Patient.Gender.Should().Be("P");
        model.Patient.AgeAtOrder.Should().Be(46);
        model.DokterPengorderId.Should().Be("DOK99");
        model.DokterPengorderName.Should().Be("dr. Pengorder");
        model.LayananPengorderId.Should().Be("LAY99");
        model.LayananPengorderName.Should().Be("Poli Bedah");
        model.ClinicalNote.Should().Be("Trauma kepala");
        model.CancelledReason.Should().Be("Salah input");
        model.CancelledDate.Should().Be(new DateTime(2026, 11, 24, 10, 0, 0));
        model.CancelledUserId.Should().Be("USER_CANCEL");
        model.AuditTrail.Created.UserId.Should().Be("USER_CRT");
        model.AuditTrail.Created.Timestamp.Should().Be(new DateTime(2026, 11, 24, 8, 0, 0));
        model.AuditTrail.Modified.UserId.Should().Be("USER_UPD");
        model.AuditTrail.Modified.Timestamp.Should().Be(new DateTime(2026, 11, 24, 9, 0, 0));
        model.AuditTrail.Voided.UserId.Should().Be("USER_VOD");
        model.AuditTrail.Voided.Timestamp.Should().Be(new DateTime(2026, 11, 24, 11, 0, 0));
        model.AuditTrail.IsVoided.Should().BeTrue();
        model.BillingTindakanId.Should().Be("TDK001");
        model.BillingLastError.Should().Be("Koneksi timeout");
        model.Items.Should().HaveCount(1);
        model.Items[0].TarifId.Should().Be("TRF_CT");
        model.Items[0].Modality.Should().Be(RadModalityEnum.CT);
    }

    [Fact]
    public void FromModel_MapsBillingFieldsCorrectly()
    {
        // Arrange
        var model = CreateTestModel();
        model.MarkCharged("TDK002", "USER_CHG", new DateTime(2026, 11, 24, 10, 0, 0));

        // Act
        var dto = RadOrderDto.FromModel(model);

        // Assert
        dto.BillingTindakanId.Should().Be("TDK002");
        dto.BillingLastError.Should().BeEmpty();
        dto.RadOrderStatus.Should().Be((int)RadOrderStatusEnum.Charged);
    }

    [Fact]
    public void FromModel_WhenBillingError_MapsLastErrorCorrectly()
    {
        // Arrange
        var model = CreateTestModel();
        model.RecordBillingError("Tarif tidak ditemukan", "USER_ERR", new DateTime(2026, 11, 24, 10, 0, 0));

        // Act
        var dto = RadOrderDto.FromModel(model);

        // Assert
        dto.BillingTindakanId.Should().BeEmpty();
        dto.BillingLastError.Should().Be("Tarif tidak ditemukan");
    }
}
