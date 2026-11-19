using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.PaymentContext.TataRekeningFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.AlokasiPembayaranFeature;

public class AllocateFinalizeHandlerTest
{
    private const string RegId = "REG-FINALIZE-001";
    private const string PetugasVerif = "USER-01";
    private static readonly DateTime TestDate = new(2026, 6, 16, 10, 0, 0);

    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IDepositRepo> _depositRepo = new();
    private readonly Mock<IRegJaminanPolicy> _regJaminanPolicy = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();
    private readonly Mock<IRegBiayaRepo> _regBiayaRepo = new();
    private readonly Mock<ITrsDirectDal> _trsDirectDal = new();
    private readonly Mock<IJurnalDal> _jurnalDal = new();
    private readonly Mock<IRegAktifRepo> _regAktifRepo = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();

    private readonly AllocateFinalizeHandler _sut;

    public AllocateFinalizeHandlerTest()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);

        _sut = new AllocateFinalizeHandler(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            _depositRepo.Object,
            _regJaminanPolicy.Object,
            _regRepo.Object,
            _unitOfWork.Object,
            TestTglJamProvider.Instance,
            _regBiayaRepo.Object,
            _trsDirectDal.Object,
            _jurnalDal.Object,
            _regAktifRepo.Object,
            _tipeJaminanRepo.Object,
            _jaminanRepo.Object);
    }

    private static TrsBillType CreateBill(string billId, string regId, decimal amount, BillModulGroup modulGroup = BillModulGroup.Jasa)
    {
        var komponen = new TrsBill2KomponenType("KOMP-01", "Komponen Test");
        var validPdpCoa = new TrsBill2CoaType(
            new CoaType("PPDP-01", ""),
            new CoaType("PDPT-01", ""),
            CoaType.Default,
            CoaType.Default,
            CoaType.Default,
            CoaType.Default);

        var trans = TrsBill2TransEventType.Create(
            0, komponen, TrsBillJenisBayarType.Pdp, amount,
            PpaType.Default.ToReff(), validPdpCoa);

        return new TrsBillType(
            billId, modulGroup, TestDate,
            new RegReff(regId, "-", "-"),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            AuditInfoType.Default,
            RekapCetakType.Default.ToReff(),
            new TrsBillNilaiType(amount, 0, 0, 0),
            new TrsBillKetType("Test", "", "REF", 1, ""),
            [trans], [], []);
    }

    [Fact]
    public async Task GivenInsuranceAllocationWithoutCoaId_WhenAllocateFinalize_ThenResolvesCoaFromTipeJaminanAndJaminanRepoAndDeactivatesReg()
    {
        // Arrange
        var billJasa = CreateBill("BILL-JASA-01", RegId, 100_000m, BillModulGroup.Jasa);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var reg = RegModel.Default;
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        _regJaminanPolicy.Setup(p => p.IsJaminanValidForReg(RegId, "BPJS"))
            .Returns(true);

        var tipeJaminan = TipeJaminanType.Default with
        {
            TipeJaminanId = "BPJS",
            TipeJaminanName = "BPJS Kesehatan",
            Jaminan = new JaminanReff("JAM-BPJS", "BPJS Pusat")
        };
        _tipeJaminanRepo.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "BPJS")))
            .Returns(MayBe.From(tipeJaminan));

        var jaminan = JaminanType.Default with
        {
            JaminanId = "JAM-BPJS",
            JaminanName = "BPJS Pusat",
            Rekening = JaminanRekeningType.Default with
            {
                PiutangPulang = new CoaType("1322000", "Piutang BPJS Kesehatan")
            }
        };
        _jaminanRepo.Setup(r => r.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == "JAM-BPJS")))
            .Returns(MayBe.From(jaminan));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BPJS", "BPJS Kesehatan", true, 100_000m, 0m)
        };

        var command = new AllocateFinalizeCommand(RegId, payments, PetugasVerif, TestDate);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Finalized);
        var bpjsPayment = tataRekening.ListPayment.First(p => p.Payment.PaymentId == "BPJS");
        bpjsPayment.Coa.CoaId.Should().Be("1322000");
        bpjsPayment.Coa.CoaName.Should().Be("Piutang BPJS Kesehatan");

        _tipeJaminanRepo.Verify(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "BPJS")), Times.Once);
        _jaminanRepo.Verify(r => r.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == "JAM-BPJS")), Times.Once);
        _regAktifRepo.Verify(r => r.Delete(It.Is<IRegKey>(k => k.RegId == RegId)), Times.Once);
        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenInsuranceAllocationWithoutCoaId_WhenTipeJaminanNotFound_ThenFallsBackToDefaultInsuranceCoa()
    {
        // Arrange
        var billJasa = CreateBill("BILL-JASA-02", RegId, 100_000m, BillModulGroup.Jasa);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var reg = RegModel.Default;
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        _regJaminanPolicy.Setup(p => p.IsJaminanValidForReg(RegId, "ADM01"))
            .Returns(true);

        _tipeJaminanRepo.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "ADM01")))
            .Returns(MayBe<TipeJaminanType>.None);

        var payments = new List<PaymentAllocationInputDto>
        {
            new("ADM01", "Asuransi Mandiri", true, 100_000m, 0m)
        };

        var command = new AllocateFinalizeCommand(RegId, payments, PetugasVerif, TestDate);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Finalized);
        var admPayment = tataRekening.ListPayment.First(p => p.Payment.PaymentId == "ADM01");
        admPayment.Coa.CoaId.Should().Be("1323000");
        admPayment.Coa.CoaName.Should().Be("Piutang Jaminan");
    }

    [Fact]
    public async Task GivenMultipleStandardPaymentsWithoutCoaId_WhenAllocateFinalize_ThenResolvesStandardCoasAutomatically()
    {
        // Arrange: Jasa 150_000, Obat 50_000 -> Total 200_000
        var billJasa = CreateBill("BILL-JASA-03", RegId, 150_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT-03", RegId, 50_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var reg = RegModel.Default;
        _regRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(reg));

        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 50_000m, 0m, 50_000m));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit Umum", false, 50_000m, 0m),
            new("BYPRI", "Hutang Pribadi", false, 50_000m, 20_000m),
            new("BYKAS", "Kasir (Bayar Sendiri)", false, 50_000m, 30_000m)
        };

        var command = new AllocateFinalizeCommand(RegId, payments, PetugasVerif, TestDate);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Status.Should().Be(TataRekeningStatusEnum.Finalized);

        var dpuPayment = tataRekening.ListPayment.First(p => p.Payment.PaymentId == "BYDPU");
        dpuPayment.Coa.CoaId.Should().Be("4210000");

        var priPayment = tataRekening.ListPayment.First(p => p.Payment.PaymentId == "BYPRI");
        priPayment.Coa.CoaId.Should().Be("1321000");

        var kasPayment = tataRekening.ListPayment.First(p => p.Payment.PaymentId == "BYKAS");
        kasPayment.Coa.CoaId.Should().Be("1499901");
    }
}
