using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.InpatientOrderFeature;

public class InpatientOrderModelTest
{
    private static PasienReff SamplePasien() =>
        new("P001", "Pasien Test", new DateOnly(1990, 1, 1), "L");

    private static PpaReff SampleDokter() => new("D001", "Dr. Test Sp.PD");

    private static KelasReff SampleKelas() => new("K1", "Kelas 1");

    private static BangsalReff SampleBangsal() => new("B1", "Bangsal Teratai");

    private static InpatientOrderModel CreateRequested(OrderSourceEnum source = OrderSourceEnum.EmrRawatJalan) =>
        InpatientOrderModel.Create(
            source,
            SamplePasien(),
            SampleDokter(),
            new DateTime(2026, 11, 10),
            "Perlu rawat inap lanjutan",
            "user_dokter",
            externalReferenceId: "EXT-1001",
            externalSystemSource: "EMR_RJ");

    private static InpatientOrderModel CreateBooking() =>
        InpatientOrderModel.CreateBooking(
            SamplePasien(),
            new DateTime(2026, 11, 15),
            SampleKelas(),
            SampleBangsal(),
            "user_admisi",
            SampleDokter(),
            "Pesanan kamar elektif");

    #region CREATION

    [Fact]
    public void DT_IPO_01_GivenCreate_WhenEmrRawatJalan_ThenStatusIsRequestedAndIdPrefixedWithOPN()
    {
        var order = CreateRequested();

        order.OrderStatus.Should().Be(InpatientOrderStatusEnum.Requested);
        order.InpatientOrderId.Should().StartWith("OPN");
        order.InpatientOrderId.Length.Should().Be(12);
        order.FulfilledRegId.Should().Be("-");
        order.OrderSource.Should().Be(OrderSourceEnum.EmrRawatJalan);
        order.ExternalReferenceId.Should().Be("EXT-1001");
        order.ExternalSystemSource.Should().Be("EMR_RJ");
        order.AuditTrail.Created.UserId.Should().Be("user_dokter");
    }

    [Fact]
    public void DT_IPO_02_GivenCreate_WhenBooking_ThenHasKelasAndBangsal()
    {
        var order = CreateBooking();

        order.OrderSource.Should().Be(OrderSourceEnum.AdmisiBooking);
        order.OrderStatus.Should().Be(InpatientOrderStatusEnum.Requested);
        order.KelasRawat.KelasId.Should().Be("K1");
        order.Bangsal.BangsalId.Should().Be("B1");
        order.Dokter.PpaId.Should().Be("D001");
    }

