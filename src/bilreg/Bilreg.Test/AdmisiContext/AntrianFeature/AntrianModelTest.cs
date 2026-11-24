using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianFactoryTests
{
    private readonly Mock<ISequencer> _mockAntrianSequencer;
    private readonly AntrianFactory _sut;

    public AntrianFactoryTests()
    {
        _mockAntrianSequencer = new Mock<ISequencer>();
        _sut = new AntrianFactory(_mockAntrianSequencer.Object);
    }

    #region Create(DateOnly, JadwalPraktekType) Tests

    [Fact]
    public void UT01_Given_ValidAntrianDateAndJadwalPraktek_When_CreateIsCalled_Then_ShouldReturnAntrianModelWithCorrectProperties()
    {
        // Arrange
        var antrianDate = new DateOnly(2025, 10, 13); // Monday
        var dokter = CreatePetugasMedisType("DOK001", "Dr. John Doe");
        var jadwalPraktek = CreateJadwalPraktekType(dokter, DayOfWeek.Monday, 
            new TimeOnly(8, 0), new TimeOnly(12, 0), 20);

        // Act
        var result = _sut.Create(antrianDate, jadwalPraktek);

        // Assert
        result.Should().NotBeNull();
        result.AntrianId.Should().NotBeNullOrEmpty();
        result.AntrianDate.Should().Be(antrianDate);
        result.StartTime.Should().Be(new TimeOnly(8, 0));
        result.EndTime.Should().Be(new TimeOnly(12, 0));
        result.SequenceTag.Should().Be("AN2510130800_DOK001");
        result.ServicePoint.ServicePointCode.Should().Be("DOK001");
        result.AntrianDescription.Should().Be("Praktek Dokter DOK001");
        result.ListEntry.Should().BeEmpty();
    }

    [Fact]
    public void UT02_Given_DokterIdWithSpaces_When_CreateIsCalled_Then_ShouldReplaceSpacesWithDollarSignInSequenceTag()
    {
        // Arrange
        var antrianDate = new DateOnly(2025, 10, 13); // Monday
        var dokter = CreatePetugasMedisType("DOK 001 A", "Dr. John Doe");
        var jadwalPraktek = CreateJadwalPraktekType(dokter, DayOfWeek.Monday, 
            new TimeOnly(8, 0), new TimeOnly(12, 0), 20);

        // Act
        var result = _sut.Create(antrianDate, jadwalPraktek);

        // Assert
        result.SequenceTag.Should().Be("AN2510130800_DOK$001$A");
        result.ServicePoint.ServicePointCode.Should().Be("DOK$001$A");
    }

    [Fact]
    public void UT03_Given_NullJadwalPraktek_When_CreateIsCalled_Then_ShouldThrowArgumentNullException()
    {
        // Arrange
        var antrianDate = new DateOnly(2025, 10, 13);

        // Act
        Action act = () => _sut.Create(antrianDate, (JadwalPraktekType)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("jadwalPraktek");
    }

    [Fact]
    public void UT04_Given_AntrianDateNotMatchingJadwalPraktekDay_When_CreateIsCalled_Then_ShouldThrowArgumentException()
    {
        // Arrange
        var antrianDate = new DateOnly(2025, 10, 14); // Tuesday
        var dokter = CreatePetugasMedisType("DOK001", "Dr. John Doe");
        var jadwalPraktek = CreateJadwalPraktekType(dokter, DayOfWeek.Monday, 
            new TimeOnly(8, 0), new TimeOnly(12, 0), 20);

        // Act
        Action act = () => _sut.Create(antrianDate, jadwalPraktek);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("antrianDate")
            .WithMessage("14-10-2025 bukan hari (Monday).*");
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, 2025, 10, 13)]
    [InlineData(DayOfWeek.Tuesday, 2025, 10, 14)]
    [InlineData(DayOfWeek.Wednesday, 2025, 10, 15)]
    [InlineData(DayOfWeek.Thursday, 2025, 10, 16)]
    [InlineData(DayOfWeek.Friday, 2025, 10, 17)]
    [InlineData(DayOfWeek.Saturday, 2025, 10, 18)]
    [InlineData(DayOfWeek.Sunday, 2025, 10, 19)]
    public void UT05_Given_AntrianDateMatchingJadwalPraktekDay_When_CreateIsCalled_Then_ShouldSucceed(
        DayOfWeek dayOfWeek, int year, int month, int day)
    {
        // Arrange
        var antrianDate = new DateOnly(year, month, day);
        var dokter = CreatePetugasMedisType("DOK001", "Dr. John Doe");
        var jadwalPraktek = CreateJadwalPraktekType(dokter, dayOfWeek, 
            new TimeOnly(8, 0), new TimeOnly(12, 0), 20);

        // Act
        var result = _sut.Create(antrianDate, jadwalPraktek);

        // Assert
        result.Should().NotBeNull();
        result.AntrianDate.Should().Be(antrianDate);
        result.AntrianDate.DayOfWeek.Should().Be(dayOfWeek);
    }

    #endregion

    #region Create(ServicePointType) Tests

    [Fact]
    public void UT06_Given_ValidServicePoint_When_CreateIsCalled_Then_ShouldReturnAntrianModelWithCorrectProperties()
    {
        // Arrange
        var servicePoint = new ServicePointType("SP001", "Loket Pendaftaran");
        var expectedDate = new DateOnly(2025, 10, 13);

        // Act
        var result = _sut.Create(servicePoint, expectedDate);

        // Assert
        result.Should().NotBeNull();
        result.AntrianId.Should().NotBeNullOrEmpty();
        result.AntrianDate.Should().Be(expectedDate);
        result.StartTime.Should().Be(TimeOnly.MinValue);
        result.EndTime.Should().Be(TimeOnly.MaxValue);
        result.SequenceTag.Should().Be("AN2510130000_SP001");
        result.ServicePoint.ServicePointCode.Should().Be("SP001");
        result.ServicePoint.ServicePointName.Should().Be("Loket Pendaftaran");
        result.AntrianDescription.Should().Be("Loket Pendaftaran");
        result.ListEntry.Should().BeEmpty();
    }

    [Fact]
    public void UT07_Given_NullServicePoint_When_CreateIsCalled_Then_ShouldThrowArgumentNullException()
    {
        // Arrange
        ServicePointType servicePoint = null!;

        // Act
        Action act = () => _sut.Create(servicePoint, new DateOnly(2025, 10, 13));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("servicePoint");
    }

    [Fact]
    public void UT08_Given_ServicePointWithSpecialCharacters_When_CreateIsCalled_Then_ShouldUseServicePointCodeAsSequenceTag()
    {
        // Arrange
        var servicePoint = new ServicePointType("SP-001-A", "Loket Khusus");

        // Act
        var result = _sut.Create(servicePoint, new DateOnly(2025, 10, 13));

        // Assert
        result.SequenceTag.Should().Be("AN2510130000_SP-001-A");
        result.ServicePoint.ServicePointCode.Should().Be("SP-001-A");
    }

    #endregion

    #region Default Property Tests

    [Fact]
    public void UT09_Given_Factory_When_DefaultPropertyIsAccessed_Then_ShouldReturnDefaultAntrianModel()
    {
        // Arrange & Act
        var result = _sut.Default;

        // Assert
        result.Should().NotBeNull();
        result.AntrianId.Should().Be("-");
        result.AntrianDate.Should().Be(new DateOnly(3000, 1, 1));
        result.StartTime.Should().Be(TimeOnly.MinValue);
        result.EndTime.Should().Be(TimeOnly.MinValue);
        result.SequenceTag.Should().Be("-");
        result.AntrianDescription.Should().Be("-");
        result.ServicePoint.Should().Be(ServicePointType.Default);
        result.ListEntry.Should().BeEmpty();
    }

    #endregion

    #region Duplicate NoUrut Tests

    [Fact]
    public void UT10_Given_ExistingNoUrut_When_AddEntryWithSameNumber_Then_Throws()
    {
        var person = new PersonType("SINTA", new DateOnly(2008, 5, 5));
        var tracker = PasienTrackerModel.Create(
            person, new DateOnly(2025, 8, 3), "BOOKING", "B1",
            new DateTime(2025, 8, 1, 9, 0, 0));
        var createdAt = new DateTime(2025, 8, 3, 6, 51, 0);
        var queue = new AntrianModel(
            "AN001", new DateOnly(2025, 8, 3), TimeOnly.MinValue, TimeOnly.MaxValue,
            "tag", "Loket", new ServicePointType("Loket", "Loket"),
            [], _mockAntrianSequencer.Object);

        queue.AddEntry(7, tracker, "B1", "BOK", createdAt);
        var act = () => queue.AddEntry(7, tracker, "B2", "BOK", createdAt);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already assigned*");
    }

    #endregion

    #region Helper Methods

    private PpaType CreatePetugasMedisType(string id, string name)
    {
        var petugasMedis = new PpaType(
            id,
            name,
            "Dr. J",
            SmfType.Default,
            GroupSpesialisType.Default,
            new List<PpaLayananType>(),
            new List<PpaSatTugasType>(),
            []);

        return petugasMedis;
    }

    private JadwalPraktekType CreateJadwalPraktekType(
        PpaType dokter, 
        DayOfWeek hari, 
        TimeOnly jamMulai, 
        TimeOnly jamSelesai,
        int maxPasien)
    {
        var layanan = new LayananReff("LAY001", "Poli Umum");
        var layananDk = new LayananDkReff("1", "UMUM");
        var ruang = new RuangType("RU01", "RUANG1", "A");
        return new JadwalPraktekType(
            Ulid.NewUlid().ToString(),
            dokter.ToReff(),
            layanan,
            layananDk,
            GroupSpesialisType.Default,
            ruang,
            hari,
            jamMulai,
            jamSelesai,
            maxPasien,
            AntrianPatternType.Default);
    }

    #endregion
}

