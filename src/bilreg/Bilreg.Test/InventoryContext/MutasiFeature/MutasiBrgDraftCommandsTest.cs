using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.MutasiFeature;
using Bilreg.Application.InventoryContext.MutasiFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.InventoryContext.MutasiFeature;

public class MutasiBrgDraftCommandsTest
{
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IOrderMutasiRepo> _orderMutasiRepoMock = new();
    private readonly Mock<IMutasiBrgRepo> _mutasiBrgRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<ISatuanRepo> _satuanRepoMock = new();

    private MutasiBrgCreateDraftHandler CreateCreateDraftHandler() =>
        new(_layananRepoMock.Object, _orderMutasiRepoMock.Object, _mutasiBrgRepoMock.Object);

    private MutasiBrgAddItemHandler CreateAddItemHandler() =>
        new(_mutasiBrgRepoMock.Object, _brgRepoMock.Object, _satuanRepoMock.Object);

    private MutasiBrgRemoveItemHandler CreateRemoveItemHandler() =>
        new(_mutasiBrgRepoMock.Object);

    private static IBrg CreateMockBrg(string id, string name, bool isAktif, string groupRekDkId)
    {
        var mock = new Mock<IBrg>();
        mock.Setup(x => x.BrgId).Returns(id);
        mock.Setup(x => x.BrgName).Returns(name);
        mock.Setup(x => x.IsAktif).Returns(isAktif);
        mock.Setup(x => x.GroupRekDk).Returns(new GroupRekDkType(groupRekDkId, groupRekDkId));
        mock.Setup(x => x.ToReff()).Returns(new BrgReff(id, name));
        return mock.Object;
    }

    private static MutasiBrgModel CreateSampleDraft(string id = "MT1234567A", string asalId = "L01", string tujuanId = "L02")
    {
        var asal = new LayananReff(asalId, "Gudang Farmasi");
        var tujuan = new LayananReff(tujuanId, "Rawat Inap");
        return MutasiBrgModel.Create(id, DateTime.Now, asal, tujuan, "", "Catatan", "USER01");
    }

    #region CREATE DRAFT COMMAND TESTS

