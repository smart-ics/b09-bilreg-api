using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.LabContext.LabExternalFeature;

public class LabExternalModelTest
{
    private static AuditInfoType SampleAudit() => new("user-test", new DateTime(2026, 10, 15, 8, 30, 0));

    private static LabExternalDemographicsType SampleDemographics(
        string name = "John Doe",
        string nik = "3201010101010001",
        DateTime? birthDate = null,
        string gender = "L",
        string phone = "081234567890",
        string address = "Jl. Sehat No. 1")
    {
        return new LabExternalDemographicsType(
            name,
            nik,
            birthDate ?? new DateTime(1990, 5, 20),
            gender,
            phone,
            address);
    }

    private static LabExternalReferralInfoType SampleReferral(
        LabReferralTypeEnum referralType = LabReferralTypeEnum.Aps,
        string doctor = "dr. Rujukan",
        string origin = "Klinik Pratama",
        string notes = "Cek hematologi rutin")
    {
        return new LabExternalReferralInfoType(referralType, doctor, origin, notes);
    }

    private static LabExternalModel CreateValidModel(
        string regExternalId = "LEX26A150001",
        string externalMrNo = "EX-202610-00001",
        string billingAccountId = "LEX26A150001")
    {
        return LabExternalModel.Create(
            regExternalId,
            externalMrNo,
            SampleDemographics(),
            SampleReferral(),
            billingAccountId,
            SampleAudit());
    }

    [Fact]
    public void Create_ValidParameters_InstantiatesModelWithActiveStatus()
    {
        var demographics = SampleDemographics();
        var referral = SampleReferral(LabReferralTypeEnum.FaskesLuar, "dr. Budi", "Klinik Medika", "Gejala demam");
        var audit = SampleAudit();

        var model = LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            demographics,
            referral,
            "LEX26A150001",
            audit);

