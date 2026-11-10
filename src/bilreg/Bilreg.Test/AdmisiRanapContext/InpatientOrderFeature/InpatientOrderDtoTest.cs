using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.AdmisiRanapContext.InpatientOrderFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.InpatientOrderFeature;

public class InpatientOrderDtoTest
{
    [Fact]
    public void FromModel_GivenCompleteModel_MapsAllPropertiesAccurately()
    {
        var model = CreateSampleModel();

        var dto = InpatientOrderDto.FromModel(model);

        dto.InpatientOrderId.Should().Be("OPN26K040001");
        dto.OrderSource.Should().Be((int)OrderSourceEnum.EmrRawatJalan);
        dto.OrderStatus.Should().Be((int)InpatientOrderStatusEnum.Requested);
        dto.PasienId.Should().Be("337502200001234");
        dto.PasienName.Should().Be("SITI AMINAH");
        dto.TglLahir.Should().Be(new DateTime(1985, 4, 12));
        dto.Gender.Should().Be("P");
        dto.DokterId.Should().Be("D001");
        dto.DokterName.Should().Be("dr. Budi Sp.PD");
        dto.PlannedDate.Should().Be(new DateTime(2026, 11, 5, 8, 0, 0));
        dto.KelasId.Should().Be("1");
        dto.KelasName.Should().Be("Kelas 1");
        dto.BangsalId.Should().Be("B01");
        dto.BangsalName.Should().Be("Bangsal Mawar");
        dto.ClinicalNotes.Should().Be("Observasi febris hari ke-4");
        dto.TipeJaminanId.Should().Be("BPJ");
        dto.TipeJaminanName.Should().Be("BPJS Kesehatan");
        dto.JaminanReffId.Should().Be("0001234567890");
        dto.FulfilledRegId.Should().Be("-");
        dto.ExternalReferenceId.Should().Be("EXT-ORD-999");
        dto.ExternalSystemSource.Should().Be("EMR_RJ");
        dto.CrtUser.Should().Be("dr_budi");
        dto.CrtDate.Should().Be(new DateTime(2026, 11, 4, 10, 0, 0));
        dto.UpdUser.Should().Be("");
        dto.UpdDate.Should().Be(new DateTime(3000, 1, 1));
        dto.VodUser.Should().Be("");
        dto.VodDate.Should().Be(new DateTime(3000, 1, 1));
    }

    [Fact]
    public void ToModel_GivenCompleteDto_MapsAllPropertiesAccurately()
    {
        var dto = new InpatientOrderDto(
            "OPN26K040001",
            (int)OrderSourceEnum.AdmisiBooking,
            (int)InpatientOrderStatusEnum.Maintained,
            "337502200001234",
            "SITI AMINAH",
            new DateTime(1985, 4, 12),
            "P",
            "D001",
            "dr. Budi Sp.PD",
            new DateTime(2026, 11, 5, 8, 0, 0),
            "1",
            "Kelas 1",
            "B01",
            "Bangsal Mawar",
            "Perlu oksigen nasal",
            "BPJ",
            "BPJS Kesehatan",
            "0001234567890",
            "-",
            "BOOK-2026-001",
            "ADM_LOKET",
            "petugas_adm",
            new DateTime(2026, 11, 4, 10, 0, 0),
            "petugas_adm",
            new DateTime(2026, 11, 4, 11, 0, 0),
            "",
            new DateTime(3000, 1, 1));

        var model = dto.ToModel();

        model.InpatientOrderId.Should().Be("OPN26K040001");
        model.OrderSource.Should().Be(OrderSourceEnum.AdmisiBooking);
        model.OrderStatus.Should().Be(InpatientOrderStatusEnum.Maintained);
        model.Pasien.PasienId.Should().Be("337502200001234");
        model.Pasien.PasienName.Should().Be("SITI AMINAH");
        model.Pasien.TglLahir.Should().Be(new DateOnly(1985, 4, 12));
        model.Pasien.Gender.Should().Be("P");
        model.Dokter.PpaId.Should().Be("D001");
        model.Dokter.PpaName.Should().Be("dr. Budi Sp.PD");
        model.PlannedDate.Should().Be(new DateTime(2026, 11, 5, 8, 0, 0));
        model.KelasRawat.KelasId.Should().Be("1");
        model.KelasRawat.KelasName.Should().Be("Kelas 1");
        model.Bangsal.BangsalId.Should().Be("B01");
        model.Bangsal.BangsalName.Should().Be("Bangsal Mawar");
        model.ClinicalNotes.Should().Be("Perlu oksigen nasal");
        model.Insurance.TipeJaminan.TipeJaminanId.Should().Be("BPJ");
        model.Insurance.TipeJaminan.TipeJaminanName.Should().Be("BPJS Kesehatan");
        model.Insurance.JaminanReffId.Should().Be("0001234567890");
        model.FulfilledRegId.Should().Be("-");
        model.ExternalReferenceId.Should().Be("BOOK-2026-001");
        model.ExternalSystemSource.Should().Be("ADM_LOKET");
        model.AuditTrail.Created.UserId.Should().Be("petugas_adm");
        model.AuditTrail.Created.Timestamp.Should().Be(new DateTime(2026, 11, 4, 10, 0, 0));
        model.AuditTrail.Modified.UserId.Should().Be("petugas_adm");
        model.AuditTrail.Modified.Timestamp.Should().Be(new DateTime(2026, 11, 4, 11, 0, 0));
    }

