using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class OrderOpRepoTest
{
    private readonly Mock<IOrderOpDal> _orderOpDalMock;
    private readonly Mock<IOrderOpResourceDal> _orderOpResourceDalMock;
    private readonly OrderOpRepo _sut;

    public OrderOpRepoTest()
    {
        _orderOpDalMock = new Mock<IOrderOpDal>();
        _orderOpResourceDalMock = new Mock<IOrderOpResourceDal>();
        _sut = new OrderOpRepo(_orderOpDalMock.Object, _orderOpResourceDalMock.Object);
    }

    [Fact]
    public void UT1_GivenExistingEntity_WhenSaveChanges_ThenUpdateIsCalledAndResourcesReplaced()
    {
        // Arrange
        var model = CreateTestModel();
        model.AddResource(OrderOpResourceTypeEnum.Darah, "PRC", 2, "Kolf", "Golongan O+");
        model.AddResource(OrderOpResourceTypeEnum.Implan, "Plate 3.5mm", 1, "Pcs", "Orthopedic plate");

        _orderOpDalMock
            .Setup(x => x.GetData(It.IsAny<IOrderOpKey>()))
            .Returns(CreateTestDto(model.OrderOpId));

        // Act
        _sut.SaveChanges(model);

        // Assert
        _orderOpDalMock.Verify(x => x.Update(It.Is<OrderOpDto>(d => d.OrderOpId == model.OrderOpId)), Times.Once);
        _orderOpDalMock.Verify(x => x.Insert(It.IsAny<OrderOpDto>()), Times.Never);
        _orderOpResourceDalMock.Verify(x => x.DeleteByOrderOpId(model.OrderOpId), Times.Once);
        _orderOpResourceDalMock.Verify(x => x.Insert(It.IsAny<OrderOpResourceDto>()), Times.Exactly(2));
    }

    [Fact]
    public void UT2_GivenNewEntity_WhenSaveChanges_ThenInsertIsCalledAndResourcesInserted()
    {
        // Arrange
        var model = CreateTestModel();
        model.AddResource(OrderOpResourceTypeEnum.AlatKhusus, "C-Arm", 1, "Unit", "Fluoroscopy");

        _orderOpDalMock
            .Setup(x => x.GetData(It.IsAny<IOrderOpKey>()))
            .Returns((OrderOpDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _orderOpDalMock.Verify(x => x.Insert(It.Is<OrderOpDto>(d => d.OrderOpId == model.OrderOpId)), Times.Once);
        _orderOpDalMock.Verify(x => x.Update(It.IsAny<OrderOpDto>()), Times.Never);
        _orderOpResourceDalMock.Verify(x => x.DeleteByOrderOpId(model.OrderOpId), Times.Once);
        _orderOpResourceDalMock.Verify(x => x.Insert(It.Is<OrderOpResourceDto>(r => r.ItemName == "C-Arm")), Times.Once);
    }

    [Fact]
    public void UT3_GivenExistingEntityWithResources_WhenLoadEntity_ThenFullAggregateWithResourcesReturned()
    {
        // Arrange
        const string orderOpId = "ORD001";
        var key = OrderOpModel.Key(orderOpId);
        var headerDto = CreateTestDto(orderOpId);
        var resourceDtos = new List<OrderOpResourceDto>
        {
            new("RES01", orderOpId, (int)OrderOpResourceTypeEnum.Darah, "PRC", 2, "Kolf", "WB O+"),
            new("RES02", orderOpId, (int)OrderOpResourceTypeEnum.Implan, "Screw 2.7", 4, "Pcs", "Titanium")
        };

        _orderOpDalMock
            .Setup(x => x.GetData(key))
            .Returns(headerDto);
        _orderOpResourceDalMock
            .Setup(x => x.ListByOrderOpId(orderOpId))
            .Returns(resourceDtos);

        // Act
        var actual = _sut.LoadEntity(key);

        // Assert
        actual.HasValue.Should().BeTrue();
        actual.Match(
            onSome: m =>
            {
                m.OrderOpId.Should().Be(orderOpId);
                m.NamaOperasi.Should().Be(headerDto.NamaOperasi);
                m.DokterPengorder.PpaId.Should().Be(headerDto.DokterPengorderId);
                m.LayananPengorder.LayananId.Should().Be(headerDto.LayananPengorderId);
                m.DokterOperator.PpaId.Should().Be(headerDto.DokterOperatorId);
                m.ProsedurTambahan.Should().Be(headerDto.ProsedurTambahan);
                m.KlasifikasiOperasi.Should().Be((KlasifikasiOperasiEnum)headerDto.KlasifikasiOperasi);
                m.AnestesiPlan.Should().Be((AnestesiPlanEnum)headerDto.AnestesiPlan);
                m.PostOpCareUnit.Should().Be((PostOpCareUnitEnum)headerDto.PostOpCareUnit);
                m.AlasanBatal.Should().Be(headerDto.AlasanBatal);

                m.ListResource.Should().HaveCount(2);
                var resList = m.ListResource.ToList();
                resList[0].OrderOpResourceId.Should().Be("RES01");
                resList[0].ResourceType.Should().Be(OrderOpResourceTypeEnum.Darah);
                resList[0].ItemName.Should().Be("PRC");
                resList[0].Jumlah.Should().Be(2);
                resList[0].Satuan.Should().Be("Kolf");

                resList[1].OrderOpResourceId.Should().Be("RES02");
                resList[1].ResourceType.Should().Be(OrderOpResourceTypeEnum.Implan);
                resList[1].ItemName.Should().Be("Screw 2.7");
                resList[1].Jumlah.Should().Be(4);
                resList[1].Satuan.Should().Be("Pcs");
            },
            onNone: () => Assert.Fail("Expected Some, got None"));
    }

    [Fact]
    public void UT4_GivenNonExistingEntity_WhenLoadEntity_ThenNoneIsReturned()
    {
        // Arrange
        var key = OrderOpModel.Key("NONEXISTENT");
        _orderOpDalMock
            .Setup(x => x.GetData(key))
            .Returns((OrderOpDto)null!);

        // Act
        var actual = _sut.LoadEntity(key);

        // Assert
        actual.HasValue.Should().BeFalse();
        _orderOpResourceDalMock.Verify(x => x.ListByOrderOpId(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UT5_GivenKey_WhenDeleteEntity_ThenDeleteChildFirstThenHeader()
    {
        // Arrange
        const string orderOpId = "ORD001";
        var key = OrderOpModel.Key(orderOpId);

        var seq = new MockSequence();
        _orderOpResourceDalMock.InSequence(seq).Setup(x => x.DeleteByOrderOpId(orderOpId));
        _orderOpDalMock.InSequence(seq).Setup(x => x.Delete(key));

        // Act
        _sut.DeleteEntity(key);

        // Assert
        _orderOpResourceDalMock.Verify(x => x.DeleteByOrderOpId(orderOpId), Times.Once);
        _orderOpDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void UT6_GivenPeriodeFilter_WhenListData_ThenCorrectOrderOpViewsReturned()
    {
        // Arrange
        var filter = new Periode(new DateTime(2025, 1, 1), new DateTime(2025, 1, 31));
        var dtos = new List<OrderOpDto>
        {
            CreateTestDto("ORD01"),
            CreateTestDto("ORD02")
        };
        _orderOpDalMock
            .Setup(x => x.ListData(filter))
            .Returns(dtos);

        // Act
        var actual = _sut.ListData(filter).ToList();

        // Assert
        actual.Should().HaveCount(2);
        actual[0].OrderOpId.Should().Be("ORD01");
        actual[1].OrderOpId.Should().Be("ORD02");
        _orderOpDalMock.Verify(x => x.ListData(filter), Times.Once);
    }

    private static OrderOpModel CreateTestModel()
    {
        var model = OrderOpModel.CreateByReg(
            RegModel.Default,
            new LayananReff("LAY01", "Poli Bedah"),
            new PpaReff("DOK01", "dr. Pengorder"),
            "user1",
            new DateTime(2025, 2, 1));

        model.SetProsedur("Apendektomi", "Drainase", KlasifikasiOperasiEnum.Sedang);
        model.SetRencanaAnestesiDanPerawatan(AnestesiPlanEnum.RegionalSpinal, PostOpCareUnitEnum.PACU);
        model.SetDokterOperator(new PpaReff("DOK02", "dr. Operator, Sp.B"));
        return model;
    }

    private static OrderOpDto CreateTestDto(string orderOpId)
        => new(
            OrderOpId: orderOpId,
            OrderDate: new DateTime(2025, 2, 1),
            RegId: "REG01",
            PasienId: "PAS01",
            Icd10Id: "K35.8",
            JenisOperasiId: "01",
            NamaOperasi: "Apendektomi Laparoskopi",
            DokterId: "DOK02",
            UrgencyLevel: (int)UrgencyLevelEnum.Cito,
            TarifId: "TRF01",
            TarifName: "Tarif Bedah",
            EstimasiDurasi: 90,
            PreferedDate: new DateTime(2025, 2, 2, 8, 0, 0),
            SpecialEquipment: "Laparoskopi Set",
            DokterPengorderId: "DOK01",
            LayananPengorderId: "LAY01",
            DokterOperatorId: "DOK02",
            ProsedurTambahan: "Adhesiolisis",
            KlasifikasiOperasi: (int)KlasifikasiOperasiEnum.Sedang,
            AnestesiPlan: (int)AnestesiPlanEnum.General,
            PostOpCareUnit: (int)PostOpCareUnitEnum.ICU,
            AlasanBatal: "",
            CrtUser: "admin",
            CrtDate: new DateTime(2025, 2, 1),
            UpdUser: "admin",
            UpdDate: new DateTime(2025, 2, 1),
            VodUser: "",
            VodDate: new DateTime(3000, 1, 1),
            PasienName: "Budi",
            TglLahir: "1990-05-15",
            Gender: "L",
            fs_ket_icd: "Appendicitis",
            fs_nm_jenis_operasi: "Bedah Umum",
            fs_nm_peg: "dr. Operator, Sp.B",
            DokterPengorderName: "dr. Pengorder",
            LayananPengorderName: "Poli Bedah",
            DokterOperatorName: "dr. Operator, Sp.B"
        );
}
