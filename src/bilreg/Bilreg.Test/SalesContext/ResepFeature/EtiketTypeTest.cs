using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

public class EtiketTypeTest
{
    [Fact]
    public void Default_ShouldReturnObjectWithDashAndZeroValues()
    {
        // Act
        var sut = EtiketType.Default;

        // Assert
        sut.Signa.Should().Be(AppConst.DASH);
        sut.Instruction.Should().Be(AppConst.DASH);
        sut.Frequency.Should().Be(0);
        sut.UnitDose.Should().Be(0);
        sut.Route.Should().Be(AppConst.DASH);
        sut.MealRelation.Should().Be(AppConst.DASH);
        sut.UddTime.Should().Be(AppConst.DASH);
        sut.Note.Should().Be(AppConst.DASH);
    }

    [Fact]
    public void Create_WithOnlyInstructionAndSigna_ShouldFallbackParseSigna()
    {
        // Act
        var sut = EtiketType.Create("3x1", "3x1 tablet", "Catatan");

        // Assert
        sut.Signa.Should().Be("3x1");
        sut.Instruction.Should().Be("3x1 tablet");
        sut.Frequency.Should().Be(3);
        sut.UnitDose.Should().Be(1m);
        sut.Route.Should().Be(AppConst.DASH);
        sut.MealRelation.Should().Be(AppConst.DASH);
        sut.UddTime.Should().Be(AppConst.DASH);
        sut.Note.Should().Be("Catatan");
    }

    [Fact]
    public void Create_WhenInstructionIsDash_ShouldParseFromSigna()
    {
        // Act
        var sut = EtiketType.Create("2dd 1/2", AppConst.DASH, "Bila perlu");

        // Assert
        sut.Signa.Should().Be("2dd 1/2");
        sut.Instruction.Should().Be(AppConst.DASH);
        sut.Frequency.Should().Be(2);
        sut.UnitDose.Should().Be(0.5m);
        sut.Note.Should().Be("Bila perlu");
    }

    [Fact]
    public void Create_WhenInstructionIsNotParseable_ShouldFallbackToSigna()
    {
        // Act - "Sesudah Makan" has no frequency/dose, so it falls back to parsing signa "3x1"
        var sut = EtiketType.Create("3x1", "Sesudah Makan", "Catatan");

        // Assert
        sut.Frequency.Should().Be(3);
        sut.UnitDose.Should().Be(1m);
        sut.Instruction.Should().Be("Sesudah Makan");
    }

