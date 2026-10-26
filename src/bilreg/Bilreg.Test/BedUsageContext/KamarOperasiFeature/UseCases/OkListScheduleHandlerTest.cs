using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkListScheduleHandlerTest
{
    private readonly Mock<IScheduleOpRepo> _scheduleRepoMock = new();

    private OkListScheduleHandler CreateSut()
    {
        return new OkListScheduleHandler(_scheduleRepoMock.Object);
    }

    [Fact]
    public async Task Handle_WhenNoSchedules_ShouldReturnEmpty()
    {
        // Arrange
        _scheduleRepoMock.Setup(x => x.ListData(new DateTime(2026, 10, 20)))
            .Returns([]);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new OkListScheduleQuery("2026-10-20"), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSchedulesExist_ShouldMapAllFieldsIncludingTurnoverBuffer()
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Tn. Budi", new DateOnly(1985, 5, 20), "L");
        var orderOp = new OrderOpReff("ORD-001", new DateTime(2026, 10, 16), "Apendektomi Laparoskopi");
        var teamLead = new PpaReff("DOC-01", "dr. Operator Utama, Sp.B");
        var kamar = new KamarReff("KMR-01", "OK 1");

        var scheduleView = new ScheduleOpView(
            ScheduleOpId: "SCH-001",
            Pasien: pasien,
            OrderOp: orderOp,
            Urgency: UrgencyLevelEnum.Cito,
            TglOp: new DateTime(2026, 10, 20, 8, 30, 0),
            Durasi: 90,
            TeamLead: teamLead,
            Kamar: kamar,
            Status: OpCaseStateEnum.Scheduled,
            IsVoid: false,
            TurnoverBuffer: 45,
            ProfesiId: "DOKTER",
            ProfesiName: "Dokter Spesialis"
        );

        _scheduleRepoMock.Setup(x => x.ListData(new DateTime(2026, 10, 20)))
            .Returns([scheduleView]);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new OkListScheduleQuery("2026-10-20"), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        var item = result[0];
        item.PasienId.Should().Be("PAS-001");
        item.PasienName.Should().Be("Tn. Budi");
        item.TglLahir.Should().Be("1985-05-20");
        item.OrderOpId.Should().Be("ORD-001");
        item.ScheduleOpId.Should().Be("SCH-001");
        item.NamaOperasi.Should().Be("Apendektomi Laparoskopi");
        item.Urgency.Should().Be("Cito");
        item.Durasi.Should().Be(90);
        item.ScheduledTime.Should().Be("08:30");
        item.StatusOp.Should().Be("Scheduled");
        item.DokterId.Should().Be("DOC-01");
        item.DokterName.Should().Be("dr. Operator Utama, Sp.B");
        item.KamarId.Should().Be("KMR-01");
        item.KamarName.Should().Be("OK 1");
        item.TurnoverBuffer.Should().Be(45);
    }

    [Fact]
    public async Task Handle_WhenVoidSchedulesExist_ShouldFilterThemOut()
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Tn. Budi", new DateOnly(1985, 5, 20), "L");
        var orderOp = new OrderOpReff("ORD-001", new DateTime(2026, 10, 16), "Apendektomi");
        var teamLead = new PpaReff("DOC-01", "dr. Operator");
        var kamar = new KamarReff("KMR-01", "OK 1");

        var activeView = new ScheduleOpView("SCH-001", pasien, orderOp, UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0), 60, teamLead, kamar, OpCaseStateEnum.Scheduled,
            IsVoid: false, TurnoverBuffer: 30);

        var voidView = new ScheduleOpView("SCH-002", pasien, orderOp, UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 10, 0, 0), 60, teamLead, kamar, OpCaseStateEnum.Scheduled,
            IsVoid: true, TurnoverBuffer: 30);

        _scheduleRepoMock.Setup(x => x.ListData(new DateTime(2026, 10, 20)))
            .Returns([activeView, voidView]);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new OkListScheduleQuery("2026-10-20"), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].ScheduleOpId.Should().Be("SCH-001");
    }

    [Theory]
    [InlineData(OpCaseStateEnum.Scheduled, "Scheduled")]
    [InlineData(OpCaseStateEnum.OpStarted, "In-Progress")]
    [InlineData(OpCaseStateEnum.RecoveryStarted, "In-Recovery")]
    [InlineData(OpCaseStateEnum.Discharged, "Others")]
    public async Task Handle_TranslateState_ShouldTranslateStateCorrectly(OpCaseStateEnum state, string expectedStatusOp)
    {
        // Arrange
        var pasien = new PasienReff("PAS-001", "Tn. Budi", new DateOnly(1985, 5, 20), "L");
        var orderOp = new OrderOpReff("ORD-001", new DateTime(2026, 10, 16), "Apendektomi");
        var teamLead = new PpaReff("DOC-01", "dr. Operator");
        var kamar = new KamarReff("KMR-01", "OK 1");

        var view = new ScheduleOpView("SCH-001", pasien, orderOp, UrgencyLevelEnum.Elective,
            new DateTime(2026, 10, 20, 8, 0, 0), 60, teamLead, kamar, state,
            IsVoid: false, TurnoverBuffer: 30);

        _scheduleRepoMock.Setup(x => x.ListData(new DateTime(2026, 10, 20)))
            .Returns([view]);
        var sut = CreateSut();

        // Act
        var result = (await sut.Handle(new OkListScheduleQuery("2026-10-20"), CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].StatusOp.Should().Be(expectedStatusOp);
    }
}
