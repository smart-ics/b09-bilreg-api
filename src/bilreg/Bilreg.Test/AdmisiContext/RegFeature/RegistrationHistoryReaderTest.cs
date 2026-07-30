using Bilreg.Infrastructure.AdmisiContext.RegFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.RegFeature;

public class RegistrationHistoryReaderTest
{
    [Fact]
    public void CreatePatientIdParameters_2600Ids_UsesTwoSqlParameters()
    {
        var patientIds = Enumerable.Range(1, 2600)
            .Select(x => $"MR{x:D6}")
            .ToArray();

        var actual = RegistrationHistoryReader.CreatePatientIdParameters(
            new DateOnly(2026, 7, 29),
            patientIds);

        actual.ParameterNames.Should().BeEquivalentTo(
            "admissionDate",
            "patientIdsCsv");
        actual.Get<string>("patientIdsCsv")
            .Split(',')
            .Should()
            .Equal(patientIds);
    }

    [Fact]
    public void CreateRegistrationIdParameters_2600Ids_UsesOneSqlParameter()
    {
        var registrationIds = Enumerable.Range(1, 2600)
            .Select(x => $"RG{x:D6}")
            .ToArray();

        var actual = RegistrationHistoryReader.CreateRegistrationIdParameters(
            registrationIds);

        actual.ParameterNames.Should().BeEquivalentTo("registrationIdsCsv");
        actual.Get<string>("registrationIdsCsv")
            .Split(',')
            .Should()
            .Equal(registrationIds);
    }
}
