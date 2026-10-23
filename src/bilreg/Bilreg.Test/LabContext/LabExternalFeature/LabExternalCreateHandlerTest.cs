using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Test.Shared;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.LabContext.LabExternalFeature;

public class LabExternalCreateHandlerTest
{
    private static readonly DateTime FixedNow = new(2026, 10, 15, 10, 30, 0);

    private readonly Mock<ILabExternalRepo> _labExternalRepo = new();
    private readonly Mock<ITataRekeningRepo> _tataRekeningRepo = new();
    private readonly Mock<ISequencer> _sequencer = new();
    private readonly LabExternalCreateHandler _sut;

    public LabExternalCreateHandlerTest()
    {
        var clock = new TestTglJamProvider(FixedNow);
        _sut = new LabExternalCreateHandler(
            _labExternalRepo.Object,
            _tataRekeningRepo.Object,
            _sequencer.Object,
            clock);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesRegistrationAndBillingAccountAtomically()
    {
        // Arrange
        _sequencer.Setup(s => s.GetNextNoUrut("EXT-202610")).Returns(1);
        TataRekeningModel? savedBilling = null;
        LabExternalModel? savedExternal = null;

        _tataRekeningRepo.Setup(r => r.SaveChanges(It.IsAny<TataRekeningModel>()))
            .Callback<TataRekeningModel>(m => savedBilling = m);
        _labExternalRepo.Setup(r => r.SaveChanges(It.IsAny<LabExternalModel>()))
            .Callback<LabExternalModel>(m => savedExternal = m);

        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Budi Santoso",
            Nik: "3171012345670001",
            BirthDate: new DateTime(1985, 4, 12),
            Gender: "L",
            Phone: "08123456789",
            Address: "Jl. Sudirman No. 1",
            ReferralType: LabReferralTypeEnum.FaskesLuar,
            ReferralDoctor: "dr. Andi Sp.PK",
            ReferralOrigin: "Klinik Sehat Sejahtera",
            ClinicalNotes: "Pemeriksaan Darah Lengkap");

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RegExternalId.Should().StartWith("LEX");
        result.RegExternalId.Length.Should().Be(12);
        result.ExternalMrNo.Should().Be("EXT-202610-00001");
        result.BillingAccountId.Should().Be(result.RegExternalId);

        savedBilling.Should().NotBeNull();
        savedBilling!.RegId.Should().Be(result.RegExternalId);
        savedBilling.RegId.Length.Should().Be(12);
        savedBilling.Status.Should().Be(TataRekeningStatusEnum.Opened);

        savedExternal.Should().NotBeNull();
        savedExternal!.RegExternalId.Should().Be(result.RegExternalId);
        savedExternal.ExternalMrNo.Should().Be("EXT-202610-00001");
        savedExternal.BillingAccountId.Should().Be(result.RegExternalId);
        savedExternal.Status.Should().Be(LabExternalStatusEnum.Active);
        savedExternal.Demographics.PatientName.Should().Be("Budi Santoso");
        savedExternal.Demographics.Nik.Should().Be("3171012345670001");
        savedExternal.Demographics.BirthDate.Should().Be(new DateTime(1985, 4, 12));
        savedExternal.Demographics.Gender.Should().Be("L");
        savedExternal.Demographics.Phone.Should().Be("08123456789");
        savedExternal.Demographics.Address.Should().Be("Jl. Sudirman No. 1");
        savedExternal.ReferralInfo.ReferralType.Should().Be(LabReferralTypeEnum.FaskesLuar);
        savedExternal.ReferralInfo.ReferralDoctor.Should().Be("dr. Andi Sp.PK");
        savedExternal.ReferralInfo.ReferralOrigin.Should().Be("Klinik Sehat Sejahtera");
        savedExternal.ReferralInfo.ClinicalNotes.Should().Be("Pemeriksaan Darah Lengkap");
        savedExternal.AuditTrail.Created.UserId.Should().Be("USR001");
        savedExternal.AuditTrail.Created.Timestamp.Should().Be(FixedNow);

        _sequencer.Verify(s => s.GetNextNoUrut("EXT-202610"), Times.Once);
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Once);
        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullableFields_SubstitutesEmptyStrings()
    {
        // Arrange
        _sequencer.Setup(s => s.GetNextNoUrut("EXT-202610")).Returns(25);
        LabExternalModel? savedExternal = null;
        _labExternalRepo.Setup(r => r.SaveChanges(It.IsAny<LabExternalModel>()))
            .Callback<LabExternalModel>(m => savedExternal = m);

        var cmd = new LabExternalCreateCmd(
            UserId: "USR002",
            PatientName: "Siti Aminah",
            Nik: null,
            BirthDate: new DateTime(1995, 8, 20),
            Gender: "P",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        // Act
        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.ExternalMrNo.Should().Be("EXT-202610-00025");
        savedExternal.Should().NotBeNull();
        savedExternal!.Demographics.Nik.Should().Be(string.Empty);
        savedExternal.Demographics.Phone.Should().Be(string.Empty);
        savedExternal.Demographics.Address.Should().Be(string.Empty);
        savedExternal.ReferralInfo.ReferralDoctor.Should().Be(string.Empty);
        savedExternal.ReferralInfo.ReferralOrigin.Should().Be(string.Empty);
        savedExternal.ReferralInfo.ClinicalNotes.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(1, "EXT-202610-00001")]
    [InlineData(42, "EXT-202610-00042")]
    [InlineData(999, "EXT-202610-00999")]
    [InlineData(12345, "EXT-202610-12345")]
    public async Task Handle_SequencerNumberPadding_FormatsCorrectly(int seqNo, string expectedMrNo)
    {
        _sequencer.Setup(s => s.GetNextNoUrut("EXT-202610")).Returns(seqNo);

        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Testing Sequencer",
            Nik: null,
            BirthDate: new DateTime(2000, 1, 1),
            Gender: "L",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var result = await _sut.Handle(cmd, CancellationToken.None);
        result.ExternalMrNo.Should().Be(expectedMrNo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidUserId_ThrowsArgumentException(string? userId)
    {
        var cmd = new LabExternalCreateCmd(
            UserId: userId!,
            PatientName: "Pasien",
            Nik: null,
            BirthDate: new DateTime(2000, 1, 1),
            Gender: "L",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidPatientName_ThrowsArgumentException(string? patientName)
    {
        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: patientName!,
            Nik: null,
            BirthDate: new DateTime(2000, 1, 1),
            Gender: "L",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
        _tataRekeningRepo.Verify(r => r.SaveChanges(It.IsAny<TataRekeningModel>()), Times.Never);
        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidGender_ThrowsArgumentException(string? gender)
    {
        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Pasien",
            Nik: null,
            BirthDate: new DateTime(2000, 1, 1),
            Gender: gender!,
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_DefaultBirthDate_ThrowsArgumentException()
    {
        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Pasien",
            Nik: null,
            BirthDate: default,
            Gender: "L",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_FutureBirthDate_ThrowsArgumentException()
    {
        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Pasien",
            Nik: null,
            BirthDate: DateTime.Today.AddDays(5),
            Gender: "L",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenLabExternalRepoThrows_TransactionFailsAndPropagates()
    {
        _sequencer.Setup(s => s.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        _labExternalRepo.Setup(r => r.SaveChanges(It.IsAny<LabExternalModel>()))
            .Throws(new InvalidOperationException("DB error"));

        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Pasien",
            Nik: null,
            BirthDate: new DateTime(2000, 1, 1),
            Gender: "L",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB error");
    }

    [Fact]
    public async Task Handle_WhenTataRekeningRepoThrows_LabExternalRepoNeverCalled()
    {
        _sequencer.Setup(s => s.GetNextNoUrut(It.IsAny<string>())).Returns(1);
        _tataRekeningRepo.Setup(r => r.SaveChanges(It.IsAny<TataRekeningModel>()))
            .Throws(new InvalidOperationException("Billing DB error"));

        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Pasien",
            Nik: null,
            BirthDate: new DateTime(2000, 1, 1),
            Gender: "L",
            Phone: null,
            Address: null,
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var act = () => _sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Billing DB error");
        _labExternalRepo.Verify(r => r.SaveChanges(It.IsAny<LabExternalModel>()), Times.Never);
    }

    [Fact]
    public void NumberHelper_GenerateExternalMrNo_GeneratesExpectedFormat()
    {
        var seqMock = new Mock<ISequencer>();
        seqMock.Setup(s => s.GetNextNoUrut("EXT-202610")).Returns(7);
        var mrNo = LabExternalNumberHelper.GenerateExternalMrNo(seqMock.Object, new DateTime(2026, 10, 15));
        mrNo.Should().Be("EXT-202610-00007");
    }

    [Fact]
    public void NumberHelper_GenerateExternalMrNo_NullSequencer_ThrowsArgumentNullException()
    {
        var act = () => LabExternalNumberHelper.GenerateExternalMrNo(null!, DateTime.Now);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NumberHelper_GenerateRegExternalId_GeneratesLexPrefixedId()
    {
        var id = LabExternalNumberHelper.GenerateRegExternalId();
        id.Should().NotBeNullOrWhiteSpace();
        id.Should().StartWith("LEX");
        id.Length.Should().Be(12);
    }

    [Fact]
    public async Task Handle_Generates12CharRegExternalId_MapsToTataRekeningRegIdWithStatusOpened()
    {
        // AC-ARCH-09 & TD-09: Verify regExternalId 12 characters (NunaId.New("LEX")) is properly mapped to TataRekeningModel.RegId and saved with status Opened
        _sequencer.Setup(s => s.GetNextNoUrut("EXT-202610")).Returns(10);
        TataRekeningModel? savedBilling = null;
        LabExternalModel? savedExternal = null;

        _tataRekeningRepo.Setup(r => r.SaveChanges(It.IsAny<TataRekeningModel>()))
            .Callback<TataRekeningModel>(m => savedBilling = m);
        _labExternalRepo.Setup(r => r.SaveChanges(It.IsAny<LabExternalModel>()))
            .Callback<LabExternalModel>(m => savedExternal = m);

        var cmd = new LabExternalCreateCmd(
            UserId: "USR001",
            PatientName: "Pasien Eksternal Uji Sizing",
            Nik: "3171012345670001",
            BirthDate: new DateTime(1992, 5, 10),
            Gender: "L",
            Phone: "08123456789",
            Address: "Jl. Laboratorium No. 12",
            ReferralType: LabReferralTypeEnum.Aps,
            ReferralDoctor: null,
            ReferralOrigin: null,
            ClinicalNotes: null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.RegExternalId.Should().StartWith("LEX");
        result.RegExternalId.Length.Should().Be(12);

        savedBilling.Should().NotBeNull();
        savedBilling!.RegId.Should().Be(result.RegExternalId);
        savedBilling.RegId.Length.Should().Be(12);
        savedBilling.Status.Should().Be(TataRekeningStatusEnum.Opened);

        savedExternal.Should().NotBeNull();
        savedExternal!.RegExternalId.Should().Be(result.RegExternalId);
        savedExternal.RegExternalId.Length.Should().Be(12);
        savedExternal.BillingAccountId.Should().Be(result.RegExternalId);
    }
}
