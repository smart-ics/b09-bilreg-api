using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class ClinicQueueOperationDalTest
{
    private readonly IOptions<DatabaseOptions> _opt = ConnStringHelper.GetTestEnv();
    private readonly ClinicQueueOperationDal _sut;

    public ClinicQueueOperationDalTest()
    {
        _sut = new ClinicQueueOperationDal(_opt);
    }

    private static ClinicQueueCallDto FakeCall(string poliId = "POLI_INT", string dokterId = "DOC_001", int noUrut = 10, int callCount = 1)
    {
        return new ClinicQueueCallDto
        {
            PoliId = poliId,
            DokterId = dokterId,
            RuangId = "R101",
            AntrianId = "ANT_20261028_001",
            NoUrut = noUrut,
            CalledAt = new DateTime(2026, 10, 28, 10, 0, 0),
            CallCount = callCount,
            CallStatus = ClinicQueueCallStatus.Active,
            UserId = "user_test",
            CrtDate = new DateTime(2026, 10, 28, 10, 0, 0),
            UpdDate = new DateTime(2026, 10, 28, 10, 0, 0)
        };
    }

    [Fact]
    public void UpsertCall_WhenNew_InsertsActiveCall()
    {
        using var trans = TransHelper.NewScope();
        var call = FakeCall("POLI_A", "DOC_A", 1, 1);

        _sut.UpsertCall(call);

        var active = _sut.GetActiveCall("POLI_A", "DOC_A");
        active.Should().NotBeNull();
        active!.PoliId.Should().Be("POLI_A");
        active.DokterId.Should().Be("DOC_A");
        active.NoUrut.Should().Be(1);
        active.CallCount.Should().Be(1);
        active.CallStatus.Should().Be(ClinicQueueCallStatus.Active);
    }

    [Fact]
    public void UpsertCall_WhenExists_UpdatesExistingCall()
    {
        using var trans = TransHelper.NewScope();
        var call = FakeCall("POLI_A", "DOC_A", 1, 1);
        _sut.UpsertCall(call);

        call.CallCount = 2;
        call.RuangId = "R102";
        _sut.UpsertCall(call);

        var active = _sut.GetActiveCall("POLI_A", "DOC_A");
        active.Should().NotBeNull();
        active!.CallCount.Should().Be(2);
        active.RuangId.Should().Be("R102");
    }

    [Fact]
    public void MarkSkipped_SetsStatusSkipped_AndActiveCallReturnsNull()
    {
        using var trans = TransHelper.NewScope();
        var call = FakeCall("POLI_B", "DOC_B", 2, 1);
        _sut.UpsertCall(call);

        var skipped = _sut.MarkSkipped("POLI_B", "DOC_B", "user_skip");
        skipped.Should().BeTrue();

        var active = _sut.GetActiveCall("POLI_B", "DOC_B");
        active.Should().BeNull();
    }

    [Fact]
    public void DismissCall_SetsStatusDismissed_AndActiveCallReturnsNull()
    {
        using var trans = TransHelper.NewScope();
        var call = FakeCall("POLI_C", "DOC_C", 3, 1);
        _sut.UpsertCall(call);

        var dismissed = _sut.DismissCall("POLI_C", "DOC_C", "user_dismiss");
        dismissed.Should().BeTrue();

        var active = _sut.GetActiveCall("POLI_C", "DOC_C");
        active.Should().BeNull();
    }

    [Fact]
    public void GetActiveCallsByPoliIds_ReturnsOnlyActiveCallsForSpecifiedPolis()
    {
        using var trans = TransHelper.NewScope();
        var call1 = FakeCall("POLI_X", "DOC_X1", 1, 1);
        var call2 = FakeCall("POLI_X", "DOC_X2", 2, 1);
        var call3 = FakeCall("POLI_Y", "DOC_Y1", 3, 1);
        var call4 = FakeCall("POLI_Z", "DOC_Z1", 4, 1);

        _sut.UpsertCall(call1);
        _sut.UpsertCall(call2);
        _sut.UpsertCall(call3);
        _sut.UpsertCall(call4);

        // Mark one skipped
        _sut.MarkSkipped("POLI_X", "DOC_X2", "user");

        var results = _sut.GetActiveCallsByPoliIds(new[] { "POLI_X", "POLI_Y" });
        results.Should().HaveCount(2);
        results.Select(x => x.DokterId).Should().Contain(new[] { "DOC_X1", "DOC_Y1" });
        results.Select(x => x.DokterId).Should().NotContain("DOC_X2");
    }

    [Fact]
    public void GetActiveCallsByPoliId_ReturnsOnlyActiveCalls()
    {
        using var trans = TransHelper.NewScope();
        var call1 = FakeCall("POLI_M", "DOC_M1", 5, 1);
        _sut.UpsertCall(call1);

        var results = _sut.GetActiveCallsByPoliId("POLI_M");
        results.Should().HaveCount(1);
        results[0].DokterId.Should().Be("DOC_M1");
    }

    [Fact]
    public void DisplayPoliMapping_ReplaceAndRetrieve_WorksCorrectly()
    {
        using var trans = TransHelper.NewScope();
        const string displayId = "DISP_CLINIC_01";
        var polis = new[] { "POLI_A", "POLI_B", "POLI_C" };

        _sut.ReplaceDisplayPolis(displayId, polis);

        var retrievedPolis = _sut.GetPolisByDisplayId(displayId);
        retrievedPolis.Should().BeEquivalentTo(polis, options => options.WithStrictOrdering());

        var dtos = _sut.GetDisplayPolis(displayId);
        dtos.Should().HaveCount(3);
        dtos[0].PoliId.Should().Be("POLI_A");
        dtos[0].SortOrder.Should().Be(1);
        dtos[1].PoliId.Should().Be("POLI_B");
        dtos[1].SortOrder.Should().Be(2);

        var displays = _sut.GetDisplaysByPoliId("POLI_B");
        displays.Should().Contain(displayId);

        // Replace with new subset
        _sut.ReplaceDisplayPolis(displayId, new[] { "POLI_C" });
        var updatedPolis = _sut.GetPolisByDisplayId(displayId);
        updatedPolis.Should().ContainSingle().Which.Should().Be("POLI_C");
    }

    [Fact]
    public void MarkInService_WhenActiveCallExists_SetsStatusInService()
    {
        using var trans = TransHelper.NewScope();
        var call = FakeCall("POLI_SERV", "DOC_SERV", 10, 1);
        _sut.UpsertCall(call);

        var marked = _sut.MarkInService("ANT_20261028_001", 10, "user_serv");
        marked.Should().BeTrue();

        var calls = _sut.GetActiveCallsByPoliIds(new[] { "POLI_SERV" });
        calls.Should().HaveCount(1);
        calls[0].CallStatus.Should().Be(ClinicQueueCallStatus.InService);
        calls[0].UserId.Should().Be("user_serv");
    }

    [Fact]
    public void MarkInService_WhenAlreadyInService_ReturnsFalse()
    {
        using var trans = TransHelper.NewScope();
        var call = FakeCall("POLI_SERV2", "DOC_SERV2", 11, 1);
        _sut.UpsertCall(call);

        _sut.MarkInService("ANT_20261028_001", 11, "user_first").Should().BeTrue();
        var second = _sut.MarkInService("ANT_20261028_001", 11, "user_second");
        second.Should().BeFalse();
    }

    [Fact]
    public void MarkDismissedByEntry_WhenInServiceOrActive_SetsStatusDismissed()
    {
        using var trans = TransHelper.NewScope();
        var call = FakeCall("POLI_DISM", "DOC_DISM", 12, 1);
        _sut.UpsertCall(call);

        // Transition to InService first
        _sut.MarkInService("ANT_20261028_001", 12, "user_serv").Should().BeTrue();

        // Dismiss
        var dismissed = _sut.MarkDismissedByEntry("ANT_20261028_001", 12, "user_dism");
        dismissed.Should().BeTrue();

        var calls = _sut.GetActiveCallsByPoliIds(new[] { "POLI_DISM" });
        calls.Should().BeEmpty();
    }

    [Fact]
    public void GetActiveCallsByPoliIds_IncludesBothActiveAndInServiceCalls()
    {
        using var trans = TransHelper.NewScope();
        var call1 = FakeCall("POLI_MIX", "DOC_MIX1", 1, 1);
        var call2 = FakeCall("POLI_MIX", "DOC_MIX2", 2, 1);
        _sut.UpsertCall(call1);
        _sut.UpsertCall(call2);

        // Mark call2 as InService
        _sut.MarkInService("ANT_20261028_001", 2, "user_mix");

        var results = _sut.GetActiveCallsByPoliIds(new[] { "POLI_MIX" });
        results.Should().HaveCount(2);
        results.Should().Contain(x => x.CallStatus == ClinicQueueCallStatus.Active && x.DokterId == "DOC_MIX1");
        results.Should().Contain(x => x.CallStatus == ClinicQueueCallStatus.InService && x.DokterId == "DOC_MIX2");
    }
}
