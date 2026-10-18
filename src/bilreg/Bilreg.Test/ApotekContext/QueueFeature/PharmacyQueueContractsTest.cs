using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using FluentAssertions;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueContractsTest
{
    #region Repository Port Contract & Default Method Tests

    private class TestQueueEntryRepo : IApotekQueueEntryRepo
    {
        public ApotekQueueEntryModel? SavedModel { get; private set; }
        public void SaveChanges(ApotekQueueEntryModel entity) => SavedModel = entity;
        public MayBe<ApotekQueueEntryModel> LoadEntity(IApotekQueueEntryKey key)
        {
            if (SavedModel is not null && SavedModel.AntrianId == key.AntrianId && SavedModel.NoUrut == key.NoUrut)
                return MayBe.From(SavedModel);
            return MayBe<ApotekQueueEntryModel>.None;
        }

        public IReadOnlyList<ApotekQueueEntryModel> ListActiveByDepo(string depoId)
            => SavedModel is not null ? new List<ApotekQueueEntryModel> { SavedModel } : new List<ApotekQueueEntryModel>();

        public IReadOnlyList<ApotekQueueEntryModel> ListByPhase(string depoId, ApotekPhaseEnum phase)
            => SavedModel is not null && SavedModel.ApotekPhase == phase ? new List<ApotekQueueEntryModel> { SavedModel } : new List<ApotekQueueEntryModel>();
    }

    [Fact]
    public void ApotekQueueEntryRepo_default_methods_delegate_to_savechanges_and_loadentity()
    {
        IApotekQueueEntryRepo repo = new TestQueueEntryRepo();
        var model = ApotekQueueEntryModel.Create("ANT-01", 5, "SP-01", "A", new DateTime(2026, 10, 18, 9, 0, 0));

        repo.Save(model);
        var loaded = repo.Get("ANT-01", 5);
        loaded.HasValue.Should().BeTrue();
        loaded.Value.QueueLabel.Should().Be("A005");

        var missing = repo.Get("ANT-01", 999);
        missing.HasValue.Should().BeFalse();

        var active = repo.ListActiveByDepo("DEPO-01");
        active.Should().ContainSingle();

        var inPhase = repo.ListByPhase("DEPO-01", ApotekPhaseEnum.TAKEN);
        inPhase.Should().ContainSingle();
    }

    private class TestServicePointRepo : IPharmacyServicePointRepo
    {
        public PharmacyServicePointModel? SavedModel { get; private set; }
        public void SaveChanges(PharmacyServicePointModel entity) => SavedModel = entity;
        public MayBe<PharmacyServicePointModel> LoadEntity(IPharmacyServicePointKey key)
        {
            if (SavedModel is not null && SavedModel.ServicePointId == key.ServicePointId)
                return MayBe.From(SavedModel);
            return MayBe<PharmacyServicePointModel>.None;
        }

        public IReadOnlyList<PharmacyServicePointModel> ListActive()
            => SavedModel is not null && SavedModel.Active ? new List<PharmacyServicePointModel> { SavedModel } : new List<PharmacyServicePointModel>();

        public IReadOnlyList<PharmacyServicePointModel> ListByDepo(string depoFarmasiId)
            => SavedModel is not null && SavedModel.DepoFarmasiId == depoFarmasiId ? new List<PharmacyServicePointModel> { SavedModel } : new List<PharmacyServicePointModel>();
    }

    [Fact]
    public void PharmacyServicePointRepo_default_methods_delegate_correctly()
    {
        IPharmacyServicePointRepo repo = new TestServicePointRepo();
        var model = PharmacyServicePointModel.Create("SP-01", "DEPO-RAJAL", "Resep Non Racik", "A");

        repo.Save(model);
        var loaded = repo.Get("SP-01");
        loaded.HasValue.Should().BeTrue();
        loaded.Value.DisplayName.Should().Be("Resep Non Racik");

        var missing = repo.Get("SP-UNKNOWN");
        missing.HasValue.Should().BeFalse();

        repo.ListActive().Should().ContainSingle();
        repo.ListByDepo("DEPO-RAJAL").Should().ContainSingle();
        repo.ListByDepo("DEPO-RANAP").Should().BeEmpty();
    }

    private class TestQueueMappingRepo : IPharmacyQueueMappingRepo
    {
        public QueueMappingModel? SavedModel { get; private set; }
        public void SaveChanges(QueueMappingModel entity) => SavedModel = entity;
        public MayBe<QueueMappingModel> LoadEntity(IQueueMappingKey key)
        {
            if (SavedModel is not null && SavedModel.DemandKind == key.DemandKind && SavedModel.DemandId == key.DemandId)
                return MayBe.From(SavedModel);
            return MayBe<QueueMappingModel>.None;
        }

        public IReadOnlyList<QueueMappingModel> ListByQueue(string antrianId, int noUrut)
            => SavedModel is not null && SavedModel.AntrianId == antrianId && SavedModel.NoUrut == noUrut
                ? new List<QueueMappingModel> { SavedModel }
                : new List<QueueMappingModel>();
    }

    [Fact]
    public void PharmacyQueueMappingRepo_default_methods_delegate_and_work()
    {
        IPharmacyQueueMappingRepo repo = new TestQueueMappingRepo();
        var model = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP001", "ANT-01", 5, "TRK-01", QueueMappingMethodEnum.Manual, "user1", new DateTime(2026, 10, 18, 9, 0, 0));

        repo.Save(model);
        var loaded = repo.GetByDemand(QueueDemandKindEnum.Resep, "KP001");
        loaded.HasValue.Should().BeTrue();
        loaded.Value.DemandId.Should().Be("KP001");

        var byQueue = repo.ListByQueue("ANT-01", 5);
        byQueue.Should().ContainSingle();

        // Default interface implementation returns false until overridden by DAL in P4
        repo.SetPasienHadir(QueueDemandKindEnum.Resep, "KP001", "user1", DateTime.Now).Should().BeFalse();
    }

    private class TestDisplayLoketRepo : IPharmacyDisplayLoketRepo
    {
        private readonly List<PharmacyDisplayLoketModel> _mappings = new();

        public IReadOnlyList<PharmacyDisplayLoketModel> ListByDisplayId(string displayId)
            => _mappings.Where(x => x.DisplayId == displayId).OrderBy(x => x.SortOrder).ToList();

        public void SaveMapping(string displayId, string loketKey, int sortOrder)
        {
            _mappings.RemoveAll(x => x.DisplayId == displayId && x.LoketKey == loketKey);
            _mappings.Add(new PharmacyDisplayLoketModel(displayId, loketKey, sortOrder));
        }

        public void DeleteMapping(string displayId, string loketKey)
        {
            _mappings.RemoveAll(x => x.DisplayId == displayId && x.LoketKey == loketKey);
        }
    }

    [Fact]
    public void PharmacyDisplayLoketRepo_can_save_list_and_delete()
    {
        var repo = new TestDisplayLoketRepo();
        repo.SaveMapping("DISP-01", "LOK-01", 1);
        repo.SaveMapping("DISP-01", "LOK-02", 2);

        var list = repo.ListByDisplayId("DISP-01");
        list.Should().HaveCount(2);
        list[0].LoketKey.Should().Be("LOK-01");
        list[1].LoketKey.Should().Be("LOK-02");

        repo.DeleteMapping("DISP-01", "LOK-01");
        repo.ListByDisplayId("DISP-01").Should().ContainSingle().Which.LoketKey.Should().Be("LOK-02");
    }

    #endregion

    #region External Ports Tests

    [Fact]
    public void FailClosedPharmacyPaymentClearancePort_returns_false_and_fail_closed_result()
    {
        var port = new FailClosedPharmacyPaymentClearancePort();

        port.IsBillCleared("DU001", "").Should().BeFalse();
        var check = port.CheckClearance("DU001", "");
        check.IsCleared.Should().BeFalse();
        check.ClearanceType.Should().Be("FailClosed");
        check.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task NullPharmacyQueuePublisher_completes_synchronously_without_exceptions()
    {
        var publisher = new NullPharmacyQueuePublisher();

        var worklistTask = publisher.PublishWorklistRefresh("DEPO-01");
        var displayTask = publisher.PublishDisplayRefresh("LOK-01", 101);

        await Task.WhenAll(worklistTask, displayTask);
        worklistTask.IsCompletedSuccessfully.Should().BeTrue();
        displayTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    #endregion

    #region DTO Immuntability & Value Records Tests

    [Fact]
    public void Intake_kiosk_dtos_preserve_values_and_support_value_equality()
    {
        var req = new PharmacyQueueIntakeReq("SP-01", "DEPO-01", "KIOSK-01");
        req.ServicePointId.Should().Be("SP-01");
        req.DepoFarmasiId.Should().Be("DEPO-01");
        req.UserId.Should().Be("KIOSK-01");

        var now = new DateTime(2026, 10, 18, 9, 30, 0);
        var resp1 = new PharmacyQueueIntakeResponse("ANT-01", 5, "A005", "SP-01", "Non Racik", "A", ApotekPhaseEnum.TAKEN, now, 4);
        var resp2 = new PharmacyQueueIntakeResponse("ANT-01", 5, "A005", "SP-01", "Non Racik", "A", ApotekPhaseEnum.TAKEN, now, 4);

        resp1.Should().Be(resp2); // Record value equality
        resp1.QueueLabel.Should().Be("A005");
        resp1.WaitingAheadCount.Should().Be(4);
    }

    [Fact]
    public void Worklist_dtos_preserve_properties_and_collections()
    {
        var qry = new PharmacyQueueWorklistQry("DEPO-01", "waiting", ApotekPhaseEnum.TAKEN);
        qry.DepoFarmasiId.Should().Be("DEPO-01");
        qry.FilterTab.Should().Be("waiting");
        qry.Phase.Should().Be(ApotekPhaseEnum.TAKEN);

        var item = new PharmacyQueueWorklistItemResponse(
            "ANT-01", 5, "A005", "SP-01", "Non Racik", ApotekPhaseEnum.TAKEN,
            1, new DateTime(2026, 10, 18, 9, 15, 0),
            0, DateTime.MinValue,
            new DateTime(2026, 10, 18, 9, 0, 0),
            DateTime.MinValue, DateTime.MinValue, DateTime.MinValue,
            true, new DateTime(2026, 10, 18, 9, 10, 0),
            new[] { "KP001", "KP002" },
            "Budi Santoso", "RM12345", "REG001", "dr. Ahmad", "Poli Penyakit Dalam", "BPJS",
            true, false, "");

        item.QueueLabel.Should().Be("A005");
        item.DemandIds.Should().HaveCount(2);
        item.PasienHadir.Should().BeTrue();
        item.IsCito.Should().BeTrue();

        var response = new PharmacyQueueWorklistResponse("DEPO-01", new[] { item }, 1, 1, 0);
        response.TotalCount.Should().Be(1);
        response.Items.Should().ContainSingle();
    }

    [Fact]
    public void Calling_operation_dtos_preserve_rowversion_and_flags()
    {
        var rowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 1, 2 };
        var callReq = new PharmacyQueueCallReq("LOK-01", "ANT-01", 5, rowVersion, "user1");
        callReq.LoketKey.Should().Be("LOK-01");
        callReq.AntrianId.Should().Be("ANT-01");
        callReq.NoUrut.Should().Be(5);
        callReq.ExpectedRowVersion.Should().BeEquivalentTo(rowVersion);

        var recallReq = new PharmacyQueueRecallReq("LOK-01", rowVersion, "user1");
        recallReq.RowVersion.Should().BeEquivalentTo(rowVersion);

        var startReq = new PharmacyQueueStartServiceReq("LOK-01", rowVersion, "user1");
        startReq.RowVersion.Should().BeEquivalentTo(rowVersion);

        var releaseReq = new PharmacyQueueReleaseReq("LOK-01", rowVersion, "user1");
        releaseReq.RowVersion.Should().BeEquivalentTo(rowVersion);

        var skipReq = new PharmacyQueueSkipReq("LOK-01", "Pasien tidak hadir", rowVersion, "user1");
        skipReq.Reason.Should().Be("Pasien tidak hadir");

        var callResp = new PharmacyQueueLoketCallResponse(
            "LOK-01", "Loket Penerimaan 1", LoketTypeEnum.PenerimaanResep, "ANT-01", 5, "A005",
            LoketClaimStateEnum.Called, 101, new DateTime(2026, 10, 18, 9, 20, 0), rowVersion, true, "Success");

        callResp.AnnouncementVersion.Should().Be(101);
        callResp.Success.Should().BeTrue();
        callResp.RowVersion.Should().BeEquivalentTo(rowVersion);
    }

    [Fact]
    public void Demand_mapping_and_pasien_hadir_dtos_preserve_values()
    {
        var mapReq = new PharmacyQueueMapDemandReq("ANT-01", 5, QueueDemandKindEnum.Resep, "KP001", "TRK-01", QueueMappingMethodEnum.Manual, "user1");
        mapReq.DemandId.Should().Be("KP001");
        mapReq.DemandKind.Should().Be(QueueDemandKindEnum.Resep);

        var mapResp = new PharmacyQueueMapDemandResponse("ANT-01", 5, QueueDemandKindEnum.Resep, "KP001", new DateTime(2026, 10, 18, 9, 25, 0), true);
        mapResp.Success.Should().BeTrue();

        var hadirReq = new PharmacyQueueFlagPasienHadirReq(QueueDemandKindEnum.Resep, "KP001", "ANT-01", 5, "user1");
        hadirReq.DemandId.Should().Be("KP001");

        var hadirResp = new PharmacyQueueFlagPasienHadirResponse(QueueDemandKindEnum.Resep, "KP001", "ANT-01", 5, true, new DateTime(2026, 10, 18, 9, 26, 0), true);
        hadirResp.PasienHadir.Should().BeTrue();
    }

    [Fact]
    public void Handover_resolution_dtos_preserve_payment_and_override_details()
    {
        var handoverReq = new PharmacyQueueCompleteHandoverReq(
            "ANT-01", 5, "DU001", "SEP12345", true, "Emergency override approved by doctor", "APT-DRURY", "user1");

        handoverReq.PenjualanId.Should().Be("DU001");
        handoverReq.NoSep.Should().Be("SEP12345");
        handoverReq.IsPaymentOverridden.Should().BeTrue();
        handoverReq.PaymentOverrideReason.Should().Be("Emergency override approved by doctor");
        handoverReq.PaymentOverridePharmacistId.Should().Be("APT-DRURY");

        var handoverResp = new PharmacyQueueCompleteHandoverResponse(
            "ANT-01", 5, ApotekPhaseEnum.SELESAI, new DateTime(2026, 10, 18, 10, 0, 0), true, true, "Handover completed.");

        handoverResp.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
        handoverResp.IsPaymentOverridden.Should().BeTrue();
        handoverResp.Success.Should().BeTrue();
    }

    #endregion
}
