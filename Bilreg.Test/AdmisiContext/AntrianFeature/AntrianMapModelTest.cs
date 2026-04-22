using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapModelTest
{
    public class CreationTests
    {
        [Fact]
        public void UT1_GivenValidParameters_WhenConstructorCalled_ThenAntrianMapModelIsCreated()
        {
            // Arrange
            var antrianMapId = "MAP001";
            var jadwalId = "JADWAL001";
            var dokter = new PpaReff("DOK001", "Dr. Adi");
            var layanan = new LayananReff("LAY001", "Umum");
            var tglJadwal = new DateOnly(2026, 4, 22);
            var jamJadwal = new TimeOnly(09, 00);
            var jamPraktek = new TimeOnly(10, 00);
            var pattern = "AUTO";
            var maxPasien = 10;
            var listMap = new List<AntrianMapDetilModel>
            {
                AntrianMapDetilModel.AutoSlot(1),
                AntrianMapDetilModel.AutoSlot(2)
            };

            // Act
            var result = new AntrianMapModel(antrianMapId, jadwalId, dokter, layanan, tglJadwal,
                jamJadwal, jamPraktek, pattern, maxPasien, listMap);

            // Assert
            result.Should().NotBeNull();
            result.AntrianMapId.Should().Be(antrianMapId);
            result.JadwalId.Should().Be(jadwalId);
            result.Dokter.Should().Be(dokter);
            result.Layanan.Should().Be(layanan);
            result.TglJadwal.Should().Be(tglJadwal);
            result.JamJadwal.Should().Be(jamJadwal);
            result.JamPraktek.Should().Be(jamPraktek);
            result.Pattern.Should().Be(pattern);
            result.MaxPasien.Should().Be(maxPasien);
            result.ListMap.Should().HaveCount(2);
        }

        [Fact]
        public void UT2_GivenNullListMap_WhenConstructorCalled_ThenListMapIsInitializedAsEmpty()
        {
            // Arrange
            var dokter = new PpaReff("DOK001", "Dr. Adi");
            var layanan = new LayananReff("LAY001", "Umum");

            // Act
            var result = new AntrianMapModel("MAP001", "JADWAL001", dokter, layanan,
                new DateOnly(2026, 4, 22), new TimeOnly(09, 00), new TimeOnly(10, 00),
                "AUTO", 10, null!);

            // Assert
            result.ListMap.Should().BeEmpty();
        }

        [Fact]
        public void UT3_GivenDefaultFactory_WhenDefaultCalled_ThenDefaultAntrianMapModelIsReturned()
        {
            // Act
            var result = AntrianMapModel.Default;

            // Assert
            result.Should().NotBeNull();
            result.AntrianMapId.Should().Be("-");
            result.JadwalId.Should().Be("-");
            result.Dokter.PpaId.Should().Be("-");
            result.Layanan.LayananId.Should().Be("-");
            result.TglJadwal.Should().Be(DateOnly.MinValue);
            result.JamJadwal.Should().Be(TimeOnly.MinValue);
            result.JamPraktek.Should().Be(TimeOnly.MinValue);
            result.Pattern.Should().Be("");
            result.MaxPasien.Should().Be(0);
            result.ListMap.Should().BeEmpty();
        }

        [Fact]
        public void UT4_GivenValidAntrianMapId_WhenKeyMethodCalled_ThenKeyedAntrianMapIsReturned()
        {
            // Arrange
            var mapId = "MAP_KEY_001";

            // Act
            var result = AntrianMapModel.Key(mapId) as AntrianMapModel;

            // Assert
            result.Should().NotBeNull();
            result!.AntrianMapId.Should().Be(mapId);
            result.JadwalId.Should().Be("-");
            result.Dokter.PpaId.Should().Be("-");
            result.ListMap.Should().BeEmpty();
        }
    }

    public class PropertiesTests
    {
        [Fact]
        public void UT1_GivenAntrianMapWithMultipleSlots_WhenLastNoUrutAccessed_ThenMaxSequenceNumberIsReturned()
        {
            // Arrange
            var listMap = new List<AntrianMapDetilModel>
            {
                AntrianMapDetilModel.AutoSlot(1),
                AntrianMapDetilModel.AutoSlot(3),
                AntrianMapDetilModel.AutoSlot(2),
                AntrianMapDetilModel.AutoSlot(5)
            };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act
            var result = model.LastNoUrut;

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public void UT2_GivenAntrianMapWithDokter_WhenDokterIdPropertyAccessed_ThenDokterIdIsReturned()
        {
            // Arrange
            var dokter = new PpaReff("DOK_ID_123", "Dr. Budi");
            var model = CreateTestAntrianMapModel(dokter: dokter);

            // Act
            var result = model.DokterId;

            // Assert
            result.Should().Be("DOK_ID_123");
        }

        [Fact]
        public void UT3_GivenAntrianMapWithLayanan_WhenLayananIdPropertyAccessed_ThenLayananIdIsReturned()
        {
            // Arrange
            var layanan = new LayananReff("LAY_ID_456", "Umum");
            var model = CreateTestAntrianMapModel(layanan: layanan);

            // Act
            var result = model.LayananId;

            // Assert
            result.Should().Be("LAY_ID_456");
        }

        [Fact]
        public void UT4_GivenAntrianMapWithVariousSlots_WhenTotalSlotCountAccessed_ThenCorrectCountIsReturned()
        {
            // Arrange
            var listMap = new List<AntrianMapDetilModel>
            {
                AntrianMapDetilModel.AutoSlot(1),
                AntrianMapDetilModel.AutoSlot(2),
                AntrianMapDetilModel.AutoSlot(3)
            };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act
            var result = model.TotalSlotCount;

            // Assert
            result.Should().Be(3);
        }

        [Fact]
        public void UT5_GivenEmptyAntrianMap_WhenTotalSlotCountAccessed_ThenZeroIsReturned()
        {
            // Arrange
            var model = CreateTestAntrianMapModel(listMap: new List<AntrianMapDetilModel>());

            // Act
            var result = model.TotalSlotCount;

            // Assert
            result.Should().Be(0);
        }
    }

    public class VoidSlotTests
    {
        [Fact]
        public void UT1_GivenSlotWithSequenceNumber_WhenVoidSlotCalled_ThenSlotIsVoided()
        {
            // Arrange
            var slot = AntrianMapDetilModel.AutoSlot(2);
            var listMap = new List<AntrianMapDetilModel>
            {
                AntrianMapDetilModel.AutoSlot(1),
                slot,
                AntrianMapDetilModel.AutoSlot(3)
            };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act
            model.VoidSlot(2);

            // Assert
            var voidedSlot = model.ListMap.First(x => x.NoUrut == 2);
            voidedSlot.ReffId.Should().Be("-");
            voidedSlot.Flag.Should().Be("AUTO");
        }

        [Fact]
        public void UT2_GivenNonExistentSlotNumber_WhenVoidSlotCalled_ThenNoExceptionIsThrown()
        {
            // Arrange
            var listMap = new List<AntrianMapDetilModel>
            {
                AntrianMapDetilModel.AutoSlot(1),
                AntrianMapDetilModel.AutoSlot(2)
            };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act & Assert
            var action = () => model.VoidSlot(999);
            action.Should().NotThrow();
        }

        [Fact]
        public void UT3_GivenEmptyAntrianMap_WhenVoidSlotCalled_ThenNoExceptionIsThrown()
        {
            // Arrange
            var model = CreateTestAntrianMapModel(listMap: new List<AntrianMapDetilModel>());

            // Act & Assert
            var action = () => model.VoidSlot(1);
            action.Should().NotThrow();
        }
    }

    public class GetNextAntrianTests
    {
        [Fact]
        public void UT1_GivenAvailableSlotWithAutoFlag_WhenGetNextAntrianCalled_ThenNextAvailableSlotIsReturned()
        {
            // Arrange
            var slot1 = AntrianMapDetilModel.AutoSlot(1);
            var slot2 = AntrianMapDetilModel.AutoSlot(2);
            var slot3 = AntrianMapDetilModel.AutoSlot(3);
            slot1.SetPasien(CreateTestPasien(), CreateTestReg(), "REF001", "MANUAL");

            var listMap = new List<AntrianMapDetilModel> { slot1, slot2, slot3 };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act
            var result = model.GetNextAntrian("AUTO");

            // Assert
            result.Should().NotBeNull();
            result.NoUrut.Should().Be(2);
            result.Flag.Should().Be("AUTO");
            result.IsTerpakai.Should().BeFalse();
        }

        [Fact]
        public void UT2_GivenMultipleAvailableSlotsWithSameFlag_WhenGetNextAntrianCalled_ThenLowestSequenceSlotIsReturned()
        {
            // Arrange
            var slot1 = AntrianMapDetilModel.AutoSlot(1);
            var slot2 = AntrianMapDetilModel.AutoSlot(2);
            var slot3 = AntrianMapDetilModel.AutoSlot(3);
            slot1.SetPasien(CreateTestPasien(), CreateTestReg(), "REF001", "AUTO");

            var listMap = new List<AntrianMapDetilModel> { slot1, slot2, slot3 };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act
            var result = model.GetNextAntrian("AUTO");

            // Assert
            result.NoUrut.Should().Be(2);
        }

        [Fact]
        public void UT3_GivenNoAvailableSlots_WhenGetNextAntrianCalled_ThenNewAutoSlotIsGenerated()
        {
            // Arrange
            var slot1 = AntrianMapDetilModel.AutoSlot(1);
            var slot2 = AntrianMapDetilModel.AutoSlot(2);
            slot1.SetPasien(CreateTestPasien(), CreateTestReg(), "REF001", "AUTO");
            slot2.SetPasien(CreateTestPasien(), CreateTestReg(), "REF002", "AUTO");

            var listMap = new List<AntrianMapDetilModel> { slot1, slot2 };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act
            var result = model.GetNextAntrian("AUTO");

            // Assert
            result.Should().NotBeNull();
            result.NoUrut.Should().Be(3); // LastNoUrut (2) + 1
            result.Flag.Should().Be("AUTO");
            result.IsTerpakai.Should().BeFalse();
        }

        [Fact]
        public void UT4_GivenDifferentFlags_WhenGetNextAntrianCalledWithSpecificFlag_ThenOnlyMatchingFlagSlotIsConsidered()
        {
            // Arrange
            var autoSlot1 = AntrianMapDetilModel.AutoSlot(1);
            var manualSlot2 = new AntrianMapDetilModel(2,
                new PasienReff("-", "-", new DateOnly(3000, 1, 1), "-"),
                new Bilreg.Domain.AdmisiContext.RegFeature.RegReff("-", "-", "_"),
                "-", "MANUAL", false);
            var autoSlot3 = AntrianMapDetilModel.AutoSlot(3);

            autoSlot1.SetPasien(CreateTestPasien(), CreateTestReg(), "REF001", "AUTO");

            var listMap = new List<AntrianMapDetilModel> { autoSlot1, manualSlot2, autoSlot3 };
            var model = CreateTestAntrianMapModel(listMap: listMap);

            // Act
            var resultManual = model.GetNextAntrian("MANUAL");

            // Assert
            resultManual.NoUrut.Should().Be(2);
            resultManual.Flag.Should().Be("MANUAL");
        }

        [Fact]
        public void UT5_GivenEmptyAntrianMap_WhenGetNextAntrianCalled_ThenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var model = CreateTestAntrianMapModel(listMap: new List<AntrianMapDetilModel>());

            // Act & Assert
            var action = () => model.GetNextAntrian("AUTO");
            action.Should().Throw<InvalidOperationException>();
        }
    }

    #region HELPER METHODS

    private static AntrianMapModel CreateTestAntrianMapModel(
        string antrianMapId = "MAP_TEST_001",
        string jadwalId = "JADWAL_TEST_001",
        PpaReff? dokter = null,
        LayananReff? layanan = null,
        DateOnly? tglJadwal = null,
        TimeOnly? jamJadwal = null,
        TimeOnly? jamPraktek = null,
        string pattern = "AUTO",
        int maxPasien = 10,
        IEnumerable<AntrianMapDetilModel>? listMap = null)
    {
        dokter ??= new PpaReff("DOK_TEST_001", "Dr. Test");
        layanan ??= new LayananReff("LAY_TEST_001", "Umum");
        tglJadwal ??= new DateOnly(2026, 4, 22);
        jamJadwal ??= new TimeOnly(09, 00);
        jamPraktek ??= new TimeOnly(10, 00);
        listMap ??= new List<AntrianMapDetilModel>();

        return new AntrianMapModel(
            antrianMapId,
            jadwalId,
            dokter,
            layanan,
            tglJadwal.Value,
            jamJadwal.Value,
            jamPraktek.Value,
            pattern,
            maxPasien,
            listMap);
    }

    private static PasienReff CreateTestPasien()
    {
        return new PasienReff("MR_TEST_001", "Pasien Test", new DateOnly(1990, 5, 15), "L");
    }

    private static Bilreg.Domain.AdmisiContext.RegFeature.RegReff CreateTestReg()
    {
        return new Bilreg.Domain.AdmisiContext.RegFeature.RegReff("REG_TEST_001", "MR_TEST_001", "Pasien Test");
    }

    #endregion
}