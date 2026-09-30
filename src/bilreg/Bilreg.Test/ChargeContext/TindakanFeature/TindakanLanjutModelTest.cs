using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using FluentAssertions;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanLanjutModelTests
{
    private static readonly string TestUserId = "TEST_USER";
    private static readonly DateTime TestDateTime = new(2023, 1, 1);

    private static TujuanLanjutType KontrolTujuan() => new(
        "Kontrol", new LayananReff("LYN1", "Poli Anak"), true);

    private static TujuanLanjutService FiveTypeService() => new(
    [
        new TujuanLanjutType("Kontrol", new LayananReff("L1", "Poli"), true),
        new TujuanLanjutType("Penunjang", new LayananReff("L2", "Lab"), true),
        new TujuanLanjutType("Transfer", new LayananReff("L3", "RS Rujukan"), true),
        new TujuanLanjutType("Operasi", new LayananReff("L4", "OK"), true),
        new TujuanLanjutType("RawatInap", new LayananReff("L5", "Bangsal"), true),
    ]);

    private static TindakanLanjutModel NewProposed() =>
        TindakanLanjutModel.Create(RegModel.Default, KontrolTujuan(), [], TestUserId, TestDateTime);

    [Fact]
    public void UT1_GivenVisitAndOrderType_WhenCreate_ThenProposedBoundToVisit()
    {
        var result = NewProposed();

        result.OrderState.Should().Be(TindakanLanjutStateEnum.Proposed);
        result.Reg.RegId.Should().Be(RegModel.Default.RegId);
        result.OrderType.Should().Be("Kontrol");
        result.Layanan.LayananId.Should().Be("LYN1");
        result.TindakanLanjutId.Should().NotBe("-");
        result.IsOutstanding.Should().BeTrue();
    }

    [Fact]
    public void UT2_GivenProposed_WhenSend_ThenSent()
    {
        var order = NewProposed();

        order.Send(TestUserId, TestDateTime);

        order.OrderState.Should().Be(TindakanLanjutStateEnum.Sent);
        order.SentDate.Should().Be(TestDateTime);
        order.IsOutstanding.Should().BeTrue();
    }

    [Fact]
    public void UT3_GivenSent_WhenSendAgain_ThenRejected()
    {
        var order = NewProposed();
        order.Send(TestUserId, TestDateTime);

        Assert.Throws<InvalidOperationException>(() => order.Send(TestUserId, TestDateTime));
    }

    [Fact]
    public void UT4_GivenSent_WhenConfirm_ThenReceivedFirstTimeOnly()
    {
        var order = NewProposed();
        order.Send(TestUserId, TestDateTime);

        var first = order.ConfirmReceived("PETUGAS_RS", TestUserId, TestDateTime);

        first.Should().BeTrue();
        order.OrderState.Should().Be(TindakanLanjutStateEnum.Received);
        order.ReceivedBy.Should().Be("PETUGAS_RS");
        order.IsOutstanding.Should().BeFalse();

        var versionAfterFirst = order.RowVersion;
        var second = order.ConfirmReceived("PETUGAS_RS", TestUserId, TestDateTime);

        second.Should().BeFalse();
        order.OrderState.Should().Be(TindakanLanjutStateEnum.Received);
        order.RowVersion.Should().Be(versionAfterFirst);
    }

    [Fact]
    public void UT5_GivenProposed_WhenConfirm_ThenRejected()
    {
        var order = NewProposed();

        Assert.Throws<InvalidOperationException>(
            () => order.ConfirmReceived("PETUGAS_RS", TestUserId, TestDateTime));
    }

    [Fact]
    public void UT6_GivenProposedOrSent_WhenCancel_ThenCancelled()
    {
        var fromProposed = NewProposed();
        fromProposed.Cancel("salah input", TestUserId, TestDateTime);
        fromProposed.OrderState.Should().Be(TindakanLanjutStateEnum.Cancelled);
        fromProposed.CancelReason.Should().Be("salah input");

        var fromSent = NewProposed();
        fromSent.Send(TestUserId, TestDateTime);
        fromSent.Cancel("batal kirim", TestUserId, TestDateTime);
        fromSent.OrderState.Should().Be(TindakanLanjutStateEnum.Cancelled);
    }

    [Fact]
    public void UT7_GivenReceived_WhenCancel_ThenRejected()
    {
        var order = NewProposed();
        order.Send(TestUserId, TestDateTime);
        order.ConfirmReceived("PETUGAS_RS", TestUserId, TestDateTime);

        Assert.Throws<InvalidOperationException>(
            () => order.Cancel("terlambat", TestUserId, TestDateTime));
    }

    [Fact]
    public void UT8_GivenUnknownOrderType_WhenResolve_ThenRejected()
    {
        var svc = FiveTypeService();

        Assert.Throws<ArgumentException>(() => svc.Resolve("TidakAda"));
        Assert.Throws<ArgumentException>(() => svc.Resolve(""));
    }

    [Fact]
    public void UT9_GivenMappingData_WhenResolve_ThenLayananFromData()
    {
        var svc = FiveTypeService();

        svc.ResolveLayanan("Penunjang").Should().Be(new LayananReff("L2", "Lab"));
        svc.ResolveLayanan("rawatinap").Should().Be(new LayananReff("L5", "Bangsal"));
        svc.Resolve("Kontrol").Layanan.LayananId.Should().Be("L1");
    }

    [Fact]
    public void UT10_GivenInactiveMapping_WhenCreate_ThenRejected()
    {
        var inactive = new TujuanLanjutType("Kontrol", new LayananReff("L1", "Poli"), false);

        Assert.Throws<ArgumentException>(() =>
            TindakanLanjutModel.Create(RegModel.Default, inactive, [], TestUserId, TestDateTime));
    }
}