    [Fact]
    public void DT_IPO_03_GivenCreate_WhenMissingRequiredPasienOrUserId_ThenThrows()
    {
        Action nullPasien = () => InpatientOrderModel.Create(
            OrderSourceEnum.EmrRawatJalan,
            null!,
            SampleDokter(),
            new DateTime(2026, 11, 10),
            "Notes",
            "user1");

        Action emptyUserId = () => InpatientOrderModel.Create(
            OrderSourceEnum.EmrRawatJalan,
            SamplePasien(),
            SampleDokter(),
            new DateTime(2026, 11, 10),
            "Notes",
            "");

        nullPasien.Should().Throw<ArgumentNullException>();
        emptyUserId.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DT_IPO_04_GivenDefaultAndKey_WhenAccessed_ThenConsistent()
    {
        var defaultModel = InpatientOrderModel.Default;
        defaultModel.InpatientOrderId.Should().Be("-");
        defaultModel.OrderStatus.Should().Be(InpatientOrderStatusEnum.Requested);
        defaultModel.FulfilledRegId.Should().Be("-");

        var key = InpatientOrderModel.Key("OPN26B040001");
        key.InpatientOrderId.Should().Be("OPN26B040001");
    }

    #endregion

    #region MAINTAIN

    [Fact]
    public void DT_IPO_05_GivenRequested_WhenMaintain_ThenStatusBecomesMaintained()
    {
        var order = CreateRequested();
        var maintainedAt = new DateTime(2026, 11, 5, 8, 30, 0);

        var maintained = order.Maintain(
            new DateTime(2026, 11, 12),
            SampleKelas(),
            SampleBangsal(),
            "user_admisi",
            maintainedAt);

        maintained.OrderStatus.Should().Be(InpatientOrderStatusEnum.Maintained);
        maintained.KelasRawat.KelasId.Should().Be("K1");
        maintained.Bangsal.BangsalId.Should().Be("B1");
        maintained.PlannedDate.Should().Be(new DateTime(2026, 11, 12));
        maintained.AuditTrail.Modified.UserId.Should().Be("user_admisi");
        maintained.AuditTrail.Modified.Timestamp.Should().Be(maintainedAt);
    }

    [Fact]
    public void DT_IPO_06_GivenMaintained_WhenMaintainAgain_ThenUpdatesRoomAndKeepsMaintained()
    {
        var maintained = CreateRequested()
            .Maintain(SampleKelas(), SampleBangsal(), "user_admisi");

        var updated = maintained.Maintain(
            new KelasReff("K2", "Kelas 2"),
            new BangsalReff("B2", "Bangsal Mawar"),
            "user_admisi_2");

        updated.OrderStatus.Should().Be(InpatientOrderStatusEnum.Maintained);
        updated.KelasRawat.KelasId.Should().Be("K2");
        updated.Bangsal.BangsalId.Should().Be("B2");
        updated.AuditTrail.Modified.UserId.Should().Be("user_admisi_2");
    }

    [Fact]
    public void DT_IPO_07_GivenCancelledOrFulfilled_WhenMaintain_ThenThrows()
    {
        var cancelled = CreateRequested().Cancel("user1");
        var fulfilled = CreateRequested().Fulfill("RG00000001", "user1");

        Action maintainCancelled = () => cancelled.Maintain(SampleKelas(), SampleBangsal(), "user2");
        Action maintainFulfilled = () => fulfilled.Maintain(SampleKelas(), SampleBangsal(), "user2");

        maintainCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus berstatus Requested atau Maintained*");
        maintainFulfilled.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus berstatus Requested atau Maintained*");
    }

    [Fact]
    public void DT_IPO_08_GivenMaintain_WhenNullRoomOrEmptyUser_ThenThrows()
    {
        var order = CreateRequested();

        Action nullKelas = () => order.Maintain(null!, SampleBangsal(), "user1");
        Action nullBangsal = () => order.Maintain(SampleKelas(), null!, "user1");
        Action emptyUser = () => order.Maintain(SampleKelas(), SampleBangsal(), "");

        nullKelas.Should().Throw<ArgumentNullException>();
        nullBangsal.Should().Throw<ArgumentNullException>();
        emptyUser.Should().Throw<ArgumentException>();
    }

    #endregion

    #region FULFILL

    [Fact]
    public void DT_IPO_09_GivenRequested_WhenFulfill_ThenStatusFulfilledWithRegId()
    {
        var order = CreateRequested();
        var fulfilledAt = new DateTime(2026, 11, 4, 10, 0, 0);

        var fulfilled = order.Fulfill("RG26B040001", "admisi_user", fulfilledAt);

        fulfilled.OrderStatus.Should().Be(InpatientOrderStatusEnum.Fulfilled);
        fulfilled.FulfilledRegId.Should().Be("RG26B040001");
        fulfilled.AuditTrail.Modified.UserId.Should().Be("admisi_user");
        fulfilled.AuditTrail.Modified.Timestamp.Should().Be(fulfilledAt);
    }

    [Fact]
    public void DT_IPO_10_GivenMaintained_WhenFulfill_ThenStatusFulfilledWithRegId()
    {
        var maintained = CreateRequested()
            .Maintain(SampleKelas(), SampleBangsal(), "user1");

        var fulfilled = maintained.Fulfill("RG26B040002", "admisi_user");

        fulfilled.OrderStatus.Should().Be(InpatientOrderStatusEnum.Fulfilled);
        fulfilled.FulfilledRegId.Should().Be("RG26B040002");
    }

    [Fact]
    public void DT_IPO_11_GivenCancelled_WhenFulfill_ThenThrows()
    {
        var cancelled = CreateRequested().Cancel("user1");

        Action fulfillCancelled = () => cancelled.Fulfill("RG26B040001", "user1");

        fulfillCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void DT_IPO_12_GivenAlreadyFulfilled_WhenFulfillAgain_ThenThrows()
    {
        var fulfilled = CreateRequested().Fulfill("RG26B040001", "user1");

        Action fulfillAgain = () => fulfilled.Fulfill("RG26B040002", "user1");

        fulfillAgain.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dipenuhi*");
    }

    [Fact]
    public void DT_IPO_13_GivenFulfill_WhenEmptyRegIdOrUserId_ThenThrows()
    {
        var order = CreateRequested();

        Action emptyReg = () => order.Fulfill("", "user1");
        Action emptyUser = () => order.Fulfill("RG001", "");

        emptyReg.Should().Throw<ArgumentException>();
        emptyUser.Should().Throw<ArgumentException>();
    }

    #endregion

    #region CANCEL

    [Fact]
    public void DT_IPO_14_GivenRequestedOrMaintained_WhenCancel_ThenStatusCancelled()
    {
        var cancelledFromReq = CreateRequested().Cancel("user1");
        cancelledFromReq.OrderStatus.Should().Be(InpatientOrderStatusEnum.Cancelled);
        cancelledFromReq.AuditTrail.Voided.UserId.Should().Be("user1");

        var cancelledFromMtn = CreateRequested()
            .Maintain(SampleKelas(), SampleBangsal(), "user1")
            .Cancel("user2");
        cancelledFromMtn.OrderStatus.Should().Be(InpatientOrderStatusEnum.Cancelled);
        cancelledFromMtn.AuditTrail.Voided.UserId.Should().Be("user2");
    }

    [Fact]
    public void DT_IPO_15_GivenFulfilled_WhenCancel_ThenThrows()
    {
        var fulfilled = CreateRequested().Fulfill("RG26B040001", "user1");

        Action act = () => fulfilled.Cancel("user1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dipenuhi*");
    }

    [Fact]
    public void DT_IPO_16_GivenAlreadyCancelled_WhenCancelAgain_ThenThrows()
    {
        var cancelled = CreateRequested().Cancel("user1");

        Action act = () => cancelled.Cancel("user2");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");
    }

    #endregion

    #region RESTORE

    [Fact]
    public void DT_IPO_17_GivenFulfilledWithRoomAllocated_WhenRestore_ThenReturnsToMaintainedAndClearsFulfilledRegId()
    {
        var maintained = CreateRequested()
            .Maintain(SampleKelas(), SampleBangsal(), "user1");
        var fulfilled = maintained.Fulfill("RG26B040001", "user1");
        var restoreTime = new DateTime(2026, 11, 4, 11, 0, 0);

        var restored = fulfilled.Restore("RG26B040001", "rollback_user", restoreTime);

        restored.OrderStatus.Should().Be(InpatientOrderStatusEnum.Maintained);
        restored.FulfilledRegId.Should().Be("-");
        restored.AuditTrail.Modified.UserId.Should().Be("rollback_user");
        restored.AuditTrail.Modified.Timestamp.Should().Be(restoreTime);
    }

    [Fact]
    public void DT_IPO_18_GivenFulfilledWithoutRoomAllocated_WhenRestore_ThenReturnsToRequestedAndClearsFulfilledRegId()
    {
        var order = CreateRequested();
        var fulfilled = order.Fulfill("RG26B040001", "user1");

        var restored = fulfilled.Restore("RG26B040001", "rollback_user");

        restored.OrderStatus.Should().Be(InpatientOrderStatusEnum.Requested);
        restored.FulfilledRegId.Should().Be("-");
        restored.AuditTrail.Modified.UserId.Should().Be("rollback_user");
    }

    [Fact]
    public void DT_IPO_19_GivenFulfilled_WhenRestoreWithExplicitTargetStatus_ThenRestoresToTargetStatus()
    {
        var maintained = CreateRequested()
            .Maintain(SampleKelas(), SampleBangsal(), "user1");
        var fulfilled = maintained.Fulfill("RG26B040001", "user1");

        var restored = fulfilled.Restore(
            "RG26B040001",
            "rollback_user",
            targetStatus: InpatientOrderStatusEnum.Requested);

        restored.OrderStatus.Should().Be(InpatientOrderStatusEnum.Requested);
        restored.FulfilledRegId.Should().Be("-");
    }

    [Fact]
    public void DT_IPO_20_GivenMismatchedRegIdOrUnfulfilled_WhenRestore_ThenThrows()
    {
        var fulfilled = CreateRequested().Fulfill("RG26B040001", "user1");

        Action wrongRegId = () => fulfilled.Restore("RG26B999999", "user2");
        Action unfulfilled = () => CreateRequested().Restore("RG26B040001", "user2");

        wrongRegId.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Fulfilled oleh Reg*");
        unfulfilled.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Fulfilled oleh Reg*");
    }

    #endregion

    #region SET INSURANCE

    [Fact]
    public void DT_IPO_21_GivenActiveOrder_WhenSetInsurance_ThenUpdatesInsuranceAndAuditTrail()
    {
        var order = CreateRequested();
        var changedAt = new DateTime(2026, 11, 4, 12, 0, 0);

        var updated = order.SetInsurance(
            new TipeJaminanReff("TJ01", "BPJS Kesehatan"),
            "00012345678",
            "bpjs_officer",
            changedAt);

        updated.Insurance.TipeJaminan.TipeJaminanId.Should().Be("TJ01");
        updated.Insurance.TipeJaminan.TipeJaminanName.Should().Be("BPJS Kesehatan");
        updated.Insurance.JaminanReffId.Should().Be("00012345678");
        updated.AuditTrail.Modified.UserId.Should().Be("bpjs_officer");
        updated.AuditTrail.Modified.Timestamp.Should().Be(changedAt);
    }

    [Fact]
    public void DT_IPO_22_GivenFulfilledOrCancelled_WhenSetInsurance_ThenThrows()
    {
        var fulfilled = CreateRequested().Fulfill("RG001", "user1");
        var cancelled = CreateRequested().Cancel("user1");

        Action setFulfilled = () => fulfilled.SetInsurance(
            new TipeJaminanReff("TJ01", "BPJS"), "123", "user1");
        Action setCancelled = () => cancelled.SetInsurance(
            new TipeJaminanReff("TJ01", "BPJS"), "123", "user1");

        setFulfilled.Should().Throw<InvalidOperationException>()
            .WithMessage("*penjaminan tidak dapat diubah*");
        setCancelled.Should().Throw<InvalidOperationException>()
            .WithMessage("*penjaminan tidak dapat diubah*");
    }

    #endregion

    #region ADMISSION MODEL INTERACTION

    [Fact]
    public void DT_IPO_23_GivenAdmissionModel_WhenAdmitWithInpatientOrderId_ThenExposesInpatientOrderId()
    {
        var admission = AdmissionModel.Admit(
            SamplePasien(),
            new KelasDkType("KD1", "Kelas 1 Standar"),
            SampleBangsal(),
            inpatientOrderId: "OPN26B040001",
            auditUserId: "user_admisi");

        admission.InpatientOrderId.Should().Be("OPN26B040001");
        admission.OpnameRequestId.Should().Be("-");
        admission.ReservationId.Should().Be("-");
    }

    [Fact]
    public void DT_IPO_24_GivenAdmissionModel_WhenAdmitWithOpnameRequestIdFallback_ThenInpatientOrderIdMatchesOpnameRequestId()
    {
        var admission = AdmissionModel.Admit(
            SamplePasien(),
            new KelasDkType("KD1", "Kelas 1 Standar"),
            SampleBangsal(),
            opnameRequestId: "OPN267200001",
            reservationId: null,
            auditUserId: "user_admisi");

        admission.OpnameRequestId.Should().Be("OPN267200001");
        admission.InpatientOrderId.Should().Be("OPN267200001");
    }

    #endregion
}
