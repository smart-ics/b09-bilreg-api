using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultVerificationWorklistHandlerTest
{
    private readonly Mock<ILabResultVerificationWorklistDal> _dalMock = new();
    private readonly LabResultVerificationWorklistHandler _sut;

    public LabResultVerificationWorklistHandlerTest()
    {
        _sut = new LabResultVerificationWorklistHandler(_dalMock.Object);
    }

    [Fact]
    public async Task Handle_PassesFilterToDal_ReturnsProjectedWorklistItems()
    {
        var expectedItems = new List<LabResultVerificationWorklistView>
        {
            new(
                OrderId: "LBO000000001",
                OrderNo: "LAB2605001",
                PatientId: "MR0001",
                PatientName: "Pasien Kritis Cito",
                RecordedDate: new DateTime(2026, 5, 18, 10, 30, 0),
                ResultStatus: 2,
                RecordedUserId: "ANALIS1",
                IsCito: true,
                Gender: "L",
                AgeAtOrder: 45,
                LayananPerujukName: "IGD PONEK",
                DokterPerujukName: "dr. Jaga IGD, Sp.B",
                TestNames: "Darah Lengkap, Analisa Gas Darah",
                HasCriticalValue: true,
                VersionNo: 1,
                ResultSource: 1
            ),
            new(
                OrderId: "LBO000000002",
                OrderNo: "LAB2605002",
                PatientId: "MR0002",
                PatientName: "Pasien Rutin",
                RecordedDate: new DateTime(2026, 5, 18, 9, 15, 0),
                ResultStatus: 2,
                RecordedUserId: "ANALIS2",
                IsCito: false,
                Gender: "P",
                AgeAtOrder: 28,
                LayananPerujukName: "Poli Penyakit Dalam",
                DokterPerujukName: "dr. Spesialis PD, Sp.PD",
                TestNames: "Urin Rutin, Faal Ginjal",
                HasCriticalValue: false,
                VersionNo: 1,
                ResultSource: 1
            )
        };

        LabResultVerificationWorklistFilter? capturedFilter = null;
        _dalMock.Setup(d => d.List(It.IsAny<LabResultVerificationWorklistFilter>()))
            .Callback<LabResultVerificationWorklistFilter>(f => capturedFilter = f)
            .Returns(expectedItems);

        var query = new LabResultVerificationWorklistQuery(
            SearchTerm: "Kritis",
            Date1: new DateTime(2026, 5, 1),
            Date2: new DateTime(2026, 5, 31));

        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        result.Should().BeEquivalentTo(expectedItems);
        result.Should().HaveCount(2);

        // Verify Sp.PK critical/cito projections
        var citoItem = result.First();
        citoItem.IsCito.Should().BeTrue();
        citoItem.HasCriticalValue.Should().BeTrue();
        citoItem.LayananPerujukName.Should().Be("IGD PONEK");
        citoItem.TestNames.Should().Contain("Gas Darah");

        capturedFilter.Should().NotBeNull();
        capturedFilter!.SearchTerm.Should().Be("Kritis");
        capturedFilter.Date1.Should().Be(new DateTime(2026, 5, 1));
        capturedFilter.Date2.Should().Be(new DateTime(2026, 5, 31));
    }

    [Fact]
    public void WorklistView_DefaultValues_AreSafeAndInitialized()
    {
        var item = new LabResultVerificationWorklistView(
            OrderId: "LBO000000003",
            OrderNo: "LAB2605003",
            PatientId: "MR0003",
            PatientName: "Pasien Default",
            RecordedDate: new DateTime(2026, 5, 18, 8, 0, 0),
            ResultStatus: 2,
            RecordedUserId: "ANALIS1");

        item.IsCito.Should().BeFalse();
        item.Gender.Should().BeEmpty();
        item.AgeAtOrder.Should().Be(0);
        item.LayananPerujukName.Should().BeEmpty();
        item.DokterPerujukName.Should().BeEmpty();
        item.TestNames.Should().BeEmpty();
        item.HasCriticalValue.Should().BeFalse();
        item.VersionNo.Should().Be(1);
        item.ResultSource.Should().Be(0);
    }
}