    [Fact]
    public void RoundTrip_ModelToDtoToModel_PreservesIdentityAndValues()
    {
        var original = CreateSampleModel();

        var dto = InpatientOrderDto.FromModel(original);
        var restored = dto.ToModel();

        restored.InpatientOrderId.Should().Be(original.InpatientOrderId);
        restored.OrderSource.Should().Be(original.OrderSource);
        restored.OrderStatus.Should().Be(original.OrderStatus);
        restored.Pasien.PasienId.Should().Be(original.Pasien.PasienId);
        restored.Pasien.PasienName.Should().Be(original.Pasien.PasienName);
        restored.Dokter.PpaId.Should().Be(original.Dokter.PpaId);
        restored.PlannedDate.Should().Be(original.PlannedDate);
        restored.KelasRawat.KelasId.Should().Be(original.KelasRawat.KelasId);
        restored.Bangsal.BangsalId.Should().Be(original.Bangsal.BangsalId);
        restored.ClinicalNotes.Should().Be(original.ClinicalNotes);
        restored.Insurance.TipeJaminan.TipeJaminanId.Should().Be(original.Insurance.TipeJaminan.TipeJaminanId);
        restored.Insurance.JaminanReffId.Should().Be(original.Insurance.JaminanReffId);
        restored.FulfilledRegId.Should().Be(original.FulfilledRegId);
        restored.ExternalReferenceId.Should().Be(original.ExternalReferenceId);
        restored.ExternalSystemSource.Should().Be(original.ExternalSystemSource);
    }

    private static InpatientOrderModel CreateSampleModel() =>
        new(
            "OPN26K040001",
            OrderSourceEnum.EmrRawatJalan,
            InpatientOrderStatusEnum.Requested,
            new PasienReff("337502200001234", "SITI AMINAH", new DateOnly(1985, 4, 12), "P"),
            new PpaReff("D001", "dr. Budi Sp.PD"),
            new DateTime(2026, 11, 5, 8, 0, 0),
            new KelasReff("1", "Kelas 1"),
            new BangsalReff("B01", "Bangsal Mawar"),
            "Observasi febris hari ke-4",
            new InpatientOrderInsuranceModel(new TipeJaminanReff("BPJ", "BPJS Kesehatan"), "0001234567890"),
            "-",
            "EXT-ORD-999",
            "EMR_RJ",
            AuditTrailType.Create("dr_budi", new DateTime(2026, 11, 4, 10, 0, 0)));
}
