using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class PreOpClearanceModelTest
{
    private static readonly DateTime CreatedAt = new(2026, 11, 25, 8, 0, 0);
    private static readonly DateTime UpdatedAt = new(2026, 11, 25, 9, 0, 0);

    private static PreOpClearanceModel CreateModel()
        => PreOpClearanceModel.Create("SCHEDULE-1", "ORDER-1", "REG-1", 1,
            "PPA-VERIFIER", "PPA-DOKTER", "user-1", CreatedAt);

    private static PreOpChecklistItemType Item(
        ChecklistItemStatusEnum status = ChecklistItemStatusEnum.Verified)
        => new(PreOpChecklistItemType.PatientIdentity, status, "verified");

    [Fact]
    public void Create_sets_pending_state_and_audit()
    {
        var model = CreateModel();

        model.ClearanceId.Should().NotBeNullOrWhiteSpace();
        model.ClearanceId.Should().HaveLength(26);
        model.ScheduleOpId.Should().Be("SCHEDULE-1");
        model.OrderOpId.Should().Be("ORDER-1");
        model.RegId.Should().Be("REG-1");
        model.NoUrut.Should().Be(1);
        model.ClearanceStatus.Should().Be(PreOpClearanceStatusEnum.PendingClearance);
        model.CompletedAt.Should().Be(new DateTime(3000, 1, 1));
        model.ChecklistItems.Should().BeEmpty();
        model.AuditTrail.Created.UserId.Should().Be("user-1");
        model.AuditTrail.Created.Timestamp.Should().Be(CreatedAt);
    }

    [Fact]
    public void Create_rejects_invalid_no_urut()
    {
        var act = () => PreOpClearanceModel.Create("S", "O", "R", 0, "V", "D", "u", CreatedAt);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateChecklist_updates_items_when_pending()
    {
        var model = CreateModel();

        model.UpdateChecklist([Item(), new(PreOpChecklistItemType.BloodLogistics, ChecklistItemStatusEnum.Cleared)],
            "user-2", UpdatedAt);

        model.ChecklistItems.Should().HaveCount(2);
        model.AuditTrail.Modified.UserId.Should().Be("user-2");
        model.AuditTrail.Modified.Timestamp.Should().Be(UpdatedAt);
    }

    [Theory]
    [InlineData(PreOpClearanceStatusEnum.Cleared)]
    [InlineData(PreOpClearanceStatusEnum.NotCleared)]
    [InlineData(PreOpClearanceStatusEnum.EmergencyOverridden)]
    public void UpdateChecklist_rejects_final_state(PreOpClearanceStatusEnum status)
    {
        var model = CreateModel();
        model.UpdateChecklist([Item()], "user-1", CreatedAt);
        if (status == PreOpClearanceStatusEnum.Cleared)
            model.Approve("user-1", UpdatedAt);
        else if (status == PreOpClearanceStatusEnum.NotCleared)
            model.Reject("Tidak layak", "user-1", UpdatedAt);
        else
            model.EmergencyOverride("DOK-1", "Kondisi darurat membutuhkan tindakan segera", "user-1",
                UpdatedAt, UrgencyLevelEnum.Cito, ["DOK-1"]);

        var act = () => model.UpdateChecklist([Item()], "user-2", UpdatedAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(ChecklistItemStatusEnum.Verified)]
    [InlineData(ChecklistItemStatusEnum.Cleared)]
    public void Approve_accepts_complete_checklist(ChecklistItemStatusEnum status)
    {
        var model = CreateModel();
        model.UpdateChecklist([Item(status)], "user-1", CreatedAt);

        model.Approve("user-2", UpdatedAt);

        model.ClearanceStatus.Should().Be(PreOpClearanceStatusEnum.Cleared);
        model.CompletedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void Approve_rejects_empty_checklist()
    {
        var act = () => CreateModel().Approve("user-1", UpdatedAt);

        act.Should().Throw<PreOpChecklistIncompleteException>();
    }

    [Theory]
    [InlineData(ChecklistItemStatusEnum.Pending)]
    [InlineData(ChecklistItemStatusEnum.Failed)]
    public void Approve_rejects_pending_or_failed_item(ChecklistItemStatusEnum status)
    {
        var model = CreateModel();
        model.UpdateChecklist([Item(status)], "user-1", CreatedAt);

        var act = () => model.Approve("user-1", UpdatedAt);

        act.Should().Throw<PreOpChecklistIncompleteException>();
    }

    [Fact]
    public void Reject_requires_reason_and_sets_not_cleared()
    {
        var model = CreateModel();
        var act = () => model.Reject("  ", "user-1", UpdatedAt);
        act.Should().Throw<ArgumentException>();

        model.Reject("Hasil pemeriksaan belum memenuhi syarat", "user-1", UpdatedAt);
        model.ClearanceStatus.Should().Be(PreOpClearanceStatusEnum.NotCleared);
        model.AlasanNotCleared.Should().Be("Hasil pemeriksaan belum memenuhi syarat");
        model.CompletedAt.Should().Be(UpdatedAt);
    }

    [Fact]
    public void EmergencyOverride_accepts_cito_authorized_doctor_and_reason()
    {
        var model = CreateModel();

        model.EmergencyOverride("DOK-1", "Pasien dalam kondisi gawat darurat", "user-1", UpdatedAt,
            UrgencyLevelEnum.Cito, ["DOK-1"]);

        model.ClearanceStatus.Should().Be(PreOpClearanceStatusEnum.EmergencyOverridden);
        model.DokterOverrideId.Should().Be("DOK-1");
        model.AlasanOverride.Should().Be("Pasien dalam kondisi gawat darurat");
    }

    [Fact]
    public void EmergencyOverride_rejects_elective_case()
    {
        var act = () => CreateModel().EmergencyOverride("DOK-1", "Alasan medis darurat", "user-1", UpdatedAt,
            UrgencyLevelEnum.Elective, ["DOK-1"]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EmergencyOverride_rejects_unauthorized_doctor()
    {
        var act = () => CreateModel().EmergencyOverride("DOK-2", "Pasien dalam kondisi gawat darurat", "user-1",
            UpdatedAt, UrgencyLevelEnum.Cito, ["DOK-1"]);

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void EmergencyOverride_rejects_short_reason()
    {
        var act = () => CreateModel().EmergencyOverride("DOK-1", "singkat", "user-1", UpdatedAt,
            UrgencyLevelEnum.Cito, ["DOK-1"]);

        act.Should().Throw<ArgumentException>();
    }
}
