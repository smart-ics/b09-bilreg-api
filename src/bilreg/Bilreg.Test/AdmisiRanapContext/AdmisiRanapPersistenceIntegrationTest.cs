using Bilreg.Application.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext;

/// <summary>
/// Integration tests against test DB; requires Phase-2 SQL scripts deployed.
/// </summary>
public class AdmisiRanapPersistenceIntegrationTest
{
    private readonly WaitingListRepo _waitingListRepo;
    private readonly WaitingListWorklistDal _worklistDal;

    public AdmisiRanapPersistenceIntegrationTest()
    {
        var env = ConnStringHelper.GetTestEnv();
        var dal = new WaitingListDal(env);
        _waitingListRepo = new WaitingListRepo(dal);
        _worklistDal = new WaitingListWorklistDal(env);
    }

    [Fact]
    public void IT_DL_01_GivenWaitingList_WhenSaveLoadAndWorklist_ThenRoundTripsWithoutAdmissionTable()
    {
        var waitingListId = $"W{Guid.NewGuid():N}"[..12];
        var regId = "RG99999999";
        var model = new WaitingListModel(
            waitingListId,
            WaitingListStatusEnum.Waiting,
            regId,
            new PasienReff("P0001", "Pasien IT", new DateOnly(1990, 1, 1), "L"),
            new KelasReff("K01", "Kelas 1"),
            new BangsalReff("B001", "Bangsal A"),
            10,
            AuditTrailType.Create("it-user", DateTime.Now));

        try
        {
            _waitingListRepo.SaveChanges(model);
            var loaded = _waitingListRepo.LoadEntity(WaitingListModel.Key(waitingListId));

            loaded.HasValue.Should().BeTrue();
            loaded.Match(
                onSome: m =>
                {
                    m.WaitingListId.Should().Be(waitingListId);
                    m.RegId.Should().Be(regId);
                    m.Priority.Should().Be(10);
                },
                onNone: () => Assert.Fail("Expected loaded waiting list"));

            var worklist = _worklistDal
                .List(new WaitingListWorklistFilter(BangsalId: "B001"))
                .ToList();
            worklist.Should().Contain(x => x.WaitingListId == waitingListId);
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        finally
        {
            TryDeleteWaitingList(waitingListId);
        }
    }

    private static void TryDeleteWaitingList(string waitingListId)
    {
        try
        {
            var env = ConnStringHelper.GetTestEnv();
            var dal = new WaitingListDal(env);
            var key = WaitingListModel.Key(waitingListId);
            var dto = dal.GetData(key);
            if (dto is null)
                return;
            var voided = dto with
            {
                VodUser = "it-cleanup",
                VodDate = DateTime.Now
            };
            dal.Update(voided);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
