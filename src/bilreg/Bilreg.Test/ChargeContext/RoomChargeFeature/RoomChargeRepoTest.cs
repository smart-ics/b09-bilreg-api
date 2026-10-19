using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.RoomChargeFeature;

public class RoomChargeRepoTest
{
    private readonly Mock<IRoomChargeDal> _dalMock = new();
    private readonly Mock<IRoomChargeKomponenDal> _komponenDalMock = new();
    private readonly Mock<IRoomChargeLegacyDal> _legacyDalMock = new();
    private readonly RoomChargeRepo _repository;

    public RoomChargeRepoTest()
    {
        _repository = new RoomChargeRepo(
            _dalMock.Object,
            _komponenDalMock.Object,
            _legacyDalMock.Object);
    }

    [Fact]
    public void UT_RP_01_GivenNewEntity_WhenSaveChanges_ThenInsertsModernAndProjectsLegacy()
    {
        // Arrange
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IRoomChargeKey>())).Returns((RoomChargeDto)null!);

        // Act
        _repository.SaveChanges(model);

        // Assert
        _dalMock.Verify(x => x.Insert(It.Is<RoomChargeDto>(d =>
            d.RoomChargeId == "RMC2610190001" &&
            d.PakaiBedId == "PB001" &&
            d.RegId == "RG001" &&
            d.PasienId == "PS001" &&
            d.BangsalId == "B01" &&
            d.KamarId == "K01" &&
            d.BedId == "BED01" &&
            d.ChargeCycleDate == "2026-10-19" &&
            d.PhysicalClassId == "KL3" &&
            d.PayerClassId == "KL2" &&
            d.BillingClassId == "KL2" &&
            d.IsTitipKelas == false &&
            d.IsNaikKelas == false &&
            d.DifferentialAmount == 0m &&
            d.IsRoomingIn == false &&
            d.ChargedUnits == 1.0m &&
            d.BaseTariff == 350000m &&
            d.TotalAmount == 350000m &&
            d.RoomChargeStatus == "Posted"
        )), Times.Once);

        _dalMock.Verify(x => x.Update(It.IsAny<RoomChargeDto>()), Times.Never);

        _komponenDalMock.Verify(x => x.DeleteByRoomChargeId("RMC2610190001"), Times.Once);
        _komponenDalMock.Verify(x => x.Insert(It.Is<IEnumerable<RoomChargeKomponenDto>>(list =>
            list.Count() == 3 &&
            list.Any(k => k.KomponenId == "K01" && k.Tarif == 200000m) &&
            list.Any(k => k.KomponenId == "K02" && k.Tarif == 100000m) &&
            list.Any(k => k.KomponenId == "K03" && k.Tarif == 50000m)
        )), Times.Once);

        _legacyDalMock.Verify(x => x.SaveChanges(model), Times.Once);
    }

    [Fact]
    public void UT_RP_02_GivenExistingEntity_WhenSaveChanges_ThenUpdatesModernAndProjectsLegacy()
    {
        // Arrange
        var model = CreateTestModel();
        var existingDto = CreateTestDto();
        _dalMock.Setup(x => x.GetData(It.IsAny<IRoomChargeKey>())).Returns(existingDto);

        // Act
        _repository.SaveChanges(model);

        // Assert
        _dalMock.Verify(x => x.Update(It.Is<RoomChargeDto>(d =>
            d.RoomChargeId == "RMC2610190001" &&
            d.PakaiBedId == "PB001" &&
            d.RegId == "RG001"
        )), Times.Once);

        _dalMock.Verify(x => x.Insert(It.IsAny<RoomChargeDto>()), Times.Never);
        _komponenDalMock.Verify(x => x.DeleteByRoomChargeId("RMC2610190001"), Times.Once);
        _komponenDalMock.Verify(x => x.Insert(It.IsAny<IEnumerable<RoomChargeKomponenDto>>()), Times.Once);
        _legacyDalMock.Verify(x => x.SaveChanges(model), Times.Once);
    }

    [Fact]
    public void UT_RP_03_GivenNewEntity_WhenSaveAlias_ThenDelegatesToSaveChanges()
    {
        // Arrange
        var model = CreateTestModel();
        _dalMock.Setup(x => x.GetData(It.IsAny<IRoomChargeKey>())).Returns((RoomChargeDto)null!);

        // Act
        _repository.Save(model);

        // Assert
        _dalMock.Verify(x => x.Insert(It.IsAny<RoomChargeDto>()), Times.Once);
        _legacyDalMock.Verify(x => x.SaveChanges(model), Times.Once);
    }

    [Fact]
    public void UT_RP_04_GivenExistingId_WhenLoadEntity_ThenFullAggregateReconstructed()
    {
        // Arrange
        var dto = CreateTestDto();
        var komponenList = CreateTestKomponenDtos("RMC2610190001");
        var key = RoomChargeModel.Key("RMC2610190001");

        _dalMock.Setup(x => x.GetData(key)).Returns(dto);
        _komponenDalMock.Setup(x => x.ListDataByRoomChargeId("RMC2610190001")).Returns(komponenList);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: model =>
            {
                model.RoomChargeId.Should().Be("RMC2610190001");
                model.PakaiBedId.Should().Be("PB001");
                model.RegId.Should().Be("RG001");
                model.PasienId.Should().Be("PS001");
                model.BangsalId.Should().Be("B01");
                model.KamarId.Should().Be("K01");
                model.BedId.Should().Be("BED01");
                model.ChargeCycleDate.Should().Be("2026-10-19");
                model.ChargeTriggerType.Should().Be(ChargeTriggerTypeEnum.DailyCutOff);
                model.PhysicalClassId.Should().Be("KL3");
                model.PayerClassId.Should().Be("KL2");
                model.BillingClassId.Should().Be("KL2");
                model.IsTitipKelas.Should().BeFalse();
                model.IsNaikKelas.Should().BeFalse();
                model.DifferentialAmount.Should().Be(0m);
                model.IsRoomingIn.Should().BeFalse();
                model.ChargedUnits.Should().Be(1.0m);
                model.BaseTariff.Should().Be(350000m);
                model.DiscountAmount.Should().Be(0m);
                model.TotalAmount.Should().Be(350000m);
                model.RoomChargeStatus.Should().Be(RoomChargeStatusEnum.Posted);
                model.ListKomponen.Should().HaveCount(3);
                model.ListKomponen[0].KomponenId.Should().Be("K01");
                model.ListKomponen[0].KomponenName.Should().Be("Sewa Kamar");
                model.ListKomponen[0].Tarif.Should().Be(200000m);
                model.AuditTrail.Created.UserId.Should().Be("admin");
            },
            onNone: () => throw new Exception("Expected some"));
    }

    [Fact]
    public void UT_RP_05_GivenStringId_WhenLoadEntity_ThenReconstructsCorrectly()
    {
        // Arrange
        var dto = CreateTestDto();
        var komponenList = CreateTestKomponenDtos("RMC2610190001");
        _dalMock.Setup(x => x.GetData(It.Is<IRoomChargeKey>(k => k.RoomChargeId == "RMC2610190001"))).Returns(dto);
        _komponenDalMock.Setup(x => x.ListDataByRoomChargeId("RMC2610190001")).Returns(komponenList);

        // Act
        var result = _repository.LoadEntity("RMC2610190001");

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m => m.RoomChargeId.Should().Be("RMC2610190001"),
            onNone: () => throw new Exception("Expected some"));
    }

    [Fact]
    public void UT_RP_06_GivenNonExistentId_WhenLoadEntity_ThenReturnsNone()
    {
        // Arrange
        var key = RoomChargeModel.Key("NOT_EXIST");
        _dalMock.Setup(x => x.GetData(key)).Returns((RoomChargeDto)null!);

        // Act
        var result = _repository.LoadEntity(key);

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void UT_RP_07_GivenPakaiBedAndDate_WhenGetByPakaiBedAndDate_ThenReturnsEntity()
    {
        // Arrange
        var dto = CreateTestDto();
        var komponenList = CreateTestKomponenDtos("RMC2610190001");
        _dalMock.Setup(x => x.GetByPakaiBedAndDate("PB001", "2026-10-19")).Returns(dto);
        _komponenDalMock.Setup(x => x.ListDataByRoomChargeId("RMC2610190001")).Returns(komponenList);

        // Act
        var result = _repository.GetByPakaiBedAndDate("PB001", "2026-10-19");

        // Assert
        result.HasValue.Should().BeTrue();
        result.Match(
            onSome: m =>
            {
                m.PakaiBedId.Should().Be("PB001");
                m.ChargeCycleDate.Should().Be("2026-10-19");
                m.ListKomponen.Should().HaveCount(3);
            },
            onNone: () => throw new Exception("Expected some"));
    }

    [Fact]
    public void UT_RP_08_GivenNonExistentBedAndDate_WhenGetByPakaiBedAndDate_ThenReturnsNone()
    {
        // Arrange
        _dalMock.Setup(x => x.GetByPakaiBedAndDate("PB999", "2026-10-19")).Returns((RoomChargeDto)null!);

        // Act
        var result = _repository.GetByPakaiBedAndDate("PB999", "2026-10-19");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void UT_RP_09_GivenRegId_WhenListDataByReg_ThenReturnsMappedEntities()
    {
        // Arrange
        var dtos = new List<RoomChargeDto>
        {
            CreateTestDto("RMC01", "2026-10-18"),
            CreateTestDto("RMC02", "2026-10-19")
        };
        _dalMock.Setup(x => x.ListDataByReg("RG001")).Returns(dtos);
        _komponenDalMock.Setup(x => x.ListDataByRoomChargeId(It.IsAny<string>()))
            .Returns((string id) => CreateTestKomponenDtos(id));

        // Act
        var result = _repository.ListDataByReg("RG001").ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].RoomChargeId.Should().Be("RMC01");
        result[0].ChargeCycleDate.Should().Be("2026-10-18");
        result[1].RoomChargeId.Should().Be("RMC02");
        result[1].ChargeCycleDate.Should().Be("2026-10-19");
    }

    [Fact]
    public void UT_RP_10_GivenWardAndDate_WhenListActiveByBangsalAndDate_ThenReturnsMappedEntities()
    {
        // Arrange
        var dtos = new List<RoomChargeDto>
        {
            CreateTestDto("RMC01", "2026-10-19"),
            CreateTestDto("RMC02", "2026-10-19")
        };
        _dalMock.Setup(x => x.ListActiveByBangsalAndDate("B01", "2026-10-19")).Returns(dtos);
        _komponenDalMock.Setup(x => x.ListDataByRoomChargeId(It.IsAny<string>()))
            .Returns((string id) => CreateTestKomponenDtos(id));

        // Act
        var result = _repository.ListActiveByBangsalAndDate("B01", "2026-10-19").ToList();

        // Assert
        result.Should().HaveCount(2);
        result.All(x => x.BangsalId == "B01" && x.ChargeCycleDate == "2026-10-19").Should().BeTrue();
    }

    [Fact]
    public void UT_RP_11_GivenReversedModel_WhenSaveChanges_ThenPassesReversalToDualWrite()
    {
        // Arrange
        var model = CreateTestModel();
        model.Reverse("supervisor", "Salah input kamar", new DateTime(2026, 10, 19, 14, 0, 0));

        _dalMock.Setup(x => x.GetData(It.IsAny<IRoomChargeKey>())).Returns(CreateTestDto());

        // Act
        _repository.SaveChanges(model);

        // Assert
        _dalMock.Verify(x => x.Update(It.Is<RoomChargeDto>(d =>
            d.RoomChargeStatus == "Reversed" &&
            d.ReversalReason == "Salah input kamar" &&
            d.UpdatedBy == "supervisor"
        )), Times.Once);

        _legacyDalMock.Verify(x => x.SaveChanges(It.Is<RoomChargeModel>(m =>
            m.RoomChargeStatus == RoomChargeStatusEnum.Reversed &&
            m.ReversalReason == "Salah input kamar"
        )), Times.Once);
    }

    [Fact]
    public void UT_RP_12_GivenRoomChargeDto_WhenRoundtripConversion_ThenAllPropertiesPreserved()
    {
        // Arrange
        var model = CreateTestModel();
        var dto = RoomChargeDto.FromModel(model);

        // Act
        var reconstructed = dto.ToModel(model.ListKomponen);

        // Assert
        reconstructed.RoomChargeId.Should().Be(model.RoomChargeId);
        reconstructed.PakaiBedId.Should().Be(model.PakaiBedId);
        reconstructed.RegId.Should().Be(model.RegId);
        reconstructed.PasienId.Should().Be(model.PasienId);
        reconstructed.BangsalId.Should().Be(model.BangsalId);
        reconstructed.KamarId.Should().Be(model.KamarId);
        reconstructed.BedId.Should().Be(model.BedId);
        reconstructed.ChargeCycleDate.Should().Be(model.ChargeCycleDate);
        reconstructed.TimeCharge.Should().Be(model.TimeCharge);
        reconstructed.ChargeTriggerType.Should().Be(model.ChargeTriggerType);
        reconstructed.PhysicalClassId.Should().Be(model.PhysicalClassId);
        reconstructed.PayerClassId.Should().Be(model.PayerClassId);
        reconstructed.BillingClassId.Should().Be(model.BillingClassId);
        reconstructed.IsTitipKelas.Should().Be(model.IsTitipKelas);
        reconstructed.IsNaikKelas.Should().Be(model.IsNaikKelas);
        reconstructed.DifferentialAmount.Should().Be(model.DifferentialAmount);
        reconstructed.IsRoomingIn.Should().Be(model.IsRoomingIn);
        reconstructed.MotherPakaiBedId.Should().Be(model.MotherPakaiBedId);
        reconstructed.ChargedUnits.Should().Be(model.ChargedUnits);
        reconstructed.BaseTariff.Should().Be(model.BaseTariff);
        reconstructed.DiscountAmount.Should().Be(model.DiscountAmount);
        reconstructed.TotalAmount.Should().Be(model.TotalAmount);
        reconstructed.RoomChargeStatus.Should().Be(model.RoomChargeStatus);
        reconstructed.ListKomponen.Should().HaveCount(3);
    }

    [Fact]
    public void UT_RP_13_GivenKomponenDto_WhenRoundtripConversion_ThenPropertiesPreserved()
    {
        // Arrange
        var komp = RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 200000m, 10000m, "1.1.1.01");
        var dto = RoomChargeKomponenDto.FromModel(komp, "RMC01");

        // Act
        var reconstructed = dto.ToModel();

        // Assert
        reconstructed.KomponenId.Should().Be("K01");
        reconstructed.KomponenName.Should().Be("Sewa Kamar");
        reconstructed.KomponenCategory.Should().Be("Akomodasi");
        reconstructed.Tarif.Should().Be(200000m);
        reconstructed.Diskon.Should().Be(10000m);
        reconstructed.Total.Should().Be(190000m);
        reconstructed.RekeningPendapatan.Should().Be("1.1.1.01");
    }

    #region TEST HELPERS

    private static RoomChargeModel CreateTestModel()
    {
        var komponen = new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", 200000m, 0, "1.1.01"),
            RoomChargeKomponenModel.Create("K02", "Askep Bangsal", "Keperawatan", 100000m, 0, "1.1.02"),
            RoomChargeKomponenModel.Create("K03", "Konsumsi", "Gizi", 50000m, 0, "1.1.03")
        };

        return RoomChargeModel.Create(
            pakaiBedId: "PB001",
            regId: "RG001",
            pasienId: "PS001",
            bangsalId: "B01",
            kamarId: "K01",
            bedId: "BED01",
            chargeCycleDate: "2026-10-19",
            timeCharge: new DateTime(2026, 10, 19, 12, 0, 0),
            chargeTriggerType: ChargeTriggerTypeEnum.DailyCutOff,
            physicalClassId: "KL3",
            payerClassId: "KL2",
            billingClassId: "KL2",
            isTitipKelas: false,
            isNaikKelas: false,
            differentialAmount: 0m,
            isRoomingIn: false,
            motherPakaiBedId: string.Empty,
            chargedUnits: 1.0m,
            baseTariff: 350000m,
            discountAmount: 0m,
            totalAmount: 350000m,
            listKomponen: komponen,
            userId: "admin",
            occurredAt: new DateTime(2026, 10, 19, 12, 0, 0),
            roomChargeId: "RMC2610190001");
    }

    private static RoomChargeDto CreateTestDto(string id = "RMC2610190001", string date = "2026-10-19")
    {
        return new RoomChargeDto(
            RoomChargeId: id,
            PakaiBedId: "PB001",
            RegId: "RG001",
            PasienId: "PS001",
            BangsalId: "B01",
            KamarId: "K01",
            BedId: "BED01",
            ChargeCycleDate: date,
            TimeCharge: new DateTime(2026, 10, 19, 12, 0, 0),
            ChargeTriggerType: "DailyCutOff",
            PhysicalClassId: "KL3",
            PayerClassId: "KL2",
            BillingClassId: "KL2",
            IsTitipKelas: false,
            IsNaikKelas: false,
            DifferentialAmount: 0m,
            IsRoomingIn: false,
            MotherPakaiBedId: string.Empty,
            ChargedUnits: 1.0m,
            BaseTariff: 350000m,
            DiscountAmount: 0m,
            TotalAmount: 350000m,
            RoomChargeStatus: "Posted",
            BillingItemRefId: string.Empty,
            ReversalOfRoomChargeId: string.Empty,
            ReversalReason: string.Empty,
            CreatedBy: "admin",
            CreatedAt: new DateTime(2026, 10, 19, 12, 0, 0),
            UpdatedBy: "admin",
            UpdatedAt: new DateTime(2026, 10, 19, 12, 0, 0));
    }

    private static List<RoomChargeKomponenDto> CreateTestKomponenDtos(string roomChargeId)
    {
        return new List<RoomChargeKomponenDto>
        {
            new(roomChargeId, "K01", "Sewa Kamar", "Akomodasi", 200000m, 0m, 200000m, "1.1.01"),
            new(roomChargeId, "K02", "Askep Bangsal", "Keperawatan", 100000m, 0m, 100000m, "1.1.02"),
            new(roomChargeId, "K03", "Konsumsi", "Gizi", 50000m, 0m, 50000m, "1.1.03")
        };
    }

    #endregion
}
