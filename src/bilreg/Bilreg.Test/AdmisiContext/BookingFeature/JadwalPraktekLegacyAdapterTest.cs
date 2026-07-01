using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekLegacyAdapterTest
{
    [Fact]
    public void UT01_ToTemplate_UsesLineageIdAndMapsScheduleFields()
    {
        var effective = new JadwalPraktekEffective
        {
            JadwalPraktekId = "JADW001",
            JadwalPraktekHarianId = "JPH00000001",
            TglPraktek = new DateOnly(2026, 7, 15),
            Dokter = new PpaReff("DR001", "Dr"),
            Layanan = new LayananReff("LY001", "Poli"),
            LayananDk = new LayananDkReff("1", "UMUM"),
            GroupSpesialis = GroupSpesialisType.Default,
            Ruang = new RuangType("RU01", "RUANG1", "A"),
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            MaxPasien = 30,
            AntrianPattern = AntrianPatternType.Default,
            Status = JadwalPraktekScheduleStatus.ACTIVE,
            Source = JadwalPraktekSource.DAILY_MANUAL
        };

        var template = JadwalPraktekLegacyAdapter.ToTemplate(effective);

        template.JadwalPraktekId.Should().Be("JADW001");
        template.Hari.Should().Be(DayOfWeek.Wednesday);
        template.JamMulai.Should().Be(effective.JamMulai);
        template.MaxPasien.Should().Be(30);
    }

    [Fact]
    public void UT02_ToTemplate_NullLineageId_UsesDashPlaceholder()
    {
        var effective = JadwalPraktekEffectiveMapper.Synthetic(
            new DateOnly(2026, 7, 15),
            new PpaReff("DR001", "Dr"),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0));

        var template = JadwalPraktekLegacyAdapter.ToTemplate(effective);

        template.JadwalPraktekId.Should().Be("-");
    }
}
