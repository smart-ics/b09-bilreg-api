using Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;

using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.BookingFeature;

public class JadwalPraktekEffectiveQueryTest
{
    private readonly Mock<IJadwalPraktekFeatureResolver> _featureResolver = new();
    private readonly Mock<IAntrianRepo> _antrianRepo = new();

    private static readonly DateOnly Tgl = new(2026, 7, 15);

    [Fact]
    public async Task UT01_ListResponse_ExcludesInternalScheduleIds()
    {
        var effective = new JadwalPraktekEffective
        {
            JadwalPraktekId = "JADW001",
            JadwalPraktekHarianId = "JPH00000001",
            TglPraktek = Tgl,
            Dokter = new PpaReff("DR001", "Dr"),
            Layanan = LayananType.Default.ToReff(),
            Ruang = RuangType.Default,
            JamMulai = new TimeOnly(8, 0),
            JamSelesai = new TimeOnly(12, 0),
            MaxPasien = 30,
            Status = JadwalPraktekScheduleStatus.ACTIVE,
            Source = JadwalPraktekSource.DAILY_MANUAL
        };
        _featureResolver.Setup(f => f.ResolveForDate(It.IsAny<JadwalPraktekResolveForDateRequest>()))
            .Returns([effective]);
        _antrianRepo.Setup(r => r.ListData(Tgl)).Returns([]);

        var handler = new JadwalPraktekEffectiveListHandler(_featureResolver.Object, _antrianRepo.Object);
        var result = (await handler.Handle(new JadwalPraktekEffectiveListQuery("DR001", "2026-07-15"),
            CancellationToken.None)).ToList();

        result.Should().ContainSingle();
        var item = result[0];
        item.DokterId.Should().Be("DR001");
        item.AvailableQuota.Should().Be(30);
        typeof(JadwalPraktekEffectiveResponse).GetProperties()
            .Select(p => p.Name)
            .Should().NotContain(new[] { "JadwalPraktekId", "JadwalPraktekHarianId", "Source" });
    }

    [Fact]
    public async Task UT02_GetResponse_ComputesAvailableQuota()
    {
        var effective = JadwalPraktekEffectiveMapper.FromTemplate(
            new JadwalPraktekType("JADW001", new PpaReff("DR001", "Dr"),
                LayananType.Default.ToReff(), LayananDkType.Default.ToReff(),
                GroupSpesialisType.Default, RuangType.Default,
                DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(12, 0), 10,
                AntrianPatternType.Default), Tgl);
        var tag = AntrianModel.GenSequenceTag(Tgl, effective);
        var header = new AntrianHeaderView("A1", "desc", Tgl, new TimeOnly(8, 0), tag);
        var entries = new[]
        {
            AntrianEntryModel.Create(1, PersonType.Default, PasienTrackerModel.Default, "-", "-"),
            AntrianEntryModel.Create(2, PersonType.Default, PasienTrackerModel.Default, "-", "-")
        };
        var antrian = new AntrianModel("A1", Tgl, new TimeOnly(8, 0), new TimeOnly(12, 0),
            tag, "desc", entries, null!);

        _featureResolver.Setup(f => f.Resolve(It.IsAny<JadwalPraktekResolveRequest>()))
            .Returns(effective);
        _antrianRepo.Setup(r => r.ListData(Tgl)).Returns([header]);
        _antrianRepo.Setup(r => r.LoadEntity(header)).Returns(MayBe.From(antrian));

        var handler = new JadwalPraktekEffectiveGetHandler(_featureResolver.Object, _antrianRepo.Object);
        var result = await handler.Handle(
            new JadwalPraktekEffectiveGetQuery("DR001", "2026-07-15", "08:00"),
            CancellationToken.None);

        result.AvailableQuota.Should().Be(8);
    }
}
