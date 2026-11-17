using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class ScheduleOpHistTypeTest
{
    [Fact]
    public void Construct_GivenValidValues_ShouldInitializeAllProperties()
    {
        // Arrange
        var oldDate = new DateTime(2026, 10, 20, 8, 0, 0);
        var newDate = new DateTime(2026, 10, 20, 10, 0, 0);
        var oldKamar = new KamarReff("K01", "OK 1");
        var newKamar = new KamarReff("K02", "OK 2");
        var oldPpa = new PpaReff("D01", "dr. Bedah");
        var newPpa = new PpaReff("D02", "dr. Subspesialis");
        var crtDate = new DateTime(2026, 10, 19, 14, 0, 0);

        // Act
        var sut = new ScheduleOpHistType(
            1,
            oldDate,
            newDate,
            120,
            150,
            oldKamar,
            newKamar,
            oldPpa,
            newPpa,
            "Pergeseran jadwal cito",
            true,
            "Disetujui komite medis",
            "USER01",
            crtDate);

        // Assert
        sut.NoUrut.Should().Be(1);
        sut.OldTglOp.Should().Be(oldDate);
        sut.NewTglOp.Should().Be(newDate);
        sut.OldDurasi.Should().Be(120);
        sut.NewDurasi.Should().Be(150);
        sut.OldKamar.Should().Be(oldKamar);
        sut.NewKamar.Should().Be(newKamar);
        sut.OldTeamLead.Should().Be(oldPpa);
        sut.NewTeamLead.Should().Be(newPpa);
        sut.Alasan.Should().Be("Pergeseran jadwal cito");
        sut.IsOverride.Should().BeTrue();
        sut.OverrideReason.Should().Be("Disetujui komite medis");
        sut.UserId.Should().Be("USER01");
        sut.CrtDate.Should().Be(crtDate);
        sut.ChangeType.Should().Be("RESCHEDULE");
    }

    [Fact]
    public void Construct_GivenExplicitChangeType_ShouldInitializeChangeType()
    {
        // Arrange & Act
        var sut = new ScheduleOpHistType(
            2,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 8, 0, 0),
            120,
            120,
            new KamarReff("K01", "OK 1"),
            new KamarReff("K01", "OK 1"),
            new PpaReff("D01", "dr. Bedah 1"),
            new PpaReff("D02", "dr. Bedah 2"),
            "Pergantian operator utama",
            false,
            "",
            "USER02",
            new DateTime(2026, 10, 19, 15, 0, 0),
            "ASSIGN_LEADER");

        // Assert
        sut.NoUrut.Should().Be(2);
        sut.ChangeType.Should().Be("ASSIGN_LEADER");
        sut.OldTeamLead.PpaId.Should().Be("D01");
        sut.NewTeamLead.PpaId.Should().Be("D02");
    }

    [Fact]
    public void ValueEquality_GivenSameValues_ShouldBeEqual()
    {
        // Arrange
        var item1 = new ScheduleOpHistType(
            1,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 10, 0, 0),
            120,
            150,
            new KamarReff("K01", "OK 1"),
            new KamarReff("K02", "OK 2"),
            new PpaReff("D01", "dr. Bedah"),
            new PpaReff("D02", "dr. Bedah"),
            "Reschedule",
            false,
            "",
            "USER01",
            new DateTime(2026, 10, 19, 14, 0, 0));

        var item2 = new ScheduleOpHistType(
            1,
            new DateTime(2026, 10, 20, 8, 0, 0),
            new DateTime(2026, 10, 20, 10, 0, 0),
            120,
            150,
            new KamarReff("K01", "OK 1"),
            new KamarReff("K02", "OK 2"),
            new PpaReff("D01", "dr. Bedah"),
            new PpaReff("D02", "dr. Bedah"),
            "Reschedule",
            false,
            "",
            "USER01",
            new DateTime(2026, 10, 19, 14, 0, 0));

        // Assert
        item1.Should().Be(item2);
        (item1 == item2).Should().BeTrue();
        item1.GetHashCode().Should().Be(item2.GetHashCode());

        var itemDifferentChangeType = item1 with { ChangeType = "ASSIGN_LEADER" };
        item1.Should().NotBe(itemDifferentChangeType);
        (item1 == itemDifferentChangeType).Should().BeFalse();
    }

    [Fact]
    public void Default_ShouldReturnExpectedSentinelValues()
    {
        // Act
        var sut = ScheduleOpHistType.Default;

        // Assert
        sut.NoUrut.Should().Be(0);
        sut.OldTglOp.Should().Be(new DateTime(3000, 1, 1));
        sut.NewTglOp.Should().Be(new DateTime(3000, 1, 1));
        sut.OldDurasi.Should().Be(0);
        sut.NewDurasi.Should().Be(0);
        sut.OldKamar.Should().Be(KamarReff.Default);
        sut.NewKamar.Should().Be(KamarReff.Default);
        sut.OldTeamLead.Should().Be(PpaReff.Default);
        sut.NewTeamLead.Should().Be(PpaReff.Default);
        sut.Alasan.Should().BeEmpty();
        sut.IsOverride.Should().BeFalse();
        sut.OverrideReason.Should().BeEmpty();
        sut.UserId.Should().BeEmpty();
        sut.CrtDate.Should().Be(new DateTime(3000, 1, 1));
        sut.ChangeType.Should().Be("RESCHEDULE");
    }

    [Fact]
    public void ScheduleCollisionException_ShouldConstructCorrectly()
    {
        // Act
        var ex1 = new ScheduleCollisionException();
        var ex2 = new ScheduleCollisionException("Kamar OK 1 telah terpakai.");
        var inner = new InvalidOperationException("Inner error");
        var ex3 = new ScheduleCollisionException("Collision detected", inner);

        // Assert
        ex1.Message.Should().NotBeNull();
        ex2.Message.Should().Be("Kamar OK 1 telah terpakai.");
        ex3.Message.Should().Be("Collision detected");
        ex3.InnerException.Should().Be(inner);
    }

    [Fact]
    public void ScheduleWarningException_ShouldContainWarningsList()
    {
        // Arrange
        var warnings = new List<string>
        {
            "Dokter Operator telah terjadwal di kamar lain.",
            "Jeda turnover buffer kurang dari 30 menit."
        };

        // Act
        var ex = new ScheduleWarningException(warnings);

        // Assert
        ex.Warnings.Should().BeEquivalentTo(warnings);
        ex.Message.Should().Contain("Dokter Operator");
        ex.Message.Should().Contain("turnover buffer");
    }

    [Fact]
    public void ScheduleWarningException_GivenCustomMessageAndWarnings_ShouldContainBoth()
    {
        // Arrange
        var warnings = new List<string> { "Peringatan 1" };

        // Act
        var ex = new ScheduleWarningException("Custom warning message", warnings);

        // Assert
        ex.Message.Should().Be("Custom warning message");
        ex.Warnings.Should().BeEquivalentTo(warnings);
    }
}
