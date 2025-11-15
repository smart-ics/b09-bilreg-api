using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Domain.AdmisiContext.AntrianFeature;

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
        result.SequenceTag.Should().Be("DOK001"); // Space-free version
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
        result.SequenceTag.Should().Be("DOK$001$A");
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
        var expectedDate = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var result = _sut.Create(servicePoint);

        // Assert
        result.Should().NotBeNull();
        result.AntrianId.Should().NotBeNullOrEmpty();
        result.AntrianDate.Should().Be(expectedDate);
        result.StartTime.Should().Be(TimeOnly.MinValue);
        result.EndTime.Should().Be(TimeOnly.MaxValue);
        result.SequenceTag.Should().Be("SP001");
        result.AntrianDescription.Should().Be("Loket Pendaftaran");
        result.ListEntry.Should().BeEmpty();
    }

    [Fact]
    public void UT07_Given_NullServicePoint_When_CreateIsCalled_Then_ShouldThrowArgumentNullException()
    {
        // Arrange
        ServicePointType servicePoint = null!;

        // Act
        Action act = () => _sut.Create(servicePoint);

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
        var result = _sut.Create(servicePoint);

        // Assert
        result.SequenceTag.Should().Be("SP-001-A");
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
        result.ListEntry.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private PetugasMedisType CreatePetugasMedisType(string id, string name)
    {
        var petugasMedis = new PetugasMedisType(
            id,
            name,
            "Dr. J",
            SmfType.Default,
            new List<PetugasMedisLayananType>(),
            new List<PetugasMedisSatTugasType>());

        return petugasMedis;
    }

    private JadwalPraktekType CreateJadwalPraktekType(
        PetugasMedisType dokter, 
        DayOfWeek hari, 
        TimeOnly jamMulai, 
        TimeOnly jamSelesai,
        int maxPasien)
    {
        var layanan = new LayananReff("LAY001", "Poli Umum");
        var layananDk = new LayananDkReff("1", "UMUM");
        return new JadwalPraktekType(
            Ulid.NewUlid().ToString(),
            dokter.ToReff(),
            layanan,
            layananDk,
            hari,
            jamMulai,
            jamSelesai,
            maxPasien);
    }

    #endregion
}