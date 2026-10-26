using Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.EmrAntrianOutboundFeature;

public class EmrAntrianOutboundPayloadBuilderTest
{
    [Fact]
    public void SerializeReg_WithPrefixAntrian_ProducesCamelCaseJsonProperty()
    {
        // Arrange
        var cmd = new AddAntrianEmrByRegCommand(
            RegId: "REG01",
            BookingId: "BOK01",
            PasienId: "PAS01",
            PasienName: "Budi",
            LayananId: "LY01",
            DokterId: "DR01",
            TglBerobat: "2026-10-24",
            JamJadwal: "08:00",
            NoAntrian: 12,
            PrefixAntrian: "A"
        );

        // Act
        var json = EmrAntrianOutboundPayloadBuilder.SerializeReg(cmd);

        // Assert
        json.Should().Contain("\"prefixAntrian\":\"A\"");
        
        var deserialized = EmrAntrianOutboundPayloadBuilder.DeserializeReg(json);
        deserialized.Should().BeEquivalentTo(cmd);
        deserialized.PrefixAntrian.Should().Be("A");
    }

    [Fact]
    public void SerializeReg_DefaultPrefixAntrian_SerializesEmptyString()
    {
        // Arrange
        var cmd = new AddAntrianEmrByRegCommand(
            "REG01", "BOK01", "PAS01", "Budi", "LY01", "DR01", "2026-10-24", "08:00", 12
        );

        // Act
        var json = EmrAntrianOutboundPayloadBuilder.SerializeReg(cmd);

        // Assert
        cmd.PrefixAntrian.Should().BeEmpty();
        json.Should().Contain("\"prefixAntrian\":\"\"");

        var deserialized = EmrAntrianOutboundPayloadBuilder.DeserializeReg(json);
        deserialized.PrefixAntrian.Should().BeEmpty();
    }

    [Fact]
    public void DeserializeReg_LegacyJsonWithoutPrefixAntrian_AssignsDefaultEmptyString()
    {
        // Arrange - legacy JSON without prefixAntrian field
        var legacyJson = """
        {
            "regId": "REG01",
            "bookingId": "BOK01",
            "pasienId": "PAS01",
            "pasienName": "Budi",
            "layananId": "LY01",
            "dokterId": "DR01",
            "tglBerobat": "2026-10-24",
            "jamJadwal": "08:00",
            "noAntrian": 12
        }
        """;

        // Act
        var result = EmrAntrianOutboundPayloadBuilder.DeserializeReg(legacyJson);

        // Assert
        result.Should().NotBeNull();
        result.RegId.Should().Be("REG01");
        result.BookingId.Should().Be("BOK01");
        result.PasienId.Should().Be("PAS01");
        result.PasienName.Should().Be("Budi");
        result.LayananId.Should().Be("LY01");
        result.DokterId.Should().Be("DR01");
        result.TglBerobat.Should().Be("2026-10-24");
        result.JamJadwal.Should().Be("08:00");
        result.NoAntrian.Should().Be(12);
        result.PrefixAntrian.Should().Be(string.Empty);
    }
}
