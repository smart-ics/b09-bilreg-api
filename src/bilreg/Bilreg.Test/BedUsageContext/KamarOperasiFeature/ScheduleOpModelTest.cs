using System;
using System.Collections.Generic;
using System.Linq;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpModelTests
{
    private static PpaType CreatePpa(
        string id,
        ProfesiType profesi)
    {
        return new PpaType(
            id,
            "Dr. " + id,
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
                    new SatTugasType("A", "B", profesi),
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
            new DateTime(3000, 1, 1),
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
            Enumerable.Empty<ScheduleOpPpaType>(),
            "",
            30,
            Enumerable.Empty<ScheduleOpHistType>()
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
    public void UT5_GivenValidDoctorInList_WhenAssignLeader_ThenLeaderAssignedAndHistoryRecorded()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var doctor = CreatePpa("D1", ProfesiType.Dokter);
        schedule.AddPpa(doctor, "user1");
        var updatedAt = new DateTime(2026, 10, 21, 10, 0, 0);

        // Act
        schedule.AssignLeader(doctor, "Pergantian dokter operator utama", "user1", updatedAt);

        // Assert
        schedule.TeamLead.PpaId.Should().Be("D1");
        schedule.ListHistory.Should().HaveCount(1);
        var hist = schedule.ListHistory.First();
        hist.NoUrut.Should().Be(1);
        hist.OldTglOp.Should().Be(schedule.TglOp);
        hist.NewTglOp.Should().Be(schedule.TglOp);
        hist.OldDurasi.Should().Be(schedule.Durasi);
        hist.NewDurasi.Should().Be(schedule.Durasi);
        hist.OldKamar.Should().Be(schedule.KamarOp);
        hist.NewKamar.Should().Be(schedule.KamarOp);
        hist.OldTeamLead.PpaId.Should().Be("-");
        hist.NewTeamLead.PpaId.Should().Be("D1");
        hist.Alasan.Should().Be("Pergantian dokter operator utama");
        hist.IsOverride.Should().BeFalse();
        hist.OverrideReason.Should().BeEmpty();
        hist.UserId.Should().Be("user1");
        hist.CrtDate.Should().Be(updatedAt);
        hist.ChangeType.Should().Be("ASSIGN_LEADER");
    }

    [Fact]
    public void UT6_GivenNonDoctor_WhenAssignLeader_ThenThrowException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var nurse = CreatePpa("N1", ProfesiType.Perawat);

        schedule.AddPpa(nurse, "user1");

        // Act
        Action act = () => schedule.AssignLeader(nurse, "Alasan valid", "user1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("PPA harus dokter");
    }

    [Fact]
    public void UT7_GivenDoctorNotInList_WhenAssignLeader_ThenDoctorAutoRegisteredAndPlacedAtNoUrutOne()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var doctor1 = CreatePpa("D1", ProfesiType.Dokter);
        var schedule = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, doctor1, DateTime.Today);
        var doctor2 = CreatePpa("D2", ProfesiType.Dokter);

        // Act
        schedule.AssignLeader(doctor2, "Dokter pengganti dari luar", "user1", isOverride: true, overrideReason: "ACC Direktur");

        // Assert
        schedule.TeamLead.PpaId.Should().Be("D2");
        var list = schedule.ListPpa.ToList();
        list.Should().HaveCount(2);
        list[0].Ppa.PpaId.Should().Be("D2");
        list[0].NoUrut.Should().Be(1);
        list[1].Ppa.PpaId.Should().Be("D1");
        list[1].NoUrut.Should().Be(2);

        var hist = schedule.ListHistory.First();
        hist.ChangeType.Should().Be("ASSIGN_LEADER");
        hist.IsOverride.Should().BeTrue();
        hist.OverrideReason.Should().Be("ACC Direktur");
    }

    [Fact]
    public void AssignLeader_GivenDoctorAlreadyInList_ShouldMoveNewDoctorToNoUrut1AndDemoteOldDoctor()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var doctor1 = CreatePpa("D1", ProfesiType.Dokter);
        var doctor2 = CreatePpa("D2", ProfesiType.Dokter);
        var nurse = CreatePpa("N1", ProfesiType.Perawat);
        var schedule = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, doctor1, DateTime.Today);
        schedule.AddPpa(doctor2, "user1");
        schedule.AddPpa(nurse, "user1");

        // Act
        schedule.AssignLeader(doctor2, "Pergantian dokter operator utama", "user1");

        // Assert
        schedule.TeamLead.PpaId.Should().Be("D2");
        var list = schedule.ListPpa.ToList();
        list.Should().HaveCount(3);
        list[0].Ppa.PpaId.Should().Be("D2");
        list[0].NoUrut.Should().Be(1);
        list[1].Ppa.PpaId.Should().Be("D1");
        list[1].NoUrut.Should().Be(2);
        list[2].Ppa.PpaId.Should().Be("N1");
        list[2].NoUrut.Should().Be(3);
    }

    [Fact]
    public void RemovePpa_GivenActiveTeamLead_ShouldThrowArgumentException()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var doctor1 = CreatePpa("D1", ProfesiType.Dokter);
        var schedule = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, doctor1, DateTime.Today);

        // Act
        Action act = () => schedule.RemovePpa(doctor1, "user1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Dokter Operator Utama aktif tidak dapat dihapus dari tim operasi. Alihkan peran TeamLead terlebih dahulu.");
    }

    [Fact]
    public void RemovePpa_GivenNonLeadMember_ShouldPreserveTeamLeadAtNoUrutOneAndResequenceOthers()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var doctor1 = CreatePpa("D1", ProfesiType.Dokter);
        var doctor2 = CreatePpa("D2", ProfesiType.Dokter);
        var nurse1 = CreatePpa("N1", ProfesiType.Perawat);
        var nurse2 = CreatePpa("N2", ProfesiType.Perawat);
        var schedule = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, doctor1, DateTime.Today);
        schedule.AddPpa(doctor2, "user1");
        schedule.AddPpa(nurse1, "user1");
        schedule.AddPpa(nurse2, "user1");

        // Act - remove doctor2 (originally NoUrut 2)
        schedule.RemovePpa(doctor2, "user1");

        // Assert
        schedule.TeamLead.PpaId.Should().Be("D1");
        var list = schedule.ListPpa.ToList();
        list.Should().HaveCount(3);
        list[0].Ppa.PpaId.Should().Be("D1");
        list[0].NoUrut.Should().Be(1);
        list[1].Ppa.PpaId.Should().Be("N1");
        list[1].NoUrut.Should().Be(2);
        list[2].Ppa.PpaId.Should().Be("N2");
        list[2].NoUrut.Should().Be(3);
    }

    [Fact]
    public void Constructor_GivenDisorderedOrGappedNoUrut_ShouldSelfHealWithTeamLeadAtNoUrutOneAndResequenceContiguously()
    {
        // Arrange
        var teamLead = new PpaReff("DOC-LEAD", "Dr. Lead");
        var listPpa = new List<ScheduleOpPpaType>
        {
            new ScheduleOpPpaType(5, new PpaReff("NURSE-01", "Nurse 1"), ProfesiType.Perawat, GroupSpesialisType.Default),
            new ScheduleOpPpaType(10, teamLead, ProfesiType.Dokter, GroupSpesialisType.Default),
            new ScheduleOpPpaType(2, new PpaReff("ASST-01", "Asst 1"), ProfesiType.Dokter, GroupSpesialisType.Default)
        };

        // Act
        var sut = new ScheduleOpModel(
            "SCH-HEAL-01",
            DateTime.Today,
            AuditTrailType.Default,
            OrderOpModel.Default.ToReff(),
            PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Elective,
            60,
            DateTime.Today,
            KamarType.Default.ToReff(),
            RegModel.Default.ToReff(),
            teamLead,
            DateTime.Today,
            DateTime.Today.AddHours(1),
            OpCaseStateEnum.Scheduled,
            listPpa,
            "",
            30,
            []
        );

        // Assert
        var resultList = sut.ListPpa.ToList();
        resultList.Should().HaveCount(3);
        resultList[0].Ppa.PpaId.Should().Be("DOC-LEAD");
        resultList[0].NoUrut.Should().Be(1);
        resultList[1].Ppa.PpaId.Should().Be("ASST-01");
        resultList[1].NoUrut.Should().Be(2);
        resultList[2].Ppa.PpaId.Should().Be("NURSE-01");
        resultList[2].NoUrut.Should().Be(3);
    }

    [Fact]
    public void Reschedule_GivenNewTeamLead_ShouldPlaceNewLeadAtNoUrutOneAndRetainOldLead()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var doctor1 = CreatePpa("DOC-01", ProfesiType.Dokter);
        var nurse = CreatePpa("NURSE-01", ProfesiType.Perawat);
        var sut = ScheduleOpModel.CreateFromOrder(order, "u1", kamar, doctor1, DateTime.Today);
        sut.AddPpa(nurse, "u1");

        var newLead = new PpaReff("DOC-02", "Dr. DOC-02");

        // Act
        sut.Reschedule(
            newTglOp: DateTime.Today.AddHours(2),
            newKamar: kamar.ToReff(),
            newDurasi: 90,
            newTurnoverBuffer: 30,
            newTeamLead: newLead,
            alasanPerubahan: "Pergantian dokter utama saat reschedule",
            isOverride: false,
            overrideReason: "",
            userId: "user1",
            occurredAt: DateTime.Now);

        // Assert
        sut.TeamLead.Should().Be(newLead);
        var list = sut.ListPpa.ToList();
        list.Should().HaveCount(3);
        list[0].Ppa.PpaId.Should().Be("DOC-02");
        list[0].NoUrut.Should().Be(1);
        list[1].Ppa.PpaId.Should().Be("DOC-01");
        list[1].NoUrut.Should().Be(2);
        list[2].Ppa.PpaId.Should().Be("NURSE-01");
        list[2].NoUrut.Should().Be(3);
    }

    [Fact]
    public void AssignLeader_GivenNullOrWhitespaceAlasanPerubahan_ShouldThrowArgumentException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var doctor = CreatePpa("D1", ProfesiType.Dokter);
        schedule.AddPpa(doctor, "user1");

        // Act & Assert - null reason
        FluentActions.Invoking(() => schedule.AssignLeader(doctor, null!, "user1"))
            .Should().Throw<ArgumentException>();

        // Act & Assert - whitespace reason
        FluentActions.Invoking(() => schedule.AssignLeader(doctor, "   ", "user1"))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignLeader_GivenSameDoctorAsActiveLeader_ShouldThrowArgumentException()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var doctor1 = CreatePpa("D1", ProfesiType.Dokter);
        var schedule = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, doctor1, DateTime.Today);

        // Act
        Action act = () => schedule.AssignLeader(doctor1, "Perubahan ke dokter yang sama", "user1");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Dokter Operator Utama baru sama dengan Dokter Operator Utama yang aktif.");
    }

    [Fact]
    public void AssignLeader_GivenMultipleChanges_ShouldSequenceHistoryIncrementally()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var doctor1 = CreatePpa("D1", ProfesiType.Dokter);
        var doctor2 = CreatePpa("D2", ProfesiType.Dokter);
        var doctor3 = CreatePpa("D3", ProfesiType.Dokter);
        var schedule = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, doctor1, DateTime.Today);

        schedule.AddPpa(doctor2, "user1");
        schedule.AddPpa(doctor3, "user1");

        var time1 = new DateTime(2026, 10, 21, 10, 0, 0);
        var time2 = new DateTime(2026, 10, 21, 11, 0, 0);

        // Act 1
        schedule.AssignLeader(doctor2, "Perubahan leader ke dokter 2", "user1", time1);

        // Act 2
        schedule.AssignLeader(doctor3, "Perubahan leader ke dokter 3", "user2", time2);

        // Assert
        schedule.TeamLead.PpaId.Should().Be("D3");
        schedule.ListHistory.Should().HaveCount(2);

        var list = schedule.ListHistory.ToList();
        list[0].NoUrut.Should().Be(1);
        list[0].OldTeamLead.PpaId.Should().Be("D1");
        list[0].NewTeamLead.PpaId.Should().Be("D2");
        list[0].Alasan.Should().Be("Perubahan leader ke dokter 2");
        list[0].ChangeType.Should().Be("ASSIGN_LEADER");
        list[0].UserId.Should().Be("user1");
        list[0].CrtDate.Should().Be(time1);

        list[1].NoUrut.Should().Be(2);
        list[1].OldTeamLead.PpaId.Should().Be("D2");
        list[1].NewTeamLead.PpaId.Should().Be("D3");
        list[1].Alasan.Should().Be("Perubahan leader ke dokter 3");
        list[1].ChangeType.Should().Be("ASSIGN_LEADER");
        list[1].UserId.Should().Be("user2");
        list[1].CrtDate.Should().Be(time2);
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
        schedule.Durasi.Should().Be(durasi);
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
        var doctor = CreatePpa("D1", ProfesiType.Dokter);

        // Act
        var result = ScheduleOpModel.CreateFromOrder(order, "user1", kamar, doctor, DateTime.Today);

        // Assert
        result.OrderOp.OrderOpId.Should().Be(order.OrderOpId);
        result.TeamLead.PpaId.Should().Be("D1");
        result.ListPpa.Should().ContainSingle(x => x.Ppa.PpaId == "D1" && x.NoUrut == 1);
        result.TurnoverBuffer.Should().Be(30);
        result.AlasanBatal.Should().BeEmpty();
        result.ListHistory.Should().BeEmpty();
    }

    [Fact]
    public void UT10_GivenNullOrEmptyDoctor_WhenCreateFromOrder_ThenThrowArgumentException()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;

        // Act & Assert - null doctor
        FluentActions.Invoking(() => ScheduleOpModel.CreateFromOrder(order, "u1", kamar, null!, DateTime.Today))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Dokter Operator Utama wajib ditentukan*");

        // Act & Assert - default doctor
        FluentActions.Invoking(() => ScheduleOpModel.CreateFromOrder(order, "u1", kamar, PpaType.Default, DateTime.Today))
            .Should().Throw<ArgumentException>()
            .WithMessage("*Dokter Operator Utama wajib ditentukan*");
    }

    [Fact]
    public void UT11_GivenNonDoctorPpa_WhenCreateFromOrder_ThenThrowArgumentException()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default;
        var nurse = CreatePpa("N1", ProfesiType.Perawat);

        // Act & Assert
        FluentActions.Invoking(() => ScheduleOpModel.CreateFromOrder(order, "u1", kamar, nurse, DateTime.Today))
            .Should().Throw<ArgumentException>()
            .WithMessage("*PPA yang ditugaskan sebagai Operator Utama harus berprofesi Dokter*");
    }

    // ---------------------------------------------------------
    // UT12 - Reschedule In-Place & History Recording
    // ---------------------------------------------------------
    [Fact]
    public void UT12_GivenActiveSchedule_WhenReschedule_ThenPreservesScheduleOpIdAndRecordsHistory()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamarAwal = KamarType.Default with { KamarId = "K-01", KamarName = "Kamar Operasi 1" };
        var dokter = CreatePpa("DOC-01", ProfesiType.Dokter);
        var initialTglOp = new DateTime(2026, 10, 25, 8, 0, 0);
        var sut = ScheduleOpModel.CreateFromOrder(order, "u1", kamarAwal, dokter, initialTglOp);
        var originalId = sut.ScheduleOpId;

        var newTglOp = new DateTime(2026, 10, 25, 13, 0, 0);
        var newKamar = new KamarReff("K-02", "Kamar Operasi 2");
        var newDurasi = 120;
        var newTurnoverBuffer = 45;
        var newLead = new PpaReff("DOC-02", "Dr. DOC-02");
        var occurredAt = new DateTime(2026, 10, 20, 10, 0, 0);

        // Act
        sut.Reschedule(
            newTglOp: newTglOp,
            newKamar: newKamar,
            newDurasi: newDurasi,
            newTurnoverBuffer: newTurnoverBuffer,
            newTeamLead: newLead,
            alasanPerubahan: "Dislokasi jadwal akibat Cito",
            isOverride: true,
            overrideReason: "Disetujui komite bedah",
            userId: "koordinator1",
            occurredAt: occurredAt);

        // Assert - Identity preserved (In-Place Mutation, no void-and-clone)
        sut.ScheduleOpId.Should().Be(originalId);
        sut.TglOp.Should().Be(newTglOp);
        sut.KamarOp.Should().Be(newKamar);
        sut.Durasi.Should().Be(newDurasi);
        sut.TurnoverBuffer.Should().Be(newTurnoverBuffer);
        sut.TeamLead.Should().Be(newLead);
        sut.AuditTrail.Modified.UserId.Should().Be("koordinator1");
        sut.AuditTrail.Modified.Timestamp.Should().Be(occurredAt);

        // Assert - History snapshot recorded
        sut.ListHistory.Should().HaveCount(1);
        var hist = sut.ListHistory.First();
        hist.NoUrut.Should().Be(1);
        hist.OldTglOp.Should().Be(initialTglOp);
        hist.NewTglOp.Should().Be(newTglOp);
        hist.OldDurasi.Should().Be(order.EstimasiDurasiInMinutes);
        hist.NewDurasi.Should().Be(newDurasi);
        hist.OldKamar.KamarId.Should().Be("K-01");
        hist.NewKamar.KamarId.Should().Be("K-02");
        hist.OldTeamLead.PpaId.Should().Be("DOC-01");
        hist.NewTeamLead.PpaId.Should().Be("DOC-02");
        hist.Alasan.Should().Be("Dislokasi jadwal akibat Cito");
        hist.IsOverride.Should().BeTrue();
        hist.OverrideReason.Should().Be("Disetujui komite bedah");
        hist.UserId.Should().Be("koordinator1");
        hist.CrtDate.Should().Be(occurredAt);
        hist.ChangeType.Should().Be("RESCHEDULE");

        // Assert - Team lead updated in ListPpa at NoUrut = 1
        sut.ListPpa.Should().ContainSingle(x => x.NoUrut == 1 && x.Ppa.PpaId == "DOC-02");
    }

    [Fact]
    public void UT13_GivenMultipleReschedules_ThenHistorySequencedIncrementally()
    {
        // Arrange
        var order = OrderOpModel.Default;
        var kamar = KamarType.Default with { KamarId = "K-01", KamarName = "Kamar 1" };
        var dokter = CreatePpa("DOC-01", ProfesiType.Dokter);
        var sut = ScheduleOpModel.CreateFromOrder(order, "u1", kamar, dokter, DateTime.Today);

        // Act 1
        sut.Reschedule(
            DateTime.Today.AddHours(9),
            kamar.ToReff(),
            90,
            30,
            dokter.ToReff(),
            "Pergeseran ke-1",
            false,
            "",
            "u1",
            DateTime.Now);

        // Act 2
        sut.Reschedule(
            DateTime.Today.AddHours(14),
            new KamarReff("K-02", "Kamar 2"),
            100,
            40,
            dokter.ToReff(),
            "Pergeseran ke-2",
            false,
            "",
            "u2",
            DateTime.Now.AddMinutes(30));

        // Assert
        sut.ListHistory.Should().HaveCount(2);
        var list = sut.ListHistory.ToList();
        list[0].NoUrut.Should().Be(1);
        list[0].Alasan.Should().Be("Pergeseran ke-1");
        list[1].NoUrut.Should().Be(2);
        list[1].Alasan.Should().Be("Pergeseran ke-2");
    }

    [Fact]
    public void UT14_GivenEmptyAlasanPerubahan_WhenReschedule_ThenThrowArgumentException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var doctor = CreatePpa("DOC-01", ProfesiType.Dokter);

        // Act & Assert - null reason
        FluentActions.Invoking(() => schedule.Reschedule(
                DateTime.Today,
                KamarReff.Default,
                60,
                30,
                doctor.ToReff(),
                null!,
                false,
                "",
                "u1",
                DateTime.Now))
            .Should().Throw<ArgumentException>();

        // Act & Assert - empty reason
        FluentActions.Invoking(() => schedule.Reschedule(
                DateTime.Today,
                KamarReff.Default,
                60,
                30,
                doctor.ToReff(),
                "   ",
                false,
                "",
                "u1",
                DateTime.Now))
            .Should().Throw<ArgumentException>();
    }

    // ---------------------------------------------------------
    // UT15 - CancelSchedule With Required Alasan
    // ---------------------------------------------------------
    [Fact]
    public void UT15_GivenValidAlasanBatal_WhenCancelSchedule_ThenAlasanBatalAndAuditRecorded()
    {
        // Arrange
        var schedule = CreateEmptySchedule();
        var cancelledAt = new DateTime(2026, 10, 20, 11, 30, 0);

        // Act
        schedule.CancelSchedule("Kondisi pasien tidak stabil (Pre-Op Unfit)", "dr_anestesi", cancelledAt);

        // Assert
        schedule.AlasanBatal.Should().Be("Kondisi pasien tidak stabil (Pre-Op Unfit)");
        schedule.AuditTrail.Voided.UserId.Should().Be("dr_anestesi");
        schedule.AuditTrail.Voided.Timestamp.Should().Be(cancelledAt);
    }

    [Fact]
    public void UT16_GivenEmptyAlasanBatal_WhenCancelSchedule_ThenThrowArgumentException()
    {
        // Arrange
        var schedule = CreateEmptySchedule();

        // Act & Assert - null
        FluentActions.Invoking(() => schedule.CancelSchedule(null!, "u1", DateTime.Now))
            .Should().Throw<ArgumentException>();

        // Act & Assert - whitespace
        FluentActions.Invoking(() => schedule.CancelSchedule("   ", "u1", DateTime.Now))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UT17_GivenConstructorParameters_WhenInstantiated_ThenPropertiesAssignedCorrectly()
    {
        // Arrange
        var hist = new ScheduleOpHistType(
            1,
            DateTime.Today,
            DateTime.Today.AddHours(2),
            60,
            90,
            KamarReff.Default,
            new KamarReff("K1", "Kamar 1"),
            PpaReff.Default,
            new PpaReff("D1", "Dokter 1"),
            "Revisi",
            false,
            "",
            "u1",
            DateTime.Now);

        // Act
        var sut = new ScheduleOpModel(
            "SCH-TEST-01",
            DateTime.Today,
            AuditTrailType.Default,
            OrderOpModel.Default.ToReff(),
            PasienModel.Default.ToReff(),
            UrgencyLevelEnum.Cito,
            90,
            DateTime.Today.AddHours(2),
            new KamarReff("K1", "Kamar 1"),
            RegModel.Default.ToReff(),
            new PpaReff("D1", "Dokter 1"),
            DateTime.Today,
            DateTime.Today.AddHours(3),
            OpCaseStateEnum.Scheduled,
            [],
            "Batal jadwal lama",
            45,
            [hist]);

        // Assert
        sut.ScheduleOpId.Should().Be("SCH-TEST-01");
        sut.AlasanBatal.Should().Be("Batal jadwal lama");
        sut.TurnoverBuffer.Should().Be(45);
        sut.ListHistory.Should().ContainSingle().Which.Should().Be(hist);
    }
}
