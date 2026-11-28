using System;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class StartOpModelTest
{
    [Fact]
    public void CreateFromSchedule_GivenValidParam_ShouldReturnStartOpModel()
    {
        // Arrange
        var scheduleOp = CreateScheduleOp();
        var tglOp = new DateTime(2023, 1, 1);
        var kamar = CreateKamar();
        var clearanceId = "CLR-01";
        var userId = "user1";
        var createdAt = new DateTime(2023, 1, 1, 10, 0, 0);

        // Act
        var result = StartOpModel.CreateFromSchedule(scheduleOp, tglOp, kamar, clearanceId, userId, createdAt);

        // Assert
        result.Should().NotBeNull();
        result.StartOpId.Should().NotBeNullOrWhiteSpace();
        result.StartOpTime.Should().Be(tglOp);
        result.ClearanceId.Should().Be(clearanceId);
        result.ScheduleOp.ScheduleOpId.Should().Be(scheduleOp.ScheduleOpId);
        result.OrderOp.OrderOpId.Should().Be(scheduleOp.OrderOp.OrderOpId);
        result.KamarOp.KamarId.Should().Be(kamar.KamarId);
        result.Pasien.PasienId.Should().Be(scheduleOp.Pasien.PasienId);
        result.Reg.RegId.Should().Be(scheduleOp.Reg.RegId);
        result.AuditTrail.Created.UserId.Should().Be(userId);
        result.AuditTrail.Created.Timestamp.Should().Be(createdAt);
    }

    [Fact]
    public void CreateFromSchedule_WhenClearanceIdNull_ShouldThrowException()
    {
        // Arrange
        var scheduleOp = CreateScheduleOp();
        var tglOp = DateTime.Today;
        var kamar = CreateKamar();

        // Act
        Action act = () => StartOpModel.CreateFromSchedule(scheduleOp, tglOp, kamar, null!, "user1");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateFromSchedule_WhenClearanceIdEmpty_ShouldThrowException()
    {
        // Arrange
        var scheduleOp = CreateScheduleOp();
        var tglOp = DateTime.Today;
        var kamar = CreateKamar();

        // Act
        Action act = () => StartOpModel.CreateFromSchedule(scheduleOp, tglOp, kamar, "", "user1");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static ScheduleOpModel CreateScheduleOp()
    {
        return new ScheduleOpModel(
            "SCH-01",
            DateTime.Today,
            AuditTrailType.Default,
            new OrderOpReff("ORD-01", DateTime.Today, "ORD-NAME"),
            new PasienReff("PAS-01", "PAS-NAME", new DateOnly(1980, 1, 1), "M"),
            UrgencyLevelEnum.Elective,
            60,
            DateTime.Today,
            KamarReff.Default,
            new RegReff("REG-01", "PAS-01", "PAS-NAME"),
            new Bilreg.Domain.AdmisiContext.PpaFeature.PpaReff("PPA-01", "PPA-NAME"),
            DateTime.Today,
            DateTime.Today,
            OpCaseStateEnum.Scheduled,
            []
        );
    }

    private static KamarType CreateKamar()
    {
        return new KamarType("KAM-01", "KAM-NAME", new BangsalReff("BANGSAL-01", "BANGSAL-NAME"), new KelasReff("KELAS-01", "KELAS-NAME"));
    }
}
