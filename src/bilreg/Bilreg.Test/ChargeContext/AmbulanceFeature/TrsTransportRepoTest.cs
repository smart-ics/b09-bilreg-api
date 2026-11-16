using Bilreg.Application.ChargeContext.AmbulanceFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class TrsTransportRepoTest
{
    private readonly Mock<ITrsTransportDal> _trsTransportDalMock;
    private readonly Mock<ITrsTransportItemDal> _trsTransportItemDalMock;
    private readonly TrsTransportRepo _sut;

    public TrsTransportRepoTest()
    {
        _trsTransportDalMock = new Mock<ITrsTransportDal>();
        _trsTransportItemDalMock = new Mock<ITrsTransportItemDal>();
        _sut = new TrsTransportRepo(_trsTransportDalMock.Object, _trsTransportItemDalMock.Object);
    }

    private static TrsTransportModel CreateSampleModel(string id = "TP26110001", string regId = "REG01")
    {
        var model = TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "LAY01",
            "USR01",
            trsTransportId: id,
            regId: regId,
            tujuanTransportId: "TJ01",
            konstanta: 1.5m,
            lokasiAsal: "IGD",
            lokasiTujuan: "RSUP Dr. Sardjito",
            jaminanId: "JAM01",
            keterangan: "Rujukan darurat");

        model.Dispatch("A01", "DRV01", "B 1234 CD", 100m, "PRW01", "DOK01", DateTime.Now);
        model.AddItem(new TrsTransportItemModel(id, "DT01", 150000m, 1m, "PRW01", 0m, "Jasa Medis"));
        return model;
    }

    [Fact]
    public void SaveChanges_WhenNew_InsertsHeaderAndItems()
    {
        // Arrange
        var model = CreateSampleModel();
        _trsTransportDalMock.Setup(x => x.GetData(It.IsAny<ITrsTransportKey>())).Returns((TrsTransportDto)null!);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _trsTransportDalMock.Verify(x => x.Insert(model), Times.Once);
        _trsTransportDalMock.Verify(x => x.Update(It.IsAny<TrsTransportModel>()), Times.Never);
        _trsTransportItemDalMock.Verify(x => x.Delete(model), Times.Once);
        _trsTransportItemDalMock.Verify(x => x.Insert(model.ListItems), Times.Once);
    }

    [Fact]
    public void SaveChanges_WhenExisting_UpdatesHeaderAndReplacesItems()
    {
        // Arrange
        var model = CreateSampleModel();
        var existingDto = new TrsTransportDto { fs_kd_trs = model.TrsTransportId };
        _trsTransportDalMock.Setup(x => x.GetData(It.IsAny<ITrsTransportKey>())).Returns(existingDto);

        // Act
        _sut.SaveChanges(model);

        // Assert
        _trsTransportDalMock.Verify(x => x.Update(model), Times.Once);
        _trsTransportDalMock.Verify(x => x.Insert(It.IsAny<TrsTransportModel>()), Times.Never);
        _trsTransportItemDalMock.Verify(x => x.Delete(model), Times.Once);
        _trsTransportItemDalMock.Verify(x => x.Insert(model.ListItems), Times.Once);
    }

    [Fact]
    public void LoadEntity_WhenNotFound_ReturnsNone()
    {
        // Arrange
        _trsTransportDalMock.Setup(x => x.GetData(It.IsAny<ITrsTransportKey>())).Returns((TrsTransportDto)null!);

        // Act
        var result = _sut.LoadEntity("TP99999999");

        // Assert
        result.HasValue.Should().BeFalse();
    }

    [Fact]
    public void LoadEntity_WhenFound_RehydratesModelWithItems()
    {
        // Arrange
        var dto = new TrsTransportDto
        {
            fs_kd_trs = "TP26110001",
            fs_kd_reg = "REG01",
            fs_kd_layanan = "LAY01",
            fs_status = "Dispatched",
            fs_tipe_perjalanan = "OutboundReferral",
            fs_metode_tarif = "Wilayah",
            fs_kd_transport = "A01",
            fs_kd_driver = "DRV01",
            fs_no_polisi = "B 1234 CD",
            fs_kd_tujuan_transport = "TJ01",
            fn_konstanta = 1.5m,
            fn_sub_total = 200000m,
            fn_total = 200000m,
            fn_odometer_awal = 100m,
            fd_tgl_trs = "2026-11-16",
            fs_jam_trs = "10:00:00",
            fd_tgl_jam_trs = "2026-11-16 10:00:00"
        };
        _trsTransportDalMock.Setup(x => x.GetData(It.IsAny<ITrsTransportKey>())).Returns(dto);

        var item = new TrsTransportItemModel("TP26110001", "DT01", 200000m, 1m, "PRW01", 0m, "Jasa Ambulans");
        _trsTransportItemDalMock.Setup(x => x.ListData(It.IsAny<ITrsTransportKey>())).Returns([item]);

        // Act
        var result = _sut.LoadEntity("TP26110001");

        // Assert
        result.HasValue.Should().BeTrue();
        var model = result.Value;
        model.TrsTransportId.Should().Be("TP26110001");
        model.Status.Should().Be(TransportStatusEnum.Dispatched);
        model.TipePerjalanan.Should().Be(TransportTypeEnum.OutboundReferral);
        model.MetodeTarif.Should().Be(ChargingMethodEnum.Wilayah);
        model.AmbulanceId.Should().Be("A01");
        model.DriverId.Should().Be("DRV01");
        model.ListItems.Should().HaveCount(1);
        model.ListItems.First().DetilTarifId.Should().Be("DT01");
    }

    [Fact]
    public void Delete_DeletesItemsAndHeader()
    {
        // Arrange
        var key = new TrsTransportKey("TP26110001");

        // Act
        _sut.Delete(key);

        // Assert
        _trsTransportItemDalMock.Verify(x => x.Delete(key), Times.Once);
        _trsTransportDalMock.Verify(x => x.Delete(key), Times.Once);
    }

    [Fact]
    public void ListDataByReg_ReturnsAllTransactionsWithItems()
    {
        // Arrange
        var dto1 = new TrsTransportDto { fs_kd_trs = "TP001", fs_kd_reg = "REG01", fs_status = "Completed" };
        var dto2 = new TrsTransportDto { fs_kd_trs = "TP002", fs_kd_reg = "REG01", fs_status = "Dispatched" };
        _trsTransportDalMock.Setup(x => x.ListDataByReg("REG01")).Returns([dto1, dto2]);
        _trsTransportItemDalMock.Setup(x => x.ListData(It.IsAny<ITrsTransportKey>())).Returns([]);

        // Act
        var result = _sut.ListDataByReg("REG01").ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].TrsTransportId.Should().Be("TP001");
        result[1].TrsTransportId.Should().Be("TP002");
        _trsTransportItemDalMock.Verify(x => x.ListData(It.IsAny<ITrsTransportKey>()), Times.Exactly(2));
    }

    [Fact]
    public void ListActiveDispatched_ReturnsDispatchedTransactionsWithItems()
    {
        // Arrange
        var dto = new TrsTransportDto { fs_kd_trs = "TP001", fs_status = "Dispatched" };
        _trsTransportDalMock.Setup(x => x.ListActiveDispatched()).Returns([dto]);
        _trsTransportItemDalMock.Setup(x => x.ListData(It.IsAny<ITrsTransportKey>())).Returns([]);

        // Act
        var result = _sut.ListActiveDispatched().ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Status.Should().Be(TransportStatusEnum.Dispatched);
    }

    [Fact]
    public void HasActiveDispatchForAmbulance_CallsDal_ReturnsTrueWhenBusy()
    {
        // Arrange
        _trsTransportDalMock.Setup(x => x.HasActiveDispatchForAmbulance("A01", null)).Returns(true);

        // Act
        var isBusy = _sut.HasActiveDispatchForAmbulance("A01");

        // Assert
        isBusy.Should().BeTrue();
    }

    [Fact]
    public void HasActiveDispatchForAmbulance_WhenEmpty_ReturnsFalseWithoutCallingDal()
    {
        // Act
        var isBusy = _sut.HasActiveDispatchForAmbulance("");

        // Assert
        isBusy.Should().BeFalse();
        _trsTransportDalMock.Verify(x => x.HasActiveDispatchForAmbulance(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void HasActiveDispatchForDriver_CallsDal_ReturnsTrueWhenBusy()
    {
        // Arrange
        _trsTransportDalMock.Setup(x => x.HasActiveDispatchForDriver("DRV01", "TP001")).Returns(true);

        // Act
        var isBusy = _sut.HasActiveDispatchForDriver("DRV01", "TP001");

        // Assert
        isBusy.Should().BeTrue();
    }

    [Fact]
    public void HasActiveDispatchForDriver_WhenEmpty_ReturnsFalseWithoutCallingDal()
    {
        // Act
        var isBusy = _sut.HasActiveDispatchForDriver("");

        // Assert
        isBusy.Should().BeFalse();
        _trsTransportDalMock.Verify(x => x.HasActiveDispatchForDriver(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void TrsTransportDto_ToModel_MapsAllFieldsAndInvariants()
    {
        // Arrange
        var dto = new TrsTransportDto
        {
            fs_kd_trs = "TP26110099",
            fs_kd_reg = "REG99",
            fs_kd_layanan = "LAY_IGD",
            fd_tgl_trs = "2026-11-16",
            fs_jam_trs = "14:30:00",
            fd_tgl_jam_trs = "2026-11-16 14:30:00",
            fs_kd_petugas = "USR_ADMIN",
            fs_status = "Completed",
            fs_tipe_perjalanan = "InboundRetrieval",
            fs_metode_tarif = "Jarak",
            fs_kd_transport = "A02",
            fs_kd_driver = "DRV02",
            fs_no_polisi = "AD 9999 XX",
            fs_kd_perawat = "PRW02",
            fs_kd_dokter = "DOK02",
            fs_lokasi_asal = "Puskesmas A",
            fs_lokasi_tujuan = "IGD",
            fd_tgl_request = "2026-11-16 14:00:00",
            fd_tgl_dispatch = "2026-11-16 14:10:00",
            fd_tgl_arrival = "2026-11-16 14:40:00",
            fd_tgl_return = "2026-11-16 15:15:00",
            fn_odometer_awal = 10500m,
            fn_odometer_akhir = 10525m,
            fs_kd_jaminan = "BPJS",
            fb_non_reg = false,
            fb_posted_billing = true,
            fn_sub_total = 350000m,
            fn_diskon = 50000m,
            fn_total = 300000m,
            fs_keterangan = "Pasien gawat darurat"
        };

        var items = new List<TrsTransportItemModel>
        {
            new("TP26110099", "DT01", 300000m, 1m, "PRW02", 50000m, "Ambulance Distance"),
            new("TP26110099", "DT02", 50000m, 1m, "DOK02", 0m, "Jasa Dokter Pendamping")
        };

        // Act
        var model = dto.ToModel(items);

        // Assert
        model.TrsTransportId.Should().Be("TP26110099");
        model.RegId.Should().Be("REG99");
        model.LayananId.Should().Be("LAY_IGD");
        model.Status.Should().Be(TransportStatusEnum.Completed);
        model.TipePerjalanan.Should().Be(TransportTypeEnum.InboundRetrieval);
        model.MetodeTarif.Should().Be(ChargingMethodEnum.Jarak);
        model.AmbulanceId.Should().Be("A02");
        model.DriverId.Should().Be("DRV02");
        model.NoPolisi.Should().Be("AD 9999 XX");
        model.PerawatId.Should().Be("PRW02");
        model.DokterId.Should().Be("DOK02");
        model.LokasiAsal.Should().Be("Puskesmas A");
        model.LokasiTujuan.Should().Be("IGD");
        model.StartOdometer.Should().Be(10500m);
        model.EndOdometer.Should().Be(10525m);
        model.TotalJarakKm.Should().Be(25m);
        model.JaminanId.Should().Be("BPJS");
        model.IsNonReg.Should().BeFalse();
        model.IsPostedBilling.Should().BeTrue();
        model.SubTotal.Should().Be(350000m);
        model.Diskon.Should().Be(50000m);
        model.Total.Should().Be(300000m);
        model.ListItems.Should().HaveCount(2);
    }
}
