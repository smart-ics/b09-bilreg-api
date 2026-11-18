using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class TrsTransportModelTest
{
    private static TrsTransportModel CreateDefaultWilayah(string? id = null)
    {
        return TrsTransportModel.Create(
            tipePerjalanan: TransportTypeEnum.OutboundReferral,
            metodeTarif: ChargingMethodEnum.Wilayah,
            layananId: "IGD",
            petugasId: "USER01",
            trsTransportId: id,
            regId: "RG2026111601",
            tujuanTransportId: "TJ1",
            konstanta: 1.5m,
            lokasiAsal: "RS",
            lokasiTujuan: "RS Rujukan"
        );
    }

    private static TrsTransportModel CreateDefaultJarak(string? id = null)
    {
        return TrsTransportModel.Create(
            tipePerjalanan: TransportTypeEnum.InboundRetrieval,
            metodeTarif: ChargingMethodEnum.Jarak,
            layananId: "IGD",
            petugasId: "USER01",
            trsTransportId: id,
            regId: "RG2026111602",
            lokasiAsal: "TKP",
            lokasiTujuan: "RS"
        );
    }

    [Fact]
    public void Create_WithoutExplicitId_GeneratesLegacyCompactTpId()
    {
        // Act
        var model = CreateDefaultWilayah();

        // Assert
        model.TrsTransportId.Should().NotBeNullOrWhiteSpace();
        model.TrsTransportId.Should().StartWith(TrsTransportModel.ID_PREFIX);
        model.TrsTransportId.Length.Should().Be(10);
        model.Status.Should().Be(TransportStatusEnum.Requested);
        model.IsNonReg.Should().BeFalse();
        model.MetodeTarif.Should().Be(ChargingMethodEnum.Wilayah);
        model.TujuanTransportId.Should().Be("TJ1");
        model.Konstanta.Should().Be(1.5m);
    }

    [Fact]
    public void Create_WithExplicitId_PreservesId()
    {
        // Act
        var model = CreateDefaultWilayah("TP26111699");

        // Assert
        model.TrsTransportId.Should().Be("TP26111699");
    }

    [Fact]
    public void Create_WilayahWithoutTujuan_ThrowsArgumentException()
    {
        // Act
        var act = () => TrsTransportModel.Create(
            TransportTypeEnum.OutboundReferral,
            ChargingMethodEnum.Wilayah,
            "IGD",
            "USER01",
            tujuanTransportId: ""
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TujuanTransportId*");
    }

    [Fact]
    public void Create_Jarak_ClearsTujuanTransportIdAndKonstanta()
    {
        // Act
        var model = CreateDefaultJarak();

        // Assert
        model.MetodeTarif.Should().Be(ChargingMethodEnum.Jarak);
        model.TujuanTransportId.Should().BeEmpty();
        model.Konstanta.Should().Be(0m);
    }

    [Fact]
    public void CoreInvariant6_SwitchingTariffMethods_EnforcesMutuallyExclusiveRules()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        model.TujuanTransportId.Should().Be("TJ1");

        // Act - switch to Jarak
        model.SetMetodeJarak();

        // Assert
        model.MetodeTarif.Should().Be(ChargingMethodEnum.Jarak);
        model.TujuanTransportId.Should().BeEmpty();
        model.Konstanta.Should().Be(0m);

        // Act - switch back to Wilayah
        model.SetMetodeWilayah("TJ2", 2.0m);

        // Assert
        model.MetodeTarif.Should().Be(ChargingMethodEnum.Wilayah);
        model.TujuanTransportId.Should().Be("TJ2");
        model.Konstanta.Should().Be(2.0m);
    }

    [Fact]
    public void CoreInvariant6_SetMetodeWilayah_WithoutTujuan_ThrowsArgumentException()
    {
        // Arrange
        var model = CreateDefaultJarak();

        // Act
        var act = () => model.SetMetodeWilayah("", 1.0m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TujuanTransportId*");
    }

    [Fact]
    public void Lifecycle_RequestedToDispatched_Success()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        var dispatchTime = new DateTime(2026, 11, 16, 10, 0, 0);

        // Act
        model.Dispatch(
            ambulanceId: "AMB01",
            driverId: "DRV01",
            noPolisi: "B 1234 CD",
            startOdometer: 10500m,
            perawatId: "NUR01",
            dokterId: "DOC01",
            dispatchTime: dispatchTime
        );

        // Assert
        model.Status.Should().Be(TransportStatusEnum.Dispatched);
        model.AmbulanceId.Should().Be("AMB01");
        model.DriverId.Should().Be("DRV01");
        model.NoPolisi.Should().Be("B 1234 CD");
        model.StartOdometer.Should().Be(10500m);
        model.PerawatId.Should().Be("NUR01");
        model.DokterId.Should().Be("DOC01");
        model.TglDispatch.Should().Be(dispatchTime);
    }

    [Fact]
    public void Lifecycle_Dispatch_InvalidStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);

        // Act: attempt second dispatch from Dispatched status
        var act = () => model.Dispatch("AMB02", "DRV02", "B 5678 EF", 1000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Requested*");
    }

    [Fact]
    public void Lifecycle_Dispatch_EmptyAmbulanceOrDriver_ThrowsArgumentException()
    {
        // Arrange
        var model1 = CreateDefaultWilayah();
        var model2 = CreateDefaultWilayah();

        // Act & Assert
        var act1 = () => model1.Dispatch("", "DRV01", "B 1234 CD", 1000m);
        act1.Should().Throw<ArgumentException>().WithMessage("*Ambulance ID*");

        var act2 = () => model2.Dispatch("AMB01", "", "B 1234 CD", 1000m);
        act2.Should().Throw<ArgumentException>().WithMessage("*Driver ID*");
    }

    [Fact]
    public void Lifecycle_RecordArrival_OnlyValidInDispatchedStatus()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        var arrivalTime = new DateTime(2026, 11, 16, 10, 30, 0);

        // Act on Requested status
        var actRequested = () => model.RecordArrival(arrivalTime);
        actRequested.Should().Throw<InvalidOperationException>();

        // Dispatch then record arrival
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);
        model.RecordArrival(arrivalTime);

        // Assert
        model.TglArrival.Should().Be(arrivalTime);
    }

    [Fact]
    public void Lifecycle_DispatchedToCompleted_Success()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);
        var returnTime = new DateTime(2026, 11, 16, 11, 30, 0);

        // Act
        model.Complete(endOdometer: 1045m, returnTime: returnTime);

        // Assert
        model.Status.Should().Be(TransportStatusEnum.Completed);
        model.EndOdometer.Should().Be(1045m);
        model.TotalJarakKm.Should().Be(45m);
        model.TglReturn.Should().Be(returnTime);
    }

    [Fact]
    public void Lifecycle_Complete_EndOdometerLessThanStartOdometer_ThrowsArgumentException()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);

        // Act: end odometer is 990 (less than 1000)
        var act = () => model.Complete(endOdometer: 990m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh lebih kecil*");
    }

    [Fact]
    public void Lifecycle_Complete_FromRequestedStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateDefaultWilayah();

        // Act
        var act = () => model.Complete(endOdometer: 1050m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Dispatched*");
    }

    [Fact]
    public void Lifecycle_CancelFromRequested_Success()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        var cancelTime = new DateTime(2026, 11, 16, 10, 15, 0);

        // Act
        model.Cancel("Permintaan dibatalkan oleh pemohon", biayaBatal: 0m, petugasVoidId: "USER02", cancelTime: cancelTime);

        // Assert
        model.Status.Should().Be(TransportStatusEnum.Cancelled);
        model.IsVoid.Should().BeTrue();
        model.AlasanBatal.Should().Be("Permintaan dibatalkan oleh pemohon");
        model.BiayaBatal.Should().Be(0m);
        model.PetugasVoidId.Should().Be("USER02");
        model.TglVoid.Should().Be(cancelTime);
    }

    [Fact]
    public void Lifecycle_CancelFromDispatched_WithStandbyFee_Success()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);

        // Act
        model.Cancel("Dibatalkan di jalan karena keluarga menolak", biayaBatal: 75000m, petugasVoidId: "USER01");

        // Assert
        model.Status.Should().Be(TransportStatusEnum.Cancelled);
        model.IsVoid.Should().BeTrue();
        model.BiayaBatal.Should().Be(75000m);
    }

    [Fact]
    public void Lifecycle_CancelFromCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);
        model.Complete(1050m);

        // Act
        var act = () => model.Cancel("Mencoba batal setelah selesai");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Completed*");
    }

    [Fact]
    public void Lifecycle_Cancel_EmptyReason_ThrowsArgumentException()
    {
        // Arrange
        var model = CreateDefaultWilayah();

        // Act
        var act = () => model.Cancel("");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan pembatalan*");
    }

    [Fact]
    public void LinkRegistration_UpdatesRegIdAndClearsNonRegFlag()
    {
        // Arrange
        var model = TrsTransportModel.Create(
            TransportTypeEnum.InboundRetrieval,
            ChargingMethodEnum.Jarak,
            "IGD",
            "USER01",
            regId: ""
        );
        model.IsNonReg.Should().BeTrue();
        model.RegId.Should().BeEmpty();

        // Act
        model.LinkRegistration("RG2026111699");

        // Assert
        model.RegId.Should().Be("RG2026111699");
        model.IsNonReg.Should().BeFalse();
    }

    [Fact]
    public void SetNonRegistration_SetsNonRegAndDemographics()
    {
        // Arrange
        var model = CreateDefaultWilayah();

        // Act
        model.SetNonRegistration("Tn. Fulan", "Jl. Merdeka No. 1", "RT 01", "Surabaya", "L");

        // Assert
        model.IsNonReg.Should().BeTrue();
        model.RegId.Should().BeEmpty();
        model.PasienName.Should().Be("Tn. Fulan");
        model.Alm1Pasien.Should().Be("Jl. Merdeka No. 1");
        model.Alm2Pasien.Should().Be("RT 01");
        model.KotaPasien.Should().Be("Surabaya");
        model.Sex.Should().Be("L");
    }

    [Fact]
    public void SetPostedBilling_OnlyAllowedWhenCompleted()
    {
        // Arrange
        var model = CreateDefaultWilayah();

        // Act on Requested
        var actRequested = () => model.SetPostedBilling();
        actRequested.Should().Throw<InvalidOperationException>();

        // Dispatch & Complete
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);
        model.Complete(1050m);

        // Act on Completed
        model.SetPostedBilling();
        model.IsPostedBilling.Should().BeTrue();
    }

    [Fact]
    public void ItemManagement_AddRemoveAndRecalculateTotals_Success()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        var item1 = new TrsTransportItemModel("", "D01", tarif: 200000m, qty: 1m, diskon: 0m);
        var item2 = new TrsTransportItemModel("", "D02", tarif: 50000m, qty: 2m, diskon: 10000m);

        // Act
        model.AddItem(item1);
        model.AddItem(item2);

        // Assert
        model.ListItems.Should().HaveCount(2);
        item1.TrsTransportId.Should().Be(model.TrsTransportId);
        item2.TrsTransportId.Should().Be(model.TrsTransportId);

        // SubTotal item1 = 200.000, item2 = (50.000 * 2) - 10.000 = 90.000
        // Header SubTotal = (200.000*1) + (50.000*2) = 300.000
        // Header Diskon = 10.000
        // Header Total = 300.000 - 10.000 = 290.000
        model.SubTotal.Should().Be(300000m);
        model.Diskon.Should().Be(10000m);
        model.Total.Should().Be(290000m);

        // Void item2
        model.VoidItem("D02");
        model.SubTotal.Should().Be(200000m);
        model.Diskon.Should().Be(0m);
        model.Total.Should().Be(200000m);

        // Remove item1
        model.RemoveItem("D01");
        model.SubTotal.Should().Be(0m);
        model.Total.Should().Be(0m);
    }

    [Fact]
    public void ItemManagement_MutatingOnTerminalState_ThrowsInvalidOperationException()
    {
        // Arrange
        var model = CreateDefaultWilayah();
        model.Dispatch("AMB01", "DRV01", "B 1234 CD", 1000m);
        model.Complete(1020m);

        var item = new TrsTransportItemModel("", "D01", 100000m);

        // Act & Assert
        var actAdd = () => model.AddItem(item);
        actAdd.Should().Throw<InvalidOperationException>().WithMessage("*terminal*");

        var actRemove = () => model.RemoveItem("D01");
        actRemove.Should().Throw<InvalidOperationException>().WithMessage("*terminal*");
    }

    [Fact]
    public void Rehydrate_RestoresFullAggregateState()
    {
        // Arrange
        var items = new List<TrsTransportItemModel>
        {
            new("TP26111601", "D01", 150000m, 1m)
        };

        // Act
        var model = TrsTransportModel.Rehydrate(
            trsTransportId: "TP26111601",
            regId: "RG2026111601",
            layananId: "IGD",
            tglTrs: new DateTime(2026, 11, 16, 8, 0, 0),
            petugasId: "USER01",
            isVoid: false,
            tglVoid: default,
            petugasVoidId: "",
            ambulanceId: "AMB01",
            driverId: "DRV01",
            noPolisi: "B 1234 CD",
            tujuanTransportId: "TJ1",
            konstanta: 1.5m,
            subTotal: 150000m,
            diskon: 0m,
            total: 150000m,
            kas: 0m,
            rekKasId: "",
            bank: 0m,
            rekBankId: "",
            kartuKredit: 0m,
            rekKartuKreditId: "",
            noKartuKredit: "",
            noBank: "",
            jenisKartuKreditId: "",
            jenisBankId: "",
            sisa: 0m,
            rekSisaId: "",
            petugasKasirId: "",
            kelasId: "KL1",
            trsChargeId: "",
            trsDepositId: "",
            trsKwitansiId: "",
            genKeu: 0m,
            tipeJaminanId: "JM1",
            pasienName: "Pasien Test",
            alm1Pasien: "Alamat Test",
            alm2Pasien: "",
            kotaPasien: "Jakarta",
            sex: "L",
            keterangan: "Ket Test",
            status: TransportStatusEnum.Completed,
            tipePerjalanan: TransportTypeEnum.OutboundReferral,
            metodeTarif: ChargingMethodEnum.Wilayah,
            perawatId: "NUR01",
            dokterId: "DOC01",
            lokasiAsal: "RS",
            lokasiTujuan: "RS2",
            tglRequest: new DateTime(2026, 11, 16, 8, 0, 0),
            tglDispatch: new DateTime(2026, 11, 16, 8, 15, 0),
            tglArrival: new DateTime(2026, 11, 16, 8, 45, 0),
            tglReturn: new DateTime(2026, 11, 16, 9, 30, 0),
            startOdometer: 5000m,
            endOdometer: 5025m,
            jaminanId: "JM1",
            isNonReg: false,
            isPostedBilling: true,
            alasanBatal: "",
            biayaBatal: 0m,
            listItems: items
        );

        // Assert
        model.TrsTransportId.Should().Be("TP26111601");
        model.Status.Should().Be(TransportStatusEnum.Completed);
        model.TotalJarakKm.Should().Be(25m);
        model.ListItems.Should().HaveCount(1);
        model.IsPostedBilling.Should().BeTrue();
    }
}
