using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AntrianMapModelTest
{
	public class CreateTests
	{
		[Fact]
		public void Constructor_ShouldInitializeProperties_WhenInputIsValid()
		{
			// Arrange
			var detils = new[]
			{
				CreateDetil(1, isTerpakai: false, flag: "N"),
				CreateDetil(3, isTerpakai: true, flag: "A"),
				CreateDetil(2, isTerpakai: false, flag: "A")
			};

			var model = new AntrianMapModel(
				antrianMapId: "MAP1",
				jadwalId: "J1",
				dokter: new PpaReff("D1","Dr Test"),
				layanan: new LayananReff("L1","Poli"),
				tglJadwal: new DateOnly(2026,1,1),
				jamJadwal: new TimeOnly(8,0),
				jamPraktek: new TimeOnly(8,30),
				pattern: AntrianPatternType.Default,
				maxPasien: 10,
				listMap: detils
			);

			// Assert
			model.AntrianMapId.Should().Be("MAP1");
			model.JadwalId.Should().Be("J1");
			model.DokterId.Should().Be("D1");
			model.LayananId.Should().Be("L1");
			model.TglJadwal.Should().Be(new DateOnly(2026,1,1));
			model.MaxPasien.Should().Be(10);
			model.TotalSlotCount.Should().Be(3);
			model.LastNoUrut.Should().Be(3);
		}

		[Fact]
		public void Constructor_ShouldUseEmptyList_WhenListMapIsNull()
		{
			// Arrange & Act
			var model = new AntrianMapModel(
				antrianMapId: "MAP2",
				jadwalId: "J2",
				dokter: new PpaReff("D2","Dr X"),
				layanan: new LayananReff("L2","Poli2"),
				tglJadwal: DateOnly.FromDateTime(DateTime.Today),
				jamJadwal: TimeOnly.FromDateTime(DateTime.Now),
				jamPraktek: TimeOnly.FromDateTime(DateTime.Now),
				pattern: AntrianPatternType.Default,
				maxPasien: 5,
				listMap: null!
			);

			// Assert
			model.TotalSlotCount.Should().Be(0);
			// Accessing LastNoUrut when there are no slots should throw (behavior of Max over empty sequence)
			var act = () => { var _ = model.LastNoUrut; };
			act.Should().Throw<InvalidOperationException>();
		}
	}

	public class PropertyTests
	{
		[Fact]
		public void Default_ShouldHaveDefaults_WhenAccessed()
		{
			// Arrange & Act
			var def = AntrianMapModel.Default;

			// Assert
			def.AntrianMapId.Should().Be("-");
			def.MaxPasien.Should().Be(0);
			def.TotalSlotCount.Should().Be(0);
			var act = () => { _ = def.LastNoUrut; };
			act.Should().Throw<InvalidOperationException>();
		}

		[Fact]
		public void Key_ShouldReturnKeyWithGivenId_WhenCalled()
		{
			// Act
			var key = AntrianMapModel.Key("X123");

			// Assert
			key.AntrianMapId.Should().Be("X123");
		}
	}

	public class BehaviorTests
	{
		[Fact]
		public void GetNextAntrian_ShouldReturnFirstAvailableSlot_WhenMatchingFlagExists()
		{
			// Arrange
			var detils = new List<AntrianMapDetilModel>
			{
				CreateDetil(1, isTerpakai: true, flag: "A"),
				CreateDetil(2, isTerpakai: false, flag: "B"),
				CreateDetil(3, isTerpakai: false, flag: "A")
			};

			var model = new AntrianMapModel("m", "j", new PpaReff("d","n"), new LayananReff("l","n"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, AntrianPatternType.Default,  1, detils);

			// Act
			var next = model.GetNextAntrian("A");

			// Assert: should return the lowest NoUrut among unused slots with flag A (3 in this case)
			next.Should().NotBeNull();
			next.NoUrut.Should().Be(3);
			next.Flag.Should().Be("A");
			next.IsTerpakai.Should().BeFalse();
		}

		[Fact]
		public void GetNextAntrian_ShouldReturnAutoSlotWithNextNoUrut_WhenNoAvailableMatchingSlot()
		{
			// Arrange: existing slots but none available for flag "Z"
			var detils = new List<AntrianMapDetilModel>
			{
				CreateDetil(1, isTerpakai: true, flag: "A"),
				CreateDetil(2, isTerpakai: true, flag: "B")
			};

			var model = new AntrianMapModel("m2", "j2", new PpaReff("d2","n2"), new LayananReff("l2","n2"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, AntrianPatternType.Default, 2, detils);

			// Act
			var next = model.GetNextAntrian("Z");

			// Assert: should be an auto slot with NoUrut = LastNoUrut + 1
			next.Should().NotBeNull();
			next.Flag.Should().Be("AUTO");
			next.NoUrut.Should().Be(model.LastNoUrut + 1);
		}

		[Fact]
		public void VoidSlot_ShouldConvertSlotToAuto_WhenSlotExists()
		{
			// Arrange
			var detil = CreateDetil(5, isTerpakai: true, flag: "X");
			var list = new List<AntrianMapDetilModel> { detil };
			var model = new AntrianMapModel("m3", "j3", new PpaReff("d3","n3"), new LayananReff("l3","n3"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, AntrianPatternType.Default, 1, list);

			// Act
			model.VoidSlot(5);

			// Assert: detil should have been voided (flag AUTO and ReffId set to "-")
			var updated = model.ListMap.Should().ContainSingle().Which;
			updated.Flag.Should().Be("AUTO");
			updated.ReffId.Should().Be("-");
			updated.PasienId.Should().Be(PasienModel.Default.ToReff().PasienId);
		}

		[Fact]
		public void VoidSlot_ShouldDoNothing_WhenSlotNotFound()
		{
			// Arrange
			var detil = CreateDetil(7, isTerpakai: false, flag: "Y");
			var list = new List<AntrianMapDetilModel> { detil };
			var model = new AntrianMapModel("m4", "j4", new PpaReff("d4","n4"), new LayananReff("l4","n4"),
				DateOnly.MinValue, TimeOnly.MinValue, TimeOnly.MinValue, AntrianPatternType.Default, 1, list);

			// Act
			Action act = () => model.VoidSlot(999);

			// Assert: no exception and original slot unchanged
			act.Should().NotThrow();
			model.ListMap.Should().ContainSingle(x => x.NoUrut == 7 && x.Flag == "Y");
		}
	}

	public class SeedingTests
	{
		// Builds a minimal JadwalPraktekType with the given pattern and max pasien.
		private static JadwalPraktekType BuildJadwal(AntrianPatternType pattern, int maxPasien)
			=> new JadwalPraktekType(
				jadwalPraktekId : "J-SEED",
				dokter          : new PpaReff("-", "-"),
				layanan         : new LayananReff("-", "-"),
				layanandk       : new LayananDkReff("-", "-"),
				groupSpesiallis : GroupSpesialisType.Default,
				ruang           : RuangType.Default,
				hari            : DayOfWeek.Monday,
				jamMulai        : TimeOnly.MinValue,
				jamSelesai      : TimeOnly.MinValue,
				maxPasien       : maxPasien,
				antrianPattern  : pattern);

		private static AntrianPatternType PatternUmumBpjs()
			=> new AntrianPatternType("MIX", 0, 0,
			[
				new AntrianPatternItemType("UMUM", 1),
				new AntrianPatternItemType("BPJS", 3)
			]);

		[Fact]
		public void CreateFromJadwal_ShouldSeedCyclically_WhenMaxPasienIs10()
		{
			// Arrange – cycle length = 4 (UMUM×1 + BPJS×3), MaxPasien = 10
			var expected = new[]
			{
				"UMUM","BPJS","BPJS","BPJS",   // cycle 1
				"UMUM","BPJS","BPJS","BPJS",   // cycle 2
				"UMUM","BPJS"                  // cycle 3, cut at 10
			};

			// Act
			var model = AntrianMapModel.CreateFromJadwal(BuildJadwal(PatternUmumBpjs(), 10),
				DateOnly.FromDayNumber(1));

			// Assert
			model.TotalSlotCount.Should().Be(10);
			var slots = model.ListMap.OrderBy(x => x.NoUrut).ToList();
			for (var i = 0; i < expected.Length; i++)
				slots[i].Flag.Should().Be(expected[i], $"slot {i + 1} should be {expected[i]}");
		}

		[Fact]
		public void CreateFromJadwal_ShouldSeedExactCycle_WhenMaxPasienEqualsOneCycleLength()
		{
			// cycle length = 4, MaxPasien = 4 → exactly one full cycle, no overflow
			var model = AntrianMapModel.CreateFromJadwal(BuildJadwal(PatternUmumBpjs(), 4),
				DateOnly.FromDayNumber(1));

			model.TotalSlotCount.Should().Be(4);
			var slots = model.ListMap.OrderBy(x => x.NoUrut).ToList();
			slots[0].Flag.Should().Be("UMUM");
			slots[1].Flag.Should().Be("BPJS");
			slots[2].Flag.Should().Be("BPJS");
			slots[3].Flag.Should().Be("BPJS");
		}

		[Fact]
		public void CreateFromJadwal_ShouldCutMidCycle_WhenMaxPasienIsNotMultipleOfCycle()
		{
			// MaxPasien = 6: full cycle (4) + 2 more (UMUM, BPJS)
			var model = AntrianMapModel.CreateFromJadwal(BuildJadwal(PatternUmumBpjs(), 6),
				DateOnly.FromDayNumber(1));

			model.TotalSlotCount.Should().Be(6);
			var slots = model.ListMap.OrderBy(x => x.NoUrut).ToList();
			slots[4].Flag.Should().Be("UMUM");  // start of second cycle
			slots[5].Flag.Should().Be("BPJS");  // cut here
		}

		[Fact]
		public void CreateFromJadwal_ShouldSeedNothing_WhenPatternIsEmpty()
		{
			// Default pattern has empty Pttrn
			var model = AntrianMapModel.CreateFromJadwal(
				BuildJadwal(AntrianPatternType.Default, 10),
				DateOnly.FromDayNumber(1));

			model.TotalSlotCount.Should().Be(0);
		}

		[Fact]
		public void CreateFromJadwal_ShouldSeedNothing_WhenMaxPasienIsZero()
		{
			var model = AntrianMapModel.CreateFromJadwal(
				BuildJadwal(PatternUmumBpjs(), 0),
				DateOnly.FromDayNumber(1));

			model.TotalSlotCount.Should().Be(0);
		}

		[Fact]
		public void CreateFromJadwal_ShouldRepeatSingleEntry_WhenPatternHasOneEntry()
		{
			// Single entry UMUM×3, MaxPasien = 5 → all 5 slots are UMUM
			var singlePattern = new AntrianPatternType("SINGLE", 0, 0,
				[new AntrianPatternItemType("UMUM", 3)]);

			var model = AntrianMapModel.CreateFromJadwal(BuildJadwal(singlePattern, 5),
				DateOnly.FromDayNumber(1));

			model.TotalSlotCount.Should().Be(5);
			model.ListMap.Should().OnlyContain(x => x.Flag == "UMUM");
		}

		[Fact]
		public void CreateFromJadwal_ShouldSeedAllSlotsNotTerpakai_WhenSeeded()
		{
			var model = AntrianMapModel.CreateFromJadwal(BuildJadwal(PatternUmumBpjs(), 10),
				DateOnly.FromDayNumber(1));

			model.ListMap.Should().OnlyContain(x => !x.IsTerpakai,
				"all freshly seeded slots must be available");
		}

		[Fact]
		public void CreateFromJadwal_ShouldAssignSequentialNoUrut_WhenSeeded()
		{
			var model = AntrianMapModel.CreateFromJadwal(BuildJadwal(PatternUmumBpjs(), 10),
				DateOnly.FromDayNumber(1));

			var noUrutList = model.ListMap.Select(x => x.NoUrut).ToList();
			noUrutList.Should().BeEquivalentTo(Enumerable.Range(1, 10),
				options => options.WithStrictOrdering());
		}

		[Theory]
		[InlineData(1,  "UMUM")]
		[InlineData(2,  "BPJS")]
		[InlineData(3,  "BPJS")]
		[InlineData(4,  "BPJS")]
		[InlineData(5,  "UMUM")]
		[InlineData(6,  "BPJS")]
		[InlineData(7,  "BPJS")]
		[InlineData(8,  "BPJS")]
		[InlineData(9,  "UMUM")]
		[InlineData(10, "BPJS")]
		public void CreateFromJadwal_ShouldHaveCorrectFlag_ForEachSlot(int noUrut, string expectedFlag)
		{
			var model = AntrianMapModel.CreateFromJadwal(BuildJadwal(PatternUmumBpjs(), 10),
				DateOnly.FromDayNumber(1));

			var slot = model.ListMap.Single(x => x.NoUrut == noUrut);
			slot.Flag.Should().Be(expectedFlag);
		}
	}

	// Helper factory for detil records to keep tests readable
	private static AntrianMapDetilModel CreateDetil(int noUrut, bool isTerpakai, string flag)
	{
		var pasien = new PasienReff($"P{noUrut}", $"Pasien{noUrut}", new DateOnly(1990,1,1), "M");
		return new AntrianMapDetilModel(noUrut, pasien.PasienName, pasien.PasienId, reffId: $"REF{noUrut}", flag: flag, isTerpakai: isTerpakai);
	}
}