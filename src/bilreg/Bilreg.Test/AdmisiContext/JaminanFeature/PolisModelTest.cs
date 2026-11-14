using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.JaminanFeature;

public class PolisModelTest
{
    private static PasienModel SamplePasien(string id) => (PasienModel)PasienModel.Key(id);

    private static PolisModel CreateSut(
        string polisId = "POL001",
        string noPolis = "NP001",
        string atasNama = "Peserta Utama",
        TipeJaminanReff? tipeJaminan = null,
        KelasReff? kelas = null,
        DateOnly? expiredDate = null,
        bool isCoverRajal = false)
    {
        var tj = tipeJaminan ?? new TipeJaminanReff("TJ01", "BPJS Kesehatan");
        var kl = kelas ?? new KelasReff("KL01", "Kelas 1");
        var exp = expiredDate ?? new DateOnly(2027, 12, 31);
        return new PolisModel(polisId, noPolis, atasNama, tj, kl, exp, isCoverRajal, []);
    }

    [Fact]
    public void GivenValidArguments_WhenConstruct_ThenPropertiesMatch()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        sut.PolisId.Should().Be("POL001");
        sut.NoPolis.Should().Be("NP001");
        sut.AtasNama.Should().Be("Peserta Utama");
        sut.TipeJaminan.TipeJaminanId.Should().Be("TJ01");
        sut.Kelas.KelasId.Should().Be("KL01");
        sut.ExpiredDate.Should().Be(new DateOnly(2027, 12, 31));
        sut.IsCoverRajal.Should().BeFalse();
        sut.ListCover.Should().BeEmpty();
    }

    [Fact]
    public void GivenMutableProperties_WhenCheckPropertySetters_ThenNoPublicSettersForMutableAttributes()
    {
        // Assert immutability / encapsulation via reflection
        var type = typeof(PolisModel);

        var mutableProperties = new[] { "NoPolis", "AtasNama", "ExpiredDate", "Kelas", "IsCoverRajal" };
        foreach (var propName in mutableProperties)
        {
            var prop = type.GetProperty(propName);
            prop.Should().NotBeNull();
            prop!.GetSetMethod(nonPublic: false).Should().BeNull(
                $"{propName} should not have a public setter");
        }

        var immutableProperties = new[] { "PolisId", "TipeJaminan" };
        foreach (var propName in immutableProperties)
        {
            var prop = type.GetProperty(propName);
            prop.Should().NotBeNull();
            prop!.GetSetMethod(nonPublic: false).Should().NotBeNull(
                $"{propName} init accessor should exist but not be a standard public setter");
        }
    }

    [Fact]
    public void GivenValidUpdateValues_WhenUpdate_ThenMutablePropertiesAreUpdated_AndImmutableRemainUnchanged()
    {
        // Arrange
        var sut = CreateSut();
        var newKelas = new KelasReff("KL02", "Kelas 2");
        var newExp = new DateOnly(2028, 6, 30);

        // Act
        sut.Update("NP002", "Nama Baru", newExp, newKelas, true);

        // Assert
        sut.PolisId.Should().Be("POL001");
        sut.TipeJaminan.TipeJaminanId.Should().Be("TJ01");
        sut.NoPolis.Should().Be("NP002");
        sut.AtasNama.Should().Be("Nama Baru");
        sut.Kelas.KelasId.Should().Be("KL02");
        sut.Kelas.KelasName.Should().Be("Kelas 2");
        sut.ExpiredDate.Should().Be(newExp);
        sut.IsCoverRajal.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Atas Nama")]
    [InlineData("   ", "Atas Nama")]
    [InlineData(null, "Atas Nama")]
    [InlineData("NP001", "")]
    [InlineData("NP001", "   ")]
    [InlineData("NP001", null)]
    public void GivenInvalidStrings_WhenUpdate_ThenThrowsArgumentException(string? noPolis, string? atasNama)
    {
        // Arrange
        var sut = CreateSut();
        var kelas = new KelasReff("KL01", "Kelas 1");

        // Act
        var act = () => sut.Update(noPolis!, atasNama!, new DateOnly(2028, 1, 1), kelas, false);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenNullKelas_WhenUpdate_ThenThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Update("NP001", "Atas Nama", new DateOnly(2028, 1, 1), null!, false);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenPatientNotInListCover_WhenUpdateCoverageStatus_ThenThrowsArgumentExceptionCoverNotFound()
    {
        // Arrange
        var sut = CreateSut();
        var pasien1 = SamplePasien("P001");
        var pasienNotInCover = SamplePasien("P999");
        sut.AddCoverage(pasien1, StatusPesertaType.Create("P"));

        // Act
        var act = () => sut.UpdateCoverageStatus(pasienNotInCover, StatusPesertaType.Create("I"));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Cover not found*");
    }

    [Fact]
    public void GivenPrimaryParticipantP_WhenUpdateCoverageStatusToNonP_ThenThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var primaryPatient = SamplePasien("P001");
        sut.AddCoverage(primaryPatient, StatusPesertaType.Create("P"));

        // Act
        var act = () => sut.UpdateCoverageStatus(primaryPatient, StatusPesertaType.Create("I"));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Status peserta utama tidak dapat diubah*");
    }

    [Fact]
    public void GivenPrimaryParticipantP_WhenUpdateCoverageStatusToP_ThenSucceeds()
    {
        // Arrange
        var sut = CreateSut();
        var primaryPatient = SamplePasien("P001");
        sut.AddCoverage(primaryPatient, StatusPesertaType.Create("P"));

        // Act
        sut.UpdateCoverageStatus(primaryPatient, StatusPesertaType.Create("P"));

        // Assert
        sut.ListCover.Should().ContainSingle(c => c.Pasien.PasienId == "P001" && c.Status.StatusCode == "P");
    }

    [Fact]
    public void GivenNonPParticipant_WhenUpdateCoverageStatusToConflictWithOtherParticipant_ThenThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var patientP = SamplePasien("P001");
        var patientI = SamplePasien("P002");
        var patientA = SamplePasien("P003");

        sut.AddCoverage(patientP, StatusPesertaType.Create("P"));
        sut.AddCoverage(patientI, StatusPesertaType.Create("I"));
        sut.AddCoverage(patientA, StatusPesertaType.Create("A"));

        // Act: patientA tries to change status to "I" (already used by patientI)
        var act = () => sut.UpdateCoverageStatus(patientA, StatusPesertaType.Create("I"));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Status Kepesertaan already exist*");
    }

    [Fact]
    public void GivenNonPParticipant_WhenUpdateCoverageStatusToP_AndPAlreadyExists_ThenThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var patientP = SamplePasien("P001");
        var patientI = SamplePasien("P002");

        sut.AddCoverage(patientP, StatusPesertaType.Create("P"));
        sut.AddCoverage(patientI, StatusPesertaType.Create("I"));

        // Act: patientI tries to change status to "P"
        var act = () => sut.UpdateCoverageStatus(patientI, StatusPesertaType.Create("P"));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Status Kepesertaan already exist*");
    }

    [Fact]
    public void GivenNonPParticipant_WhenUpdateCoverageStatusToSameStatus_ThenSucceeds()
    {
        // Arrange
        var sut = CreateSut();
        var patientP = SamplePasien("P001");
        var patientI = SamplePasien("P002");

        sut.AddCoverage(patientP, StatusPesertaType.Create("P"));
        sut.AddCoverage(patientI, StatusPesertaType.Create("I"));

        // Act
        sut.UpdateCoverageStatus(patientI, StatusPesertaType.Create("I"));

        // Assert
        sut.ListCover.Should().ContainSingle(c => c.Pasien.PasienId == "P002" && c.Status.StatusCode == "I");
    }

    [Fact]
    public void GivenNonPParticipant_WhenUpdateCoverageStatusToAvailableStatus_ThenSucceeds()
    {
        // Arrange
        var sut = CreateSut();
        var patientP = SamplePasien("P001");
        var patientI = SamplePasien("P002");

        sut.AddCoverage(patientP, StatusPesertaType.Create("P"));
        sut.AddCoverage(patientI, StatusPesertaType.Create("I"));

        // Act: patientI changes to "A" (Anak)
        sut.UpdateCoverageStatus(patientI, StatusPesertaType.Create("A"));

        // Assert
        var updated = sut.ListCover.First(c => c.Pasien.PasienId == "P002");
        updated.Status.StatusCode.Should().Be("A");
        updated.Status.StatusDesc.Should().Be("Anak");
    }

    [Fact]
    public void GivenValidArguments_WhenCombinedUpdate_ThenUpdatesHeaderAndCoverageStatus()
    {
        // Arrange
        var sut = CreateSut();
        var patientP = SamplePasien("P001");
        var patientI = SamplePasien("P002");

        sut.AddCoverage(patientP, StatusPesertaType.Create("P"));
        sut.AddCoverage(patientI, StatusPesertaType.Create("I"));

        var newKelas = new KelasReff("KL03", "Kelas VIP");
        var newExp = new DateOnly(2029, 1, 1);

        // Act
        sut.Update("NP-UPDATED", "Atas Nama Updated", newExp, newKelas, true, patientI, StatusPesertaType.Create("S"));

        // Assert
        sut.NoPolis.Should().Be("NP-UPDATED");
        sut.AtasNama.Should().Be("Atas Nama Updated");
        sut.ExpiredDate.Should().Be(newExp);
        sut.Kelas.KelasId.Should().Be("KL03");
        sut.IsCoverRajal.Should().BeTrue();

        var updatedCover = sut.ListCover.First(c => c.Pasien.PasienId == "P002");
        updatedCover.Status.StatusCode.Should().Be("S");
        updatedCover.Status.StatusDesc.Should().Be("Suami");
    }
}
