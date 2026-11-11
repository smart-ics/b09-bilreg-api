using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using System.Data.SqlClient;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class CoordinatedCancellationRepoTest
{
    static CoordinatedCancellationRepoTest()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            IF COL_LENGTH('BILRG_AdmAdmission', 'InpatientOrderId') IS NULL
            BEGIN
                ALTER TABLE BILRG_AdmAdmission
                    ADD InpatientOrderId VARCHAR(12) NOT NULL
                        CONSTRAINT DF_BILRG_AdmAdmission_InpatientOrderId DEFAULT('-');
            END";
        cmd.ExecuteNonQuery();
    }

    private readonly CoordinatedCancellationRepo _sut = new(ConnStringHelper.GetTestEnv());
    private readonly InpatientOrderDal _orderDal = new(ConnStringHelper.GetTestEnv());

    [Fact]
    public void RestoreSource_WithInpatientOrderModel_RestoresOrderStatusToRequestedAndClearsFulfilledRegId()
    {
        using var trans = TransHelper.NewScope();
        var orderId = GenerateOrderId();
        var regId = GenerateRegId();
        var now = DateTime.UtcNow;

        var dto = CreateTestDto(orderId, InpatientOrderStatusEnum.Fulfilled, OrderSourceEnum.EmrRawatJalan) with
        {
            FulfilledRegId = regId
        };
        _orderDal.Insert(dto);

        var state = CreateState(regId, orderId, "InpatientOrderModel");

        var affected = _sut.RestoreSource(state, "canceller_user", now);

        affected.Should().Be(1);
        var restored = _orderDal.GetData(InpatientOrderModel.Key(orderId));
        restored.Should().NotBeNull();
        restored.OrderStatus.Should().Be((int)InpatientOrderStatusEnum.Requested);
        restored.FulfilledRegId.Should().Be("-");
        restored.UpdUser.Should().Be("canceller_user");
    }

    [Fact]
    public void VoidSource_WithInpatientOrderModel_UpdatesOrderStatusToCancelled()
    {
        using var trans = TransHelper.NewScope();
        var orderId = GenerateOrderId();
        var regId = GenerateRegId();
        var now = DateTime.UtcNow;

        var dto = CreateTestDto(orderId, InpatientOrderStatusEnum.Fulfilled, OrderSourceEnum.AdmisiBooking) with
        {
            FulfilledRegId = regId
        };
        _orderDal.Insert(dto);

        var state = CreateState(regId, orderId, "InpatientOrderModel");

        var affected = _sut.VoidSource(state, "Patient decided to cancel admission", "canceller_user", now);

        affected.Should().Be(1);
        var cancelled = _orderDal.GetData(InpatientOrderModel.Key(orderId));
        cancelled.Should().NotBeNull();
        cancelled.OrderStatus.Should().Be((int)InpatientOrderStatusEnum.Cancelled);
        cancelled.VodUser.Should().Be("canceller_user");
    }

    [Fact]
    public void RestoreSource_WhenRegIdMismatch_ReturnsZeroAffected()
    {
        using var trans = TransHelper.NewScope();
        var orderId = GenerateOrderId();
        var actualRegId = GenerateRegId();
        var mismatchedRegId = GenerateRegId();
        var now = DateTime.UtcNow;

        var dto = CreateTestDto(orderId, InpatientOrderStatusEnum.Fulfilled, OrderSourceEnum.EmrRawatJalan) with
        {
            FulfilledRegId = actualRegId
        };
        _orderDal.Insert(dto);

        var state = CreateState(mismatchedRegId, orderId, "InpatientOrderModel");

        var affected = _sut.RestoreSource(state, "canceller_user", now);

        affected.Should().Be(0);
        var unchanged = _orderDal.GetData(InpatientOrderModel.Key(orderId));
        unchanged.OrderStatus.Should().Be((int)InpatientOrderStatusEnum.Fulfilled);
        unchanged.FulfilledRegId.Should().Be(actualRegId);
    }

    [Fact]
    public void TryStartLedger_And_LockLedger_TracksCancellationRequest()
    {
        using var trans = TransHelper.NewScope();
        var reqId = $"REQ-{Random.Shared.Next(100000, 999999)}";
        var regId = GenerateRegId();
        var now = DateTime.UtcNow;

        var ledger = new CoordinatedCancellationLedger(
            reqId, "fingerprint_abc", regId, "InProgress", "{}", now, new DateTime(3000, 1, 1), reqId);

        var started = _sut.TryStartLedger(ledger);
        started.Should().BeTrue();

        var locked = _sut.LockLedger(reqId);
        locked.Should().NotBeNull();
        locked.RequestId.Should().Be(reqId);
        locked.RegId.Should().Be(regId);
        locked.Lifecycle.Should().Be("InProgress");
    }

    private static string GenerateOrderId() =>
        $"OPN{DateTime.Now:yy}{Random.Shared.Next(10000000, 99999999)}"[..12];

    private static string GenerateRegId() =>
        $"RG{Random.Shared.Next(10000000, 99999999)}"[..10];

    private static CoordinatedCancellationState CreateState(string regId, string orderId, string sourceKind) =>
        new(
            RegId: regId,
            AdmissionStatus: AdmissionStatusEnum.Admitted,
            AdmissionUpdatedAt: DateTime.UtcNow,
            AdmissionSource: AdmissionSourceEnum.Admission,
            OpnameRequestId: "-",
            ReservationId: "-",
            RegistrationExists: true,
            RegInapExists: true,
            ActiveDoctorAssignments: 0,
            WaitingListId: null,
            WaitingListStatus: null,
            RegAktifCount: 1,
            SourceId: orderId,
            SourceKind: sourceKind,
            SourceOwnedAndCancellable: true,
            IsCoherentlyCancelled: false,
            AuditSnapshot: new { RegId = regId },
            AdmissionExists: true,
            AuditSnapshots: null,
            InpatientOrderId: orderId);

    private static InpatientOrderDto CreateTestDto(
        string id,
        InpatientOrderStatusEnum status,
        OrderSourceEnum source) =>
        new(
            id,
            (int)source,
            (int)status,
            $"337502{Random.Shared.Next(100000000, 999999999)}"[..15],
            "PASIEN TEST",
            new DateTime(1990, 1, 1),
            "L",
            "D001",
            "dr. Budi Sp.PD",
            DateTime.Today.AddDays(1),
            "1",
            "Kelas 1",
            "B01",
            "Bangsal Mawar",
            "Febris H-4",
            "BPJ",
            "BPJS Kesehatan",
            "00012345678",
            "-",
            $"REF-{Random.Shared.Next(10000, 99999)}",
            "EMR_RJ",
            "test_user",
            DateTime.Now,
            "",
            new DateTime(3000, 1, 1),
            "",
            new DateTime(3000, 1, 1));
}
