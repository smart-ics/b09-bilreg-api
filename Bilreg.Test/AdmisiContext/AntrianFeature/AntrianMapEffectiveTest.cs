using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapEffectiveTest
{
    [Fact]
    public void UT01_CreateFromEffective_SetsJadwalIds()
    {
        var effective = new JadwalPraktekEffective
        {
            JadwalPraktekId = "JADW001",
            JadwalPraktekHarianId = "JPH00000001",
            TglPraktek = new DateOnly(2026, 7, 15),
            Dokter = new PpaReff("DR001", "Dr"),
            Layanan = new LayananReff("LY001", "Poli"),
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            MaxPasien = 30,
            AntrianPattern = AntrianPatternType.Default,
            Status = JadwalPraktekScheduleStatus.ACTIVE,
            Source = JadwalPraktekSource.DAILY_MANUAL
        };

        var map = AntrianMapModel.CreateFromEffective(effective, effective.TglPraktek);

        map.JadwalId.Should().Be("JADW001");
        map.JadwalHarianId.Should().Be("JPH00000001");
        map.MaxPasien.Should().Be(30);
    }
}
