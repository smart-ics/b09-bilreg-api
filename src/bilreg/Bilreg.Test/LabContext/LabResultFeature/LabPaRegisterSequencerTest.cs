using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabPaRegisterSequencerTest
{
    private readonly Mock<ISequencer> _sequencerMock = new();

    [Theory]
    [InlineData("DISC-PAH", "H")]
    [InlineData("PAH", "H")]
    [InlineData("H", "H")]
    [InlineData("HISTOPATOLOGI", "H")]
    public void Generate_Histopathology_ReturnsHPrefixWithYearAndFourDigitSequence(string disciplineCode, string expectedPrefix)
    {
        var testDate = new DateTime(2026, 5, 18);
        _sequencerMock.Setup(s => s.GetNextNoUrut("PA_H_26")).Returns(1);

        var result = LabPaRegisterSequencer.NextNumber(_sequencerMock.Object, disciplineCode, testDate);

        result.Should().Be($"{expectedPrefix}.26.0001");
        _sequencerMock.Verify(s => s.GetNextNoUrut("PA_H_26"), Times.Once);
    }

    [Theory]
    [InlineData("DISC-PAS", "S")]
    [InlineData("PAS", "S")]
    [InlineData("S", "S")]
    [InlineData("SITOPATOLOGI", "S")]
    [InlineData("CYTOLOGY", "S")]
    public void Generate_Cytopathology_ReturnsSPrefixWithYearAndFourDigitSequence(string disciplineCode, string expectedPrefix)
    {
        var testDate = new DateTime(2026, 8, 1);
        _sequencerMock.Setup(s => s.GetNextNoUrut("PA_S_26")).Returns(42);

        var result = LabPaRegisterSequencer.NextNumber(_sequencerMock.Object, disciplineCode, testDate);

        result.Should().Be($"{expectedPrefix}.26.0042");
        _sequencerMock.Verify(s => s.GetNextNoUrut("PA_S_26"), Times.Once);
    }

    [Fact]
    public void Generate_Instance_DelegatesCorrectly()
    {
        var testDate = new DateTime(2027, 1, 10);
        _sequencerMock.Setup(s => s.GetNextNoUrut("PA_H_27")).Returns(1234);

        var service = new LabPaRegisterSequencer(_sequencerMock.Object);
        var result = service.Generate("DISC-PAH", testDate);

        result.Should().Be("H.27.1234");
    }

    [Theory]
    [InlineData("PK")]
    [InlineData("DISC-PK")]
    [InlineData("MIKRO")]
    [InlineData("XYZ")]
    public void Generate_UnknownDiscipline_ThrowsArgumentException(string invalidDiscipline)
    {
        var act = () => LabPaRegisterSequencer.NextNumber(_sequencerMock.Object, invalidDiscipline);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak dikenali untuk registrasi Patologi Anatomi*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_NullOrEmptyDiscipline_ThrowsArgumentException(string? emptyDiscipline)
    {
        var act = () => LabPaRegisterSequencer.NextNumber(_sequencerMock.Object, emptyDiscipline!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_NullSequencer_ThrowsArgumentNullException()
    {
        var act = () => LabPaRegisterSequencer.NextNumber(null!, "DISC-PAH");

        act.Should().Throw<ArgumentNullException>();
    }
}
