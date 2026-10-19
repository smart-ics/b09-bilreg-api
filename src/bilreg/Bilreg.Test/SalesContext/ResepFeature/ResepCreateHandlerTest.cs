using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Application.SalesContext.ResepFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

public class ResepCreateHandlerTest
{
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<ILayananRepo> _layananRepo = new();
    private readonly Mock<ITipeBrgRepo> _tipeBrgRepo = new();
    private readonly Mock<IBrgRepo> _brgRepo = new();
    private readonly Mock<ISatuanRepo> _satuanRepo = new();
    private readonly Mock<IResepRepo> _resepRepo = new();
    private readonly Mock<IResepCreateDal> _resepDal = new();

    private readonly ResepCreateHandler _sut;

    public ResepCreateHandlerTest()
    {
        _sut = new ResepCreateHandler(
            _regRepo.Object,
            _ppaRepo.Object,
            _layananRepo.Object,
            _tipeBrgRepo.Object,
            _brgRepo.Object,
            _satuanRepo.Object,
            _resepRepo.Object,
            _resepDal.Object);

        SetupDefaultMocks();
    }

    private void SetupDefaultMocks()
    {
        _regRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.Some(RegModel.Default));

        var defaultPpa = PpaType.Default with { PpaId = "DOK01", PpaName = "dr. Albertus Sp.A" };
        _ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe<PpaType>.Some(defaultPpa));

        var defaultLayanan = LayananType.Default with { LayananId = "POLI01", LayananName = "Poli Anak" };
        _layananRepo.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "POLI01" || k.LayananId == "-")))
            .Returns(MayBe<LayananType>.Some(defaultLayanan));

        var defaultTipeBrg = TipeBrgType.Default with { TipeBrgId = "TB01", TipeBrgName = "Reguler" };
        _tipeBrgRepo.Setup(x => x.LoadEntity(It.IsAny<ITipeBrgKey>()))
            .Returns(MayBe<TipeBrgType>.Some(defaultTipeBrg));

        _brgRepo.Setup(x => x.LoadEntity(It.IsAny<IBrgKey>()))
            .Returns((IBrgKey k) => MayBe<IBrg>.Some(BrgObatType.Default with { BrgId = k.BrgId, BrgName = $"Obat {k.BrgId}" }));

        _satuanRepo.Setup(x => x.LoadEntity(It.IsAny<ISatuanKey>()))
            .Returns((ISatuanKey k) => MayBe<SatuanType>.Some(SatuanType.Create(k.SatuanId, "Tablet")));

        _resepDal.Setup(x => x.FindPotentialDuplicate(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Enumerable.Empty<IResepKey>());
    }

    private static ResepCreateCommand CreateValidCommand(
        IEnumerable<ResepCreateObatCommand>? listObat = null,
        string layananStokId = "",
        DateTime? tglRencanaPeriksa = null,
        string resepAsalReffId = "",
        bool isConfirmedDuplicate = false,
        int iter = 0)
    {
        var defaultListObat = new List<ResepCreateObatCommand>
        {
            new(
                BrgId: "BRG01",
                SatuanId: "TAB",
                Qty: 10m,
                Iter: 0,
                Signa: "3dd1",
                Instruction: "3x1 tablet sehari",
                Note: "Diminum sesudah makan",
                ListItemRacik: Enumerable.Empty<ResepCreateItemRacikCommand>())
        };

        return new ResepCreateCommand(
            RegId: "REG001",
            PpaId: "DOK01",
            LayananId: "POLI01",
            UrgenitasId: "RUTIN",
            TipeBrgId: "TB01",
            Iter: iter,
            Description: "Resep rawat jalan",
            BeratBadan: 60m,
            TinggiBadan: 170m,
            Lpb: 1.7m,
            UserId: "USER01",
            ListObat: listObat ?? defaultListObat,
            LayananStokId: layananStokId,
            TglRencanaPeriksa: tglRencanaPeriksa,
            ResepAsalReffId: resepAsalReffId,
            IsConfirmedDuplicate: isConfirmedDuplicate);
    }

    [Fact]
    public async Task Handle_ValidNonRacik_CreatesResepAndCallsSaveChanges()
    {
        // Arrange
        var cmd = CreateValidCommand();
        ResepModel? savedModel = null;
        _resepRepo.Setup(x => x.SaveChanges(It.IsAny<ResepModel>()))
            .Callback<ResepModel>(m => savedModel = m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ResepId.Should().StartWith("KP");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Once);

        savedModel.Should().NotBeNull();
        savedModel!.ListObat.Should().HaveCount(1);
        savedModel.ListObat.First().Brg.BrgId.Should().Be("BRG01");
        savedModel.ListObat.First().IsRacik.Should().BeFalse();
        savedModel.AuditTrail.Created.UserId.Should().Be("USER01");
    }

    [Fact]
    public async Task Handle_WithExplicitTargetDepoAndPlannedDate_SetsModelProperties()
    {
        // Arrange
        var targetDepo = LayananType.Default with { LayananId = "DEPO01", LayananName = "Depo Farmasi Rajal" };
        _layananRepo.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "DEPO01")))
            .Returns(MayBe<LayananType>.Some(targetDepo));

        var plannedDate = DateTime.Today.AddDays(2);
        var cmd = CreateValidCommand(
            layananStokId: "DEPO01",
            tglRencanaPeriksa: plannedDate,
            resepAsalReffId: "KP-INDUK-99");

        ResepModel? savedModel = null;
        _resepRepo.Setup(x => x.SaveChanges(It.IsAny<ResepModel>()))
            .Callback<ResepModel>(m => savedModel = m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedModel.Should().NotBeNull();
        savedModel!.LayananStok.LayananId.Should().Be("DEPO01");
        savedModel.LayananStok.LayananName.Should().Be("Depo Farmasi Rajal");
        savedModel.TglRencanaPeriksa.Date.Should().Be(plannedDate.Date);
        savedModel.ResepAsalReffId.Should().Be("KP-INDUK-99");
    }

    [Fact]
    public async Task Handle_DefaultDepoAndPlannedDate_WhenNotSpecified_SetsFallback()
    {
        // Arrange
        var cmd = CreateValidCommand(layananStokId: "", tglRencanaPeriksa: null);
        ResepModel? savedModel = null;
        _resepRepo.Setup(x => x.SaveChanges(It.IsAny<ResepModel>()))
            .Callback<ResepModel>(m => savedModel = m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedModel.Should().NotBeNull();
        savedModel!.LayananStok.LayananId.Should().Be("POLI01");
        savedModel.TglRencanaPeriksa.Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task Handle_WithStructuredEtiket_SetsStructuredFieldsCorrectly()
    {
        // Arrange
        var listObat = new List<ResepCreateObatCommand>
        {
            new(
                BrgId: "BRG01",
                SatuanId: "TAB",
                Qty: 15m,
                Iter: 0,
                Signa: "3dd1",
                Instruction: "3x1 tablet",
                Note: "Sesudah makan",
                ListItemRacik: Enumerable.Empty<ResepCreateItemRacikCommand>(),
                IsRacik: false,
                Frequency: 3,
                UnitDose: 1.5m,
                Route: "ORAL",
                MealRelation: "Sesudah makan",
                UddTime: "08:00, 14:00, 20:00")
        };
        var cmd = CreateValidCommand(listObat: listObat);

        ResepModel? savedModel = null;
        _resepRepo.Setup(x => x.SaveChanges(It.IsAny<ResepModel>()))
            .Callback<ResepModel>(m => savedModel = m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedModel.Should().NotBeNull();
        var item = savedModel!.ListObat.First();
        item.Etiket.Frequency.Should().Be(3);
        item.Etiket.UnitDose.Should().Be(1.5m);
        item.Etiket.Route.Should().Be("ORAL");
        item.Etiket.MealRelation.Should().Be("Sesudah makan");
        item.Etiket.UddTime.Should().Be("08:00, 14:00, 20:00");
    }

    [Fact]
    public async Task Handle_ValidRacikWithComponents_CreatesResepWithRacikan()
    {
        // Arrange
        var components = new List<ResepCreateItemRacikCommand>
        {
            new(BrgId: "CMP01", SatuanId: "TAB", Qty: 5m, Dosis: 250m, DosisTxt: "250 mg"),
            new(BrgId: "CMP02", SatuanId: "TAB", Qty: 5m, Dosis: 500m, DosisTxt: "500 mg")
        };
        var listObat = new List<ResepCreateObatCommand>
        {
            new(
                BrgId: "RCK01",
                SatuanId: "BKS",
                Qty: 10m,
                Iter: 0,
                Signa: "3dd1",
                Instruction: "3x1 bungkus puyer",
                Note: "Puyer racik batuk",
                ListItemRacik: components,
                IsRacik: true)
        };
        var cmd = CreateValidCommand(listObat: listObat);

        ResepModel? savedModel = null;
        _resepRepo.Setup(x => x.SaveChanges(It.IsAny<ResepModel>()))
            .Callback<ResepModel>(m => savedModel = m);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        savedModel.Should().NotBeNull();
        savedModel!.ListObat.Should().HaveCount(1);
        var racik = savedModel.ListObat.First();
        racik.IsRacik.Should().BeTrue();
        racik.ListItemRacik.Should().HaveCount(2);
        racik.ListItemRacik.Select(x => x.Brg.BrgId).Should().Contain(["CMP01", "CMP02"]);
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyListObat_ThrowsInvalidOperationException_EX01()
    {
        // Arrange (EX-01: Resep tanpa item obat)
        var cmd = CreateValidCommand(listObat: Enumerable.Empty<ResepCreateObatCommand>());

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*minimal 1 item obat*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RacikWithoutComponents_ThrowsInvalidOperationException_EX02()
    {
        // Arrange (EX-02: Wadah racikan tanpa bahan baku)
        var listObat = new List<ResepCreateObatCommand>
        {
            new(
                BrgId: "RCK-KOSONG",
                SatuanId: "BKS",
                Qty: 10m,
                Iter: 0,
                Signa: "3dd1",
                Instruction: "3x1 bungkus",
                Note: "",
                ListItemRacik: Enumerable.Empty<ResepCreateItemRacikCommand>(),
                IsRacik: true)
        };
        var cmd = CreateValidCommand(listObat: listObat);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Wadah racikan 'RCK-KOSONG' harus memiliki minimal 1 bahan komponen*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PotentialDuplicateDetected_WhenNotConfirmed_ThrowsArgumentException_EX04()
    {
        // Arrange (EX-04: Potensi duplikasi resep pada hari yang sama)
        _resepDal.Setup(x => x.FindPotentialDuplicate(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new List<IResepKey> { ResepModel.Key("KP-EXISTING-001") });

        var cmd = CreateValidCommand(isConfirmedDuplicate: false);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*potensi duplikasi*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PotentialDuplicateDetected_WhenConfirmed_Succeeds_EX04()
    {
        // Arrange (EX-04: Potensi duplikasi resep tapi sudah dikonfirmasi DPJP)
        _resepDal.Setup(x => x.FindPotentialDuplicate(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<IEnumerable<string>>()))
            .Returns(new List<IResepKey> { ResepModel.Key("KP-EXISTING-001") });

        var cmd = CreateValidCommand(isConfirmedDuplicate: true);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NegativeIter_ThrowsArgumentException_EX05()
    {
        // Arrange (EX-05: Iter negatif)
        var cmd = CreateValidCommand(iter: -1);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Iter tidak boleh bernilai negatif*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveRegister_ThrowsArgumentException()
    {
        // Arrange
        var inactiveReg = RegModel.Default;
        inactiveReg.BatalBerobat("USER_BATAL", DateTime.Now);

        _regRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.Some(inactiveReg));

        var cmd = CreateValidCommand();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak aktif*");
        _resepRepo.Verify(x => x.SaveChanges(It.IsAny<ResepModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RegisterNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _regRepo.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        var cmd = CreateValidCommand();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Register REG001 not found*");
    }

    [Fact]
    public async Task Handle_DoctorNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _ppaRepo.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>()))
            .Returns(MayBe<PpaType>.None);

        var cmd = CreateValidCommand();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*PPA 'DOK01' invalid*");
    }

    [Fact]
    public async Task Handle_LayananNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _layananRepo.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "POLI01")))
            .Returns(MayBe<LayananType>.None);

        var cmd = CreateValidCommand();

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Layanan POLI01 not found*");
    }

    [Fact]
    public async Task Handle_TargetDepoNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _layananRepo.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "DEPO_INVALID")))
            .Returns(MayBe<LayananType>.None);

        var cmd = CreateValidCommand(layananStokId: "DEPO_INVALID");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Layanan Stok 'DEPO_INVALID' not found*");
    }

    [Fact]
    public async Task Handle_NullUserId_ThrowsArgumentException()
    {
        // Arrange
        var cmd = new ResepCreateCommand(
            RegId: "REG001",
            PpaId: "DOK01",
            LayananId: "POLI01",
            UrgenitasId: "RUTIN",
            TipeBrgId: "TB01",
            Iter: 0,
            Description: "Resep rawat jalan",
            BeratBadan: 60m,
            TinggiBadan: 170m,
            Lpb: 1.7m,
            UserId: "",
            ListObat: new List<ResepCreateObatCommand>
            {
                new("BRG01", "TAB", 10m, 0, "3dd1", "3x1", "", Enumerable.Empty<ResepCreateItemRacikCommand>())
            });

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullResepDal_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ResepCreateHandler(
            _regRepo.Object,
            _ppaRepo.Object,
            _layananRepo.Object,
            _tipeBrgRepo.Object,
            _brgRepo.Object,
            _satuanRepo.Object,
            _resepRepo.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
