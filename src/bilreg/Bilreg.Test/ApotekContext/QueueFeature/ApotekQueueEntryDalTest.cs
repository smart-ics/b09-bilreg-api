using System.Data.SqlClient;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class ApotekQueueEntryDalTest
{
    private readonly ApotekQueueEntryDal _dal;
    private readonly ApotekQueueEntryRepo _repo;
    private readonly PharmacyServicePointDal _spDal;

    public ApotekQueueEntryDalTest()
    {
        var opt = ConnStringHelper.GetTestEnv();
        _dal = new ApotekQueueEntryDal(opt);
        _repo = new ApotekQueueEntryRepo(_dal);
        _spDal = new PharmacyServicePointDal(opt);
    }

    private static string NewId(string prefix = "Q") => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

    [Fact]
    public void Dal_Insert_And_GetData_PersistsAndRetrievesAllColumns()
    {
        var antrianId = NewId("ANT");
        const int noUrut = 1;
        var spId = NewId("SP");
        var now = new DateTime(2026, 10, 18, 9, 30, 0);

        Cleanup(antrianId, noUrut);

        try
        {
            var dto = new ApotekQueueEntryDto(
                AntrianId: antrianId,
                NoUrut: noUrut,
                ServicePointId: spId,
                QueueLabel: "A001",
                ApotekPhase: (int)ApotekPhaseEnum.TAKEN,
                CallCountPenerimaan: 1,
                LastCalledPenerimaanAt: now.AddMinutes(2),
                CallCountSerah: 0,
                LastCalledSerahAt: new DateTime(3000, 1, 1),
                TakenAt: now,
                ServedAt: new DateTime(3000, 1, 1),
                PreparedAt: new DateTime(3000, 1, 1),
                DoneAt: new DateTime(3000, 1, 1),
                WithdrawnAt: new DateTime(3000, 1, 1),
                LastSkippedAt: new DateTime(3000, 1, 1),
                LastSkippedLoketType: 0,
                WithdrawalReason: "",
                WithdrawalUserId: "",
                IsPaymentOverridden: false,
                PaymentOverrideReason: "",
                PaymentOverridePharmacistId: "",
                CrtUser: "usr1",
                CrtDate: now,
                UpdUser: "usr1",
                UpdDate: now,
                VodUser: "",
                VodDate: new DateTime(3000, 1, 1));

            _dal.Insert(dto);

            var actual = _dal.GetData(ApotekQueueEntryModel.Key(antrianId, noUrut));
            actual.Should().NotBeNull();
            actual.AntrianId.Should().Be(antrianId);
            actual.NoUrut.Should().Be(noUrut);
            actual.ServicePointId.Should().Be(spId);
            actual.QueueLabel.Should().Be("A001");
            actual.ApotekPhase.Should().Be((int)ApotekPhaseEnum.TAKEN);
            actual.CallCountPenerimaan.Should().Be(1);
            actual.LastCalledPenerimaanAt.Should().BeCloseTo(now.AddMinutes(2), TimeSpan.FromSeconds(1));
            actual.CallCountSerah.Should().Be(0);
            actual.TakenAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
            actual.CrtUser.Should().Be("usr1");

            var actualByComposite = _dal.Get(antrianId, noUrut);
            actualByComposite.Should().NotBeNull();
            actualByComposite!.QueueLabel.Should().Be("A001");
        }
        finally
        {
            Cleanup(antrianId, noUrut);
        }
    }

    [Fact]
    public void Dal_Update_ModifiesOperationalPhaseAndCounters()
    {
        var antrianId = NewId("ANT");
        const int noUrut = 2;
        var spId = NewId("SP");
        var now = DateTime.Now;

        Cleanup(antrianId, noUrut);

        try
        {
            var dto = new ApotekQueueEntryDto(
                antrianId, noUrut, spId, "A002", (int)ApotekPhaseEnum.TAKEN,
                0, new DateTime(3000, 1, 1), 0, new DateTime(3000, 1, 1),
                now, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
                new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), 0, "", "", false, "", "",
                "usr1", now, "usr1", now, "", new DateTime(3000, 1, 1));
            _dal.Insert(dto);

            var servedAt = now.AddMinutes(5);
            var updated = dto with
            {
                ApotekPhase = (int)ApotekPhaseEnum.PENERIMAAN,
                CallCountPenerimaan = 2,
                LastCalledPenerimaanAt = servedAt,
                ServedAt = servedAt,
                UpdUser = "usr2",
                UpdDate = servedAt
            };
            _dal.Update(updated);

            var actual = _dal.GetData(ApotekQueueEntryModel.Key(antrianId, noUrut));
            actual.ApotekPhase.Should().Be((int)ApotekPhaseEnum.PENERIMAAN);
            actual.CallCountPenerimaan.Should().Be(2);
            actual.ServedAt.Should().BeCloseTo(servedAt, TimeSpan.FromSeconds(1));
            actual.UpdUser.Should().Be("usr2");
        }
        finally
        {
            Cleanup(antrianId, noUrut);
        }
    }

    [Fact]
    public void Dal_ListActiveByDepo_And_ListByPhase_FiltersCorrectly()
    {
        var depoId = NewId("DEP");
        var spId = NewId("SP");
        var antrianId = NewId("ANT");
        var now = DateTime.Now;

        // Create service point linked to depo
        _spDal.Insert(new PharmacyServicePointDto(
            spId, depoId, "Loket Depo", "A", true,
            "u", now, "u", now, "", new DateTime(3000, 1, 1)));

        Cleanup(antrianId, 1);
        Cleanup(antrianId, 2);
        Cleanup(antrianId, 3);

        try
        {
            // Entry 1: TAKEN (Active)
            _dal.Insert(new ApotekQueueEntryDto(
                antrianId, 1, spId, "A001", (int)ApotekPhaseEnum.TAKEN,
                0, new DateTime(3000, 1, 1), 0, new DateTime(3000, 1, 1),
                now, new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), new DateTime(3000, 1, 1),
                new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), 0, "", "", false, "", "",
                "u", now, "u", now, "", new DateTime(3000, 1, 1)));

            // Entry 2: SIAP_SERAH (Active)
            _dal.Insert(new ApotekQueueEntryDto(
                antrianId, 2, spId, "A002", (int)ApotekPhaseEnum.SIAP_SERAH,
                1, now, 0, new DateTime(3000, 1, 1),
                now, now, now, new DateTime(3000, 1, 1),
                new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), 0, "", "", false, "", "",
                "u", now, "u", now, "", new DateTime(3000, 1, 1)));

            // Entry 3: SELESAI (Terminal / Inactive)
            _dal.Insert(new ApotekQueueEntryDto(
                antrianId, 3, spId, "A003", (int)ApotekPhaseEnum.SELESAI,
                1, now, 1, now,
                now, now, now, now,
                new DateTime(3000, 1, 1), new DateTime(3000, 1, 1), 0, "", "", false, "", "",
                "u", now, "u", now, "", new DateTime(3000, 1, 1)));

            var activeList = _dal.ListActiveByDepo(depoId).ToList();
            activeList.Should().HaveCount(2);
            activeList.Should().Contain(x => x.NoUrut == 1);
            activeList.Should().Contain(x => x.NoUrut == 2);
            activeList.Should().NotContain(x => x.NoUrut == 3);

            var siapSerahList = _dal.ListByPhase(depoId, (int)ApotekPhaseEnum.SIAP_SERAH).ToList();
            siapSerahList.Should().ContainSingle(x => x.NoUrut == 2);
        }
        finally
        {
            Cleanup(antrianId, 1);
            Cleanup(antrianId, 2);
            Cleanup(antrianId, 3);
            _spDal.Delete(PharmacyServicePointModel.Key(spId));
        }
    }

    [Fact]
    public void Repo_FullLifecycle_Transitions_PersistAccurately()
    {
        var depoId = NewId("DEP");
        var spId = NewId("SP");
        var antrianId = NewId("ANT");
        const int noUrut = 1;
        var now = DateTime.Now;

        _spDal.Insert(new PharmacyServicePointDto(
            spId, depoId, "Layanan 1", "A", true,
            "u", now, "u", now, "", new DateTime(3000, 1, 1)));

        Cleanup(antrianId, noUrut);

        try
        {
            // 1. Create and Save (TAKEN)
            var model = ApotekQueueEntryModel.Create(antrianId, noUrut, spId, "A", now);
            _repo.SaveChanges(model);

            var step1 = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            step1.ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
            step1.QueueLabel.Should().Be("A001");
            step1.CallCountPenerimaan.Should().Be(0);

            // 2. Call Loket 1
            step1.RecordCallPenerimaan(now.AddMinutes(1));
            _repo.SaveChanges(step1);

            var step2 = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            step2.CallCountPenerimaan.Should().Be(1);
            step2.LastCalledPenerimaanAt.Should().BeCloseTo(now.AddMinutes(1), TimeSpan.FromSeconds(1));

            // 3. Start Service Loket 1 (PENERIMAAN)
            step2.StartServicePenerimaan(now.AddMinutes(2));
            _repo.SaveChanges(step2);

            var step3 = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            step3.ApotekPhase.Should().Be(ApotekPhaseEnum.PENERIMAAN);
            step3.ServedAt.Should().BeCloseTo(now.AddMinutes(2), TimeSpan.FromSeconds(1));

            // 4. Transition to PENYIAPAN
            step3.TransitionToPenyiapan();
            _repo.SaveChanges(step3);

            var step4 = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            step4.ApotekPhase.Should().Be(ApotekPhaseEnum.PENYIAPAN);

            // 5. Consolidate to SIAP_SERAH
            step4.ConsolidateToSiapSerah(now.AddMinutes(15));
            _repo.SaveChanges(step4);

            var step5 = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            step5.ApotekPhase.Should().Be(ApotekPhaseEnum.SIAP_SERAH);
            step5.PreparedAt.Should().BeCloseTo(now.AddMinutes(15), TimeSpan.FromSeconds(1));

            // 6. Call Loket 2 (Serah)
            step5.RecordCallSerah(now.AddMinutes(16));
            _repo.SaveChanges(step5);

            var step6 = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            step6.CallCountSerah.Should().Be(1);
            step6.LastCalledSerahAt.Should().BeCloseTo(now.AddMinutes(16), TimeSpan.FromSeconds(1));

            // 7. Start Service Loket 2 (SERAH_OBAT)
            step6.StartServiceSerah(now.AddMinutes(17));
            _repo.SaveChanges(step6);

            var step7 = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            step7.ApotekPhase.Should().Be(ApotekPhaseEnum.SERAH_OBAT);

            // 8. Complete Handover with override (SELESAI)
            step7.CompleteHandover(now.AddMinutes(18), isPaymentOverridden: true, reason: "Emergency release", pharmacistId: "PH-001");
            _repo.SaveChanges(step7);

            var finalStep = _repo.LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut)).Value;
            finalStep.ApotekPhase.Should().Be(ApotekPhaseEnum.SELESAI);
            finalStep.DoneAt.Should().BeCloseTo(now.AddMinutes(18), TimeSpan.FromSeconds(1));
            finalStep.IsPaymentOverridden.Should().BeTrue();
            finalStep.PaymentOverrideReason.Should().Be("Emergency release");
            finalStep.PaymentOverridePharmacistId.Should().Be("PH-001");
            finalStep.IsTerminal.Should().BeTrue();
        }
        finally
        {
            Cleanup(antrianId, noUrut);
            _spDal.Delete(PharmacyServicePointModel.Key(spId));
        }
    }

    private static void Cleanup(string antrianId, int noUrut)
    {
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("DELETE FROM BILRG_AptQueueEntry WHERE AntrianId = @antrianId AND NoUrut = @noUrut", new { antrianId, noUrut });
    }
}
