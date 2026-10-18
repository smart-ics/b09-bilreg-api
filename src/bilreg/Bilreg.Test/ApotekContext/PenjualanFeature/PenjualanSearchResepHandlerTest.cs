using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class PenjualanSearchResepHandlerTest
{
    private readonly InMemoryResepRepo _resepRepo = new();
    private readonly InMemoryPenjualanRepo _penjualanRepo = new();
    private readonly InMemoryTelaahRepo _telaahRepo = new();
    private readonly InMemoryQueueMappingRepo _mappingRepo = new();
    private readonly PenjualanSearchResepHandler _sut;

    public PenjualanSearchResepHandlerTest()
    {
        _sut = new PenjualanSearchResepHandler(_resepRepo, _penjualanRepo, _telaahRepo, _mappingRepo);
    }

    [Fact]
    public async Task Search_by_MedicalRecordNo_returns_matching_prescription()
    {
        // Arrange
        var resep = SeedResep("KP001", "REG01", "MR001", "Budi Santoso", ("B01", "Paracetamol", 10m));

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("MR001"), default);

        // Assert
        result.Should().ContainSingle();
        var item = result.First();
        item.ResepId.Should().Be("KP001");
        item.PasienId.Should().Be("MR001");
        item.PasienName.Should().Be("Budi Santoso");
        item.RegId.Should().Be("REG01");
    }

    [Fact]
    public async Task Search_by_RegistrationId_returns_matching_prescription()
    {
        // Arrange
        var resep = SeedResep("KP002", "REG-RAD-100", "MR002", "Siti Rahma", ("B01", "Amoxicillin", 15m));

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("REG-RAD-100"), default);

        // Assert
        result.Should().ContainSingle();
        result[0].ResepId.Should().Be("KP002");
        result[0].PasienName.Should().Be("Siti Rahma");
    }

    [Fact]
    public async Task Search_by_PatientName_returns_matching_prescription()
    {
        // Arrange
        var resep = SeedResep("KP003", "REG03", "MR003", "Ahmad Fauzi", ("B02", "Antasida", 5m));

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("Fauzi"), default);

        // Assert
        result.Should().ContainSingle();
        result[0].ResepId.Should().Be("KP003");
        result[0].PasienName.Should().Be("Ahmad Fauzi");
    }

    [Fact]
    public async Task Search_by_ResepId_returns_matching_prescription()
    {
        // Arrange
        var resep = SeedResep("KP999", "REG04", "MR004", "Dewi Lestari", ("B03", "Cetirizine", 10m));

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("KP999"), default);

        // Assert
        result.Should().ContainSingle();
        result[0].ResepId.Should().Be("KP999");
    }

    [Fact]
    public async Task Prescription_already_billed_in_dobill_umum_is_excluded_from_search_results()
    {
        // Arrange
        var resep = SeedResep("KP010", "REG10", "MR010", "Pasien Billed", ("B01", "Drug A", 10m));
        
        // Simulasikan tagihan komersial DU... telah terbit untuk resep ini
        var pItem = PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: "DU2400000101",
            brgId: "B01",
            brgName: "Drug A",
            satuanId: "TAB",
            satuanName: "Tablet",
            qty: 10,
            hargaSatuan: 500);

        var duModel = PenjualanModel.CreateFromTelaah(
            resepId: "KP010",
            regId: "REG10",
            pasienName: "Pasien Billed",
            dokterId: "DOK01",
            layananId: "FAR01",
            layananResepId: "FAR01",
            tipeJaminanId: "00000",
            kelasId: "3",
            petugasId: "u",
            penjualanDate: DateTime.Today,
            items: new[] { pItem },
            penjualanId: "DU24000001");
        _penjualanRepo.SaveChanges(duModel);

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("MR010"), default);

        // Assert: Harus tereksklusi karena sudah tertagih
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Prescription_with_approved_telaah_returns_disetujui_and_can_establish_true()
    {
        // Arrange
        var resep = SeedResep("KP020", "REG20", "MR020", "Pasien Disetujui", ("B01", "Amoxicillin", 10m));
        
        var telaah = TelaahResepModel.Open(
            resep.ResepId,
            resep.Register.RegId,
            [new TelaahResepItemModel(1, 1, "B01", "Amoxicillin", 10m, TelaahDispositionEnum.AcceptedAsPrescribed, "B01", "Amoxicillin", 10m, "", "pharm")]);
        telaah.Start("pharm", DateTime.Now);
        telaah.Complete(DateTime.Now);
        _telaahRepo.SaveChanges(telaah);

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("KP020"), default);

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.TelaahResepId.Should().Be(telaah.TelaahResepId);
        item.TelaahStatus.Should().Be((int)TelaahStatusEnum.Approved);
        item.TelaahStatusDesc.Should().Be("Disetujui");
        item.CanEstablishSalesOrder.Should().BeTrue();
        item.Items.Should().ContainSingle();
        item.Items[0].AcceptedQty.Should().Be(10m);
        item.Items[0].Disposition.Should().Be((int)TelaahDispositionEnum.AcceptedAsPrescribed);
    }

    [Fact]
    public async Task Prescription_without_telaah_returns_belum_ditelaah_and_can_establish_false()
    {
        // Arrange
        var resep = SeedResep("KP030", "REG30", "MR030", "Pasien Belum Telaah", ("B01", "Drug X", 10m));

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("KP030"), default);

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.TelaahResepId.Should().BeEmpty();
        item.TelaahStatus.Should().BeNull();
        item.TelaahStatusDesc.Should().Be("Belum Ditelaah");
        item.CanEstablishSalesOrder.Should().BeFalse();
    }

    [Fact]
    public async Task Prescription_with_mapped_queue_ticket_returns_queue_info_and_pasien_hadir()
    {
        // Arrange
        var resep = SeedResep("KP040", "REG40", "MR040", "Pasien Hadir di Loket", ("B01", "Drug Y", 10m));
        
        var mapping = QueueMappingModel.Create(
            QueueDemandKindEnum.Resep,
            resep.ResepId,
            "ANT-001",
            5,
            "",
            QueueMappingMethodEnum.Manual,
            "officer",
            DateTime.Now,
            pasienHadir: true,
            pasienHadirAt: DateTime.Now);
        _mappingRepo.SaveChanges(mapping);

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("KP040"), default);

        // Assert
        result.Should().ContainSingle();
        var item = result[0];
        item.AntrianId.Should().Be("ANT-001");
        item.NoUrut.Should().Be(5);
        item.PasienHadir.Should().BeTrue();
        item.PasienHadirAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Search_with_no_matching_prescriptions_returns_empty_list()
    {
        // Arrange
        SeedResep("KP001", "REG01", "MR001", "Budi Santoso", ("B01", "Drug", 5m));

        // Act
        var result = await _sut.Handle(new PenjualanSearchResepQry("XYZ_NON_EXISTENT"), default);

        // Assert
        result.Should().BeEmpty();
    }

    #region Helpers

    private ResepModel SeedResep(
        string resepId,
        string regId,
        string pasienId,
        string pasienName,
        params (string brgId, string brgName, decimal qty)[] items)
    {
        var regReff = new RegReff(regId, pasienId, pasienName);
        var resep = new ResepModel(
            resepId: resepId,
            reg: regReff,
            bodyMetric: BodyMetricType.Default(),
            dokter: DokterType.Default.ToReff(),
            layanan: LayananType.Default.ToReff(),
            layananStok: LayananType.Default.ToReff(),
            urgenitas: UrgenitasType.Default,
            tipeBrg: TipeBrgType.Default.ToReff(),
            iter: 0,
            description: "Test Prescription",
            tglRencanaPeriksa: DateTime.Today,
            resepAsalReffId: AppConst.DASH,
            voidReason: AppConst.DASH,
            auditTrail: AuditTrailType.Create("doctor", DateTime.Now),
            listObat: []);

        foreach (var (brgId, brgName, qty) in items)
        {
            var brg = BrgObatType.Default with { BrgId = brgId, BrgName = brgName };
            resep.AddObat(brg, SatuanType.Default, qty, 0, "3x1", "Sesudah Makan", "");
        }

        _resepRepo.SaveChanges(resep);
        return resep;
    }

    #endregion
}
