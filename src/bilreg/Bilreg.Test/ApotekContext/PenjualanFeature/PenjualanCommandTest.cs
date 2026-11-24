using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Application.ApotekContext.PenjualanFeature.UseCases;
using Bilreg.Application.ApotekContext.Shared;
using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using MediatR;
using TipeBrgType = Bilreg.Domain.BrgContext.PricingPolicyFeature.TipeBrgType;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class PenjualanCommandTest
{
    private readonly InMemoryPenjualanRepo _penjualanRepo = new();
    private readonly InMemoryTelaahRepo _telaahRepo = new();
    private readonly InMemoryResepRepo _resepRepo = new();
    private readonly FakeRegRepo _regRepo = new();
    private readonly FakeTrkTarifPricingPort _pricingPort = new();
    private readonly FakeTrkBillingChargePort _billingChargePort = new();
    private readonly FakeTataRekeningPermissionPort _permissionPort = new();
    private readonly FakeFornasCheckPort _fornasCheckPort = new();
    private readonly AllowAllAuth _auth = new();

    #region Helpers

    private RegModel SeedReg(
        string regId = "REG001",
        string tipeJaminanId = "00000",
        bool isRanap = false,
        bool isAktif = true,
        string pasienName = "Budi Santoso")
    {
        var pasienReff = new PasienReff("PAS001", pasienName, new DateOnly(1990, 1, 1), "L");
        var reg = new RegModel(
            regId,
            DateOnly.FromDateTime(DateTime.Today),
            new AuditInfoType("tester", DateTime.Today),
            isAktif ? AuditInfoType.Default : new AuditInfoType("USER", DateTime.Now),
            AuditInfoType.Default,
            AuditInfoType.Default,
            isRanap ? JenisRegEnum.RegInap : JenisRegEnum.RegJalan,
            pasienReff,
            new TipeJaminanReff(tipeJaminanId, tipeJaminanId == "JKN00" ? "BPJS Kesehatan" : "Umum"),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            new PpaReff("DOK001", "dr. Subroto, Sp.PD"),
            new LayananReff("POLI01", "Poli Penyakit Dalam"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);

        _regRepo.SaveChanges(reg);
        return reg;
    }

    private ResepModel SeedResep(string resepId, RegModel reg, params (string brgId, string brgName, decimal qty)[] items)
    {
        var listObat = new List<ResepObatType>();
        int no = 1;
        foreach (var item in items)
        {
            var brg = BrgObatType.Default with { BrgId = item.brgId, BrgName = item.brgName };
            var etiket = EtiketType.Default with { Signa = "3x1", Instruction = "Sesudah makan", Note = "Habiskan" };
            var obat = ResepObatType.Create(no++, brg, SatuanType.Default, item.qty, 0, etiket);
            listObat.Add(obat);
        }

        var resep = new ResepModel(
            resepId,
            reg.ToReff(),
            BodyMetricType.Default(),
            new DokterReff(reg.Dokter.PpaId, reg.Dokter.PpaName),
            new LayananReff(reg.Layanan.LayananId, reg.Layanan.LayananName),
            UrgenitasType.Default,
            TipeBrgType.Default.ToReff(),
            0,
            "Resep Uji",
            AuditTrailType.Default,
            listObat);

        _resepRepo.SaveChanges(resep);
        return resep;
    }

    private TelaahResepModel SeedTelaah(string resepId, string regId, TelaahStatusEnum status, params (string brgId, string brgName, decimal qty, TelaahDispositionEnum disp)[] items)
    {
        var telaahItems = new List<TelaahResepItemModel>();
        int no = 1;
        foreach (var it in items)
        {
            var acceptedQty = it.disp == TelaahDispositionEnum.Rejected ? 0 : it.qty;
            telaahItems.Add(new TelaahResepItemModel(
                itemNo: no,
                resepItemNo: no++,
                disposition: it.disp,
                acceptedBrgId: it.brgId,
                acceptedBrgName: it.brgName,
                acceptedQty: acceptedQty,
                reason: it.disp == TelaahDispositionEnum.Rejected ? "Tidak tersedia" : "",
                pharmacistId: "APT01"));
        }

        var telaah = TelaahResepModel.Open(resepId, regId, telaahItems);
        if (status is TelaahStatusEnum.Approved or TelaahStatusEnum.PartiallyApproved or TelaahStatusEnum.Rejected)
        {
            telaah.Start("APT01", DateTime.Now);
            telaah.Complete(DateTime.Now);
        }

        _telaahRepo.SaveChanges(telaah);
        return telaah;
    }

    #endregion

    #region 1. PenjualanCreateFromTelaahCmd Tests

    [Fact]
    public async Task CreateFromTelaah_single_document_for_non_bpjs_succeeds()
    {
        // Arrange
        var reg = SeedReg("REG001", "00000", isRanap: false);
        var resep = SeedResep("RSP001", reg, ("BRG01", "Paracetamol 500mg", 10m), ("BRG02", "Amoxicillin 500mg", 15m));
        SeedTelaah("RSP001", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Paracetamol 500mg", 10m, TelaahDispositionEnum.AcceptedAsPrescribed),
            ("BRG02", "Amoxicillin 500mg", 15m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var sut = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);

        var cmd = new PenjualanCreateFromTelaahCmd("USER01", "RSP001");

        // Act
        var result = await sut.Handle(cmd, default);

        // Assert
        result.Should().HaveCount(1);
        var penjualanId = result[0];
        penjualanId.Should().StartWith("DU");

        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(penjualanId)).Value;
        stored.ResepId.Should().Be("RSP001");
        stored.RegId.Should().Be("REG001");
        stored.TipeJaminanId.Should().Be("00000");
        stored.Status.Should().Be(PenjualanStatusEnum.Issued);
        stored.Items.Should().HaveCount(2);
        stored.Items.All(x => x.PenjualanItemId.StartsWith(penjualanId)).Should().BeTrue();
        stored.Items.All(x => x.NilaiKlaim == 0).Should().BeTrue();
        stored.TrsMutasiId.Should().BeEmpty();
        stored.TglSerah.Should().Be(ApotekDate.Empty);
    }

    [Fact]
    public async Task CreateFromTelaah_single_document_for_bpjs_all_fornas_has_nilaiklaim()
    {
        // Arrange
        var reg = SeedReg("REG002", "JKN00", isRanap: false);
        var resep = SeedResep("RSP002", reg, ("BRG01", "Paracetamol 500mg", 10m));
        SeedTelaah("RSP002", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Paracetamol 500mg", 10m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var sut = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);

        var cmd = new PenjualanCreateFromTelaahCmd("USER01", "RSP002");

        // Act
        var result = await sut.Handle(cmd, default);

        // Assert
        result.Should().HaveCount(1);
        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(result[0])).Value;
        stored.TipeJaminanId.Should().Be("JKN00");
        stored.Items.Should().HaveCount(1);
        stored.Items[0].NilaiKlaim.Should().Be(stored.Items[0].Total);
    }

    [Fact]
    public async Task CreateFromTelaah_mixed_payer_bpjs_automatically_splits_into_two_du_documents()
    {
        // Arrange
        var reg = SeedReg("REG003", "JKN00", isRanap: false);
        var resep = SeedResep("RSP003", reg,
            ("BRG01", "Paracetamol Fornas", 10m),
            ("BRG02", "Suplemen Non-Fornas", 5m));

        SeedTelaah("RSP003", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Paracetamol Fornas", 10m, TelaahDispositionEnum.AcceptedAsPrescribed),
            ("BRG02", "Suplemen Non-Fornas", 5m, TelaahDispositionEnum.AcceptedAsPrescribed));

        _fornasCheckPort.NonFornasItemIds.Add("BRG02");

        var sut = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);

        var cmd = new PenjualanCreateFromTelaahCmd("USER01", "RSP003");

        // Act
        var result = await sut.Handle(cmd, default);

        // Assert: 2 DU Documents produced with distinct collision-free IDs
        result.Should().HaveCount(2);
        result[0].Should().NotBe(result[1]);
        result[0].Should().EndWith("A");
        result[1].Should().EndWith("B");

        var bpjsDoc = _penjualanRepo.LoadEntity(new PenjualanKey(result[0])).Value;
        var mandiriDoc = _penjualanRepo.LoadEntity(new PenjualanKey(result[1])).Value;

        // BPJS DU Document: Fornas only, JKN00, has nilaiKlaim
        bpjsDoc.TipeJaminanId.Should().Be("JKN00");
        bpjsDoc.Items.Should().HaveCount(1);
        bpjsDoc.Items[0].BrgId.Should().Be("BRG01");
        bpjsDoc.Items[0].NilaiKlaim.Should().Be(bpjsDoc.Items[0].Total);
        bpjsDoc.ResepId.Should().Be("RSP003");
        bpjsDoc.RegId.Should().Be("REG003");

        // Mandiri DU Document: Non-Fornas only, 00000, zero nilaiKlaim
        mandiriDoc.TipeJaminanId.Should().Be("00000");
        mandiriDoc.Items.Should().HaveCount(1);
        mandiriDoc.Items[0].BrgId.Should().Be("BRG02");
        mandiriDoc.Items[0].NilaiKlaim.Should().Be(0);
        mandiriDoc.ResepId.Should().Be("RSP003");
        mandiriDoc.RegId.Should().Be("REG003");
    }

    [Fact]
    public async Task CreateFromTelaah_mixed_payer_split_repeated_in_loop_never_generates_colliding_ids()
    {
        for (int i = 0; i < 20; i++)
        {
            var regId = $"REG_M_{i:D3}";
            var resepId = $"RSP_M_{i:D3}";

            var reg = SeedReg(regId, "JKN00", isRanap: false);
            var resep = SeedResep(resepId, reg,
                ("BRG01", "Paracetamol Fornas", 10m),
                ("BRG02", "Suplemen Non-Fornas", 5m));

            SeedTelaah(resepId, reg.RegId, TelaahStatusEnum.Approved,
                ("BRG01", "Paracetamol Fornas", 10m, TelaahDispositionEnum.AcceptedAsPrescribed),
                ("BRG02", "Suplemen Non-Fornas", 5m, TelaahDispositionEnum.AcceptedAsPrescribed));

            _fornasCheckPort.NonFornasItemIds.Add("BRG02");

            var sut = new PenjualanCreateFromTelaahHandler(
                _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);

            var cmd = new PenjualanCreateFromTelaahCmd("USER01", resepId);
            var result = await sut.Handle(cmd, default);

            result.Should().HaveCount(2);
            result[0].Should().NotBe(result[1]);
            result[0].Should().EndWith("A");
            result[1].Should().EndWith("B");

            var bpjsDoc = _penjualanRepo.LoadEntity(new PenjualanKey(result[0])).Value;
            var mandiriDoc = _penjualanRepo.LoadEntity(new PenjualanKey(result[1])).Value;

            bpjsDoc.TipeJaminanId.Should().Be("JKN00");
            mandiriDoc.TipeJaminanId.Should().Be("00000");
        }
    }

    [Fact]
    public async Task CreateFromTelaah_for_ranap_triggers_billing_charge_and_saves_charge_id()
    {
        // Arrange
        var reg = SeedReg("REG004", "00000", isRanap: true);
        var resep = SeedResep("RSP004", reg, ("BRG01", "Ceftriaxone 1g", 2m));
        SeedTelaah("RSP004", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Ceftriaxone 1g", 2m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var sut = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);

        var cmd = new PenjualanCreateFromTelaahCmd("USER01", "RSP004");

        // Act
        var result = await sut.Handle(cmd, default);

        // Assert
        result.Should().HaveCount(1);
        _billingChargePort.ChargedRequests.Should().HaveCount(1);

        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(result[0])).Value;
        stored.TrsChargeId.Should().NotBeNullOrWhiteSpace();
        stored.TrsChargeId.Should().StartWith("TB");
    }

    [Fact]
    public async Task CreateFromTelaah_is_idempotent_when_called_again_for_same_resep()
    {
        // Arrange
        var reg = SeedReg("REG005", "00000", isRanap: false);
        var resep = SeedResep("RSP005", reg, ("BRG01", "Paracetamol 500mg", 10m));
        SeedTelaah("RSP005", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Paracetamol 500mg", 10m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var sut = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);

        var cmd = new PenjualanCreateFromTelaahCmd("USER01", "RSP005");

        // Act
        var firstCall = await sut.Handle(cmd, default);
        var secondCall = await sut.Handle(cmd, default);

        // Assert
        secondCall.Should().BeEquivalentTo(firstCall);
        _penjualanRepo.Store.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateFromTelaah_throws_when_telaah_is_not_approved()
    {
        // Arrange
        var reg = SeedReg("REG006", "00000", isRanap: false);
        var resep = SeedResep("RSP006", reg, ("BRG01", "Paracetamol 500mg", 10m));
        SeedTelaah("RSP006", reg.RegId, TelaahStatusEnum.UnderReview,
            ("BRG01", "Paracetamol 500mg", 10m, TelaahDispositionEnum.Pending));

        var sut = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);

        var cmd = new PenjualanCreateFromTelaahCmd("USER01", "RSP006");

        // Act & Assert
        var act = () => sut.Handle(cmd, default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*telaah belum Approved atau PartiallyApproved*");
    }

    #endregion

    #region 2. PenjualanCreateDirectCmd Tests

    [Fact]
    public async Task CreateDirect_with_active_reg_creates_registered_direct_sale()
    {
        // Arrange
        var reg = SeedReg("REG007", "00000", isRanap: false);
        var sut = new PenjualanCreateDirectHandler(_penjualanRepo, _regRepo, _pricingPort, _billingChargePort, _auth);

        var items = new List<PenjualanDirectItemInput>
        {
            new("BRG01", "Infus RL 500ml", "BTL", "Botol", 2m),
            new("BRG02", "Abocath 20G", "PCS", "Pcs", 1m)
        };

        var cmd = new PenjualanCreateDirectCmd(
            UserId: "USER01",
            RegId: "REG007",
            DokterId: "DOK001",
            LayananId: "IGD01",
            Items: items);

        // Act
        var penjualanId = await sut.Handle(cmd, default);

        // Assert
        penjualanId.Should().StartWith("DU");
        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(penjualanId)).Value;
        stored.JenisPenjualan.Should().Be(JenisPenjualanEnum.RegisteredDirect);
        stored.RegId.Should().Be("REG007");
        stored.ResepId.Should().BeEmpty();
        stored.PasienName.Should().Be("Budi Santoso");
        stored.DokterId.Should().Be("DOK001");
        stored.Items.Should().HaveCount(2);
        stored.GrandTotal.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateDirect_for_ranap_triggers_billing_charge()
    {
        // Arrange
        var reg = SeedReg("REG008", "00000", isRanap: true);
        var sut = new PenjualanCreateDirectHandler(_penjualanRepo, _regRepo, _pricingPort, _billingChargePort, _auth);

        var items = new List<PenjualanDirectItemInput>
        {
            new("BRG01", "Spuit 3cc", "PCS", "Pcs", 5m)
        };

        var cmd = new PenjualanCreateDirectCmd(
            UserId: "USER01",
            RegId: "REG008",
            DokterId: "DOK001",
            LayananId: "RANAP01",
            Items: items);

        // Act
        var penjualanId = await sut.Handle(cmd, default);

        // Assert
        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(penjualanId)).Value;
        stored.TrsChargeId.Should().NotBeNullOrWhiteSpace();
        _billingChargePort.ChargedRequests.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateDirect_throws_when_registration_is_inactive()
    {
        // Arrange
        var reg = SeedReg("REG009", "00000", isRanap: false, isAktif: false);
        var sut = new PenjualanCreateDirectHandler(_penjualanRepo, _regRepo, _pricingPort, _billingChargePort, _auth);

        var items = new List<PenjualanDirectItemInput>
        {
            new("BRG01", "Spuit 3cc", "PCS", "Pcs", 1m)
        };

        var cmd = new PenjualanCreateDirectCmd(
            UserId: "USER01",
            RegId: "REG009",
            DokterId: "DOK001",
            LayananId: "IGD01",
            Items: items);

        // Act & Assert
        var act = () => sut.Handle(cmd, default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*tidak aktif atau sudah keluar/batal*");
    }

    #endregion

    #region 3. PenjualanCreateWalkInCmd Tests

    [Fact]
    public async Task CreateWalkIn_without_registration_sets_00000_and_belibebas()
    {
        // Arrange
        var sut = new PenjualanCreateWalkInHandler(_penjualanRepo, _pricingPort, _auth);

        var items = new List<PenjualanWalkInItemInput>
        {
            new("BRG01", "Minyak Kayu Putih 60ml", "BTL", "Botol", 1m),
            new("BRG02", "Plester Cepat", "PCS", "Pcs", 5m)
        };

        var cmd = new PenjualanCreateWalkInCmd(
            UserId: "KASIR01",
            PembeliName: "Ny. Siska Handayani",
            LayananId: "APT_OTC",
            Items: items,
            PembeliAlamat: "Jl. Veteran No. 45",
            PembeliKota: "Pekalongan");

        // Act
        var penjualanId = await sut.Handle(cmd, default);

        // Assert
        penjualanId.Should().StartWith("DU");
        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(penjualanId)).Value;
        stored.JenisPenjualan.Should().Be(JenisPenjualanEnum.WalkInOTC);
        stored.RegId.Should().BeEmpty();
        stored.ResepId.Should().BeEmpty();
        stored.PasienName.Should().Be("Ny. Siska Handayani");
        stored.PasienAlamat.Should().Be("Jl. Veteran No. 45");
        stored.PasienKota.Should().Be("Pekalongan");
        stored.TipeJaminanId.Should().Be("00000");
        stored.Items.Should().HaveCount(2);
        stored.Items.All(x => x.IsBeliBebas).Should().BeTrue();
    }

    #endregion

    #region 4. PenjualanVoidCmd Tests

    [Fact]
    public async Task Void_soft_voids_header_and_details_and_cancels_billing_charge()
    {
        // Arrange
        var reg = SeedReg("REG010", "00000", isRanap: true);
        var resep = SeedResep("RSP010", reg, ("BRG01", "Obat Ranap", 1m));
        SeedTelaah("RSP010", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Obat Ranap", 1m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var createHandler = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);
        var created = await createHandler.Handle(new PenjualanCreateFromTelaahCmd("USER01", "RSP010"), default);
        var penjualanId = created[0];

        var sut = new PenjualanVoidHandler(_penjualanRepo, _permissionPort, _billingChargePort, _auth);
        var voidCmd = new PenjualanVoidCmd("USER01", penjualanId, "Salah entri resep dokter");

        // Act
        await sut.Handle(voidCmd, default);

        // Assert
        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(penjualanId)).Value;
        stored.IsVoid.Should().BeTrue();
        stored.Status.Should().Be(PenjualanStatusEnum.Cancelled);
        stored.VoidReason.Should().Be("Salah entri resep dokter");
        stored.PetugasVoid.Should().Be("USER01");
        stored.Items.All(x => x.IsVoid).Should().BeTrue();

        _billingChargePort.CancelledCharges.Should().HaveCount(1);
        _billingChargePort.CancelledCharges[0].ChargeId.Should().Be(stored.TrsChargeId);
    }

    [Fact]
    public async Task Void_rejected_when_tata_rekening_denies_permission()
    {
        // Arrange
        var reg = SeedReg("REG011", "00000", isRanap: false);
        var resep = SeedResep("RSP011", reg, ("BRG01", "Obat Rajal", 1m));
        SeedTelaah("RSP011", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Obat Rajal", 1m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var createHandler = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);
        var created = await createHandler.Handle(new PenjualanCreateFromTelaahCmd("USER01", "RSP011"), default);
        var penjualanId = created[0];

        _permissionPort.AllowVoidResult = false; // Denied by ADR-APT-003

        var sut = new PenjualanVoidHandler(_penjualanRepo, _permissionPort, _billingChargePort, _auth);
        var voidCmd = new PenjualanVoidCmd("USER01", penjualanId, "Batal");

        // Act & Assert
        var act = () => sut.Handle(voidCmd, default);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*ADR-APT-003*");
    }

    #endregion

    #region 5. PenjualanUpdateHandoverInfoCmd Tests

    [Fact]
    public async Task UpdateHandoverInfo_updates_handover_and_mutation_reference()
    {
        // Arrange
        var reg = SeedReg("REG012", "00000", isRanap: false);
        var resep = SeedResep("RSP012", reg, ("BRG01", "Obat Serah", 1m));
        SeedTelaah("RSP012", reg.RegId, TelaahStatusEnum.Approved,
            ("BRG01", "Obat Serah", 1m, TelaahDispositionEnum.AcceptedAsPrescribed));

        var createHandler = new PenjualanCreateFromTelaahHandler(
            _penjualanRepo, _telaahRepo, _resepRepo, _regRepo, _pricingPort, _billingChargePort, _auth, _fornasCheckPort);
        var created = await createHandler.Handle(new PenjualanCreateFromTelaahCmd("USER01", "RSP012"), default);
        var penjualanId = created[0];

        var sut = new PenjualanUpdateHandoverInfoHandler(_penjualanRepo, _auth);
        var handoverDate = new DateTime(2026, 10, 18, 14, 30, 0);

        var cmd = new PenjualanUpdateHandoverInfoCmd(
            UserId: "USER01",
            PenjualanId: penjualanId,
            TglSerah: handoverDate,
            JamSerah: "14:30:00",
            PetugasSerah: "APT_SERAH_01",
            TrsMutasiId: "DI202610180001");

        // Act
        await sut.Handle(cmd, default);

        // Assert
        var stored = _penjualanRepo.LoadEntity(new PenjualanKey(penjualanId)).Value;
        stored.TglSerah.Should().Be(handoverDate);
        stored.JamSerah.Should().Be("14:30:00");
        stored.PetugasSerah.Should().Be("APT_SERAH_01");
        stored.TrsMutasiId.Should().Be("DI202610180001");
    }

    #endregion
}