public class AntrianModelTest
{
    private static AntrianModel CreateAntrian(string prefix = "")
    {
        return new AntrianModel(
            "ANT001",
            new DateOnly(2026, 11, 12),
            new TimeOnly(8, 0),
            new TimeOnly(12, 0),
            "AN2611120800_DOK001",
            "Praktek Dokter DOK001",
            new ServicePointType("DOK001", "Poli Umum"),
            new List<AntrianEntryModel>(),
            Mock.Of<ISequencer>(),
            queuePrefixSnapshot: prefix);
    }

    #region NormalizeClinicPrefix Tests

    [Theory]
    [InlineData("a", "A")]
    [InlineData("A", "A")]
    [InlineData("z", "Z")]
    [InlineData("ob", "OB")]
    [InlineData("OB", "OB")]
    [InlineData(" Ob ", "OB")]
    [InlineData("  g  ", "G")]
    public void NormalizeClinicPrefix_GivenValidOneOrTwoLetters_ShouldReturnUppercase(string input, string expected)
    {
        AntrianModel.NormalizeClinicPrefix(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData(" - ")]
    public void NormalizeClinicPrefix_GivenNullEmptyWhitespaceOrDash_ShouldReturnEmptyString(string? input)
    {
        AntrianModel.NormalizeClinicPrefix(input).Should().Be(string.Empty);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    [InlineData("1")]
    [InlineData("12")]
    [InlineData("A1")]
    [InlineData("1A")]
    [InlineData("A-B")]
    [InlineData("A!")]
    [InlineData("@")]
    [InlineData("Å")]
    public void NormalizeClinicPrefix_GivenInvalidInput_ShouldThrowArgumentException(string input)
    {
        var act = () => AntrianModel.NormalizeClinicPrefix(input);
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region FormatClinicQueueLabel Tests

    [Fact]
    public void FormatClinicQueueLabel_GivenSingleLetterPrefix_ShouldReturnCompactFormatWithoutLeadingZero()
    {
        var queue = CreateAntrian("A");
        queue.FormatClinicQueueLabel(1).Should().Be("A1");
        queue.FormatClinicQueueLabel(3).Should().Be("A3");
        queue.FormatClinicQueueLabel(25).Should().Be("A25");
    }

    [Fact]
    public void FormatClinicQueueLabel_GivenTwoLetterPrefix_ShouldReturnCompactFormatWithoutLeadingZero()
    {
        var queue = CreateAntrian("OB");
        queue.FormatClinicQueueLabel(1).Should().Be("OB1");
        queue.FormatClinicQueueLabel(12).Should().Be("OB12");
        queue.FormatClinicQueueLabel(105).Should().Be("OB105");
    }

    [Fact]
    public void FormatClinicQueueLabel_GivenEmptyPrefix_ShouldReturnNumberOnlyWithoutLeadingZero()
    {
        var queue = CreateAntrian("");
        queue.FormatClinicQueueLabel(1).Should().Be("1");
        queue.FormatClinicQueueLabel(3).Should().Be("3");
        queue.FormatClinicQueueLabel(42).Should().Be("42");
    }

    #endregion

    #region FormatQueueLabel Tests (Admission Desk)

    [Fact]
    public void FormatQueueLabel_GivenPrefix_ShouldReturnFourDigitFormat()
    {
        var queue = CreateAntrian("A");
        queue.FormatQueueLabel(1).Should().Be("A0001");
        queue.FormatQueueLabel(3).Should().Be("A0003");
        queue.FormatQueueLabel(9999).Should().Be("A9999");
    }

    [Fact]
    public void FormatQueueLabel_GivenEmptyPrefix_ShouldReturnNull()
    {
        var queue = CreateAntrian("");
        queue.FormatQueueLabel(3).Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void FormatQueueLabel_GivenOutOfRangeNumber_ShouldThrowArgumentOutOfRangeException(int invalidNoUrut)
    {
        var queue = CreateAntrian("A");
        var act = () => queue.FormatQueueLabel(invalidNoUrut);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region TryUpdatePrefix Tests

    [Fact]
    public void TryUpdatePrefix_WhenSnapshotEmpty_GivenValidOneLetterPrefix_ShouldUpdateAndReturnTrue()
    {
        var queue = CreateAntrian("");
        var updated = queue.TryUpdatePrefix("A");

        updated.Should().BeTrue();
        queue.QueuePrefixSnapshot.Should().Be("A");
    }

    [Fact]
    public void TryUpdatePrefix_WhenSnapshotEmpty_GivenValidTwoLetterPrefix_ShouldUpdateAndReturnTrue()
    {
        var queue = CreateAntrian("");
        var updated = queue.TryUpdatePrefix("ob");

        updated.Should().BeTrue();
        queue.QueuePrefixSnapshot.Should().Be("OB");
    }

    [Fact]
    public void TryUpdatePrefix_WhenSnapshotAlreadyPopulated_ShouldRejectOverwriteAndReturnFalse()
    {
        var queue = CreateAntrian("A");
        var updated = queue.TryUpdatePrefix("B");

        updated.Should().BeFalse();
        queue.QueuePrefixSnapshot.Should().Be("A");
    }

    [Fact]
    public void TryUpdatePrefix_WhenUpdatedOnce_SubsequentUpdatesShouldBeRejectedDueToImmutability()
    {
        var queue = CreateAntrian("");
        var firstUpdate = queue.TryUpdatePrefix("A");
        firstUpdate.Should().BeTrue();
        queue.QueuePrefixSnapshot.Should().Be("A");

        var secondUpdate = queue.TryUpdatePrefix("OB");
        secondUpdate.Should().BeFalse();
        queue.QueuePrefixSnapshot.Should().Be("A");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    public void TryUpdatePrefix_WhenSnapshotEmpty_GivenEmptyOrSentinel_ShouldReturnFalseAndNotChangeSnapshot(string? emptyPrefix)
    {
        var queue = CreateAntrian("");
        var updated = queue.TryUpdatePrefix(emptyPrefix);

        updated.Should().BeFalse();
        queue.QueuePrefixSnapshot.Should().Be(string.Empty);
    }

    [Fact]
    public void TryUpdatePrefix_WhenSnapshotEmpty_GivenInvalidPrefix_ShouldThrowArgumentException()
    {
        var queue = CreateAntrian("");
        var act = () => queue.TryUpdatePrefix("ABC");

        act.Should().Throw<ArgumentException>();
        queue.QueuePrefixSnapshot.Should().Be(string.Empty);
    }

    #endregion

    #region AntrianView and AntrianViewDto Tests

    [Fact]
    public void AntrianView_GivenSingleLetterPrefix_ShouldReturnCompactQueueLabel()
    {
        var view = new AntrianView("AN01", 0, 7, "Pasien", "REG01", "REG",
            DateTime.Today, "TAG_DR01", "DR01", "Poli", new TimeOnly(8, 0), new TimeOnly(12, 0),
            QueuePrefixSnapshot: "C");

        view.QueueLabel.Should().Be("C7");
    }

    [Fact]
    public void AntrianView_GivenTwoLetterPrefix_ShouldReturnCompactQueueLabel()
    {
        var view = new AntrianView("AN01", 0, 12, "Pasien", "REG01", "REG",
            DateTime.Today, "TAG_DR01", "DR01", "Poli", new TimeOnly(8, 0), new TimeOnly(12, 0),
            QueuePrefixSnapshot: "OB");

        view.QueueLabel.Should().Be("OB12");
    }

    [Fact]
    public void AntrianView_GivenEmptyPrefix_ShouldReturnNumberOnly()
    {
        var view = new AntrianView("AN01", 0, 7, "Pasien", "REG01", "REG",
            DateTime.Today, "TAG_DR01", "DR01", "Poli", new TimeOnly(8, 0), new TimeOnly(12, 0),
            QueuePrefixSnapshot: "");

        view.QueueLabel.Should().Be("7");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AntrianView_GivenZeroOrNegativeNoUrut_ShouldReturnEmptyString(int noUrut)
    {
        var view = new AntrianView("AN01", 0, noUrut, "Pasien", "REG01", "REG",
            DateTime.Today, "TAG_DR01", "DR01", "Poli", new TimeOnly(8, 0), new TimeOnly(12, 0),
            QueuePrefixSnapshot: "C");

        view.QueueLabel.Should().Be("");
    }

    [Fact]
    public void AntrianViewDto_ToView_ShouldForwardQueuePrefixSnapshot()
    {
        var dto = new AntrianViewDto("AN01", 0, 7, "Pasien", "REG01", "REG",
            DateTime.Today, "TAG_DR01", "Poli", "08:00", "12:00",
            QueuePrefixSnapshot: "C");

        var view = dto.ToView();

        view.QueuePrefixSnapshot.Should().Be("C");
        view.QueueLabel.Should().Be("C7");
        view.NoUrut.Should().Be(7);
    }

    #endregion
}