    [Fact]
    public async Task CreateDraft_DirectMutation_CreatesDraftSuccessfully_With10CharLegacyId()
    {
        // Arrange
        var asal = LayananType.Default with { LayananId = "L01", LayananName = "Gudang Farmasi" };
        var tujuan = LayananType.Default with { LayananId = "L02", LayananName = "Rawat Inap" };

        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L01")))
            .Returns(MayBe.From(asal));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L02")))
            .Returns(MayBe.From(tujuan));

        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("L01", "L02", DispatchNote: "Kiriman rutin", UserId: "USER01");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.MutasiBrgId.Should().NotBeNullOrWhiteSpace();
        response.MutasiBrgId.Length.Should().Be(10);
        response.MutasiBrgId.Should().StartWith("MT");
        response.MutasiBrgId.Should().EndWith("A");

        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBrgModel>(m =>
            m.MutasiBrgId == response.MutasiBrgId &&
            m.State == MutasiBrgStateEnum.Draft &&
            m.LayananAsal.LayananId == "L01" &&
            m.LayananTujuan.LayananId == "L02" &&
            m.OrderMutasiId == string.Empty &&
            m.DispatchNote == "Kiriman rutin"
        )), Times.Once);
    }

    [Fact]
    public async Task CreateDraft_WithApprovedOrderMutasi_CreatesDraftSuccessfully()
    {
        // Arrange
        var asal = LayananType.Default with { LayananId = "GUD01", LayananName = "Gudang Sentral" };
        var tujuan = LayananType.Default with { LayananId = "POLI01", LayananName = "Poli Gigi" };

        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "GUD01")))
            .Returns(MayBe.From(asal));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "POLI01")))
            .Returns(MayBe.From(tujuan));

        // Create an approved OrderMutasi where destination is GUD01 (warehouse fulfilling the order)
        var orderMutasi = new OrderMutasiModel(
            orderMutasiId: "ORD01",
            orderMutasiDate: DateTime.Now.AddDays(-1),
            state: OrderMutasiStateEnum.Approved,
            layananOrder: tujuan.ToReff(),
            layananTujuan: asal.ToReff(),
            approval: new ApprovalType("APPROVER", DateTime.Now),
            rejection: ApprovalType.Default,
            orderNote: "Permintaan",
            auditTrail: AuditTrailType.Default,
            listItem: []);

        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "ORD01")))
            .Returns(MayBe.From(orderMutasi));

        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("GUD01", "POLI01", OrderMutasiId: "ORD01", UserId: "USER01");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.MutasiBrgId.Should().NotBeNullOrWhiteSpace();
        response.MutasiBrgId.Length.Should().Be(10);
        response.MutasiBrgId.Should().StartWith("MT");
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBrgModel>(m =>
            m.OrderMutasiId == "ORD01" &&
            m.State == MutasiBrgStateEnum.Draft
        )), Times.Once);
    }

    [Fact]
    public async Task CreateDraft_WhenAsalEqualsTujuan_ThrowsArgumentException()
    {
        // Arrange
        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("L01", "L01", UserId: "USER01");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ArgumentException>();
        ex.WithMessage("*Unit asal dan tujuan tidak boleh sama*");
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.IsAny<MutasiBrgModel>()), Times.Never);
    }

    [Fact]
    public async Task CreateDraft_WhenLayananAsalNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L_NONE")))
            .Returns(MayBe<LayananType>.None);

        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("L_NONE", "L02", UserId: "USER01");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*LayananAsalId 'L_NONE' not found*");
    }

    [Fact]
    public async Task CreateDraft_WhenLayananTujuanNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var asal = LayananType.Default with { LayananId = "L01", LayananName = "Gudang Farmasi" };
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L01")))
            .Returns(MayBe.From(asal));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L_NONE")))
            .Returns(MayBe<LayananType>.None);

        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("L01", "L_NONE", UserId: "USER01");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*LayananTujuanId 'L_NONE' not found*");
    }

    [Fact]
    public async Task CreateDraft_WhenOrderMutasiNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var asal = LayananType.Default with { LayananId = "L01", LayananName = "Gudang Farmasi" };
        var tujuan = LayananType.Default with { LayananId = "L02", LayananName = "Rawat Inap" };
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L01")))
            .Returns(MayBe.From(asal));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L02")))
            .Returns(MayBe.From(tujuan));

        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "ORD_NONE")))
            .Returns(MayBe<OrderMutasiModel>.None);

        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("L01", "L02", OrderMutasiId: "ORD_NONE", UserId: "USER01");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*OrderMutasi 'ORD_NONE' not found*");
    }

    [Fact]
    public async Task CreateDraft_WhenOrderMutasiNotApproved_ThrowsInvalidOperationException()
    {
        // Arrange
        var asal = LayananType.Default with { LayananId = "L01", LayananName = "Gudang Farmasi" };
        var tujuan = LayananType.Default with { LayananId = "L02", LayananName = "Rawat Inap" };
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L01")))
            .Returns(MayBe.From(asal));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L02")))
            .Returns(MayBe.From(tujuan));

        var unapprovedOrder = new OrderMutasiModel(
            orderMutasiId: "ORD02",
            orderMutasiDate: DateTime.Now,
            state: OrderMutasiStateEnum.Submitted,
            layananOrder: tujuan.ToReff(),
            layananTujuan: asal.ToReff(),
            approval: ApprovalType.Default,
            rejection: ApprovalType.Default,
            orderNote: "Permintaan",
            auditTrail: AuditTrailType.Default,
            listItem: []);

        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "ORD02")))
            .Returns(MayBe.From(unapprovedOrder));

        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("L01", "L02", OrderMutasiId: "ORD02", UserId: "USER01");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is not in Approved state*");
    }

    [Fact]
    public async Task CreateDraft_WhenOrderMutasiDestinationMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        var asal = LayananType.Default with { LayananId = "L01", LayananName = "Gudang Farmasi" };
        var tujuan = LayananType.Default with { LayananId = "L02", LayananName = "Rawat Inap" };
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L01")))
            .Returns(MayBe.From(asal));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "L02")))
            .Returns(MayBe.From(tujuan));

        var wrongDestinationOrder = new OrderMutasiModel(
            orderMutasiId: "ORD03",
            orderMutasiDate: DateTime.Now,
            state: OrderMutasiStateEnum.Approved,
            layananOrder: tujuan.ToReff(),
            layananTujuan: new LayananReff("OTHER_LOC", "Other Location"),
            approval: new ApprovalType("APP", DateTime.Now),
            rejection: ApprovalType.Default,
            orderNote: "Permintaan",
            auditTrail: AuditTrailType.Default,
            listItem: []);

        _orderMutasiRepoMock.Setup(x => x.LoadEntity(It.Is<IOrderMutasiKey>(k => k.OrderMutasiId == "ORD03")))
            .Returns(MayBe.From(wrongDestinationOrder));

        var handler = CreateCreateDraftHandler();
        var command = new MutasiBrgCreateDraftCommand("L01", "L02", OrderMutasiId: "ORD03", UserId: "USER01");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not match OrderMutasi destination*");
    }

    #endregion

    #region ADD ITEM COMMAND TESTS

    [Fact]
    public async Task AddItem_NonPharmacyItem_AddsItemSuccessfully()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000001A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000001A")))
            .Returns(MayBe.From(draft));

        var atkBrg = CreateMockBrg("ATK01", "Kertas HVS A4", isAktif: true, groupRekDkId: "ATK");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "ATK01")))
            .Returns(MayBe.From(atkBrg));

        var satuan = new SatuanType("RIM", "Rim");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "RIM")))
            .Returns(MayBe.From(satuan));

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000001A", "ATK01", BatchNo: "", ExpiredDate: "", QtyKirim: 5m, SatuanId: "RIM");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBrgModel>(m =>
            m.ListItem.Count == 1 &&
            m.ListItem[0].Brg.BrgId == "ATK01" &&
            m.ListItem[0].QtyKirim == 5m &&
            m.ListItem[0].Satuan.SatuanId == "RIM" &&
            m.ListItem[0].NoUrut == 1
        )), Times.Once);
    }

    [Fact]
    public async Task AddItem_PharmacyItem_WithBatchAndExpiredDate_AddsItemSuccessfully()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000002A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000002A")))
            .Returns(MayBe.From(draft));

        var obatBrg = CreateMockBrg("MED01", "Paracetamol 500mg", isAktif: true, groupRekDkId: "OBT");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "MED01")))
            .Returns(MayBe.From(obatBrg));

        var satuan = new SatuanType("STRIP", "Strip");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "STRIP")))
            .Returns(MayBe.From(satuan));

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000002A", "MED01", BatchNo: "BATCH-999", ExpiredDate: "2027-06-30", QtyKirim: 20m, SatuanId: "STRIP");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBrgModel>(m =>
            m.ListItem.Count == 1 &&
            m.ListItem[0].Brg.BrgId == "MED01" &&
            m.ListItem[0].BatchNo == "BATCH-999" &&
            m.ListItem[0].ExpiredDate == "2027-06-30" &&
            m.ListItem[0].QtyKirim == 20m
        )), Times.Once);
    }

    [Fact]
    public async Task AddItem_WhenMutasiNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBrgKey>()))
            .Returns(MayBe<MutasiBrgModel>.None);

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT_NONE123", "B01", "B01", "2027-01-01", 1m, "PCS");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*MutasiBrg with Id 'MT_NONE123' not found*");
    }

    [Fact]
    public async Task AddItem_WhenMutasiNotDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000003A");
        draft.AddItem(MutasiBrgItemModel.Create(new BrgReff("B01", "Item"), "B01", "2027-01-01", 1m, new SatuanType("PCS", "Pcs")));
        draft.Dispatch("USER01"); // State -> Dispatched

        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000003A")))
            .Returns(MayBe.From(draft));

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000003A", "B01", "B01", "2027-01-01", 1m, "PCS");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is not in Draft state*");
    }

    [Fact]
    public async Task AddItem_WhenBarangNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000004A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000004A")))
            .Returns(MayBe.From(draft));

        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "B_NONE")))
            .Returns(MayBe<IBrg>.None);

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000004A", "B_NONE", "", "", 1m, "PCS");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*BrgId 'B_NONE' not found*");
    }

    [Fact]
    public async Task AddItem_WhenBarangInactive_ThrowsInvalidOperationException()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000005A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000005A")))
            .Returns(MayBe.From(draft));

        var inactiveBrg = CreateMockBrg("B_INACT", "Obat Nonaktif", isAktif: false, groupRekDkId: "OBT");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "B_INACT")))
            .Returns(MayBe.From(inactiveBrg));

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000005A", "B_INACT", "B01", "2027-01-01", 1m, "PCS");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("Barang 'Obat Nonaktif' (B_INACT) berstatus Non-Aktif.");
    }

    [Fact]
    public async Task AddItem_WhenSatuanNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000006A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000006A")))
            .Returns(MayBe.From(draft));

        var activeBrg = CreateMockBrg("B01", "Barang Aktif", isAktif: true, groupRekDkId: "ATK");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "B01")))
            .Returns(MayBe.From(activeBrg));

        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "S_NONE")))
            .Returns(MayBe<SatuanType>.None);

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000006A", "B01", "", "", 1m, "S_NONE");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*SatuanId 'S_NONE' not found*");
    }

    [Fact]
    public async Task AddItem_PharmacyItem_WithEmptyBatchNo_AndValidExpiredDate_AddsItemSuccessfully()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000007A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000007A")))
            .Returns(MayBe.From(draft));

        var obatBrg = CreateMockBrg("MED02", "Amoxicillin", isAktif: true, groupRekDkId: "OBT");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "MED02")))
            .Returns(MayBe.From(obatBrg));

        var satuan = new SatuanType("BOX", "Box");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "BOX")))
            .Returns(MayBe.From(satuan));

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000007A", "MED02", BatchNo: "", ExpiredDate: "2027-01-01", QtyKirim: 10m, SatuanId: "BOX");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBrgModel>(m =>
            m.ListItem.Count == 1 &&
            m.ListItem[0].Brg.BrgId == "MED02" &&
            m.ListItem[0].BatchNo == "" &&
            m.ListItem[0].ExpiredDate == "2027-01-01" &&
            m.ListItem[0].QtyKirim == 10m
        )), Times.Once);
    }

    [Fact]
    public async Task AddItem_PharmacyItem_MissingOrInvalidExpiredDate_ThrowsArgumentException()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000008A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000008A")))
            .Returns(MayBe.From(draft));

        var bhpBrg = CreateMockBrg("BHP01", "Spuit 3cc", isAktif: true, groupRekDkId: "BHP");
        _brgRepoMock.Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BHP01")))
            .Returns(MayBe.From(bhpBrg));

        var satuan = new SatuanType("PCS", "Pcs");
        _satuanRepoMock.Setup(x => x.LoadEntity(It.Is<ISatuanKey>(k => k.SatuanId == "PCS")))
            .Returns(MayBe.From(satuan));

        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000008A", "BHP01", BatchNo: "BATCH-1", ExpiredDate: "not-a-date", QtyKirim: 10m, SatuanId: "PCS");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ArgumentException>();
        ex.WithMessage("*ExpiredDate wajib diisi dengan format tanggal yang valid untuk barang farmasi*");
    }

    [Fact]
    public async Task AddItem_WhenQtyKirimZeroOrNegative_ThrowsArgumentException()
    {
        // Arrange
        var handler = CreateAddItemHandler();
        var command = new MutasiBrgAddItemCommand("MT1000001A", "B01", "", "", 0m, "PCS");

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ArgumentException>();
        ex.WithMessage("*QtyKirim harus lebih dari 0*");
    }

    #endregion

    #region REMOVE ITEM COMMAND TESTS

    [Fact]
    public async Task RemoveItem_ValidDraftItem_RemovesItemSuccessfully()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000010A");
        var item1 = MutasiBrgItemModel.Create(1, new BrgReff("B01", "Barang 1"), "", "", 5m, new SatuanType("PCS", "Pcs"));
        var item2 = MutasiBrgItemModel.Create(2, new BrgReff("B02", "Barang 2"), "", "", 10m, new SatuanType("PCS", "Pcs"));
        draft.AddItem(item1);
        draft.AddItem(item2);

        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000010A")))
            .Returns(MayBe.From(draft));

        var handler = CreateRemoveItemHandler();
        var command = new MutasiBrgRemoveItemCommand("MT1000010A", NoUrut: 1, UserId: "USER01");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _mutasiBrgRepoMock.Verify(x => x.SaveChanges(It.Is<MutasiBrgModel>(m =>
            m.ListItem.Count == 1 &&
            m.ListItem[0].Brg.BrgId == "B02" &&
            m.ListItem[0].NoUrut == 1 // Reordered to 1
        )), Times.Once);
    }

    [Fact]
    public async Task RemoveItem_WhenMutasiNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.IsAny<IMutasiBrgKey>()))
            .Returns(MayBe<MutasiBrgModel>.None);

        var handler = CreateRemoveItemHandler();
        var command = new MutasiBrgRemoveItemCommand("MT_NONE123", NoUrut: 1);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*MutasiBrg with Id 'MT_NONE123' not found*");
    }

    [Fact]
    public async Task RemoveItem_WhenMutasiNotDraft_ThrowsInvalidOperationException()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000011A");
        draft.AddItem(MutasiBrgItemModel.Create(new BrgReff("B01", "Barang"), "", "", 1m, new SatuanType("PCS", "Pcs")));
        draft.Dispatch("USER01"); // State -> Dispatched

        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000011A")))
            .Returns(MayBe.From(draft));

        var handler = CreateRemoveItemHandler();
        var command = new MutasiBrgRemoveItemCommand("MT1000011A", NoUrut: 1);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is not in Draft state*");
    }

    [Fact]
    public async Task RemoveItem_WhenItemNotFound_ThrowsArgumentException()
    {
        // Arrange
        var draft = CreateSampleDraft("MT1000012A");
        _mutasiBrgRepoMock.Setup(x => x.LoadEntity(It.Is<IMutasiBrgKey>(k => k.MutasiBrgId == "MT1000012A")))
            .Returns(MayBe.From(draft));

        var handler = CreateRemoveItemHandler();
        var command = new MutasiBrgRemoveItemCommand("MT1000012A", NoUrut: 99);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ArgumentException>();
        ex.WithMessage("*Item dengan NoUrut 99 tidak ditemukan*");
    }

    [Fact]
    public async Task RemoveItem_WhenNoUrutInvalid_ThrowsArgumentException()
    {
        // Arrange
        var handler = CreateRemoveItemHandler();
        var command = new MutasiBrgRemoveItemCommand("MT1000012A", NoUrut: 0);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