        model.RegExternalId.Should().Be("LEX26A150001");
        model.ExternalMrNo.Should().Be("EX-202610-00001");
        model.Demographics.Should().Be(demographics);
        model.ReferralInfo.Should().Be(referral);
        model.BillingAccountId.Should().Be("LEX26A150001");
        model.Status.Should().Be(LabExternalStatusEnum.Active);
        model.CancelInfo.Should().Be(LabExternalCancelInfoType.Default);
        model.AuditTrail.Created.UserId.Should().Be(audit.UserId);
        model.AuditTrail.Created.Timestamp.Should().Be(audit.Timestamp);
    }

    [Fact]
    public void Create_EmptyBillingAccountId_DefaultsToRegExternalId()
    {
        var model = LabExternalModel.Create(
            "LEX26A150002",
            "EX-202610-00002",
            SampleDemographics(),
            SampleReferral(),
            "",
            SampleAudit());

        model.BillingAccountId.Should().Be("LEX26A150002");
    }

    [Fact]
    public void Create_CustomBillingAccountId_UsesProvidedBillingAccountId()
    {
        var model = LabExternalModel.Create(
            "LEX26A150003",
            "EX-202610-00003",
            SampleDemographics(),
            SampleReferral(),
            "BILL-CUSTOM-99",
            SampleAudit());

        model.BillingAccountId.Should().Be("BILL-CUSTOM-99");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_InvalidRegExternalId_ThrowsArgumentException(string? invalidId)
    {
        var act = () => LabExternalModel.Create(
            invalidId!,
            "EX-202610-00001",
            SampleDemographics(),
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_InvalidExternalMrNo_ThrowsArgumentException(string? invalidMrNo)
    {
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            invalidMrNo!,
            SampleDemographics(),
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NullDemographics_ThrowsArgumentNullException()
    {
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            null!,
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_EmptyPatientName_ThrowsArgumentException(string invalidName)
    {
        var demographics = SampleDemographics(name: invalidName);
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            demographics,
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentException>().WithMessage("*Nama pasien wajib diisi*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_EmptyGender_ThrowsArgumentException(string invalidGender)
    {
        var demographics = SampleDemographics(gender: invalidGender);
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            demographics,
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentException>().WithMessage("*Jenis kelamin wajib diisi*");
    }

    [Fact]
    public void Create_DefaultBirthDate_ThrowsArgumentException()
    {
        var demographics = SampleDemographics(birthDate: default(DateTime));
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            demographics,
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentException>().WithMessage("*Tanggal lahir pasien tidak valid*");
    }

    [Fact]
    public void Create_SentinelEmptyBirthDate_ThrowsArgumentException()
    {
        var demographics = SampleDemographics(birthDate: LabExternalModel.EmptyDate);
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            demographics,
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentException>().WithMessage("*Tanggal lahir pasien tidak valid*");
    }

    [Fact]
    public void Create_FutureBirthDate_ThrowsArgumentException()
    {
        var demographics = SampleDemographics(birthDate: DateTime.Today.AddDays(5));
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            demographics,
            SampleReferral(),
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentException>().WithMessage("*Tanggal lahir pasien tidak valid*");
    }

    [Fact]
    public void Create_NullReferralInfo_ThrowsArgumentNullException()
    {
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            SampleDemographics(),
            null!,
            "LEX01",
            SampleAudit());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_NullAudit_ThrowsArgumentNullException()
    {
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            SampleDemographics(),
            SampleReferral(),
            "LEX01",
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_EmptyAuditUserId_ThrowsArgumentException(string? invalidUserId)
    {
        var act = () => LabExternalModel.Create(
            "LEX26A150001",
            "EX-202610-00001",
            SampleDemographics(),
            SampleReferral(),
            "LEX01",
            new AuditInfoType(invalidUserId!, DateTime.Now));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Demographics_CalculateAge_ReturnsAccurateAge()
    {
        var birthDate = new DateTime(2000, 4, 15);
        var demographics = SampleDemographics(birthDate: birthDate);

        var refBeforeBirthday = new DateTime(2026, 3, 1);
        var refAfterBirthday = new DateTime(2026, 5, 1);

        demographics.CalculateAge(refBeforeBirthday).Should().Be(25);
        demographics.CalculateAge(refAfterBirthday).Should().Be(26);
    }

    [Fact]
    public void Load_RestoresFullAggregateState()
    {
        var demographics = SampleDemographics();
        var referral = SampleReferral(LabReferralTypeEnum.FaskesLuar, "dr. SpPK", "Klinik Sehat", "Rujukan darah lengkap");
        var cancelInfo = new LabExternalCancelInfoType("Pasien tidak jadi periksa", "user-admin", new DateTime(2026, 10, 15, 9, 0, 0));
        var audit = new AuditTrailType(
            new AuditInfoType("user-creator", new DateTime(2026, 10, 15, 8, 0, 0)),
            new AuditInfoType("user-admin", new DateTime(2026, 10, 15, 9, 0, 0)),
            AuditInfoType.Default);

        var loaded = LabExternalModel.Load(
            "LEX26A150099",
            "EX-202610-00099",
            demographics,
            referral,
            "LEX26A150099",
            LabExternalStatusEnum.Cancelled,
            cancelInfo,
            audit);

        loaded.RegExternalId.Should().Be("LEX26A150099");
        loaded.ExternalMrNo.Should().Be("EX-202610-00099");
        loaded.Demographics.Should().Be(demographics);
        loaded.ReferralInfo.Should().Be(referral);
        loaded.BillingAccountId.Should().Be("LEX26A150099");
        loaded.Status.Should().Be(LabExternalStatusEnum.Cancelled);
        loaded.CancelInfo.Should().Be(cancelInfo);
        loaded.AuditTrail.Should().Be(audit);
    }

    [Fact]
    public void Key_ProducesExpectedInterface()
    {
        var key = LabExternalModel.Key("LEX12345");
        key.RegExternalId.Should().Be("LEX12345");

        var model = CreateValidModel("LEX67890");
        ((ILabExternalKey)model).RegExternalId.Should().Be("LEX67890");
    }

    [Fact]
    public void Cancel_WhenActive_SucceedsAndUpdatesStatusAndCancelInfo()
    {
        var model = CreateValidModel();
        var cancelDate = new DateTime(2026, 10, 15, 9, 30, 0);

        model.Cancel(
            reason: "Pasien batal periksa karena antrean",
            userId: "user-cancel",
            cancelledAt: cancelDate,
            hasPaidCashier: false,
            hasCollectedSpecimen: false);

        model.Status.Should().Be(LabExternalStatusEnum.Cancelled);
        model.CancelInfo.CancelReason.Should().Be("Pasien batal periksa karena antrean");
        model.CancelInfo.CancelledUserId.Should().Be("user-cancel");
        model.CancelInfo.CancelledDate.Should().Be(cancelDate);
        model.AuditTrail.Modified.UserId.Should().Be("user-cancel");
        model.AuditTrail.Modified.Timestamp.Should().Be(cancelDate);
    }

    [Fact]
    public void Cancel_EX01_WhenHasPaidCashierIsTrue_ThrowsInvalidOperationException()
    {
        var model = CreateValidModel();
        var cancelDate = new DateTime(2026, 10, 15, 9, 30, 0);

        var act = () => model.Cancel(
            reason: "Minta batal",
            userId: "user-cancel",
            cancelledAt: cancelDate,
            hasPaidCashier: true,
            hasCollectedSpecimen: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("EX-01: Pembatalan ditolak. Tagihan telah dibayar di kasir. Lakukan void kasir terlebih dahulu.");
        model.Status.Should().Be(LabExternalStatusEnum.Active);
    }

    [Fact]
    public void Cancel_EX02_WhenHasCollectedSpecimenIsTrue_ThrowsInvalidOperationException()
    {
        var model = CreateValidModel();
        var cancelDate = new DateTime(2026, 10, 15, 9, 30, 0);

        var act = () => model.Cancel(
            reason: "Minta batal",
            userId: "user-cancel",
            cancelledAt: cancelDate,
            hasPaidCashier: false,
            hasCollectedSpecimen: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("EX-02: Pembatalan ditolak. Spesimen laboratorium telah diambil untuk order ini.");
        model.Status.Should().Be(LabExternalStatusEnum.Active);
    }

    [Fact]
    public void Cancel_BothPaidAndCollected_EvaluatesEX01First()
    {
        var model = CreateValidModel();
        var cancelDate = new DateTime(2026, 10, 15, 9, 30, 0);

        var act = () => model.Cancel(
            reason: "Minta batal",
            userId: "user-cancel",
            cancelledAt: cancelDate,
            hasPaidCashier: true,
            hasCollectedSpecimen: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("EX-01: Pembatalan ditolak. Tagihan telah dibayar di kasir. Lakukan void kasir terlebih dahulu.");
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsInvalidOperationException()
    {
        var model = CreateValidModel();
        var cancelDate = new DateTime(2026, 10, 15, 9, 30, 0);

        model.Cancel(
            reason: "Pembatalan pertama",
            userId: "user-cancel",
            cancelledAt: cancelDate,
            hasPaidCashier: false,
            hasCollectedSpecimen: false);

        var act = () => model.Cancel(
            reason: "Pembatalan kedua",
            userId: "user-cancel",
            cancelledAt: cancelDate.AddMinutes(5),
            hasPaidCashier: false,
            hasCollectedSpecimen: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Registrasi eksternal sudah dalam status dibatalkan.");
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsInvalidOperationException()
    {
        var model = CreateValidModel();
        model.MarkCompleted("user-comp", new DateTime(2026, 10, 15, 10, 0, 0));

        var act = () => model.Cancel(
            reason: "Membatalkan yang sudah selesai",
            userId: "user-cancel",
            cancelledAt: new DateTime(2026, 10, 15, 10, 30, 0),
            hasPaidCashier: false,
            hasCollectedSpecimen: false);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Registrasi eksternal yang sudah selesai tidak dapat dibatalkan.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Cancel_EmptyReason_ThrowsArgumentException(string? invalidReason)
    {
        var model = CreateValidModel();
        var act = () => model.Cancel(
            reason: invalidReason!,
            userId: "user-cancel",
            cancelledAt: DateTime.Now,
            hasPaidCashier: false,
            hasCollectedSpecimen: false);

        act.Should().Throw<ArgumentException>().WithMessage("*Alasan pembatalan wajib diisi*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Cancel_EmptyUserId_ThrowsArgumentException(string? invalidUserId)
    {
        var model = CreateValidModel();
        var act = () => model.Cancel(
            reason: "Alasan valid",
            userId: invalidUserId!,
            cancelledAt: DateTime.Now,
            hasPaidCashier: false,
            hasCollectedSpecimen: false);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkCompleted_WhenActive_SucceedsAndUpdatesStatus()
    {
        var model = CreateValidModel();
        var completedDate = new DateTime(2026, 10, 15, 11, 0, 0);

        model.MarkCompleted("user-tech", completedDate);

        model.Status.Should().Be(LabExternalStatusEnum.Completed);
        model.AuditTrail.Modified.UserId.Should().Be("user-tech");
        model.AuditTrail.Modified.Timestamp.Should().Be(completedDate);
    }

    [Fact]
    public void MarkCompleted_WhenAlreadyCompleted_ThrowsInvalidOperationException()
    {
        var model = CreateValidModel();
        model.MarkCompleted("user-tech", new DateTime(2026, 10, 15, 11, 0, 0));

        var act = () => model.MarkCompleted("user-tech", new DateTime(2026, 10, 15, 11, 30, 0));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Hanya registrasi berstatus Active yang dapat diselesaikan*");
    }

    [Fact]
    public void MarkCompleted_WhenCancelled_ThrowsInvalidOperationException()
    {
        var model = CreateValidModel();
        model.Cancel("Batal", "user-cancel", new DateTime(2026, 10, 15, 9, 0, 0), false, false);

        var act = () => model.MarkCompleted("user-tech", new DateTime(2026, 10, 15, 11, 0, 0));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Hanya registrasi berstatus Active yang dapat diselesaikan*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void MarkCompleted_EmptyUserId_ThrowsArgumentException(string? invalidUserId)
    {
        var model = CreateValidModel();
        var act = () => model.MarkCompleted(invalidUserId!, DateTime.Now);

        act.Should().Throw<ArgumentException>();
    }
}
