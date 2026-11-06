using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.DepositFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.DepositFeature;

public class DepositCreateHandlerTest
{
    private readonly Mock<IDepositRepo> _depositRepoMock;
    private readonly Mock<IRegRepo> _regRepoMock;
    private readonly Mock<ILayananRepo> _layananRepoMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly DepositCreateHandler _sut;

    public DepositCreateHandlerTest()
    {
        _depositRepoMock = new Mock<IDepositRepo>();
        _regRepoMock = new Mock<IRegRepo>();
        _layananRepoMock = new Mock<ILayananRepo>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();

        _tglJamProviderMock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 22, 10, 0, 0));

        _sut = new DepositCreateHandler(
            _depositRepoMock.Object,
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    [Fact]
    public async Task GivenValidRequestForRawatInapWithMultiBangsal_WhenHandle_ThenSavesDepositAndReturnsDepositId()
    {
        // Arrange
        var cmd = new DepositCreateCmd(
            RegId: "RGA6R7FJSX",
            LayananId: "3RI01",
            Keterangan: "Deposit Awal Ranap",
            NilaiDeposit: 500_000m,
            UserId: "USER01");

        var fakeRegReff = new RegReff("RGA6R7FJSX", "MR001", "Pasien Rawat Inap");
        var fakeLayanan = LayananType.Default with
        {
            LayananId = "3RI01",
            LayananName = "Layanan Rawat Inap Multi-Bangsal"
        };

        _regRepoMock
            .Setup(x => x.LoadReff(It.Is<IRegKey>(k => k.RegId == "RGA6R7FJSX")))
            .Returns(MayBe.From(fakeRegReff));

        _layananRepoMock
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "3RI01")))
            .Returns(MayBe.From(fakeLayanan));

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.DepositId.Should().StartWith("DP");

        _depositRepoMock.Verify(
            x => x.SaveChanges(It.Is<DepositModel>(d =>
                d.Reg.RegId == "RGA6R7FJSX"
                && d.Layanan.LayananId == "3RI01"
                && d.NilaiDeposit == 500_000m
                && d.Keterangan == "Deposit Awal Ranap"
                && d.Audit.Created.UserId == "USER01"
                && d.Audit.Created.Timestamp == new DateTime(2026, 10, 22, 10, 0, 0))),
            Times.Once);
    }

    [Fact]
    public async Task GivenRegNotFoundOrInactive_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new DepositCreateCmd(
            RegId: "RG_INACTIVE",
            LayananId: "3RI01",
            Keterangan: "Deposit Inactive",
            NilaiDeposit: 100_000m,
            UserId: "USER01");

        _regRepoMock
            .Setup(x => x.LoadReff(It.Is<IRegKey>(k => k.RegId == "RG_INACTIVE")))
            .Returns(MayBe<RegReff>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi RG_INACTIVE tidak ditemukan atau sudah tidak aktif.*");
        _depositRepoMock.Verify(x => x.SaveChanges(It.IsAny<DepositModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenLayananNotFound_WhenHandle_ThenThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new DepositCreateCmd(
            RegId: "RG00000001",
            LayananId: "LYN_NOT_FOUND",
            Keterangan: "Deposit Layanan Not Found",
            NilaiDeposit: 100_000m,
            UserId: "USER01");

        var fakeRegReff = new RegReff("RG00000001", "MR001", "Pasien Test");

        _regRepoMock
            .Setup(x => x.LoadReff(It.Is<IRegKey>(k => k.RegId == "RG00000001")))
            .Returns(MayBe.From(fakeRegReff));

        _layananRepoMock
            .Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == "LYN_NOT_FOUND")))
            .Returns(MayBe<LayananType>.None);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Layanan LYN_NOT_FOUND tidak ditemukan.*");
        _depositRepoMock.Verify(x => x.SaveChanges(It.IsAny<DepositModel>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenInvalidRegId_WhenHandle_ThenThrowsArgumentException(string? invalidRegId)
    {
        // Arrange
        var cmd = new DepositCreateCmd(
            RegId: invalidRegId!,
            LayananId: "3RI01",
            Keterangan: "Deposit Test",
            NilaiDeposit: 100_000m,
            UserId: "USER01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenInvalidLayananId_WhenHandle_ThenThrowsArgumentException(string? invalidLayananId)
    {
        // Arrange
        var cmd = new DepositCreateCmd(
            RegId: "RG00000001",
            LayananId: invalidLayananId!,
            Keterangan: "Deposit Test",
            NilaiDeposit: 100_000m,
            UserId: "USER01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenInvalidUserId_WhenHandle_ThenThrowsArgumentException(string? invalidUserId)
    {
        // Arrange
        var cmd = new DepositCreateCmd(
            RegId: "RG00000001",
            LayananId: "3RI01",
            Keterangan: "Deposit Test",
            NilaiDeposit: 100_000m,
            UserId: invalidUserId!);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public async Task GivenZeroOrNegativeNilaiDeposit_WhenHandle_ThenThrowsArgumentException(decimal invalidNilai)
    {
        // Arrange
        var cmd = new DepositCreateCmd(
            RegId: "RG00000001",
            LayananId: "3RI01",
            Keterangan: "Deposit Test",
            NilaiDeposit: invalidNilai,
            UserId: "USER01");

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
