using Bilreg.Application.LabContext.LabResultFeature.UseCases;
using Bilreg.Infrastructure.LabContext.LabResultFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabResultFeature;

public class LabResultPdfRendererTest
{
    [Fact]
    public void Render_ProducesValidPdfHeader()
    {
        var view = new LabResultPdfView(
            HospitalName: "RS Tes",
            OrderNo: "LAB000099",
            OrderId: "LBO000000099",
            RegId: "REG1",
            PatientId: "MR1",
            PatientName: "Pasien",
            BirthDate: new DateTime(1990, 5, 1),
            Gender: "L",
            AgeAtOrder: 35,
            VersionNo: 2,
            ResultStatus: 2,
            ResultStatusLabel: "Recorded (belum diverifikasi)",
            VerifiedDate: new DateTime(3000, 1, 1),
            VerifiedUserId: "",
            AmendmentReason: "Koreksi HB",
            PreviousVersionNo: 1,
            Items:
            [
                new LabResultPdfItemView("Hemoglobin", "", "14", "g/dL", "12-16", "")
            ]);

        var bytes = new LabResultPdfRenderer().Render(view);

        bytes.Should().NotBeEmpty();
        bytes[0].Should().Be((byte)'%');
        bytes[1].Should().Be((byte)'P');
        bytes[2].Should().Be((byte)'D');
        bytes[3].Should().Be((byte)'F');
    }
}
