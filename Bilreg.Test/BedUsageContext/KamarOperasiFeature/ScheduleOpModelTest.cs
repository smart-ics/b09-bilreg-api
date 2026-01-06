using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

public class ScheduleOpModelTests
{
    private static PpaType CreatePpa(
        string id,
        ProfesiType profesi)
    {
        return new PpaType(
            id,
            "A",
            "B",
            SmfType.Default,
            GroupSpesialisType.Default,
            new List<PpaLayananType>
            {
                new PpaLayananType(new LayananReff("A", "B"), true)
            },
            new List<PpaSatTugasType>
            {
                new PpaSatTugasType(
                    new SatTugasType("A","B", profesi),
                    true)
            },
            new List<ContactType>
            {
                new ContactType(JenisContactEnum.Email, "C")
            }
        );
    }

    private static ScheduleOpModel CreateEmptySchedule()
    {
        return new ScheduleOpModel(
            "S1",
            new DateTime(3000,1,1),
            AuditTrailType.Default,
            OrderOpModel.Default.ToReff(),
            PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective,
            60,
            DateTime.Today,
            KamarType.Default.ToReff(),
            RegModel.Default.ToReff(),
            PpaType.Default.ToReff(),
            new DateTime(3000, 1, 1),
            new DateTime(3000, 1, 1),
            OpCaseStateEnum.Scheduled,
            Enumerable.Empty<ScheduleOpPpaType>()
        );
    }

    // ---------------------------------------------------------
    // UT1 - AddPpa
    // ---------------------------------------------------------
    [Fact]
    public void UT1_GivenValidPpa_WhenAddPpa_ThenPpaAddedWithNextNoUrut()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var ppa = CreatePpa("P1", ProfesiType.Dokter);

        // Act
        schedule.AddPpa(ppa, "user1");

        // Assert
        var list = schedule.ListPpa.ToList();
        list.Should().HaveCount(1);
        list[0].Ppa.PpaId.Should().Be("P1");
        list[0].NoUrut.Should().Be(1);
    }

    [Fact]
    public void UT2_GivenExistingPpa_WhenAddPpa_ThenThrowException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var ppa = CreatePpa("P1", ProfesiType.Dokter);
        schedule.AddPpa(ppa, "user1");

        // Act
        Action act = () => schedule.AddPpa(ppa, "user1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("PPA sudah ada dalam schedule");
    }

    [Fact]
    public void UT3_GivenInvalidProfesi_WhenAddPpa_ThenThrowException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var ppa = CreatePpa("X", profesi: null!); // profesi null

        // Act
        Action act = () => schedule.AddPpa(ppa, "user1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Profesi PPA tidak valid");
    }

    // ---------------------------------------------------------
    // UT4 - RemovePpa
    // ---------------------------------------------------------
    [Fact]
    public void UT4_GivenExistingPpa_WhenRemovePpa_ThenPpaRemovedAndNoUrutResequenced()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var p1 = CreatePpa("P1", ProfesiType.Default);
        var p2 = CreatePpa("P2", ProfesiType.Default);
        var p3 = CreatePpa("P3", ProfesiType.Default);

        schedule.AddPpa(p1, "user1");
        schedule.AddPpa(p2, "user1");
        schedule.AddPpa(p3, "user1");

        // Act
        schedule.RemovePpa(p2, "user1");

        // Assert
        var list = schedule.ListPpa.ToList();
        list.Should().HaveCount(2);

        list[0].Ppa.PpaId.Should().Be("P1");
        list[0].NoUrut.Should().Be(1);

        list[1].Ppa.PpaId.Should().Be("P3");
        list[1].NoUrut.Should().Be(2);
    }

    // ---------------------------------------------------------
    // UT5 - AssignLeader
    // ---------------------------------------------------------
    [Fact]
    public void UT5_GivenValidDoctorInList_WhenAssignLeader_ThenLeaderAssigned()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var doctor = CreatePpa("D1", ProfesiType.Dokter);

        schedule.AddPpa(doctor, "user1");

        // Act
        schedule.AssignLeader(doctor, "user1");

        // Assert
        schedule.TeamLead.PpaId.Should().Be("D1");
    }

    [Fact]
    public void UT6_GivenNonDoctor_WhenAssignLeader_ThenThrowException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var nurse = CreatePpa("N1", ProfesiType.Perawat);

        schedule.AddPpa(nurse, "user1");

        // Act
        Action act = () => schedule.AssignLeader(nurse, "user1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("PPA harus dokter");
    }

    [Fact]
    public void UT7_GivenDoctorNotInList_WhenAssignLeader_ThenThrowException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var doctor = CreatePpa("D1", ProfesiType.Dokter);

        // Act
        Action act = () => schedule.AssignLeader(doctor, "user1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("PPA harus terdaftar di member");
    }

    // ---------------------------------------------------------
    // UT8 - SetSchedule
    // ---------------------------------------------------------
    [Fact]
    public void UT8_GivenNewSchedule_WhenSetSchedule_ThenScheduleUpdated()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var newDate = DateTime.Today.AddDays(3);
        var newKamar = new KamarReff("K1", "Kamar 1");
        var durasi = 90;

        // Act
        schedule.SetSchedule(newDate, newKamar, durasi, "user1");

        // Assert
        schedule.TglOp.Should().Be(newDate);
        schedule.KamarOp.KamarId.Should().Be("K1");
    }

    // ---------------------------------------------------------
    // UT9 - CreateFromOrder
    // ---------------------------------------------------------
    [Fact]
    public void UT9_GivenOrder_WhenCreateFromOrder_ThenInitialValuesAssigned()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var teamLeader = CreatePpa("D1", ProfesiType.Dokter);

        // Act
        var result = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, teamLeader, DateTime.Today);

        // Assert
        result.OrderOp.OrderOpId.Should().Be(order.OrderOpId);
        result.TeamLead.PpaId.Should().Be("D1");
        result.ListPpa.Should().ContainSingle(x => x.Ppa.PpaId == "D1");
    }
}
