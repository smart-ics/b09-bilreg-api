using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Domain.Shared;
using FluentAssertions;
using Moq;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.PaymentContext.KasirFeature.UseCases;

public class OpenKasirShiftCommandTest
{
    private readonly Mock<IKasirShiftRepo> _kasirShiftRepoMock;
    private readonly Mock<ITglJamProvider> _tglJamProviderMock;
    private readonly OpenKasirShiftHandler _sut;
    private readonly OpenKasirShiftValidator _validator;

    private static readonly DateTime FixedNow = new(2026, 11, 7, 8, 0, 0);

    public OpenKasirShiftCommandTest()
    {
        _kasirShiftRepoMock = new Mock<IKasirShiftRepo>();
        _tglJamProviderMock = new Mock<ITglJamProvider>();
        _tglJamProviderMock.Setup(x => x.Now).Returns(FixedNow);

        _sut = new OpenKasirShiftHandler(_kasirShiftRepoMock.Object, _tglJamProviderMock.Object);
        _validator = new OpenKasirShiftValidator();
    }

    [Fact]
    public async Task GivenValidRequest_WhenKasirHasNoActiveShift_ThenOpensShiftSavesToRepoAndReturnsResponse()
    {
        // Arrange
        var cmd = new OpenKasirShiftCommand(
            KodeShift: "1",
            KasirId: "KASIR-01",
            Loket: "Loket 01",
            SaldoAwal: 500_000m);

        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-01"))
            .Returns((KasirShiftModel?)null);

        KasirShiftModel? savedModel = null;
        _kasirShiftRepoMock
            .Setup(x => x.Save(It.IsAny<KasirShiftModel>()))
            .Callback<KasirShiftModel>(m => savedModel = m);

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.ShiftId.Should().StartWith("SH");
        response.KodeShift.Should().Be("1");
        response.KasirId.Should().Be("KASIR-01");
        response.Loket.Should().Be("Loket 01");
        response.SaldoAwal.Should().Be(500_000m);
        response.TglShift.Should().Be("2026-11-07");
        response.TglJamBuka.Should().Be("2026-11-07 08:00:00");
        response.StatusShift.Should().Be(KasirConst.StatusShiftBuka);

        _kasirShiftRepoMock.Verify(x => x.Save(It.IsAny<KasirShiftModel>()), Times.Once);
        savedModel.Should().NotBeNull();
        savedModel!.KasirId.Should().Be("KASIR-01");
        savedModel.IsOpen.Should().BeTrue();
    }

    [Fact]
    public async Task GivenKasirAlreadyHasActiveShift_WhenHandle_ThenThrowsBusinessValidationException()
    {
        // Arrange
        var cmd = new OpenKasirShiftCommand(
            KodeShift: "2",
            KasirId: "KASIR-01",
            Loket: "Loket 02",
            SaldoAwal: 100_000m);

        var existingShift = KasirShiftModel.OpenShift(
            kodeShift: "1",
            kasirId: "KASIR-01",
            loket: "Loket 01",
            saldoAwal: 200_000m,
            openTime: FixedNow.AddHours(-2));

        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-01"))
            .Returns(existingShift);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*masih memiliki sesi shift aktif*");

        _kasirShiftRepoMock.Verify(x => x.Save(It.IsAny<KasirShiftModel>()), Times.Never);
    }

    [Fact]
    public async Task GivenKasirHasClosedShift_WhenHandle_ThenAllowsNewShiftToOpen()
    {
        // Arrange
        var cmd = new OpenKasirShiftCommand(
            KodeShift: "2",
            KasirId: "KASIR-01",
            Loket: "Loket 02",
            SaldoAwal: 300_000m);

        // Previous shift closed, GetActiveShift returns null
        _kasirShiftRepoMock
            .Setup(x => x.GetActiveShift("KASIR-01"))
            .Returns((KasirShiftModel?)null);

        // Act
        var response = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.KodeShift.Should().Be("2");
        _kasirShiftRepoMock.Verify(x => x.Save(It.IsAny<KasirShiftModel>()), Times.Once);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("4")]
    [InlineData("A")]
    public async Task GivenInvalidKodeShift_WhenHandle_ThenThrowsBusinessValidationException(string invalidKode)
    {
        // Arrange
        var cmd = new OpenKasirShiftCommand(
            KodeShift: invalidKode,
            KasirId: "KASIR-01",
            Loket: "Loket 01",
            SaldoAwal: 100_000m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessValidationException>()
            .WithMessage("*Kode shift harus bernilai '1', '2', atau '3'*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GivenEmptyKasirId_WhenHandle_ThenThrowsArgumentException(string? invalidKasirId)
    {
        // Arrange
        var cmd = new OpenKasirShiftCommand(
            KodeShift: "1",
            KasirId: invalidKasirId!,
            Loket: "Loket 01",
            SaldoAwal: 100_000m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenNegativeSaldoAwal_WhenHandle_ThenThrowsArgumentException()
    {
        // Arrange
        var cmd = new OpenKasirShiftCommand(
            KodeShift: "1",
            KasirId: "KASIR-01",
            Loket: "Loket 01",
            SaldoAwal: -50_000m);

        // Act
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Validator_WhenValidCommand_ShouldPassValidation()
    {
        var cmd = new OpenKasirShiftCommand("1", "KASIR-01", "Loket 01", 100_000m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("4")]
    [InlineData("X")]
    public void Validator_WhenInvalidKodeShift_ShouldFailValidation(string kodeShift)
    {
        var cmd = new OpenKasirShiftCommand(kodeShift, "KASIR-01", "Loket 01", 100_000m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpenKasirShiftCommand.KodeShift));
    }

    [Fact]
    public void Validator_WhenEmptyKasirId_ShouldFailValidation()
    {
        var cmd = new OpenKasirShiftCommand("1", "", "Loket 01", 100_000m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpenKasirShiftCommand.KasirId));
    }

    [Fact]
    public void Validator_WhenNegativeSaldoAwal_ShouldFailValidation()
    {
        var cmd = new OpenKasirShiftCommand("1", "KASIR-01", "Loket 01", -10_000m);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(OpenKasirShiftCommand.SaldoAwal));
    }
}
