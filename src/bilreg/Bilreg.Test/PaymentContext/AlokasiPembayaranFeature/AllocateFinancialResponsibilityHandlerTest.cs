using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
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

public class AllocateFinancialResponsibilityHandlerTest
{
    private const string RegId = "REG-ALLOC-001";
    private const string PetugasVerif = TataRekeningDomainTestHelper.DefaultPetugasVerif;
    private static readonly DateTime TestDate = TataRekeningTestDataBuilder.TestDate;

    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ITrsBillingRepo> _trsBillingRepo = new();
    private readonly Mock<IDepositRepo> _depositRepo = new();
    private readonly Mock<IRegJaminanPolicy> _regJaminanPolicy = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _unitOfWorkScope = new();
    private readonly Mock<ITipeJaminanRepo> _tipeJaminanRepo = new();
    private readonly Mock<IJaminanRepo> _jaminanRepo = new();

    private readonly AllocateFinancialResponsibilityHandler _sut;

    public AllocateFinancialResponsibilityHandlerTest()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_unitOfWorkScope.Object);

        _sut = new AllocateFinancialResponsibilityHandler(
            _tataRekeningRepo.Object,
            _trsBillingRepo.Object,
            _depositRepo.Object,
            _regJaminanPolicy.Object,
            _unitOfWork.Object,
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

    private static DepositModel CreateDeposit(string depositId, string regId, decimal amount, bool isVoided = false)
    {
        var audit = AuditTrailType.Create("tester", TestDate);
        if (isVoided)
        {
            audit.Batal("void-user", TestDate.AddHours(2));
        }

        return new DepositModel(
            depositId,
            TestDate,
            new RegReff(regId, "-", "-"),
            LayananType.Default.ToReff(),
            "Deposit Test",
            amount,
            audit);
    }

    #region a. Sukses Alokasi

    [Fact]
    public async Task GivenClosedAndVerifiedTataRekening_WhenAllocateCashPayment_ThenSucceedsAndPersists()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m, BillModulGroup.Jasa);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(1);
        result.Projection[0].NilaiJasa.Should().Be(100_000m);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(bill), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenClosedAndVerifiedTataRekening_WhenAllocateCashPaymentWithEmptyOrByKasCoaId_ThenFallsBackTo1499901AndPersists()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m, BillModulGroup.Jasa);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();

        _tataRekeningRepo.Verify(r => r.SaveChanges(It.Is<TataRekeningModel>(m =>
            m.ListPayment.Any(p =>
                p.Payment.PaymentId == "BYKAS" &&
                p.Coa.CoaId == "1499901" &&
                p.Coa.CoaName == "Rekening Piutang Kasir Perantara"))), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(bill), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenClosedAndVerifiedTataRekening_WhenAllocateWithValidInsuranceAndCash_ThenSucceeds()
    {
        // Arrange: Jasa 150_000, Obat 50_000 -> Total 200_000
        var billJasa = CreateBill("BILL-JASA", RegId, 150_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 50_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Insurance valid
        _regJaminanPolicy.Setup(p => p.IsJaminanValidForReg(RegId, "BPJS"))
            .Returns(true);

        // Payments:
        // BPJS (IsTipeJaminan: true): Jasa 100_000, Obat 30_000
        // BYKAS (Cash): Jasa 50_000, Obat 20_000
        // Total Jasa = 150_000, Total Obat = 50_000 (100% exact match)
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BPJS", "BPJS Kesehatan", true, 100_000m, 30_000m),
            new("BYKAS", "KAS", false, 50_000m, 20_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(2);
        _regJaminanPolicy.Verify(p => p.IsJaminanValidForReg(RegId, "BPJS"), Times.Once);
        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billJasa), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billObat), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenInsuranceAllocationWithoutCoaId_WhenTipeJaminanAndJaminanFound_ThenResolvesCoaFromPiutangPulang()
    {
        // Arrange
        var billJasa = CreateBill("BILL-JASA-COA", RegId, 100_000m, BillModulGroup.Jasa);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        _regJaminanPolicy.Setup(p => p.IsJaminanValidForReg(RegId, "BPJS"))
            .Returns(true);

        var tipeJaminan = TipeJaminanType.Default with
        {
            TipeJaminanId = "BPJS",
            TipeJaminanName = "BPJS Kesehatan",
            Jaminan = new JaminanReff("JAM-BPJS", "BPJS Sehat")
        };
        _tipeJaminanRepo.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "BPJS")))
            .Returns(MayBe.From(tipeJaminan));

        var jaminan = JaminanType.Default with
        {
            JaminanId = "JAM-BPJS",
            JaminanName = "BPJS Sehat",
            Rekening = JaminanRekeningType.Default with
            {
                PiutangPulang = new CoaType("1322000", "Piutang BPJS")
            }
        };
        _jaminanRepo.Setup(r => r.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == "JAM-BPJS")))
            .Returns(MayBe.From(jaminan));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BPJS", "BPJS Kesehatan", true, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        var bpjsPayment = tataRekening.ListPayment.First(p => p.Payment.PaymentId == "BPJS");
        bpjsPayment.Coa.CoaId.Should().Be("1322000");
        bpjsPayment.Coa.CoaName.Should().Be("Piutang BPJS");
        _tipeJaminanRepo.Verify(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "BPJS")), Times.Once);
        _jaminanRepo.Verify(r => r.LoadEntity(It.Is<IJaminanKey>(k => k.JaminanId == "JAM-BPJS")), Times.Once);
    }

    [Fact]
    public async Task GivenInsuranceAllocationWithoutCoaId_WhenTipeJaminanNotFound_ThenFallsBackToDefaultInsuranceCoa()
    {
        // Arrange
        var billJasa = CreateBill("BILL-JASA-COA2", RegId, 100_000m, BillModulGroup.Jasa);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        _regJaminanPolicy.Setup(p => p.IsJaminanValidForReg(RegId, "ASR01"))
            .Returns(true);

        _tipeJaminanRepo.Setup(r => r.LoadEntity(It.Is<ITipeJaminanKey>(k => k.TipeJaminanId == "ASR01")))
            .Returns(MayBe<TipeJaminanType>.None);

        var payments = new List<PaymentAllocationInputDto>
        {
            new("ASR01", "Asuransi Lain", true, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var asrPayment = tataRekening.ListPayment.First(p => p.Payment.PaymentId == "ASR01");
        asrPayment.Coa.CoaId.Should().Be("1323000");
        asrPayment.Coa.CoaName.Should().Be("Piutang Jaminan");
    }



    [Fact]
    public async Task GivenFullDepositAllocation_WhenAllocate_ThenSucceedsAndPersists()
    {
        // Arrange: Jasa 500_000, Obat 250_000 -> Total 750_000
        var billJasa = CreateBill("BILL-JASA", RegId, 500_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 250_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Active deposit balance: 500_000
        var deposit = CreateDeposit("DP01", RegId, 500_000m);
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 500_000m, 0m, 500_000m));

        // Full deposit absorption: BYDPU 500_000 (Jasa 300_000, Obat 200_000)
        // Cashier balance: BYKAS 250_000 (Jasa 200_000, Obat 50_000)
        // Total Jasa = 500_000, Total Obat = 250_000 (100% exact match)
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit", false, 300_000m, 200_000m),
            new("BYKAS", "KAS", false, 200_000m, 50_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(2);

        var dpuProjection = result.Projection.First(p => p.PaymentId == "BYDPU");
        dpuProjection.NilaiJasa.Should().Be(300_000m);
        dpuProjection.NilaiObat.Should().Be(200_000m);

        var kasProjection = result.Projection.First(p => p.PaymentId == "BYKAS");
        kasProjection.NilaiJasa.Should().Be(200_000m);
        kasProjection.NilaiObat.Should().Be(50_000m);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billJasa), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billObat), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }



    [Fact]
    public async Task GivenPartialDepositAllocation_WhenAllocate_ThenSucceedsAndPersists()
    {
        // Arrange: Jasa 500_000, Obat 250_000 -> Total 750_000
        var billJasa = CreateBill("BILL-JASA", RegId, 500_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 250_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Active deposit balance: 500_000
        var deposit = CreateDeposit("DP01", RegId, 500_000m);
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 500_000m, 0m, 500_000m));

        // Partial deposit allocation: 400_000 (Jasa 250_000, Obat 150_000) <= Saldo Aktif (500_000) & <= Total Tagihan (750_000)
        // Cashier balance: 350_000 (Jasa 250_000, Obat 100_000)
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit", false, 250_000m, 150_000m),
            new("BYKAS", "KAS", false, 250_000m, 100_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert: partial deposit allocation succeeds
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(2);

        var dpuProjection = result.Projection.First(p => p.PaymentId == "BYDPU");
        dpuProjection.NilaiJasa.Should().Be(250_000m);
        dpuProjection.NilaiObat.Should().Be(150_000m);

        var kasProjection = result.Projection.First(p => p.PaymentId == "BYKAS");
        kasProjection.NilaiJasa.Should().Be(250_000m);
        kasProjection.NilaiObat.Should().Be(100_000m);

        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billJasa), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billObat), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenDepositGreaterThanTotalBill_WhenAllocateDepositCappedToBill_ThenSucceedsWithoutException()
    {
        // Arrange: Jasa 2_000_000, Obat 1_000_000 -> Total Tagihan 3_000_000
        var billJasa = CreateBill("BILL-JASA", RegId, 2_000_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 1_000_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Saldo aktif deposit: 10_000_000 > Total Tagihan 3_000_000
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 10_000_000m, 0m, 10_000_000m));

        // Verifikator mengalokasikan deposit tepat sebesar tagihan (3_000_000): Jasa 2_000_000, Obat 1_000_000
        // Sisa saldo deposit (7_000_000) tetap aktif di akun deposit pasien
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit Pasien", false, 2_000_000m, 1_000_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert: partial allocation succeeds without exception
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(1);
        result.Projection[0].NilaiJasa.Should().Be(2_000_000m);
        result.Projection[0].NilaiObat.Should().Be(1_000_000m);

        _depositRepo.Verify(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)), Times.Once);
        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billJasa), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billObat), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenExcessDepositWithInsuranceExceedingKurangBayar_WhenAllocate_ThenThrowsArgumentException()
    {
        // Arrange: Jasa 1_500_000, Obat 500_000 -> Total Tagihan 2_000_000
        var billJasa = CreateBill("BILL-JASA", RegId, 1_500_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 500_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        _regJaminanPolicy.Setup(p => p.IsJaminanValidForReg(RegId, "BPJS"))
            .Returns(true);

        // Saldo aktif deposit: 1_000_000
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 1_000_000m, 0m, 1_000_000m));

        // Tagihan = 2_000_000, Deposit = 1_000_000, Kasir Refund = -500_000.
        // netDpuApplied = 1_000_000 - 500_000 = 500_000.
        // Sisa Kurang Bayar = 2_000_000 - 500_000 = 1_500_000.
        // BPJS = 1_600_000 melebihi Kurang Bayar (1_500_000) sehingga harus ditolak sesuai TD-01.
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BPJS", "BPJS Kesehatan", true, 1_300_000m, 300_000m),
            new("BYDPU", "Deposit Pasien", false, 600_000m, 400_000m),
            new("BYKAS", "Kasir Refund", false, -300_000m, -200_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Total alokasi Hutang dan Jaminan*tidak boleh melebihi sisa Kurang Bayar*");
    }

    [Fact]
    public async Task GivenExcessBydpuOverTotalBillWithNegativeBykas_WhenAllocate_ThenSucceedsAndGeneratesProjection()
    {
        // Arrange: Jasa 150_000, Obat 50_000 -> Total Tagihan 200_000
        var billJasa = CreateBill("BILL-JASA", RegId, 150_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 50_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Saldo aktif deposit: 500_000
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 500_000m, 0m, 500_000m));

        // BYDPU = 300_000 > totalTagihan (200_000), BYKAS = -100_000
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit Pasien", false, 200_000m, 100_000m),
            new("BYKAS", "Kasir (Bayar Sendiri)", false, -50_000m, -50_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(2);

        var netJasa = result.Projection.Sum(p => p.NilaiJasa);
        var netObat = result.Projection.Sum(p => p.NilaiObat);
        netJasa.Should().Be(150_000m);
        netObat.Should().Be(50_000m);

        _depositRepo.Verify(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)), Times.Once);
        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billJasa), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billObat), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }

    [Fact]
    public async Task GivenDepositWithPriorUsage_WhenAllocateFullActiveSaldo_ThenSucceedsAndMatchesSaldoAktif()
    {
        // Arrange: Jasa 400_000, Obat 200_000 -> Total 600_000
        var billJasa = CreateBill("BILL-JASA", RegId, 400_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 200_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Prior setoran: 1_000_000, used: 400_000 -> SaldoAktif: 600_000
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 1_000_000m, 400_000m, 600_000m));

        // BYDPU absorbs exact active saldo: 600_000
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit Pasien", false, 400_000m, 200_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
        result.Projection.Should().HaveCount(1);
        result.Projection[0].NilaiJasa.Should().Be(400_000m);
        result.Projection[0].NilaiObat.Should().Be(200_000m);

        _depositRepo.Verify(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)), Times.Once);
        _tataRekeningRepo.Verify(r => r.SaveChanges(tataRekening), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billJasa), Times.Once);
        _trsBillingRepo.Verify(r => r.SaveChanges(billObat), Times.Once);
        _unitOfWorkScope.Verify(s => s.Complete(), Times.Once);
    }



    #endregion

    #region b. Gagal Status Bukan Closed

    [Fact]
    public async Task GivenOpenedTataRekening_WhenAllocate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CLOSED*");
    }

    [Fact]
    public async Task GivenFinalizedTataRekening_WhenAllocate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.Hydrate(RegId, TataRekeningStatusEnum.Opened, bill);
        TataRekeningDomainTestHelper.CloseVerifyAllocateFinalize(
            tataRekening,
            [new TataRekeningPaymentType(PaymentType.ByKas, 100_000m, 0m, CoaType.Default)],
            PetugasVerif);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CLOSED*");
    }

    [Fact]
    public async Task GivenNonExistentTataRekening_WhenAllocate_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe<TataRekeningModel>.None);

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{RegId}*");
    }

    #endregion

    #region c. Gagal Alokasi Deposit (Over-Allocation)

    [Fact]
    public async Task GivenDepositAllocationExceedsSaldo_WhenAllocate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Active deposit balance: 30_000
        var deposit = CreateDeposit("DP01", RegId, 30_000m);
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 30_000m, 0m, 30_000m));

        // Requested deposit: 50_000 (exceeds 30_000)
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit", false, 50_000m, 0m),
            new("BYKAS", "KAS", false, 50_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert: Invariant 1 violation
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Alokasi deposit (50,000) melebihi saldo deposit aktif yang tersedia (30,000).*");
    }

    [Fact]
    public async Task GivenVoidedDepositsExcludedFromSaldo_WhenAllocateDepositExceedsActive_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Active deposit: 20_000, Voided deposit: 80_000 (Total non-voided = 20_000)
        var activeDeposit = CreateDeposit("DP01", RegId, 20_000m, isVoided: false);
        var voidedDeposit = CreateDeposit("DP02", RegId, 80_000m, isVoided: true);
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 100_000m, 0m, 20_000m));

        // Requested BYDPK: 50_000 (exceeds active 20_000)
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPK", "Deposit Khusus", false, 50_000m, 0m),
            new("BYKAS", "KAS", false, 50_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert: Invariant 1 violation
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Alokasi deposit (50,000) melebihi saldo deposit aktif yang tersedia (20,000).*");
    }

    [Fact]
    public async Task GivenZeroDepositAvailable_WhenAllocateDeposit_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 0m, 0m, 0m));

        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit", false, 50_000m, 0m),
            new("BYKAS", "KAS", false, 50_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert: Invariant 1 violation
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Alokasi deposit (50,000) melebihi saldo deposit aktif yang tersedia (0).*");
    }

    [Fact]
    public async Task GivenDepositKhususExceedsTotalBill_WhenAllocate_ThenThrowsInvalidOperationException()
    {
        // Arrange: Jasa 500_000, Obat 250_000 -> Total Tagihan 750_000
        var billJasa = CreateBill("BILL-JASA", RegId, 500_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 250_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Active deposit balance: 1_000_000
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 1_000_000m, 0m, 1_000_000m));

        // Requested BYDPK: 1_000_000 (exceeds total bill 750_000)
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPK", "Deposit Khusus", false, 650_000m, 350_000m),
            new("BYKAS", "Kasir Refund", false, -150_000m, -100_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert: Invariant 2 violation for deposit khusus
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Alokasi deposit khusus (1,000,000) melebihi total tagihan kunjungan (750,000).*");
    }

    [Fact]
    public async Task GivenDepositGreaterThanTotalBill_WhenAllocateDepositExceedingBillWithRefund_ThenSucceeds()
    {
        // Arrange: Jasa 800_000, Obat 400_000 -> Total Tagihan 1_200_000
        var billJasa = CreateBill("BILL-JASA", RegId, 800_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 400_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Saldo aktif deposit: 2_000_000
        _depositRepo.Setup(r => r.GetSaldo(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(new DepositSaldoModel(RegId, 2_500_000m, 500_000m, 2_000_000m));

        // Requested BYDPU: 2_000_000 (exceeds total bill 1_200_000), BYKAS: -800_000 (Net 1_200_000)
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYDPU", "Deposit Pasien", false, 1_300_000m, 700_000m),
            new("BYKAS", "Kasir Refund", false, -500_000m, -300_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.IsFinancialResponsibilityAllocated.Should().BeTrue();
    }

    #endregion

    #region d. Gagal Penjamin Ditolak Policy

    [Fact]
    public async Task GivenUnregisteredInsurance_WhenAllocate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Insurance rejected by policy
        _regJaminanPolicy.Setup(p => p.IsJaminanValidForReg(RegId, "ASR-UNKNOWN"))
            .Returns(false);

        var payments = new List<PaymentAllocationInputDto>
        {
            new("ASR-UNKNOWN", "Asuransi Asing", true, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Penjamin 'Asuransi Asing' (ASR-UNKNOWN) tidak terdaftar pada registrasi '{RegId}'.*");
    }

    #endregion

    #region e. Gagal Total Alokasi Tidak Seimbang (Invarian 100% Exact Match)

    [Fact]
    public async Task GivenJasaAllocationDoesNotMatchBillTotal_WhenAllocate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m, BillModulGroup.Jasa);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Jasa is 100_000, but payment is 80_000
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 80_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Total Jasa pada payment*tidak sama dengan total Jasa pada transaksi*");
    }

    [Fact]
    public async Task GivenObatAllocationDoesNotMatchBillTotal_WhenAllocate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var billJasa = CreateBill("BILL-JASA", RegId, 50_000m, BillModulGroup.Jasa);
        var billObat = CreateBill("BILL-OBAT", RegId, 50_000m, BillModulGroup.Obat);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, billJasa, billObat);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        // Jasa matches (50_000), but Obat payment is 70_000 instead of 50_000
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 50_000m, 70_000m)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Total Obat pada payment*tidak sama dengan total Obat pada transaksi*");
    }

    #endregion

    #region f. Gagal Nilai Alokasi Negatif Selain Kasir

    [Theory]
    [InlineData("BPJS", "BPJS Kesehatan", true, -50_000, 50_000)]
    [InlineData("BYDPU", "Deposit Pasien", false, -50_000, 50_000)]
    [InlineData("ASR-01", "Asuransi Swasta", true, 50_000, -20_000)]
    public async Task GivenNegativeValueOnGuarantorOrDeposit_WhenAllocate_ThenThrowsInvalidOperationException(
        string paymentId, string paymentName, bool isTipeJaminan, decimal nilaiJasa, decimal nilaiObat)
    {
        // Arrange
        var bill = CreateBill("BILL-01", RegId, 100_000m);
        var tataRekening = TataRekeningTestDataBuilder.HydrateClosed(RegId, bill);
        tataRekening.CompleteFinancialVerification(PetugasVerif, TestDate);

        _tataRekeningRepo.Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == RegId)))
            .Returns(MayBe.From(tataRekening));

        var payments = new List<PaymentAllocationInputDto>
        {
            new(paymentId, paymentName, isTipeJaminan, nilaiJasa, nilaiObat)
        };

        var command = new AllocateFinancialResponsibilityCommand(RegId, payments);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Nilai alokasi negatif hanya diizinkan untuk tipe pembayaran Kasir (BYKAS), ditemukan pada '{paymentName}' ({paymentId}).*");
    }

    #endregion

    #region Guard Clause Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenInvalidRegId_WhenAllocate_ThenThrowsArgumentException(string? invalidRegId)
    {
        var payments = new List<PaymentAllocationInputDto>
        {
            new("BYKAS", "KAS", false, 100_000m, 0m)
        };

        var command = new AllocateFinancialResponsibilityCommand(invalidRegId!, payments);

        var act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenNullPayments_WhenAllocate_ThenThrowsArgumentNullException()
    {
        var command = new AllocateFinancialResponsibilityCommand(RegId, null!);

        var act = () => _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion
}
