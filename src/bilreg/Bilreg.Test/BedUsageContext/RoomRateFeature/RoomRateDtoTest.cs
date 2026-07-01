using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.BedUsageContext.RoomRateFeature;

namespace Bilreg.Test.BedUsageContext.RoomRateFeature;

using Xunit;
using FluentAssertions;

public class RoomRateDtoTests
{
    // ============================================================
    // UT1 — REGULER: Projection → DTO
    // ============================================================
    [Fact]
    public void UT1_GivenRegulerModel_WhenConvertedToDto_ThenDtoFieldsMatch()
    {
        // Given
        var tipe = new TipeKamarReff("TP1", "Tipe 1", true);
        var komponen = new RoomRateKomponenType(new KomponenReff("KM1", "Komponen 1"), 50000);
        var tipeDet = new RoomRateRegulerTipeType(tipe, new[] { komponen });

        var kamar = new KamarType("K001", "Kamar A", BangsalType.Default.ToReff(), KelasType.Default.ToReff());
        var model = new RoomRateRegulerType("K001", kamar.ToReff(), new[] { tipeDet });

        var kelas = new KelasReff("CLS1", "Kelas 1");

        // When
        var dtoList = RoomRateDto.FromModel(model, kelas).ToList();

        // Then
        dtoList.Should().HaveCount(1);
        dtoList[0].fs_kd_kamar.Should().Be("K001");
        dtoList[0].fs_kd_detil_tarif.Should().Be("KM1");
        dtoList[0].fs_kd_tipe_kamar.Should().Be("TP1");
        dtoList[0].fn_tarif.Should().Be(50000);
        dtoList[0].fs_kd_kelas.Should().Be("CLS1");
        dtoList[0].fn_harike.Should().Be(1);
    }

    // ============================================================
    // UT2 — DAILY: Projection → DTO
    // ============================================================
    [Fact]
    public void UT2_GivenDailyModel_WhenConvertedToDto_ThenHariKeIsCorrect()
    {
        // Given
        var tipe = new TipeKamarReff("TP1", "Tipe 1", true);
        var komponen = new RoomRateKomponenType(new KomponenReff("KM1", "Komponen 1"), 70000);
        var hdr = new RoomRateDayType(tipe, 2, [komponen]);

        var kamar = new KamarType("K002", "Kamar B", BangsalType.Default.ToReff(), KelasType.Default.ToReff());
        var model = new RoomRateDailyType("K002", kamar.ToReff(), [hdr]);

        var kelas = new KelasReff("CLS2", "Kelas 2");

        // When
        var dtoList = RoomRateDto.FromModel(model, kelas).ToList();

        // Then
        dtoList.Should().HaveCount(1);
        dtoList[0].fn_harike.Should().Be(2);
        dtoList[0].fn_tarif.Should().Be(70000);
    }

    // ============================================================
    // UT3 — FLOATING: Projection → DTO
    // ============================================================
    [Fact]
    public void UT3_GivenFloatingModel_WhenConvertedToDto_ThenKelasIdIsTakenFromHeader()
    {
        // Given
        var tipe = new TipeKamarReff("TP9", "VIP", true);
        var kelas = new KelasReff("VX", "VIP X");
        var komponen = new RoomRateKomponenType(new KomponenReff("KM9", "Admin"), 300000);

        var hdr = new RoomRateKelasType(tipe, kelas, new[] { komponen });

        var kamar = new KamarType("KA10", "VIP Room", BangsalType.Default.ToReff(), KelasType.Default.ToReff());
        var model = new RoomRateFloatingType("KA10", kamar.ToReff(), new[] { hdr });

        // When
        var dtoList = RoomRateDto.FromModel(model).ToList();

        // Then
        dtoList.Should().HaveCount(1);
        dtoList[0].fs_kd_kelas.Should().Be("VX");
        dtoList[0].fn_tarif.Should().Be(300000);
    }


    // ============================================================
    // UT4 — ToModel: Detects FLOATING from distinct kelas
    // ============================================================
    [Fact]
    public void UT4_GivenFloatingDto_WhenParsedToModel_ThenModelIsRoomRateFloatingType()
    {
        // Given — 2 different kelas → FLOATING
        var dtoList = new[]
        {
            new RoomRateDto("K01","C01","TP1",100m,1,"A",1,"Kamar A","Comp","TP1","Kelas A"),
            new RoomRateDto("K01","C02","TP1",200m,2,"B",1,"Kamar A","Comp2","TP1","Kelas B")
        };

        // When
        var model = RoomRateDto.ToModel(dtoList);

        // Then
        model.Should().BeOfType<RoomRateFloatingType>();
        model.ListTipe.Should().HaveCount(2);
    }


    // ============================================================
    // UT5 — ToModel: Detects DAILY from distinct hariKe
    // ============================================================
    [Fact]
    public void UT5_GivenDailyDto_WhenParsedToModel_ThenModelIsRoomRateDailyType()
    {
        // Given — same kelas, but different Harike → DAILY
        var dtoList = new[]
        {
            new RoomRateDto("K01","C01","TP1",100m,1,"CLS",1,"Kamar A","Comp","TP1","Kelas"),
            new RoomRateDto("K01","C02","TP1",200m,2,"CLS",2,"Kamar A","Comp2","TP1","Kelas")
        };

        // When
        var model = RoomRateDto.ToModel(dtoList);

        // Then
        model.Should().BeOfType<RoomRateDailyType>();
        model.ListTipe.Should().HaveCount(2);
    }


    // ============================================================
    // UT6 — ToModel: Detects REGULER when kelas & harike are identical
    // ============================================================
    [Fact]
    public void UT6_GivenRegulerDto_WhenParsedToModel_ThenModelIsRoomRateRegulerType()
    {
        // Given — same kelas, same Harike → REGULER
        var dtoList = new[]
        {
            new RoomRateDto("K01","C01","TP1",100m,1,"CLS",1,"KA","Comp","TP1","CLS"),
            new RoomRateDto("K01","C02","TP1",150m,2,"CLS",1,"KA","Comp2","TP1","CLS")
        };

        // When
        var model = RoomRateDto.ToModel(dtoList);
        
        // Then
        model.Should().BeOfType<RoomRateRegulerType>();
        model.ListTipe.Should().HaveCount(1); // Both grouped by tipe kamar
        if (model is RoomRateRegulerType castedModel)
            castedModel.ListTipe.First().ListKomponen.Should().HaveCount(2);
        else
            Assert.False(true);
    }
}
