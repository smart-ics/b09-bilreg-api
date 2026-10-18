using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueWorklistHandlerTest
{
    private readonly Mock<IApotekQueueEntryRepo> _queueEntryRepo = new();
    private readonly Mock<IPharmacyServicePointRepo> _servicePointRepo = new();
    private readonly Mock<IPharmacyQueueMappingRepo> _mappingRepo = new();
    private readonly Mock<IResepRepo> _resepRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ITelaahResepRepo> _telaahRepo = new();

    private readonly DateTime _testTime = new(2026, 10, 18, 10, 0, 0);

    private PharmacyQueueWorklistHandler CreateSut(bool includeEnrichment = false)
    {
        if (includeEnrichment)
        {
            return new PharmacyQueueWorklistHandler(
                _queueEntryRepo.Object,
                _servicePointRepo.Object,
                _mappingRepo.Object,
                _resepRepo.Object,
                _regRepo.Object,
                _telaahRepo.Object);
        }

        return new PharmacyQueueWorklistHandler(
            _queueEntryRepo.Object,
            _servicePointRepo.Object,
            _mappingRepo.Object);
    }

    [Fact]
    public async Task Handle_Throws_When_DepoFarmasiId_NullOrWhiteSpace()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(new PharmacyQueueWorklistQry(""), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_Returns_All_Active_Entries_With_Summary_Counts()
    {
        // Arrange
        var sp1 = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Resep Non-Racik", "A");
        var sp2 = PharmacyServicePointModel.Create("SP-02", "DEPO-RJ", "Resep Racikan", "B");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp1, sp2]);

        var itemTaken = ApotekQueueEntryModel.Create("ANT-1", 1, "SP-01", "A", _testTime.AddMinutes(-20));
        var itemServed = ApotekQueueEntryModel.Create("ANT-1", 2, "SP-01", "A", _testTime.AddMinutes(-15));
        itemServed.StartServicePenerimaan(_testTime.AddMinutes(-10));
        var itemReady = ApotekQueueEntryModel.Create("ANT-1", 3, "SP-02", "B", _testTime.AddMinutes(-12));
        itemReady.TransitionToPenyiapan();
        itemReady.ConsolidateToSiapSerah(_testTime.AddMinutes(-2));

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([itemTaken, itemServed, itemReady]);
        _mappingRepo.Setup(x => x.ListByQueue(It.IsAny<string>(), It.IsAny<int>())).Returns([]);

        var sut = CreateSut();

        // Act
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.DepoFarmasiId.Should().Be("DEPO-RJ");
        result.TotalCount.Should().Be(3);
        result.WaitingCount.Should().Be(1);
        result.PreparedCount.Should().Be(1);
        result.Items.Should().HaveCount(3);
        result.Items.Select(x => x.QueueLabel).Should().BeEquivalentTo(["A001", "A002", "B003"]);
    }

    [Fact]
    public async Task Handle_Filters_By_Tab_Menunggu()
    {
        var sp1 = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racik", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp1]);

        var itemTaken = ApotekQueueEntryModel.Create("ANT-1", 1, "SP-01", "A", _testTime);
        var itemServed = ApotekQueueEntryModel.Create("ANT-1", 2, "SP-01", "A", _testTime);
        itemServed.StartServicePenerimaan(_testTime);

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([itemTaken, itemServed]);
        _mappingRepo.Setup(x => x.ListByQueue(It.IsAny<string>(), It.IsAny<int>())).Returns([]);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ", FilterTab: "Menunggu"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].ApotekPhase.Should().Be(ApotekPhaseEnum.TAKEN);
        result.Items[0].QueueLabel.Should().Be("A001");
    }

    [Fact]
    public async Task Handle_Filters_By_Tab_MenungguTelaah()
    {
        var sp1 = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racik", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp1]);

        var itemTaken = ApotekQueueEntryModel.Create("ANT-1", 1, "SP-01", "A", _testTime);
        var itemServed = ApotekQueueEntryModel.Create("ANT-1", 2, "SP-01", "A", _testTime);
        itemServed.StartServicePenerimaan(_testTime);

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([itemTaken, itemServed]);
        _mappingRepo.Setup(x => x.ListByQueue(It.IsAny<string>(), It.IsAny<int>())).Returns([]);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ", FilterTab: "Menunggu Telaah"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].ApotekPhase.Should().Be(ApotekPhaseEnum.PENERIMAAN);
        result.Items[0].QueueLabel.Should().Be("A002");
    }

    [Fact]
    public async Task Handle_Filters_By_Tab_Dilewati()
    {
        var sp1 = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racik", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp1]);

        var item1 = ApotekQueueEntryModel.Create("ANT-1", 1, "SP-01", "A", _testTime);
        var item2 = ApotekQueueEntryModel.Create("ANT-1", 2, "SP-01", "A", _testTime);
        item2.Skip(LoketTypeEnum.PenerimaanResep, _testTime);

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([item1, item2]);
        _mappingRepo.Setup(x => x.ListByQueue(It.IsAny<string>(), It.IsAny<int>())).Returns([]);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ", FilterTab: "Dilewati"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].ApotekPhase.Should().Be(ApotekPhaseEnum.DILEWATI);
        result.Items[0].QueueLabel.Should().Be("A002");
    }

    [Fact]
    public async Task Handle_Filters_By_Tab_SiapPenjualan_When_Telaah_Approved()
    {
        var sp1 = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racik", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp1]);

        var item = ApotekQueueEntryModel.Create("ANT-1", 5, "SP-01", "A", _testTime);
        item.StartServicePenerimaan(_testTime);

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([item]);

        // Map demand to prescription KP001
        var mapping = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP001", "ANT-1", 5, "TRK-01", QueueMappingMethodEnum.Manual, "user1", _testTime);
        _mappingRepo.Setup(x => x.ListByQueue("ANT-1", 5)).Returns([mapping]);

        // Resep mock
        var resep = ResepModel.Key("KP001");
        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP001"))).Returns(MayBe.From(resep));

        // Telaah mock with Approved status (CanEstablishSalesOrder = true)
        var telaah = TelaahResepModel.Rehydrate("TEL001", "KP001", "REG-01", TelaahStatusEnum.Approved, "PHARM-01", _testTime, _testTime, 1, [
            new TelaahResepItemModel(1, 1, TelaahDispositionEnum.AcceptedAsPrescribed, "B01", "Obat", 10, "", "PHARM-01")
        ]);
        _telaahRepo.Setup(x => x.LoadByResep("KP001")).Returns(MayBe.From(telaah));

        var sut = CreateSut(includeEnrichment: true);
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ", FilterTab: "Siap Penjualan"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].QueueLabel.Should().Be("A005");
    }

    [Fact]
    public async Task Handle_Filters_By_Tab_SiapSerah()
    {
        var sp1 = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racik", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp1]);

        var item1 = ApotekQueueEntryModel.Create("ANT-1", 1, "SP-01", "A", _testTime);
        var item2 = ApotekQueueEntryModel.Create("ANT-1", 2, "SP-01", "A", _testTime);
        item2.TransitionToPenyiapan();
        item2.ConsolidateToSiapSerah(_testTime);

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([item1, item2]);
        _mappingRepo.Setup(x => x.ListByQueue(It.IsAny<string>(), It.IsAny<int>())).Returns([]);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ", FilterTab: "Siap Serah"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].ApotekPhase.Should().Be(ApotekPhaseEnum.SIAP_SERAH);
        result.Items[0].QueueLabel.Should().Be("A002");
    }

    [Fact]
    public async Task Handle_Filters_By_Explicit_Phase_Enum()
    {
        var sp1 = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racik", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp1]);

        var item1 = ApotekQueueEntryModel.Create("ANT-1", 1, "SP-01", "A", _testTime);
        var item2 = ApotekQueueEntryModel.Create("ANT-1", 2, "SP-01", "A", _testTime);
        item2.Skip(LoketTypeEnum.PenerimaanResep, _testTime);

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([item1, item2]);
        _mappingRepo.Setup(x => x.ListByQueue(It.IsAny<string>(), It.IsAny<int>())).Returns([]);

        var sut = CreateSut();
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ", Phase: ApotekPhaseEnum.DILEWATI), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].ApotekPhase.Should().Be(ApotekPhaseEnum.DILEWATI);
    }

    [Fact]
    public async Task Handle_Orders_By_Cito_PasienHadir_And_NoUrut()
    {
        var sp = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racik", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp]);

        // item 1: Normal, PasienHadir = false, NoUrut = 1
        var item1 = ApotekQueueEntryModel.Create("ANT-1", 1, "SP-01", "A", _testTime);
        // item 2: Normal, PasienHadir = true, NoUrut = 5
        var item2 = ApotekQueueEntryModel.Create("ANT-1", 5, "SP-01", "A", _testTime);
        // item 3: CITO, PasienHadir = false, NoUrut = 10
        var item3 = ApotekQueueEntryModel.Create("ANT-1", 10, "SP-01", "A", _testTime);

        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([item1, item2, item3]);

        var map1 = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP001", "ANT-1", 1, "TRK-1", QueueMappingMethodEnum.Manual, "u", _testTime);
        var map2 = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP002", "ANT-1", 5, "TRK-2", QueueMappingMethodEnum.Manual, "u", _testTime);
        map2.MarkPasienHadir(_testTime);
        var map3 = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP003", "ANT-1", 10, "TRK-3", QueueMappingMethodEnum.Manual, "u", _testTime);

        _mappingRepo.Setup(x => x.ListByQueue("ANT-1", 1)).Returns([map1]);
        _mappingRepo.Setup(x => x.ListByQueue("ANT-1", 5)).Returns([map2]);
        _mappingRepo.Setup(x => x.ListByQueue("ANT-1", 10)).Returns([map3]);

        var resep1 = new ResepModel("KP001", new RegReff("R1", "P1", "Patient 1"), null!, null!, null!, UrgenitasType.Default, null!, 0, "", null!, []);
        var resep2 = new ResepModel("KP002", new RegReff("R2", "P2", "Patient 2"), null!, null!, null!, UrgenitasType.Default, null!, 0, "", null!, []);
        var resep3 = new ResepModel("KP003", new RegReff("R3", "P3", "Patient 3"), null!, null!, null!, UrgenitasType.Load("1", "CITO"), null!, 0, "", null!, []);

        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP001"))).Returns(MayBe.From(resep1));
        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP002"))).Returns(MayBe.From(resep2));
        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP003"))).Returns(MayBe.From(resep3));

        var sut = CreateSut(includeEnrichment: true);
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ"), CancellationToken.None);

        // Expected order:
        // 1st: item 3 (CITO)
        // 2nd: item 2 (PasienHadir)
        // 3rd: item 1 (Normal)
        result.Items.Select(x => x.NoUrut).Should().ContainInOrder([10, 5, 1]);
    }

    [Fact]
    public async Task Handle_Enriches_With_Patient_Resep_And_Telaah_Data()
    {
        var sp = PharmacyServicePointModel.Create("SP-01", "DEPO-RJ", "Non-Racikan", "A");
        _servicePointRepo.Setup(x => x.ListByDepo("DEPO-RJ")).Returns([sp]);

        var item = ApotekQueueEntryModel.Create("ANT-1", 7, "SP-01", "A", _testTime);
        _queueEntryRepo.Setup(x => x.ListActiveByDepo("DEPO-RJ")).Returns([item]);

        var mapping = QueueMappingModel.Create(QueueDemandKindEnum.Resep, "KP007", "ANT-1", 7, "TRK-7", QueueMappingMethodEnum.Manual, "petugas1", _testTime);
        mapping.MarkPasienHadir(_testTime.AddMinutes(5));
        _mappingRepo.Setup(x => x.ListByQueue("ANT-1", 7)).Returns([mapping]);

        var dokterReff = new DokterReff("D01", "dr. Sutomo, Sp.PD");
        var layananReff = new LayananReff("L01", "Poli Penyakit Dalam");
        var resep = new ResepModel(
            "KP007",
            new RegReff("REG-99", "RM-123", "Ny. Siti"),
            null!,
            dokterReff,
            layananReff,
            UrgenitasType.Load("1", "CITO"),
            new TipeBrgReff("TB01", "BPJS Fornas"),
            0,
            "Resep kronis",
            null!,
            []);
        _resepRepo.Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP007"))).Returns(MayBe.From(resep));

        var ppaReff = new PpaReff("D01", "dr. Sutomo, Sp.PD");
        var reg = new RegModel(
            "REG-99",
            DateOnly.FromDateTime(_testTime),
            null!, null!, null!, null!,
            JenisRegEnum.RegJalan,
            new PasienReff("RM-123", "Ny. Siti", DateOnly.FromDateTime(_testTime.AddYears(-30)), "P"),
            new TipeJaminanReff("BPJS", "BPJS Kesehatan"),
            null!, null!, null!, null!,
            ppaReff, layananReff, null!, null!, []);
        _regRepo.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-99"))).Returns(MayBe.From(reg));

        var sut = CreateSut(includeEnrichment: true);
        var result = await sut.Handle(new PharmacyQueueWorklistQry("DEPO-RJ"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        var row = result.Items[0];
        row.AntrianId.Should().Be("ANT-1");
        row.NoUrut.Should().Be(7);
        row.QueueLabel.Should().Be("A007");
        row.PatientName.Should().Be("Ny. Siti");
        row.MedicalRecordNo.Should().Be("RM-123");
        row.RegistrationId.Should().Be("REG-99");
        row.DokterName.Should().Be("dr. Sutomo, Sp.PD");
        row.PoliName.Should().Be("Poli Penyakit Dalam");
        row.Penjamin.Should().Be("BPJS Kesehatan");
        row.IsCito.Should().BeTrue();
        row.PasienHadir.Should().BeTrue();
        row.PasienHadirAt.Should().Be(_testTime.AddMinutes(5));
        row.DemandIds.Should().ContainSingle().Which.Should().Be("KP007");
    }
}
