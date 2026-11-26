using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Application.Shared.User;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class VoidTransaksiKasirCommandTest
{
    private readonly Mock<ITrsKasirRepo> _trsKasirRepoMock;
    private readonly Mock<ITrsKasirLegacyDal> _legacyDalMock;
    private readonly Mock<IKasirSupervisorAuthDal> _supervisorAuthDalMock;
    private readonly Mock<IUsmanGetUserService> _usmanGetUserServiceMock;
    private readonly Mock<IKasirStatusSyncDal> _statusSyncDalMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly VoidTransaksiKasirHandler _sut;
    private readonly VoidTransaksiKasirValidator _validator;

    private static readonly DateTime FixedNow = new(2026, 11, 7, 12, 0, 0);

    public VoidTransaksiKasirCommandTest()
    {
        _trsKasirRepoMock = new Mock<ITrsKasirRepo>();
        _legacyDalMock = new Mock<ITrsKasirLegacyDal>();
        _supervisorAuthDalMock = new Mock<IKasirSupervisorAuthDal>();
        _usmanGetUserServiceMock = new Mock<IUsmanGetUserService>();
        _usmanGetUserServiceMock
            .Setup(x => x.Execute(It.IsAny<UsmanGetUserRequest>()))
            .Throws(new ArgumentException("User invalid"));
        _statusSyncDalMock = new Mock<IKasirStatusSyncDal>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _tglJamProviderMock.Setup(x => x.Now).Returns(FixedNow);

        _sut = new VoidTransaksiKasirHandler(
            _trsKasirRepoMock.Object,
            _legacyDalMock.Object,
            _supervisorAuthDalMock.Object,
            _usmanGetUserServiceMock.Object,
            _statusSyncDalMock.Object,
            _tglJamProviderMock.Object);
        _validator = new VoidTransaksiKasirValidator();
    }

    private static TrsKasirModel CreateSampleBerhasilModel()
    {
        var pTunai = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m, 0m);
        return TrsKasirModel.CreateTerimaKas(
            shiftId: "SH26000001",
            kasirId: "KASIR-01",
            regId: "RG260001",
            pasienId: "RM001",
            pasienName: "Budi Santoso",
            orderRef: "BL260001",
            keterangan: "Pembayaran Billing",
            totalNilai: 100_000m,
            payments: new[] { pTunai },
            trsTime: FixedNow.AddHours(-1));
    }

    private static UsmanGetUserResponse CreateUsmanUser(string email, params string[] roles)
        => new(
            pegId: "PG001",
            userName: "Supervisor Kasir",
            UserLogin: email,
            email: email,
            expiredDate: "2027-12-31",
            listRole: roles.Select(r => new UsmanGetUserRoleResponse(r)));

    [Fact]
    public async Task GivenValidRequest_WhenSupervisorValidAndTransactionBerhasil_ThenVoidsSavesToRepoDualWritesLegacyReversalsAndReturnsResponse()
    {
        // Arrange
        var pTunai = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m, 0m);
        var model = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH26000001",
            kasirId: "KASIR-01",
            regId: "RG260001",
            pasienId: "RM001",
            pasienName: "Budi Santoso",
            orderRef: "BL260001",
            keterangan: "Pembayaran Billing",
            totalNilai: 100_000m,
            payments: new[] { pTunai },
            trsTime: FixedNow.AddHours(-1));

        _supervisorAuthDalMock
            .Setup(x => x.ValidateSupervisor("SPV01", "password123"))
            .Returns(true);

        _trsKasirRepoMock
            .Setup(x => x.GetKey(model.TrsKasirId))
            .Returns(model);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: model.TrsKasirId,
            SupervisorUserId: "SPV01",
            SupervisorPassword: "password123",
            AlasanVoid: "Salah nominal bayar tagihan pasien");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TrsKasirId.Should().Be(model.TrsKasirId);
        response.StatusTrs.Should().Be(KasirConst.StatusTrsVoid);
        response.PetugasVoid.Should().Be("SPV01");
        response.AlasanVoid.Should().Be("Salah nominal bayar tagihan pasien");
        response.TglVoid.Should().Be("2026-11-07");
        response.JamVoid.Should().Be("12:00:00");

        model.IsVoid.Should().BeTrue();
        _supervisorAuthDalMock.Verify(x => x.ValidateSupervisor("SPV01", "password123"), Times.Once);
        _trsKasirRepoMock.Verify(x => x.Save(model), Times.Once);
        _legacyDalMock.Verify(x => x.Void(model.TrsKasirId, "2026-11-07", "12:00:00", "SPV01"), Times.Once);
        _statusSyncDalMock.Verify(x => x.Reversal("BL260001", model.TrsKasirId, "RG260001"), Times.Once);
    }

    [Fact]
    public async Task GivenInvalidSupervisor_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        _supervisorAuthDalMock
            .Setup(x => x.ValidateSupervisor("SPV01", "wrongpass"))
            .Returns(false);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: "DX00000001",
            SupervisorUserId: "SPV01",
            SupervisorPassword: "wrongpass",
            AlasanVoid: "Salah nominal bayar tagihan pasien");

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*Kredensial supervisor tidak valid atau user tidak memiliki hak otorisasi void kasir.*");

        _trsKasirRepoMock.Verify(x => x.Save(It.IsAny<TrsKasirModel>()), Times.Never);
        _legacyDalMock.Verify(x => x.Void(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _statusSyncDalMock.Verify(x => x.Reversal(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenAlasanVoidLessThan10Chars_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: "DX00000001",
            SupervisorUserId: "SPV01",
            SupervisorPassword: "password123",
            AlasanVoid: "Pendek");

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*minimal 10 karakter*");
    }

    [Fact]
    public async Task GivenNonExistentTransaction_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        _supervisorAuthDalMock
            .Setup(x => x.ValidateSupervisor("SPV01", "password123"))
            .Returns(true);

        _trsKasirRepoMock
            .Setup(x => x.GetKey("DX99999999"))
            .Returns((TrsKasirModel?)null);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: "DX99999999",
            SupervisorUserId: "SPV01",
            SupervisorPassword: "password123",
            AlasanVoid: "Salah input pembayaran pasien");

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*tidak ditemukan*");
    }

    [Fact]
    public async Task GivenAlreadyVoidedTransaction_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var pTunai = TrsKasirPaymentType.Create(KasirConst.MetodeBayarTunai, 100_000m, 0m);
        var model = TrsKasirModel.CreateTerimaKas(
            shiftId: "SH26000001",
            kasirId: "KASIR-01",
            regId: "RG260001",
            pasienId: "RM001",
            pasienName: "Budi",
            orderRef: "BL260001",
            keterangan: "Pembayaran",
            totalNilai: 100_000m,
            payments: new[] { pTunai },
            trsTime: FixedNow.AddHours(-2));

        model.Void("SUPERVISOR1", "Batal pertama tagihan", FixedNow.AddHours(-1));

        _supervisorAuthDalMock
            .Setup(x => x.ValidateSupervisor("SPV01", "password123"))
            .Returns(true);

        _trsKasirRepoMock
            .Setup(x => x.GetKey(model.TrsKasirId))
            .Returns(model);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: model.TrsKasirId,
            SupervisorUserId: "SPV01",
            SupervisorPassword: "password123",
            AlasanVoid: "Batal kedua tagihan kasir");

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*sudah berstatus VOID*");
    }

    [Theory]
    [InlineData("celestial")]
    [InlineData("ksr-spv")]
    [InlineData("CELESTIAL")]
    [InlineData(" Ksr-Spv ")]
    public async Task GivenUsmanUserWithAuthorizedRole_WhenHandle_ThenSucceedsWithoutFallback(string role)
    {
        // Arrange
        var model = CreateSampleBerhasilModel();
        var usmanUser = CreateUsmanUser("kasir-spv@email.com", "ksr-user", role);

        _usmanGetUserServiceMock
            .Setup(x => x.Execute(new UsmanGetUserRequest("kasir-spv@email.com", "kasirspv123", "Bilreg")))
            .Returns(usmanUser);

        _trsKasirRepoMock
            .Setup(x => x.GetKey(model.TrsKasirId))
            .Returns(model);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: model.TrsKasirId,
            SupervisorUserId: " kasir-spv@email.com ",
            SupervisorPassword: " kasirspv123 ",
            AlasanVoid: "Salah nominal bayar tagihan pasien");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.StatusTrs.Should().Be(KasirConst.StatusTrsVoid);
        response.PetugasVoid.Should().Be("PG001");
        model.PetugasVoid.Should().Be("PG001");
        _usmanGetUserServiceMock.Verify(
            x => x.Execute(new UsmanGetUserRequest("kasir-spv@email.com", "kasirspv123", "Bilreg")),
            Times.Once);
        _supervisorAuthDalMock.Verify(
            x => x.ValidateSupervisor(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _trsKasirRepoMock.Verify(x => x.Save(model), Times.Once);
        _legacyDalMock.Verify(x => x.Void(model.TrsKasirId, "2026-11-07", "12:00:00", "PG001"), Times.Once);
    }

    [Fact]
    public async Task GivenUsmanUserWithLongEmailAndPegId_WhenHandle_ThenUsesResolvedPegIdForPetugasVoid()
    {
        // Arrange
        var model = CreateSampleBerhasilModel();
        var usmanUser = new UsmanGetUserResponse(
            pegId: " KAS-UTAMA ",
            userName: "Kasir Utama",
            UserLogin: "kasirspv@email.com",
            email: "kasirspv@email.com",
            expiredDate: "2027-12-31",
            listRole: new[] { new UsmanGetUserRoleResponse("ksr-spv") });

        _usmanGetUserServiceMock
            .Setup(x => x.Execute(new UsmanGetUserRequest("kasirspv@email.com", "kasirspv123", "Bilreg")))
            .Returns(usmanUser);

        _trsKasirRepoMock
            .Setup(x => x.GetKey(model.TrsKasirId))
            .Returns(model);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: model.TrsKasirId,
            SupervisorUserId: "kasirspv@email.com",
            SupervisorPassword: "kasirspv123",
            AlasanVoid: "Salah nominal bayar tagihan pasien",
            PetugasVoid: "kasirspv@email.com");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        model.PetugasVoid.Should().Be("KAS-UTAMA");
        response.PetugasVoid.Should().Be("KAS-UTAMA");
        _legacyDalMock.Verify(
            x => x.Void(model.TrsKasirId, "2026-11-07", "12:00:00", "KAS-UTAMA"),
            Times.Once);
        _trsKasirRepoMock.Verify(x => x.Save(model), Times.Once);
    }

    [Fact]
    public async Task GivenUsmanUserWithOnlyKsrUserRole_WhenHandle_ThenThrowsWithoutFallback()
    {
        // Arrange
        var usmanUser = CreateUsmanUser("kasir@email.com", "ksr-user");

        _usmanGetUserServiceMock
            .Setup(x => x.Execute(new UsmanGetUserRequest("kasir@email.com", "kasir123", "Bilreg")))
            .Returns(usmanUser);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: "DX00000001",
            SupervisorUserId: "kasir@email.com",
            SupervisorPassword: "kasir123",
            AlasanVoid: "Salah nominal bayar tagihan pasien");

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*Kredensial supervisor tidak valid atau user tidak memiliki hak otorisasi void kasir.*");

        _usmanGetUserServiceMock.Verify(
            x => x.Execute(new UsmanGetUserRequest("kasir@email.com", "kasir123", "Bilreg")),
            Times.Once);
        _supervisorAuthDalMock.Verify(
            x => x.ValidateSupervisor(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _trsKasirRepoMock.Verify(x => x.Save(It.IsAny<TrsKasirModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenUsmanThrowsException_WhenLegacySupervisorValid_ThenFallbackSucceeds()
    {
        // Arrange
        var model = CreateSampleBerhasilModel();

        _usmanGetUserServiceMock
            .Setup(x => x.Execute(new UsmanGetUserRequest("SPVLEGACY", "legacy123", "Bilreg")))
            .Throws(new ArgumentException("User invalid"));

        _supervisorAuthDalMock
            .Setup(x => x.ValidateSupervisor("SPVLEGACY", "legacy123"))
            .Returns(true);

        _trsKasirRepoMock
            .Setup(x => x.GetKey(model.TrsKasirId))
            .Returns(model);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: model.TrsKasirId,
            SupervisorUserId: "SPVLEGACY",
            SupervisorPassword: "legacy123",
            AlasanVoid: "Salah nominal bayar tagihan pasien");

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.StatusTrs.Should().Be(KasirConst.StatusTrsVoid);
        _usmanGetUserServiceMock.Verify(
            x => x.Execute(new UsmanGetUserRequest("SPVLEGACY", "legacy123", "Bilreg")),
            Times.Once);
        _supervisorAuthDalMock.Verify(x => x.ValidateSupervisor("SPVLEGACY", "legacy123"), Times.Once);
        _trsKasirRepoMock.Verify(x => x.Save(model), Times.Once);
    }

    [Fact]
    public async Task GivenUsmanThrowsException_WhenLegacySupervisorInvalid_ThenThrowsBusinessValidationException()
    {
        // Arrange
        _usmanGetUserServiceMock
            .Setup(x => x.Execute(new UsmanGetUserRequest("SPVLEGACY", "wrongpass", "Bilreg")))
            .Throws(new ArgumentException("User invalid"));

        _supervisorAuthDalMock
            .Setup(x => x.ValidateSupervisor("SPVLEGACY", "wrongpass"))
            .Returns(false);

        var cmd = new VoidTransaksiKasirCommand(
            TrsKasirId: "DX00000001",
            SupervisorUserId: "SPVLEGACY",
            SupervisorPassword: "wrongpass",
            AlasanVoid: "Salah nominal bayar tagihan pasien");

        // Act
        Func<Task> act = async () => await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*Kredensial supervisor tidak valid atau user tidak memiliki hak otorisasi void kasir.*");

        _supervisorAuthDalMock.Verify(x => x.ValidateSupervisor("SPVLEGACY", "wrongpass"), Times.Once);
        _trsKasirRepoMock.Verify(x => x.Save(It.IsAny<TrsKasirModel>()), Times.Never);
    }

    [Fact]
    public void GivenValidCommand_WhenValidate_ThenPasses()
    {
        var cmd = new VoidTransaksiKasirCommand("DX01", "SPV01", "password123", "Alasan pembatalan valid");
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "SPV01", "password123", "Alasan pembatalan valid")]
    [InlineData("DX01", "", "password123", "Alasan pembatalan valid")]
    [InlineData("DX01", "SPV01", "", "Alasan pembatalan valid")]
    [InlineData("DX01", "SPV01", "password123", "")]
    [InlineData("DX01", "SPV01", "password123", "   ")]
    [InlineData("DX01", "SPV01", "password123", "Pendek")]
    public void GivenInvalidInputs_WhenValidate_ThenFails(string trsKasirId, string spvUser, string spvPass, string alasanVoid)
    {
        var cmd = new VoidTransaksiKasirCommand(trsKasirId, spvUser, spvPass, alasanVoid);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}