    [Fact]
    public void Create_WhenBothSignaAndInstructionAreInvalid_ShouldThrowFormatException()
    {
        // Act
        var act = () => EtiketType.Create("invalid_signa", "invalid_instruction", "catatan");

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Create_WithStructuredParametersProvided_ShouldUseStructuredValuesWithoutCallingParser()
    {
        // Act - Arbitrary strings in signa/instruction that would fail SignaParser if invoked
        var sut = EtiketType.Create(
            "CUSTOM",
            "Aturan Khusus",
            frequency: 4,
            unitDose: 2.5m,
            route: "ORAL",
            mealRelation: "Sesudah Makan",
            uddTime: "06:00-12:00-18:00-24:00",
            note: "Minum dengan air putih");

        // Assert
        sut.Signa.Should().Be("CUSTOM");
        sut.Instruction.Should().Be("Aturan Khusus");
        sut.Frequency.Should().Be(4);
        sut.UnitDose.Should().Be(2.5m);
        sut.Route.Should().Be("ORAL");
        sut.MealRelation.Should().Be("Sesudah Makan");
        sut.UddTime.Should().Be("06:00-12:00-18:00-24:00");
        sut.Note.Should().Be("Minum dengan air putih");
    }

    [Fact]
    public void Create_WhenFrequencyIsZero_ShouldFallbackFrequencyFromSigna()
    {
        // Act - Frequency = 0, but UnitDose is explicitly 3m
        var sut = EtiketType.Create(
            "3x1",
            "3x1 tablet",
            frequency: 0,
            unitDose: 3m,
            route: "ORAL",
            mealRelation: "Sebelum Makan",
            uddTime: "Pagi-Siang-Malam",
            note: "Catatan");

        // Assert - Frequency parsed as 3, while explicit UnitDose (3m) is retained
        sut.Frequency.Should().Be(3);
        sut.UnitDose.Should().Be(3m);
        sut.Route.Should().Be("ORAL");
        sut.MealRelation.Should().Be("Sebelum Makan");
        sut.UddTime.Should().Be("Pagi-Siang-Malam");
    }

    [Fact]
    public void Create_WhenUnitDoseIsZero_ShouldFallbackUnitDoseFromSigna()
    {
        // Act - Frequency is explicitly 5, but UnitDose is 0
        var sut = EtiketType.Create(
            "3x2",
            "3x2 tablet",
            frequency: 5,
            unitDose: 0m,
            route: "TOP",
            mealRelation: "AppConst.DASH",
            uddTime: "Pagi",
            note: "Oles tipis");

        // Assert - Explicit Frequency (5) retained, UnitDose fallback parsed from "3x2" (2m)
        sut.Frequency.Should().Be(5);
        sut.UnitDose.Should().Be(2m);
        sut.Route.Should().Be("TOP");
        sut.Note.Should().Be("Oles tipis");
    }

    [Fact]
    public void Create_WithNullOrWhitespaceValues_ShouldNormalizeToDash()
    {
        // Act
        var sut = EtiketType.Create(
            null!,
            "  ",
            frequency: 1,
            unitDose: 1m,
            route: null!,
            mealRelation: "   ",
            uddTime: null!,
            note: "  ");

        // Assert
        sut.Signa.Should().Be(AppConst.DASH);
        sut.Instruction.Should().Be(AppConst.DASH);
        sut.Route.Should().Be(AppConst.DASH);
        sut.MealRelation.Should().Be(AppConst.DASH);
        sut.UddTime.Should().Be(AppConst.DASH);
        sut.Note.Should().Be(AppConst.DASH);
    }

    [Fact]
    public void Create_WithPositionalStructuredParameters_ShouldSetAllPropertiesCorrectly()
    {
        // Act
        var sut = EtiketType.Create(
            "2dd1",
            "2dd1 tablet",
            2,
            1m,
            "ORAL",
            "Bersama Makan",
            "08:00-20:00",
            "Harus habis");

        // Assert
        sut.Signa.Should().Be("2dd1");
        sut.Instruction.Should().Be("2dd1 tablet");
        sut.Frequency.Should().Be(2);
        sut.UnitDose.Should().Be(1m);
        sut.Route.Should().Be("ORAL");
        sut.MealRelation.Should().Be("Bersama Makan");
        sut.UddTime.Should().Be("08:00-20:00");
        sut.Note.Should().Be("Harus habis");
    }

    [Fact]
    public void Load_WithFiveParameters_ShouldPopulateLegacyPropertiesAndDefaultNewStructuredFields()
    {
        // Act
        var sut = EtiketType.Load("3x1", "3x1 tablet", 3, 1m, "Catatan");

        // Assert
        sut.Signa.Should().Be("3x1");
        sut.Instruction.Should().Be("3x1 tablet");
        sut.Frequency.Should().Be(3);
        sut.UnitDose.Should().Be(1m);
        sut.Note.Should().Be("Catatan");
        sut.Route.Should().Be(AppConst.DASH);
        sut.MealRelation.Should().Be(AppConst.DASH);
        sut.UddTime.Should().Be(AppConst.DASH);
    }

    [Fact]
    public void Load_WithEightParameters_ShouldPopulateAllPropertiesFaithfully()
    {
        // Act
        var sut = EtiketType.Load(
            "3x1",
            "3x1 kapsul",
            3,
            1m,
            "ORAL",
            "Sesudah Makan",
            "Pagi-Siang-Malam",
            "Habiskan");

        // Assert
        sut.Signa.Should().Be("3x1");
        sut.Instruction.Should().Be("3x1 kapsul");
        sut.Frequency.Should().Be(3);
        sut.UnitDose.Should().Be(1m);
        sut.Route.Should().Be("ORAL");
        sut.MealRelation.Should().Be("Sesudah Makan");
        sut.UddTime.Should().Be("Pagi-Siang-Malam");
        sut.Note.Should().Be("Habiskan");
    }

    [Fact]
    public void Load_WithNullOrWhitespaceValues_ShouldNormalizeToDash()
    {
        // Act
        var sut = EtiketType.Load(
            null!,
            "   ",
            0,
            0m,
            null!,
            "   ",
            null!,
            "   ");

        // Assert
        sut.Signa.Should().Be(AppConst.DASH);
        sut.Instruction.Should().Be(AppConst.DASH);
        sut.Route.Should().Be(AppConst.DASH);
        sut.MealRelation.Should().Be(AppConst.DASH);
        sut.UddTime.Should().Be(AppConst.DASH);
        sut.Note.Should().Be(AppConst.DASH);
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var etiket1 = EtiketType.Load("3x1", "3x1 tab", 3, 1m, "ORAL", "PC", "UDD", "Note");
        var etiket2 = EtiketType.Load("3x1", "3x1 tab", 3, 1m, "ORAL", "PC", "UDD", "Note");

        // Assert
        etiket1.Should().Be(etiket2);
        (etiket1 == etiket2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentStructuredValues_ShouldNotBeEqual()
    {
        // Arrange
        var etiket1 = EtiketType.Load("3x1", "3x1 tab", 3, 1m, "ORAL", "PC", "UDD", "Note");
        var etiket2 = EtiketType.Load("3x1", "3x1 tab", 3, 1m, "TOP", "PC", "UDD", "Note");

        // Assert
        etiket1.Should().NotBe(etiket2);
        (etiket1 != etiket2).Should().BeTrue();
    }
}
