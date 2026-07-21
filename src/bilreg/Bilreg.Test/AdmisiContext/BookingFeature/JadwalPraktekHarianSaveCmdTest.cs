using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;

using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekHarianSaveCmdTest
{
    private readonly Mock<IJadwalPraktekHarianRepo> _harianRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<IRuangRepo> _ruangRepo = new();
    private readonly Mock<IJadwalPraktekHarianOverrideGuard> _overrideGuard = new();

    private readonly JadwalPraktekHarianSaveHandler _sut;

    public JadwalPraktekHarianSaveCmdTest()
    {
        _sut = new JadwalPraktekHarianSaveHandler(
            _harianRepo.Object, _ppaRepo.Object,
            _layananRepo.Object, _ruangRepo.Object, _overrideGuard.Object, TestTglJamProvider.Instance);
    }

    [Fact]
    public async Task UT01_CreateNew_AssignsNunaIdAndSaves()
    {
        var dokter = new PpaType("DR001", "Dr", "Dr", SmfType.Default,
            GroupSpesialisType.Default, [], [], []);
        var layanan = LayananType.Default;
        var ruang = RuangType.Default;
        JadwalPraktekHarianType? saved = null;

        _ppaRepo.Setup(r => r.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DR001"))).Returns(MayBe.From(dokter));
        _layananRepo.Setup(r => r.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LY001"))).Returns(MayBe.From(layanan));
        _ruangRepo.Setup(r => r.LoadEntity(It.IsAny<RuangType>())).Returns(MayBe.From(ruang));
        _harianRepo.Setup(r => r.SaveChanges(It.IsAny<JadwalPraktekHarianType>()))
            .Callback<JadwalPraktekHarianType>(m => saved = m);

        var cmd = new JadwalPraktekHarianSaveCmd(
            null, "JADW001", "2026-07-15", "DR001", "LY001", "R001",
            "08:00", "12:00", 30,
            new JadwalPraktekSaveAntrianPatternCmd("DEFAULT", 30, 0, []),
            null, "user1");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.JadwalPraktekHarianId.Should().StartWith("JPH");
        saved.Should().NotBeNull();
        saved!.JadwalPraktekHarianId.Should().Be(result.JadwalPraktekHarianId);
        saved.Source.Should().Be(JadwalPraktekHarianSource.MANUAL);
        _harianRepo.Verify(r => r.SaveChanges(It.IsAny<JadwalPraktekHarianType>()), Times.Once);
        _overrideGuard.Verify(g => g.EnsureNoOperationalConflict(
            It.IsAny<DateOnly>(), It.IsAny<IPpaKey>(), It.IsAny<TimeOnly>()), Times.Once);
    }
}
