using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RegFeature.UseCases;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegGetQueryTest
{
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepo = new();
    private readonly Mock<IAntrianRepo> _queRepo = new();
    private readonly Mock<ITglJamProvider> _tglJamProvider = new();

    private readonly RegJalanGethandler _sut;

    public RegGetQueryTest()
    {
        _sut = new RegJalanGethandler(
            _regRepo.Object,
            _jaminanRepo.Object,
            _tipeJaminanRepo.Object,
            _queRepo.Object,
            _tglJamProvider.Object);
    }

    [Fact]
    public async Task Handle_WhenPhysicianQueueHasPrefixC_ReturnsNoAntrian7_QueueLabelC7_AndPrefixAntrianC()
    {
        // Arrange
        var tgl = new DateOnly(2026, 11, 13);
        var reg = CreateRegModel("REG01", tgl);
        SetupDependencies(reg, tgl);

        var dateTime = tgl.ToDateTime(TimeOnly.MinValue);
        var queue = new AntrianView(
            AntrianId: "AN01",
            AntrianStatus: 0,
            NoUrut: 7,
            PersonName: "Pasien Budi",
            ReffId: "REG01",
            ReffDesc: "REG",
            AntrianDate: dateTime,
            SquenceTag: "AN2611130800_DR01",
            DokterId: "DR01",
            AntrianDescription: "Praktek Dokter Spesialis",
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            QueuePrefixSnapshot: "C");

        _queRepo.Setup(x => x.ListData(dateTime)).Returns([queue]);

        // Act
        var result = await _sut.Handle(new RegGetQuery("REG01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("REG01");
        result.NoAntrian.Should().Be(7);
        result.QueueLabel.Should().Be("C7");
        result.PrefixAntrian.Should().Be("C");
    }

    [Fact]
    public async Task Handle_WhenPhysicianQueueHasEmptyPrefix_ReturnsNoAntrian7_QueueLabel7_AndPrefixAntrianEmpty()
    {
        // Arrange
        var tgl = new DateOnly(2026, 11, 13);
        var reg = CreateRegModel("REG01", tgl);
        SetupDependencies(reg, tgl);

        var dateTime = tgl.ToDateTime(TimeOnly.MinValue);
        var queue = new AntrianView(
            AntrianId: "AN01",
            AntrianStatus: 0,
            NoUrut: 7,
            PersonName: "Pasien Budi",
            ReffId: "REG01",
            ReffDesc: "REG",
            AntrianDate: dateTime,
            SquenceTag: "AN2611130800_DR01",
            DokterId: "DR01",
            AntrianDescription: "Praktek Dokter Spesialis",
            StartTime: new TimeOnly(8, 0),
            EndTime: new TimeOnly(12, 0),
            QueuePrefixSnapshot: "");

        _queRepo.Setup(x => x.ListData(dateTime)).Returns([queue]);

        // Act
        var result = await _sut.Handle(new RegGetQuery("REG01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("REG01");
        result.NoAntrian.Should().Be(7);
        result.QueueLabel.Should().Be("7");
        result.PrefixAntrian.Should().Be("");
    }

    [Fact]
    public async Task Handle_WhenNoQueueFound_ReturnsFallbackNoAntrianMinus1_QueueLabelEmpty_AndPrefixAntrianEmpty()
    {
        // Arrange
        var tgl = new DateOnly(2026, 11, 13);
        var reg = CreateRegModel("REG01", tgl);
        SetupDependencies(reg, tgl);

        var dateTime = tgl.ToDateTime(TimeOnly.MinValue);
        _queRepo.Setup(x => x.ListData(dateTime)).Returns([]);

        // Act
        var result = await _sut.Handle(new RegGetQuery("REG01"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("REG01");
        result.NoAntrian.Should().Be(-1);
        result.QueueLabel.Should().Be("");
        result.PrefixAntrian.Should().Be("");
    }

    private void SetupDependencies(RegModel reg, DateOnly tgl)
    {
        _regRepo.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == reg.RegId)))
            .Returns(MayBe.From(reg));

        var tipeJaminan = TipeJaminanType.BayarSendiri;
        _tipeJaminanRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeJaminanKey>()))
            .Returns(MayBe.From(tipeJaminan));

        _jaminanRepo.Setup(x => x.LoadEntity(It.IsAny<IJaminanKey>()))
            .Returns(MayBe.From(JaminanType.Default));

        _tglJamProvider.Setup(x => x.Now)
            .Returns(tgl.ToDateTime(new TimeOnly(9, 0)));
    }

    private static RegModel CreateRegModel(string regId, DateOnly tgl)
    {
        var person = new PersonInfoType("BUDI", new DateOnly(1990, 1, 1), "L",
            AlamatType.Default, ContactType.Default, IdentitasType.Default);
        var ktp = new KtpType("3201010101900001", AlamatType.Default, "01", "01", KelurahanType.Default);
        var pasien = new PasienModel("P01", person, "Budi", "Jakarta", GolDarahType.Default, "-",
            ktp, KelurahanType.Default, IdentitasType.Default, [],
            PasienKeluargaType.Default, AgamaType.Default, SukuType.Default,
            StatusKawinDkType.Default, PendidikanDkType.Default, PekerjaanDkType.Default,
            DateTime.Now, true);

        var layanan = LayananType.Default with { LayananId = "LAY01", LayananName = "Poli Penyakit Dalam" };
        var dokter = new PpaType("DR01", "dr. Budi", "Dokter Spesialis", SmfType.Default, GroupSpesialisType.Default, [], [], []);
        var karcis = KarcisType.Default with { KarcisId = "K01", KarcisName = "Karcis Spesialis" };

        return new RegModel(
            regId,
            tgl,
            new AuditInfoType("USER1", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            pasien.ToReff(),
            TipeJaminanType.BayarSendiri.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.DatangSendiri,
            RujukanType.Default.ToReff(),
            dokter.ToReff(),
            layanan.ToReff(),
            karcis.ToReff(),
            RegEligibilityType.Default,
            []);
    }
}
