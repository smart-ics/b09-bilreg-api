using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkScheduleOpDoctorSyncIntegrationTest
{
    private readonly Dictionary<string, OrderOpModel> _orderOps = new();
    private readonly Dictionary<string, OpCaseModel> _opCases = new();
    private readonly Dictionary<string, ScheduleOpModel> _schedules = new();

    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<IScheduleOpRepo> _scheduleOpRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IKamarRepo> _kamarRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IIcd10Repo> _icdRepoMock = new();
    private readonly Mock<IJenisOperasiRepo> _jenisOperasiRepoMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();

    private readonly OkCreateOrderOpByRegHandler _createOrderHandler;
    private readonly OkScheduleOpSetCommandHandler _setScheduleHandler;
    private readonly OkScheduleOpAssignLeaderHandler _assignLeaderHandler;
    private readonly OkListOperasiAktifHandler _listActiveHandler;
    private readonly OkScheduleOpCancelHandler _cancelHandler;

    public OkScheduleOpDoctorSyncIntegrationTest()
    {
        var fixedTime = new DateTime(2026, 10, 22, 8, 0, 0);
        _tglJamProviderMock.Setup(x => x.Now).Returns(fixedTime);

        // Doctor master setup: Dr. Martahadinan (DR00083) & Dr. Arya Yunan (DR00139)
        var drMartahadinan = CreateSampleDoctor("DR00083", "dr. Martahadinan, Sp.B");
        var drAryaYunan = CreateSampleDoctor("DR00139", "dr. Arya Yunan, Sp.B");
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DR00083"))).Returns(MayBe.From(drMartahadinan));
        _ppaRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == "DR00139"))).Returns(MayBe.From(drAryaYunan));

        // Room master setup
        var kamarOk1 = CreateSampleKamar("KAMAR-OK1", "OK 01");
        _kamarRepoMock.Setup(x => x.LoadEntity(It.Is<IKamarKey>(k => k.KamarId == "KAMAR-OK1"))).Returns(MayBe.From(kamarOk1));

        // Reg, Layanan, ICD, JenisOperasi, Tarif
        var reg = CreateSampleReg("REG-001");
        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-001"))).Returns(MayBe.From(reg));

        var layanan = LayananType.Default with { LayananId = "LYN-001", LayananName = "Poli Bedah" };
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LYN-001"))).Returns(MayBe.From(layanan));

        var icd = new Icd10Type("K35.8", "Acute Appendicitis");
        _icdRepoMock.Setup(x => x.LoadEntity(It.Is<IIcd10Key>(k => k.Icd10Id == "K35.8"))).Returns(MayBe.From(icd));

        var jop = new JenisOperasiType("JOP-001", "Bedah Umum");
        _jenisOperasiRepoMock.Setup(x => x.LoadEntity(It.Is<IJenisOperasiKey>(k => k.JenisOperasiId == "JOP-001"))).Returns(MayBe.From(jop));

        var tarif = new TarifType("TRF-001", "Apendektomi", GroupTarifType.Default, GroupTarifDkType.Default, JenisTarifType.Default, RekapCetakType.Default.ToReff());
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == "TRF-001"))).Returns(MayBe.From(tarif));

        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>())).Returns(new List<OrderOpView>());

        // State-tracking setup for OrderOp repository
        _orderOpRepoMock.Setup(x => x.SaveChanges(It.IsAny<OrderOpModel>()))
            .Callback<OrderOpModel>(m => _orderOps[m.OrderOpId] = m);
        _orderOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns<IOrderOpKey>(k => _orderOps.TryGetValue(k.OrderOpId, out var m) ? MayBe.From(m) : MayBe<OrderOpModel>.None);

        // State-tracking setup for OpCase repository
        _opCaseRepoMock.Setup(x => x.SaveChanges(It.IsAny<OpCaseModel>()))
            .Callback<OpCaseModel>(m => _opCases[m.OrderOp.OrderOpId] = m);
        _opCaseRepoMock.Setup(x => x.LoadEntity(It.IsAny<IOrderOpKey>()))
            .Returns<IOrderOpKey>(k => _opCases.TryGetValue(k.OrderOpId, out var m) ? MayBe.From(m) : MayBe<OpCaseModel>.None);
        _opCaseRepoMock.Setup(x => x.ListActiveOpCase())
            .Returns(() => _opCases.Values
                .Where(x => x.OrderOpState != OpCaseStateEnum.Cancelled && x.OrderOpState != OpCaseStateEnum.Discharged)
                .Select(x => new OpCaseOrderView(
                    x.OrderOp.OrderOpId,
                    x.Pasien,
                    x.OperasiName,
                    x.ListPpa.FirstOrDefault(p => p.NoUrut == 1)?.Ppa ?? PpaType.Default.ToReff(),
                    60,
                    x.UrgencyLevel,
                    x.OrderOp.OrderDate,
                    x.OrderOpState)));

        // State-tracking setup for ScheduleOp repository
        _scheduleOpRepoMock.Setup(x => x.SaveChanges(It.IsAny<ScheduleOpModel>()))
            .Callback<ScheduleOpModel>(m => _schedules[m.ScheduleOpId] = m);
        _scheduleOpRepoMock.Setup(x => x.LoadEntity(It.IsAny<IScheduleOpKey>()))
            .Returns<IScheduleOpKey>(k => _schedules.TryGetValue(k.ScheduleOpId, out var m) ? MayBe.From(m) : MayBe<ScheduleOpModel>.None);
        _scheduleOpRepoMock.Setup(x => x.ListData(It.IsAny<IPasienKey>()))
            .Returns<IPasienKey>(p => _schedules.Values
                .Where(s => s.Pasien.PasienId == p.PasienId)
                .Select(s => new ScheduleOpView(
                    s.ScheduleOpId,
                    s.Pasien,
                    s.OrderOp,
                    s.UrgencyLevel,
                    s.TglOp,
                    s.Durasi,
                    s.TeamLead,
                    s.KamarOp,
                    s.OrderOpState,
                    s.AuditTrail.IsVoided,
                    s.TurnoverBuffer))
                .ToList());
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesInRoom(It.IsAny<string>(), It.IsAny<DateTime>())).Returns([]);
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForDoctor(It.IsAny<string>(), It.IsAny<DateTime>())).Returns([]);
        _scheduleOpRepoMock.Setup(x => x.ListActiveSchedulesForPpaInTeam(It.IsAny<string>(), It.IsAny<DateTime>())).Returns([]);

        // Mediator Mock
        _mediatorMock.Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Instantiate handlers sharing the same repository state
        _createOrderHandler = new OkCreateOrderOpByRegHandler(
            _orderOpRepoMock.Object,
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _ppaRepoMock.Object,
            _icdRepoMock.Object,
            _jenisOperasiRepoMock.Object,
            _opCaseRepoMock.Object,
            _tarifRepoMock.Object,
            _tglJamProviderMock.Object);

        _setScheduleHandler = new OkScheduleOpSetCommandHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _kamarRepoMock.Object,
            _ppaRepoMock.Object,
            _opCaseRepoMock.Object,
            _mediatorMock.Object,
            _tglJamProviderMock.Object);

        _assignLeaderHandler = new OkScheduleOpAssignLeaderHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _ppaRepoMock.Object,
            _opCaseRepoMock.Object,
            _tglJamProviderMock.Object);

        _listActiveHandler = new OkListOperasiAktifHandler(
            _orderOpRepoMock.Object,
            _opCaseRepoMock.Object);

        _cancelHandler = new OkScheduleOpCancelHandler(
            _scheduleOpRepoMock.Object,
            _orderOpRepoMock.Object,
            _opCaseRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task DoctorSync_Lifecycle_EndToEnd_MaintainsConsistencyAcrossAllUseCases()
    {
        // -------------------------------------------------------------------------------------------------
        // 1. Penerbitan Order dengan Dr. Martahadinan (DR00083)
        // -------------------------------------------------------------------------------------------------
        var createCmd = new OkCreateOrderOpByRegCmd(
            RegId: "REG-001",
            DokterPengorderId: "DR00083",
            LayananPengorderId: "LYN-001",
            DokterOperatorId: "DR00083",
            DiagCode: "K35.8",
            JenisOperasiId: "JOP-001",
            NamaOperasi: "Apendektomi Laparoskopi",
            ProsedurTambahan: "",
            KlasifikasiOperasi: KlasifikasiOperasiEnum.Sedang,
            AnestesiPlan: AnestesiPlanEnum.General,
            PostOpCareUnit: PostOpCareUnitEnum.PACU,
            TarifId: "TRF-001",
            EstimasiDurasiInMinutes: 60,
            PreferedDate: new DateTime(2026, 10, 25),
            SpecialEquipment: "",
            UserId: "user1",
            IsForceCreate: false,
            ListResource: []);

        var createResponse = await _createOrderHandler.Handle(createCmd, CancellationToken.None);
        var orderOpId = createResponse.OrderOpId;
        orderOpId.Should().NotBe("-");

        _orderOps.Should().ContainKey(orderOpId);
        var orderOp = _orderOps[orderOpId];
        orderOp.DokterOperator.PpaId.Should().Be("DR00083");
        orderOp.DokterOperator.PpaName.Should().Be("dr. Martahadinan, Sp.B");

        var opCase = _opCases[orderOpId];
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Requested);

        // -------------------------------------------------------------------------------------------------
        // 2. Pembuatan Jadwal dengan Dr. Martahadinan (DR00083)
        // -------------------------------------------------------------------------------------------------
        var setCmd = new OkScheduleOpSetCommand(
            OrderOpId: orderOpId,
            KamarId: "KAMAR-OK1",
            Tgl: "2026-10-25",
            Jam: "08:00",
            Durasi: 60,
            UserId: "user1",
            DokterOperatorId: "DR00083");

        var setResponse = await _setScheduleHandler.Handle(setCmd, CancellationToken.None);
        var scheduleOpId = setResponse.ScheduleOpId;
        scheduleOpId.Should().NotBeNullOrWhiteSpace();

        _schedules.Should().ContainKey(scheduleOpId);
        var scheduleOp = _schedules[scheduleOpId];
        scheduleOp.TeamLead.PpaId.Should().Be("DR00083");
        scheduleOp.ListPpa.Should().ContainSingle(p => p.Ppa.PpaId == "DR00083" && p.NoUrut == 1);

        orderOp = _orderOps[orderOpId];
        orderOp.DokterOperator.PpaId.Should().Be("DR00083");
        opCase = _opCases[orderOpId];
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Scheduled);

        // -------------------------------------------------------------------------------------------------
        // 3. Pergantian Operator via AssignLeader ke Dr. Arya Yunan (DR00139)
        //    -> Memverifikasi ScheduleOp.TeamLead dan OrderOp.DokterOperatorId sama-sama menjadi DR00139
        // -------------------------------------------------------------------------------------------------
        var assignLeaderCmd = new OkScheduleOpAssignLeaderCommand(
            OrderOpId: orderOpId,
            PpaId: "DR00139",
            UserId: "user1",
            AlasanPerubahan: "Dr. Martahadinan berhalangan, dialihkan ke Dr. Arya Yunan");

        await _assignLeaderHandler.Handle(assignLeaderCmd, CancellationToken.None);

        scheduleOp = _schedules[scheduleOpId];
        scheduleOp.TeamLead.PpaId.Should().Be("DR00139");
        scheduleOp.TeamLead.PpaName.Should().Be("dr. Arya Yunan, Sp.B");
        scheduleOp.ListPpa.First(p => p.NoUrut == 1).Ppa.PpaId.Should().Be("DR00139");
        scheduleOp.ListPpa.Should().Contain(p => p.Ppa.PpaId == "DR00083" && p.NoUrut >= 2);

        orderOp = _orderOps[orderOpId];
        orderOp.DokterOperator.PpaId.Should().Be("DR00139");
        orderOp.DokterOperator.PpaName.Should().Be("dr. Arya Yunan, Sp.B");

        // -------------------------------------------------------------------------------------------------
        // 4. Eksekusi OkListOperasiAktifQuery -> Mengembalikan DokterOperatorId = DR00139 (Zero Split-Brain Screen)
        // -------------------------------------------------------------------------------------------------
        var activeList = (await _listActiveHandler.Handle(new OkListOperasiAktifQuery(), CancellationToken.None)).ToList();
        var listItem = activeList.Should().ContainSingle(x => x.OrderOpId == orderOpId).Subject;
        listItem.DokterOperatorId.Should().Be("DR00139");
        listItem.DokterOperatorName.Should().Be("dr. Arya Yunan, Sp.B");
        listItem.DokterId.Should().Be("DR00139");
        listItem.DokterName.Should().Be("dr. Arya Yunan, Sp.B");

        // -------------------------------------------------------------------------------------------------
        // 5. Eksekusi Reschedule tanpa parameter dokter -> Memverifikasi dokter tetap DR00139 (Zero Silent Reversion)
        // -------------------------------------------------------------------------------------------------
        var rescheduleCmd = new OkScheduleOpSetCommand(
            OrderOpId: orderOpId,
            KamarId: "KAMAR-OK1",
            Tgl: "2026-10-25",
            Jam: "10:30",
            Durasi: 60,
            UserId: "user1",
            DokterOperatorId: "", // Dokter tidak disertakan saat reschedule
            AlasanPerubahan: "Pergeseran jadwal ke jam 10:30");

        var rescheduleResponse = await _setScheduleHandler.Handle(rescheduleCmd, CancellationToken.None);
        rescheduleResponse.ScheduleOpId.Should().Be(scheduleOpId);

        scheduleOp = _schedules[scheduleOpId];
        scheduleOp.TeamLead.PpaId.Should().Be("DR00139");
        scheduleOp.TglOp.ToString("HH:mm").Should().Be("10:30");

        orderOp = _orderOps[orderOpId];
        orderOp.DokterOperator.PpaId.Should().Be("DR00139");
        orderOp.DokterOperator.PpaName.Should().Be("dr. Arya Yunan, Sp.B");

        // -------------------------------------------------------------------------------------------------
        // 6. Eksekusi CancelSchedule -> Memverifikasi status OpCase menjadi Requested namun dokter pada OrderOp tetap DR00139
        // -------------------------------------------------------------------------------------------------
        var cancelCmd = new OkScheduleOpCancelCommand(
            OrderOpId: orderOpId,
            UserId: "user1",
            AlasanBatal: "Kondisi pasien tidak stabil, operasi ditunda");

        await _cancelHandler.Handle(cancelCmd, CancellationToken.None);

        scheduleOp = _schedules[scheduleOpId];
        scheduleOp.AuditTrail.IsVoided.Should().BeTrue();
        scheduleOp.AlasanBatal.Should().Be("Kondisi pasien tidak stabil, operasi ditunda");

        opCase = _opCases[orderOpId];
        opCase.OrderOpState.Should().Be(OpCaseStateEnum.Requested);

        orderOp = _orderOps[orderOpId];
        orderOp.DokterOperator.PpaId.Should().Be("DR00139");
        orderOp.DokterOperator.PpaName.Should().Be("dr. Arya Yunan, Sp.B");
    }

    private static KamarType CreateSampleKamar(string kamarId = "KAMAR-OK1", string kamarName = "OK 01")
    {
        return new KamarType(
            kamarId,
            kamarName,
            new BangsalReff("B-01", "Gedung Bedah"),
            new KelasReff("K-01", "Kelas 1"));
    }

    private static PpaType CreateSampleDoctor(string ppaId = "DR00083", string ppaName = "dr. Martahadinan, Sp.B")
    {
        var satTugas = new SatTugasType("ST-1", "Spesialis Bedah", ProfesiType.Dokter);
        var ppaSatTugas = new PpaSatTugasType(satTugas, true);
        return new PpaType(
            ppaId,
            ppaName,
            ppaName,
            SmfType.Default,
            GroupSpesialisType.Default,
            [],
            [ppaSatTugas],
            []);
    }

    private static RegModel CreateSampleReg(string id = "REG-001")
    {
        return new RegModel(
            id,
            new DateOnly(2026, 10, 22),
            new AuditInfoType("user-reg", new DateTime(2026, 10, 22)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff("PAS-001", "Tn. Pasien Tes", new DateOnly(1985, 5, 20), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);
    }
}
