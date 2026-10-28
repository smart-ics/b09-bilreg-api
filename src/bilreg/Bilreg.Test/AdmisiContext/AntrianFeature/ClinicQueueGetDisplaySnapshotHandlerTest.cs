using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class ClinicQueueGetDisplaySnapshotHandlerTest
{
    private readonly Mock<IClinicQueueOperationDal> _dalMock = new();
    private readonly Mock<IAntrianRepo> _repoMock = new();
    private readonly Mock<ITglJamProvider> _clockMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IRuangRepo> _ruangRepoMock = new();

    private readonly DateTime _now = new(2026, 10, 28, 9, 30, 0);

    public ClinicQueueGetDisplaySnapshotHandlerTest()
    {
        _clockMock.Setup(c => c.Now).Returns(_now);
    }

    private ClinicQueueGetDisplaySnapshotHandler CreateSut() =>
        new(
            _dalMock.Object,
            _repoMock.Object,
            _clockMock.Object,
            _layananRepoMock.Object,
            _ppaRepoMock.Object,
            _ruangRepoMock.Object);

    [Fact]
    public async Task Handle_WhenNoPoliIdsAndNoDisplayId_ReturnsEmptyResponse()
    {
        var sut = CreateSut();
        var query = new ClinicQueueGetDisplaySnapshotQuery();

        var response = await sut.Handle(query, CancellationToken.None);

        response.ActiveCalls.Should().BeEmpty();
        response.WaitingItems.Should().BeEmpty();
        _dalMock.Verify(d => d.GetActiveCallsByPoliIds(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDisplayIdProvidedWithoutPoliIds_ResolvesPolisFromDal()
    {
        var sut = CreateSut();
        _dalMock.Setup(d => d.GetPolisByDisplayId("DISP-01"))
            .Returns(new List<string> { "POLI-01", "POLI-02" });
        _dalMock.Setup(d => d.GetActiveCallsByPoliIds(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<ClinicQueueCallDto>());

        var query = new ClinicQueueGetDisplaySnapshotQuery(DisplayId: "DISP-01");

        var response = await sut.Handle(query, CancellationToken.None);

        _dalMock.Verify(d => d.GetPolisByDisplayId("DISP-01"), Times.Once);
        _dalMock.Verify(d => d.GetActiveCallsByPoliIds(It.Is<IEnumerable<string>>(p => p.Contains("POLI-01") && p.Contains("POLI-02"))), Times.Once);
        response.ActiveCalls.Should().BeEmpty();
        response.WaitingItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPoliIdsProvidedDirectly_UsesProvidedPoliIdsWithoutCallingDalForDisplay()
    {
        var sut = CreateSut();
        _dalMock.Setup(d => d.GetActiveCallsByPoliIds(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<ClinicQueueCallDto>());

        var query = new ClinicQueueGetDisplaySnapshotQuery(PoliIds: new[] { "POLI-INT", "POLI-ANAK" });

        var response = await sut.Handle(query, CancellationToken.None);

        _dalMock.Verify(d => d.GetPolisByDisplayId(It.IsAny<string>()), Times.Never);
        _dalMock.Verify(d => d.GetActiveCallsByPoliIds(It.Is<IEnumerable<string>>(p => p.Contains("POLI-INT") && p.Contains("POLI-ANAK"))), Times.Once);
        response.ActiveCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithActiveCallsAndWaitingItems_MapsResponseCorrectly()
    {
        var sut = CreateSut();
        var poliIds = new[] { "POLI-INT" };

        var callDto = new ClinicQueueCallDto
        {
            PoliId = "POLI-INT",
            DokterId = "DOC-01",
            RuangId = "R-101",
            AntrianId = "QUE-100",
            NoUrut = 1,
            CalledAt = _now,
            CallCount = 2,
            CallStatus = ClinicQueueCallStatus.Active
        };

        _dalMock.Setup(d => d.GetActiveCallsByPoliIds(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<ClinicQueueCallDto> { callDto });

        // Setup Layanan, Ppa, Ruang names
        var layanan = LayananType.Default with { LayananId = "POLI-INT", LayananName = "Poliklinik Penyakit Dalam" };
        _layananRepoMock.Setup(r => r.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "POLI-INT")))
            .Returns(MayBe.From(layanan));

        var ppa = PpaType.Default with { PpaId = "DOC-01", PpaName = "dr. Budi Santoso, Sp.PD" };
        _ppaRepoMock.Setup(r => r.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DOC-01")))
            .Returns(MayBe.From(ppa));

        var ruang = new RuangType("R-101", "Ruang Periksa 1", "A");
        _ruangRepoMock.Setup(r => r.LoadEntity(It.Is<IRuangKey>(k => k.RuangId == "R-101")))
            .Returns(MayBe.From(ruang));

        // Setup Queue Model with 1 active and 2 waiting entries
        var birthDate = new DateOnly(1980, 1, 1);
        var entry1 = new AntrianEntryModel(1, new PersonType("Tn. Ahmad", birthDate), PasienTrackerModel.Default,
            AntrianStatusEnum.Waiting, _now.AddMinutes(-30), DateTime.MaxValue, DateTime.MaxValue, "REG-1", "", false, 0, 2);
        var entry2 = new AntrianEntryModel(2, new PersonType("Ny. Siti", birthDate), PasienTrackerModel.Default,
            AntrianStatusEnum.Waiting, _now.AddMinutes(-20), DateTime.MaxValue, DateTime.MaxValue, "REG-2", "", false, 0, 0);
        var entry3 = new AntrianEntryModel(3, new PersonType("Tn. Joko", birthDate), PasienTrackerModel.Default,
            AntrianStatusEnum.Waiting, _now.AddMinutes(-10), DateTime.MaxValue, DateTime.MaxValue, "REG-3", "", true, 0, 0);

        var queue = new AntrianModel("QUE-100", DateOnly.FromDateTime(_now), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "AN2610280800_DOC-01", "Praktek Dokter DOC-01", ServicePointType.Default,
            new[] { entry1, entry2, entry3 }, null!, queuePrefixSnapshot: "A");

        _repoMock.Setup(r => r.LoadEntity(It.Is<IAntrianKey>(k => k.AntrianId == "QUE-100")))
            .Returns(MayBe.From(queue));

        var query = new ClinicQueueGetDisplaySnapshotQuery(PoliIds: poliIds);

        var response = await sut.Handle(query, CancellationToken.None);

        // Active Calls verification
        response.ActiveCalls.Should().HaveCount(1);
        var activeCall = response.ActiveCalls[0];
        activeCall.AntrianId.Should().Be("QUE-100");
        activeCall.NoUrut.Should().Be(1);
        activeCall.NomorAntrean.Should().Be("A0001");
        activeCall.NamaPasien.Should().Be("Tn. Ahmad");
        activeCall.PoliId.Should().Be("POLI-INT");
        activeCall.PoliName.Should().Be("Poliklinik Penyakit Dalam");
        activeCall.DokterId.Should().Be("DOC-01");
        activeCall.DokterName.Should().Be("dr. Budi Santoso, Sp.PD");
        activeCall.RuangId.Should().Be("R-101");
        activeCall.RuangName.Should().Be("Ruang Periksa 1");
        activeCall.CallCount.Should().Be(2);
        activeCall.CalledAt.Should().Be(_now);

        // Waiting Items verification: entry 1 is active, so waiting items are entry 3 (Priority) then entry 2
        response.WaitingItems.Should().HaveCount(2);
        var wait1 = response.WaitingItems[0];
        wait1.NoUrut.Should().Be(3);
        wait1.NomorAntrean.Should().Be("A0003");
        wait1.NamaPasien.Should().Be("Tn. Joko");
        wait1.Priority.Should().BeTrue();
        wait1.PoliName.Should().Be("Poliklinik Penyakit Dalam");
        wait1.DokterName.Should().Be("dr. Budi Santoso, Sp.PD");

        var wait2 = response.WaitingItems[1];
        wait2.NoUrut.Should().Be(2);
        wait2.NomorAntrean.Should().Be("A0002");
        wait2.NamaPasien.Should().Be("Ny. Siti");
        wait2.Priority.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenResolversFail_FallsBackToRawIdsGracefully()
    {
        // Sut without optional repo dependencies
        var sut = new ClinicQueueGetDisplaySnapshotHandler(
            _dalMock.Object,
            _repoMock.Object,
            _clockMock.Object);

        var callDto = new ClinicQueueCallDto
        {
            PoliId = "POLI-SARAF",
            DokterId = "DOC-SARAF",
            RuangId = "R-202",
            AntrianId = "QUE-200",
            NoUrut = 5,
            CalledAt = _now,
            CallCount = 1,
            CallStatus = ClinicQueueCallStatus.Active
        };

        _dalMock.Setup(d => d.GetActiveCallsByPoliIds(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<ClinicQueueCallDto> { callDto });

        var entry = new AntrianEntryModel(5, new PersonType("Pasien X", new DateOnly(1985, 5, 5)), PasienTrackerModel.Default,
            AntrianStatusEnum.Waiting, _now, DateTime.MaxValue, DateTime.MaxValue, "REG-5", "");

        var queue = new AntrianModel("QUE-200", DateOnly.FromDateTime(_now), new TimeOnly(8, 0), new TimeOnly(12, 0),
            "AN", "Desc", ServicePointType.Default, new[] { entry }, null!, queuePrefixSnapshot: "");

        _repoMock.Setup(r => r.LoadEntity(It.Is<IAntrianKey>(k => k.AntrianId == "QUE-200")))
            .Returns(MayBe.From(queue));

        var query = new ClinicQueueGetDisplaySnapshotQuery(PoliIds: new[] { "POLI-SARAF" });

        var response = await sut.Handle(query, CancellationToken.None);

        response.ActiveCalls.Should().HaveCount(1);
        response.ActiveCalls[0].PoliName.Should().Be("POLI-SARAF");
        response.ActiveCalls[0].DokterName.Should().Be("DOC-SARAF");
        response.ActiveCalls[0].RuangName.Should().Be("R-202");
        response.ActiveCalls[0].NomorAntrean.Should().Be("5");
    }
}
